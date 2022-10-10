using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace FullPathGeneratorTest.MultipleImportanceTests
{
    [TestClass]
    public class PointDataBeamQueryTest //Bilde alle MIS-2er-Kombinationen zwischen VertexMerging und dem Rest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void WithDistanceSampling_WithPathTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,
                UsePointDataBeamQuery = true,
            });
            var settings = new PhotonmapSettings() { CreatePointDataBeamQueryMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void WithDistanceSampling_WithDirectLighting() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseDirectLighting = true,
                UsePointDataBeamQuery = true,
            });
            var settings = new PhotonmapSettings() { CreatePointDataBeamQueryMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void WithDistanceSampling_WithLighTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseLightTracing = true,
                UsePointDataBeamQuery = true,
            });
            var settings = new PhotonmapSettings() { CreatePointDataBeamQueryMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void WithDistanceSampling_WithVertexConnection() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseVertexConnection = true,
                UsePointDataBeamQuery = true,
            });
            var settings = new PhotonmapSettings() { CreatePointDataBeamQueryMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L"});
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void NoDistanceSampling_WithPathTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,
                UsePointDataBeamQuery = true,
            });
            var settings = new PhotonmapSettings() { CreatePointDataBeamQueryMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void NoDistanceSampling_WithDirectLighting() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseDirectLighting = true,
                UsePointDataBeamQuery = true,
            });
            var settings = new PhotonmapSettings() { CreatePointDataBeamQueryMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C P L", "C D D L", "C D P L", "C D D D L", "C D D P L", "C D D D D L", "C D D D P L", "C D D D D D L", "C D D D D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }
    }
}
