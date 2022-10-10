using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GraphicMinimal;

namespace TriangleObjectGeneration
{
    public class WaveFrontMaterialFile
    {
        public WaveFrontMaterial[] Materials { get; private set; }

        public static List<string> NoneMaterials = new List<string>();

        public WaveFrontMaterialFile(string fileName)
        {
            string fileText = File.ReadAllText(fileName);
            List<WaveFrontMaterial> materials = new List<WaveFrontMaterial>();
            string[] blocks = fileText.Split(new string[] { "\n\n" }, StringSplitOptions.None);
            foreach (var block in blocks)
            {
                //if (block.StartsWith("newmtl ") && block.StartsWith("newmtl None") == false) //So fehlt beim Haus1 bei AddTestszene17_TheFifthElement die Texture da das Material None_Fenster4.png heißt. Das None-Material führt dazu, dass der GlossyPowExponent 0 statt 200 ist. Da aber die Brdf kein Gloss ist, hat das kein Effekt.
                if (block.StartsWith("newmtl "))
                {
                    var material = new WaveFrontMaterial(block);
                    if (material.TextureFile != null) material.TextureFile = Path.GetDirectoryName(fileName) + "\\" + material.TextureFile;
                    materials.Add(material);

                }
            }
            this.Materials = materials.ToArray();
        }

        public bool ContainsMaterial(string materialName)
        {
            return this.Materials.Any(x => x.Name == materialName);
        }

        public WaveFrontMaterial GetMaterialByName(string materialName)
        {
            return this.Materials.First(x => x.Name == materialName);
        }
    }

    public class WaveFrontMaterial
    {
        public string Name { get; private set; }
        public Vector3D DiffuseColor { get; private set; } = new Vector3D(0, 0, 0);
        public string TextureFile { get; set; } = null;//Relativ zur Obj-Datei oder als Absolutpfad

        public Vector3D PhongColor { get; private set; } = new Vector3D(0, 0, 0);
        public float PhongExponent = float.NaN; //0 .. 1000

        public WaveFrontMaterial(string textBlock)
        {
            string[] lines = textBlock.Split(new string[] { "\n" }, StringSplitOptions.None);
            lines = lines.Select(x => x.Trim()).ToArray();
            this.Name = lines[0].Split(' ')[1];

            for (int i=1;i<lines.Length;i++)
            {
                int space = lines[i].IndexOf(' ');
                if (space != -1)
                {
                    string command = lines[i].Substring(0, space);
                    string argumens = lines[i].Substring(space + 1);
                    ParseCommand(command, argumens);
                }                
            }
        }

        private void ParseCommand(string command, string arguments)
        {
            switch(command)
            {
                case "Ns":
                    this.PhongExponent = float.Parse(arguments.Replace('.', ','));
                    break;
                case "Kd":
                    this.DiffuseColor = ParseVector(arguments);
                    break;
                case "Ks":
                    this.PhongColor = ParseVector(arguments);
                    break;
                //Ks = SpecularColor
                //Ka = AmbientColor
                //Ke = EmissionColor
                //Ni = Brechungsindex
                case "map_Kd":
                    this.TextureFile = arguments;
                    if (this.TextureFile.Contains("\\")) this.TextureFile = this.TextureFile.Split('\\').Last();
                    break;
            }
        }

        private static Vector3D ParseVector(string text)
        {
            var fields = text.Split(' ');
            return new Vector3D(float.Parse(fields[0].Replace('.', ',')),
                                   float.Parse(fields[1].Replace('.', ',')),
                                   float.Parse(fields[2].Replace('.', ',')));
        }
    }
}
