using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphicGlobal;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using ParticipatingMedia.DistanceSampling;
using ParticipatingMedia.Media;
using ParticipatingMedia.PhaseFunctions;
using PdfHistogram;
using RaytracingRandom;

namespace ParticipatingMediaTest
{
    [TestClass]
    public class IDistanceSamplerTest
    {
        private static string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        private const float startXPosition = 0;
        private const float maxSampleDistance = 10;
        private const int maxSampleDistanceError = 17; //Zahle zwischen 0 und 100

        //Das Glas-Medium bei den grünen Topf bei Stilllife enthält keine Scatterpartikel sondern nur Absorbationspartikel. In so ein Fall
        //soll Medium ohne Distanzsampling durchlaufen werden
        [TestMethod]
        public void SampleDistance1_NoScatteringParticleAvailable_NoDistanceIsSampled()
        {
            var sut = new HomogenDistanceSampler(new Vector3D(0.1f, 0, 0), false);
            var result = sut.SampleRayPositionWithPdfFromRayMinToInfinity(new Ray(null, null), 0, 10, null, false);
            Assert.AreEqual(1, result.PdfL);
            Assert.AreEqual(1, result.ReversePdfL);
            Assert.AreEqual(10, result.RayPosition);
        }

        [TestMethod]
        public void SampleDistance2_NoScatteringParticleAvailable_NoDistanceIsSampled()
        {
            var sut = new HomogenDistanceSampler(new Vector3D(0.1f, 0, 0), false);
            var result = sut.SampleRayPositionWithPdfFromRayMinToRayMax(new Ray(null, null), 0, 10, null);
            Assert.AreEqual(1, result.PdfL);
            Assert.AreEqual(10, result.RayPosition);
        }

        [TestMethod]
        public void SampleDistanceToInfinity_HomogonMedia_PdfMatchWithHistogram()
        {
            var sut = new HomogenDistanceSampler(new Vector3D(0.1f, 0, 0), true);
            Ray ray = new Ray(new Vector3D(startXPosition, 0, 0), new Vector3D(1, 0, 0));
            var histogram = SampleDistanceToInfinity_CalledMultipeTimes_MatchWithHistogram(sut, ray, maxSampleDistance);

            List<FunctionToPlot> functions = new List<FunctionToPlot>();
            functions.Add(new FunctionToPlot() { Function = histogram.GetPdfFunctionFromHistogram(), Color = Color.Blue, Text = "Histogram" });
            var pdfLFunction = new SimpleFunction((x) =>
            {
                return sut.GetSamplePdfFromRayMinToInfinity(ray, 0, maxSampleDistance, (float)x, true, x < maxSampleDistance).PdfL;
            });
            functions.Add(new FunctionToPlot()
            {
                Function = pdfLFunction,
                Color = Color.Red,
                Text = "PdfL"
            });
            functions.Add(new FunctionToPlot()
            {
                Function = (x) =>
                {
                    float minPositiveAttenuationCoeffizent = 0.1f;
                    return (float)Math.Exp(-minPositiveAttenuationCoeffizent * x);
                },
                Color = Color.Green,
                Text = "Attenuation"
            });

            FunctionPlotter plotter = new FunctionPlotter(new RectangleF(-1, -1, maxSampleDistance + 2, 3), 0, maxSampleDistance, new Size(400, 300));
            int error = (int)FunctionComparer.GetMaxDifferenceAreaBetweenFunctions(0, maxSampleDistance, histogram.GetPdfFunctionFromHistogram(), histogram.GetPdfFunctionFromPdfProperty(), pdfLFunction);
            plotter.PlotFunctions(functions, "Error=" + error).Save(WorkingDirectory + "HomogenDistanceSamplingToInfinity.bmp");
            Assert.IsTrue(error < maxSampleDistanceError, "Error=" + error);
        }

        [TestMethod]
        public void SampleDistanceToRayMax_HomogonMedia_PdfMatchWithHistogram()
        {
            var sut = new HomogenDistanceSampler(new Vector3D(0.1f, 0, 0), true);
            Ray ray = new Ray(new Vector3D(startXPosition, 0, 0), new Vector3D(1, 0, 0));
            var histogram = SampleDistanceToRayMax_CalledMultipeTimes_MatchWithHistogram(sut, ray, maxSampleDistance);

            List<FunctionToPlot> functions = new List<FunctionToPlot>();
            functions.Add(new FunctionToPlot() { Function = histogram.GetPdfFunctionFromHistogram(), Color = Color.Blue, Text = "Histogram" });
            functions.Add(new FunctionToPlot() { Function = histogram.GetPdfFunctionFromPdfProperty(), Color = Color.Yellow, Text = "SamplePdfL" });

            var pdfLFunction = new SimpleFunction((x) =>
            {
                //float minPositiveAttenuationCoeffizent = 0.1f;
                //double cdfSMax = 1 - Math.Exp(-minPositiveAttenuationCoeffizent * maxSampleDistance);
                //return minPositiveAttenuationCoeffizent * (float)(Math.Exp(-minPositiveAttenuationCoeffizent * x) / cdfSMax);
                return sut.GetSamplePdfFromRayMinToRayMax(ray, 0, maxSampleDistance, (float)x).PdfL;
            });
            functions.Add(new FunctionToPlot()
            {
                Function = pdfLFunction,
                Color = Color.Red,
                Text = "FunctionPdfL"
            });
            functions.Add(new FunctionToPlot()
            {
                Function = (x) =>
                {
                    float minPositiveAttenuationCoeffizent = 0.1f;
                    return (float)Math.Exp(-minPositiveAttenuationCoeffizent * x);
                },
                Color = Color.Green,
                Text = "Attenuation"
            });

            FunctionPlotter plotter = new FunctionPlotter(new RectangleF(-1, -1, maxSampleDistance + 2, 3), 0, maxSampleDistance, new Size(400, 300));

            int error = (int)FunctionComparer.GetMaxDifferenceAreaBetweenFunctions(0, maxSampleDistance, histogram.GetPdfFunctionFromHistogram(), histogram.GetPdfFunctionFromPdfProperty(), pdfLFunction);
            plotter.PlotFunctions(functions, "Error=" + error).Save(WorkingDirectory + "HomogenDistanceSamplingInMinMaxRange.bmp");
            Assert.IsTrue(error < maxSampleDistanceError, "Error=" + error);
        }

        [TestMethod]
        public void SampleDistanceToInfinity_RayMarching_PdfMatchWithHistogram()
        {
            var sut = new RayMarchingDistanceSampler(new SkyMediaOnWaveLength());
            var d = new DescriptionForSkyMedia();
            var result = SampleDistanceToInfinity_CalledMultipeTimes_MatchWithHistogram(sut, new Ray(new Vector3D(d.EarthRadius, 0, 0), new Vector3D(1, 0, 0)), (d.AtmosphereRadius - d.EarthRadius)).GetResult();
            Assert.IsTrue(result.MaxError < maxSampleDistanceError, "Error = " + result.MaxError + "; Max-Error = " + maxSampleDistanceError);
        }

        [TestMethod]
        public void SampleDistanceToInfinity_WoodCockTracking_PdfMatchWithHistogram()
        {
            var d = new DescriptionForSkyMedia();

            //Ray ray = new Ray(new Vector3D(d.EarthRadius, 0, 0), new Vector3D(1, 0, 0));
            //float rayMax = (d.AtmosphereRadius - d.EarthRadius) / 200;

            //Ray ray = new Ray(new Vector3D(d.AtmosphereRadius, 0, 0), new Vector3D(-1, 0, 0));
            //float rayMax = (d.AtmosphereRadius - d.EarthRadius) / 100;

            //Diese Werte werden beim FullpathSampler ohne Distancesampling verwendet
            Ray ray = new Ray(new Vector3D(0f, 6360100f, 0f), new Vector3D(0.482398868f, 0.875943303f, 0.0038151287f));
            float rayMax = 6828.69453f;

            var skyMedia = new SkyMediaOnWaveLength();
            var sut = new WoodCockTrackingDistanceSampler(skyMedia);
            var histogram = SampleDistanceToInfinity_CalledMultipeTimes_MatchWithHistogram(sut, ray, rayMax);

            List<FunctionToPlot> functions = new List<FunctionToPlot>();

            functions.Add(new FunctionToPlot() { Function = histogram.GetPdfFunctionFromHistogram(), Color = Color.Blue, Text = "Histogram" });
            //functions.Add(new FunctionToPlot() { Function = histogram.GetPdfFunctionFromPdfProperty(), Color = Color.Red, Text = "PdfL" });


            double attenuationIntegral = FunctionIntegrator.IntegrateWithRieman((x) => { return skyMedia.EvaluateAttenuationOnWave(ray, 0, (float)x); }, 0, (d.AtmosphereRadius - d.EarthRadius));
            functions.Add(new FunctionToPlot()
            {
                Function = (x) =>
                {
                    return skyMedia.EvaluateAttenuationOnWave(ray, 0, (float)x) / attenuationIntegral;
                },
                Color = Color.Green,
                Text = "Attenuation"
            });

            var pdfLFunction = new SimpleFunction((x) =>
            {
                return sut.GetSamplePdfFromRayMinToInfinity(ray, 0, rayMax, (float)x, true, x < rayMax).PdfL;
            });
            functions.Add(new FunctionToPlot()
            {
                Function = pdfLFunction,
                Color = Color.Red,
                Text = "PdfL"
            });

            //FunctionPlotter plotter = new FunctionPlotter(new RectangleF(-50, 0.0001f, rayMax + 100, 0.0002f), 0, rayMax, new Size(400, 300));
            FunctionPlotter plotter = new FunctionPlotter(0, rayMax - 1, new Size(400, 300));

            int error = (int)FunctionComparer.GetMaxDifferenceAreaBetweenFunctions(0, rayMax - 1, histogram.GetPdfFunctionFromHistogram(), histogram.GetPdfFunctionFromPdfProperty(), pdfLFunction);
            plotter.PlotFunctions(functions, "Error=" + error).Save(WorkingDirectory + "WoodCockTrackingDistanceSamplingToInfinity.bmp");
            Assert.IsTrue(error < maxSampleDistanceError, "Error=" + error);
        }

        [TestMethod] //Ziel: Transmission-Funktion soll wie eine Cosinus-Kurve aussehen
        public void PlotTransmissionFunction()
        {
            FunctionPlotter plotter = new FunctionPlotter(new RectangleF(0, -3, 10, 6), 0, 10, new Size(400, 300));
            Bitmap result = plotter.PlotFunctions(
                new List<FunctionToPlot>()
                {
                    new FunctionToPlot()
                    {
                        Color = Color.Blue,
                        Function = (t)=>
                        {
                            return Math.Exp(-FunctionIntegrator.IntegrateWithRieman((tI) => { return -1 / Math.Cos(tI); }, 0, t));
                        }
                    }
                }
            );

            result.Save(WorkingDirectory + "TransmissionWithCos.bmp");
        }

        [TestMethod]
        [Ignore] //Das muss ich noch implementieren. Das geht momentan noch nicht
        public void SampleDistanceToRayMax_WoodCockTracking_PdfMatchWithHistogram()
        {
            var d = new DescriptionForSkyMedia();

            //Geht
            //Ray ray = new Ray(new Vector3D(d.EarthRadius, 0, 0), new Vector3D(1, 0, 0));
            //float rayMax = (d.AtmosphereRadius - d.EarthRadius) / 200;

            //Geht
            //Ray ray = new Ray(new Vector3D(d.AtmosphereRadius, 0, 0), new Vector3D(-1, 0, 0));
            //float rayMax = (d.AtmosphereRadius - d.EarthRadius) / 100;

            //Diese Werte werden beim FullpathSampler ohne Distancesampling verwendet
            Ray ray = new Ray(new Vector3D(0f, 6360100f, 0f), new Vector3D(0.482398868f, 0.875943303f, 0.0038151287f));
            float rayMax = 68286.9453f;


            var skyMedia = new SkyMediaOnWaveLength();
            var sut = new WoodCockTrackingDistanceSampler(skyMedia);
            var histogram = SampleDistanceToRayMax_CalledMultipeTimes_MatchWithHistogram(sut, ray, rayMax);

            List<FunctionToPlot> functions = new List<FunctionToPlot>();

            functions.Add(new FunctionToPlot() { Function = histogram.GetPdfFunctionFromHistogram(), Color = Color.Blue, Text = "Histogram" });
            functions.Add(new FunctionToPlot() { Function = histogram.GetPdfFunctionFromPdfProperty(), Color = Color.Yellow, Text = "SamplePdfL" });


            double attenuationIntegral = FunctionIntegrator.IntegrateWithRieman((x) => { return skyMedia.EvaluateAttenuationOnWave(ray, 0, (float)x); }, 0, rayMax);
            functions.Add(new FunctionToPlot()
            {
                Function = (x) =>
                {
                    return skyMedia.EvaluateAttenuationOnWave(ray, 0, (float)x) / attenuationIntegral;
                },
                Color = Color.Green,
                Text = "Attenuation"
            });

            var pdfLFunction = new SimpleFunction((x) =>
            {
                return sut.GetSamplePdfFromRayMinToRayMax(ray, 0, rayMax, (float)x).PdfL;
            });
            functions.Add(new FunctionToPlot()
            {
                Function = pdfLFunction,
                Color = Color.Red,
                Text = "FunctionPdfL"
            });

            //FunctionPlotter plotter = new FunctionPlotter(new RectangleF(-50, 0.01f, rayMax + 100, 0.02f), 0, rayMax, new Size(400, 300));
            FunctionPlotter plotter = new FunctionPlotter(0, rayMax, new Size(400, 300));

            int error = (int)FunctionComparer.GetMaxDifferenceAreaBetweenFunctions(0, rayMax, histogram.GetPdfFunctionFromHistogram(), histogram.GetPdfFunctionFromPdfProperty(), pdfLFunction);
            plotter.PlotFunctions(functions, "Error=" + error).Save(WorkingDirectory + "WoodCockTrackingDistanceSamplingInMinMaxRange.bmp");
            Assert.IsTrue(error < maxSampleDistanceError, "Error=" + error);
        }

        class SkyMediaOnWaveLength : IMediaOnWaveLength
        {
            private DescriptionForSkyMedia mediaDescription = new DescriptionForSkyMedia();
            private SkyIntegrator skyIntegrator;

            public Vector3D MaxExtinctionCoeffizient { get; private set; }
            public SkyMediaOnWaveLength()
            {
                this.MaxExtinctionCoeffizient = mediaDescription.RayleighScatteringCoeffizientOnSeaLevel;
                this.skyIntegrator = new SkyIntegrator(new LayerOfAirDescription()
                {
                    EarthCenter = this.mediaDescription.CenterOfEarth,
                    EarthRadius = this.mediaDescription.EarthRadius,
                    AtmosphereRadius = this.mediaDescription.AtmosphereRadius,
                    ScaleHeigh = this.mediaDescription.RayleighScaleHeight,
                });
            }

            public float ExtinctionCoeffizientOnWave(Vector3D position)
            {
                float height = (position - this.mediaDescription.CenterOfEarth).Length() - this.mediaDescription.EarthRadius;
                float expFactor = (float)Math.Exp(-height / this.mediaDescription.RayleighScaleHeight);
                return this.mediaDescription.RayleighScatteringCoeffizientOnSeaLevel.Z * expFactor;
            }

            public float EvaluateAttenuationOnWave(Ray ray, float rayMin, float rayMax)
            {
                double opticalDepth = this.mediaDescription.RayleighScatteringCoeffizientOnSeaLevel.Z * this.skyIntegrator.GetIntegralFromLine(ray, rayMin, rayMax);
                return (float)Math.Exp(-opticalDepth);
            }
        }

        [TestMethod]
        public void SampleDistanceToInfinity_VacuumMedia_PdfMatchWithHistogram()
        {
            var sut = new VacuumDistanceSampler();
            var result = SampleDistanceToInfinity_CalledMultipeTimes_MatchWithHistogram(sut, new Ray(new Vector3D(startXPosition, 0, 0), new Vector3D(1, 0, 0)), maxSampleDistance).GetResult();
            Assert.IsTrue(result.MaxError < maxSampleDistanceError, "Error = " + result.MaxError + "; Max-Error = " + maxSampleDistanceError);
        }

        private static DistanceHistogram SampleDistanceToInfinity_CalledMultipeTimes_MatchWithHistogram(IDistanceSampler sut, Ray ray, float maxDistance)
        {
            IRandom rand = new Rand(0);
            int chunkCount = 50;
            int sampleCount = 100000;
            DistanceHistogram histogram = new DistanceHistogram(0, maxDistance, chunkCount);
            for (int i = 0; i < sampleCount; i++)
            {
                histogram.AddSample(sut.SampleRayPositionWithPdfFromRayMinToInfinity(ray, 0, maxDistance, rand, false));
            }

            return histogram;
        }

        private static SimpleFunctionHistogram SampleDistanceToRayMax_CalledMultipeTimes_MatchWithHistogram(IDistanceSampler sut, Ray ray, float maxDistance)
        {
            IRandom rand = new Rand(0);
            int chunkCount = 50;
            int sampleCount = 100000;
            SimpleFunctionHistogram histogram = new SimpleFunctionHistogram(0, maxDistance, chunkCount);
            for (int i = 0; i < sampleCount; i++)
            {
                var sample = sut.SampleRayPositionWithPdfFromRayMinToRayMax(ray, 0, maxDistance, rand);
                histogram.AddSample(sample.RayPosition, sample.PdfL);
            }

            return histogram;
        }
    }
}
