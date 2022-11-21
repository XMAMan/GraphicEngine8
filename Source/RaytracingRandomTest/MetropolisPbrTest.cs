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
    //https://pbr-book.org/3ed-2018/Monte_Carlo_Integration/Metropolis_Sampling
    //Ich erzeuge hier Zufallszahlen laut einer vorgegebenen unnormalisierten Funktion und plotte das Histogram dazu

    [TestClass]
    public class MetropolisPbrTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void PlotTargetAndHistogram()
        {
            BitmapHelper.BitmapHelp.TransformBitmapListToRow(new List<Bitmap>()
            {
                GetTargetFunctionImage(),
                GetHistogramImage(new Mutate1(), "Independence-Sampler"),
                GetHistogramImage(new Mutate2(), "Pertubation-Sampler"),
                GetHistogramImage(new MutateBoth(), "MutateBoth")
            }).Save(WorkingDirectory + "MetropolisPbr.bmp");
        }

        private Bitmap GetTargetFunctionImage()
        {
            List<FunctionToPlot> functions = new List<FunctionToPlot>();
            functions.Add(new FunctionToPlot() { Function = new SimpleFunction(TargetFunction), Color = Color.Blue, Text = "TargetFunction" });
            FunctionPlotter plotter = new FunctionPlotter(0, 1, new Size(400, 300));
            return plotter.PlotFunctions(functions);
        }

        private Bitmap GetHistogramImage(IMutate mutate, string text)
        {
            int sampleCount = 1000000;
            int chunkCount = 50 * 2;
            SimpleFunctionHistogram histogram = new SimpleFunctionHistogram(0, 1, chunkCount);

            double area = (1/3.0) * 0.5 * 0.5 * 0.5 * 2; //2/3 * 1/8 = 2/24 = 1/12
            MetropolisSampler sampler = new MetropolisSampler(TargetFunction, mutate, (x, weight) =>
            {
                double pdf = TargetFunction(x) / area;
                double clampedX = Math.Min(1, Math.Max(0, x));
                histogram.AddSample(clampedX, pdf);
            });
            sampler.RunNMutationSteps(sampleCount);

            return histogram.GetPlotterImage(400, 300, (xValue) => { return TargetFunction(xValue) / area; }, text);
        }

        //Ich möchte laut dieser Funktion sampeln
        private double TargetFunction(double x)
        {
            if (x >= 0 && x <= 1) return (x - 0.5) * (x - 0.5);
            return 0;
        }

        //Erzeugt Zufallszahlen laut einer TargetFunction
        class MetropolisSampler
        {
            public delegate double TargetFunction(double x);
            public delegate void Record(double x, double weight);

            private TargetFunction targetFunction;
            private IMutate mutate;
            private Record record;
            

            public MetropolisSampler(TargetFunction targetFunction, IMutate mutate, Record record)
            {
                this.targetFunction = targetFunction;
                this.mutate = mutate;
                this.record = record;
            }

            public void RunNMutationSteps(int n)
            {
                Random rand = new Random(0);

                double X0 = rand.NextDouble();
                double sampleWeight = this.targetFunction(X0);

                double X = X0;
                for (int i=0;i<n;i++)
                {
                    double newX = this.mutate.SampleNextX(rand, X);
                    double a = AcceptancePdf(X, newX);

                    //Wenn ich das so mache, weicht der Independence stark ab (Verbesserte Lösung von Pbr)
                    //this.record(X, (1 - a) * sampleWeight);
                    //this.record(newX, a * sampleWeight);

                    //this.record(X * (1 - a) + newX * a , 1); //Geht so halb

                    if (rand.NextDouble() < a)
                        X = newX;

                    //So geht es (Originallösung)
                    this.record(X, 1);
                }
            }

            private double AcceptancePdf(double previousX, double newX)
            {
                double numerator = this.targetFunction(newX) * mutate.ProposalDistribution(newX, previousX);
                double denominator = this.targetFunction(previousX) * mutate.ProposalDistribution(previousX, newX);
                return Math.Min(1, numerator / denominator);
            }
        }

        

        #region Mutate
        interface IMutate
        {
            double SampleNextX(Random rand, double previousX);
            double ProposalDistribution(double previousX, double newX); //T(newX,previousX) = Pdf(newX | previousX)
        }

        //Independence-Sampler
        class Mutate1 : IMutate
        {
            public double SampleNextX(Random rand, double previousX)
            {
                return rand.NextDouble();
            }

            public double ProposalDistribution(double previousX, double newX)
            {
                return 1;
            }
        }

        //Pertubation   
        class Mutate2 : IMutate
        {
            public double SampleNextX(Random rand, double previousX)
            {
                return previousX + 0.1 * (rand.NextDouble() - 0.5);
            }

            public double ProposalDistribution(double previousX, double newX)
            {
                if (Math.Abs(previousX - newX) <= 0.05) return 1 / 0.1;
                return 0;
            }
        }

        //Combination from Mutate1 and Mutate2   
        class MutateBoth : IMutate
        {
            private Mutate1 independence = new Mutate1();
            private Mutate2 pertubation = new Mutate2();
            public double SampleNextX(Random rand, double previousX)
            {
                if (rand.NextDouble() > 0.9)
                {
                    //Independence-Sampler
                    return this.independence.SampleNextX(rand, previousX);
                }
                else
                {
                    //Pertubation                
                    return this.pertubation.SampleNextX(rand, previousX);
                }
            }

            public double ProposalDistribution(double previousX, double newX)
            {
                double selectionPdf = 0.1;
                double independecePdf = selectionPdf * independence.ProposalDistribution(previousX, newX);
                double pertubePdf = (1 - selectionPdf) * pertubation.ProposalDistribution(previousX, newX);
                
                return pertubePdf + independecePdf;
            }
        }
        #endregion


    }
}
