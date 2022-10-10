using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;
using ImageCreator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaytracingColorEstimator;
using RaytracingMethods;
using System.Drawing;

namespace RadiosityTest
{
    //Berechnet die Farbe von ein einzelnen Pixel in der BoxTestScene
    [TestClass]
    public class RadiosityTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod] 
        [Ignore]
        public void CreateExpectedImage() //SampleCount: 1000 = 4,6 Minuten; 10000 = 33 Minuten
        {
            GetImage(new BidirectionalPathTracing(), BoxTestScene.CreateScene(), 10000).Save(WorkingDirectory + "\\ExpectedValues\\RadiosityBox_BPT_Expected.bmp");
        }

        [TestMethod]
        public void CreateSolidAngleImage()
        {
            var data = BoxTestScene.CreateScene();
            var s = data.GlobalObjektPropertys.RadiositySettings;
            s.RadiosityColorMode = RadiosityColorMode.WithColorInterpolation;
            s.UseShadowRaysForVisibleTest = false;
            s.MaxAreaPerPatch = 0.001f; //0.001 = 43 Sekunden; 0.0005f = 3.1 Minuten
            var sut = new Radiosity.Radiosity(Radiosity.Radiosity.Mode.SolidAngle);
            var actual = GetImage(sut, data, 1);
            actual.Save(WorkingDirectory + "RadiosityBox_SolidAngle.bmp");

            var expected = new Bitmap(WorkingDirectory + "\\ExpectedValues\\RadiosityBox_SolidAngle_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, actual));
        }

        [TestMethod]
        public void CreateHemicubeImage()
        {
            var data = BoxTestScene.CreateScene();
            var s = data.GlobalObjektPropertys.RadiositySettings;
            s.RadiosityColorMode = RadiosityColorMode.WithColorInterpolation;
            s.MaxAreaPerPatch = 0.01f; //0.01 = 1.2 Minuten; 0.001 mit Resolution von 20 = 68 Minuten
            s.HemicubeResolution = 20;
            var sut = new Radiosity.Radiosity(Radiosity.Radiosity.Mode.Hemicube);
            var actual = GetImage(sut, data, 1);
            actual.Save(WorkingDirectory + "RadiosityBox_Hemicube.bmp");

            var expected = new Bitmap(WorkingDirectory + "\\ExpectedValues\\RadiosityBox_Hemicube_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, actual));
        }

        private Bitmap GetImage(IPixelEstimator pix, RaytracingFrame3DData data, int sampleCount)
        {
            pix.BuildUp(data);

            IRandom rand = new Rand(0);
            ImageBuffer sum = new ImageBuffer(data.ScreenWidth, data.ScreenHeight, new Vector3D(0, 0, 0));
            for (int x = 0; x < data.ScreenWidth; x++)
                for (int y = 0; y < data.ScreenHeight; y++)
                    for (int i = 0; i < sampleCount; i++)
                    {
                        Vector3D color = pix.GetFullPathSampleResult(x, y, rand).RadianceFromRequestetPixel;
                        if (color != null) sum[x, y] += color;
                    }

            Bitmap image = Tonemapping.GetImage(sum.GetColorScaledImage(1.0f / sampleCount), TonemappingMethod.None);
            return image;
        }
    }
}
