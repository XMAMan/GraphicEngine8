using BitmapHelper;
using GraphicMinimal;
using GraphicPanels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;

namespace GraphicPanelsTest
{
    //Untersucht das Wolkenkantenproblem -> Es gab zwei Fehler welche ich hiermit gefunden habe:
    //1. ThinMediaTracer hat ParticipatingMediaLongRayOneSegmentWithDistanceSampling anstatt ParticipatingMediaLongRayManySegmentsWithDistanceSampling verwendet
    //2. DirectLightingOnEdge hat für die PfadPdfA PdfHelper.PdfWToPdfAOrV(eyePoint.BrdfSampleEventOnThisPoint.PdfW, eyePoint, pointOnT) anstatt eyePoint.BrdfSampleEventOnThisPoint.PdfW verwendet 
    [TestClass]
    public class CloudEdgeTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        //Farbdifferenz der unteren Wolkenkante von der rechten Wolke (Sollte gegen 0 gehen)
        [TestMethod]
        public void ShowEdgeDiffernceFromRightCloud()
        {
            var line0 = GetPixelLine(Mode3D.MediaBidirectionalPathTracing);
            var line1 = GetPixelLine(Mode3D.ThinMediaMultipleScattering);

            Vector3D[] diff = new Vector3D[line0.Length];
            for (int i = 0; i < diff.Length; i++) diff[i] = (line0[i] - line1[i]).Abs();
            float error = diff.Max(x => x.Max());

            BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>()
            {
                BitmapHelp.GetBitmapText("Error " + error, 10, Color.Black, Color.White),
                GetPlotterImage(diff, new Vector3D(0,0,0)),                
            }).Save(WorkingDirectory + "Cloud-SingleLineDiff.bmp");

            Assert.IsTrue(error < 0.099f, "Error=" + error);
        }

        private Bitmap GetPlotterImage(Vector3D[] line, Vector3D rangeRgb)
        {
            return BitmapHelper.BitmapHelp.TransformBitmapListToRow(new List<Bitmap>()
            {
                PdfHistogram.FunctionPlotter.PlotFloatArray(line.Select(x => x.X).ToArray(), rangeRgb.X, "Red"),
                PdfHistogram.FunctionPlotter.PlotFloatArray(line.Select(x => x.Y).ToArray(), rangeRgb.Y, "Green"),
                PdfHistogram.FunctionPlotter.PlotFloatArray(line.Select(x => x.Z).ToArray(), rangeRgb.Z, "Blue"),
            });
        }

        private Vector3D[] GetPixelLine(Mode3D mode)
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = 420, Height = 328 };
            TestScenes.AddTestScene18_CloudsForTestImage(graphic);
            graphic.Mode = mode;
            graphic.GlobalSettings.SamplingCount = mode == Mode3D.ThinMediaSingleScatteringBiased ? 1 : 1000;

            //var image = graphic.GetRaytracingImageSynchron(graphic.Width, graphic.Height, new ImagePixelRange(351, 10, 1, 9)); //Rechte Wolke Kante oben
            var image = graphic.GetRaytracingImageSynchron(graphic.Width, graphic.Height, new ImagePixelRange(289, 212, 1, 8)); //Rechte Wolke Kante unten

            return image.RawImage.GetColum(0);
        }

        //Pixel1/2 = Obere Kante; Pixel 3/4 = Untere Kante
        private static Point[] pixels = new Point[]
            {
                new Point(351, 10),  //Pixel 1: Himmel über der rechten Wolke
                new Point(351, 19),  //Pixel 2: Rechte Wolke oben
                new Point(289, 212), //Pixel 3: Rechte Wolke unten 
                new Point(289, 218)  //Pixel 4: Himmel unter der rechten Wolke
            };
        private static Vector3D[] expectedSky = new Vector3D[] { new Vector3D(93f, 143f, 193f), new Vector3D(94f, 144f, 195f), new Vector3D(137f, 207f, 256f), new Vector3D(142f, 213f, 259f) };
        private static Vector3D[] expectedHomogen = new Vector3D[] { new Vector3D(66f, 68f, 66f), new Vector3D(78f, 119f, 147f), new Vector3D(122f, 146f, 150f), new Vector3D(120f, 125f, 120f) };

        //Erstellt für 4 Pixel den Erwartungswert. Die Pixel liegen so, dass zwei an der oberen Wolkenkante liegen und 2 an der unteren Kante
        [TestMethod]
        [Ignore]
        public void CreateExpectedPixelColorsForSky()
        {
            var expected = GetColorFromPixels(Mode3D.MediaBidirectionalPathTracing, 100000, pixels, false);
            string result = "private static Vector3D[] expectedSky = new Vector3D[]{ " + string.Join(", ", expected.Select(x => x.ToCtorString())) + " };";
        }

        [TestMethod]
        [Ignore]
        public void CreateExpectedPixelColorsForHomogen()
        {
            var expected = GetColorFromPixels(Mode3D.MediaBidirectionalPathTracing, 100000, pixels, true);
            string result = "private static Vector3D[] expectedHomogen = new Vector3D[]{ " + string.Join(", ", expected.Select(x => x.ToCtorString())) + " };";
        }

        [TestMethod]
        public void ThinMediaMulti_SkyPixelColors_MatchWithExpected()
        {
            var actual = GetColorFromPixels(Mode3D.ThinMediaMultipleScattering, 10000, pixels, false);
            float error = Vector3D.Diff(expectedSky, actual).Max(x => x.Max());
            Assert.IsTrue(error < 5, "Error=" +error);
        }

        [TestMethod]
        public void ThinMediaMulti_HomogenPixelColors_MatchWithExpected()
        {
            var actual = GetColorFromPixels(Mode3D.ThinMediaMultipleScattering, 10000, pixels, true);
            float error = Vector3D.Diff(expectedHomogen, actual).Max(x => x.Max());
            Assert.IsTrue(error < 5, "Error=" + error);
        }

        private Vector3D[] GetColorFromPixels(Mode3D mode, int sampleCount, Point[] pixels, bool useHomogenMedia)
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = 420, Height = 328 };
            TestScenes.AddTestScene18_CloudsForTestImage(graphic);
            graphic.Mode = mode;
            //graphic.GlobalSettings.RecursionDepth = 3;

            if (useHomogenMedia)
            {
                var sky = graphic.GetObjectById(2);
                sky.MediaDescription = new DescriptionForHomogeneousMedia()
                {
                    ScatteringCoeffizent = new Vector3D(1, 1, 1) * 0.0000005f * 0.9f,
                    AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 0.000025f * 0.0005f,
                    AnisotropyCoeffizient = 0.00f
                };
                var clouds = graphic.GetAllObjects().Where(x => x.MediaDescription is DescriptionForCloudMedia).ToList();
                clouds.ForEach(x => x.MediaDescription = new DescriptionForHomogeneousMedia()
                {
                    ScatteringCoeffizent = new Vector3D(0.1f, 0.5f, 1) * 0.0005f * 0.9f,
                    AbsorbationCoeffizent = new Vector3D(0.1f, 0.5f, 1) * 0.025f * 0.0005f,
                    AnisotropyCoeffizient = 0.00f
                });
            }

            List<Vector3D> colors = new List<Vector3D>();
            foreach (var pix in pixels)
            {
                string pathSpace = graphic.GetPathContributionsForSinglePixel(graphic.Width, graphic.Height, null, pix.X, pix.Y, sampleCount);

                Vector3D color = Vector3D.Parse(new Regex(@"PixelColor=(\[.*\])").Match(pathSpace).Groups[1].Value);
                //Vector3D CPL = Vector3D.Parse(new Regex(@"C P L=(\[.*\])").Match(pathSpace).Groups[1].Value);
                //Vector3D CPPL = Vector3D.Parse(new Regex(@"C P P L=(\[.*\])").Match(pathSpace).Groups[1].Value);

                colors.Add(color);
            }

            return colors.ToArray();
        }
    }
}
