using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using ParticipatingMedia.Media;
using PdfHistogram;
using RayObjects;
using RayObjects.RayObjects;
using RaytracingLightSource.Basics;
using RaytracingLightSource.RayLightSource.Importance;

namespace RaytracingLightSourceTest.Basics
{
    [TestClass]
    public class ImportanceUVMapSamplerTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void Triangle_HistogramTest()
        {
            Size size = new Size(100, 100);
            IUVMapable triangle = new RayTriangle(new Triangle(new Vector3D(0, 0, 0), new Vector3D(1 * size.Width, 0, 0), new Vector3D(0.5f * size.Width, 1 * size.Height, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));
            var histogram = CreateHistogram(new List<IUVMapable>() { triangle }, size);
            histogram.GetResultImage().Save(WorkingDirectory + "TriangleImportanceUVMap.bmp");
        }

        [TestMethod]
        public void TwoTriangles_HistogramTest()
        {
            Size size = new Size(100, 100);
            IUVMapable triangle1 = new RayTriangle(new Triangle(new Vector3D(0, 0, 0), new Vector3D(size.Width, 0, 0), new Vector3D(size.Width, size.Height, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));
            IUVMapable triangle2 = new RayTriangle(new Triangle(new Vector3D(size.Width, size.Height, 0), new Vector3D(0, size.Height, 0), new Vector3D(0, 0, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));
            var histogram = CreateHistogram(new List<IUVMapable>() { triangle1 , triangle2 }, size);
            histogram.GetResultImage().Save(WorkingDirectory + "TwoTrianglesImportanceUVMap.bmp");
        }

        [TestMethod]
        public void Quad_HistogramTest()
        {
            Size size = new Size(100, 100);
            IUVMapable quad = new RayQuad(new Quad(new Vertex(0, 0, 0), new Vertex(size.Width, 0, 0), new Vertex(size.Width, size.Height, 0), new Vertex(0, size.Height, 0)), new RayDrawingObject(new ObjectPropertys(), null, null));
            var histogram = CreateHistogram(new List<IUVMapable>() { quad }, size);
            histogram.GetResultImage().Save(WorkingDirectory + "QuadImportanceUVMap.bmp");
        }

        class DoNothing { }

        private RectangleHistogram CreateHistogram(List<IUVMapable> uvmaps, Size objectSpaceSize)
        {
            int cellCount = 5; //So viele ImportanceCellen hat eine Zeile/Reihe
            ImportanceUVMapListSampler<DoNothing> sut = new ImportanceUVMapListSampler<DoNothing>(uvmaps, cellCount, cellCount);

            for (int i=0;i<sut.Cells.Length;i++)
            {
                sut.Cells[i].IsEnabled = (i % 2 == 0);
            }

            sut.UpateRussiaRolleteSamplerAfterEnablingDisablingCells();

            int histogramSize = 100; //So viele HistogrammCellen hat eine Zeile/Reihe vom Histogram
            int sampleCount = 100000;
            Random rand = new Random(0);
            RectangleHistogram histogram = new RectangleHistogram(histogramSize, histogramSize, objectSpaceSize);

            for (int i=0;i<sampleCount;i++)
            {
                var sample = sut.SampleSurfacePoint(rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), out _);
                histogram.AddSample(new Vector2D(sample.Position.X, sample.Position.Y), sample.PdfA);

                float functionPdfA = sut.GetPdfA(new IntersectionPoint(new Vertex(sample.Position, sample.Normal), null, null, null, sample.Normal, null, sample.PointSampler as IIntersecableObject, null));
                Assert.IsTrue(Math.Abs(sample.PdfA - functionPdfA) < 0.0001f);
            }

            return histogram;
        }
    }
}
