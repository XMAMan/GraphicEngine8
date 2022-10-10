using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace FullPathGeneratorTest.BasicTests
{
    [TestClass]
    public class MultipeDirectLightingTest //Tests für 1,2,3,4,5
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void A_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.MultipeDirectLighting);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=1,10671984136735E-11}
        }

        [TestMethod]
        public void B_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.MultipeDirectLighting);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck);
            result.Image.Save(WorkingDirectory + "MultipeDirectLighting.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(4) < 16, "Error=" + result.MaxErrorUptoGivenPathLength(4));
        }

        [TestMethod]
        public void C_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.MultipeDirectLighting);
            //testSzene.CheckEachPathContributionCameraEqualSampling(PathContributionCalculator.GetPathContributionForEachPathLength(method, testSzene, testSzene.SamplecountForPathContributionCheck).PathContribution);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck * 2);
            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }
    }
}
