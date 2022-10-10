using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfHistogram;
using RaytracingLightSource;
using RaytracingRandom;

namespace RaytracingLightSourceTest.Basics
{
    [TestClass]
    public class SphereWithImageSamplerTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;
        private float MaxError = 6;

        //Radiance-Werte vom Hdr-Image
        //Achtung: Zeilen und Spalten sind vertauscht (Liegt an C#) D.h. ich muss mit image[y,x] arbeiten
        private double[,] image = new double[,]
            {
                {10,  2,  4,  8,  1}, //Diese Zeile hier ist die Theta==0-Zeile, welche im Histogram ausgegeben wird. Ich spreche sie mit image[0;x=0..5] an
                {5,  6,  7,  8,  9},
                {15, 11, 12, 13, 14},
                {1, 16, 17, 18, 19},
                {20, 21, 22, 23, 24},
            };

        //Prüfe dass eine weiße Hdr-Map zu gleichmäßigen Sampling führt und die PdfA == 1/Einheitskugeloberfläche ist
        [TestMethod]
        public void WhiteHdrMap_CreatesEqualSampling()
        {
            SphereWithImageSampler sampler = new SphereWithImageSampler(new ImageBuffer(100,100, new Vector3D(1,1,1)), 0, new Vector3D(0, 0, 1));

            int sampleCount = 5000;
            Random rand = new Random(0);

            int size = 100;
            int r = size / 2;
            Bitmap img = new Bitmap(size * 4, size * 2); //4 Quadranten; Jeder Quadrant ist size*size groß

            double expectedPdfA = 1.0 / (4.0f * (float)Math.PI); //1 / Oberlächeninhalt von Einheitskugel

            for (int i = 0; i < sampleCount; i++)
            {
                var samp = sampler.SamplePointOnSphere(rand.NextDouble(), rand.NextDouble());

                //PdfA muss Equal sein
                double pdfA = samp.PdfA;
                double difference = Math.Abs(expectedPdfA - pdfA);
                Assert.IsTrue(difference < 0.001, "Difference-Error: " + difference);

                var p1 = samp.Position;
                var p2 = Vector3D.GetRandomDirection(rand.NextDouble(), rand.NextDouble());
                if (p1 == null) continue;

                //Quadrant Links oben; XY
                Point xy1 = new Point((int)(size / 2 + p1.X * r), (int)(size / 2 + p1.Y * r));
                Point xy2 = new Point((int)(size * 2 + size / 2 + p2.X * r), (int)(size / 2 + p2.Y * r));

                //Quadrant rechts oben; XZ
                Point xz1 = new Point((int)(size + size / 2 + p1.X * r), (int)(size / 2 + p1.Z * r));
                Point xz2 = new Point((int)(size * 2 + size + size / 2 + p2.X * r), (int)(size / 2 + p2.Z * r));

                //Quadrant Links unten; YZ
                Point yz1 = new Point((int)(size / 2 + p1.Y * r), (int)(size + size / 2 + p1.Z * r));
                Point yz2 = new Point((int)(size * 2 + size / 2 + p2.Y * r), (int)(size + size / 2 + p2.Z * r));

                //Ist-Wert
                img.SetPixel(xy1.X, xy1.Y, Color.Blue);
                img.SetPixel(xz1.X, xz1.Y, Color.Blue);
                img.SetPixel(yz1.X, yz1.Y, Color.Blue);

                //Expected-Wert
                img.SetPixel(xy2.X, xy2.Y, Color.Green);
                img.SetPixel(xz2.X, xz2.Y, Color.Green);
                img.SetPixel(yz2.X, yz2.Y, Color.Green);
            }
            img.Save(WorkingDirectory + "SphereWithImageSamplerEqualSampling.bmp");
        }

        [TestMethod]
        public void HistogramMatchWithFunction()
        {
            SphereWithImageSampler sut = new SphereWithImageSampler(Convert(image), 0, new Vector3D(0, 0, 1));

            RunTestOnSampler(sut, out int error).Save(WorkingDirectory + "SphereWithImageSampler.bmp");
            Assert.IsTrue(error < MaxError, "Error=" + error);
        }

        private ImageBuffer Convert(double[,] image)
        {
            ImageBuffer data = new ImageBuffer(image.GetLength(0), image.GetLength(1));
            for (int x=0;x<data.Width;x++)
                for (int y=0;y<data.Height;y++)
                {
                    float c = (float)image[x, y];
                    data[x, y] = new Vector3D(c, c, c);
                }
            return data;                    
        }

        //Gibt die PdfA-Funktion für Theta=0 (Erste Spalte im Bild) zurück
        private SimpleFunction GetExpectedPdfAFunction(double[,] image)
        {
            double dPhi = (2 * Math.PI) / image.GetLength(0);
            double dTheta = Math.PI / image.GetLength(1);

            double sum = 0;
            for (int x = 0; x < image.GetLength(0); x++)
                for (int y = 0; y < image.GetLength(1); y++)
                {
                    double texelArea = dPhi * dTheta * Math.Sin((y + 0.5) / (double)image.GetLength(1) * Math.PI);
                    double emission = image[x, y] * texelArea; //Wichte die Luminance mit der Fläche weil es der SphereWithImageSampler auch so macht
                    sum += emission;
                }

            

            double[] pdfAs = new double[image.GetLength(1)];
            for (int y = 0; y < image.GetLength(1); y++)
            {
                double texelArea = dPhi * dTheta * Math.Sin((y+0.5) / (double)image.GetLength(1) * Math.PI);
                double emission = image[0, y] * texelArea;
                double pmf = emission / sum; //Wahrscheinlichkeitswert/Pmf (Es fehlt das Maß)
                pdfAs[y] = pmf / texelArea;
            }

            return new SimpleFunction((x) =>
            {
                if (x < 0 || x > Math.PI) return 0;

                int index = Math.Min((int)(x / Math.PI * (pdfAs.Length)), pdfAs.Length - 1);

                return pdfAs[index];
            });
        }

        private Bitmap RunTestOnSampler(SphereWithImageSampler sampler, out int error)
        {
            int sampleCount = 1000000;
            //int histogramChunkCount = 30;

            DirectionHistogram histogram = new DirectionHistogram(5, 5, new Vector3D(0,0,1));
            Random rand = new Random(0);

            for (int i = 0; i < sampleCount; i++)
            {
                var p = sampler.SamplePointOnSphere(rand.NextDouble(), rand.NextDouble());
                if (p == null) continue;
                float pdfA = (float)sampler.GetPdfAFromPointOnSphere(p.Position);
                histogram.AddSample(p.Position, pdfA);
            }

            //Erstelle Histogram für die erste Spalte aus dem Vorgabeimage (Phi=0)
            List<FunctionToPlot> functions = new List<FunctionToPlot>();
            functions.Add(new FunctionToPlot() { Function = histogram.GetPdfThetaFunctionFromHistogram(), Color = Color.Blue, Text = "Histogram" });
            functions.Add(new FunctionToPlot() { Function = histogram.GetPdfThetaFunctionFromPdfProperty(), Color = Color.Red, Text = "Function-PdfA" });
            functions.Add(new FunctionToPlot() { Function = GetExpectedPdfAFunction(this.image), Color = Color.Green, Text = "Expected-PdfA" });

            //FunctionPlotter plotter = new FunctionPlotter(new RectangleF(-1, -0.05f, 4, 0.1f), 0, Math.PI, new Size(400, 300)); //Fixes Fenster
            FunctionPlotter plotter = new FunctionPlotter(0, Math.PI, new Size(400, 300)); //AutoScale
            error = (int)FunctionComparer.GetMaxDifferenceAreaBetweenFunctions(0, Math.PI, histogram.GetPdfThetaFunctionFromHistogram(), histogram.GetPdfThetaFunctionFromPdfProperty());
            return plotter.PlotFunctions(functions, "Error=" + error);
        }
    }
}
