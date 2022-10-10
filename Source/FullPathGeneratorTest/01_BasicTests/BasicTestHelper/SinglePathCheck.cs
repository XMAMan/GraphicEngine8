using System;
using System.Collections.Generic;
using System.Linq;
using ParticipatingMedia;
using FullPathGenerator;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Photonusmap;
using RayCameraNamespace;
using RaytracingBrdf;
using SubpathGenerator;
using IntersectionTests;
using RayTracerGlobal;
using GraphicGlobal;
using FullPathGeneratorTest._01_BasicTests.BasicTestHelper;

namespace FullPathGeneratorTest.BasicTests.BasicTestHelper
{
    class MaxOccuredError
    {
        public double EyePdfA = 0;
        public double LightPdfA = 0;
        public double GeometryTerm = 0;

        public void SetEyePdfAIfMax(double eyePdfA)
        {
            if (eyePdfA > this.EyePdfA) this.EyePdfA = eyePdfA;
        }

        public void SetLightPdfAIfMax(double lightPdfA)
        {
            if (lightPdfA > this.LightPdfA) this.LightPdfA = lightPdfA;
        }

        public void SetGeometryTermIfMax(double geometryTerm)
        {
            if (geometryTerm > this.GeometryTerm) this.GeometryTerm = geometryTerm;
        }

        public override string ToString()
        {
            return $"EyePdfA={this.EyePdfA}; LightPdfA={this.LightPdfA}; GeometryTerm={this.GeometryTerm}";
        }
    }

    class SinglePathCheck
    {
        public static MaxOccuredError ComparePathContributionWithGeometrySum(Sampler sampler, BoxTestScene testSzene, int sampleCount = 10000, float maxGeometryTermError = 3.0e-5f, float maxPdfAError = 0)
        {
            return new SinglePathCheck(sampler, testSzene, maxPdfAError).ComparePathContributionWithGeometrySum(maxGeometryTermError, sampleCount);
        }

        private readonly Sampler sampler;
        private readonly BoxTestScene testSzene;
        private readonly PathTroughputCalculator pathTroughputCalculator;
        private readonly PathPdfACalculator pathPdfACalculator;

        private SinglePathCheck(Sampler sampler, BoxTestScene testSzene, float maxPdfAError)
        {
            this.sampler = sampler;
            this.testSzene = testSzene;            

            float maxDistance = sampler.PhotonSettings != null ? 0.031f * testSzene.SizeFactor : MagicNumbers.DistanceForPoint2PointVisibleCheck;           
            this.pathTroughputCalculator = new PathTroughputCalculator(testSzene.ScatteringFromMedia, testSzene.AbsorbationFromMedia, testSzene.AnisotrophyCoeffizient, testSzene.PixX, testSzene.PixY, testSzene.Camera, maxDistance);
            this.pathPdfACalculator = new PathPdfACalculator(new PathPdfACalculatorInputData()
            {
                EyePathSamplingType = testSzene.EyePathSamplingType,
                LightPathSamplingType = testSzene.LightPathSamplingType,
                Camera = testSzene.Camera,
                PixX = testSzene.PixX,
                PixY = testSzene.PixY,
                SceneHasMedia = testSzene.SceneHasMedia,
                ScatteringFromMedia = testSzene.ScatteringFromMedia,
                AbsorbationFromMedia = testSzene.AbsorbationFromMedia,
                ScatteringFromGlobalMedia = testSzene.ScatteringFromGlobalMedia,
                AnisotrophyCoeffizient = testSzene.AnisotrophyCoeffizient,
                LightSourceArea = testSzene.LightSourceArea,
                MaxMediaLineEndToPathPointDistance = maxDistance,
                KernelDistance = sampler.PhotonSettings != null ? 0.031f * testSzene.SizeFactor : 0,
                MaxAllowedError = maxPdfAError
            });
        }               

        private MaxOccuredError ComparePathContributionWithGeometrySum(float maxGeometryTermError, int sampleCount)
        {
            MaxOccuredError maxError = new MaxOccuredError();
            int mediaPathCounter = 0;
            SubPathFloatingErrorCleaner subPathCleaner = new SubPathFloatingErrorCleaner(this.testSzene);

            var frameData = PathContributionCalculator.CreateFrameDataWithPhotonmap(testSzene.LightPathSampler, testSzene.PhotonenCount, testSzene.rand, testSzene.SizeFactor, sampler.PhotonSettings, subPathCleaner);

            //string randString = "AAEAAAD/////AQAAAAAAAAAEAQAAAA1TeXN0ZW0uUmFuZG9tAwAAAAVpbmV4dAZpbmV4dHAJU2VlZEFycmF5AAAHCAgIAwAAABgAAAAJAgAAAA8CAAAAOAAAAAgAAAAAPvDiXeDggi3or8pOqP+uLTWueh8ZFzwyBWcRSjvqrVxKZeUrNYBKPd7pAByffNl+/5Fvdw3FxxiUdwM3MNGyfCSh9joemI1d8krcSbOiNiBBZjwsMqv+al5crzeyoEF3CXGcC4MOHR0dav48M9pTLU7x2DR2e9cyKF9eBIgspxPY8WFJ+0/pfmMXpDsF6WJWDzIRRj1l9BNPlUIu3bsRTjHp+m0GuRVZHKPTJiFwYhF4yGMYwEfROyAyDk+3xJdOwUxVSDw0gjBI/CM3Net2ckFAgT5D845nhogCSws=";
            //testSzene.rand = new Rand(randString);

            for (int i = 0; i < sampleCount; i++)
            {
                string randomObjectBase64Coded = testSzene.rand.ToBase64String();

                var eyePath = sampler.CreateEyePath ? testSzene.EyePathSampler.SamplePathFromCamera(testSzene.PixX, testSzene.PixY, testSzene.rand) : null;
                var lightPath = sampler.CreateLightPath ? testSzene.LightPathSampler.SamplePathFromLighsource(testSzene.rand) : null;

                subPathCleaner.RemoveFloatingPointErrors(eyePath, testSzene.EyePathSamplingType);
                subPathCleaner.RemoveFloatingPointErrors(lightPath, testSzene.LightPathSamplingType);

                List<FullPath> paths = sampler.SamplerMethod.SampleFullPaths(eyePath, lightPath, frameData, testSzene.rand);
                foreach (var path in paths)
                {
                    //Assert.IsTrue(sampler.SamplerMethod.CanSampleThisPathLength(path.Points.Length), "CanSampleThisPathLength=false für PathLength=" + path.Points.Length);//Jeder Pfad, der gesampelt wurde, muss auch mit CanSampleThisPathLength=true beantwortet werden
                    Assert.IsTrue(path.Points.Length <= testSzene.MaxPathLength); //Überprüfe, dass maximale Pfadlänge nicht überschritten wird
                    Assert.IsTrue(HasEachPointScattering(path)); //Bei unbiased-Verfahren (Pathtracing, Lightracing, Directlighting, VertexConnection) müssen alle Pfadpunkte auf ein Scatterteilchen liegen

                    //Überprüfe die Eye- und LightPdfA-Werte von den FullpathPoints
                    maxError.SetEyePdfAIfMax(this.pathPdfACalculator.CheckEyePdfA(path));
                    maxError.SetLightPdfAIfMax(this.pathPdfACalculator.CheckLightPdfA(path));

                    if (testSzene.SceneHasMedia)
                    {
                        double err = GetDifferenceBetweenPathContributionAndGeometryBrdfSumWithMedia(path);
                        Assert.IsTrue(err <= maxGeometryTermError, "PathContribtion-Error=" + err);
                        maxError.SetGeometryTermIfMax(err);

                        if (path.Points.Any(x => x.LocationType == MediaPointLocationType.MediaParticle)) mediaPathCounter++;
                    }
                    else
                    {
                        double err = GetDifferenceBetweenPathContributionAndGeometryBrdfSum(path);
                        Assert.IsTrue(err < maxGeometryTermError, "PathContribtion-Error=" + err);
                        maxError.SetGeometryTermIfMax(err);
                    }
                }

                //Beim MultipleDirectLighting wird StratifiedSampling angewendet. Somit deckt jeder einzelne Pfad für eine gegebenen Pfadlänge nur ein Teilbereich vom Integral ab
                //Bei StratifiedSampling-Samplingverfahren kann die SampleCountForGivenPathlength-Methode nicht überprüft werden
                if (sampler.SamplerMethod.GetType() != typeof(MultipleDirectLighting) && //Ich unterteile den Pfadraum anhand der Lichtquellen in Disjunkte Mengen
                    sampler.SamplerMethod.GetType() != typeof(DirectLightingOnEdge) && //Ich unterteile den Pfadraum in Particle-Pfade und Surface-Pfade
                    sampler.SamplerMethod.GetType() != typeof(LightTracingOnEdge) &&
                    sampler.SamplerMethod.GetType() != typeof(VertexMerging) &&
                    sampler.SamplerMethod.GetType() != typeof(PointDataPointQuery) &&
                    sampler.SamplerMethod.GetType() != typeof(PointDataBeamQuery) &&
                    sampler.SamplerMethod.GetType() != typeof(BeamDataLineQuery)) 
                {
                    //Prüfe, dass nicht mehr Pfade für eine gegebene Pfadlänge erzeugt wurden, als wie vom SampleVerfahren behauptet das es das tun würde
                    foreach (var pathGroup in paths.GroupBy(x => x.Points.Length))
                    {
                        Assert.IsTrue(pathGroup.Count() <= sampler.SamplerMethod.SampleCountForGivenPath(pathGroup.First()), "PathLength=" + pathGroup.First().Points.Length + ", SampleCountForGivenPathlength=" + sampler.SamplerMethod.SampleCountForGivenPath(pathGroup.First()) + ", SampledPaths=" + pathGroup.Count());
                    }
                }

            }

            if (testSzene.SceneHasMedia && testSzene.EyePathSamplingType != PathSamplingType.ParticipatingMediaWithoutDistanceSampling) Assert.IsTrue(mediaPathCounter > 0);

            return maxError;
        }

        private static bool HasEachPointScattering(FullPath path)
        {
            foreach (var point in path.Points)
            {
                if (HasLocationTypeScattering(point.LocationType) == false) return false;
                if (point.EyeLineToNext != null && HasLocationTypeScattering(point.EyeLineToNext.EndPointLocation) == false) return false;
                if (point.LightLineToNext != null && HasLocationTypeScattering(point.LightLineToNext.EndPointLocation) == false) return false;
            }
            return true;
        }

        private static bool HasLocationTypeScattering(MediaPointLocationType locationType)
        {
            bool hasScatter = locationType == MediaPointLocationType.Camera ||
                              locationType == MediaPointLocationType.Surface ||
                              locationType == MediaPointLocationType.MediaBorder ||
                              locationType == MediaPointLocationType.MediaParticle;
            return hasScatter;
        }

        private double GetDifferenceBetweenPathContributionAndGeometryBrdfSum(FullPath path)
        {
            double geometrySum = this.pathTroughputCalculator.GetPathtroughput(path.Points);
            double geometryContribution = path.PathContribution.X * path.PathPdfA / testSzene.EmissionPerArea;
            return Math.Abs(geometryContribution - geometrySum);
        }

        private double GetDifferenceBetweenPathContributionAndGeometryBrdfSumWithMedia(FullPath path)
        {
            Assert.AreEqual(1, path.Points.Count(x => x.EyeLineToNext == null && x.LightLineToNext == null));//Genau ein Punkt aus der Kette hat keine LineTo-Nexts. Dort ist der Verbindungspunkt zwischen den SubPaths

            float geometrySum = this.pathTroughputCalculator.GetPathtroughputWithMedia(path.Points);
            // float geometryContribution = (float)(path.PathContribution.X * path.PathPdfA) / testSzene.EmissionPerArea;
            float geometryContribution = (float)(path.PathContribution.X * (float)path.PathPdfA) / testSzene.EmissionPerArea;

            //return Math.Abs(geometryContribution - geometrySum);
            return Math.Abs((float)geometryContribution - (float)geometrySum);
        }

        
    }
}
