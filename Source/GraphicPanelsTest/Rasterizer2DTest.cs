using BitmapHelper;
using GraphicPanels;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Graphic2DTest;
using System;
using GraphicGlobal.Rasterizer2DFunctions;

namespace GraphicPanelsTest
{
    [TestClass]
    public class Rasterizer2DTest
    {
        private static readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;
        public static string DataDirectory = UnitTestHelper.FilePaths.DataDirectory;

        private readonly List<Mode2D> modesToTest = new List<Mode2D>()
            {
                Mode2D.OpenGL_Version_1_0,
                Mode2D.OpenGL_Version_3_0,
                Mode2D.Direct3D_11,
                Mode2D.CPU,
            };


        [TestMethod]
        public void Rasterizer2D()
        {
            Bitmap result = BitmapHelp.TransformBitmapListToRow(modesToTest.Select(x => CreateImage(x, 477, 359)).ToList());

            result.Save(WorkingDirectory + "Rasterizer2D.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\Rasterizer2D_Expected.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void CircleArcTest()
        {
            //Bitmap result = CreateCircleArcImage(Mode2D.CPU, 800);
            Bitmap result = BitmapHelp.TransformBitmapListToRow(modesToTest.Select(x => CreateCircleArcImage(x, 800)).ToList());

            result.Save(WorkingDirectory + "CircleArcs.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\CircleArcs_Expected.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void FillCircleArcTest()
        {
            //Bitmap result = CreateFillCircleArcImage(Mode2D.CPU, 800);
            Bitmap result = BitmapHelp.TransformBitmapListToRow(modesToTest.Select(x => CreateFillCircleArcImage(x, 800)).ToList());

            result.Save(WorkingDirectory + "FillCircleArcs.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\FillCircleArcs_Expected.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        //Hiermit erkläre ich wie ich die CircleArc-Funktion erstellt habe
        [TestMethod]
        public void CircleArcTutorial()
        {
            List<Bitmap> images = new List<Bitmap>();

            GraphicPanel2D graphic = new GraphicPanel2D() { Width = 800, Height = 800, Mode = Mode2D.CPU };

            graphic.ClearScreen(Color.White);
            CircleArcDrawer.DrawCircleArc1(new Vector2D(400, 400), 300, 350, 10, (p) => graphic.DrawPixel(p, Color.Red, 5));
            images.Add(graphic.GetScreenShoot());

            graphic.ClearScreen(Color.White);
            CircleArcDrawer.DrawCircleArc2(new Vector2D(400, 400), 300, (p, c) => graphic.DrawPixel(p, c, 5));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(0, 0), new Vector2D(800, 800));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(0, 800), new Vector2D(800, 0));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(0, 400), new Vector2D(800, 400));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(400, 0), new Vector2D(400, 800));
            images.Add(graphic.GetScreenShoot());

            graphic.ClearScreen(Color.White);
            CircleArcDrawer.DrawCircleArc3(new Vector2D(400, 400), 300, (p, c) => graphic.DrawPixel(p, c, 5));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(0, 0), new Vector2D(800, 800));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(0, 800), new Vector2D(800, 0));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(0, 400), new Vector2D(800, 400));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(400, 0), new Vector2D(400, 800));
            images.Add(graphic.GetScreenShoot());

            graphic.ClearScreen(Color.White);
            CircleArcDrawer.DrawCircleArc4(new Vector2D(400, 400), 300 + 10 * 1, 10, 40, (p) => graphic.DrawPixel(p, Color.Red, 5));
            CircleArcDrawer.DrawCircleArc4(new Vector2D(400, 400), 300 + 10 * 2, 10, 190, (p) => graphic.DrawPixel(p, Color.Green, 5));
            CircleArcDrawer.DrawCircleArc4(new Vector2D(400, 400), 300 + 10 * 3, 270, 10, (p) => graphic.DrawPixel(p, Color.Blue, 5)); //Geht nicht da (i > startI && i < endI) nicht true ist, da endI==0 ist und startI>0
            CircleArcDrawer.DrawCircleArc4(new Vector2D(400, 400), 300 + 10 * 4, 40, 10, (p) => graphic.DrawPixel(p, Color.Orange, 5));//Geht nicht
            CircleArcDrawer.DrawCircleArc4(new Vector2D(400, 400), 300 + 10 * 5, 280, 360, (p) => graphic.DrawPixel(p, Color.Magenta, 5));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(0, 0), new Vector2D(800, 800));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(0, 800), new Vector2D(800, 0));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(0, 400), new Vector2D(800, 400));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(400, 0), new Vector2D(400, 800));
            images.Add(graphic.GetScreenShoot());

            graphic.ClearScreen(Color.White);
            CircleArcDrawer.DrawCircleArc(new Vector2D(400, 400), 300 + 10 * 1, 10, 40, false, (p) => graphic.DrawPixel(p, Color.Red, 5));
            CircleArcDrawer.DrawCircleArc(new Vector2D(400, 400), 300 + 10 * 2, 10, 190, false, (p) => graphic.DrawPixel(p, Color.Green, 5));
            CircleArcDrawer.DrawCircleArc(new Vector2D(400, 400), 300 + 10 * 3, 270, 10, false, (p) => graphic.DrawPixel(p, Color.Blue, 5));
            CircleArcDrawer.DrawCircleArc(new Vector2D(400, 400), 300 + 10 * 4, 40, 10, false, (p) => graphic.DrawPixel(p, Color.Orange, 5));
            CircleArcDrawer.DrawCircleArc(new Vector2D(400, 400), 300 + 10 * 5, 280, 360, false, (p) => graphic.DrawPixel(p, Color.Magenta, 5));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(0, 0), new Vector2D(800, 800));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(0, 800), new Vector2D(800, 0));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(0, 400), new Vector2D(800, 400));
            graphic.DrawLine(new Pen(Color.Black, 5), new Vector2D(400, 0), new Vector2D(400, 800));
            images.Add(graphic.GetScreenShoot());

            BitmapHelp.TransformBitmapListToRow(images).Save(WorkingDirectory + "CircleArcsTutorial.bmp");
        }

        private Bitmap CreateImage(Mode2D mode, int width, int height)
        {
            GraphicPanel2D graphic = new GraphicPanel2D() { Width = width, Height = height, Mode = mode };

            float yAngle = 10;
            int spriteNr = 180;
            var marioTexture = HelperFor2D.CreateMarioTexture(graphic, DataDirectory + "nes_super_mario_bros.png", yAngle);
            var voronoiCellPoints = GraphicPanel2D.GetRandomPointList(10, marioTexture.Image.Width, marioTexture.Image.Height);
            var voronioPolygons = GraphicPanel2D.GetVoronoiPolygons(marioTexture.Image.Size, voronoiCellPoints);
            voronioPolygons = voronioPolygons.Select(x => HelperFor2D.TransformPolygon(x, new Vector2D(340, 30))).ToList(); //Verschiebe an Position

            HelperFor2D.Draw2D(graphic, DataDirectory, spriteNr, voronioPolygons, voronoiCellPoints, marioTexture, true);
            Bitmap img = graphic.GetScreenShoot();
            graphic.Dispose();
            return img;
        }

        private Bitmap CreateCircleArcImage(Mode2D mode, int size)
        {
            int radius = size / 2;
            Vector2D center = new Vector2D(radius, radius);

            GraphicPanel2D graphic = new GraphicPanel2D() { Width = size, Height = size, Mode = mode };
            graphic.ClearScreen(Color.White);

            Random rand = new Random(0);
            for (int r = radius; r > 5; r -= 5)
            {
                float startAngle = (float)rand.Next(0, 360);
                float endAngle = 0;
                if (r % 2 == 0)
                    endAngle = (float)rand.Next(0, 360);
                else
                    endAngle = Math.Min(360, startAngle + (float)rand.Next(0, 50));

                graphic.DrawCircleArc(new Pen(Color.Red, 2), center, r, startAngle, endAngle, false);


            }

            Bitmap img = graphic.GetScreenShoot();
            graphic.Dispose();
            return img;
        }

        private Bitmap CreateFillCircleArcImage(Mode2D mode, int size)
        {
            int count = 10; //Anzahl pro Zeile
            int fieldSize = size / count;

            GraphicPanel2D graphic = new GraphicPanel2D() { Width = size, Height = size, Mode = mode };
            graphic.ClearScreen(Color.White);

            int c = 0;

            Random rand = new Random(0);
            for (int x=0;x<count;x++)
            {
                for (int y=0;y<count;y++)
                {
                    Vector2D center = new Vector2D(fieldSize * x + fieldSize / 2, fieldSize * y + fieldSize / 2);
                    float radius = fieldSize * 0.4f;
                    float startAngle = (float)rand.Next(0, 360);
                    float endAngle = (float)rand.Next(0, 360);

                    graphic.DrawFillCircleArc(Color.Red, center, (int)radius, startAngle, endAngle);

                    graphic.DrawCircleArc(new Pen(Color.Blue, 1), center, (int)radius, startAngle, endAngle, true);
                }
            }

            Bitmap img = graphic.GetScreenShoot();
            graphic.Dispose();
            return img;
        }
    }
}
