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

namespace FullPathGeneratorTest.MultipleImportanceTests
{
    //Bilde alle MIS-2er-Kombinationen zwischen DirectLightingOnEdge und dem Rest
    [TestClass]
    public class DirectLightingOnEdgeTest //Tests für 6,7 (Alle FullPath-Sampling-Verfahren in Mis-Gewichteter Summe)
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void NoDistanceSampling_WithPathTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,
                UseDirectLightingOnEdge = true,
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void NoDistanceSampling_WithDirectLighting() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseDirectLighting = true,
                UseDirectLightingOnEdge = true,
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void NoDistanceSampling_WithLighTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseLightTracing = true,
                UseDirectLightingOnEdge = true,
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void NoDistanceSampling_WithVertexConnection() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseVertexConnection = true,
                UseDirectLightingOnEdge = true,
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void NoDistanceSampling_WithLightTracingOnEdge() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseLightTracingOnEdge = true,
                UseDirectLightingOnEdge = true,
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C P L", "C P D L", "C D P L", "C P D D L", "C D D P L", "C P D D D L", "C D D D P L", "C P D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void WithDistanceSampling_WithPathTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,
                UseDirectLightingOnEdge = true,
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck * 2);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C P L", "C D P L", "C D D P L", "C D D D P L", "C D D D D P" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void WithDistanceSampling_WithDirectLighting() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseDirectLighting = true,
                UseDirectLightingOnEdge = true,
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck);

            string compare = expected.CompareWithOther(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C P L", "C D P L", "C D D P L", "C D D D P L", "C D D D D P" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void WithDistanceSampling_WithLighTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseLightTracing = true,
                UseDirectLightingOnEdge = true,
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck);

            string compare = expected.CompareWithOther(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void WithDistanceSampling_WithVertexConnection() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseVertexConnection = true,
                UseDirectLightingOnEdge = true,
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck);

            string compare = expected.CompareWithOther(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L"});
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void WithDistanceSampling_WithUseLightTracingOnEdge() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseLightTracingOnEdge = true,
                UseDirectLightingOnEdge = true,
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C P L", "C P D L", "C D P L", "C P P L", "C D P P L", "C P P D L", "C P D D L", "C D D P L", "C P P P L", "C P D P L", "C P P P D L", "C P D D P L", "C P D P P L", "C P D P D L", "C P P D D L", "C D P P P L", "C D P D P L", "C P P D P L", "C P D D D L", "C D D P P L", "C P P P P L", "C D D D P L", "C D P D D P L", "C P D D P D L", "C P P P D D L", "C D D D P P L", "C D D P P P L", "C D P D P P L", "C P D D D D L", "C P D D D P L", "C D D D D P L", "C D D P D P L", "C P D P P D L", "C D P P P P L", "C P D P D D L", "C P D P P P L", "C P P P P D L", "C P D D P P L", "C P P D P D L", "C P P D D D L", "C P P D D P L", "C P P P D P L", "C D P P D P L", "C P D P D P L", "C P P D P P L", "C P P P P P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }
    }
}
