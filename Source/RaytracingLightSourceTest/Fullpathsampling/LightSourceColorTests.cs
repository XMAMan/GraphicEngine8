using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace RaytracingLightSourceTest.Fullpathsampling
{
    [TestClass]
    public class LightSourceColorTests
    {
        private readonly int sampleCount = 40000;
        private readonly float maxCDLError = 20;
        private readonly float maxCPLError = 20;

        #region NoMedia-Tests
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
        [Ignore] //Diese Lampe ist fehlerhaft implementiert und schafft somit den Test nicht
        public void SphereWithSpot()
        {
            CheckSinglePixelColor(LightsourceType.SphereWithSpot, 46, 0, false, false, PathSamplingType.NoMedia);
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
        public void ImportanceSurface_CheckHowManyLightPathsAreVisible()
        {
            CheckHowManyLightPathsAreVisible(LightsourceType.ImportanceSurface, false, false, PathSamplingType.NoMedia);
        }

        [TestMethod]
        public void ImportanceSurfaceWithSpot()
        {
            CheckSinglePixelColor(LightsourceType.ImportanceSurfaceWithSpot, 190, 0, false, false, PathSamplingType.NoMedia);
        }

        [TestMethod]
        public void ImportanceSurfaceWithSpot_CheckHowManyLightPathsAreVisible()
        {
            CheckHowManyLightPathsAreVisible(LightsourceType.ImportanceSurfaceWithSpot, false, false, PathSamplingType.NoMedia);
        }

        [TestMethod]
        public void DirectionFromInfinity_NoMediaSampling()
        {
            CheckSinglePixelColorOnlyForDirectLightingAndLightTracing(LightsourceType.DirectionFromInfinity, 127, 0, false, false, PathSamplingType.NoMedia);
        }

        [TestMethod]
        public void DirectionFromInfinity_UseMediaSampler()
        {
            CheckSinglePixelColorOnlyForDirectLightingAndLightTracing(LightsourceType.DirectionFromInfinity, 127, 0, false, false, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }

        [TestMethod]
        public void Environment_NoMediaSampling()
        {
            CheckSinglePixelColor(LightsourceType.Environment, 396, 0, false, false, PathSamplingType.NoMedia);
        }

        [TestMethod]
        public void Environment_UseMediaSampler()
        {
            CheckSinglePixelColor(LightsourceType.Environment, 396, 0, false, false, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
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
        #endregion

        #region WithMediaSphere-Tests

        [TestMethod]
        public void Surface_WithMediaSphere()
        {
            CheckSinglePixelColor(LightsourceType.Surface, 355, 36, true, false, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }

        [TestMethod]
        public void Sphere_WithMediaSphere()
        {
            CheckSinglePixelColor(LightsourceType.Sphere, 33, 1, true, false, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }

        [TestMethod]
        [Ignore]
        public void SphereWithSpot_WithMediaSphere()
        {
            CheckSinglePixelColor(LightsourceType.SphereWithSpot, 36, 0, true, false, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }

        [TestMethod]
        public void SurfaceWithSpot_WithMediaSphere()
        {
            CheckSinglePixelColor(LightsourceType.SurfaceWithSpot, 153, 7, true, false, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }

        [TestMethod]
        public void ImportanceSurface_WithMediaSphere()
        {
            CheckSinglePixelColor(LightsourceType.ImportanceSurface, 353, 39, true, false, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }

        [TestMethod]
        public void DirectionFromInfinity_WithMediaSphere()
        {
            CheckSinglePixelColorOnlyForDirectLightingAndLightTracing(LightsourceType.DirectionFromInfinity, 74, 1, true, false, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }

        [TestMethod]
        public void Environment_WithMediaSphere()
        {
            CheckSinglePixelColor(LightsourceType.Environment, 229, 14, true, false, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }
        #endregion

        #region WitGlobalhMedia-Tests

        [TestMethod]
        public void Surface_WithGlobalMedia()
        {
            CheckSinglePixelColor(LightsourceType.Surface, 173, 134, false, true, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }

        [TestMethod]
        public void Sphere_WithGlobalMedia()
        {
            CheckSinglePixelColor(LightsourceType.Sphere, 11, 4, false, true, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }

        [TestMethod]
        [Ignore]
        public void SphereWithSpot_WithGlobalMedia()
        {
            CheckSinglePixelColor(LightsourceType.SphereWithSpot, 36, 0, false, true, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }

        [TestMethod]
        public void SurfaceWithSpot_WithGlobalMedia()
        {
            CheckSinglePixelColor(LightsourceType.SurfaceWithSpot, 82, 31, false, true, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }

        [TestMethod]
        [Ignore] //Das Importancelicht verwendet intern ein ImportanePhotonSender, welcher ohne Media arbeitet. Dadurch können L P C-Pfade nicht erkannt werden, was dazu führt, dass die Lampe weniger Importancecellen aktiviert
        public void ImportanceSurface_WithGlobalMedia()
        {
            CheckSinglePixelColor(LightsourceType.ImportanceSurface, 172, 131, false, true, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }

        [TestMethod]
        public void DirectionFromInfinity_WithGlobalMedia()
        {
            CheckSinglePixelColorOnlyForDirectLightingAndLightTracing(LightsourceType.DirectionFromInfinity, 0, 0, false, true, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling);
        }

        [TestMethod]
        public void Environment_WithGlobalMedia()
        {
            CheckSinglePixelColorOnlyForDirectLightingAndLightTracing(LightsourceType.Environment, 9, 3, false, true, PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling, 10000);
        }
        #endregion

        private void CheckSinglePixelColor(LightsourceType lightsourceType, float expectedColorCDL, float expectedColorCPL, bool withMediaSphere, bool withGlobalMedia, PathSamplingType pathSamplingType)
        {
            FullPathTestData testData = new FullPathTestData(lightsourceType, 1, withMediaSphere, withGlobalMedia, pathSamplingType);

            var c1 = GetPixelColor(testData, testData.PathtracingSampler, sampleCount * 2);   //BrdfSampling
            var c2 = GetPixelColor(testData, testData.DirectLightingSampler, sampleCount);//LightSourceSampling1
            var c3 = GetPixelColor(testData, testData.DirectLightingOnEdgeSampler, sampleCount);//LightSourceSampling2
            var c4 = GetPixelColor(testData, testData.LightTracingSampler, sampleCount * 5);  //Lighttracing

            string result = PathContributionForEachPathSpace.CompareAll(c1, c2, c3, c4);

            Assert.IsTrue(Math.Abs(c1["C D L"] - expectedColorCDL) < maxCDLError, "PathtracingCDL: " + c1["C D L"]);
            Assert.IsTrue(Math.Abs(c1["C P L"] - expectedColorCPL) < maxCPLError, "PathtracingCPL: " + c1["C P L"]);

            Assert.IsTrue(Math.Abs(c2["C D L"] - expectedColorCDL) < maxCDLError, "DirectLightingCDL: " + c2["C D L"]);
            Assert.IsTrue(Math.Abs(c2["C P L"] - expectedColorCPL) < maxCPLError, "DirectLightingCPL: " + c2["C P L"]);

            //Assert.IsTrue(Math.Abs(c3["C D L"] - expectedColorCDL) < maxError, "DirectLightingOnEdgeCDL: " + c3["C D L"]); //Wird von diesen Verfahren nicht geampelt
            Assert.IsTrue(Math.Abs(c3["C P L"] - expectedColorCPL) < maxCPLError, "DirectLightingOnEdgeCPL: " + c3["C P L"]);

            Assert.IsTrue(Math.Abs(c4["C D L"] - expectedColorCDL) < maxCDLError, "LighttracingCDL: " + c4["C D L"]);
            Assert.IsTrue(Math.Abs(c4["C P L"] - expectedColorCPL) < maxCPLError, "LighttracingCPL: " + c4["C P L"]); 
        }

        private void CheckSinglePixelColorOnlyForDirectLightingAndLightTracing(LightsourceType lightsourceType, float expectedColorCDL, float expectedColorCPL, bool withMediaSphere, bool withGlobalMedia, PathSamplingType pathSamplingType, float emission = 1000)
        {
            FullPathTestData testData = new FullPathTestData(lightsourceType, 1, withMediaSphere, withGlobalMedia, pathSamplingType, emission);

            //var c1 = GetPixelColor(testData, testData.PathtracingSampler, sampleCount);   //BrdfSampling
            var c2 = GetPixelColor(testData, testData.DirectLightingSampler, sampleCount);//LightSourceSampling1
            var c3 = GetPixelColor(testData, testData.DirectLightingOnEdgeSampler, sampleCount);//LightSourceSampling2
            var c4 = GetPixelColor(testData, testData.LightTracingSampler, sampleCount);  //Lighttracing

            string result = PathContributionForEachPathSpace.CompareAll(c2, c3, c4);

            //Assert.IsTrue(Math.Abs(c1["C D L"] - expectedColorCDL) < maxCDLError, "PathtracingCDL: " + c1["C D L"]);
            //Assert.IsTrue(Math.Abs(c1["C P L"] - expectedColorCPL) < maxCPLError, "PathtracingCPL: " + c1["C P L"]);

            Assert.IsTrue(Math.Abs(c2["C D L"] - expectedColorCDL) < maxCDLError, "DirectLightingCDL: " + c2["C D L"]);
            Assert.IsTrue(Math.Abs(c2["C P L"] - expectedColorCPL) < maxCPLError, "DirectLightingCPL: " + c2["C P L"]);

            //Assert.IsTrue(Math.Abs(c3["C D L"] - expectedColorCDL) < maxCDLError, "DirectLightingOnEdgeCDL: " + c3["C D L"]); //Wird von diesen Verfahren nicht geampelt
            Assert.IsTrue(Math.Abs(c3["C P L"] - expectedColorCPL) < maxCPLError, "DirectLightingOnEdgeCPL: " + c3["C P L"]);

            Assert.IsTrue(Math.Abs(c4["C D L"] - expectedColorCDL) < maxCDLError, "LighttracingCDL: " + c4["C D L"]);
            Assert.IsTrue(Math.Abs(c4["C P L"] - expectedColorCPL) < maxCPLError, "LighttracingCPL: " + c4["C P L"]);
        }

        private void CheckHowManyLightPathsAreVisible(LightsourceType lightsourceType, bool withMediaSphere, bool withGlobalMedia, PathSamplingType pathSamplingType)
        {
            FullPathTestData testData = new FullPathTestData(lightsourceType, 10, withMediaSphere, withGlobalMedia, pathSamplingType);
            float isVisible = GetHowManyLightPathsAreVisible(testData, sampleCount);
            Assert.IsTrue(isVisible > 0.5f);
        }

            //Gibt an, wie viel Prozent der ausgesendeten Photonen im Sichtbareich liegen
        private float GetHowManyLightPathsAreVisible(FullPathTestData testData, int sampleCount)
        {
            int visibleCounter = 0;
            IRandom rand = new Rand(0);
            for (int i = 0; i < sampleCount; i++)
            {
                var lightPath = testData.LightPathSampler.SamplePathFromLighsource(rand);
                bool isVisible = lightPath.Points.Any(x => x.Index > 0 && testData.Camera.GetPixelPositionFromEyePoint(x.Position) != null);
                if (isVisible) visibleCounter++;
            }
            return visibleCounter / (float)sampleCount;
        }


        private PathContributionForEachPathSpace GetPixelColor(FullPathTestData testData, IFullPathSamplingMethod fullPathSampler, int sampleCount)
        {            
            int pixX = 0, pixY = 0;
            PathContributionForEachPathSpace pathContribution = new PathContributionForEachPathSpace();
            if (fullPathSampler == null) return pathContribution;
            IRandom rand = new Rand(0);

            int errorCount = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                List<FullPath> paths = fullPathSampler.SampleFullPaths(testData.EyePathSampler.SamplePathFromCamera(pixX, pixY, rand), testData.LightPathSampler.SamplePathFromLighsource(rand), null, rand);
                foreach (var path in paths)
                {
                    //Vergleiche DirectLigting-Sample-PdfA mit GetDirectLightingPdfA
                    var directLightingResult = testData.LightSourceSampler.GetRandomPointOnLight(path.Points[path.Points.Length - 2].Position, rand);
                    if (directLightingResult != null)
                    {
                        IntersectionPoint directLightingPoint = directLightingResult.LightPointIfNotIntersectable;
                        if (directLightingPoint == null) directLightingPoint = testData.IntersectionFinder.GetIntersectionPoint(new Ray(path.Points[path.Points.Length - 2].Position, directLightingResult.DirectionToLightPoint), 0, path.Points[path.Points.Length - 2].LocationType == ParticipatingMedia.MediaPointLocationType.Surface ? path.Points[path.Points.Length - 2].Point.IntersectedObject : null);
                        if (directLightingPoint != null) //Spotlichter erzeugen Richtungsvektoren, die auch außerhalb der Lichtquelle zeigen
                        {
                            float directLightingPdfA = testData.LightSourceSampler.GetDirectLightingPdfA(path.Points[path.Points.Length - 2].Position, directLightingPoint, 0);
                            float difference = Math.Abs(directLightingResult.PdfA - directLightingPdfA);
                            if (directLightingResult.PdfA > 1000)
                            {
                                errorCount++;
                                Assert.IsTrue(difference < 1, $"Difference= + {difference}, directLightingResult.PdfA={directLightingResult.PdfA}, directLightingPdfA={directLightingPdfA}");
                            }
                            else
                            {
                                errorCount++;
                                Assert.IsTrue(difference < 0.01f, $"Difference= + {difference}, directLightingResult.PdfA={directLightingResult.PdfA}, directLightingPdfA={directLightingPdfA}");
                            }
                        }
                    }
                    

                    //Vergleiche Lighttracing-SampleDirection-PdfW mit Ligthtracing-GetPdfW
                    float pdfW1 = testData.LightSourceSampler.PdfWFromLightDirectionSampling(path.Points.Last().Point.SurfacePoint, Vector3D.Normalize(path.Points[path.Points.Length - 2].Position - path.Points.Last().Position));
                    if (path.Points.Last().Point.BrdfSampleEventOnThisPoint != null)
                    {
                        float pdfw2 = path.Points.Last().Point.BrdfSampleEventOnThisPoint.PdfW;
                        if (pdfw2 != 1000000) //Beim FarAway-Light ist die PdfW fix 1000000 und stimmt somit nicht
                        {
                            errorCount++;
                            Assert.IsTrue(Math.Abs(pdfW1 - pdfw2) < 0.01f, "Difference=" + Math.Abs(pdfW1 - pdfw2));
                        }
                        
                    }

                    //Vergleiche Lighttracing-SamplePosition-PdfA mit Ligthtracing-GetPositionPdfA
                    float pdfA1 = testData.LightSourceSampler.PdfAFromRandomPointOnLightSourceSampling(path.Points.Last().Point.SurfacePoint);
                    float pdfA2 = (float)path.Points.Last().LightPdfA;
                    errorCount++;
                    Assert.IsTrue(Math.Abs(pdfA1 - pdfA2) < 0.000001f, "Difference=" + Math.Abs(pdfA1 - pdfA2));


                    //Achtung: Diese Lokik klappt nicht, wenn der PixelRange nur ein Pixel groß ist und die PixePosition im Minusbereich ist
                    //Beim Lighttracing mit Media erhält man solche Pfade
                    if (path.PixelPosition == null || ((int)path.PixelPosition.X == pixX && (int)path.PixelPosition.Y == pixY))
                    {
                        pathContribution.AddEntry(path, path.PathContribution / fullPathSampler.SampleCountForGivenPath(path) / sampleCount);
                    }

                }
            }

            return pathContribution;
        }

        [TestMethod]
        public void DirectionFromInfinity_LightTracing_EachLightPointHasEqualPathWeight()
        {
            FullPathTestData testData = new FullPathTestData(LightsourceType.DirectionFromInfinity, 1, false, false,  PathSamplingType.NoMedia);
            IRandom rand = new Rand(0);

            Vector3D pathWeight = null;

            int sampleCount = 1000;
            for (int i = 0; i < sampleCount; i++)
            {
                var points = testData.LightPathSampler.SamplePathFromLighsource(rand).Points;
                if (points.Length >= 2)
                {
                    if (pathWeight == null)
                        pathWeight = points[1].PathWeight;
                    else
                        Assert.AreEqual(pathWeight.X, points[1].PathWeight.X);
                }
            }
        }
    }
}
