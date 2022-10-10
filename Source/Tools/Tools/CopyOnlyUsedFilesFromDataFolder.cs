using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tools.Tools
{
    //Damit der Data-Ordner im Deploymentpackage nicht so groß ist, kopiere ich hiermit nur die Dateien aus dem Data-Ordner,
    //welche laut den Scenes.Bat/Json-Dateien auch nur genutzt werden
    static class CopyOnlyUsedFilesFromDataFolder
    {
        public static void CopyOnlyUsedData(string scenesFolder, string dataSource, string dataDestination)
        {
            var usedFiles = GetUsedDataFolderFiles(scenesFolder, dataSource);
            CopyFilesToFolder(usedFiles.Select(x => dataSource + x).ToArray(), dataDestination);
        }

        private static void CopyFilesToFolder(string[]files, string folder)
        {
            for (int i=0;i<files.Length;i++)
            {
                string file = files[i];
                try
                {
                    string source = file;
                    string destination = folder + new FileInfo(file).Name;

                    File.Copy(source, destination);
                }catch(Exception ex)
                {
                    throw new Exception($"Index={i}; File={file}", ex);
                }
                
            }
        }

        private static string[] GetUsedDataFolderFiles(string scenesFolder, string dataSource)
        {
            var jsonFiles = ExtractJsonFilesFromScenesFolder(scenesFolder);
            var dataFiles = ExtractAllDataFilesFromJsonFiles(dataSource, jsonFiles);
            var mtlAuxFiles = GetWaveFrontMtlAndAuxFiles(dataSource, dataFiles);
            var searchMaskFiles = ExtractSearchMaskFilesFromScenesFolder(scenesFolder);

            List<string> usedFiles = new List<string>();
            usedFiles.AddRange(jsonFiles);
            usedFiles.AddRange(dataFiles);
            usedFiles.AddRange(mtlAuxFiles);
            usedFiles.AddRange(searchMaskFiles);
            return usedFiles.Select(x => new { Low = x.ToLower(), Name = x }).GroupBy(x => x.Low).Select(x => x.First().Name).ToArray();
        }

        private static string[] ExtractSearchMaskFilesFromScenesFolder(string scenesFolder)
        {
            var sceneBatFiles = Directory.GetFiles(scenesFolder, "*.bat");
            List<string> searchMaskFiles = new List<string>();
            string[] searchPatterns = new string[] { "-searchMask", "-mask" };
            foreach (var scene in sceneBatFiles)
            {             
                foreach (var pat in searchPatterns)
                {
                    foreach (var line in File.ReadAllLines(scene).Where(x => x.Contains(pat)))
                    {
                        string searchMask = line.Substring(line.IndexOf(pat) + pat.Length).Split(' ')[1].Trim();
                        searchMaskFiles.Add(searchMask);
                    }
                }
            }
            return searchMaskFiles.Distinct().ToArray();
        }

        private static string[] ExtractJsonFilesFromScenesFolder(string scenesFolder)
        {
            var sceneBatFiles = Directory.GetFiles(scenesFolder, "*.bat");
            List<string> jsonFiles = new List<string>();
            foreach (var scene in sceneBatFiles)
            {
                foreach (var line in File.ReadAllLines(scene).Where(x => x.Contains("_json.txt")))
                {
                    string json = line.Substring(line.IndexOf("Data\\") + "Data\\".Length).Split(' ')[0].Trim();
                    jsonFiles.Add(json);
                }
            }
            return jsonFiles.Distinct().ToArray();
        }

        private static string[] ExtractAllDataFilesFromJsonFiles(string dataSource, string[] jsonFiles)
        {
            List<string> allDataFiles = new List<string>();
            foreach (var json in jsonFiles)
            {
                var dataFiles = ExtractDataFilesFromJSonFile(dataSource,dataSource + json);
                allDataFiles.AddRange(dataFiles);
            }
            return allDataFiles.Distinct().ToArray();
        }

        private static string[] ExtractDataFilesFromJSonFile(string dataSource, string jsonFile)
        {
            List<string> dataFiles = new List<string>();
            foreach (var line in File.ReadAllLines(jsonFile).Where(x => x.Contains("<DataFolder>")))
            {
                string dataFile = line.Substring(line.IndexOf("<DataFolder>") + "<DataFolder>".Length).Split('"')[0];
                if (File.Exists(dataSource + dataFile))
                    dataFiles.Add(dataFile);
            }
            return dataFiles.Distinct().ToArray();
        }

        private static string[] GetWaveFrontMtlAndAuxFiles(string dataSource, string[] dataFiles)
        {
            List<string> files = new List<string>();
            foreach (var file in dataFiles)
            {
                if (file.EndsWith(".obj"))
                {
                    files.AddRange(GetMtlAndAuxFileFromObjFile(dataSource, dataSource + file));
                }
            }
            return files.ToArray();
        }

        private static string[] GetMtlAndAuxFileFromObjFile(string dataSource, string objFile)
        {
            List<string> files = new List<string>();
            var mtlFiles = File.ReadLines(objFile).Where(x => x.StartsWith("mtllib")).Select(x => x.Split(' ')[1]).ToList();
            if (mtlFiles.Any())
            {
                string materialFileName = dataSource + mtlFiles.First();
                if (File.Exists(materialFileName))
                {
                    files.Add(new FileInfo(materialFileName).Name);
                    string auxiliaryFileName = materialFileName.Replace(".mtl", ".obj") + ".aux";
                    if (File.Exists(auxiliaryFileName))
                    {
                        files.Add(new FileInfo(auxiliaryFileName).Name);
                    }
                }
            }
            return files.ToArray();
        }
    }
}
