using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfHistogram;
using RaytracingRandom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaytracingRandomTest
{
    [TestClass]
    public class PdfWithTableSamplerTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void CreateFromArray_CalledForConstantPdf_GetXValuesIsAscendingLine()
        {
            double[] yValues = new double[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            PdfWithTableSampler sut = PdfWithTableSampler.CreateFromUnnormalisizedFunctionArray(yValues);
            for (int i=0;i<=100;i++)
            {
                double u = i / 100.0;
                double actualX = sut.GetXValue(u);
                double expectedX = u * yValues.Length;
                Assert.IsTrue(Math.Abs(actualX - expectedX) < 0.0001);

                double actualPdf = sut.PdfValue(u);
                double expectedPdf = 1.0 / yValues.Length;
                Assert.IsTrue(Math.Abs(actualPdf - expectedPdf) < 0.0001);
            }
        }

        [TestMethod]
        public void CreateFromArray_HistogramMatchWithFunction()
        {
            double[] yValues = new double[] { 0, 1, 0, 1, 0 };
            PdfWithTableSampler sut = PdfWithTableSampler.CreateFromUnnormalisizedFunctionArray(yValues);
            var histogram = Create1DHistogram(sut, yValues.Length);
            histogram.GetPlotterImage(400, 300, null, out int error).Save(WorkingDirectory + "1D_Distribution.bmp");             
            Assert.IsTrue(error < 5, "Error=" + error);
        }

        private static SimpleFunctionHistogram Create1DHistogram(PdfWithTableSampler sut, float maxValue)
        {
            Random rand = new Random(0);
            int chunkCount = 50;
            int sampleCount = 100000;
            SimpleFunctionHistogram histogram = new SimpleFunctionHistogram(0, maxValue, chunkCount);
            for (int i = 0; i < sampleCount; i++)
            {
                double x = sut.GetXValue(rand.NextDouble());
                double pdfL = sut.PdfValue(x);
                histogram.AddSample(x, pdfL);
            }

            return histogram;
        }

        [TestMethod]
        public void CreateFromPdf_CalledWithAscendingLinearPdf_GetXValueMatchWithLine()
        {
            PdfWithTableSampler sut = PdfWithTableSampler.CreateFromPdf(AscendingLinearPdf, 0, 1, 2);

            CheckGetXValue(sut, AscendingCdf);
        }

        [TestMethod]
        public void CreateFromCdf_CalledWithAscendingLinearCdf_GetXValueMatchWithLine()
        {
            PdfWithTableSampler sut = PdfWithTableSampler.CreateFromCdf(AscendingCdf, 0, 1, 2);

            CheckGetXValue(sut, AscendingCdf);
        }

        [TestMethod]
        public void CreateFromPdf_CalledWithDescendingLinearPdf_GetXValueMatchWithLine()
        {
            PdfWithTableSampler sut = PdfWithTableSampler.CreateFromPdf(DescendingLinearPdf, 1, 0, 2);

            CheckGetXValue(sut, DescendingCdf);
        }

        [TestMethod]
        public void CreateFromCdf_CalledWithDescendingLinearCdf_GetXValueMatchWithLine()
        {
            PdfWithTableSampler sut = PdfWithTableSampler.CreateFromCdf(DescendingCdf, 1, 0, 2);

            CheckGetXValue(sut, DescendingCdf);
        }

        [TestMethod]
        public void CreateFromUnnormalisizedFunction_CalledWithAscendingLinearFunction_GetXValueMatchWithLine()
        {
            double normalisationValue = 2; //Beliebige Zahl != 1
            PdfWithTableSampler sut = PdfWithTableSampler.CreateFromUnnormalisizedFunction((number)=> { return AscendingLinearPdf(number) * normalisationValue; }, 0, 1, 2);

            Assert.AreEqual(normalisationValue, sut.NormalisationConstant);
            CheckGetXValue(sut, AscendingCdf);
        }

        [TestMethod]
        public void TablePdfMatchWithHistogramPdf()
        {
            int sampleCount = 1000000;
            PdfWithTableSampler cosThetaSampler = PdfWithTableSampler.CreateFromUnnormalisizedFunction(MieBrdf, Math.Cos(0), Math.Cos(Math.PI), 2048);
            SimpleFunctionHistogram histogram = new SimpleFunctionHistogram(Math.Cos(0), Math.Cos(Math.PI), 50);
            Random rand = new Random(0);

            for (int i=0;i<sampleCount;i++)
            {
                double cosThetaValue = cosThetaSampler.GetXValue(rand.NextDouble());
                double pdf = MieBrdf(cosThetaValue) / cosThetaSampler.NormalisationConstant;
                histogram.AddSample(cosThetaValue, pdf);
            }

            histogram.GetPlotterImage(400, 300, null, out int error).Save(WorkingDirectory + "PdfWithTableSampler-PdfHistogram-Mie.bmp");
            Assert.IsTrue(error <5, "Error=" + error);
        }

        private double MieBrdf(double cosTheta)
        {
            float g = 0.76f;
            return 3 / (8 * Math.PI) * ((1 - g * g) * (1 + cosTheta * cosTheta)) / ((2 + g * g) * Math.Pow(1 + g * g - 2 * g * cosTheta, 1.5f));
        }

        private void CheckGetXValue(PdfWithTableSampler sut, Func<double,double> cdf)
        {
            int stepCount = 10;

            for (int i = 0; i <= stepCount; i++)
            {
                double x = i / (double)stepCount;
                Assert.IsTrue(Math.Abs(x - sut.GetXValue(cdf(x))) < 0.00000001);
            }
        }


        [TestMethod]
        public void IntegralFromMinXValueToX_CalledWithLinearFunction1_AreaMatchWithTriangleArea()
        {
            PdfWithTableSampler sut = PdfWithTableSampler.CreateFromUnnormalisizedFunction((x)=> { return x; }, 0, 1, 2);

            double area = 0.5 * 0.5 / 2;

            Assert.AreEqual(area, sut.IntegralFromMinXValueToX(0.5));
        }

        [TestMethod]
        public void IntegralFromMinXValueToX_CalledWithLinearFunction2_AreaMatchWithTriangleArea()
        {
            PdfWithTableSampler sut = PdfWithTableSampler.CreateFromUnnormalisizedFunction((x) => { return x * 3; }, 0, 1, 4);

            double area = 0.5 * 0.5 * 3 / 2;

            Assert.AreEqual(area, sut.IntegralFromMinXValueToX(0.5));
        }

        //Gibt den X-Wert zu ein gegebenen CDF-Wert zurück
        //private double InverseCdf(double u)
        //{
        //    return u;
        //}

        private double AscendingCdf(double xValue)
        {
            return xValue;
        }

        //Pdf ist im Bereich von 0 bis 1 definiert
        private double AscendingLinearPdf(double xValue)
        {
            return 1;
        }

        private double DescendingCdf(double xValue)
        {
            return 1 - xValue;
        }

        //Pdf ist im Bereich von 1 bis 0 definiert
        private double DescendingLinearPdf(double xValue)
        {
            return 1;
        }

        [TestMethod]
        public void SampleDiscrete_CalledWithConstantPdf_ReturnValueMatchWithCastInt()
        {
            PdfWithTableSampler sut = PdfWithTableSampler.CreateFromUnnormalisizedFunctionArray(new double[]{ 1,1,1});
            Assert.AreEqual(0, sut.SampleDiscrete(0));
            Assert.AreEqual((int)0.5, sut.SampleDiscrete(0.5 / 3));
            Assert.AreEqual((int)1.0, sut.SampleDiscrete(1.0 / 3));
            Assert.AreEqual((int)1.5, sut.SampleDiscrete(1.5 / 3));
            Assert.AreEqual((int)2.0, sut.SampleDiscrete(2.0 / 3));
            Assert.AreEqual((int)2.5, sut.SampleDiscrete(2.5 / 3));
            Assert.AreEqual(2, sut.SampleDiscrete(3.0 / 3)); //Hier wird 2 und nicht 3 zurück gegeben, da der Returnwert immer ein gültigen Indexwert aus dem Inputarray liefern soll
        }

        [TestMethod]
        public void SampleDiscrete_MatchWithHistogram()
        {
            PdfWithTableSampler sut = PdfWithTableSampler.CreateFromUnnormalisizedFunctionArray(new double[] { 1, 2, 3 });
            double[] expectedPdfs = new double[] { 1.0 / 6, 2.0 / 6, 3.0 / 6 };
            int[] histogram = new int[] { 0, 0, 0 };
            int sampleCount = 10000;
            Random rand = new Random(0);
            for (int i=0;i<sampleCount;i++)
            {
                int index = sut.SampleDiscrete(rand.NextDouble());
                double pdf = sut.PdfValue(index);

                Assert.AreEqual(expectedPdfs[index], pdf);
                histogram[index]++;
            }
            for (int i=0;i<histogram.Length;i++)
            {
                Assert.IsTrue(Math.Abs(histogram[i] / (double)sampleCount - expectedPdfs[i]) < 0.001f);
            }
        }
    }
}
