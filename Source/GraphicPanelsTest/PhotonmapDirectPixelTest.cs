using GraphicMinimal;
using GraphicPanels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;

namespace GraphicPanelsTest
{
    [TestClass]
    public class PhotonmapDirectPixelTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void ShowGodRays()
        {
            Bitmap result = GetImage(1920 / 5, 1016 / 5, TestScenes.AddTestscene11_PillarsOfficeGodRay, PhotonmapDirectPixelSetting.ShowGodRays);
            result.Save(WorkingDirectory + "PhotonmapPixel_GodRays.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\PhotonmapPixel_GodRays_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void NoMediaCaustics()
        {
            Bitmap result = GetImage(420, 328, (graphic)=> 
            {
                TestScenes.AddTestscene5_Cornellbox(graphic);
                graphic.GlobalSettings.PhotonCount = 1000;
            }, PhotonmapDirectPixelSetting.ShowNoMediaCaustics);
            result.Save(WorkingDirectory + "PhotonmapPixel_NoMediaCaustics.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\PhotonmapPixel_NoMediaCaustics_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void ParticlePhotons()
        {
            Bitmap result = GetImage(1217 / 5, 600 / 5, (graphic) =>
            {
                TestScenes.AddTestscene10_MirrorGlassCaustic(graphic);
                graphic.GlobalSettings.PhotonCount = 10000;
            }, PhotonmapDirectPixelSetting.ShowParticlePhotons);
            result.Save(WorkingDirectory + "PhotonmapPixel_ParticlePhotons.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\PhotonmapPixel_ParticlePhotons_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void DirectLightPhotons()
        {
            Bitmap result = GetImage(420, 328, (graphic) =>
            {
                TestScenes.AddTestscene2_NoWindowRoom(graphic);
                graphic.GlobalSettings.PhotonCount = 10000;
            }, PhotonmapDirectPixelSetting.ShowDirectLightPhotons);
            result.Save(WorkingDirectory + "PhotonmapPixel_DirectLightPhotons.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\PhotonmapPixel_DirectLightPhotons_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        private Bitmap GetImage(int width, int height, Action<GraphicPanel3D> addSceneAction, PhotonmapDirectPixelSetting settings)
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = width, Height = height };
            addSceneAction(graphic);
            graphic.Mode = Mode3D.PhotonmapPixel;
            graphic.GlobalSettings.SamplingCount = 1;
            graphic.GlobalSettings.PhotonmapPixelSettings = settings;
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.None;
            Bitmap image = graphic.GetSingleImage(graphic.Width, graphic.Height);
            graphic.Dispose();
            return image;
        }
    }
}
