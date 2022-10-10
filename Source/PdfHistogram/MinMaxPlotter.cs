using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfHistogram
{
    //Wenn man die PfadPdfA/Radiance von ganz vielen Pfaden untersuchen will und sehen will, wie die Werte im Mittel so sind und wie viel Ausreißer es gibt
    public class MinMaxPlotter
    {
        class Pixel
        {
            public double Min { get; private set; } = double.MaxValue;
            public double Max { get; private set; } = double.MinValue;
            public bool HasData { get; private set; } = false;

            public void AddSample(double d)
            {
                this.Min = Math.Min(this.Min, d);
                this.Max = Math.Max(this.Max, d);
                this.HasData = true;
            }
        }

        private Pixel[] data;
        public double Sum { get; private set; } = 0;
        public int Count { get; private set; } = 0;

        //resolution = So viel Pixel ist das Ergebnisbild breit
        public MinMaxPlotter(int resolution)
        {
            this.data = new Pixel[resolution];
            for (int i = 0; i < resolution; i++) this.data[i] = new Pixel();
        }

        //position = Zahl zwischen 0 und 1
        public void AddSample(double position, double value)
        {
            int index = Math.Min(data.Length - 1, Math.Max(0, (int)(this.data.Length * position)));
            this.data[index].AddSample(value);
            this.Sum += value;
            this.Count++;
        }

        public Bitmap GetResult(string title)
        {
            Bitmap image = new Bitmap(this.data.Length, 100);            
            Graphics grx = Graphics.FromImage(image);

            if (this.data.All(x => x.HasData == false))
            {
                grx.DrawString(title + " No Data", new Font("Consolas", 15), Brushes.Black, 0, 15);
                grx.Dispose();
                return image;
            }

            double min = this.data.Where(x => x.HasData).Min(x => x.Min);
            double max = this.data.Where(x => x.HasData).Max(x => x.Max);
            double avgMax = this.data.Where(x => x.HasData).Average(x => x.Max);
            double avgWithoutFireflys = this.data.Where(x => x.HasData && x.Max < avgMax).Select(x => x.Max).DefaultIfEmpty(avgMax).Average();
            double avg = this.Sum / Math.Max(1, this.Count);
            double span = max - min;
            if (span == 0) span = 1;

            for (int x=0;x<this.data.Length;x++)
            {
                var d = this.data[x];
                if (d.HasData)
                {
                    int y1 = (int)((d.Min - min) / span * image.Height);
                    int y2 = (int)((d.Max - min) / span * image.Height);
                    y2 = Math.Max(y1 + 1, y2);

                    grx.DrawLine(d.Max > avgMax ? Pens.Red : Pens.Blue, x, image.Height - 1 - y1, x, image.Height - 1 - y2);
                }
            }

            int y = (int)((avgMax - min) / span * image.Height);
            grx.DrawLine(Pens.Gray, 0, image.Height - 1 - y, image.Width, image.Height - 1 - y);

            if (string.IsNullOrEmpty(title) == false)
            {
                grx.DrawString(title + " AvgNoFireFlys=" + avgWithoutFireflys.ToString("0.###")+ " Avg=" + avg.ToString("0.###") + " Count=" + this.Count, new Font("Consolas", 15), Brushes.Black, 0, 15);
            }

            grx.Dispose();
            return image;
        }
    }
}
