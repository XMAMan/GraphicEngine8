using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace FullPathGeneratorTest.MultipleImportanceTests
{
    [TestClass]
    public class PointDataPointQueryTest //Bilde alle MIS-2er-Kombinationen zwischen PointDataPointQuery und dem Rest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void Media_WithPathTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,
                UsePointDataPointQuery = true,
            });
            var settings = new PhotonmapSettings() { CreatePointDataPointQueryMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L", "C P P P D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void Media_WithLightTracing() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true, PixelSamplingMode.Equal);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UseLightTracing = true,
                UsePointDataPointQuery = true,
            });
            var settings = new PhotonmapSettings() { CreatePointDataPointQueryMap = true };

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck, settings);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L", "C P P P D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }
    }
}
