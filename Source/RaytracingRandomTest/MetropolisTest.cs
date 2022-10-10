using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfHistogram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaytracingRandomTest
{
    [TestClass]
    public class MetropolisTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        private static double minX = -2.327;
        private static double maxX = +2.327;
        private double pertube = (maxX - minX) / 100 * 5;

        //Quelle: Light Transport on Path-Space Manifolds - Dissertation - Wenzel Jakob 2013.pdf Seite 52
        //Beim MetropolisHasting-Algorithmus werden Zufallszahlen laut einer gewünschten nicht-normalisierten Dichte-Funktion erzeugt
        //Grundidee: Man hat den Punkt X und will im Bereich von MinX bis MaxX lauter X-Werte in gewünschter Pdf erzeugen. Dazu bewege ich
        //das X ich entweder per Pertubation (Addiere Zufallswert auf vorherigen X-Wert drauf um neuen X-Wert zu erhalten) oder per 
        //Independece-Sampler(Vorheriger X-Wert ist egal; Gleichmäßig zufällig). Der X-Wert springt also im Bereich von MinX bis MaxX hin und her
        //und tastet somit die Funktion in den Bereich ab.
        //Zielfunktion, welche meine Pdf sein soll: 1 - x^2*1.2 + x^4 - x^6*0.15 im Bereich von x=-2.327 bis x=+2.327
        // Stammfunktion von der Zielfunktion: x - 1/3 * x^3 * 1.2 + 1/5 * x^5 - 1/7 * x^7 * 0.15
        [TestMethod]
        public void MetropolisHastingSampler() //Erzeugt Zufallszahlen laut angegebener TargetFunction
        {
            Random rand = new Random(0);
            double x = 0;
            int sampleCount = 10000;
            int chunkCount = 50;
            SimpleFunctionHistogram histogram = new SimpleFunctionHistogram(minX, maxX, chunkCount);

            double area = Stammfunction(maxX) - Stammfunction(minX);

            for (int i=0;i<sampleCount;i++)
            {
                double nextX = GetNextX(rand, x);
                double acceptancePdf = Math.Min(1, TargetFunction(nextX) * ProposalDistribution(x, nextX) / TargetFunction(x) / ProposalDistribution(nextX, x));
                //double acceptancePdf = Math.Min(1, TargetFunction(nextX) * ProposalDistribution(nextX, x) / TargetFunction(x) / ProposalDistribution(x, nextX));

                if (rand.NextDouble() <= acceptancePdf) x = nextX;

                double pdf = TargetFunction(x) / area;
                histogram.AddSample(x, pdf);
            }
            
            histogram.GetPlotterImage(400, 300, (xValue)=> { return TargetFunction(xValue) / area; }, out int error).Save(WorkingDirectory + "MetropolisHasting.bmp");
        }

        //Ich möchte laut dieser Funktion sampeln
        private double TargetFunction(double x)
        {
            double x2 = x * x;
            double x4 = x2 * x2;
            double x6 = x4 * x2;
            return 1 - x2 * 1.2 + x4 - x6 * 0.15;
        }

        //Wird zur Kontrolle für mein Histogram benötigt
        private double Stammfunction(double x)
        {
            double x3 = x * x * x;
            double x5 = x3 * x * x;
            double x7 = x5 * x * x;
            return x - 1 / 3.0 * x3 * 1.2 + 1 / 5.0 * x5 - 1 / 7.0 * x7 * 0.15;
        }

        //T(newX,previousX) = Pdf(newX | previousX)
        private double ProposalDistribution(double previousX, double newX)
        {
            double diff = Math.Abs(newX - previousX);

            double selectionPdf = 0.5;
            double pertubePdf = selectionPdf * 1.0 / (pertube * 2);
            double independecePdf = selectionPdf * 1.0f / (maxX - minX);

            if (diff < pertube)
            {
                return pertubePdf + independecePdf;
            }else
            {
                return independecePdf;
            }
        }

        //Mutations-Funktion mit Mixing-Ratio von 0.5
        private double GetNextX(Random rand, double previousX)
        {
            
            if (rand.NextDouble() > 0.5)
            {
                //Independence-Sampler
                return minX + (maxX - minX) * rand.NextDouble();
            }else
            {
                //Pertubation                
                double x = previousX + pertube * (rand.NextDouble() * 2 - 1);
                x = Math.Max(minX, x);
                x = Math.Min(maxX, x);
                return x;
            }
        }
    }
}
