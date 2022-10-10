using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace FullPathGeneratorTest.BasicTests
{
    [TestClass]
    public class MediaPathtracingTest //Tests für 1,2,3,4,5
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void A_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            //var testSzene = new BoxTestSzene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true);
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true, ScreenWidth = 30, ScreenHeight = 30, PixX = 10, PixY = 10, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.8f });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Pathtracing);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=2,01948391736579E-28}
        }

        [TestMethod]
        public void B_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Pathtracing);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck);
            result.Image.Save(WorkingDirectory + "MediaPathtracing.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(3) < 16, "Error=" + result.MaxErrorUptoGivenPathLength(3));
        }

        [TestMethod]
        public void C_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Pathtracing);
            //testSzene.CheckEachPathContributionCameraEqualSamplingWithMedia(PathContributionCalculator.GetPathContributionForEachPathLength(method, testSzene, testSzene.SamplecountForPathContributionCheck * 2).PathContribution);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck * 2);
            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            Assert.AreEqual("", error, error);

        }
    }
}
