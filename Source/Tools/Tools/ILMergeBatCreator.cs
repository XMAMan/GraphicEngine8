using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools.Tools
{
    //Erzeugt eine Windows-Bat-Datei, welche ILMerge aufruft und damit eine Exe-Datei erstellt, wo alle Dlls reingemerged werden
    static class ILMergeBatCreator
    {
        public static void CreateILMergeBatFile(string exeFolder, string ilMergeFilePath, string outputFileName)
        {
            ilMergeFilePath = GetRelativePath(exeFolder, ilMergeFilePath);
            string relativeExeFolder = GetRelativePath(Directory.GetCurrentDirectory(), exeFolder);            

            string[] notHandleableForIlMerge = new string[] { "SlimDX.dll", "OpenTK.dll" };

            var dllFiles = Directory.GetFiles(exeFolder, "*.dll")
                .Where(x => notHandleableForIlMerge.All(y => x.EndsWith(y) == false))
                .Select(x => new FileInfo(x).Name)
                .ToList();

            var pdbFiles = Directory.GetFiles(relativeExeFolder, "*.pdb");

            StringBuilder str = new StringBuilder();
            str.AppendLine("rem Hiermit kann ich alle Dlls in die Exe mit packen");
            str.AppendLine("rem Hinweis: Newtsonsoft.Json kann nicht mehr deserialisieren, wenn ich das hier mache da er GraphicPanels.dll sucht aber nicht findet. D.h. die 2D- und 3D-Demoanwendung geht noch aber die Raytracing-Scenen nicht");
            str.AppendLine("rem Erzeuge erst mit dir /b *.dll eine DLL-Auflistung und entferne dort SlimDX.dll und OpenTK.dll");
            str.AppendLine("cd " + relativeExeFolder);
            str.AppendLine(ilMergeFilePath + " /targetplatform:v4 /useFullPublicKeyForReferences /out:GraphicTool1.exe GraphicTool.exe " + string.Join(" ", dllFiles));
            str.AppendLine("del *.pdb");
            str.AppendLine("mkdir temp");
            foreach (var file in notHandleableForIlMerge)
                str.AppendLine($"move {file} temp\\{file}");
            str.AppendLine("del *.dll");
            str.AppendLine("move temp\\* .");
            str.AppendLine("rmdir temp");
            str.AppendLine("del GraphicTool.exe");
            str.AppendLine("move GraphicTool1.exe GraphicTool.exe");
            str.AppendLine("cd " + GetRelativePath(exeFolder, Directory.GetCurrentDirectory()));

            File.WriteAllText(outputFileName, str.ToString());
        }

        //Quelle: https://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fromPath"/> or <paramref name="toPath"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath))
            {
                throw new ArgumentNullException("fromPath");
            }

            if (string.IsNullOrEmpty(toPath))
            {
                throw new ArgumentNullException("toPath");
            }

            Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
            Uri toUri = new Uri(AppendDirectorySeparatorChar(toPath));

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            if (relativePath == "") return ".";

            return relativePath;
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            // Append a slash only if the path is a directory and does not have a slash.
            if (!Path.HasExtension(path) &&
                !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }
    }
}
