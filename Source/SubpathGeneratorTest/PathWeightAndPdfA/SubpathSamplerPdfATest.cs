using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfHistogram;
using SubpathGenerator;
using System.IO;

namespace SubpathGeneratorTest
{
    [TestClass]
    public class SubpathSamplerPdfATest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        private int histogramSize = 5;// 10; //Anzahl der Gitterzellen vom 3D-Grid pro Dimmension
        private int sampleCount = 1000000;

        [TestMethod]
        public void SamplePathFromCamera_CalledMultipleTimes_PathPdfAMatchWithHistogram()
        {
            BoundingBox mediaBox = null;

            PdfATestSzene testSzene = new PdfATestSzene(SubpathGenerator.PathSamplingType.NoMedia, mediaBox);

            SubPathHistogram histogram = new SubPathHistogram(testSzene.Quads, mediaBox, histogramSize);

            for (int i = 0; i < sampleCount; i++)
            {
                PathPoint[] points = testSzene.SubpathSampler.SamplePathFromCamera(testSzene.PixX, testSzene.PixY, testSzene.rand).Points;
                histogram.AddPathToHistogram(points, 1); //Füge subPath in Pfad-Histogram ein
            }

            var result = histogram.GetTestResult(400, 300);
            result.Image.Save(WorkingDirectory + "SubpathHistogram_FromEye.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(3) < 20, "Error=" + result.MaxErrorUptoGivenPathLength(3));
        }

        [TestMethod]
        public void SamplePathFromLighsource_CalledMultipleTimes_PathPdfAMatchWithHistogram()
        {
            BoundingBox mediaBox = null;

            PdfATestSzene testSzene = new PdfATestSzene(SubpathGenerator.PathSamplingType.NoMedia, mediaBox);
            testSzene.Quads.Reverse();
            SubPathHistogram histogram = new SubPathHistogram(testSzene.Quads, mediaBox, histogramSize);

            for (int i = 0; i < sampleCount; i++)
            {
                PathPoint[] points = testSzene.SubpathSampler.SamplePathFromLighsource(testSzene.rand).Points;
                histogram.AddPathToHistogram(points, 0); //Füge subPath in Pfad-Histogram ein
            }

            var result = histogram.GetTestResult(400, 300);
            result.Image.Save(WorkingDirectory + "SubpathHistogram_FromLight.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(3) < 20, "Error=" + result.MaxErrorUptoGivenPathLength(3));
        }


        [TestMethod]
        public void SamplePathFromCamera_MediaWithDistanceSampling_PathPdfAMatchWithHistogram()
        {
            BoundingBox mediaBox = new BoundingBox(new Vector3D(-1, 0, -1), new Vector3D(+10, 0.5f, +1)); //MediaBox über der ersten Platte

            PdfATestSzene testSzene = new PdfATestSzene(SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, mediaBox);

            SubPathHistogram histogram = new SubPathHistogram(testSzene.Quads, mediaBox, histogramSize);

            for (int i = 0; i < sampleCount; i++)
            {
                PathPoint[] points = testSzene.SubpathSampler.SamplePathFromCamera(testSzene.PixX, testSzene.PixY, testSzene.rand).Points;
                histogram.AddPathToHistogram(points, 1); //Füge subPath in Pfad-Histogram ein
            }

            var result = histogram.GetTestResult(400, 300);
            result.Image.Save(WorkingDirectory + "SubpathHistogram_FromEyeWithDistanceSampling.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(3) < 20, "Error=" + result.MaxErrorUptoGivenPathLength(3));
        }

        [TestMethod]
        public void SamplePathFromCamera_MediaWithoutDistanceSampling_PathPdfAMatchWithHistogram()
        {
            BoundingBox mediaBox = new BoundingBox(new Vector3D(-1, 0, -1), new Vector3D(+10, 0.5f, +1)); //MediaBox über der ersten Platte

            PdfATestSzene testSzene = new PdfATestSzene(SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling, mediaBox);

            SubPathHistogram histogram = new SubPathHistogram(testSzene.Quads, mediaBox, histogramSize);

            for (int i = 0; i < sampleCount; i++)
            {
                PathPoint[] points = testSzene.SubpathSampler.SamplePathFromCamera(testSzene.PixX, testSzene.PixY, testSzene.rand).Points;
                histogram.AddPathToHistogram(points, 1); //Füge subPath in Pfad-Histogram ein
            }

            var result = histogram.GetTestResult(400, 300);
            result.Image.Save(WorkingDirectory + "SubpathHistogram_FromEyeWithoutDistanceSampling.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(3) < 20, "Error=" + result.MaxErrorUptoGivenPathLength(3));
        }

        [TestMethod]
        public void SamplePathFromLighsource_MediaWithDistanceSampling_PathPdfAMatchWithHistogram()
        {
            BoundingBox mediaBox = new BoundingBox(new Vector3D(-1 + 8, 1.5f, -1), new Vector3D(+1 + 8, 2.0f, +1)); //MediaBox über der letzten Platte

            PdfATestSzene testSzene = new PdfATestSzene(SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, mediaBox);
            testSzene.Quads.Reverse();
            SubPathHistogram histogram = new SubPathHistogram(testSzene.Quads, mediaBox, histogramSize);

            for (int i = 0; i < sampleCount / 10; i++)
            {
                PathPoint[] points = testSzene.SubpathSampler.SamplePathFromLighsource(testSzene.rand).Points;
                histogram.AddPathToHistogram(points, 0); //Füge subPath in Pfad-Histogram ein
            }

            var result = histogram.GetTestResult(400, 300);
            result.Image.Save(WorkingDirectory + "SubpathHistogram_FromLightWithDistanceSampling.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(3) < 20, "Error=" + result.MaxErrorUptoGivenPathLength(3));
        }

        [TestMethod]
        public void SamplePathFromLighsource_MediaWithoutDistanceSampling_PathPdfAMatchWithHistogram()
        {
            BoundingBox mediaBox = new BoundingBox(new Vector3D(-1 + 8, 1.5f, -1), new Vector3D(+1 + 8, 2.0f, +1)); //MediaBox über der letzten Platte

            PdfATestSzene testSzene = new PdfATestSzene(SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling, mediaBox);
            testSzene.Quads.Reverse();
            SubPathHistogram histogram = new SubPathHistogram(testSzene.Quads, mediaBox, histogramSize);

            for (int i = 0; i < sampleCount; i++)
            {
                PathPoint[] points = testSzene.SubpathSampler.SamplePathFromLighsource(testSzene.rand).Points;
                histogram.AddPathToHistogram(points, 0); //Füge subPath in Pfad-Histogram ein
            }

            var result = histogram.GetTestResult(400, 300);
            result.Image.Save(WorkingDirectory + "SubpathHistogram_FromLightWithoutDistanceSampling.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(3) < 20, "Error=" + result.MaxErrorUptoGivenPathLength(3));
        }
    }
}
