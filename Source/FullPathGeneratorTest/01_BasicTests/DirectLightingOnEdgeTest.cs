using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace FullPathGeneratorTest.BasicTests
{
    [TestClass]
    public class DirectLightingOnEdgeTest //Tests für 1,2,3,4,5
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

       

        [TestMethod]
        public void A_NoDistanceSampling_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            //var testSzene = new BoxTestSzene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true);
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling, CreateMediaBox = true, ScreenWidth = 30, ScreenHeight = 30, PixX = 10, PixY = 10, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.8f });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.DirectLightingOnEdgeWithSegmentSampling);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=1,51823251882616E-18}
        }


        //Erst sample ich von der Kamera oder von ein Surface aus eine Richtung beim SubPath-Erstellen und lande dann
        //an einer Wand. Die SubPath-Correction-Funktion bestimmt dann den Richtungsvektor zur Wand und korrigiert damit
        //die PdfW. Das DirectLightingOnEdge erzeugt dann zwischen Kamera und Wand ein Partikelpunkt und der
        //PathPdfACalculator bestimmt dann den Richtungsvektor zwischen Kamera und Partikel anstatt Kamera und Wand.
        //Wegen dieser Abweichung stimmt dann die EyePdfA nicht 100%
        //Die PdfW welche zum PointOnT-Punkt (Gesampelten Punkt) zeigt ist beim OnEdge-Sampeln falsch. Ich müsste im Fullpath
        //Sampler per GetBrdf/GetPixelPdfW neu nachjustieren, wenn ich PdfA-Error von 0 will
        [TestMethod]
        public void A_LongRays_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            //var testSzene = new BoxTestSzene(PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, true);
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true, ScreenWidth = 30, ScreenHeight = 30, PixX = 10, PixY = 10, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.4f });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.DirectLightingOnEdgeWithSegmentSampling);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=2,36434478896862E-14}
        }

        [TestMethod]
        public void B_NoDistanceSampling_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.DirectLightingOnEdgeWithSegmentSampling);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck);
            result.Image.Save(WorkingDirectory + "DirectLightingOnEdgeNoDistanceSampling.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(4) < 15, "Error=" + result.MaxErrorUptoGivenPathLength(4));
        }

        [TestMethod]
        public void B_LongRays_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.DirectLightingOnEdgeWithSegmentSampling);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck);
            result.Image.Save(WorkingDirectory + "DirectLightingOnEdgeWithDistanceSampling.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(4) < 15, "Error=" + result.MaxErrorUptoGivenPathLength(4));
        }

        [TestMethod]
        public void C_NoDistanceSampling_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.DirectLightingOnEdgeWithSegmentSampling);
           
            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C P L", "C D P L", "C D D P L", "C D D D P L", "C D D D D P L" });
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C P L" });
            Assert.AreEqual("", error, error);
        }

        [TestMethod]
        public void C_LongRays_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.DirectLightingOnEdgeWithSegmentSampling);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C P L", "C D P L", "C P P L", "C D P P L", "C D D P L", "C P P P L", "C P D P L", "C P D D P L", "C P D P P L", "C D P P P L", "C D P D P L", "C P P D P L", "C D D P P L", "C P P P P L", "C D D D P L", "C D P D D P L", "C D D D P P L", "C D D P P P L", "C D P D P P L", "C P D D D P L", "C D D D D P L", "C D D P D P L", "C D P P P P L", "C P D P P P L", "C P D D P P L", "C P P D D P L", "C P P P D P L", "C D P P D P L", "C P D P D P L", "C P P D P P L", "C P P P P P L" });
            //string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C P L" });
            Assert.AreEqual("", error, error);
        }
    }
}
