using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullPathGeneratorTest
{
    [TestClass]
    public class SkyTests
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        private int SamplecountForPathContributionCheck = 40000;
        private float MaxContributionError = 0.1f;

        [TestMethod]
        [Ignore]
        public void CreateExpectedValuesForSkyTests()
        {
            SkyTestSzene data = new SkyTestSzene(SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling);
            FullPathSampler fullPathSampler = new FullPathSampler(data.FullPathKonstruktorData, new FullPathSettings()
            {
                UsePathTracing = true,
                UseLightTracing = true,
                UseDirectLighting = true,
                //UseMultipleDirectLighting = true,
                UseVertexConnection = true,
            });

            var result = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, data, 2000000).ToString();

            File.WriteAllText(WorkingDirectory + "ExpectedValues\\ExpectedValuesForSkyPathSpace.txt", result);
        }

        [TestMethod]
        public void SampleFullPaths_DirectLightingOnEdge_RadianceForEachPathSpaceCheck()
        {
            //Multiple Scattering
            SkyTestSzene data = new SkyTestSzene(SubpathGenerator.PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling);
            FullPathSampler fullPathSampler = new FullPathSampler(data.FullPathKonstruktorData, new FullPathSettings()
            {
                UseDirectLightingOnEdge = true,
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForSkyPathSpace.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, data, SamplecountForPathContributionCheck);

            string error = expected.CompareOnlyProvidedPathsWithOther(actual, MaxContributionError, new string[] { "C P L", "C P P L", "C P D P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void SampleFullPaths_SingleScattering_RadianceForEachPathSpaceCheck()
        {
            SkyTestSzene data = new SkyTestSzene(SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling);
            FullPathSampler fullPathSampler = new FullPathSampler(data.FullPathKonstruktorData, new FullPathSettings()
            {
                UseDirectLightingOnEdge = true
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForSkyPathSpace.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, data, SamplecountForPathContributionCheck);

            string error = expected.CompareOnlyProvidedPathsWithOther(actual, MaxContributionError, new string[] { "C P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void SampleFullPaths_SingleScatteringBiased_RadianceForEachPathSpaceCheck()
        {
            SkyTestSzene data = new SkyTestSzene(SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling);
            FullPathSampler fullPathSampler = new FullPathSampler(data.FullPathKonstruktorData, new FullPathSettings()
            {
                UseDirectLightingOnEdge = true,
                UseSegmentSamplingForDirectLightingOnEdge = false, //Hier kommt der Bias rein
            });

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForSkyPathSpace.txt");
            var actual = PathContributionCalculator.GetMisWeightedPathContributionForEachPathSpace(fullPathSampler, data, 1);

            string error = expected.CompareOnlyProvidedPathsWithOther(actual, MaxContributionError, new string[] { "C P L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }
    }
}
