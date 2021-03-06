﻿using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using SnakeBite.GzsTool;
using GzsTool.Core.Fpk;
using GzsTool.Core.Qar;

namespace SnakeBite.QuickMod
{
    public static class Methods
    {
        public static bool QuickCheck(string ZipFile)
        {
            string chunkPath = string.Empty;
            using (FileStream fs = new FileStream(ZipFile, FileMode.Open))
            using (ZipFile z = new ZipFile(fs))
            {
                foreach(ZipEntry ze in z)
                {
                    if(ze.Name.Contains("chunk"))
                    {
                        chunkPath = ze.Name;
                        break;
                    }
                    if(ze.Name.Contains("Assets"))
                    {
                        chunkPath = ze.Name.Substring(0, ze.Name.IndexOf("Assets"));
                        break;
                    }
                }
                if (chunkPath == string.Empty) return false;
                Debug.LogLine(String.Format("[QuickMod] Quick Check Success: {0}", chunkPath));
                return true;
            }
        }
        public static string GetModRoot(string ZipFile)
        {
            string chunkPath = null;
            using (FileStream fs = new FileStream(ZipFile, FileMode.Open))
            using (ZipFile z = new ZipFile(fs))
            {
                
                foreach (ZipEntry ze in z)
                {
                    var lcn = ze.Name.ToLower();
                    if (lcn.Contains("chunk"))
                    {
                        var chunkName = ze.Name.Substring(ze.Name.IndexOf("chunk"));
                        chunkName = chunkName.Substring(0,chunkName.IndexOf("/"));
                        chunkPath = ze.Name.Substring(0, ze.Name.IndexOf(chunkName) + chunkName.Length + 1);
                        break;
                    }
                    if (lcn.Contains("assets"))
                    {
                        chunkPath = lcn.Substring(0, lcn.IndexOf("assets"));
                        break;
                    }
                }
                return chunkPath.TrimEnd('/');
            }
        }
        public static void ExtractFiles(string ZipFile, string OutputDir)
        {
            if (Directory.Exists("_zip")) Directory.Delete("_zip", true);
            string ModRoot = GetModRoot(ZipFile);
            FastZip z = new FastZip();
            z.ExtractZip(ZipFile, "_zip", ModRoot + ".*");
            var f = Path.Combine("_zip", Tools.ToWinPath(ModRoot));
            foreach (var file in Directory.GetFiles(f, "*", SearchOption.AllDirectories))
            {
                
                var newPath = Path.Combine(OutputDir,file.Substring(f.Length+1));
                if (!Directory.Exists(Path.GetDirectoryName(newPath))) Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                File.Move(file, newPath);
            }
            if(Directory.Exists("_zip")) Directory.Delete("_zip", true);
        }
        public static void GenerateMgsv(string MgsvFile, string ModName, string SourceFolder)
        {
            ModEntry metaData = new ModEntry();
            metaData.Name = ModName;
            metaData.Author = "SnakeBite";
            metaData.MGSVersion.Version = "0.0.0.0";
            metaData.SBVersion.Version = ModManager.GetSBVersion().ToString();
            metaData.Version = "[QM]";
            metaData.Description = "[Generated by SnakeBite]";
            metaData.Website = "";

            List<ModFpkEntry> fpkEntries = new List<ModFpkEntry>();
            List<ModQarEntry> qarEntries = new List<ModQarEntry>();

            foreach(var File in Directory.GetFiles(SourceFolder, "*", SearchOption.AllDirectories))
            {
                string ShortFileName = File.Substring(SourceFolder.Length + 1);
                if(File.ToLower().EndsWith(".fpk") || File.ToLower().EndsWith(".fpkd"))
                {
                    // do fpk
                    var fpkCont = GzsLib.ListArchiveContents<FpkFile>(File);
                    foreach(var fpkFile in fpkCont)
                    {
                        fpkEntries.Add(new ModFpkEntry()
                        {
                            FpkFile = ShortFileName,
                            FilePath = fpkFile,
                            SourceType = FileSource.Mod
                        });
                    }
                } else
                {
                    // do qar
                    qarEntries.Add(new ModQarEntry() { FilePath = ShortFileName, SourceType = FileSource.Mod });
                }
            }

            metaData.ModQarEntries = qarEntries;
            metaData.ModFpkEntries = fpkEntries;
            metaData.SaveToFile(Path.Combine(SourceFolder, "metadata.xml"));

            FastZip makeZip = new FastZip();
            makeZip.CreateZip(MgsvFile, SourceFolder, true, ".*");
        }
    }
}
