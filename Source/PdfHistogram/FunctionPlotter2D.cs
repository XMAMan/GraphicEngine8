using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitmapHelper;

namespace PdfHistogram
{
    public delegate double HighFieldValue(double x, double y);

    public class FunctionPlotter2D
    {
        private RectangleF valueRange; //In diesen XY-Wertebereich wird abgetastet
        private Size imageSize;

        public FunctionPlotter2D(RectangleF valueRange, Size imageSize)
        {
            this.valueRange = valueRange;
            this.imageSize = imageSize;
        }

        public Bitmap PlotFunctions(List<HighFieldValue> functions)
        {
            List<double[,]> values = new List<double[,]>();
            for (int i = 0; i < functions.Count; i++) values.Add(GetSamplePoints(functions[i]));

            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            for (int i = 0; i < functions.Count; i++)
            {
                GetMinMaxValue(values[i], out double minValue1, out double maxValue1);
                if (minValue1 < minValue) minValue = minValue1;
                if (maxValue1 > maxValue) maxValue = maxValue1;
            }

            Bitmap image = new Bitmap(this.imageSize.Width * functions.Count, this.imageSize.Height);
            for (int i = 0; i < functions.Count; i++)
                for (int x = 0; x < this.imageSize.Width; x++)
                    for (int y = 0; y < this.imageSize.Height; y++)
                    {
                        double f = (values[i][x, y] - minValue) / (maxValue - minValue);
                        Color color = PixelHelper.ConvertFloatToColor((float)f);
                        image.SetPixel(x + this.imageSize.Width * i, y, color);
                    }
            return image;
        }

        public Bitmap PlotFunction(HighFieldValue function)
        {
            double[,] values = GetSamplePoints(function);
            GetMinMaxValue(values, out double minValue, out double maxValue);

            Bitmap image = new Bitmap(this.imageSize.Width, this.imageSize.Height);
            for (int x = 0; x < this.imageSize.Width; x++)
                for (int y = 0; y < this.imageSize.Height; y++)
                {
                    double f = (values[x, y] - minValue) / (maxValue - minValue);
                    Color color = PixelHelper.ConvertFloatToColor((float)f);
                    image.SetPixel(x, y, color);
                }
            return image;
        }
        
        private void GetMinMaxValue(double[,] values, out double minValue, out double maxValue)
        {
            minValue = double.MaxValue;
            maxValue = double.MinValue;
            for (int x = 0; x < values.GetLength(0); x++)
                for (int y = 0; y < values.GetLength(1); y++)
                {
                    if (values[x, y] < minValue) minValue = values[x, y];
                    if (values[x, y] > maxValue) maxValue = values[x, y];
                }
        }

        private double[,] GetSamplePoints(HighFieldValue function)
        {
            double[,] values = new double[this.imageSize.Width, this.imageSize.Height];

            for (int x=0;x<this.imageSize.Width;x++)
                for (int y=0;y<this.imageSize.Height;y++)
                {
                    double x1 = x / (double)this.imageSize.Width * this.valueRange.Width + this.valueRange.X;
                    double y1 = y / (double)this.imageSize.Height * this.valueRange.Height + this.valueRange.Y;
                    values[x, y] = function(x1, y1);
                }

            return values;
        }
    }
}
