using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RayCameraNamespace;
using SubpathGenerator;

namespace FullPathGeneratorTest.PixelRadianceTests
{
    [TestClass]
    public class PixelRadianceTests //Tests für 7 (Summe aller PathSpaces)
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void FullBidirectionalPathTracing_NoMedia() //Test 7 (Pixelradiance)
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia, false, PixelSamplingMode.Tent);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, BTPTSettings());

            double expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaTent.txt").SumOverAllPathSpaces().X;
            double actual = PathContributionCalculator.GetMisWeightedPixelRadiance(testSzene, fullPathSampler, testSzene.SamplecountForPathContributionCheck);

            Assert.IsTrue(Math.Abs(expected - actual) < testSzene.maxContributionError, "expected=" + expected + " actual=" + actual);
        }

        [TestMethod]
        public void FullBidirectionalPathTracing_WithMedia() //Test 7 (Pixelradiance)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true, PixelSamplingMode.Tent);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, BTPTSettings());

            double expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaTent.txt").SumOverAllPathSpaces().X;
            double actual = PathContributionCalculator.GetMisWeightedPixelRadiance(testSzene, fullPathSampler, testSzene.SamplecountForPathContributionCheck);

            Assert.IsTrue(Math.Abs(expected - actual) < testSzene.maxContributionError, "expected=" + expected + " actual=" + actual);
        }

        [TestMethod]
        public void UnbiasedMedia_WithMedia() //Test 7 (Pixelradiance)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling, true, PixelSamplingMode.Tent);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,
                UseLightTracing = true,
                UseDirectLighting = true,
                UseMultipleDirectLighting = true,
                UseVertexConnection = true,
                UseDirectLightingOnEdge = true,
                UseLightTracingOnEdge = true
            });


            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaTent.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck);
            string compare = expected.CompareWithOther(actual);
            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);

            //double expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaTent.txt").SumOverAllPathSpaces().X;
            //double actual = PathContributionCalculator.GetMisWeightedPixelRadiance(testSzene, fullPathSampler, testSzene.SamplecountForPathContributionCheck);

            //Assert.IsTrue(Math.Abs(expected - actual) < testSzene.maxContributionError, "expected=" + expected + " actual=" + actual);
        }

        [TestMethod]
        public void UPBP_WithMedia() //Test 7 (Pixelradiance)
        {
            int photonenCount = 1000;
            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling, CreateMediaBox = true, PixelMode = PixelSamplingMode.Tent, PhotonenCount = photonenCount });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,
                UseDirectLighting = true,
                UseVertexConnection = true,
                UseLightTracing = true,
                UseVertexMerging = true,
                UsePointDataPointQuery = true,
                UsePointDataBeamQuery = true,
                UseBeamDataLineQuery = true
            });
            var settings = new PhotonmapSettings() 
            { 
                CreateBeamDataLineQueryMap = true, BeamDataLineQueryReductionFactor = 1,
                CreatePointDataBeamQueryMap = true,
                CreatePointDataPointQueryMap =true,
                CreateSurfaceMap = true
            };
            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaTent.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck / 2, settings);
            string compare = expected.CompareWithOther(actual);
            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }



        private FullPathSettings BTPTSettings()
        {
            return new FullPathSettings()
            {
                UsePathTracing = true,
                UseLightTracing = true,
                UseDirectLighting = true,
                UseMultipleDirectLighting = true,
                UseVertexConnection = true,
            };
        }
    }
}
