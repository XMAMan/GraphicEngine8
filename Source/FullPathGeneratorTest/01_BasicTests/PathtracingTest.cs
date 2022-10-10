using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace FullPathGeneratorTest.BasicTests
{
    [TestClass]
    public class PathtracingTest  //Tests für 1,2,3,4,5
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void A_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Pathtracing);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=1,49172313092466E-11}
        }

        [TestMethod]
        public void B_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Pathtracing);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck * 10);
            result.Image.Save(WorkingDirectory + "Pathtracing.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(4) < 7, "Error=" + result.MaxErrorUptoGivenPathLength(4));
        }

        [TestMethod]
        public void C_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Pathtracing);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck * 2);
            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            Assert.AreEqual("", error, error);
        }
    }
}
