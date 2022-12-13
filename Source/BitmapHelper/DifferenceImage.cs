using System;
using System.Drawing;

namespace BitmapHelper
{
    //Erzeugt ein Differenzbild / Differenzwert zwischen zwei Bildern
    public static class DifferenceImage
    {
        public static Bitmap GetImage(Bitmap refImg, Bitmap estimateImg)
        {
            if (refImg.Width != estimateImg.Width || refImg.Height != estimateImg.Height) throw new Exception("Images must be the same size");

            Bitmap result = new Bitmap(refImg.Width, refImg.Height);

            for (int x = 0; x < refImg.Width; x++)
                for (int y = 0; y < estimateImg.Height; y++)
                {
                    Color c1 = refImg.GetPixel(x, y);
                    Color c2 = estimateImg.GetPixel(x, y);
                    float diff = ColorDifference(c1, c2);
                    Color diffColor = PixelHelper.ConvertFloatToColor(1 - diff);
                    result.SetPixel(x, y, diffColor);
                }

            return result;
        }

        public static float GetDifference(Bitmap refImg, Bitmap estimateImg)
        {
            if (refImg.Width != estimateImg.Width || refImg.Height != estimateImg.Height) throw new Exception("Images must be the same size");

            double sum = 0;
            for (int x = 0; x < refImg.Width; x++)
                for (int y = 0; y < estimateImg.Height; y++)
                {
                    Color c1 = refImg.GetPixel(x, y);
                    Color c2 = estimateImg.GetPixel(x, y);
                    float diff = ColorDifference(c1, c2);
                    sum += diff;
                }

            return (float)(sum / (refImg.Width * refImg.Height));
        }

        //Gibt eine Prozentzahl zurück. 0 = Farben sind gleich; 1 = Komplett unterschiedlich
        private static float ColorDifference(Color refColor, Color estimateColor)
        {
            float f1 = PixelHelper.ColorToGray(PixelHelper.ColorToVector(refColor));
            float f2 = PixelHelper.ColorToGray(PixelHelper.ColorToVector(estimateColor));

            //https://en.wikipedia.org/wiki/Mean_absolute_percentage_error
            return Math.Abs((f1 - f2) / f1);
        }
    }
}
