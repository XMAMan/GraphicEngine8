using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;
using System.IO;

namespace FullPathGeneratorTest.BasicTests
{
    [TestClass]
    public class DirectlightingTest //Tests für 1,2,3,4,5
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void A_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.DirectLighting);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=7,31589648480776E-12}
        }

        [TestMethod]
        public void A_MediaNoDistance_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.DirectLighting);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=6,5346540348673E-16}
        }

        [TestMethod]
        public void B_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.DirectLighting);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck);
            result.Image.Save(WorkingDirectory + "DirectLighting.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(4) < 16, "Error=" + result.MaxErrorUptoGivenPathLength(4));
        }

        [TestMethod]
        public void C_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.DirectLighting);
            //testSzene.CheckEachPathContributionCameraEqualSampling(PathContributionCalculator.GetPathContributionForEachPathLength(method, testSzene, testSzene.SamplecountForPathContributionCheck).PathContribution);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck * 3);
            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }
    }
}
