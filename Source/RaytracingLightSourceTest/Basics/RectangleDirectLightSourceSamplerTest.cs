using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfHistogram;
using RayObjects;
using RayObjects.RayObjects;
using RaytracingLightSource.Basics;
using RaytracingLightSource.Basics.LightDirectionSampler;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaytracingLightSourceTest.Basics
{
    [TestClass]
    public class RectangleDirectLightSourceSamplerTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void SampleToLightDirection_HistogramTest()
        {
            Size size = new Size(100, 100);
            RayTriangle triangle1 = new RayTriangle(new Triangle(new Vector3D(0, 0, 0), new Vector3D(size.Width, 0, 0), new Vector3D(size.Width, size.Height, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));
            RayTriangle triangle2 = new RayTriangle(new Triangle(new Vector3D(size.Width, size.Height, 0), new Vector3D(0, size.Height, 0), new Vector3D(0, 0, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));
            float spotCutoff = 10; //1 Grad
            Vector3D spotDirection = Vector3D.Normalize(new Vector3D(10, 0, 1)); //triangle1.Normal;
            RectangleDirectLightSourceSampler sut = new RectangleDirectLightSourceSampler(new List<IFlatRandomPointCreator>() { triangle1, triangle2 }, 0.7f, new UniformOverThetaRangeLightDirectionSampler(-spotDirection, 0, spotCutoff * Math.PI / 180));
            Vector3D eyePoint = new Vector3D(50, 50, 0) + spotDirection * 5f;

            int histogramSize = 100; //So viele HistogrammCellen hat eine Zeile/Reihe vom Histogram
            int sampleCount = 100000;
            IRandom rand = new Rand(0);
            RectangleHistogram histogram = new RectangleHistogram(histogramSize, histogramSize, size);

            for (int i = 0; i < sampleCount; i++)
            {
                var sample = sut.SampleToLightDirection(eyePoint, rand);
                Vector3D pointOnLight = GetIntersectionPoint(triangle1, triangle2, new Ray(eyePoint, sample.DirectionToLightPoint));
                if (pointOnLight  == null) continue;
                histogram.AddSample(new Vector2D(pointOnLight.X, pointOnLight.Y), sample.PdfA);

                float functionPdfA = sut.GetDirectLightingPdfA(eyePoint, pointOnLight);
                Assert.IsTrue(Math.Abs(sample.PdfA - functionPdfA) < 0.01f);
            }

            histogram.GetResultImage().Save(WorkingDirectory + "RectangleDirectLightSourceSamplerTest.bmp");
        }

        private Vector3D GetIntersectionPoint(RayTriangle t1, RayTriangle t2, Ray ray)
        {
            var p1 = t1.GetSimpleIntersectionPoint(ray, 0);
            if (p1 != null) return p1.Position;

            var p2 = t2.GetSimpleIntersectionPoint(ray, 0);
            if (p2 != null) return p2.Position;

            return null;
        }
    }
}
