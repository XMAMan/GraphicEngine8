using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace FullPathGeneratorTest.BasicTests
{
    [TestClass]
    public class PointDataBeamQueryTest //Tests für 1,2,3,4,5
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void A_WithDistanceSampling_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            //var testSzene = new BoxTestSzene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true });
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true, ScreenWidth = 30, ScreenHeight = 30, PixX = 10, PixY = 10, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.5f });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.PointDataBeamQuery);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene, 1000);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=1,88704349606202E-14}
        }

        [TestMethod]
        [Ignore] //Eine Mischung aus Distanz- und Nicht-Distanzsampling erzeugt keine gültigen Eye/Light-PdfAs
        public void A_NoDistanceSampling_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            //Der LightPath wird mit Distancesampling erzeugt und der EyePath ohne. Dadurch ist dann die PdfL beim LightPath != 1 und beim EyePath == 1
            //Mein PdfA-Checker geht davon aus, dass entweder der komplette Pfad mit oder ohne Distanzesampling erzeugt wurde. 
            //Deswegen habe ich hier so ein großen PdfA-Error-Schwellwert            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.PointDataBeamQuery);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=1,43051147460938E-06; LightPdfA=5,96046447753906E-08; GeometryTerm=2,42275045364058E-07}
        }

        [TestMethod]
        public void B_WithDistanceSampling_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.PointDataBeamQuery);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck);
            result.Image.Save(WorkingDirectory + "PointDataBeamQuery1.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(4) < 24, "Error=" + result.MaxErrorUptoGivenPathLength(4));
        }

        [TestMethod]
        public void B_NoDistanceSampling_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.PointDataBeamQuery);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck);
            result.Image.Save(WorkingDirectory + "PointDataBeamQuery2.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(4) < 24, "Error=" + result.MaxErrorUptoGivenPathLength(4));
        }

        [TestMethod]
        public void C_WithDistanceSampling_PathContributionSumForEachPathLengthCheck() //Test 5
        {            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.PointDataBeamQuery);
            //testSzene.CheckEachPathContributionCameraEqualSampling(PathContributionCalculator.GetPathContributionForEachPathLength(method, testSzene, testSzene.SamplecountForPathContributionCheck).PathContribution);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck);
            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] {"C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L"});
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void C_NoDistanceSampling_PathContributionSumForEachPathLengthCheck() //Test 5
        {            
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.PointDataBeamQuery);
            //testSzene.CheckEachPathContributionCameraEqualSampling(PathContributionCalculator.GetPathContributionForEachPathLength(method, testSzene, testSzene.SamplecountForPathContributionCheck).PathContribution);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck * 2);
            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }
    }
}
