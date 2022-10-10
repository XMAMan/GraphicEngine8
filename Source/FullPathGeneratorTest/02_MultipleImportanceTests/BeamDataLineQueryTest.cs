using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace FullPathGeneratorTest.MultipleImportanceTests
{
    [TestClass]
    public class BeamDataLineQueryTest //Bilde alle MIS-2er-Kombinationen zwischen BeamDataLineQuery und dem Rest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void Media_WithPathTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            int photonenCount = 1000;            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, CreateMediaBox = true, PhotonenCount = photonenCount });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,
                UseBeamDataLineQuery = true,
            });
            var settings = new PhotonmapSettings() { CreateBeamDataLineQueryMap = true, BeamDataLineQueryReductionFactor = 1 };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L", "C P P P D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void WithDistanceSampling_WithDirectLighting() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            int photonenCount = 1000;            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, CreateMediaBox = true, PhotonenCount = photonenCount });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseDirectLighting = true,
                UseBeamDataLineQuery = true,
            });
            var settings = new PhotonmapSettings() { CreateBeamDataLineQueryMap = true, BeamDataLineQueryReductionFactor = 1 };

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
            int photonenCount = 1000;            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, CreateMediaBox = true, PhotonenCount = photonenCount });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseLightTracing = true,
                UseBeamDataLineQuery = true,
            });
            var settings = new PhotonmapSettings() { CreateBeamDataLineQueryMap = true, BeamDataLineQueryReductionFactor = 1 };

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
            int photonenCount = 1000;            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, CreateMediaBox = true, PhotonenCount = photonenCount });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseVertexConnection = true,
                UseBeamDataLineQuery = true,
            });
            var settings = new PhotonmapSettings() { CreateBeamDataLineQueryMap = true, BeamDataLineQueryReductionFactor = 1 };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L" });            
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void WithDistanceSampling_WithDirectLightingOnEdge() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            int photonenCount = 1000;            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, CreateMediaBox = true, PhotonenCount = photonenCount });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseDirectLightingOnEdge = true,
                UseBeamDataLineQuery = true,
            });
            var settings = new PhotonmapSettings() { CreateBeamDataLineQueryMap = true, BeamDataLineQueryReductionFactor = 1 };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void WithDistanceSampling_WithDirectPointDataPointQuery() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            int photonenCount = 10000;            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, CreateMediaBox = true, PhotonenCount = photonenCount });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePointDataPointQuery = true,
                UseBeamDataLineQuery = true,
            });
            var settings = new PhotonmapSettings() { CreatePointDataPointQueryMap = true, CreateBeamDataLineQueryMap = true, BeamDataLineQueryReductionFactor = 0.1f };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void WithDistanceSampling_WithPointDataBeamQuery() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            int photonenCount = 10000;            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, CreateMediaBox = true, PhotonenCount = photonenCount });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePointDataBeamQuery = true,
                UseBeamDataLineQuery = true,
            });
            var settings = new PhotonmapSettings() { CreatePointDataBeamQueryMap = true, CreateBeamDataLineQueryMap = true, BeamDataLineQueryReductionFactor = 0.1f };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }
    }
}
