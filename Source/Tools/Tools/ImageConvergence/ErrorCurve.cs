using PdfHistogram;
using RaytracingRandom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Tools.Tools.ImageConvergence
{
    //Wird wärend des Renderns vom CollectImageConvergenceData angezeigt
    class ErrorCurve
    {
        private readonly int imageWidth;
        private readonly int imageHeight;
        public readonly int ScaleFactor; //Um diesen Fkator wird das Bild von der Größe her hochskaliert

        public ErrorCurve(int imageWidth, int imageHeight, int scaleFactor)
        {
            this.imageWidth = imageWidth;
            this.imageHeight = imageHeight;
            this.ScaleFactor = scaleFactor;
        }
                
        public Bitmap PlotImageFromSingleCsvFile(CsvFile csvFile, string label, float progress)
        {
            var lines = csvFile.ReadAllLines();
            float tickRate = lines.Last().TimeToStart / (float)(lines.Length - 1);
            int maxTime = (int)(lines.Last().TimeToStart * (1f / progress)); //Vorraussichtliche Gesamtzeit
            var plotter = new FunctionPlotter(new RectangleF(0, 0, maxTime, 100), 0, maxTime, new Size(this.imageWidth * this.ScaleFactor, this.imageHeight * this.ScaleFactor));
            var plotImage = plotter.PlotFunctions(new List<FunctionToPlot>() { new FunctionToPlot() { Color = Color.Black, Function = new SimpleFunction((x)=>
            {
                int index = Math.Max(0, (int)(x / tickRate));
                if (index >= lines.Length) return double.NaN;
                return lines[index].Error;
            })} }, label);

            return plotImage;
        }
    }
}
