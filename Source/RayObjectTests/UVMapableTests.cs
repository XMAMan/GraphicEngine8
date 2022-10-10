using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphicGlobal;
using GraphicGlobal.MathHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RayObjects;
using RayObjects.RayObjects;

namespace RayObjectTests
{
    //Test 1: Für jedes UV-Map-Kästchen stimmt der zugehörige ObjektSpace-Flächeninhalt mit Objektspace-Polygon überrein (Überprüfe GetSurfaceAreaFromUVRectangle)
    //Test 2: Ich sample Zufallspunkt im UV-Kästchen und schaue, dass er im ObjektSpace-Polygon liegt
    //Test 3: Zufalls-Objektspace-Punkt liegt im UV-Kästchen

    [TestClass]
    public class UVMapableTests
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        #region Test 1
        [TestMethod]
        public void Triangle_GetSurfaceAreaFromUVRectangle() //Test 1: Für jedes UV-Map-Kästchen stimmt der zugehörige ObjektSpace-Flächeninhalt mit Objektspace-Polygon überrein
        {
            int boxCount = 5; //Anzahl der Kästchen in der UV-Map
            int boxPixelSize = 20; //So viele Pixel ist ein Kästchen groß
            Size imageSize = new Size(boxCount * boxPixelSize, boxCount * boxPixelSize);
            IUVMapable uvmap = new RayTriangle(new Triangle(new Vector3D(0, 0, 0), new Vector3D(1 * imageSize.Width, 0, 0), new Vector3D(0.5f * imageSize.Width, 1 * imageSize.Height, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));

            CheckSurfaceAreaFromEachUVBox(uvmap, boxCount, boxPixelSize);
        }

        [TestMethod]
        public void Quad_GetSurfaceAreaFromUVRectangle() //Test 1: Für jedes UV-Map-Kästchen stimmt der zugehörige ObjektSpace-Flächeninhalt mit Objektspace-Polygon überrein
        {
            int boxCount = 5; //Anzahl der Kästchen in der UV-Map
            int boxPixelSize = 20; //So viele Pixel ist ein Kästchen groß
            Size imageSize = new Size(boxCount * boxPixelSize * 2, boxCount * boxPixelSize);
            IUVMapable uvmap = new RayQuad(new Quad(new Vertex(0, 0, 0), new Vertex(imageSize.Width, 0, 0), new Vertex(imageSize.Width, imageSize.Height, 0), new Vertex(0, imageSize.Height, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));

            CheckSurfaceAreaFromEachUVBox(uvmap, boxCount, boxPixelSize);
        }

        private void CheckSurfaceAreaFromEachUVBox(IUVMapable uvmap, int boxCount, int boxPixelSize)
        {
            int imagePixelSize = boxCount * boxPixelSize;

            for (int x = 0; x < boxCount; x++)
                for (int y = 0; y < boxCount; y++)
                {
                    Polygon polygon = GetBoxPolygon(uvmap, x, y, boxPixelSize, imagePixelSize);

                    float expectedArea = polygon.GetSurfaceArea();
                    float actualArea = (float)uvmap.GetSurfaceAreaFromUVRectangle(new RectangleF((x * boxPixelSize) / (float)imagePixelSize, (y * boxPixelSize) / (float)imagePixelSize, boxPixelSize / (float)imagePixelSize, boxPixelSize / (float)imagePixelSize));

                    Assert.IsTrue(Math.Abs(expectedArea - actualArea) < 1);
                }

        }

        [TestMethod]
        public void Triangle_BoxBorder() //Ausgabe der UV-Boxen (Zum besseren Verständniss von Test 1)
        {
            int boxCount = 5; //Anzahl der Kästchen in der UV-Map
            int boxPixelSize = 20; //So viele Pixel ist ein Kästchen groß
            Size imageSize = new Size(boxCount * boxPixelSize, boxCount * boxPixelSize);
            IUVMapable uvmap = new RayTriangle(new Triangle(new Vector3D(0, 0, 0), new Vector3D(1 * imageSize.Width, 0, 0), new Vector3D(0.5f * imageSize.Width, 1 * imageSize.Height, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));

            GetBoxBorderImage(uvmap, boxCount, boxPixelSize, imageSize).Save(WorkingDirectory + "TriangleBoxBorder.bmp");
        }

        [TestMethod] 
        public void Quad_BoxBorder() //Ausgabe der UV-Boxen (Zum besseren Verständniss von Test 1)
        {
            int boxCount = 5; //Anzahl der Kästchen in der UV-Map
            int boxPixelSize = 20; //So viele Pixel ist ein Kästchen groß
            Size imageSize = new Size(boxCount * boxPixelSize * 2, boxCount * boxPixelSize);
            IUVMapable uvmap = new RayQuad(new Quad(new Vertex(0, 0, 0), new Vertex(imageSize.Width, 0, 0), new Vertex(imageSize.Width, imageSize.Height, 0), new Vertex(0, imageSize.Height, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));

            GetBoxBorderImage(uvmap, boxCount, boxPixelSize, imageSize).Save(WorkingDirectory + "QuadBoxBorder.bmp");
        }

        private Bitmap GetBoxBorderImage(IUVMapable uvmap, int boxCount, int boxPixelSize, Size imageSize)
        {
            Bitmap image = new Bitmap(imageSize.Width, imageSize.Height);
            Graphics grx = Graphics.FromImage(image);
            int imagePixelSize = boxCount * boxPixelSize;

            for (int x = 0; x < boxCount; x++)
                for (int y = 0; y < boxCount; y++)
                {
                    Polygon polygon = GetBoxPolygon(uvmap, x, y, boxPixelSize, imagePixelSize);
                    for (int i = 0; i < polygon.Points.Count - 1; i++)
                    {
                        grx.DrawLine(Pens.Red, polygon.Points[i].X, polygon.Points[i].Y, polygon.Points[i + 1].X, polygon.Points[i + 1].Y);
                    }

                    float expectedArea = polygon.GetSurfaceArea();
                    float actualArea = (float)uvmap.GetSurfaceAreaFromUVRectangle(new RectangleF((x * boxPixelSize) / (float)imagePixelSize, (y * boxPixelSize) / (float)imagePixelSize, boxPixelSize / (float)imagePixelSize, boxPixelSize / (float)imagePixelSize));
                }

            grx.Dispose();

            return image;
        }

        private Polygon GetBoxPolygon(IUVMapable uvmap, int x, int y, int boxPixelSize, int imageSize)
        {
            List<Vector3D> polygon = new List<Vector3D>();

            polygon.Add(uvmap.GetSurfacePointFromUAndV((x * boxPixelSize) / (double)imageSize, (y * boxPixelSize) / (double)imageSize).Position);
            polygon.Add(uvmap.GetSurfacePointFromUAndV(((x + 1) * boxPixelSize) / (double)imageSize, (y * boxPixelSize) / (double)imageSize).Position);
            polygon.Add(uvmap.GetSurfacePointFromUAndV(((x + 1) * boxPixelSize) / (double)imageSize, ((y + 1) * boxPixelSize) / (double)imageSize).Position);
            polygon.Add(uvmap.GetSurfacePointFromUAndV((x * boxPixelSize) / (double)imageSize, ((y + 1) * boxPixelSize) / (double)imageSize).Position);

            return new Polygon(polygon.Select(p => new Vector2D(p.X, p.Y)).ToList());
        }
        #endregion

        #region Test 2
        [TestMethod]
        public void Triangle_RandomPointIsInsideBox() //Test 1: Für jedes UV-Map-Kästchen stimmt der zugehörige ObjektSpace-Flächeninhalt mit Objektspace-Polygon überrein
        {
            int boxCount = 5; //Anzahl der Kästchen in der UV-Map
            int boxPixelSize = 20; //So viele Pixel ist ein Kästchen groß
            Size imageSize = new Size(boxCount * boxPixelSize, boxCount * boxPixelSize);
            IUVMapable uvmap = new RayTriangle(new Triangle(new Vector3D(0, 0, 0), new Vector3D(1 * imageSize.Width, 0, 0), new Vector3D(0.5f * imageSize.Width, 1 * imageSize.Height, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));

            RandomPointIsInsidePolygon(uvmap, boxCount, boxPixelSize);
        }

        [TestMethod]
        public void Quad_RandomPointIsInsideBox() //Test 1: Für jedes UV-Map-Kästchen stimmt der zugehörige ObjektSpace-Flächeninhalt mit Objektspace-Polygon überrein
        {
            int boxCount = 5; //Anzahl der Kästchen in der UV-Map
            int boxPixelSize = 20; //So viele Pixel ist ein Kästchen groß
            Size imageSize = new Size(boxCount * boxPixelSize * 2, boxCount * boxPixelSize);
            IUVMapable uvmap = new RayQuad(new Quad(new Vertex(0, 0, 0), new Vertex(imageSize.Width, 0, 0), new Vertex(imageSize.Width, imageSize.Height, 0), new Vertex(0, imageSize.Height, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));

            RandomPointIsInsidePolygon(uvmap, boxCount, boxPixelSize);
        }
        private void RandomPointIsInsidePolygon(IUVMapable uvmap, int boxCount, int boxPixelSize)
        {
            int sampleCountPerBox = 100;

            int imagePixelSize = boxCount * boxPixelSize;
            Random rand = new Random(0);

            for (int x = 0; x < boxCount; x++)
                for (int y = 0; y < boxCount; y++)
                {
                    Polygon polygon = GetBoxPolygon(uvmap, x, y, boxPixelSize, imagePixelSize);

                    for (int i=0;i<sampleCountPerBox;i++)
                    {
                        double u = (x * boxPixelSize + rand.NextDouble() * boxPixelSize) / (double)imagePixelSize;
                        double v = (y * boxPixelSize + rand.NextDouble() * boxPixelSize) / (double)imagePixelSize;

                        //Test 2
                        Vector3D objSpacePoint = uvmap.GetSurfacePointFromUAndV(u, v).Position;
                        bool isPointInsidePolygon = polygon.IsPointInsidePolygon(new Vector2D(objSpacePoint.X, objSpacePoint.Y));
                        Assert.IsTrue(isPointInsidePolygon);

                        //Test 3
                        uvmap.GetUAndVFromSurfacePoint(objSpacePoint, out double u1, out double u2);
                        Assert.IsTrue(Math.Abs(u1 - u) < 0.01);
                        Assert.IsTrue(Math.Abs(u2 - v) < 0.01);
                    }
                }

        }
        #endregion

        //Hilfsmethoden, um die Tests besser zu verstehen
        [TestMethod]
        public void Triangle_UVMap()
        {
            int boxCount = 5; //Anzahl der Kästchen in der UV-Map
            int boxPixelSize = 20; //So viele Pixel ist ein Kästchen groß
            int imageSize = boxCount * boxPixelSize;
            IUVMapable uvmap = new RayTriangle(new Triangle(new Vector3D(0, 0, 0), new Vector3D(1 * imageSize, 0, 0), new Vector3D(0.5f * imageSize, 1 * imageSize, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));

            GetUvMapImage(uvmap, boxCount, boxPixelSize, imageSize).Save(WorkingDirectory + "TriangleUVMap.bmp");
        }

        [TestMethod]
        public void Quad_UVMap()
        {
            int boxCount = 5; //Anzahl der Kästchen in der UV-Map
            int boxPixelSize = 20; //So viele Pixel ist ein Kästchen groß
            int imageSize = boxCount * boxPixelSize;
            IUVMapable uvmap = new RayQuad(new Quad(new Vertex(0, 0, 0), new Vertex(imageSize, 0, 0), new Vertex(imageSize, imageSize, 0), new Vertex(0, imageSize, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));

            GetUvMapImage(uvmap, boxCount, boxPixelSize, imageSize).Save(WorkingDirectory + "QuadUVMap.bmp");
        }

        private Bitmap GetUvMapImage(IUVMapable uvmap, int boxCount, int boxPixelSize, int imageSize)
        {
            Bitmap image = new Bitmap(imageSize * 4, imageSize);

            for (int x = 0; x < boxCount; x++)
                for (int y = 0; y < boxCount; y++)
                {
                    for (int ui = 0; ui < boxPixelSize; ui++)
                        for (int vi = 0; vi < boxPixelSize; vi++)
                        {
                            int pixX = x * boxPixelSize + ui;
                            int pixY = y * boxPixelSize + vi;
                            double u = pixX / (double)imageSize;
                            double v = pixY / (double)imageSize;
                            Vector3D objPos = uvmap.GetSurfacePointFromUAndV(u, v).Position;

                            uvmap.GetUAndVFromSurfacePoint(objPos, out double u1, out double u2);

                            uvmap.GetUAndVFromSurfacePoint(new Vector3D(pixX, pixY, 0), out double u3, out double u4);

                            //Color c = GetColorFromDistance(u, v, u1, u2);
                            Color c = GetColor(u, v);
                            //Color c = GetColor(x, y);

                            image.SetPixel(MathExtensions.Clamp((int)(u * imageSize + 0.5), 0, image.Width), MathExtensions.Clamp((int)(v * imageSize + 0.5), 0, image.Height), c); //UV-Map-Vorgabe
                            image.SetPixel(MathExtensions.Clamp((int)(u1 * imageSize + imageSize + 0.5), 0, image.Width), MathExtensions.Clamp((int)(u2 * imageSize + 0.5), 0, image.Height), GetColor(u1, u2)); //UV-Map-Nach Rückrechnung
                            image.SetPixel(MathExtensions.Clamp((int)(objPos.X + imageSize * 2 + 0.5), 0, image.Width), MathExtensions.Clamp((int)(objPos.Y + 0.5), 0, image.Height), c); //3D-Objekt
                            image.SetPixel(MathExtensions.Clamp((int)(u3 * imageSize + imageSize * 3 + 0.5), 0, image.Width), MathExtensions.Clamp((int)(u4 * imageSize + 0.5), 0, image.Height), GetColor(u3, u4)); //UV-Abfrage von Pixel
                        }
                }

            return image;
        }

        

        //Wenn ich gleichmäßig im UV-Space sampel, bekomme ich dann auch gleichmäßige Objekt-Space-Punkte?
        [TestMethod]
        public void Triangle_RandomPoints()
        {
            int imageSize = 100;
            IUVMapable uvmap = new RayTriangle(new Triangle(new Vector3D(0, 0, 0), new Vector3D(1 * imageSize, 0, 0), new Vector3D(0.5f * imageSize, 1 * imageSize, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));
            GetRandomPointImage(uvmap, imageSize).Save(WorkingDirectory + "TriangleRandom.bmp");
        }

        //Wenn ich gleichmäßig im UV-Space sampel, bekomme ich dann auch gleichmäßige Objekt-Space-Punkte?
        [TestMethod]
        public void Quad_RandomPoints()
        {
            int imageSize = 100;
            IUVMapable uvmap = new RayQuad(new Quad(new Vertex(0,0,0), new Vertex(imageSize,0,0), new Vertex(imageSize, imageSize,0), new Vertex(0, imageSize,0)), new RayDrawingObject(new ObjectPropertys(), null, null));
            GetRandomPointImage(uvmap, imageSize).Save(WorkingDirectory + "QuadRandom.bmp");
        }

        private Bitmap GetRandomPointImage(IUVMapable uvmap, int imageSize)
        {
            int sampleCount = 500;

            Bitmap image = new Bitmap(imageSize * 2, imageSize);

            Random rand = new Random(0);

            for (int i = 0; i < sampleCount; i++)
            {
                var pos = uvmap.GetSurfacePointFromUAndV(rand.NextDouble(), rand.NextDouble()).Position;
                uvmap.GetUAndVFromSurfacePoint(pos, out double u1, out double u2);
                image.SetPixel(MathExtensions.Clamp((int)(pos.X + 0.5), 0, image.Width), MathExtensions.Clamp((int)(pos.Y + 0.5), 0, image.Height), Color.Blue); //3D-Objekt
                image.SetPixel(MathExtensions.Clamp((int)(u1 * imageSize + imageSize + 0.5), 0, image.Width), MathExtensions.Clamp((int)(u2 * imageSize + 0.5), 0, image.Height), Color.Red);
            }

            return image;
        }

        private Color GetColorFromDistance(double u, double v, double u1, double u2)
        {
            double distance = Math.Sqrt((u1 - u) * (u1 - u) + (u2 - v) * (u2 - v));

            if (double.IsNaN(distance)) return Color.Black;
            int colorI = (int)(Math.Min(1, distance) * 255);
            Color c = Color.FromArgb(colorI, colorI, colorI);
            return c;
        }

        private Color GetColor(double u, double v)
        {
            //return Color.FromArgb((int)(255 * u), (int)(255 * v), 0);
            if (double.IsNaN(u)) return Color.Black;
            if (double.IsNaN(v)) return Color.Black;
            if (u < 0) return Color.Black;
            if (v < 0) return Color.Black;
            return GetColor((int)(u * 100) / 20, (int)(v * 100) / 20);
        }

        private Color GetColor(int x, int y)
        {
            Color[] colors = new Color[100];
            Random rand = new Random(0);
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.FromArgb((int)(255 * rand.NextDouble()), (int)(255 * rand.NextDouble()), (int)(255 * rand.NextDouble()));
            }
            return colors[(x * 10 + y) % colors.Length];
            //int b = (x ^ y) & 1;
            //return b == 0 ? Color.Red : Color.Blue;
        }
    }
}
