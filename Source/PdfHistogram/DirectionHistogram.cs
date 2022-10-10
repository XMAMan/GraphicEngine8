using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GraphicGlobal;
using GraphicMinimal;
using RaytracingRandom;

namespace PdfHistogram
{
    public class DirectionHistogram
    {
        internal class DirectionSample
        {
            public Vector3D Direction;
            public float PdfW;
        }
        internal class HistogramEntry
        {
            public List<DirectionSample> PdfWSamples = new List<DirectionSample>();
        }

        private int sampleCount = 0;
        private readonly DirectionChunkTable<HistogramEntry> histogram = null;

        public DirectionHistogram()
        {
            this.histogram = new DirectionChunkTable<HistogramEntry>(64, 64, new SphericalCoordinateConverter());
        }

        public DirectionHistogram(int phiSize, int thetaSize, Vector3D normal)
        {
            this.histogram = new DirectionChunkTable<HistogramEntry>(phiSize, thetaSize, new SphericalCoordinateConverter(new Frame(normal)));
        }

        public void AddSample(Vector3D direction, float pdfW)
        {
            this.sampleCount++;
            this.histogram[direction].PdfWSamples.Add(new DirectionSample() { Direction = direction, PdfW = pdfW });
        }

        public SimpleFunction GetPdfThetaFunctionFromHistogram()
        {
            int phi = 0;
            double[] thetaHistogram = new double[this.histogram.Data.GetLength(1)];
            for (int thetaIndex = 0;thetaIndex < this.histogram.Data.GetLength(1);thetaIndex++)
            {
                var samples = this.histogram.Data[phi, thetaIndex].PdfWSamples;
                if (samples.Any())
                {
                    double differentialSolidAngle = samples.Average(x => this.histogram.GetDifferentialSolidAngle(x.Direction));
                    thetaHistogram[thetaIndex] = samples.Count / (double)this.sampleCount / (double)differentialSolidAngle;
                }
                else
                {
                    thetaHistogram[thetaIndex] = 0;
                }
                
            }

            return new SimpleFunction((x) =>
            {
                if (x < 0 || x > Math.PI) return 0;

                int index = Math.Min((int)(x / Math.PI * (thetaHistogram.Length)), thetaHistogram.Length - 1);

                return thetaHistogram[index];
            });
        }

        public SimpleFunction GetPdfThetaFunctionFromPdfProperty()
        {
            int phi = 0;
            double[] thetaPdfW = new double[this.histogram.Data.GetLength(1)];
            for (int thetaIndex = 0; thetaIndex < this.histogram.Data.GetLength(1); thetaIndex++)
            {
                var samples = this.histogram.Data[phi, thetaIndex].PdfWSamples;
                if (samples.Any())
                {
                    thetaPdfW[thetaIndex] = samples.Average(x => x.PdfW);
                }
                else
                {
                    thetaPdfW[thetaIndex] = 0;
                }

            }

            return new SimpleFunction((x) =>
            {
                if (x < 0 || x > Math.PI) return 0;

                int index = Math.Min((int)(x / Math.PI * (thetaPdfW.Length)), thetaPdfW.Length - 1);

                return thetaPdfW[index];
            });
        }

        public Bitmap GetThetaPlotterImage(int width, int height)
        {
            List<FunctionToPlot> functions = new List<FunctionToPlot>
            {
                new FunctionToPlot() { Function = GetPdfThetaFunctionFromHistogram(), Color = Color.Blue, Text = "Histogram" },
                new FunctionToPlot() { Function = GetPdfThetaFunctionFromPdfProperty(), Color = Color.Red, Text = "PdfW" }
            };
            FunctionPlotter plotter = new FunctionPlotter(0, Math.PI, new Size(width, height));
            string errorText = "Error=" + (int)FunctionComparer.GetMaxDifferenceAreaBetweenFunctions(0, Math.PI, GetPdfThetaFunctionFromHistogram(), GetPdfThetaFunctionFromPdfProperty());
            return plotter.PlotFunctions(functions);
        }
    }
}
