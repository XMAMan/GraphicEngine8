using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RayObjects;
using RaytracingLightSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriangleObjectGeneration;

namespace RaytracingLightSourceTest.Basics
{
    [TestClass]
    public class FlatSurfaceListSamplingTest
    {
        private readonly float maxError = 0.006f;

        [TestMethod]
        public void FlatSurfaceListSamplingNonUniform_GetRandomPointOnSurface_PointPdfAMatchWithHistogramPdfA()
        {
            TriangleObject data = TriangleObjectGenerator.CreateCube(1, 0.5f, 1.5f);
            var cube = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat });
            var flats = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { cube }, true, (source, obj) => { return obj.SurfaceArea < 0.2f; });

            int sampleCount = 10000;
            IRandom rand = new Rand(0);
            FlatSurfaceListSamplingNonUniform sut = new FlatSurfaceListSamplingNonUniform(flats.Cast<IFlatRandomPointCreator>(), new Vector3D(5, 2, 0.5f));
            
            Dictionary<IUniformRandomSurfacePointCreator, HistogramEntry> histogram = new Dictionary<IUniformRandomSurfacePointCreator, HistogramEntry>();
            for (int i=0;i<sampleCount;i++)
            {
                var p = sut.GetRandomPointOnSurface(rand);
                if (histogram.ContainsKey(p.PointSampler) == false) histogram.Add(p.PointSampler, new HistogramEntry());
                histogram[p.PointSampler].PointPdfAs.Add(p.PdfA);
                histogram[p.PointSampler].FunctionPdfAs.Add(sut.PdfA(p.PointSampler as IFlatRandomPointCreator));
            }

            foreach (var e in histogram)
            {
                e.Value.PdfAFromPoint = e.Value.PointPdfAs.Average();
                e.Value.PdfAFromFunction = e.Value.FunctionPdfAs.Average();
                e.Value.PdfAFromHistogram = (float)(e.Value.PointPdfAs.Count / (double)sampleCount / e.Key.SurfaceArea);
                e.Value.Difference = Math.Abs(e.Value.PdfAFromPoint - e.Value.PdfAFromHistogram);
            }

            float error = histogram.Values.Average(x => x.Difference);
            string resultText = error + System.Environment.NewLine + string.Join(System.Environment.NewLine, histogram.Values.OrderBy(x => x.Difference).Select(x => x.PdfAFromPoint + "\t" + x.PdfAFromFunction + "\t" + x.PdfAFromHistogram + "\t" + x.Difference + "(" + x.PointPdfAs.Count + ")"));
            Assert.IsTrue(error < maxError);
        }

        [TestMethod]
        public void FlatSurfaceListSamplingUniform_GetRandomPointOnSurface_PointPdfAMatchWithHistogramPdfA()
        {
            TriangleObject data = TriangleObjectGenerator.CreateCube(1, 0.5f, 1.5f);
            var cube = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat });
            var flats = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { cube }, true, (source, obj) => { return obj.SurfaceArea < 0.2f; });

            int sampleCount = 10000;
            IRandom rand = new Rand(0);
            FlatSurfaceListSamplingUniform sut = new FlatSurfaceListSamplingUniform(flats.Cast<IFlatRandomPointCreator>());
            Dictionary<IUniformRandomSurfacePointCreator, HistogramEntry> histogram = new Dictionary<IUniformRandomSurfacePointCreator, HistogramEntry>();
            for (int i = 0; i < sampleCount; i++)
            {
                var p = sut.GetRandomPointOnSurface(rand);
                if (histogram.ContainsKey(p.PointSampler) == false) histogram.Add(p.PointSampler, new HistogramEntry());
                histogram[p.PointSampler].PointPdfAs.Add(p.PdfA);
            }

            foreach (var e in histogram)
            {
                e.Value.PdfAFromPoint = e.Value.PointPdfAs.Average();
                e.Value.PdfAFromHistogram = (float)(e.Value.PointPdfAs.Count / (double)sampleCount / e.Key.SurfaceArea);
                e.Value.Difference = Math.Abs(e.Value.PdfAFromPoint - e.Value.PdfAFromHistogram);
            }

            float error = histogram.Values.Average(x => x.Difference);
            string resultText = error + System.Environment.NewLine + string.Join(System.Environment.NewLine, histogram.Values.OrderBy(x => x.Difference).Select(x => x.PdfAFromPoint + "\t" + x.PdfAFromFunction + "\t" + x.PdfAFromHistogram + "\t" + x.Difference + "(" + x.PointPdfAs.Count + ")"));
            Assert.IsTrue(error < maxError);
        }

        class HistogramEntry
        {
            public List<float> PointPdfAs = new List<float>();
            public List<float> FunctionPdfAs = new List<float>();
            public float PdfAFromPoint = float.NaN; //SurfacePoint.PdfA
            public float PdfAFromFunction = float.NaN; //FlatSurfaceListSamplingNonUniform.PdfA
            public float PdfAFromHistogram = float.NaN;
            public float Difference;
        }
    }
}
