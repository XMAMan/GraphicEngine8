using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using GraphicGlobal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaytracingLightSourceTest.Fullpathsampling
{
    //MIS-Kombination von Brdf-, Directlighting- und Lightracing-Sampling
    [TestClass]
    public class LightMISTest
    {
        private readonly int sampleCount = 40000;
        private readonly float maxCDLError = 20;
        private readonly float maxCPLError = 20;

        [TestMethod]
        public void Surface()
        {
            CheckSinglePixelColor(LightsourceType.Surface, 423, 0, false, false, PathSamplingType.NoMedia);
        }

        [TestMethod]
        public void Sphere()
        {
            CheckSinglePixelColor(LightsourceType.Sphere, 46, 0, false, false, PathSamplingType.NoMedia);
        }

        [TestMethod]
        public void SurfaceWithSpot()
        {
            CheckSinglePixelColor(LightsourceType.SurfaceWithSpot, 171, 0, false, false, PathSamplingType.NoMedia);
        }

        [TestMethod]
        public void ImportanceSurface()
        {
            CheckSinglePixelColor(LightsourceType.ImportanceSurface, 426, 0, false, false, PathSamplingType.NoMedia);
        }

        [TestMethod]
        public void ImportanceSurfaceWithSpot()
        {
            CheckSinglePixelColor(LightsourceType.ImportanceSurfaceWithSpot, 190, 0, false, false, PathSamplingType.NoMedia);
        }

        [TestMethod]
        public void DirectionFromInfinity_NoMediaSampling()
        {
            CheckSinglePixelColorOnlyForDirectLightingAndLightTracing(LightsourceType.DirectionFromInfinity, 127, 0, false, false, PathSamplingType.NoMedia);
        }

        [TestMethod]
        public void Environment_NoMediaSampling()
        {
            CheckSinglePixelColor(LightsourceType.Environment, 396, 0, false, false, PathSamplingType.NoMedia);
        }

        [TestMethod]
        public void HdrMap_NoMediaSampling()
        {
            CheckSinglePixelColor(LightsourceType.HdrMap, 149, 0, false, false, PathSamplingType.NoMedia);
        }

        [TestMethod]
        public void Motion()
        {
            CheckSinglePixelColor(LightsourceType.Motion, 423, 0, false, false, PathSamplingType.NoMedia);
        }

        //Alle möglichen 2er-Kombinationen, welche mit Brdf-, Directlighting- und Lightracing-Sampling möglich sind
        private void CheckSinglePixelColor(LightsourceType lightsourceType, float expectedColorCDL, float expectedColorCPL, bool withMediaSphere, bool withGlobalMedia, PathSamplingType pathSamplingType)
        {
            FullPathTestData testData = new FullPathTestData(lightsourceType, 1, withMediaSphere, withGlobalMedia, pathSamplingType);

            var c1 = GetPixelColor(testData, testData.PathtracingSampler, testData.DirectLightingSampler, sampleCount);   //Pathtracing+DirectLighting
            var c2 = GetPixelColor(testData, testData.PathtracingSampler, testData.LightTracingSampler, sampleCount);     //Pathtracing+LightTracing
            var c3 = GetPixelColor(testData, testData.DirectLightingSampler, testData.LightTracingSampler, sampleCount);  //DirectLighting+LightTracing

            string result = PathContributionForEachPathSpace.CompareAll(c1, c2, c3);

            Assert.IsTrue(Math.Abs(c1["C D L"] - expectedColorCDL) < maxCDLError, "Pathtracing+DirectLightingCDL: " + c1["C D L"]);
            Assert.IsTrue(Math.Abs(c1["C P L"] - expectedColorCPL) < maxCPLError, "Pathtracing+DirectLightingCPL: " + c1["C P L"]);

            Assert.IsTrue(Math.Abs(c2["C D L"] - expectedColorCDL) < maxCDLError, "Pathtracing+LightTracingCDL: " + c2["C D L"]);
            Assert.IsTrue(Math.Abs(c2["C P L"] - expectedColorCPL) < maxCPLError, "Pathtracing+LightTracingCPL: " + c2["C P L"]);

            Assert.IsTrue(Math.Abs(c3["C D L"] - expectedColorCDL) < maxCDLError, "DirectLighting+LightTracingCDL: " + c3["C D L"]);
            Assert.IsTrue(Math.Abs(c3["C P L"] - expectedColorCPL) < maxCPLError, "DirectLighting+LightTracingCPL: " + c3["C P L"]);
        }

        //Alle möglichen 2er-Kombinationen, welche mit Directlighting- und Lightracing-Sampling möglich sind
        private void CheckSinglePixelColorOnlyForDirectLightingAndLightTracing(LightsourceType lightsourceType, float expectedColorCDL, float expectedColorCPL, bool withMediaSphere, bool withGlobalMedia, PathSamplingType pathSamplingType)
        {
            FullPathTestData testData = new FullPathTestData(lightsourceType, 1, withMediaSphere, withGlobalMedia, pathSamplingType);

            var c3 = GetPixelColor(testData, testData.DirectLightingSampler, testData.LightTracingSampler, sampleCount);  //DirectLighting+LightTracing

            string result = PathContributionForEachPathSpace.CompareAll(c3);

            Assert.IsTrue(Math.Abs(c3["C D L"] - expectedColorCDL) < maxCDLError, "DirectLighting+LightTracingCDL: " + c3["C D L"]);
            Assert.IsTrue(Math.Abs(c3["C P L"] - expectedColorCPL) < maxCPLError, "DirectLighting+LightTracingCPL: " + c3["C P L"]);
        }

        //2er-Kombination von sampler1 und sampler2
        private PathContributionForEachPathSpace GetPixelColor(FullPathTestData testData, IFullPathSamplingMethod sampler1, IFullPathSamplingMethod sampler2, int sampleCount)
        {
            int pixX = 0, pixY = 0;
            PathContributionForEachPathSpace pathContribution = new PathContributionForEachPathSpace();
            if (sampler1 == null || sampler2 == null) return pathContribution;
            IRandom rand = new Rand(0);

            for (int i = 0; i < sampleCount; i++)
            {
                List<FullPath> paths = new List<FullPath>();
                paths.AddRange(sampler1.SampleFullPaths(testData.EyePathSampler.SamplePathFromCamera(pixX, pixY, rand), testData.LightPathSampler.SamplePathFromLighsource(rand), null, rand));
                paths.AddRange(sampler2.SampleFullPaths(testData.EyePathSampler.SamplePathFromCamera(pixX, pixY, rand), testData.LightPathSampler.SamplePathFromLighsource(rand), null, rand));
                foreach (var path in paths)
                {
                    double pdfSum = sampler1.GetPathPdfAForAGivenPath(path, null) + sampler2.GetPathPdfAForAGivenPath(path, null);
                    path.MisWeight = (float)(path.PathPdfA / pdfSum);
                    path.Radiance = path.PathContribution * path.MisWeight;

                    //Achtung: Diese Lokik klappt nicht, wenn der PixelRange nur ein Pixel groß ist und die PixePosition im Minusbereich ist
                    //Beim Lighttracing mit Media erhält man solche Pfade
                    if (path.PixelPosition == null || ((int)path.PixelPosition.X == pixX && (int)path.PixelPosition.Y == pixY))
                    {
                        pathContribution.AddEntry(path, path.Radiance / sampleCount);
                    }
                }
            }

            return pathContribution;
        }
    }
}
