using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfHistogram;
using SubpathGenerator;
using System;
using System.Collections.Generic;

namespace FullPathGeneratorTest.BasicTests
{
    [TestClass]
    public class VertexMergingTest //Tests für 1,2,3,4,5
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        //[Ignore] //In der PathWeight und der PdfA-Formel landet der Light-Merging-Punkt an Stelle x und in der 
                 //Geometry-,-PdfA-Kontrollpunktfunktion liegt der LightMerging-Punkt an der Stelle x + Suchradius
                 //Durch diese Verschiebung kommt es im Geometryterm zwischen dem LightMerging-Punkt und dessen Vorgänger zu
                 //Differenzen. Die maximale Differenz hängt vom Verhältniss zwischen dem Suchradius und der maximalen Punkt-zu-Punkt
                 //Distanz in der jeweiligen Szene ab. Deswegen verwende ich für den GeometryTerm-Error und PdfA-Error hier eine 1
        public void A_PathLengthOK_PathPdfA_PathContribution() //Test 1,2
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexMerging);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene);
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=8,28323251424789E-06}
        }

        [TestMethod]
        //[Ignore] //Ähnlich wie beim VertexMerging gibt es für alle Pfade die Lä
        public void B_PathPdfAMatchWithFunctionAndHistogramPdfA() //Test 3,4
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexMerging);
            var result = PathPdfAHistogram.ComparePathPdfAWithFunctionPdfAAndHistogram(method, testSzene, testSzene.SampleCountForPathPdfACheck);
            result.Image.Save(WorkingDirectory + "VertexMerging.bmp");
            Assert.IsTrue(result.MaxErrorUptoGivenPathLength(4) < 12, "Error=" + result.MaxErrorUptoGivenPathLength(4));
        }

        //Untersuchung: Wie viel Photonen pro Frame braucht man im optimalfall? Wenn ich Photonmap hab, ab wann bringt es nichts mehr noch weitere Samples daraus zu generieren?
        //Erkentniss bis jetzt: Mit fortschreitender Sampling-Anzahl nimmt den Fehler immer weiter bis zu ein gewissen Punkt
        //ab, und ab da nicht weiter. Diesen Fehlerrestwert nenne ich Bias. Der Bias hängt vom Suchradius und der Photonmapgröße ab.
        //Die Photonmap muss eine gewisse Mindestgröße haben, um ein vertretbaren kleinen Fehler zu haben.
        //Komischerweise gibt es kein perfekt lineren Zusammenhang zwischen der Photonmapgröße und dem Fehler/Benötigte SampleCount
        [TestMethod]
        [Ignore]
        public void ConvergenzTest() //Stellt den Zusammenhang zwischen Sampling-Anzahl und Fehler dar
        {
            int sampleCount = 2000 * 20;

            //Weg 1: Für eine bestimmte Photonmap zeigen, wie sich der Fehler über die SampleCount entwickelt
            //int photonenCount1 = 7000;
            //List<float> errorValues = GetErrorValues(sampleCount, photonenCount1);
            //string errorText = ((int)(errorValues.GetRange(errorValues.Count / 2, errorValues.Count / 2).Average())).ToString();

            //Weg 2: Zusammenhang zwischen Photonmap-Auflösung und Photonmap-Bias zeigen
            //List<float> errorValues = new List<float>();
            //for (int photonenCount = 7000; photonenCount < 14000; photonenCount+=100)
            //{
            //    List<float> radianceErrors = GetErrorValues(sampleCount, photonenCount, 0, float.MaxValue);
            //    float biasError = radianceErrors.GetRange(radianceErrors.Count / 2, radianceErrors.Count / 2).Average();
            //    errorValues.Add(biasError); //Bei einer Photonmapauflösung von 'photonenCount' erhalte ich biasError als Bias-Fehlerwert 
            //}
            //string errorText = null;
            //FunctionPlotter.PlotFloatArray(errorValues.ToArray(), 0.04f, 50, "Error=" + errorText).Save(WorkingDirectory + "VertexMerging-ErrorCurve.bmp");

            //Weg 3: Schaue für jede Photonmapauflösung, wie viel Samples es braucht, damit der Fehler kleiner 10 ist
            List<float> errorValues = new List<float>();
            for (int photonenCount = 9000; photonenCount < 16000; photonenCount += 100)
            {
                List<float> radianceErrors = GetErrorValues(sampleCount, photonenCount, 1000, 5);
                errorValues.Add(radianceErrors.Count); //Bei einer Photonmapauflösung von 'photonenCount' brauche ich radianceErrors.Count Samples, damit der Fehler kleiner 10 ist 
            }

            FunctionPlotter.PlotFloatArray(errorValues.ToArray(), 0.04f, "Samples/Size" ).Save(WorkingDirectory + "VertexMerging-ErrorCurve.bmp");
        }

        //Gibt den Radiance-Abweichungswert über die SampleCount zurück
        //Es wird der Durchschnittsfehlerwert der letzten 'lastN' Samples genommen und geschaut, ob er kleiner als maxErrorForStop ist. Wenn ja, dann erfolgt schon eher ein Abbruch. 
        private List<float> GetErrorValues(int sampleCount, int photonenCount, int lastN, float maxErrorForStop)
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var sampler = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexMerging);

            float expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaEqual.txt").SumOverAllPathSpaces().X;

            var frameData = PathContributionCalculator.CreateFrameDataWithPhotonmap(testSzene.LightPathSampler, photonenCount, testSzene.rand, testSzene.SizeFactor, new PhotonmapSettings() { CreateSurfaceMap = true });

            List<float> errorValues = new List<float>();
            double runningSum = 0;
            double sumFromLastNErrorValues = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                var eyePath = sampler.CreateEyePath ? testSzene.EyePathSampler.SamplePathFromCamera(testSzene.PixX, testSzene.PixY, testSzene.rand) : null;
                var lightPath = sampler.CreateLightPath ? testSzene.LightPathSampler.SamplePathFromLighsource(testSzene.rand) : null;
                //var frameData = sampler.CreatePhotonMap ? testSzene.CreateFrameDataWithPhotonmap() : null;
                //float kernelFunction = frameData != null ? frameData.PhotonMaps.GlobalSurfacePhotonmap.KernelFunction(0, frameData.PhotonMaps.GlobalSurfacePhotonmap.SearchRadius) : 1;
                List<FullPath> paths = sampler.SamplerMethod.SampleFullPaths(eyePath, lightPath, frameData, testSzene.rand);

                float sampleSum = 0;
                foreach (var path in paths)
                {
                    if (path.PixelPosition == null || ((int)path.PixelPosition.X == testSzene.PixX && (int)path.PixelPosition.Y == testSzene.PixY))
                    {
                        float sample = path.PathContribution.X / sampler.SamplerMethod.SampleCountForGivenPath(path);
                        sampleSum += sample;
                    }
                }
                runningSum += sampleSum;
                double actualValue = runningSum / (i + 1);
                double error = Math.Abs(actualValue - expected);
                errorValues.Add((float)error);

                if (lastN > 0)
                {
                    sumFromLastNErrorValues += error;
                    int removeIndex = errorValues.Count - 1 - lastN;
                    if (removeIndex >= 0)
                    {
                        sumFromLastNErrorValues -= errorValues[removeIndex];
                        double lastNAvg = sumFromLastNErrorValues / lastN;
                        if (lastNAvg < maxErrorForStop) break;
                    }                    
                }                
            }

            return errorValues;
        }

        [TestMethod]
        //[Ignore]
        public void C_NoMedia_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            var testSzene = new BoxTestScene(PathSamplingType.NoMedia);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexMerging);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceNoMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck*2);

            string compare = expected.CompareWithOther(actual);

            string error = expected.CompareAllPathsWithOther(actual, testSzene.maxContributionError);
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void C_MediaNoDistance_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaWithoutDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexMerging);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck * 2);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }

        [TestMethod]
        public void C_MediaLongRays_PathContributionSumForEachPathLengthCheck() //Test 5
        {
            var testSzene = new BoxTestScene(PathSamplingType.ParticipatingMediaLongRayWithDistanceSampling, true);
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexMerging);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceWithMediaEqual.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck * 2);

            string compare = expected.CompareWithOther(actual);
            string compareArray = expected.GetCompareArray(actual);

            string error = expected.CompareOnlyProvidedPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C D L", "C D D L", "C D D D L", "C D D D D L", "C D D D D D L" });
            Assert.IsTrue(string.IsNullOrEmpty(error), error, error);
        }
    }
}
