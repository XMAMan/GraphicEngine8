using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RaytracingRandom;

namespace PdfHistogram
{
    public class FunctionToPlot
    {
        public SimpleFunction Function { get; set; }
        public Color Color { get; set; }
        public string Text { get; set; }
    }

    public class FunctionPlotter
    {
        class PointD
        {
            public double X { get; private set; }
            public double Y { get; private set; }
            public PointD(double x, double y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        private bool useAutoScale = false; //ViewRec wird automatisch bestimmt
        private SizeF minAutoScaleSize = new SizeF(0, 0);
        private SizeF maxAutoScaleSize = new SizeF(float.MaxValue, float.MaxValue);
        private RectangleF viewRec;
        private double minX;
        private double maxX;
        private Size imageSize;

        public static Bitmap PlotFloatArray(float[] yValues, float minY = 0.04f, float maxY = 50, string customText = null)
        {
            var function = new SimpleFunction((x) =>
            {
                if (x < 0 || x >= yValues.Length) return 0;

                int index = Math.Min((int)(x), yValues.Length - 1);

                return yValues[index];
            });

            List<FunctionToPlot> functions = new List<FunctionToPlot>();
            functions.Add(new FunctionToPlot() { Function = function, Color = Color.Blue, Text = "" });
            int width = 400, height = 300;
            FunctionPlotter plotter = new FunctionPlotter(new SizeF(yValues.Length, minY), new SizeF(yValues.Length, maxY), 0, yValues.Length, new Size(width, height));

            return plotter.PlotFunctions(functions, customText);
        }

        public static Bitmap PlotFloatArray(float[] yValues, float minY = 0.04f, string customText = null)
        {
            var function = new SimpleFunction((x) =>
            {
                if (x < 0 || x >= yValues.Length) return 0;

                int index = Math.Min((int)(x), yValues.Length - 1);

                return yValues[index];
            });

            List<FunctionToPlot> functions = new List<FunctionToPlot>();
            functions.Add(new FunctionToPlot() { Function = function, Color = Color.Blue, Text = "" });
            int width = 400, height = 300;
            FunctionPlotter plotter = new FunctionPlotter(new SizeF(yValues.Length, minY), 0, yValues.Length, new Size(width, height));

            return plotter.PlotFunctions(functions, customText);
        }

        public FunctionPlotter(RectangleF viewRec, double minX, double maxX, Size imageSize)
        {
            this.viewRec = viewRec;
            this.minX = minX;
            this.maxX = maxX;
            this.imageSize = imageSize;
        }

        public FunctionPlotter(double minX, double maxX, Size imageSize)
        {
            this.useAutoScale = true;
            this.minX = minX;
            this.maxX = maxX;
            this.imageSize = imageSize;
        }

        public FunctionPlotter(SizeF minAutoScaleSize, double minX, double maxX, Size imageSize)
        {
            this.useAutoScale = true;
            this.minAutoScaleSize = minAutoScaleSize;
            this.minX = minX;
            this.maxX = maxX;
            this.imageSize = imageSize;
        }

        public FunctionPlotter(SizeF minAutoScaleSize, SizeF maxAutoScaleSize, double minX, double maxX, Size imageSize)
        {
            this.useAutoScale = true;
            this.minAutoScaleSize = minAutoScaleSize;
            this.maxAutoScaleSize = maxAutoScaleSize;
            this.minX = minX;
            this.maxX = maxX;
            this.imageSize = imageSize;
        }

        public Bitmap PlotFunctions(List<FunctionToPlot> functions, string customText = null)
        {
            Bitmap image = new Bitmap(this.imageSize.Width, this.imageSize.Height);
            Graphics grx = Graphics.FromImage(image);

            //Automatische Einstellung des ViewRecs
            if (this.useAutoScale)
            {
                var rec = GetMinMaxRangeFromFunctions(functions);
                rec = new RectangleF(rec.X, rec.Y, Math.Max(rec.Width, this.minAutoScaleSize.Width), Math.Max(rec.Height, this.minAutoScaleSize.Height));
                rec = new RectangleF(rec.X, rec.Y, Math.Min(rec.Width, this.maxAutoScaleSize.Width), Math.Min(rec.Height, this.maxAutoScaleSize.Height));
                this.viewRec = AddBorderToRectangel(rec, 0.1f);
            }

            AddAxisToImage(grx);
            AddPlotlinesToImage(functions, grx);
            AddTextForEachFunction(functions, grx);
            if (string.IsNullOrEmpty(customText) == false) AddCustomText(customText, grx);

            grx.Dispose();
            return image;
        }

        private RectangleF GetMinMaxRangeFromFunctions(List<FunctionToPlot> functions)
        {
            double minX = float.MaxValue;
            double maxX = float.MinValue;
            double minY = float.MaxValue;
            double maxY = float.MinValue;

            foreach (var func in functions)
            {
                var points = GetNPointsFromFunction(func.Function, this.imageSize.Width);
                foreach (var p in points)
                {
                    if (p.X < minX) minX = p.X;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.Y > maxY) maxY = p.Y;
                }
            }
            return new RectangleF((float)minX, (float)minY, (float)(maxX - minX), (float)(maxY - minY));
        }

        //f .. So viel Prozent von der Breite/Höhe ist der Rand breit, welcher an das Rechteck noch drangemacht wird
        private RectangleF AddBorderToRectangel(RectangleF rec, float f)
        {
            float x = rec.Width * f;
            float y = rec.Height * f;
            return new RectangleF(rec.X - x, rec.Y - y, rec.Width + 2 * x, rec.Height + 2 * y);
        }

        private void AddPlotlinesToImage(List<FunctionToPlot> functions, Graphics grx)
        {
            double scaleX = this.imageSize.Width / (this.maxX - this.minX);
            double scaleY = scaleX * 10;

            foreach (var func in functions)
            {
                var points = GetNPointsFromFunction(func.Function, this.imageSize.Width);
                for (int i = 0; i < points.Length - 1; i++)
                {
                    var p1 = TransformPointToImage(points[i]);
                    var p2 = TransformPointToImage(points[i + 1]);
                   
                    grx.DrawLine(new Pen(func.Color), p1, p2);
                }
            }
        }

        private System.Drawing.Point TransformPointToImage(PointD point)
        {
            int x = (int)((point.X - this.viewRec.X) / (this.viewRec.Width) * this.imageSize.Width);
            int y = (int)((point.Y - this.viewRec.Y) / (this.viewRec.Height) * this.imageSize.Height);
            y = this.imageSize.Height - y - 1;

            //Clipping
            int c = 100; //Clipping-Rand. So viele Pixel darf der Punkt noch außerhalb liegen
            if (x < -c) x = -c;
            if (x > this.imageSize.Width + c) x = this.imageSize.Width + c;
            if (y < -c) y = -c;
            if (y > this.imageSize.Height + c) y = this.imageSize.Height + c;

            return new Point(x, y);
        }

        private void AddAxisToImage(Graphics grx)
        {
            float textSize = 12;
            Font font = new Font("Consolas", textSize);
            Brush fontBrush = Brushes.Gray;
            Pen pen = Pens.Gray;

            double max = 10000; //Länge der Achsen
            int l = 3; //So viel Pixel ist ein Axen-Strich lang

            var xMax = TransformPointToImage(new PointD(max, 0));
            var xMin = TransformPointToImage(new PointD(-max, 0));
            var yMax = TransformPointToImage(new PointD(0, max));
            var yMin = TransformPointToImage(new PointD(0, -max));

            grx.DrawLine(pen, xMin, xMax); //X-Achse
            grx.DrawLine(pen, yMin, yMax); //Y-Achse

            SizeF numberSize = grx.MeasureString(0.123f.ToString("0.###"), font);

            float scaleX = GetXScaleFactorForAxialText();
            float scaleY = GetYScaleFactorForAxialText();

            for (int i=-1000;i<=1000;i++)
            {
                if (i == 0) continue;

                //X-Achsenbeschriftung
                string xText = scaleX < 1 ? (i * scaleX).ToString("0.###") : ((int)(i * scaleX)).ToString();
                var px = TransformPointToImage(new PointD(i * scaleX, 0));
                grx.DrawLine(pen, px.X, px.Y - l, px.X, px.Y + l);
                SizeF sx = grx.MeasureString(xText, font);
                grx.DrawString(xText, font, fontBrush, px.X - sx.Width / 2, px.Y + l);

                //Y-Achsenbeschriftung
                string yText = scaleY < 1 ? (i * scaleY).ToString("0.###") : ((int)(i * scaleY)).ToString();
                var py = TransformPointToImage(new PointD(0, i * scaleY));
                grx.DrawLine(pen, py.X - l, py.Y, py.X + l, py.Y);
                SizeF sy = grx.MeasureString(yText, font);
                grx.DrawString(yText, font, fontBrush, py.X + l, py.Y - sy.Height / 2);
            }
        }

        private float GetXScaleFactorForAxialText()
        {
            float x1 = (1 - this.viewRec.X) / (this.viewRec.Width) * this.imageSize.Width;
            float x2 = (2 - this.viewRec.X) / (this.viewRec.Width) * this.imageSize.Width;
            float pixelDistance = x2 - x1; //So viele Pixel sind zwei Striche auf der Axe entfernt
            return GetScaleFromPixelDistance(pixelDistance);
        }

        private float GetYScaleFactorForAxialText()
        {
            float y1 = (1 - this.viewRec.Y) / (this.viewRec.Height) * this.imageSize.Height;
            float y2 = (2 - this.viewRec.Y) / (this.viewRec.Height) * this.imageSize.Height;
            float pixelDistance = y2 - y1; //So viele Pixel sind zwei Striche auf der Axe entfernt
            return GetScaleFromPixelDistance(pixelDistance);
        }

        private float GetScaleFromPixelDistance(float pixelDistance)
        {
            float dSet = 60 * 1.8f; //So viele Pixel sollen zwei Striche entfernt sein
            float f = dSet / pixelDistance;
            if (f > 1) return (int)f;
            int n = 1;
            while (f < 1)
            {
                n *= 5;
                f *= n;
            }
            return 1 / (float)n;
        }

        private void AddTextForEachFunction(List<FunctionToPlot> functions, Graphics grx)
        {
            float textSize = 10;
            Font font = new Font("Consolas", textSize);

            float y = 0;
            for (int i=0;i<functions.Count;i++)
            {
                SizeF sizef = grx.MeasureString(functions[i].Text, font);
                grx.DrawString(functions[i].Text, font, new SolidBrush(functions[i].Color), this.imageSize.Width - sizef.Width, y);                
                y += sizef.Height;
            }
        }

        private void AddCustomText(string customText, Graphics grx)
        {
            float textSize = 10;
            Font font = new Font("Consolas", textSize);
            grx.DrawString(customText, font, Brushes.Black, 50, 0);
        }

        //Tastet die Funktion mit sampleCount Werten im Bereich vom minX bis maxX ab
        private PointD[] GetNPointsFromFunction(SimpleFunction function, int sampleCount)
        {
            if (sampleCount < 2) throw new Exception("sampleCount muss mindestens 2 sein, um minX/maxX abtasten zu können");

            PointD[] points = new PointD[sampleCount];
            for (int i=0;i<points.Length;i++)
            {
                double x = (double)i / (points.Length - 1) * (this.maxX - this.minX) + this.minX;
                double y = function(x);
                points[i] = new PointD(x, y);
            }

            return points;
        }
    }
}
