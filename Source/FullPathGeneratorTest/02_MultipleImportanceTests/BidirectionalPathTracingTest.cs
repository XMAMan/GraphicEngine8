using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RayCameraNamespace;
using SubpathGenerator;

namespace FullPathGeneratorTest.MultipleImportanceTests
{
    [TestClass]
    public class BidirectionalPathTracingTest //Tests für 6 (Alle FullPath-Sampling-Verfahren in Mis-Gewichteter Summe)
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void SampleFullPathsBTPT1_NoMedia_MisWeightedRadianceForEachPathSpaceIsOk() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia, false, PixelSamplingMode.Tent);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, BTPTSettings());


            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaTent.txt"); //expected.SumOverAllPathSpaces().X = 757.3781
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck);

            //File.WriteAllText(WorkingDirectory + "Fullpathsampler_PathContributionWithMIS_NoMedia.txt", actual.ToString());

            //File.WriteAllText(WorkingDirectory + "Fullpaths1.txt", fullPathSampler.pixelAnalyser.GetOverview());

            float sumActual = actual.SumOverAllPathSpaces().X;

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
            Assert.AreEqual(761.8037f, sumActual); //Ohne MultipeDirectLighting
        }

        [TestMethod]
        public void SampleFullPathsBTPT2_NoMedia_MisWeightedRadianceForEachPathSpaceIsOk() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia, false, PixelSamplingMode.Tent);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,
                UseDirectLighting = true,
                UseVertexConnection = true,                
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaTent.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck);

            //File.WriteAllText(WorkingDirectory + "Fullpathsampler_PathContributionWithMIS_NoMedia.txt", actual.ToString());

            //File.WriteAllText(WorkingDirectory + "Fullpaths1.txt", fullPathSampler.pixelAnalyser.GetOverview());

            float sumActual = actual.SumOverAllPathSpaces().X;

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
            Assert.AreEqual(759.643066f, sumActual); //Ohne MultipeDirectLighting; Ohne Lighttracing
        }

        [TestMethod]
        public void SampleFullPathsBTPT_WithMedia_MisWeightedRadianceForEachPathSpaceIsOk() //Test 6 (Alle Verfahren als Summe mit MIS-Gewicht)
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true, PixelSamplingMode.Tent);
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, BTPTSettings());

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaTent.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, testSzene, testSzene.SamplecountForPathContributionCheck);

            //File.WriteAllText(WorkingDirectory + "Fullpathsampler_PathContributionWithMIS_WithMedia.txt", actual.ToString());

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        private FullPathSettings BTPTSettings()
        {
            return new FullPathSettings()
            {
                UsePathTracing = true,
                UseLightTracing = true,
                UseDirectLighting = true,
                //UseMultipleDirectLighting = true,
                UseVertexConnection = true,     
            };
        }
    }
}
