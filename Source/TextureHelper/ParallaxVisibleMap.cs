using GraphicGlobal;
using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TextureHelper
{
    //Transformiert eine Menge von Triangles in den Texturspace in den 2D-0..mapSize-Bereich und trägt überall dort 
    //eine Ebene vom jeweiligen Dreieck ein.
    //Mit dieser Klasse kann ich inverses Texturmapping machen. Bei normalen Texturmapping wird ein 
    //3DObjektspace-Punkt in ein 2D-Texurpunkt umgewandelt.
    //Beim inversen Texturmapping wandle ich ein 2D-Texturpunkt in ein 3D-Wordspacepunkt um
    public class ParallaxVisibleMap
    {
        private Size mapSize;
        private Plane[,] visibleMap;
        private Matrix3x3 visibleTexturMatrix;
        private Matrix3x3 textureMatrix;
        private float textureHeighFaktor;
        private float textureScaleFaktorY;
        private bool[,] visibleBoolMap;

        public bool ObjectHasOnlyTwoTriangles { get; private set; }

        public ParallaxVisibleMap(Size mapSize, Matrix3x3 textureMatrix, float texturHighFaktor, float texturScaleFaktorY)
        {
            this.mapSize = mapSize;
            this.textureMatrix = textureMatrix;
            this.textureHeighFaktor = texturHighFaktor;
            this.textureScaleFaktorY = texturScaleFaktorY;

            this.visibleMap = new Plane[mapSize.Width + 1, mapSize.Height + 1];
            this.visibleBoolMap = new bool[mapSize.Width + 1, mapSize.Height + 1];

            //ParallaxVisibleTest.GetVisibleMap1 = (cameraPoint) => { return GetAsBitmap(cameraPoint); };
            //ParallaxVisibleTest.GetVisibleMap2 = () => { return GetVisibleBoolMap(); };
        }

        #region Funktionen, um zu sehen, ob die VisibleMap richtig erstellt wird

        //Gibt all die Texturpunkte zurück, welche vom worldPoint aus sichtbar sind (Zum Testen)
        public Bitmap GetAsBitmap(Vector3D worldPoint)
        {
            Size size = GetTextureSize();
            Bitmap image = new Bitmap(size.Width, size.Height);

            for (int x = 0; x < size.Width; x++)
                for (int y = 0; y < size.Height; y++)
                {
                    if (this.visibleMap[x, y] != null && this.visibleMap[x, y].IsPointAbovePlane(worldPoint)) image.SetPixel(x, y, Color.Red);
                }

            return image;
        }

        //Zum Testen, um zu sehen, ob die Map gleich aussieht wie die mit GetAsBitmap erzeugte Karte
        public Bitmap GetVisibleBoolMap()
        {
            Bitmap image = new Bitmap(this.visibleBoolMap.GetLength(0), this.visibleBoolMap.GetLength(1));

            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    if (this.visibleBoolMap[x, y]) image.SetPixel(x, y, Color.Red);
                }

            return image;
        }

        //Zum Testen, um zu sehen, ob die Map gleich aussieht wie die mit GetAsBitmap erzeugte Karte
        public void MarkAsVisibleInBoolMap(Vector2D tex)
        {
            Size size = GetTextureSize();

            //Transformiere in den 0..1-Bereich
            Vector2D tex01 = (visibleTexturMatrix * new Vector3D(tex.X, tex.Y, 1)).XY;

            int x = (int)(tex01.X * size.Width + 0.5f);
            int y = (int)(tex01.Y * size.Height + 0.5f);

            this.visibleBoolMap[x, y] = true;
        }
        #endregion

        private Triangle TextureTransformedTriangle(Triangle triangle)
        {
            var t = triangle;
            Vector2D textcoord1 = (this.textureMatrix * new Vector3D(t.V[0].TexcoordU, t.V[0].TexcoordV, 1)).XY;
            Vector2D textcoord2 = (this.textureMatrix * new Vector3D(t.V[1].TexcoordU, t.V[1].TexcoordV, 1)).XY;
            Vector2D textcoord3 = (this.textureMatrix * new Vector3D(t.V[2].TexcoordU, t.V[2].TexcoordV, 1)).XY;

            return new Triangle(
                new Vertex(triangle.V[0].Position, t.V[0].Normal, t.V[0].Tangent, textcoord1.X, textcoord1.Y),
                new Vertex(triangle.V[1].Position, t.V[1].Normal, t.V[1].Tangent, textcoord2.X, textcoord2.Y),
                new Vertex(triangle.V[2].Position, t.V[2].Normal, t.V[2].Tangent, textcoord3.X, textcoord3.Y)
                );
        }

        //triangles = Aus all diesen Dreiecken besteht das zugehörige RayHeight/DrawingObjekt
        public void MarkTrianglesWhichAreVisibleFromTheCamera(List<Triangle> triangles)
        {
            triangles = triangles.Select(TextureTransformedTriangle).ToList();

            this.ObjectHasOnlyTwoTriangles = triangles.Count == 2;

            //Schritt 1: Berechne wie hoch die Textur in Worldspace ist
            float texHeighWorld = GetTextureWorldHeigh(triangles.First());

            //Schritt 2: Erzeuge Matrix, mit der alle Texturkoordinaten des DrawingObjektes in den 0..1-Bereich transformiert werden können
            this.visibleTexturMatrix = GetVisibleTexturMatrix(triangles);

            //Schritt 3: Male alle sichtbaren Dreiecke in die Map
            Size size = GetTextureSize();
            foreach (var triangle in triangles)
            {
                //Schritt 3.1: Verschiebe das Dreieck im Worldspace bis auf den Texturboden
                Vector3D trans = triangle.Normal * texHeighWorld;
                Triangle bottomTriangle = new Triangle(triangle.V[0].Position - trans, triangle.V[1].Position - trans, triangle.V[2].Position - trans);

                //Schritt 3.2: Transformiere Textur-Dreieck in den [0..1]-Bereich
                var tex1 = (visibleTexturMatrix * new Vector3D(triangle.V[0].TextcoordVector, 1)).XY;
                var tex2 = (visibleTexturMatrix * new Vector3D(triangle.V[1].TextcoordVector, 1)).XY;
                var tex3 = (visibleTexturMatrix * new Vector3D(triangle.V[2].TextcoordVector, 1)).XY;

                //Schritt 3.3: Rasterisiere das Dreieck im 0..size-Bereich
                DrawTriangle2D(
                    new Vector2D(tex1.X * size.Width, tex1.Y * size.Height),
                    new Vector2D(tex2.X * size.Width, tex2.Y * size.Height),
                    new Vector2D(tex3.X * size.Width, tex3.Y * size.Height),
                    (texPoint) =>
                    {
                        //Trage in die Map das Pixel ein
                        Point p = new Point((int)(texPoint.X + 0.5f), (int)(texPoint.Y + 0.5f));
                        this.visibleMap[p.X, p.Y] = bottomTriangle.GetTrianglePlane();
                    }
                    );
            }
        }

        //So hoch ist die Textur (Bei Höhe 0) im Worldspace
        private float GetTextureWorldHeigh(Triangle triangle)
        {
            var tex1 = triangle.V[0].TextcoordVector; //UV-Koordinaten ohne TexturMatrix-Transformation
            var tex2 = triangle.V[1].TextcoordVector;
            float texDistance = (tex2 - tex1).Length();
            float worldDistance = (triangle.V[1].Position - triangle.V[0].Position).Length();
            float texHeighWorld = this.textureHeighFaktor * worldDistance / texDistance / this.textureScaleFaktorY;
            return texHeighWorld;
        }

        //Mit dieser Matrix kann ich Texturkoordinaten in den [0..1]-Bereich bringen
        private Matrix3x3 GetVisibleTexturMatrix(List<Triangle> triangles)
        {
            var allTexPoints = triangles.SelectMany(x => x.V.Select(y => new Vector2D(y.TexcoordU, y.TexcoordV))).ToList();

            Vector2D min = new Vector2D(float.MaxValue, float.MaxValue);
            Vector2D max = new Vector2D(float.MinValue, float.MinValue);
            foreach (var p in allTexPoints)
            {
                if (p.X < min.X) min.X = p.X;
                if (p.Y < min.Y) min.Y = p.Y;
                if (p.X > max.X) max.X = p.X;
                if (p.Y > max.Y) max.Y = p.Y;
            }

            Vector2D translate = new Vector2D(-min.X, -min.Y);  //Verschiebe zuerst den Bildausschnitt an die Position (0,0)
            Vector2D scale = new Vector2D(1.0f / (max.X - min.X), 1.0f / (max.Y - min.Y));    //Skaliere das Bild dann klein so das nur das Kästchen 1*1 groß ist

            return Matrix3x3.Translate(translate.X, translate.Y) * Matrix3x3.Scale(scale.X, scale.Y);
        }

        private Size GetTextureSize()
        {
            return new Size(this.mapSize.Width, this.mapSize.Height);
        }

        

        //prüft, ob der worldPoint vom tex-Point aus sichtbar ist
        public bool IsPointVisibleFromTextPoint(Vector3D worldPoint, Vector2D tex)
        {
            Size size = GetTextureSize();

            //Transformiere in den 0..1-Bereich
            var tex01 = (this.visibleTexturMatrix * new Vector3D(tex.X, tex.Y, 1)).XY;

            float bias = 0.01f;
            if (tex01.X < bias || tex01.X > 1 - bias || tex01.Y < bias || tex01.Y > 1 - bias) return false;

            int x = (int)(tex01.X * size.Width + 0.5f);
            int y = (int)(tex01.Y * size.Height + 0.5f);

            var plane = this.visibleMap[x, y];
            if (plane == null) return true;

            return plane.IsPointAbovePlane(worldPoint);
        }

        private void DrawTriangle2D(Vector2D p1, Vector2D p2, Vector2D p3, Action<Vector2D> drawPixel)
        {
            Vector2D[] vx = new Vector2D[] { p1, p2, p3 };

            //Sortieren von klein nach groß mit x[]
            int l1, l2, l3;
            for (int i = 0; i < 3; i++)
                for (int j = i + 1; j < 3; j++)
                    if (vx[i].Xi > vx[j].Xi)
                    {
                        Vector2D tmp = vx[i];
                        vx[i] = vx[j];
                        vx[j] = tmp;
                    }

            l1 = vx[1].Xi - vx[0].Xi + 1;
            l2 = vx[2].Xi - vx[1].Xi + 1;
            l3 = vx[2].Xi - vx[0].Xi + 1;

            if (vx[0].Xi == vx[1].Xi)
            {
                int y1, y2;
                if (vx[1].Yi > vx[0].Yi)
                {
                    y1 = vx[0].Yi;
                    y2 = vx[1].Yi;
                }else
                {
                    y1 = vx[1].Yi;
                    y2 = vx[0].Yi;
                }
                for (int y = y1; y <= y2; y++)
                {
                    drawPixel(new Vector2D(vx[0].Xi, y));
                }
            }

            //von Links nach Rechts das Dreieck langgehen
            for (int x = vx[0].Xi; x <= vx[2].Xi; x++)
            {
                //f geht von 0 bis 1
                float f1 = (x - vx[0].Xi) / (float)l1;
                float f2 = (x - vx[1].Xi) / (float)l2;
                float f3 = (x - vx[0].Xi) / (float)l3;

                Vector2D vy1 = new Vector2D(vx[0] * (1 - f1) + vx[1] * f1);//Interpolierter Vector2D
                Vector2D vy2 = new Vector2D(vx[1] * (1 - f2) + vx[2] * f2);
                Vector2D vy3 = new Vector2D(vx[0] * (1 - f3) + vx[2] * f3);

                Vector2D po1 = null, po2 = null;

                if (x <= vx[1].Xi)
                {
                    if (vy1.Y < vy3.Y)
                    {
                        po1 = vy1;
                        po2 = vy3;
                    }
                    else
                    {
                        po1 = vy3;
                        po2 = vy1;
                    }
                }
                else
                {
                    if (vy2.Y < vy3.Y)
                    {
                        po1 = vy2;
                        po2 = vy3;
                    }
                    else
                    {
                        po1 = vy3;
                        po2 = vy2;
                    }
                }

                for (int y = po1.Yi; y <= po2.Yi; y++)
                {
                    drawPixel(new Vector2D(x, y));
                }
            }
        }
    }
}
