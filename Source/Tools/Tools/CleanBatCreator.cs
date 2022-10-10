using System.IO;
using System.Linq;
using System.Text;

namespace Tools.Tools
{
    static class CleanBatCreator
    {
        public static void CreateCleanFile(string projectFolder)
        {
            var files = Directory
                .GetFiles(projectFolder, "*.*", SearchOption.AllDirectories)
                .ToList()
                .Select(x => x.Remove(0, (projectFolder + "\\").Length))
                .Where(x =>
                    (x.EndsWith(".dll") || x.EndsWith(".pdb")) &&
                    x.Split('\\').Length > 2 &&
                    x.Contains("OpenEXRDLL") == false &&
                    x.StartsWith("packages") == false
                    ).ToList();

            var binFiles = Directory
                .GetFiles(projectFolder, "*.*", SearchOption.AllDirectories)
                .ToList()
                .Select(x => x.Remove(0, (projectFolder + "\\").Length))
                .Where(x => x.Contains("\\bin\\"))
                .Select(x => x.Substring(0, x.IndexOf("\\bin")) + "\\bin")
                .Distinct()
                .SelectMany(x => Directory.GetFiles(projectFolder + "\\" + x, "*.*", SearchOption.AllDirectories))
                .Select(x => x.Remove(0, (projectFolder + "\\").Length))
                .ToList();

            binFiles = binFiles.Where(x => files.Contains(x) == false).ToList();

            files.AddRange(binFiles);

            var objFolders = Directory
                .GetFiles(projectFolder, "*.*", SearchOption.AllDirectories)
                .ToList()
                .Select(x => x.Remove(0, (projectFolder + "\\").Length))
                .Where(x => x.Contains("\\obj\\"))
                .Select(x => x.Substring(0, x.IndexOf("\\obj")) + "\\obj")
                .Distinct()
                .SelectMany(x => Directory.GetDirectories(projectFolder + "\\" + x))
                .Select(x => x.Remove(0, (projectFolder + "\\").Length))
                .ToList();

            files.AddRange(Directory.GetFiles(projectFolder + "\\UnitTestResults").Select(x => x.Remove(0, (projectFolder + "\\").Length)));

            StringBuilder str = new StringBuilder();
            str.AppendLine(@"del UnitTestResults\ImageCreatorSaveFolder\* /Q");
            str.AppendLine(@"del UnitTestResults\SceneBatfiles\* /Q");
            foreach (var file in files)
            {
                str.Append("del " + file + System.Environment.NewLine);
            }

            foreach (var folder in objFolders)
            {
                str.Append($"rmdir {folder} /s /q" + System.Environment.NewLine);
            }

            string fileContent = str.ToString();

            File.WriteAllText(projectFolder + "\\Clean.bat", fileContent);
        }
    }
}
