using BitmapHelper;
using PdfHistogram;
using RaytracingRandom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tools.Tools.ImageConvergence
{
    //Wird am Ende wo alle Daten gesammelt wurden genommen um ein Vergleichsbild zu bekommen
    class DataVisualizer
    {
        public class Folder
        {
            private readonly string dataFolder;
            private readonly CsvFile.Line[] lines;

            public readonly string Label;            
            public readonly uint MaxTime;

            public Folder(string folderName, string label)
            {
                this.dataFolder = folderName;
                this.Label = label;
                this.lines = new CsvFile(folderName + "\\Output.csv").ReadAllLines();
                this.MaxTime = lines.Last().TimeToStart;
            }

            public double GetError(int time)
            {
                int index = FoundCsvIndex(this.lines, time);
                if (index == -1) return double.NaN;
                return this.lines[index].Error;
            }

            private static int FoundCsvIndex(CsvFile.Line[] lines, int time)
            {
                int minDiff = int.MaxValue;
                int foundIndex = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    int diff = Math.Abs((int)lines[i].TimeToStart - time);
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        foundIndex = i;
                    }
                }

                if (foundIndex == lines.Length - 1 && Math.Abs(lines[foundIndex].TimeToStart - time) > 5) return -1;
                return foundIndex;
            }

            public Bitmap GetImage(int time)
            {
                string[] files = Directory
                    .GetFiles(this.dataFolder, "*.bmp")
                    .Where(x => new Regex(@"\d+_Progress\.bmp").IsMatch(x))                    
                    .ToArray();

                int index = FoundImgageIndex(files.Select(x => new FileInfo(x).Name).ToArray(), time);
                if (index == -1) return null;
                return new Bitmap(files[index]);
            }

            private static int FoundImgageIndex(string[] lines, int time)
            {
                int minDiff = int.MaxValue;
                int foundIndex = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    int timeToStart = Convert.ToInt32(lines[i].Split('_')[0]);
                    int diff = Math.Abs(timeToStart - time);
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        foundIndex = i;
                    }
                }

                return foundIndex;
            }
        }

        private readonly Folder[] folder;
        private Bitmap referenceImage;
        private MaxTime maxTime;

        public enum MaxTime { Min, Max}

        public DataVisualizer(Folder[] folder, Bitmap referenceImage, MaxTime maxTime)
        {
            this.folder = folder;
            this.referenceImage = referenceImage;
            this.maxTime = maxTime;
        }

        private uint GetMaxTime()
        {
            if (this.maxTime == MaxTime.Max)
                return this.folder.Max(x => x.MaxTime);
            else
                return this.folder.Min(x => x.MaxTime);
        }

        private Bitmap PlotErrorCurves(int imageWidth, int imageHeight, int maxShownError)
        {
            Color[] colors = new Color[] { Color.Red, Color.Blue, Color.Green, Color.Violet, Color.RosyBrown, Color.DarkBlue, Color.Orange };

            List<FunctionToPlot> functions = new List<FunctionToPlot>();

            for (int i=0;i<folder.Length;i++)
            {
                var foldy = folder[i]; //Man braucht eine eigene lokale Variable da diese dann im Functor festgehalten wird
                functions.Add(new FunctionToPlot() { Color = colors[i % colors.Length], Text = folder[i].Label, Function = new SimpleFunction(x =>
                {
                    return foldy.GetError((int)x);
                })
                });
            }

            uint maxTime = GetMaxTime();
            var plotter = new FunctionPlotter(new RectangleF(0, -2, maxTime, maxShownError), 0, maxTime, new Size(imageWidth, imageHeight));
            return plotter.PlotFunctions(functions, "Error over time in seconds - Equal-Time-Compare");
        }


        private Bitmap GetDifferenceImages(int sizeFactor)
        {
            int time = (int)GetMaxTime();

            int textSize = 10;
            int imgWidth = this.referenceImage.Width * sizeFactor;

            List<Bitmap> images = new List<Bitmap>();

            images.Add(BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>()
            {
                BitmapHelp.ScaleImageUp(this.referenceImage, sizeFactor),
                BitmapHelp.GetBitmapText("Reference", imgWidth, textSize),
                BitmapHelp.GetBitmapText("Time=" + new TimeSpan(0,0, (int)time).ToNiceString(), imgWidth, textSize)
            }));

            foreach (var fold in this.folder)
            {
                var img = fold.GetImage(time);
                images.Add(BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>()
                {
                    BitmapHelp.ScaleImageUp(img, sizeFactor),
                    BitmapHelp.ScaleImageUp(DifferenceImage.GetImage(this.referenceImage, img), sizeFactor),                    
                    BitmapHelp.GetBitmapText(fold.Label + " Error=" + (int)(DifferenceImage.GetDifference(this.referenceImage, img) * 100), imgWidth, textSize),
                }));                
            }

            return BitmapHelp.TransformBitmapListToRow(images, true);
        }

        public enum Layout { AllInRow, AllInColum}
        public Bitmap GetCompareImage(int imageWidth, int imageHeight, int scaleUpFactor, Layout layout, int maxShownError)
        {
            switch(layout)
            {
                case Layout.AllInRow:
                    return BitmapHelp.TransformBitmapListToRow(new List<Bitmap>()
                    {
                        PlotErrorCurves(imageWidth,imageHeight, maxShownError),
                        GetDifferenceImages(scaleUpFactor)
                    });

                case Layout.AllInColum:
                    return BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>()
                    {
                        PlotErrorCurves(imageWidth,imageHeight, maxShownError),
                        GetDifferenceImages(scaleUpFactor)
                    });
            }

            throw new ArgumentException(nameof(layout));
        }
    }
}
