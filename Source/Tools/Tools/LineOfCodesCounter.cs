using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Tools.Tools
{
    //Commandline-Arguments im VisualStudio: CountLineOfCodes ..\..\..
    internal class LineOfCodesCounter
    {
        public static string CountLineOfCodes(string projectFolder)
        {
            string curDir = new FileInfo(projectFolder).FullName + "\\";

            var files = Directory.GetFiles(projectFolder, "*.*", SearchOption.AllDirectories).Select(x => x.Remove(0, curDir.Length)).ToList().Where(x =>
                (x.EndsWith(".cs") || (x.Contains("Resources") && x.EndsWith(".txt"))) &&
                x.StartsWith("GraphicEngine8\\") == false &&
                x.Contains(".Designer.") == false &&
                x.Contains("AssemblyInfo.") == false
                ).ToList();

            var myFiles = files.Select(x => new FileWithAssociatedProjectFolder(x, projectFolder));

            var folders = new FolderCollection(myFiles);
            return folders.GetOverview();
        }

        class FolderCollection
        {
            private readonly ProjectFolder[] folders;

            public FolderCollection(IEnumerable<FileWithAssociatedProjectFolder> files)
            {
                var groupBy = from file in files
                              group file.Path by file.Folder into g
                              select new { Folder = g.Key, Files = g.ToArray() };


                var folder = groupBy.Select(x => new ProjectFolder(x.Folder, x.Files)).ToList();

                foreach (var f in folder)
                {
                    if (
                        (f.FolderName.EndsWith("Test") && folder.Any(x => f.FolderName.Substring(0, f.FolderName.Length - "Test".Length) == x.FolderName)) ||
                        (f.FolderName.EndsWith("Tests") && folder.Any(x => f.FolderName.Substring(0, f.FolderName.Length - "Tests".Length) == x.FolderName))
                        )
                        f.FolderType = ProjectFolder.Type.Test;
                    else
                        f.FolderType = ProjectFolder.Type.Code;

                    //Bibliotheken, die zwar keine Tests enthalten aber trotzdem ausschließlich für Tests da sind
                    if (f.FolderName == "UnitTestHelper") f.FolderType = ProjectFolder.Type.Test;
                    if (f.FolderName == "PdfHistogram") f.FolderType = ProjectFolder.Type.Test;
                }

                this.folders = folder.ToArray();
            }

            public string GetOverview()
            {
                StringBuilder str = new StringBuilder();

                foreach (var folder in this.folders)
                {
                    str.Append(folder.FolderName + "\t" + folder.LinesOfCode + "\t" + folder.FolderType + "\n");
                }

                int all = this.folders.Sum(x => x.LinesOfCode);
                int code = this.folders.Where(x => x.FolderType == ProjectFolder.Type.Code).Sum(x => x.LinesOfCode);
                int test = this.folders.Where(x => x.FolderType == ProjectFolder.Type.Test).Sum(x => x.LinesOfCode);
                str.Append("------------------------------------\nGesamt:\t" + $"{all} (Code={(code * 100 / all)}% Test={(test * 100 / all)}%)");

                return str.ToString();
            }
        }

        class ProjectFolder
        {
            public enum Type { NoValue, Code, Test}
            public Type FolderType { get; set; } = Type.NoValue;

            public string FolderName { get; private set; }
            public string[] Files { get; private set; }
            public int LinesOfCode { get; private set; }

            public ProjectFolder(string folderName, string[] files)
            {
                this.FolderName = folderName;
                this.Files = files;
                this.LinesOfCode = files.Sum(x => File.ReadAllLines(x).Length);
            }
        }

        class FileWithAssociatedProjectFolder
        {
            public string Folder;
            public string Path;

            public FileWithAssociatedProjectFolder(string path, string projectFolder)
            {
                int i = path.IndexOf('\\');
                this.Folder = path.Substring(0, i);
                this.Path = projectFolder + "\\" + path;
            }
        }
    }
}
