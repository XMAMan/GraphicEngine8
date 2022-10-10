using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using GraphicGlobal;

namespace TriangleObjectGeneration
{
    static class Bitmap3D
    {
        public static TriangleList Create3DBitmap(Bitmap image, int depth)
        {
            RectangleList list = GetRectanglesFromScanline(image);         // Schritt 1: Per Scanline 2D-Rechtecke erzeugen
            list.MergeNeighbors();                                         // Schritt 2: 2D-Rechtecke zusammenfassen
            list.GenerateSurfaceAtTopAndBelow();                           // Schritt 3: Bei den Rechtecken oben und unten schauen, ob eine Wand hin muss und wo nicht

            int zDepth = (int)(Math.Min(image.Width, image.Height) / 10.0f * depth);
            var triangles = list.GetTriangles(image.Width, image.Height, zDepth);    // Schritt 4: Aus 2D-Recheckten 3D-Dreiecke erzeugen

            List<Vertex> uvCache = new List<Vertex>();
            TriangleList obj = new TriangleList();                          // Schritt 5: Aus Dreiecken TriangleList erzeugen
            foreach (var t in triangles)
            {
                obj.AddTriangle(t.V1, t.V2, t.V3);

                string key = t.V1.Position.ToString();
                if (uvCache.Any(x => (x.Position - t.V1.Position).Length() < 0.1f))
                {
                    var uv = uvCache.First(x => (x.Position - t.V1.Position).Length() < 0.1f);
                    t.V1.TexcoordU = uv.TexcoordU;
                    t.V1.TexcoordV = uv.TexcoordV;
                }
                else
                {
                    uvCache.Add(t.V1);
                }

                key = t.V2.Position.ToString();
                if (uvCache.Any(x => (x.Position - t.V2.Position).Length() < 0.1f))
                {
                    var uv = uvCache.First(x => (x.Position - t.V2.Position).Length() < 0.1f);
                    t.V2.TexcoordU = uv.TexcoordU;
                    t.V2.TexcoordV = uv.TexcoordV;
                }
                else
                {
                    uvCache.Add(t.V2);
                }

                key = t.V3.Position.ToString();
                if (uvCache.Any(x => (x.Position - t.V3.Position).Length() < 0.1f))
                {
                    var uv = uvCache.First(x => (x.Position - t.V3.Position).Length() < 0.1f);
                    t.V3.TexcoordU = uv.TexcoordU;
                    t.V3.TexcoordV = uv.TexcoordV;
                }
                else
                {
                    uvCache.Add(t.V3);
                }
            }
            obj.TransformToCoordinateOrigin();
            obj.SetNormals();

            return obj;
        }

        class MyTriangle
        {
            public Vertex V1, V2, V3;

            public MyTriangle(Vertex v1, Vertex v2, Vertex v3)
            {
                this.V1 = v1;
                this.V2 = v2;
                this.V3 = v3;

                if (v1.TextcoordVector == v2.TextcoordVector || v1.TextcoordVector == v3.TextcoordVector || v2.TextcoordVector == v3.TextcoordVector) throw new Exception("sdf");
            }
        }

        class Rectangle
        {
            public class SimpleRectangle
            {
                public int StartX;
                public int EndX;

                public SimpleRectangle(int startX, int endX)
                {
                    this.StartX = startX;
                    this.EndX = endX;
                }
            }

            public int X;
            public int Y;
            public int Width;
            public int Height;

            public bool UsedForMerge; // Flag, um anzuzeigen, ob dieses Rechteck schon gemergt wurde

            public List<SimpleRectangle> atTop = new List<SimpleRectangle>();
            public List<SimpleRectangle> above = new List<SimpleRectangle>();

            public Rectangle(int x, int y, int width, int height)
            {
                this.X = x;
                this.Y = y;
                this.Width = width;
                this.Height = height;
            }

            public void GenerateSurfaceAtTopAndBelow(RectangleList recList)
            {
                bool isRecField = false;
                int startX = 0;

                //Schritt 1: Wände oben erzeugen
                for (int x = this.X; x < this.X + this.Width; x++)
                {
                    bool newIsRecField = !recList.IsRecField(x, this.Y - 1);
                    if (isRecField == false && newIsRecField == true) //Rechteck beginnt
                    {
                        startX = x;
                    }
                    else if (isRecField == true && (newIsRecField == false || x == this.X + this.Width - 1)) //Rechteck endet
                    {
                        int minus = 1;
                        if (x == this.X + this.Width - 1) minus = 0;
                        this.atTop.Add(new SimpleRectangle(startX, x - minus));
                    }
                    isRecField = newIsRecField;
                }

                isRecField = false;
                //Schritt 2: Wände unten erzeugen
                for (int x = this.X; x < this.X + this.Width; x++)
                {
                    bool newIsRecField = !recList.IsRecField(x, this.Y + this.Height);
                    if (isRecField == false && newIsRecField == true) //Rechteck beginnt
                    {
                        startX = x;
                    }
                    else if (isRecField == true && (newIsRecField == false || x == this.X + this.Width - 1)) //Rechteck endet
                    {
                        int minus = 1;
                        if (x == this.X + this.Width - 1) minus = 0;
                        this.above.Add(new SimpleRectangle(startX, x - minus));
                    }
                    isRecField = newIsRecField;
                }
            }

            public bool IsNeibohr(Rectangle rec)
            {
                return this.X == rec.X && this.Width == rec.Width && (this.Y + this.Height == rec.Y || rec.Y + rec.Height == this.Y);
            }

            private float TexX(int x, int y, int z, int imageWidth, int imageHeight, int zDepth, string plane)
            {
                return TexXY(x, y, z, imageWidth, imageHeight, zDepth, plane[0], plane[1]);
            }

            private float TexY(int x, int y, int z, int imageWidth, int imageHeight, int zDepth, string ebene)
            {
                return TexXY(x, y, z, imageWidth, imageHeight, zDepth, ebene[2], ebene[3]);
            }

            private float TexXY(int x, int y, int z, int imageWidth, int imageHeight, int zDepth, char v, char xyz)
            {
                float f = 1, a = 0;
                if (v == '-') { f = -1; a = 1; }
                if (xyz == 'X') return (float)(imageWidth - x + 0.5f * f) / (float)imageWidth * f + a;
                if (xyz == 'Y') return (float)(imageHeight - y + 1.5f * f) / (float)imageHeight * f + a;
                if (xyz == 'Z') return (float)z / (float)zDepth * f + a;
                throw new FormatException("Parameter ebene ist falsch" + xyz);
            }

            private List<MyTriangle> GetQuad(int x1, int y1, int z1, int x2, int y2, int z2, int x3, int y3, int z3, int x4, int y4, int z4, int bildWidth, int bildHeight, int zDepth, string ebene)
            {
                return new List<MyTriangle>()
                {
                    new MyTriangle(new Vertex(x1, y1, z1, TexX(x1, y1, z1, bildWidth, bildHeight, zDepth, ebene), TexY(x1, y1, z1, bildWidth, bildHeight, zDepth, ebene)), 
                                   new Vertex(x2, y2, z2, TexX(x2, y2, z2, bildWidth, bildHeight, zDepth, ebene), TexY(x2, y2, z2, bildWidth, bildHeight, zDepth, ebene)),
                                   new Vertex(x3, y3, z3, TexX(x3, y3, z3, bildWidth, bildHeight, zDepth, ebene), TexY(x3, y3, z3, bildWidth, bildHeight, zDepth, ebene))),
                    new MyTriangle(new Vertex(x3, y3, z3, TexX(x3, y3, z3, bildWidth, bildHeight, zDepth, ebene), TexY(x3, y3, z3, bildWidth, bildHeight, zDepth, ebene)), 
                                   new Vertex(x4, y4, z4, TexX(x4, y4, z4, bildWidth, bildHeight, zDepth, ebene), TexY(x4, y4, z4, bildWidth, bildHeight, zDepth, ebene)),
                                   new Vertex(x1, y1, z1, TexX(x1, y1, z1, bildWidth, bildHeight, zDepth, ebene), TexY(x1, y1, z1, bildWidth, bildHeight, zDepth, ebene))),
                };
            }

            public List<MyTriangle> GetTriangles(int imageWidth, int imageHeight, int zDepth)
            {
                List<MyTriangle> list = new List<MyTriangle>();

                list.AddRange(GetQuad(X, Y, 0, X, Y + Height, 0, X + Width, Y + Height, 0, X + Width, Y, 0, imageWidth, imageHeight, zDepth, "-X+Y"));                             // Vorne
                list.AddRange(GetQuad(X + Width, Y, zDepth, X + Width, Y + Height, zDepth, X, Y + Height, zDepth, X, Y, zDepth, imageWidth, imageHeight, zDepth, "-X+Y"));         // Hinten
                list.AddRange(GetQuad(X + Width, Y, 0, X + Width, Y + Height, 0, X + Width, Y + Height, zDepth, X + Width, Y, zDepth, imageWidth, imageHeight, zDepth, "-Z+Y"));   // Rechts
                list.AddRange(GetQuad(X, Y, zDepth, X, Y + Height, zDepth, X, Y + Height, 0, X, Y, 0, imageWidth, imageHeight, zDepth, "-Z+Y"));                                   // Links

                foreach (var r in this.atTop)
                {
                    list.AddRange(GetQuad(r.StartX, Y, zDepth, r.StartX, Y, 0, r.EndX + 1, Y, 0, r.EndX + 1, Y, zDepth, imageWidth, imageHeight, zDepth, "+X+Z"));                            // Oben
                }
                foreach (var r in this.above)
                {
                    list.AddRange(GetQuad(r.StartX, Y + Height, 0, r.StartX, Y + Height, zDepth, r.EndX + 1, Y + Height, zDepth, r.EndX + 1, Y + Height, 0, imageWidth, imageHeight, zDepth, "-X+Z")); // Unten                
                }

                return list;
            }
        }

        class RectangleList
        {
            private List<Rectangle> rectangles = new List<Rectangle>();

            public void AddRectangle(Rectangle rec)
            {
                this.rectangles.Add(rec);
            }

            public void GenerateSurfaceAtTopAndBelow()
            {
                foreach (var rec in this.rectangles)
                {
                    rec.GenerateSurfaceAtTopAndBelow(this);
                }
            }

            public bool IsRecField(int x, int y)
            {
                foreach (var rec in this.rectangles)
                {
                    if (x >= rec.X && x < rec.X + rec.Width && y >= rec.Y && y < rec.Y + rec.Height) return true;
                }
                return false;
            }

            public void MergeNeighbors()
            {
                while (this.MergeNeighbors1()) { }
            }

            private bool MergeNeighbors1()
            {
                List<Rectangle> newList = new List<Rectangle>();

                this.rectangles.ForEach(x => x.UsedForMerge = false); //Nur wenn Flag true ist, dann wurde noch kein Merge für das Rechteck gemacht

                bool someMergeWasDone = false;

                //Schritt 1: Fisch erstmal die Nachbarn herraus
                for (int i = 0; i < rectangles.Count - 1; i++)
                {
                    for (int j = i + 1; j < rectangles.Count; j++)
                    {
                        if (!this.rectangles[i].UsedForMerge && !this.rectangles[j].UsedForMerge && this.rectangles[i].IsNeibohr(this.rectangles[j]))
                        {
                            this.rectangles[i].UsedForMerge = true;
                            this.rectangles[j].UsedForMerge = true;
                            newList.Add(Merge(this.rectangles[i], this.rectangles[j]));
                            someMergeWasDone = true;
                            break;
                        }
                    }
                }

                //Schritt 2: Nehme den Rest, der nicht gemergt werden konnte
                for (int i = 0; i < rectangles.Count; i++)
                {
                    if (this.rectangles[i].UsedForMerge == false)
                    {
                        newList.Add(this.rectangles[i]);
                    }
                }

                this.rectangles = newList;

                return someMergeWasDone;
            }

            private Rectangle Merge(Rectangle r1, Rectangle r2)
            {
                return new Rectangle(r1.X, Math.Min(r1.Y, r2.Y), r1.Width, r1.Height + r2.Height);
            }

            public List<MyTriangle> GetTriangles(int imageWidth, int imageHeight, int zDepth)
            {
                List<MyTriangle> liste = new List<MyTriangle>();

                foreach (var rec in this.rectangles)
                {
                    liste.AddRange(rec.GetTriangles(imageWidth, imageHeight, zDepth));
                }

                return liste;
            }

            //Testausgabe um zu sehen, wo auf dem 2D-Bild überall Rechtecke sind
            public Bitmap ToBitmap()
            {
                int minX = this.rectangles.Min(x => x.X);
                int minY = this.rectangles.Min(x => x.Y);
                int maxX = this.rectangles.Max(x => x.X + x.Width);
                int maxY = this.rectangles.Max(x => x.Y + x.Height);

                Brush[] colors = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Yellow, Brushes.Black };

                Bitmap image = new Bitmap(maxX, maxY);
                Graphics grx = Graphics.FromImage(image);
                int colorIndex = 0;
                foreach (var rec in this.rectangles)
                {
                    grx.FillRectangle(colors[colorIndex], rec.X, rec.Y, rec.Width, rec.Height);
                    foreach (var wall in rec.atTop)
                    {
                        grx.FillRectangle(Brushes.Brown, wall.StartX, rec.Y, 1, 1);
                        grx.FillRectangle(Brushes.Violet, wall.EndX, rec.Y, 1, 1);
                    }
                    foreach (var wall in rec.above)
                    {
                        grx.FillRectangle(Brushes.Brown, wall.StartX, rec.Y + rec.Height - 1, 1, 1);
                        grx.FillRectangle(Brushes.Violet, wall.EndX, rec.Y + rec.Height - 1, 1, 1);
                    }
                    colorIndex = (colorIndex + 1) % colors.Length;
                }
                grx.Dispose();

                return image;
            }
        }

        private static RectangleList GetRectanglesFromScanline(Bitmap image)
        {
            Color background = image.GetPixel(0, 0);

            RectangleList liste = new RectangleList();

            bool isOutside = true;
            int xStart = 0;

            for (int y = 0; y < image.Height; y++)
            {
                isOutside = true;
                for (int x = 0; x < image.Width; x++)
                {
                    bool newIsOutside = image.GetPixel(x, y) == background;
                    if (isOutside != newIsOutside || x == image.Width - 1)
                    {
                        if (isOutside == false && (newIsOutside || x == image.Width - 1)) //Ich gehe aus Rechteck raus
                        {
                            liste.AddRectangle(new Rectangle(xStart, image.Height - y, x - xStart, 1));
                        }
                        else //Es beginnt neues Rechteck
                        {
                            xStart = x;
                        }
                        isOutside = newIsOutside;
                    }
                }
            }

            return liste;
        }

        
    }
}
