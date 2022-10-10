using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using RayObjects;
using System;
using System.Drawing;
using System.Linq;
using TriangleObjectGeneration;

namespace GraphicPanels.Helper
{
    //Erzeugt aus einer Obj-Datei eine Normal+Heighmap (Steht nicht im TexturHelper-Projekt, da ich den IntersectionFinder benötige)
    //Anwendung: Bitmap bumpmap = GraphicPanel3D.CreateBumpmapFromObjFile(Scenes.DataDirectory + "Huckel.obj", new Size(256, 256), 0.7f)
    public class ObjToHeightMapConverter
    {
        public Bitmap CreateBumpmap(string objFile, Size size, float border)
        {
            float[,] map = CreateHeightMap(objFile, new Size(size.Width / 2, size.Height / 2), border, out BoundingBox box);
            return ConvertHeightMapToBitmap(map, box);
        }

        //Gibt die Heightmap normiert in den 0..1-Bereich zurück
        //border = Prozentzahl, die sagt wie breit der Rand ist (0 = Kein Rand)
        private float[,] CreateHeightMap(string objFile, Size size, float border, out BoundingBox box)
        {
            var drawingObjects = WaveFrontLoader.LoadObjectsFromFile(objFile, false, new ObjectPropertys() { ShowFromTwoSides = true });
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(drawingObjects);
            var intersectionFinder = new IntersectionFinder(rayObjects.Cast<IIntersecableObject>().ToList(), null);
            box = new BoundingBox(drawingObjects.Select(x => x.GetBoundingBoxFromObject()));

            RectangleF xz = new RectangleF(box.Min.X, box.Min.Z, box.XSize, box.ZSize);
            xz = new RectangleF(xz.X - xz.Width / 2 * border, xz.Y - xz.Height / 2 * border, xz.Width + (xz.Width / 2 * border) * 2, xz.Height + (xz.Height / 2 * border) * 2);

            //Schaue enthlang der Y-Achse
            float[,] map = new float[size.Width, size.Height];
            for (int x = 0; x < size.Width; x++)
                for (int y = 0; y < size.Height; y++)
                {
                    float xf = x / (float)size.Width;
                    float yf = y / (float)size.Height;
                    var start = new Vector3D(xz.X + xz.Width * xf, box.Max.Y + 1, xz.Y + xz.Height * yf);
                    var ray = new Ray(start, new Vector3D(0, -1, 0));
                    var point = intersectionFinder.GetIntersectionPoint(ray, 0);
                    float height = point != null ? point.Position.Y : box.Min.Y;
                    map[x, y] = (height - box.Min.Y) / box.YSize;
                }

            return map;
        }

        private Bitmap ConvertHeightMapToBitmap(float[,] map, BoundingBox box)
        {
            //Um diesen Faktor muss ich die Höhe skalieren, damit es beim Normalen-Erstellung keine Skalierung gibt
            float fh = map.GetLength(0) / box.XSize * box.YSize * 2;

            //Erzeuge doppelt so große Bumpmap (Es entsteht ein ein Pixel breiter Rand)
            Bitmap bumpmap = new Bitmap(map.GetLength(0) * 2, map.GetLength(1) * 2);
            for (int y = 0; y < map.GetLength(1) - 1; y++)
                for (int x = 0; x < map.GetLength(0) - 1; x++)
                {
                    Vector3D p00 = new Vector3D(x * 2 + 1, y * 2 + 1, map[x, y] * fh); //Versetze um halben Pixel nach rechts unten (Desswegen +1 bei x und y)
                    Vector3D p10 = new Vector3D(x * 2 + 2, y * 2 + 1, map[x + 1, y] * fh); //Rechts daneben von p00
                    Vector3D p01 = new Vector3D(x * 2 + 1, y * 2 + 2, map[x, y + 1] * fh); //Darunter von p00
                    Vector3D normal = Vector3D.Cross(Vector3D.Normalize(p10 - p00), Vector3D.Normalize(p01 - p00));
                    if (normal.Z < 0) throw new Exception("The normal must always point upwards");

                    Vector3D N = (normal / 2.0f + new Vector3D(0.5f, 0.5f, 0.5f)) * 255;
                    bumpmap.SetPixel(x * 2, y * 2, Color.FromArgb((int)(map[x, y] * 255), (int)N.X, (int)N.Y, (int)N.Z));
                    bumpmap.SetPixel(x * 2 + 1, y * 2, Color.FromArgb((int)(map[x + 1, y] * 255), (int)N.X, (int)N.Y, (int)N.Z));
                    bumpmap.SetPixel(x * 2, y * 2 + 1, Color.FromArgb((int)(map[x, y + 1] * 255), (int)N.X, (int)N.Y, (int)N.Z));
                    bumpmap.SetPixel(x * 2 + 1, y * 2 + 1, Color.FromArgb((int)(map[x + 1, y + 1] * 255), (int)N.X, (int)N.Y, (int)N.Z));

                    //Nur die Höhenwerte
                    //bumpmap.SetPixel(x, y, Color.FromArgb((int)(map[x, y] * 255), (int)(map[x, y] * 255), (int)(map[x, y] * 255))); 

                    //Nur die Normale
                    /*bumpmap.SetPixel(x * 2, y * 2, Color.FromArgb((int)N.X, (int)N.Y, (int)N.Z));
                    bumpmap.SetPixel(x * 2 + 1, y * 2, Color.FromArgb((int)N.X, (int)N.Y, (int)N.Z));
                    bumpmap.SetPixel(x * 2, y * 2 + 1, Color.FromArgb((int)N.X, (int)N.Y, (int)N.Z));
                    bumpmap.SetPixel(x * 2 + 1, y * 2 + 1, Color.FromArgb((int)N.X, (int)N.Y, (int)N.Z));*/
                }

            //Beschreibe noch den kompletten Rand mit Werten
            {
                Vector3D N = (new Vector3D(0, 0, 1) / 2.0f + new Vector3D(0.5f, 0.5f, 0.5f)) * 255;
                var c = Color.FromArgb(0, (int)N.X, (int)N.Y, (int)N.Z);
                for (int x = 1; x < bumpmap.Width - 1; x++)
                {
                    bumpmap.SetPixel(x, 0, c);
                    bumpmap.SetPixel(x, bumpmap.Height - 1, c);
                    bumpmap.SetPixel(x, bumpmap.Height - 2, c);
                }
                for (int y = 0; y < bumpmap.Height - 1; y++)
                {
                    bumpmap.SetPixel(0, y, c);
                    bumpmap.SetPixel(bumpmap.Width - 2, y, c);
                    bumpmap.SetPixel(bumpmap.Width - 1, y, c);
                }
                bumpmap.SetPixel(0, 0, c);
                bumpmap.SetPixel(bumpmap.Width - 1, 0, c);
                bumpmap.SetPixel(0, bumpmap.Height - 1, c);
                bumpmap.SetPixel(bumpmap.Width - 1, bumpmap.Height - 1, c);
            }


            return bumpmap;
        }


    }
}
