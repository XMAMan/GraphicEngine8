using System;
using System.Collections.Generic;
using System.Linq;
using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;

namespace FullPathGeneratorTest.BasicTests
{
    [TestClass]
    public class MediaVertexConnectionTest //Tests für 1,2,3,4,5
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void A_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true);
            //var testSzene = new BoxTestSzene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true, ScreenWidth = 30, ScreenHeight = 30, PixX = 10, PixY = 10, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.8f });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexConnection);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=1,35525271560688E-20}
        }

        [TestMethod]
        [Ignore]
        public void B_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {
            //Die VertexConnection-Klasse ist die Vereinigung von vielen Samplern (EyeIndex n; LightIndex m; jeden n-m-Ausprägung ist ein VC-Sampler)
            //Die Pfad-PdfA ist dann die PdfA von ein konkreten nn-Sampler. Die Funktion-PdfA ist dann die Oder-Verküpfung über alle nm-Sampler.
            //Somit kann die Pfad-PdfA niemals mit der Funktion-PdfA matchen und dieser Unittest ist somit rot.
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexConnection);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck);
            result.Image.Save(WorkingDirectory + "MediaVertexConnection.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(4) < 10, "Error=" + result.MaxErrorUptoGivenPathLength(4));
        }

        [TestMethod]
        public void C_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexConnection);
            //testSzene.CheckEachPathContributionCameraEqualSamplingWithMedia(PathContributionCalculator.GetPathContributionForEachPathLength(method, testSzene, testSzene.SamplecountForPathContributionCheck * 2).PathContribution);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck * 1);
            //string all = actual.ToString();
            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] {"C P L", "C D L" });
            Assert.AreEqual("", error, error);
        }
    }
}
