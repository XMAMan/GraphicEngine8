using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FullPathGenerator;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfHistogram;
using RayCameraNamespace;
using RaytracingBrdf;
using SubpathGenerator;

namespace FullPathGeneratorTest
{
    //Wenn ein neues Fullpathsampling-Verfahren entwickelt werden soll, dann sollten zwingend folgende Tests in genau dieser Reihenfolge grün gemacht werden:
    //1. Maximal erzeugte Pfadlänge == Rekursionstiefe
    //2. Path-PdfA == PdfA-Summe; PathContribution == Geometryterm und Brdf Sum    
    //3. FullPath.PathPdfA == GetPathPdfAForAGivenPath
    //4. FullPath.PathPdfA == Histogram-PdfA
    //5. PathContribution-Werte für die jeweiligen einzelnen Pfadlängen sind gleich den jeweiligen Contributionwerten von den anderen Verfahren
    //6. MIS-Gewichtete PathContribution-Werte für die jeweiligen einzelnen Pfadlängen sind gleich den jeweiligen MIS-Gewichteten Contributionwerten von den anderen Verfahren
    //7. PathContribution-Summe über alle Pfadlängen mit MIS-Gewicht == PathContribution-Summe mit MIS-Gewicht von den anderen Verfahren
    //8. Firelytest. Minimaler und Maximaler Wert bei den PathPoint-Gewichten/PdfAs

    [TestClass]
    public class CompareTests //Tests für 1,2,3,4,5
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        [Ignore]
        public void CreateExpectedValuesForPathSpaceRadiance_NoMediaEqual()
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Pathtracing);
            string result = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, 200000).ToString();
            File.WriteAllText(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaEqual.txt", result);
        }

        [TestMethod]
        [Ignore]
        public void CreateExpectedValuesForPathSpaceRadiance_WithMediaEqual()
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Pathtracing);
            string result = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, 500000).ToString();
            File.WriteAllText(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt", result);
        }

        [TestMethod]
        [Ignore]
        public void CreateExpectedValuesForPathSpaceRadiance_NoMediaTent()
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia, false, PixelSamplingMode.Tent);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Pathtracing);
            string result = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, 500000).ToString();
            File.WriteAllText(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaTent.txt", result);
        }

        [TestMethod]
        [Ignore]
        public void CreateExpectedValuesForPathSpaceRadiance_WithMediaTent()
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true, PixelSamplingMode.Tent);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Pathtracing);
            string result = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, 500000).ToString();
            File.WriteAllText(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaTent.txt", result);
        }

        [TestMethod]
        public void Compare_MinMaxPathWeightsAndPdfAs()
        {
            var samplerList = GetAllSamplers();

            StringBuilder resultText = new StringBuilder();

            string header =
                string.Format("{0,-30}", "Sampler") + "\t" +
                string.Format("{0,30}", "Min-PathWeight") + "\t" +
                string.Format("{0,30}", "Max-PathWeight") + "\t" +
                string.Format("{0,30}", "Min-PdfA") + "\t" +
                string.Format("{0,30}", "Max-PdfA");
            resultText.AppendLine(header);
            foreach (var sampler in samplerList)
            {
                resultText.AppendLine(string.Format("{0,-30}", sampler.Name) +":" + FirelyTest(sampler.Sampler, sampler.TestSzene));
            }

            File.WriteAllText(WorkingDirectory + "Fullpathsampler_MinMaxPathWeightsAndPdfAs.txt", resultText.ToString());
        }

        private static List<TestSamplerData> GetAllSamplers()
        {
            List<TestSamplerData> returnList = new List<TestSamplerData>();

            var noMediaSzene = new BoxTestScene(PathSamplingType.NoMedia);
            returnList.Add(new TestSamplerData() { Name = "Pathtracing", TestSzene = noMediaSzene, Sampler = new PathSamplerFactory(noMediaSzene).Create(SamplerEnum.Pathtracing) });
            returnList.Add(new TestSamplerData() { Name = "Lighttracing", TestSzene = noMediaSzene, Sampler = new PathSamplerFactory(noMediaSzene).Create(SamplerEnum.Lighttracing) });
            returnList.Add(new TestSamplerData() { Name = "DirectLighting", TestSzene = noMediaSzene, Sampler = new PathSamplerFactory(noMediaSzene).Create(SamplerEnum.DirectLighting) });
            returnList.Add(new TestSamplerData() { Name = "MultipeDirectLighting", TestSzene = noMediaSzene, Sampler = new PathSamplerFactory(noMediaSzene).Create(SamplerEnum.MultipeDirectLighting) });
            returnList.Add(new TestSamplerData() { Name = "VertexConnection", TestSzene = noMediaSzene, Sampler = new PathSamplerFactory(noMediaSzene).Create(SamplerEnum.VertexConnection) });

            var withMediaSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, true);
            returnList.Add(new TestSamplerData() { Name = "MediaPathtracing", TestSzene = withMediaSzene, Sampler = new PathSamplerFactory(withMediaSzene).Create(SamplerEnum.Pathtracing) });
            returnList.Add(new TestSamplerData() { Name = "MediaLighttracing", TestSzene = withMediaSzene, Sampler = new PathSamplerFactory(withMediaSzene).Create(SamplerEnum.Lighttracing) });
            returnList.Add(new TestSamplerData() { Name = "MediaDirectLighting", TestSzene = withMediaSzene, Sampler = new PathSamplerFactory(withMediaSzene).Create(SamplerEnum.DirectLighting) });
            returnList.Add(new TestSamplerData() { Name = "MediaMultipeDirectLighting", TestSzene = withMediaSzene, Sampler = new PathSamplerFactory(withMediaSzene).Create(SamplerEnum.MultipeDirectLighting) });
            returnList.Add(new TestSamplerData() { Name = "MediaVertexConnection", TestSzene = withMediaSzene, Sampler = new PathSamplerFactory(withMediaSzene).Create(SamplerEnum.VertexConnection) });

            return returnList;
        }

        //Zum Testen von ein einzelnen Sampler
        class TestSamplerData
        {
            public Sampler Sampler;
            public BoxTestScene TestSzene;
            public string Name;
        }

        //Firelytest. Minimaler und Maximaler Wert bei den PathPoint-Gewichten/PdfAs
        private static string FirelyTest(Sampler sampler, BoxTestScene testSzene, int sampleCount = 10000)
        {
            List<float> pathPointWeights = new List<float>();
            List<double> pathPointPdfAs = new List<double>();

            var frameData = PathContributionCalculator.CreateFrameDataWithPhotonmap(testSzene.LightPathSampler, testSzene.PhotonenCount, testSzene.rand, testSzene.SizeFactor, sampler.PhotonSettings);

            for (int i = 0; i < sampleCount; i++)
            {
                var eyePath = sampler.CreateEyePath ? testSzene.EyePathSampler.SamplePathFromCamera(testSzene.PixX, testSzene.PixY, testSzene.rand) : null;
                var lightPath = sampler.CreateLightPath ? testSzene.LightPathSampler.SamplePathFromLighsource(testSzene.rand) : null;
                //var frameData = sampler.CreatePhotonMap ? testSzene.CreateFrameDataWithPhotonmap() : null;

                List<FullPath> paths = sampler.SamplerMethod.SampleFullPaths(eyePath, lightPath, frameData, testSzene.rand);
                foreach (var path in paths)
                {
                    for (int j = 0; j < path.Points.Length; j++)
                    {
                        Vector3D pathWeight = path.Points[j].Point.PathWeight;
                        if (pathWeight != null) pathPointWeights.Add(pathWeight.X);
                        pathPointPdfAs.Add(path.Points[j].EyePdfA);
                        pathPointPdfAs.Add(path.Points[j].LightPdfA);
                    }
                }
            }

            var orderedPointWeights = pathPointWeights.OrderBy(x => x).ToList();
            var orderedPdfAs = pathPointPdfAs.OrderBy(x => x).ToList();

            //Min-PathWeight - Max-PathWeight - Min-PdfA - Max-PdfA
            return
                string.Format("{0,30}", orderedPointWeights.First()) + "\t" +
                string.Format("{0,30}", orderedPointWeights.Last()) + "\t" +
                string.Format("{0,30}", orderedPdfAs.First()) + "\t" +
                string.Format("{0,30}", orderedPdfAs.Last());
        }
    }
}
