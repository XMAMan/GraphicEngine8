using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace FullPathGeneratorTest.MultipleImportanceTests
{
    [TestClass]
    public class VertexMergingTest //Bilde alle MIS-2er-Kombinationen zwischen VertexMerging und dem Rest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void NoMedia_WithPathTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia, false, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void NoMedia_WithDirectLighting() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia, false, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseDirectLighting = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void NoMedia_WithLighTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia, false, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseLightTracing = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void NoMedia_WithVertexConnection() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia, false, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseVertexConnection = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void MediaNoDistance_WithPathTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void MediaNoDistance_WithDirectLighting() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseDirectLighting = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void MediaNoDistance_WithLightTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseLightTracing = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void MediaNoDistance_WithVertexConnection() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseVertexConnection = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void MediaNoDistance_WithDirectLightingOnEdge() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseDirectLightingOnEdge = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void MediaNoDistance_WithLightTracingOnEdge() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseLightTracingOnEdge = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C P D L", "C D D L", "C D D D L", "C P D D L", "C P D D D L", "C D D D D L", "C D D D D D L", "C P D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void MediaLongRays_WithPathTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck * 3, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void MediaLongRays_WithDirectLighting() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseDirectLighting = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void MediaLongRays_WithLightTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseLightTracing = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void MediaLongRays_WithDirectLightingOnEdge() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseDirectLightingOnEdge = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck * 2, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void MediaLongRays_WithLightTracingOnEdge() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseLightTracingOnEdge = true,
                UseVertexMerging = true,
            });
            var settings = new PhotonmapSettings() { CreateSurfaceMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D P P P P L" });
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }
    }
}
