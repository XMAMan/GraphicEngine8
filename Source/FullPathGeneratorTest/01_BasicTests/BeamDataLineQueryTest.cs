using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace FullPathGeneratorTest.BasicTests
{
    [TestClass]
    public class BeamDataLineQueryTest //Tests für 1,2,3,4,5
    {
        //private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void A_NoDistanceSampling_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            int photonenCount = 1000;            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling, CreateMediaBox = true, ScreenWidth = 30, ScreenHeight = 30, PixX = 10, PixY = 10, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.8f , PhotonenCount = photonenCount });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.BeamDataLineQuery);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene, 1000);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=1,17780773788294E-13}
        }

        [TestMethod]
        public void A_WithDistanceSampling_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            int photonenCount = 1000;            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling, CreateMediaBox = true, ScreenWidth = 30, ScreenHeight = 30, PixX = 10, PixY = 10, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.5f , PhotonenCount = photonenCount });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.BeamDataLineQuery);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene, 1000);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=5,66218891684057E-12}
        }

        [TestMethod]
        public void B_NoDistanceSampling_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {
            int photonenCount = 1000;            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling, CreateMediaBox = true, PhotonenCount = photonenCount });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.BeamDataLineQuery);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck / 10);
            result.Image.Save(WorkingDirectory + "BeamDataLineQuery1.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(4) < 24, "Error=" + result.MaxErrorUptoGivenPathLength(4));
        }

        [TestMethod]
        public void B_WithDistanceSampling_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {
            int photonenCount = 1000;            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling, CreateMediaBox = true, PhotonenCount = photonenCount });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.BeamDataLineQuery);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck / 10);
            result.Image.Save(WorkingDirectory + "BeamDataLineQuery2.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(4) < 24, "Error=" + result.MaxErrorUptoGivenPathLength(4));
        }

        [TestMethod]
        public void C_NoDistanceSampling_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            int photonenCount = 1000;            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling, CreateMediaBox = true, PhotonenCount = photonenCount });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.BeamDataLineQuery);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck / 10);
            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C P L", "C P D L", "C P D D L", "C D P D D D L", "C D P D D L", "C D P D L", "C D P L", "C D D P L", "C D D P D L", "C D D D P L", "C D D D D P L", "C P D D D L", "C P D D D D L", "C D D D P D L", "C D D P D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void C_WithDistanceSampling_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            int photonenCount = 1000;            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling, CreateMediaBox = true, PhotonenCount = photonenCount });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.BeamDataLineQuery);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck / 10);
            //string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        
    }
}
