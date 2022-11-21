using GraphicGlobal;
using GraphicGlobal.MathHelper;
using RaytracingRandom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfHistogram
{
    //Histogram für ein Sampler, welche eine Double-Zahl in ein angegebenen Min-Max-Bereich sampelt
    public class SimpleFunctionHistogram
    {
        internal class SampleValue
        {
            public double SampledValue;
            public double Pdf;
        }

        internal class Chunk
        {
            public List<SampleValue> Samples = new List<SampleValue>();
        }

        public class ChunkResult
        {
            public double PdfFromHistogram { get; private set; }
            public double PdfFromFunction { get; private set; }
            public double Error { get; private set; }
            public int SampleCount { get; private set; }

            internal ChunkResult(Chunk chunk, int sampleCount, double differantialLength)
            {
                if (chunk.Samples.Count > 0)
                {
                    this.PdfFromHistogram = chunk.Samples.Count / (double)sampleCount / differantialLength;
                    this.PdfFromFunction = chunk.Samples.Average(x => x.Pdf);
                    this.Error = Math.Abs(this.PdfFromHistogram - this.PdfFromFunction);
                    this.SampleCount = chunk.Samples.Count;
                }
                else
                {
                    this.PdfFromHistogram = -1;
                    this.PdfFromFunction = -1;
                    this.Error = 0;
                    this.SampleCount = 0;
                }

            }

            public override string ToString()
            {
                return PdfFromHistogram + " " + PdfFromFunction + " -> " + Error + "(" + SampleCount + ")";
            }
        }

        public class Result
        {
            public ChunkResult[] ChunkResults;
            public double MaxError;
            public string ErrorText;
            public Bitmap Image;

            public Result(ChunkResult[] chunkResults)
            {
                this.ChunkResults = chunkResults;
                this.MaxError = chunkResults.Max(x => x.Error);
                this.ErrorText = string.Join(System.Environment.NewLine, chunkResults.OrderByDescending(x => x.Error).Select(x => x.ToString()));
                this.Image = GetBitmap(chunkResults);
            }

            private Bitmap GetBitmap(ChunkResult[] chunkResults)
            {
                double maxY = chunkResults.Max(x => Math.Max(x.PdfFromFunction, x.PdfFromHistogram));
                Bitmap image = new Bitmap(chunkResults.Length, 100);
                for (int x=0;x<chunkResults.Length;x++)
                {
                    int y1 = MathExtensions.Clamp((int)(image.Height - chunkResults[x].PdfFromFunction / maxY * image.Height), 0, image.Height - 1);
                    int y2 = MathExtensions.Clamp((int)(image.Height - chunkResults[x].PdfFromHistogram / maxY * image.Height), 0, image.Height - 1);

                    image.SetPixel(x, y1, Color.Red); //PdfFromFunction
                    image.SetPixel(x, y2, Color.Blue); //PdfFromHistogram
                }

                return image;
            }
        }

        private double minSampleValue;
        private double maxSampleValue;
        private double minMaxRange;
        private Chunk[] chunks = null;
        private int sampleCount = 0;

        public SimpleFunctionHistogram(double minSampleValue, double maxSampleValue, int chunkCount)
        {
            if (minSampleValue < maxSampleValue)
            {
                this.minSampleValue = minSampleValue;
                this.maxSampleValue = maxSampleValue;
                this.minMaxRange = maxSampleValue - minSampleValue;
            }else
            {
                this.minSampleValue = maxSampleValue;
                this.maxSampleValue = minSampleValue;
                this.minMaxRange = minSampleValue - maxSampleValue;
            }            

            this.chunks = new Chunk[chunkCount];
            for (int i = 0; i < this.chunks.Length; i++) this.chunks[i] = new Chunk();
        }

        public void AddSample(double sample, double pdf)
        {
            this.sampleCount++;

            double t = sample - this.minSampleValue;
            int index = Math.Min((int)(t / this.minMaxRange * (this.chunks.Length)), this.chunks.Length - 1);

            this.chunks[index].Samples.Add(new SampleValue() { SampledValue = sample, Pdf = pdf });
        }

        private Result GetResult()
        {
            double differantialLength = this.minMaxRange / this.chunks.Length;
            return new Result(this.chunks.Select(x => new ChunkResult(x, this.sampleCount, differantialLength)).ToArray());
        }

        public SimpleFunction GetPdfFunctionFromHistogram()
        {
            var chungs = GetResult().ChunkResults;
            return new SimpleFunction((x) => 
            {
                if (x < this.minSampleValue || x > this.maxSampleValue) return 0;

                double t = x - this.minSampleValue;
                int index = Math.Min((int)(t / this.minMaxRange * (this.chunks.Length)), this.chunks.Length - 1);

                return chungs[index].PdfFromHistogram;
            });
        }

        public SimpleFunction GetPdfFunctionFromPdfProperty()
        {
            var chungs = GetResult().ChunkResults;
            return new SimpleFunction((x) =>
            {
                if (x < this.minSampleValue || x > this.maxSampleValue) return 0;

                double t = x - this.minSampleValue;
                int index = Math.Min((int)(t / this.minMaxRange * (this.chunks.Length)), this.chunks.Length - 1);

                return chungs[index].PdfFromFunction;
            });
        }

        public Bitmap GetPlotterImage(int width, int height, SimpleFunction pdf, out int error)
        {
            List<FunctionToPlot> functions = new List<FunctionToPlot>();
            functions.Add(new FunctionToPlot() { Function = GetPdfFunctionFromHistogram(), Color = Color.Blue, Text = "Histogram" });
            functions.Add(new FunctionToPlot() { Function = GetPdfFunctionFromPdfProperty(), Color = Color.Red, Text = "SamplePdfL" });
            if (pdf != null) functions.Add(new FunctionToPlot() { Function = pdf, Color = Color.Green, Text = "FunctionPdf" });
            FunctionPlotter plotter = new FunctionPlotter(this.minSampleValue, this.maxSampleValue, new Size(width, height));
            error = (int)FunctionComparer.GetMaxDifferenceAreaBetweenFunctions(this.minSampleValue, this.maxSampleValue, GetPdfFunctionFromHistogram(), GetPdfFunctionFromPdfProperty());
            return plotter.PlotFunctions(functions, "Error=" + error);
        }

        public Bitmap GetPlotterImage(int width, int height, SimpleFunction pdf, string customText)
        {
            List<FunctionToPlot> functions = new List<FunctionToPlot>();
            functions.Add(new FunctionToPlot() { Function = GetPdfFunctionFromHistogram(), Color = Color.Blue, Text = "Histogram" });
            functions.Add(new FunctionToPlot() { Function = GetPdfFunctionFromPdfProperty(), Color = Color.Red, Text = "SamplePdfL" });
            if (pdf != null) functions.Add(new FunctionToPlot() { Function = pdf, Color = Color.Green, Text = "FunctionPdf" });
            FunctionPlotter plotter = new FunctionPlotter(this.minSampleValue, this.maxSampleValue, new Size(width, height));
            return plotter.PlotFunctions(functions, customText);
        }
    }
}
