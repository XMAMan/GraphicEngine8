using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RayCameraNamespace;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FullPathGeneratorTest.BasicTests
{
    [TestClass]
    public class LighttracingTest //Tests für 1,2,3,4,5
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void A_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Lighttracing);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=1,48172732221917E-11}
        }

        [TestMethod]
        public void A_MediaNoDistance_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Lighttracing);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=1,66081148687852E-15}
        }

        [TestMethod]
        public void B_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Lighttracing);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck);
            result.Image.Save(WorkingDirectory + "Lighttracing.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(4) < 24, "Error=" + result.MaxErrorUptoGivenPathLength(4));
        }

        [TestMethod]
        public void C_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Lighttracing);
            //testSzene.CheckEachPathContributionCameraEqualSampling(PathContributionCalculator.GetPathContributionForEachPathLength(method, testSzene, testSzene.SamplecountForPathContributionCheck).PathContribution);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck);
            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void D_SampleFullPaths_PathContributionFromLighttracing_TentSampling()
        {
            int sampleCount = 20000;
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia, false, PixelSamplingMode.Tent);
            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaTent.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(new PathSamplerFactory(testSzene).Create(SamplerEnum.Lighttracing), testSzene, sampleCount);

            //Tue so, als ob das Bild 3*3 Pixel groß ist. Gesucht ist aber nur die Farbe von dem Pixel in der Mitte
            /*var sampler = new PathSamplerFactory(testSzene).Create(SamplerEnum.Lighttracing);
            PathContributionForEachPathSpace actual = new PathContributionForEachPathSpace();
            int frameCount = 1;
            for (int j = 0; j < frameCount; j++)
            {
                var frameData = PathContributionCalculator.CreateFrameDataWithPhotonmap(testSzene.LightPathSampler, testSzene.PhotonenCount / frameCount, testSzene.rand, testSzene.SizeFactor, sampler.PhotonSettings);

                for (int i = 0; i < sampleCount; i++)
                {
                    for (int x=-1;x <= 1;x++)
                        for (int y=-1;y<=1;y++)
                        {
                            var eyePath = sampler.CreateEyePath ? testSzene.EyePathSampler.SamplePathFromCamera(testSzene.PixX + x, testSzene.PixY + y, testSzene.rand) : null;
                            var lightPath = sampler.CreateLightPath ? testSzene.LightPathSampler.SamplePathFromLighsource(testSzene.rand) : null;

                            List<FullPath> paths = sampler.SamplerMethod.SampleFullPaths(eyePath, lightPath, frameData, testSzene.rand);
                            foreach (var path in paths)
                            {
                                if (path.PixelPosition == null || ((int)path.PixelPosition.X == testSzene.PixX && (int)path.PixelPosition.Y == testSzene.PixY))
                                {
                                    actual.AddEntry(path, path.PathContribution / sampler.SamplerMethod.SampleCountForGivenPath(path) / (sampleCount * frameCount));
                                }

                            }
                        }
                    
                }
            }*/

            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] {"C L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }
    }
}
