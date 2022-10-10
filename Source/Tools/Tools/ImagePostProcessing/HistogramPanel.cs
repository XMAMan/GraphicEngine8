using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GraphicMinimal;
using BitmapHelper;


namespace Tools.Tools.ImagePostProcessing
{
    //Markiert all die Bildbereiche in ein ImageBuffer, welcher größer als der Schwellwert 'UpperBoundGray' sind
    //Man kann für all diese zu hellen Bildbereiche dann noch ein Faktor 'ScaleFactorForClampedColors' angeben,
    //um somit die Farbe etwas runter zu regulieren. Das ist also ein einstellbares Tonemapping
    public class HistogramPanel : Panel
    {
        private ImageBuffer Image { get; set; } = null;
        private double minGray = double.NaN, maxGray = double.NaN;

        public ImageBuffer GetClampedImageBuffer(ImageBuffer source)
        {
            if (double.IsNaN(upperBoundGray)) return source;

            ImageBuffer image = new ImageBuffer(source.Width, source.Height);
            for (int x = 0; x < source.Width; x++)
            {
                for (int y = 0; y < source.Height; y++)
                {
                    if (source[x, y] != null)
                    {
                        double gray = ColorToDouble(source[x, y]);
                        if (gray <= upperBoundGray)
                        {
                            image[x, y] = source[x, y];
                        }
                        else
                        {
                            image[x, y] = this.ShowClampedValuesRed ? new Vector3D(1, 0, 0) : (source[x, y] * this.ScaleFactorForClampedColors);
                        }
                    }
                    else
                    {
                        image[x, y] = source[x, y];
                    }
                }
            }

            return image;
        }

        public void UpdateImage(ImageBuffer image)
        {
            this.Image = image;

            this.minGray = double.MaxValue;
            this.maxGray = double.MinValue;
            int pixelCount = 0;
            for (int x = 0; x < this.Image.Width; x++)
                for (int y = 0; y < this.Image.Height; y++)
                    if (this.Image[x, y] != null)
                    {
                        pixelCount++;
                        double gray = ColorToDouble(this.Image[x, y]);
                        if (gray > this.maxGray) this.maxGray = gray;
                        if (gray < this.minGray) this.minGray = gray;
                    }

            this.UpperBoundGray = this.maxGray;
        }


        private double upperBoundGray = float.NaN;
        public double UpperBoundGray
        {
            get
            {
                return this.upperBoundGray;
            }
            set
            {
                if (this.upperBoundGray != value)
                {
                    this.upperBoundGray = value;
                    this.UpperBoundGrayChanged?.Invoke(this, value);
                }
            }
        }

        private Point mouseClickLeftPosition = new Point(0, 0);

        public EventHandler<double> UpperBoundGrayChanged;
        public bool ShowClampedValuesRed { get; set; } = true;
        public float ScaleFactorForClampedColors { get; set; } = 1;

        public HistogramPanel()
        {
            this.MouseClick += HistogramPanel_MouseClick;
            this.Paint += HistogramPanel_Paint;
        }

        private void HistogramPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(CreateHistogram(this.Width, this.Height), new Point(0, 0));
        }

        private void HistogramPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && double.IsNaN(this.maxGray) == false)
            {
                this.mouseClickLeftPosition = e.Location;
                this.UpperBoundGray = this.mouseClickLeftPosition.X / (double)this.Width * (this.maxGray - this.minGray);
            }

            if (e.Button == MouseButtons.Right)
            {
                this.mouseClickLeftPosition = new Point(0, 0);

                this.UpperBoundGray = this.maxGray;
            }


            this.Invalidate();
        }

        private Bitmap CreateHistogram(int width, int height)
        {
            if (this.Image == null) return new Bitmap(width, height);

            double lowerBound = this.minGray;

            int[] histogram = new int[width + 2]; //Das erste und das letzte Kästchen sind für die Upper/Lower-Bound-Werte
            for (int x = 0; x < this.Image.Width; x++)
                for (int y = 0; y < this.Image.Height; y++)
                    if (this.Image[x, y] != null)
                    {
                        double gray = ColorToDouble(this.Image[x, y]);
                        if (gray >= lowerBound && gray <= this.UpperBoundGray)
                        {
                            double f = (gray - lowerBound) / (this.UpperBoundGray - lowerBound);
                            int index = Math.Min((int)(f * (histogram.Length - 2)), histogram.Length - 3) + 1;
                            histogram[index]++;
                        }
                        if (gray < lowerBound) histogram[0]++;
                        if (gray > this.UpperBoundGray) histogram[histogram.Length - 1]++;
                    }
            int sumInBounds = histogram.Sum() - histogram[0] - histogram.Last();
            double maxPercentInBounds = 0;
            for (int i = 1; i < histogram.Length - 1; i++)
            {
                double percent = histogram[i] / (double)sumInBounds;
                if (percent > maxPercentInBounds) maxPercentInBounds = percent;
            }

            Bitmap image = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                double percent = histogram[x] / (double)sumInBounds;
                int h = Math.Min(height - 1, (int)(height - percent / maxPercentInBounds * height));
                for (int y = h; y < image.Height; y++) image.SetPixel(x, y, Color.Red);
            }

            if (this.mouseClickLeftPosition.X != 0)
            {
                for (int y = 0; y < image.Height; y++) image.SetPixel(this.mouseClickLeftPosition.X, y, Color.Blue);
            }

            return image;
        }

        private double ColorToDouble(Vector3D color)
        {
            return PixelHelper.ColorToGray(color);

        }

        private double Clamp(double x)
        {
            return x < 0 ? 0 : x > 1 ? 1 : x;
        }
    }
}
