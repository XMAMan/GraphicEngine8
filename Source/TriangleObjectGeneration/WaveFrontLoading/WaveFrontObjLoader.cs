using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphicMinimal;
using GraphicGlobal;
using System.IO;

namespace TriangleObjectGeneration
{
    static class WaveFrontObjLoader
    {
        public static List<TriangleObject> LoadWaveFrontFile(string fileName, bool splitInManyObjects, bool takeNormalsFromFile, out string materialFile)
        {
            if (!File.Exists(fileName)) throw new FileNotFoundException(fileName);
            
            string aufrufparameter = "Load3DSMaxFileInWavefrontFormat:" + fileName + ":" + splitInManyObjects + ":";

            WaveFrontObjFile file = new WaveFrontObjFile(File.ReadAllText(fileName));

            materialFile = file.MaterialFileName;
            materialFile = new FileInfo(fileName).DirectoryName + "\\" + materialFile;

            if (splitInManyObjects == false)
            {
                TriangleList trianglesList = new TriangleList();
                file.Objects.SelectMany(x => x.Triangles).ToList().ForEach(x => trianglesList.AddTriangle(x.T.V[0], x.T.V[1], x.T.V[2]));
                trianglesList.TransformToCoordinateOrigin();
                if (takeNormalsFromFile == false) trianglesList.SetNormals();
                trianglesList.Name = aufrufparameter;
                TriangleObject triangleObject = trianglesList.GetTriangleObject();
                if (triangleObject == null) return new List<TriangleObject>();
                return new List<TriangleObject>() { trianglesList.GetTriangleObject() };
            }
            else
            {
                List<TriangleObject> objList = new List<TriangleObject>();
                foreach (var obj in file.Objects)
                {
                    if (obj.Triangles.Any() == false) continue;
                    TriangleList trianglesList = new TriangleList();
                    //obj.Triangles.ForEach(x => trianglesList.AddTriangle(x.T.V[0], x.T.V[1], x.T.V[2])); //Mit Kontrolle das kein Dreieck doppelt ist
                    obj.Triangles.ForEach(x => trianglesList.Triangles.Add(new Triangle(x.T.V[0], x.T.V[1], x.T.V[2]))); //Ohne Kontrolle, ob Dreieck doppelt ist
                    //trianglesList.TransformToCoordinateOrigin();//Beim Split darf man das nicht zum Koordinatenursprung verschieben, da die Position-Angabe noch gesetzt wird
                    if (takeNormalsFromFile == false) trianglesList.SetNormals();
                    trianglesList.Name = aufrufparameter + obj.Name;
                    TriangleObject triangleObject = trianglesList.GetTriangleObject();
                    if (triangleObject == null) throw new Exception(trianglesList.Name + " nicht ladebar, da Dreiecks-Normalen nicht ok sind");
                    triangleObject.MaterialName = obj.MaterialName;
                    if (triangleObject != null) objList.Add(triangleObject);
                }
                return objList;
            }
        }
    }

    //obj-Datei
    public class WaveFrontObjFile
    {
        public string Header { get; private set; }
        public string MaterialFileName { get; private set; } = "";
        public List<WaveFrontObject> Objects { get; private set; }

        //In den WaveFront-Datei kann eine f-Zeile auf Vertice verweisen, die garnicht in sein o-Abschnitt sind
        public List<Vector3D> VertexPositions { get; private set; }
        public List<Vector3D> TextureCoordinates { get; private set; }
        public List<Vector3D> Normals { get; private set; }

        public WaveFrontObjFile(string fileLines)
        {
            string[] lines = fileLines.Replace("\r", "").Split('\n');

            this.VertexPositions = new List<Vector3D>();
            this.TextureCoordinates = new List<Vector3D>();
            this.Normals = new List<Vector3D>();

            this.Objects = new List<WaveFrontObject>();
            int index = 0;
            this.Header = ReadHeader(lines, ref index);
            WaveFrontObject obj;
            while ((obj = ReadWaveFrontObject(lines, ref index)) != null)
            {
                this.Objects.Add(obj);
            }
            if (index != lines.Length) throw new Exception("Read Error");
        }

        public WaveFrontObjFile(string header, List<WaveFrontObject> objects)
        {
            this.Header = header;
            this.Objects = objects;
        }

        public string GetAllLines()
        {
            StringBuilder str = new StringBuilder();
            str.Append(Header);
            this.Objects.ForEach(x => str.Append(x.FileLines));
            return str.ToString();
        }

        private string ReadHeader(string[] lines, ref int index)
        {
            StringBuilder str = new StringBuilder();
            
            while (lines[index].StartsWith("o ") == false)
            {
                if (lines[index].StartsWith("mtllib")) this.MaterialFileName = lines[index].Split(' ')[1];
                str.Append(lines[index] + System.Environment.NewLine);
                index++;
            }
            return str.ToString();
        }

        private WaveFrontObject ReadWaveFrontObject(string[] lines, ref int index)
        {
            if (index >= lines.Length || lines[index].StartsWith("o ") == false) return null;

            string name = lines[index].Substring(2);

            StringBuilder str = new StringBuilder();

            do
            {
                str.Append(lines[index] + System.Environment.NewLine);
                index++;
            } while (index < lines.Length && lines[index].StartsWith("o ") == false);
            return new WaveFrontObject(name, str.ToString(), this.VertexPositions, this.TextureCoordinates, this.Normals);
        }
    }

    public class WaveFrontObject
    {
        public string Name { get; private set; }
        public string FileLines { get; private set; }

        public List<WaveFrontTriangle> Triangles { get; private set; }
        public string MaterialName { get; private set; }

        public WaveFrontObject(string name, string fileLines, List<Vector3D> vertexPositions, List<Vector3D> textureCoordinates, List<Vector3D> normals)
        {
            this.Name = name;
            this.FileLines = fileLines;

            this.Triangles = new List<WaveFrontTriangle>();

            string[] lines = fileLines.Replace("\r", "").Split('\n');
            
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("v "))//Vertex-Position
                {
                    string[] felder = lines[i].Replace("  ", " ").Replace('.', ',').Split(' ');
                    Vector3D pos = new Vector3D((float)Convert.ToDouble(felder[1]), (float)Convert.ToDouble(felder[2]), (float)Convert.ToDouble(felder[3]));
                    vertexPositions.Add(pos);
                }

                if (lines[i].StartsWith("vt "))//Vertex-Texturcoordinaten
                {
                    string[] felder = lines[i].Replace('.', ',').Split(' ');
                    Vector3D pos;
                    if (felder.Length >= 4)
                    {
                        pos = new Vector3D((float)Convert.ToDouble(felder[1]), (float)Convert.ToDouble(felder[2]), (float)Convert.ToDouble(felder[3]));
                    }
                    else
                    {
                        pos = new Vector3D((float)Convert.ToDouble(felder[1]), (float)Convert.ToDouble(felder[2]), 0);
                    }
                    pos = new Vector3D(1 - pos.X, 1 - pos.Y, 1 - pos.Z);
                    textureCoordinates.Add(pos);
                }

                if (lines[i].StartsWith("vn "))//Vertex-Normale
                {
                    string[] felder = lines[i].Replace("  ", " ").Replace('.', ',').Split(' ');
                    Vector3D pos = new Vector3D((float)Convert.ToDouble(felder[1]), (float)Convert.ToDouble(felder[2]), (float)Convert.ToDouble(felder[3]));
                    normals.Add(pos);
                }

                if (lines[i].StartsWith("f "))//Dreieck(Fragment)
                {
                    string[] felder = lines[i].Split(' ');
                    string[] feld1 = felder[1].Split('/');
                    string[] feld2 = felder[2].Split('/');
                    string[] feld3 = felder[3].Split('/');
                    Vector3D pos1 = vertexPositions[Convert.ToInt32(feld1[0]) - 1];
                    Vector3D tex1;
                    if (feld1.Length > 1 && feld1[1] != "") tex1 = textureCoordinates[Convert.ToInt32(feld1[1]) - 1]; else tex1 = new Vector3D(0, 0, 0);
                    Vector3D nor1 = null; if (feld1.Length > 2 && feld1[2] != "") nor1 = normals[Convert.ToInt32(feld1[2]) - 1]; 

                    Vector3D pos2 = vertexPositions[Convert.ToInt32(feld2[0]) - 1];
                    Vector3D tex2;
                    if (feld2.Length > 1 && feld2[1] != "") tex2 = textureCoordinates[Convert.ToInt32(feld2[1]) - 1]; else tex2 = new Vector3D(0, 0, 0);
                    Vector3D nor2 = null; if (feld2.Length > 2 && feld2[2] != "") nor2 = normals[Convert.ToInt32(feld2[2]) - 1];

                    Vector3D pos3 = vertexPositions[Convert.ToInt32(feld3[0]) - 1];
                    Vector3D tex3;
                    if (feld3.Length > 1 && feld3[1] != "") tex3 = textureCoordinates[Convert.ToInt32(feld3[1]) - 1]; else tex3 = new Vector3D(0, 0, 0);
                    Vector3D nor3 = null; if (feld3.Length > 2 && feld3[2] != "") nor3 = normals[Convert.ToInt32(feld3[2]) - 1];

                    //Bei Blender ist die u==x-Koordinate geflippt. Deswegen steht hier 1-tex.X anstatt tex.X
                    Vertex V1 = new Vertex(pos1.X, pos1.Y, pos1.Z, 1 - tex1.X, tex1.Y) { Normal = nor1 };
                    Vertex V2 = new Vertex(pos2.X, pos2.Y, pos2.Z, 1 - tex2.X, tex2.Y) { Normal = nor2 };
                    Vertex V3 = new Vertex(pos3.X, pos3.Y, pos3.Z, 1 - tex3.X, tex3.Y) { Normal = nor3 };

                    Vector3D normalUnnormalized = Vector3D.Cross(V2.Position - V1.Position, V3.Position - V1.Position);
                    float length = normalUnnormalized.Length();
                    if (length > 0.00001f)
                    {
                        Vector3D normal = normalUnnormalized / length;
                        if (Math.Abs(normal.Length() - 1) < 0.1f)
                        {
                            bool flipNormal = false;
                            if (V1.Normal != null)
                            {
                                Vector3D mdl = 1.0f / 3 * (V1.Normal + V2.Normal + V3.Normal);
                                if (normal * mdl < 0) flipNormal = true;
                            }
                           
                            if (flipNormal == false)
                                this.Triangles.Add(new WaveFrontTriangle(lines[i], new Triangle(V1, V2, V3)));
                            else
                                this.Triangles.Add(new WaveFrontTriangle(lines[i], new Triangle(V3, V2, V1)));
                        }
                    }                    
                }

                if (lines[i].StartsWith("usemtl "))//Material
                {
                    this.MaterialName = lines[i].Split(' ')[1];
                }
            }
        }

        public class WaveFrontTriangle
        {
            public string Line;
            public Triangle T;

            public WaveFrontTriangle(string line, Triangle t)
            {
                this.Line = line;
                this.T = t;
            }
        }
    }
}
