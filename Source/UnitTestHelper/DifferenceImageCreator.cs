using BitmapHelper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace UnitTestHelper
{
    public class DifferenceImageResult
    {
        public Bitmap Image;
        public int RedError; //Zahl zwischen 0 und 100
        public int GreenError; //Zahl zwischen 0 und 100
        public int BlueError; //Zahl zwischen 0 und 100
        public int HueError; //Zahl zwischen 0 und 100
        public int SaturationError; //Zahl zwischen 0 und 100
        public int BrightnessError; //Zahl zwischen 0 und 100

        public int GetMaxError()
        {
            return (new int[] { RedError, GreenError, BlueError, HueError, SaturationError, BrightnessError }).Max();
        }

        public string GetMaxErrorWithName()
        {
            int[] values = new int[] { RedError, GreenError, BlueError, HueError, SaturationError, BrightnessError };
            string[] names = new string[] { nameof(RedError), nameof(GreenError), nameof(BlueError), nameof(HueError), nameof(SaturationError), nameof(BrightnessError) };
            int i = values.ToList().IndexOf(values.Max());
            return names[i] + "=" + values[i];
        }

        public string GetErros()
        {
            return
                "RedError=" + RedError + "\n" +
                "GreenError=" + GreenError + "\n" +
                "BlueError=" + BlueError + "\n" +
                "HueError=" + HueError + "\n" +
                "SaturationError=" + SaturationError + "\n" +
                "BrightnessError=" + BrightnessError + "\n";

        }
    }

    public class DifferenceImageCreator
    {
        //showErrorAmplified => true=Fehler werden noch mehr rötlich angezeigt; false=Keine verstärkte Fehleranzeige / Zeige wie es ist
        public static DifferenceImageResult GetDifferenceImage(Bitmap img1, Bitmap img2, bool showErrorAmplified = true)
        {
            if (img1.Width != img2.Width || img1.Height != img2.Height) throw new Exception($"img1.Width={img1.Width}, img1.Height={img1.Height} <-> img2.Width={img2.Width}, img2.Height={img2.Height}");

            float colorFactor = showErrorAmplified ? 2 : 0; //Um so größer, um so mehr werden Fehler sichtbar aber auch unwichtiges Rauschen wird erhöht
            int sizeFaktor = showErrorAmplified ? 4 : 2;//Um diesen Faktor wird das Bild erst kleiner gemacht, dann die Differenz gebildet. Damit soll Bildrauschen unterdrückt werden. Wenn hier 4 statt 2 steht, dann sieht man den Fehler stärker

            List<Bitmap> row1 = new List<Bitmap>();
            row1.Add(BitmapHelp.WriteToBitmap(new Bitmap(img1), "Reference", Color.Black));
            row1.Add(BitmapHelp.WriteToBitmap(new Bitmap(img2), "Mein Versuch", Color.Black));


            Bitmap img1Small = BitmapHelp.ScaleImageDown(img1, sizeFaktor);
            Bitmap img2Small = BitmapHelp.ScaleImageDown(img2, sizeFaktor);
            List<DifferenceImageData> diffData1 = new List<DifferenceImageData>();
            diffData1.Add(new DifferenceImageData(img1Small, img2Small, (c1, c2) => Math.Abs(c1.R - c2.R) / 255f));
            diffData1.Add(new DifferenceImageData(img1Small, img2Small, (c1, c2) => Math.Abs(c1.G - c2.G) / 255f));
            diffData1.Add(new DifferenceImageData(img1Small, img2Small, (c1, c2) => Math.Abs(c1.B - c2.B) / 255f));
            float maxError1 = diffData1.Max(x => x.MaxDifference);

            List<DifferenceImageData> diffData2 = new List<DifferenceImageData>();
            diffData2.Add(new DifferenceImageData(img1Small, img2Small, (c1, c2) => Math.Abs(c1.GetHue() - c2.GetHue()) / 360f));
            diffData2.Add(new DifferenceImageData(img1Small, img2Small, (c1, c2) => Math.Abs(c1.GetSaturation() - c2.GetSaturation())));
            diffData2.Add(new DifferenceImageData(img1Small, img2Small, (c1, c2) => Math.Abs(c1.GetBrightness() - c2.GetBrightness())));

            List<Bitmap> row2 = new List<Bitmap>();
            row2.Add(BitmapHelp.ScaleImageUp(GetDifferenceImage("Red", diffData1[0], maxError1, colorFactor), sizeFaktor)); //Red-Difference
            row2.Add(BitmapHelp.ScaleImageUp(GetDifferenceImage("Green", diffData1[1], maxError1, colorFactor), sizeFaktor)); //Green-Difference
            row2.Add(BitmapHelp.ScaleImageUp(GetDifferenceImage("Blue", diffData1[2], maxError1, colorFactor), sizeFaktor)); //Blue-Difference

            //HSB-Farbraum: https://de.wikipedia.org/wiki/HSV-Farbraum
            List<Bitmap> row3 = new List<Bitmap>();
            row3.Add(BitmapHelp.ScaleImageUp(GetDifferenceImage("Hue", diffData2[0], diffData2[0].MaxDifference, colorFactor), sizeFaktor)); //Hue-Difference -> Farbwinkel auf dem Farbkreis (0° für Rot, 120° für Grün, 240° für Blau) (Auswahl der Spektralfarbe/Wellenlänge)
            row3.Add(BitmapHelp.ScaleImageUp(GetDifferenceImage("Saturation", diffData2[1], diffData2[1].MaxDifference, colorFactor), sizeFaktor)); //Saturation-Difference -> 0% = Neutralgrau(100% Weißes Licht hinzu gemischt), 50% = wenig gesättigte Farbe, 100% = gesättigte, reine Farbe(0% weißes Licht hinzu; Somit reine Spektralfarbe)
            row3.Add(BitmapHelp.ScaleImageUp(GetDifferenceImage("Brightness", diffData2[2], diffData2[2].MaxDifference, colorFactor), sizeFaktor)); //Brightness-Difference -> 0% = keine Helligkeit, 100% = volle Helligkeit

            int histogramSize = 500;
            List<Bitmap> row4 = new List<Bitmap>();
            row4.Add(CreateHistogram(diffData1[0].Data.Cast<float>().ToArray(), "Red", histogramSize, histogramSize, out int redError));
            row4.Add(CreateHistogram(diffData1[1].Data.Cast<float>().ToArray(), "Green", histogramSize, histogramSize, out int greenError));
            row4.Add(CreateHistogram(diffData1[2].Data.Cast<float>().ToArray(), "Blue", histogramSize, histogramSize, out int blueError));

            List<Bitmap> row5 = new List<Bitmap>();
            row5.Add(CreateHistogram(diffData2[0].Data.Cast<float>().ToArray(), "Hue", histogramSize, histogramSize, out int hueError));
            row5.Add(CreateHistogram(diffData2[1].Data.Cast<float>().ToArray(), "Saturation", histogramSize, histogramSize, out int saturationError));
            row5.Add(CreateHistogram(diffData2[2].Data.Cast<float>().ToArray(), "Brightness", histogramSize, histogramSize, out int brightnessError));

            var rows = new List<Bitmap>()
            {
                BitmapHelp.TransformBitmapListToRow(row1),
                BitmapHelp.TransformBitmapListToRow(row2),
                BitmapHelp.TransformBitmapListToRow(row3),
            };

            if (showErrorAmplified == false)
            {
                rows.Add(BitmapHelp.TransformBitmapListToRow(row4));
                rows.Add(BitmapHelp.TransformBitmapListToRow(row5));
            }

            Bitmap resultImage = BitmapHelp.TransformBitmapListToCollum(rows);

            return new DifferenceImageResult()
            {
                Image = resultImage,
                RedError = redError,
                GreenError = greenError,
                BlueError = blueError,
                HueError = hueError,
                SaturationError = saturationError,
                BrightnessError = brightnessError
            };
        }

        class DifferenceImageData
        {
            public int ImageWidth { get; private set; }
            public int ImageHeight { get; private set; }
            public float MinDifference { get; private set; }
            public float MaxDifference { get; private set; }
            public float[,] Data { get; private set; }
            public DifferenceImageData(Bitmap img1, Bitmap img2, Func<Color, Color, float> colToFloat)
            {
                float[,] data = ConvertDifferenceBitmapToFloatArray(img1, img2, colToFloat);
                float min = data.Cast<float>().Min();
                float max = data.Cast<float>().Max();

                var diffList = data.Cast<float>().OrderBy(x => x).ToList();

                this.Data = data;
                this.ImageWidth = img1.Width;
                this.ImageHeight = img1.Height;
                this.MinDifference = min;
                this.MaxDifference = max;
            }

            public float GetDifference(int pixX, int pixY)
            {
                return (this.Data[pixX, pixY] - this.MinDifference) / (this.MaxDifference - this.MinDifference);
            }
        }

        private static Bitmap GetDifferenceImage(string label, DifferenceImageData data, float max, float colorFactor)
        {
            Bitmap errorImage = new Bitmap(data.ImageWidth, data.ImageHeight);
            for (int x = 0; x < data.ImageWidth; x++)
                for (int y = 0; y < data.ImageHeight; y++)
                {
                    if (colorFactor > 0) //Zeige Fehler verstärkt an
                    {
                        float f = (data.Data[x, y] - data.MinDifference) / (max - data.MinDifference);
                        errorImage.SetPixel(x, y, PixelHelper.ConvertFloatToColor(1 - f * colorFactor));
                    }
                    else //Keine Fehlerverstärkung / Zeige wie es ist
                    {
                        errorImage.SetPixel(x, y, PixelHelper.ConvertFloatToColor(1 - data.Data[x, y]));
                    }
                }

            int barWidth = data.ImageWidth / 4;
            int barHeight = 5;
            for (int x = 0; x < barWidth; x++)
                for (int y = 0; y < barHeight; y++)
                {
                    errorImage.SetPixel(x, y, PixelHelper.ConvertFloatToColor(1 - x / (float)barWidth));
                }

            int maxError = -1;
            if (colorFactor > 0) //verstärkte Fehleranzeige
                maxError = (int)((data.MaxDifference - data.MinDifference) * 255);
            else //Keine Verstärkung
                maxError = (int)(data.MaxDifference * 255);
            BitmapHelp.WriteToBitmap(errorImage, label + " Error=" + maxError, Color.Black);

            return errorImage;
        }

        private static float[,] ConvertDifferenceBitmapToFloatArray(Bitmap img1, Bitmap img2, Func<Color, Color, float> colToFloat)
        {
            float[,] data = new float[img1.Width, img1.Height];

            for (int x = 0; x < img1.Width; x++)
                for (int y = 0; y < img1.Height; y++)
                {
                    Color c1 = img1.GetPixel(x, y);
                    Color c2 = img2.GetPixel(x, y);

                    data[x, y] = colToFloat(c1, c2);
                }

            return data;
        }

        //histoArea = Gibt an, wie viel Prozent der Fläche mit Linien bedeckt sind
        private static Bitmap CreateHistogram(float[] data, string label, int imageWidth, int imageHeight, out int histoArea)
        {
            //Untersuche nur den 1%-Bereich, der den größten Fehler hat
            float threshold = data.OrderBy(x => x).ToList()[data.Length * 99 / 100];
            var dataSmall = data.Where(x => x > threshold).ToArray();
            if (dataSmall.Any() == false) dataSmall = data;

            int[] chunks = new int[imageWidth];
            float min = dataSmall.Min();
            float max = dataSmall.Max();
            for (int i = 0; i < dataSmall.Length; i++)
            {
                int pos = (int)((dataSmall[i] - min) / (max - min) * chunks.Length);
                pos = Math.Max(0, Math.Min(chunks.Length - 1, pos));
                chunks[pos]++;
            }

            int maxChunk = chunks.Max();

            Point[] notNullChunks = GetNoNullChunks(chunks);

            Bitmap image = new Bitmap(imageWidth, imageHeight);
            Graphics grx = Graphics.FromImage(image);
            grx.Clear(Color.White);
            for (int i = 0; i < chunks.Length; i++)
            {
                grx.DrawLine(Pens.Black, new Point(i, 0), new Point(i, chunks[i] * imageHeight / maxChunk));
            }
            for (int i = 0; i < notNullChunks.Length - 1; i++)
            {
                grx.DrawLine(Pens.Blue, new Point(notNullChunks[i].X, notNullChunks[i].Y * imageHeight / maxChunk), new Point(notNullChunks[i + 1].X, notNullChunks[i + 1].Y * imageHeight / maxChunk));
            }

            int area = 0;
            for (int i = 0; i < notNullChunks.Length - 1; i++)
            {
                int width = notNullChunks[i + 1].X - notNullChunks[i].X;
                int height = Math.Min(100, ((notNullChunks[i + 1].Y + notNullChunks[i].Y) / 2) * 100 / maxChunk);
                area += width * height;
            }
            area = area * 100 / (chunks.Length * 100); //Wie viel Prozent der Histogramfläche ist mit Linien bedeckt?

            grx.DrawString($"Error={area}", new Font("Arial", 30), Brushes.Red, imageWidth / 2, imageHeight / 2);


            grx.Dispose();
            BitmapHelp.WriteToBitmap(image, label, Color.Black);

            histoArea = area;
            return image;
        }

        private static Point[] GetNoNullChunks(int[] chunks)
        {
            List<Point> points = new List<Point>();
            for (int i = 0; i < chunks.Length; i++)
            {
                if (chunks[i] > 0)
                    points.Add(new Point(i, chunks[i]));
            }
            return points.ToArray();
        }
    }
}
