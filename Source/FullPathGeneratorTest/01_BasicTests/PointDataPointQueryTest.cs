using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace FullPathGeneratorTest.BasicTests
{
    [TestClass]
    public class PointDataPointQueryTest //Tests für 1,2,3,4,5
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void A_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            //var testSzene = new BoxTestSzene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true);
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true, ScreenWidth = 30, ScreenHeight = 30, PixX = 10, PixY = 10, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.8f });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.PointDataPointQuery);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=1,79844173197356E-20}
        }

        [TestMethod]
        public void B_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.PointDataPointQuery);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck);
            result.Image.Save(WorkingDirectory + "PointDataPointQuery.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(3) < 15, "Error=" + result.MaxErrorUptoGivenPathLength(3));
        }

        [TestMethod]
        public void C_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.PointDataPointQuery);
 
            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck * 2);

            string actualString = actual.ToString();

            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L","C D D L","C D D D L","C D D D D L","C D D D D D L", "C P P P D P L" });
            Assert.AreEqual("", error, error);
        }
    }
}
