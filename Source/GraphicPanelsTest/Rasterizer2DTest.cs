using BitmapHelper;
using GraphicPanels;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Graphic2DTest;

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
    }
}
