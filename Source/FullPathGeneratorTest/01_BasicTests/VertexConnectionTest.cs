using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace FullPathGeneratorTest.BasicTests
{
    [TestClass]
    public class VertexConnectionTest //Tests für 1,2,3,4,5
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void A_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexConnection);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=2,92783161263633E-14}
        }

        [TestMethod]
        public void A_MediaNoDistance_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexConnection);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=2,29485206939219E-20}
        }

        [TestMethod]
        //[Ignore] //Kontrolliere beim FunktionPdfA==Path-Property-PdfA-Check nur noch den 4er-Pfad da es dort nur eine Samplingmöglichkeit gibt
        public void B_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {
            //Dieser Test faild momentan, da VertexConnection eigentlich mehrere Sampler sind, welche in einer Klasse vereint wurden
            //Beim Samplen bekomme ich die PfadPdfA von genau einen Sampler
            //Bei der Abfrage der FunctionPdfA erhalte ich die Summe von allen VC-Samplern
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexConnection);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck);
            result.Image.Save(WorkingDirectory + "VertexConnection.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(4) < 13, "Error=" + result.MaxErrorUptoGivenPathLength(4));
        }

        [TestMethod]
        public void C_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexConnection);
            //testSzene.CheckEachPathContributionCameraEqualSampling(PathContributionCalculator.GetPathContributionForEachPathLength(method, testSzene, testSzene.SamplecountForPathContributionCheck).PathContribution);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck);
            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }
    }
}
