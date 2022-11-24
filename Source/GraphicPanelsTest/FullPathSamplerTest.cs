using BitmapHelper;
using FullPathGenerator.AnalyseHelper;
using GraphicMinimal;
using GraphicPanels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphicPanelsTest
{
    //Nutzt die Box aus den FullPathGeneratorTest um somit auch Verfahren zu testen, die mit Lighttracing arbeiten
    [TestClass]
    public class FullPathSamplerTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void FullBidirectionalPathTracing()
        {
            DoTest(new TestData() { Mode = Mode3D.FullBidirectionalPathTracing, WithMediaBox = false, SamplingCount = 20000 });            
        }

        [TestMethod]
        public void BidirectionalPathTracing()
        {
            DoTest(new TestData() { Mode = Mode3D.BidirectionalPathTracing, WithMediaBox = false, SamplingCount = 20000 });
        }

        [TestMethod]
        public void MediaFullBidirectionalPathTracing()
        {
            DoTest(new TestData() { Mode = Mode3D.MediaFullBidirectionalPathTracing, WithMediaBox = true, SamplingCount = 20000 });
        }

        [TestMethod]
        public void MultiplexedMetropolisLightTransport_NoMedia()
        {
            DoTest(new TestData() { Mode = Mode3D.MMLT, WithMediaBox = false, SamplingCount = 20000, UseCameraTentFilter = false, UsePathSpaceCompare =true });
        }

        [TestMethod]
        public void MultiplexedMetropolisLightTransport_WithMedia()
        {
            DoTest(new TestData() { Mode = Mode3D.MMLT_WithMedia, WithMediaBox = true, SamplingCount = 10000, UseCameraTentFilter = false, UsePathSpaceCompare = true });
        }

        [TestMethod]
        public void MediaUPBP_SinglePixelCheck()
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = 3, Height = 3 };
            TestScenes.AddTestscene_FullPathSamplerTestScene(graphic, true);
            graphic.Mode = Mode3D.UPBP;
            graphic.GlobalSettings.SamplingCount = 500;
            graphic.GlobalSettings.ThreadCount = 1;
            graphic.GlobalSettings.PhotonCount = 10;
            graphic.GlobalSettings.RecursionDepth = 7;
            graphic.GlobalSettings.BackgroundImage = "#000000";
            var image = graphic.GetRaytracingImageSynchron(graphic.Width, graphic.Height, new ImagePixelRange(graphic.Width / 2, graphic.Height / 2, 1, 1));
            float pixelColor = image.RawImage[0, 0].X;

            float maxContributionError = 10;
            //Equal=517; Tent=574 -> Wenn das Bild 3*3 groß ist
            Assert.IsTrue(Math.Abs(pixelColor - 383.483063f) < maxContributionError, "pixelColor=" + pixelColor + " Expected: 383.483063");// Wenn das Bild 3*3 groß ist
        }

        [TestMethod]
        [Ignore]
        public void ProgressivePhotonmapping_SmallImageCheck()
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = 8, Height = 8 };
            TestScenes.AddTestscene_FullPathSamplerTestScene(graphic, false);
            graphic.Mode = Mode3D.ProgressivePhotonmapping;
            graphic.GlobalSettings.SamplingCount = 100;
            //grafik.GlobalSettings.ThreadCount = 1;
            graphic.GlobalSettings.PhotonCount = 1000;
            graphic.GlobalSettings.RecursionDepth = 7;
            var image = graphic.GetRaytracingImageSynchron(graphic.Width, graphic.Height, new ImagePixelRange(0, 0, graphic.Width, graphic.Height)).RawImage;
            string colorValuesString = string.Join(", ", image.GetAllPixels().Select(pix => pix.X.ToString().Replace(",", ".") + "f"));

            //Bias
            float[] colorValues = image.GetAllPixels().Select(pix => pix.X).ToArray();
            //float[] expectedColors = new float[] { 1395.595f, 659.0582f, 507.4663f, 392.7745f, 345.357f, 2397.413f, 1637.042f, 653.0329f, 526.186f, 503.7565f, 2494.19f, 2042.259f, 723.7379f, 620.739f, 549.4247f, 2395.805f, 1644.14f, 652.1058f, 525.1409f, 505.7125f, 1410.042f, 657.4718f, 508.3405f, 391.701f, 347.117f }; //5*5
            float[] expectedColors = new float[] { 1322.472f, 635.8564f, 571.2836f, 493.639f, 425.3983f, 367.9767f, 314.7753f, 303.5971f, 2216.027f, 1625.106f, 843.148f, 696.6908f, 561.3663f, 463.542f, 446.4767f, 417.3549f, 2342.771f, 2574.882f, 1607.546f, 735.8435f, 552.7795f, 501.511f, 593.796f, 473.8102f, 2412.109f, 2670.977f, 1980.725f, 842.328f, 595.8668f, 583.2389f, 653.8317f, 503.0658f, 2412.543f, 2671.582f, 1967.344f, 845.9264f, 598.8852f, 584.2397f, 652.1523f, 503.8617f, 2342.273f, 2575.8f, 1604.772f, 737.597f, 550.0483f, 501.4027f, 593.4083f, 474.1697f, 2215.587f, 1622.167f, 847.45f, 692.3815f, 561.7902f, 462.4264f, 440.2713f, 415.6837f, 1326.135f, 636.5949f, 573.1226f, 496.0584f, 423.1108f, 370.6363f, 315.1721f, 306.3682f }; //8*8
            float biasError = 0;
            for (int i = 0; i < colorValues.Length; i++) biasError += Math.Abs(colorValues[i] - expectedColors[i]);
            biasError /= colorValues.Length;

            //Noise
            float noise = 0;
            for (int x = 1; x < image.Width - 1; x++)
                for (int y = 1; y < image.Height - 1; y++)
                {
                    noise += Math.Abs(image[x, y].X - image[x - 1, y].Y) + Math.Abs(image[x, y].X - image[x + 1, y].Y) +
                             Math.Abs(image[x, y].X - image[x, y - 1].Y) + Math.Abs(image[x, y].X - image[x, y + 1].Y);
                }
            noise /= colorValues.Length;

            Assert.IsTrue(biasError < 100 && noise < 700, "error=" + biasError + " noise=" + noise);

            //Assert.IsTrue(biasError < 100, "error=" + biasError);
            //Assert.IsTrue(noise < 700, "noise=" + noise);
        }

        class TestData
        {
            public Mode3D Mode;
            public bool WithMediaBox = false;
            public int SamplingCount = 20000;
            public bool UseCameraTentFilter = true;
            public bool UsePathSpaceCompare = false;
        }
        private void DoTest(TestData data)
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = 3, Height = 3 };
            TestScenes.AddTestscene_FullPathSamplerTestScene(graphic, data.WithMediaBox);
            graphic.Mode = data.Mode;
            graphic.GlobalSettings.SamplingCount = data.SamplingCount;
            graphic.GlobalSettings.ThreadCount = 1;
            graphic.GlobalSettings.PhotonCount = 10;
            graphic.GlobalSettings.RecursionDepth = 7;
            graphic.GlobalSettings.BackgroundImage = "#000000";
            if (data.UseCameraTentFilter)
                graphic.GlobalSettings.CameraSamplingMode = PixelSamplingMode.Tent;
            else
                graphic.GlobalSettings.CameraSamplingMode = PixelSamplingMode.Equal;


            PathContributionForEachPathSpace expectedSpace = null;
            if (data.WithMediaBox)
            {
                if (data.UseCameraTentFilter)
                    expectedSpace = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaTent.txt");
                else
                    expectedSpace = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            }                
            else
            {
                if (data.UseCameraTentFilter)
                    expectedSpace = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaTent.txt");
                else
                    expectedSpace = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaEqual.txt");
            }

            float maxContributionError = 10;
            if (data.UsePathSpaceCompare)
            {
                //Auf PathSpace-Ebene vergleichen
                var actualSpace = PathContributionForEachPathSpace.FromString(graphic.GetPathContributionsForSinglePixel(3, 3, null, 1, 1, data.SamplingCount));
                string compare = expectedSpace.CompareWithOther(actualSpace);
                string error = expectedSpace.CompareAllPathsWithOther(actualSpace, maxContributionError);
                Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
            }else
            {
                //Auf Pixel-Color-Ebene vergleichen
                var image = graphic.GetRaytracingImageSynchron(graphic.Width, graphic.Height, new ImagePixelRange(graphic.Width / 2, graphic.Height / 2, 1, 1));
                float pixelColor = image.RawImage[0, 0].X;
                double expected = expectedSpace.SumOverAllPathSpaces().X;
                Assert.IsTrue(Math.Abs(pixelColor - expected) < maxContributionError, "pixelColor=" + pixelColor + " Expected: " + expected);// Wenn das Bild 3*3 groß ist
            }                     
        }
    }
}
