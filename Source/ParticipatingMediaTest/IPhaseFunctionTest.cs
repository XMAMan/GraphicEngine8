using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphicGlobal;
using GraphicGlobal.MathHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia.PhaseFunctions;
using PdfHistogram;
using RaytracingRandom;
using UnitTestHelper;

namespace ParticipatingMediaTest
{
    //Testfälle:
    //-Brdf-Integal ist 1 (Energieerhaltungssatz)
    //-Function-Pdf match with Histogram-Pdf

    [TestClass]
    public class IPhaseFunctionTest
    {
        private double maxBrdfError = 0.01f;
        private double maxPdfError = 3;

        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void GetBrdf_Isotrophic_SphereIntegralIsOne()
        {
            double error = EnergyConservationTest(new IsotrophicPhaseFunction(), 100000);
            Assert.IsTrue(error < maxBrdfError, "Error=" + error);
        }

        [TestMethod]
        public void GetBrdf_Anisotropic_SphereIntegralIsOne()
        {
            double error = EnergyConservationTest(new AnisotropicPhaseFunction(0.76f, 1), 500000);
            Assert.IsTrue(error < maxBrdfError, "Error=" + error);
        }

        [TestMethod]
        public void GetBrdf_RayleighPhase_SphereIntegralIsOne()
        {
            double error = EnergyConservationTest(new RayleighPhaseFunction(), 100000);
            Assert.IsTrue(error < maxBrdfError, "Error=" + error);
        }

        [TestMethod]
        public void GetBrdf_Mie_SphereIntegralIsOne()
        {
            double error = EnergyConservationTest(new MiePhaseFunction(0.76f), 100000);
            Assert.IsTrue(error < maxBrdfError, "Error=" + error);
        }

        [TestMethod]
        public void SampleDirection_Isotrophic_FunctionPdfMatchWithHistogram()
        {
            FunctionPdfMatchHistogramPdfTest(new IsotrophicPhaseFunction(), 100000, out int error).Save(WorkingDirectory + "IsotrophicPhase.bmp");
            Assert.IsTrue(error < maxPdfError, "Error=" + error);
        }

        [TestMethod]
        public void SampleDirection_Anisotropic_FunctionPdfMatchWithHistogram()
        {
            FunctionPdfMatchHistogramPdfTest(new AnisotropicPhaseFunction(0.76f, 1), 100000, out int error).Save(WorkingDirectory + "AnisotropicPhase.bmp");
            Assert.IsTrue(error < maxPdfError, "Error=" + error);
        }

        [TestMethod]
        public void SampleDirection_Rayleigh_FunctionPdfMatchWithHistogram()
        {
            FunctionPdfMatchHistogramPdfTest(new RayleighPhaseFunction(), 1000000 *10, out int error).Save(WorkingDirectory + "RayleighPhase.bmp");
            Assert.IsTrue(error < maxPdfError, "Error=" + error);
        }

        [TestMethod]
        public void SampleDirection_Rayleigh_CreatePolarImage()
        {
            float maxRayleighFunctionValue = (float)(3 / (8 * Math.PI));
            CreatePolarPlotImageFromPhaseFunction(new RayleighPhaseFunction(), 1000, maxRayleighFunctionValue).Save(WorkingDirectory + "Rayleigh-PhaseFunction.bmp");
        }

        [TestMethod]
        public void SampleDirection_Mie_FunctionPdfMatchWithHistogram()
        {
            FunctionPdfMatchHistogramPdfTest(new MiePhaseFunction(0.76f), 1000000, out int error).Save(WorkingDirectory + "MiePhase.bmp");
            Assert.IsTrue(error < maxPdfError, "Error=" + error);
        }

        [TestMethod]
        public void SampleDirection_Mie_CreatePolarImage()
        {
            float maxRayleighFunctionValue = 3;
            CreatePolarPlotImageFromPhaseFunction(new MiePhaseFunction(0.76f), 1000, maxRayleighFunctionValue).Save(WorkingDirectory + "Mie-PhaseFunction.bmp");
        }

        private static double EnergyConservationTest(IPhaseFunction phaseFunction, int sampleCount)
        {
            Vector3D directionToMediaPoint = new Vector3D(1, 0, 0);
            Vector3D mediaPoint = new Vector3D(0, 0, 0);

            double integral = SphereIntegrator.IntegrateWithMonteCarlo((wo, phiP, thetaP) =>
            {
                return phaseFunction.GetBrdf(directionToMediaPoint, mediaPoint, wo).Brdf;
            }, 0, 360, 0, 180, sampleCount);

            return Math.Abs(integral - 1);
        }

        private static Bitmap FunctionPdfMatchHistogramPdfTest(IPhaseFunction sut, int sampleCount, out int error)
        {
            int histogramChunkCount = 40;

            Vector3D mediaPoint = new Vector3D(0, 0, 0);
            Vector3D directionToMediaPoint = new Vector3D(0, 0, 1);

            IRandom rand = new Rand(0);
            DirectionHistogram histogram = new DirectionHistogram(12, histogramChunkCount, directionToMediaPoint);
            
            for (int i = 0; i < sampleCount; i++)
            {
                var sample = sut.SampleDirection(mediaPoint, directionToMediaPoint, rand);
                histogram.AddSample(sample.Ray.Direction, sample.PdfW);
            }

            SphericalCoordinateConverter spherical = new SphericalCoordinateConverter(new Frame(directionToMediaPoint));
            SimpleFunction pdfWFunction = new SimpleFunction((x) =>
            {
                if (x < 0 || x > Math.PI) return 0;

                Vector3D direction = spherical.ToWorldDirection(new SphericalCoordinate(0, x));

                return sut.GetBrdf(directionToMediaPoint, mediaPoint, direction).PdfW;
            });

            List<FunctionToPlot> functions = new List<FunctionToPlot>();
            functions.Add(new FunctionToPlot() { Function = histogram.GetPdfThetaFunctionFromHistogram(), Color = Color.Blue, Text = "Histogram" });
            functions.Add(new FunctionToPlot() { Function = histogram.GetPdfThetaFunctionFromPdfProperty(), Color = Color.Red, Text = "Sample-PdfW" });
            functions.Add(new FunctionToPlot() { Function = pdfWFunction, Color = Color.Green, Text = "Function-PdfW" });
            //functions.Add(new FunctionToPlot() { Function = sampler.GetBrdfOverThetaFunction(), Color = Color.Yellow, Text = "Brdf" });
            FunctionPlotter plotter = new FunctionPlotter(0, Math.PI, new Size(400, 300));
            error = (int)FunctionComparer.GetMaxDifferenceAreaBetweenFunctions(0, Math.PI, histogram.GetPdfThetaFunctionFromHistogram(), histogram.GetPdfThetaFunctionFromPdfProperty(), pdfWFunction);
            return plotter.PlotFunctions(functions, "Error=" + error);


            //return histogram.GetResult();
        }

        private static Bitmap CreatePolarPlotImageFromPhaseFunction(IPhaseFunction sut, int sampleCount, float maxBrdfValue)
        {
            int imageSize = 100;
            Bitmap image = new Bitmap(imageSize, imageSize);
            IRandom rand = new Rand(0);

            Vector3D mediaPoint = new Vector3D(0, 0, 0);
            Vector3D inputDirection = new Vector3D(1, 0, 0);

            for (int i=0;i<sampleCount;i++)
            {
                var result = sut.SampleDirection(mediaPoint, inputDirection, rand);
                float brdf = sut.GetBrdf(inputDirection, mediaPoint, result.Ray.Direction).Brdf;

                Vector3D normedOutDirection = result.Ray.Direction * brdf / maxBrdfValue;
                Vector2D point = new Vector2D(0.5f + normedOutDirection.X / 2, 0.5f + normedOutDirection.Y / 2);

                int x = MathExtensions.Clamp((int)(point.X * imageSize), 0, imageSize - 1);
                int y = MathExtensions.Clamp((int)(point.Y * imageSize), 0, imageSize - 1);
                image.SetPixel(x, y, Color.Blue);
            }

            return image;
        }

        //--------------------------Versuchswerkstatt-----------------------
        //Dieses Tests sind meine Programmier-Mitschriften, um das Paper 'Importance sampling the Rayleigh phase function (2011)' zu verstehen
        [TestMethod]
        public void CreateRayleighPhaseFunctionPolarPlotImage()
        {
            //So plottet man die Rayleigh-Funktion in Polarkoordinaten
            float maxRayleighFunctionValue = (float)(3 / (8 * Math.PI));
            int imageSize = 100;
            Bitmap image = new Bitmap(imageSize, imageSize);
            int stepCount = 1000;
            for (int i = 0; i < stepCount; i++)
            {
                double angle = i / (double)stepCount * (2 * Math.PI);
                double cosAngle = Math.Cos(angle);
                float f = (float)(3 / (16 * Math.PI) * (1 + cosAngle * cosAngle));
                Vector2D center = new Vector2D(0.5f, 0.5f);
                Vector2D point = center + new Vector2D((float)Math.Cos(angle), (float)Math.Sin(angle)) * (f / maxRayleighFunctionValue / 2.1f);

                int x = MathExtensions.Clamp((int)(point.X * imageSize), 0, imageSize - 1);
                int y = MathExtensions.Clamp((int)(point.Y * imageSize), 0, imageSize - 1);
                image.SetPixel(x, y, Color.Blue);
            }

            image.Save(WorkingDirectory + "Rayleigh-PhaseFunction.bmp");
        }

        [TestMethod]
        public void SampleRayleighFunctionIsotrophic()
        {
            int sampleCount = 2000;
            int imageSize = 100;
            IRandom rand = new Rand(0);
            Vector3D directionIn = new Vector3D(1, 0, 0);
            Bitmap image = new Bitmap(imageSize, imageSize);

            float maxRayleighFunctionValue = (float)(3 / (8 * Math.PI));

            for (int i = 0; i < sampleCount; i++)
            {
                Vector3D directionOut = GetRandomDirection(rand);
                float rayleighValue = RayleighPhasefunction(directionIn, directionOut);

                Vector3D direction = directionOut * (float)(rayleighValue / maxRayleighFunctionValue / 2.0f); //Richtungsvektor wird mit Rayleigh-Wert skaliert un im Bereich von -0.5 bis +0.5 gebracht

                Vector2D center = new Vector2D(0.5f, 0.5f);
                Vector2D point = center + new Vector2D(direction.X, direction.Y);

                int x = MathExtensions.Clamp((int)(point.X * imageSize), 0, imageSize - 1);
                int y = MathExtensions.Clamp((int)(point.Y * imageSize), 0, imageSize - 1);
                image.SetPixel(x, y, Color.Blue);
            }

            image.Save(WorkingDirectory + "Rayleigh-PhaseFunction_IsotrophicDirectionSampling.bmp");
        }

        [TestMethod]
        public void SampleRayleighWithRejectionSampling()
        {
            int sampleCount = 2000;
            int imageSize = 100;
            IRandom rand = new Rand(0);
            Vector3D directionIn = new Vector3D(1, 0, 0);
            Bitmap image = new Bitmap(imageSize, imageSize);

            float maxRayleighFunctionValue = (float)(3 / (8 * Math.PI));

            for (int i = 0; i < sampleCount; i++)
            {
                Vector3D directionOut = null;
                float rayleighValue, normedRayleighValue;
                do
                {
                    directionOut = GetRandomDirection(rand);
                    rayleighValue = RayleighPhasefunction(directionIn, directionOut);

                    normedRayleighValue = rayleighValue / maxRayleighFunctionValue;
                } while (rand.NextDouble() > normedRayleighValue);


                Vector3D direction = directionOut * (float)(rayleighValue / maxRayleighFunctionValue / 2.3f);

                Vector2D center = new Vector2D(0.5f, 0.5f);
                Vector2D point = center + new Vector2D(direction.X, direction.Y);

                int x = MathExtensions.Clamp((int)(point.X * imageSize), 0, imageSize - 1);
                int y = MathExtensions.Clamp((int)(point.Y * imageSize), 0, imageSize - 1);
                image.SetPixel(x, y, Color.Blue);
            }

            image.Save(WorkingDirectory + "Rayleigh-PhaseFunction_RejectionDirectionSampling.bmp");
        }

        private float RayleighPhasefunction(Vector3D directionIn, Vector3D directionOut)
        {
            float cos = directionIn * directionOut;
            return (float)(3 / (16 * Math.PI) * (1 + cos * cos));
        }

        private Vector3D GetRandomDirection(IRandom rand)
        {
            float phi = 2 * (float)(Math.PI * rand.NextDouble());
            float theta = (float)(Math.Acos(1 - 2 * rand.NextDouble()));
            return new Vector3D((float)(Math.Cos(phi) * Math.Sin(theta)), (float)(Math.Sin(phi) * Math.Sin(theta)), (float)(Math.Cos(theta)));

        }

        [TestMethod]
        public void SampleRayleighWithSimplifiedRejectionSampling()
        {
            int sampleCount = 2000;
            int imageSize = 100;
            IRandom rand = new Rand(0);
            Vector3D directionIn = new Vector3D(1, 0, 0);
            Bitmap image = new Bitmap(imageSize, imageSize);

            float maxRayleighFunctionValue = (float)(3 / (8 * Math.PI));

            for (int i = 0; i < sampleCount; i++)
            {
                double cosTheta, cosThetaSquare;
                do
                {
                    cosTheta = 2 * rand.NextDouble() - 1;
                    cosThetaSquare = cosTheta * cosTheta;
                } while (rand.NextDouble() > 0.5 * (1 + cosThetaSquare));
                double phi = 2 * Math.PI * rand.NextDouble();

                Vector3D w = directionIn,
                   u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
                   v = Vector3D.Cross(w, u);

                float sinTheta = (float)Math.Sqrt(1 - cosThetaSquare);
                Vector3D directionOut = Vector3D.Normalize((u * (float)Math.Cos(phi) * sinTheta + v * (float)Math.Sin(phi) * sinTheta + w * (float)cosTheta));

                float rayleighValue = RayleighPhasefunction(directionIn, directionOut);
                Vector3D direction = directionOut * (float)(rayleighValue / maxRayleighFunctionValue / 2.3f);

                Vector2D center = new Vector2D(0.5f, 0.5f);
                Vector2D point = center + new Vector2D(direction.X, direction.Y);

                int x = MathExtensions.Clamp((int)(point.X * imageSize), 0, imageSize - 1);
                int y = MathExtensions.Clamp((int)(point.Y * imageSize), 0, imageSize - 1);
                image.SetPixel(x, y, Color.Blue);
            }

            image.Save(WorkingDirectory + "Rayleigh-PhaseFunction_SimplifiedRejectionDirectionSampling.bmp");
        }
    }
}
