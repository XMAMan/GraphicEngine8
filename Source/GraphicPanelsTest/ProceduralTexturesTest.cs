using GraphicPanels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;

namespace GraphicPanelsTest
{
    [TestClass]
    public class ProceduralTexturesTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void ProceduralImage()
        {
            Bitmap result = CreateImage(Mode3D.Raytracer, 420, 328, (grafik) => { TestScenes.AddTestszene4_ProceduralTextures(grafik); });
            result.Save(WorkingDirectory + "ProceduralTextures.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\ProceduralTextures_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        private Bitmap CreateImage(Mode3D modus, int width, int height, Action<GraphicPanel3D> addSzeneMethod)
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = width, Height = height, Mode = modus };
            addSzeneMethod(graphic);
            graphic.GlobalSettings.SamplingCount = 1;
            graphic.Mode = modus;
            var image = graphic.GetSingleImage(graphic.Width, graphic.Height);
            graphic.Dispose();
            return image;
        }
    }
}
