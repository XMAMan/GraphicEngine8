using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitmapHelper;
using GraphicMinimal;

namespace PdfHistogram
{
    //Zur Kontrolle der PdfA von Punkten, die in der XY-Ebene im Bereich von X=0..Size.Width und Y=0..Size.Height gesampelt werden
    public class RectangleHistogram
    {
        internal class PositionSample
        {
            public Vector2D Position;
            public float PdfA;
        }
        internal class HistogramEntry
        {
            public List<PositionSample> PdfWSamples = new List<PositionSample>();
        }

        private int sampleCount = 0;
        private RectangleChunkTable<HistogramEntry> histogram = null;
        private Size size; //Größe des Rechtecks

        public RectangleHistogram(int xSize, int ySize, Size size)
        {
            this.histogram = new RectangleChunkTable<HistogramEntry>(xSize, ySize, size);
            this.size = size;
        }

        public void AddSample(Vector2D position, float pdfA)
        {
            this.sampleCount++;
            this.histogram[position].PdfWSamples.Add(new PositionSample() { Position = position, PdfA = pdfA });
        }

        public Bitmap GetResultImage()
        {
            SizeF factor = new SizeF(this.size.Width / (float)this.histogram.Data.GetLength(0), this.size.Height / (float)this.histogram.Data.GetLength(1));
            FunctionPlotter2D plotter = new FunctionPlotter2D(new RectangleF(0, 0, this.size.Width, this.size.Height), new Size(this.histogram.Data.GetLength(0), this.histogram.Data.GetLength(1)));
            return plotter.PlotFunctions(new List<HighFieldValue>(){
                (x, y) =>
                {
                    var samples = this.histogram.Data[(int)(x / factor.Width), (int)(y / factor.Height)].PdfWSamples;
                    if (samples.Any())
                    {
                        return samples.Count / (double)this.sampleCount / this.histogram.DifferentialSurfaceArea;
                    }
                    else
                    {
                        return 0;
                    }
                },
                (x, y) =>
                {
                    var samples = this.histogram.Data[(int)(x / factor.Width), (int)(y / factor.Height)].PdfWSamples;
                    if (samples.Any())
                    {
                        return samples.Average(t => t.PdfA);
                    }
                    else
                    {
                        return 0;
                    }
                }
            });
        }

        public Bitmap GetHistogramImage()
        {
            SizeF factor = new SizeF(this.size.Width / (float)this.histogram.Data.GetLength(0), this.size.Height / (float)this.histogram.Data.GetLength(1));
            FunctionPlotter2D plotter = new FunctionPlotter2D(new RectangleF(0, 0, this.size.Width, this.size.Height), new Size(this.histogram.Data.GetLength(0), this.histogram.Data.GetLength(1)));
            return plotter.PlotFunction((x, y) =>
            {
                var samples = this.histogram.Data[(int)(x / factor.Width), (int)(y / factor.Height)].PdfWSamples;
                if (samples.Any())
                {
                    return samples.Count / (double)this.sampleCount / this.histogram.DifferentialSurfaceArea;
                }else
                {
                    return 0;
                }                
            });
        }

        public Bitmap GetPdfAFunctionImage()
        {
            SizeF factor = new SizeF(this.size.Width / (float)this.histogram.Data.GetLength(0), this.size.Height / (float)this.histogram.Data.GetLength(1));
            FunctionPlotter2D plotter = new FunctionPlotter2D(new RectangleF(0, 0, this.size.Width, this.size.Height), new Size(this.histogram.Data.GetLength(0), this.histogram.Data.GetLength(1)));
            return plotter.PlotFunction((x, y) =>
            {
                var samples = this.histogram.Data[(int)(x / factor.Width), (int)(y / factor.Height)].PdfWSamples;
                if (samples.Any())
                {
                    return samples.Average(t => t.PdfA);
                }
                else
                {
                    return 0;
                }

            });
        }
    }
}
