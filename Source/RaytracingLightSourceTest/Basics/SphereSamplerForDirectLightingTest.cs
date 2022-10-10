using System;
using System.Collections.Generic;
using System.Drawing;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfHistogram;
using RaytracingLightSource;

namespace RaytracingLightSourceTest.Basics
{
    [TestClass]
    public class SphereSamplerForDirectLightingTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;
        private readonly float MaxError = 5;

        [TestMethod]
        public void SamplePointOnSphere_SphereSamplingShirley_MatchWithPdfA()
        {
            ISphereSamplerForDirectLighting sampler = new SphereSamplingShirley(new Vector3D(0, 0, 0), 1);
            RunTestOnSampler(sampler, new Vector3D(0, 0, 1.5f), out int error).Save(WorkingDirectory + "SphereSamplingShirley.bmp");
            Assert.IsTrue(error < MaxError, "Error=" + error);
        }

        [TestMethod]
        public void SamplePointOnSphere_SphereSamplingNonUniform_MatchWithPdfA()
        {
            ISphereSamplerForDirectLighting sampler = new SphereSamplingNonUniform(new Vector3D(0, 0, 0), 1);
            RunTestOnSampler(sampler, new Vector3D(0, 0, 1.5f), out int error).Save(WorkingDirectory + "SphereSamplingNonUniform.bmp");
            Assert.IsTrue(error < MaxError, "Error=" + error);
        }

        [TestMethod]
        public void SamplePointOnSphere_SphereSamplingUniform_MatchWithPdfA()
        {
            ISphereSamplerForDirectLighting sampler = new SphereSamplingUniform(new Vector3D(0, 0, 0), 1);
            RunTestOnSampler(sampler, new Vector3D(0, 0, 1.5f), out int error).Save(WorkingDirectory + "SphereSamplingUniform.bmp");
            Assert.IsTrue(error < MaxError, "Error=" + error);
        }

        private Bitmap RunTestOnSampler(ISphereSamplerForDirectLighting sampler, Vector3D eyePoint, out int error)
        {
            int sampleCount = 100000;
            int histogramChunkCount = 20;

            Vector3D normal = Vector3D.Normalize(eyePoint);

            DirectionHistogram histogram = new DirectionHistogram(4, histogramChunkCount, normal);
            Random rand = new Random(0);

            for (int i = 0; i < sampleCount; i++)
            {
                var p = sampler.SamplePointOnSphere((float)rand.NextDouble(), (float)rand.NextDouble(), eyePoint);
                if (p == null) continue;
                float pdfA = sampler.PdfA(eyePoint, new IntersectionPoint(new Vertex(p, p), null, null, null, p, null, null, null));
                histogram.AddSample(p, pdfA);
            }

            List<FunctionToPlot> functions = new List<FunctionToPlot>
            {
                new FunctionToPlot() { Function = histogram.GetPdfThetaFunctionFromHistogram(), Color = Color.Blue, Text = "Histogram" },
                new FunctionToPlot() { Function = histogram.GetPdfThetaFunctionFromPdfProperty(), Color = Color.Red, Text = "Function-PdfA" }
            };

            FunctionPlotter plotter = new FunctionPlotter(0, Math.PI, new Size(400, 300));
            error = (int)FunctionComparer.GetMaxDifferenceAreaBetweenFunctions(0, Math.PI, histogram.GetPdfThetaFunctionFromHistogram(), histogram.GetPdfThetaFunctionFromPdfProperty());
            return plotter.PlotFunctions(functions, "Error=" + error);
        }
    }
}
