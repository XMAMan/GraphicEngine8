using GraphicGlobal;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfHistogram;
using RaytracingLightSource.Basics;
using RaytracingLightSource.Basics.LightDirectionSampler;
using RaytracingRandom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTestHelper;

namespace RaytracingLightSourceTest.Basics
{
    [TestClass]
    public class LightDirectionSamplingTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        private readonly Vector3D normal = new Vector3D(0, 0, 1);
        private readonly int maxError = 6; //Zahle zwischen 0 und 100

        [TestMethod]
        public void UniformOverHalfSphere_ThetaHistogram()
        {
            ThetaHistogramTest(new UniformOverHalfSphereLightDirectionSampler(this.normal), out int error).Save(WorkingDirectory + "UniformOverHalfSphereTheta.bmp");
            Assert.IsTrue(error < maxError, "Error=" + error);
        }

        [TestMethod]
        public void UniformOverHalfSphere_PdfWIntegralIsOne()
        {
            double actual = GetPdfWIntegral(new UniformOverHalfSphereLightDirectionSampler(this.normal));
            Assert.IsTrue(Math.Abs(actual - 1) < 0.1, "Actual=" + actual);
        }

        [TestMethod]
        public void UniformOverThetaRange_ThetaHistogram()
        {
            ThetaHistogramTest(new UniformOverThetaRangeLightDirectionSampler(this.normal, 0, Math.PI / 2 / 4), out int error).Save(WorkingDirectory + "UniformOverThetaRangeTheta.bmp");
            Assert.IsTrue(error < maxError + 1, "Error=" + error);
        }

        [TestMethod]
        public void UniformOverThetaRange_PdfWIntegralIsOne()
        {
            double actual = GetPdfWIntegral(new UniformOverThetaRangeLightDirectionSampler(this.normal, 0, Math.PI / 2 / 4));
            Assert.IsTrue(Math.Abs(actual - 1) < 0.1, "Actual=" + actual);
        }

        

        [TestMethod]
        public void CosWeighted_ThetaHistogram()
        {
            ThetaHistogramTest(new CosWeightedLightDirectionSampler(this.normal), out int error).Save(WorkingDirectory + "CosWeightTheta.bmp");
            Assert.IsTrue(error < maxError, "Error=" + error);
        }

        [TestMethod]
        public void CosWeighted_PdfWIntegralIsOne()
        {
            double actual = GetPdfWIntegral(new CosWeightedLightDirectionSampler(this.normal));
            Assert.IsTrue(Math.Abs(actual - 1) < 0.1, "Actual=" + actual);
        }

        [TestMethod]
        public void CosWeightedSphereSegment_ThetaHistogram()
        {
            ThetaHistogramTest(new CosWeightedSphereSegmentLightDirectionSampler(this.normal, 0, 2 * Math.PI,  30 * Math.PI / 180, 70 * Math.PI / 180), out int error).Save(WorkingDirectory + "CosWeightThetaSphereSegment.bmp");
            Assert.IsTrue(error < maxError, "Error=" + error);
        }

        [TestMethod]
        public void CosWeightedSphereSegment_PdfWIntegralIsOne()
        {
            double actual = GetPdfWIntegral(new CosWeightedSphereSegmentLightDirectionSampler(this.normal, 0, 2 * Math.PI, 30 * Math.PI / 180, 70 * Math.PI / 180));
            Assert.IsTrue(Math.Abs(actual - 1) < 0.1, "Actual=" + actual);
        }

        [TestMethod]
        public void PowCosWeighted_ThetaHistogram()
        {
            ThetaHistogramTest(new PowCosWeightedLightDirectionSampler(this.normal, 3), out int error).Save(WorkingDirectory + "PowCosWeightTheta.bmp");
            Assert.IsTrue(error < maxError, "Error=" + error);
        }

        [TestMethod]
        public void MixTwoFunctions_PdfWIntegralIsOne()
        {
            double actual = GetPdfWIntegral(new MixTwoFunctionsLightDirectionSampler(new CosWeightedLightDirectionSampler(this.normal), new PowCosWeightedLightDirectionSampler(this.normal, 10), 0.7f));
            Assert.IsTrue(Math.Abs(actual - 1) < 0.1, "Actual=" + actual);
        }

        [TestMethod]
        public void MixTwoFunctions_ThetaHistogram()
        {
            //ThetaHistogramTest(new MixTwoFunctionsLightDirectionSampler(new UniformOverHalfSphereLightDirectionSampler(this.normale), new UniformOverThetaRangeLightDirectionSampler(this.normal, 3 * Math.PI / 180), 0.95f), out int error).Save(WorkingDirectory + "MixTwoFunctionsTheta.bmp");
            ThetaHistogramTest(new MixTwoFunctionsLightDirectionSampler(new CosWeightedLightDirectionSampler(this.normal), new PowCosWeightedLightDirectionSampler(this.normal, 10), 0.7f), out int error).Save(WorkingDirectory + "MixTwoFunctionsTheta.bmp");
            Assert.IsTrue(error < maxError, "Error=" + error);
        }

        [TestMethod]
        public void PowCosWeighted_PdfWIntegralIsOne()
        {
            double actual = GetPdfWIntegral(new PowCosWeightedLightDirectionSampler(this.normal, 3));
            Assert.IsTrue(Math.Abs(actual - 1) < 0.1, "Actual=" + actual);
        }

        [TestMethod]
        public void Importance_ThetaHistogram()
        {
            ThetaHistogramTest(CreateImportanceDirectionSampler(), out int error).Save(WorkingDirectory + "ImportanceDirection.bmp");
            Assert.IsTrue(error < maxError, "Error=" + error);
        }

        [TestMethod]
        public void Importance_PdfWIntegralIsOne()
        {
            double actual = GetPdfWIntegral(CreateImportanceDirectionSampler());
            Assert.IsTrue(Math.Abs(actual - 1) < 0.1, "Actual=" + actual);
        }

        class DoNothing { }

        private ImportanceLightDirectionSampler<DoNothing> CreateImportanceDirectionSampler()
        {
            var sampler = new ImportanceLightDirectionSampler<DoNothing>(this.normal, 5, 5);
            for (int theta = 0;theta < 5;theta++)
            {
                if (theta % 2 == 0)
                {
                    for (int phi = 0;phi<5;phi++)
                    {
                        sampler.Cells[phi, theta].IsEnabled = true;
                    }
                }
            }
            sampler.UpateRussiaRolleteSamplerAfterEnablingDisablingCells();
            return sampler;
        }

        private Bitmap ThetaHistogramTest(ILightDirectionSampler sampler, out int error)
        {
            
            int sampleCount = 100000;
            int histogramChunkCount = 50;

            DirectionHistogram histogram = new DirectionHistogram(4, histogramChunkCount, this.normal);
            Random rand = new Random(0);

            for (int i=0;i<sampleCount;i++)
            {
                var sample = sampler.SampleDirection(rand.NextDouble(), rand.NextDouble(), rand.NextDouble());
                histogram.AddSample(sample.Direction, sample.PdfW);
            }

            SphericalCoordinateConverter spherical = new SphericalCoordinateConverter(new Frame(normal));
            SimpleFunction pdfWFunction = new SimpleFunction((x) =>
            {
                if (x < 0 || x > Math.PI) return 0;

                Vector3D direction = spherical.ToWorldDirection(new SphericalCoordinate(0, x));

                return sampler.GetPdfW(direction);
            });

            var unnormalisizedBrdfFunction = sampler.GetBrdfOverThetaFunction();
            //double normalisationConstant = FunctionIntegrator.IntegrateWithRieman(unnormalisizedBrdfFunction, 0, Math.PI);
            double normalisationConstant = SphereIntegrator.IntegrateWithMonteCarlo((direction, phi, theta) => 
            {
                return unnormalisizedBrdfFunction(theta);
            });
            SimpleFunction brdfFunction = new SimpleFunction((x) =>
            {
                return unnormalisizedBrdfFunction(x) / normalisationConstant;
            });

            List<FunctionToPlot> functions = new List<FunctionToPlot>
            {
                new FunctionToPlot() { Function = histogram.GetPdfThetaFunctionFromHistogram(), Color = Color.Blue, Text = "Histogram" },
                new FunctionToPlot() { Function = histogram.GetPdfThetaFunctionFromPdfProperty(), Color = Color.Red, Text = "Sample-PdfW" },
                new FunctionToPlot() { Function = pdfWFunction, Color = Color.Green, Text = "Function-PdfW" },
                new FunctionToPlot() { Function = brdfFunction, Color = Color.Yellow, Text = "Normed Brdf" }
            };
            FunctionPlotter plotter = new FunctionPlotter(0, Math.PI, new Size(400, 300));
            error = (int)FunctionComparer.GetMaxDifferenceAreaBetweenFunctions(0, Math.PI, histogram.GetPdfThetaFunctionFromHistogram(), histogram.GetPdfThetaFunctionFromPdfProperty(), pdfWFunction);
            return plotter.PlotFunctions(functions, "Error=" + error);
        }

        private static double GetPdfWIntegral(ILightDirectionSampler sampler)
        {
            Vector3D normal = new Vector3D(0, 0, 1);
            return SphereIntegrator.IntegrateWithMonteCarlo((direction, phi, theta) =>
            {
                return sampler.GetPdfW(direction);
            }, 0, 360, 0, 90, 10000);
        }
    }
}
