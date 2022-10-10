using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;
using RaytracingColorEstimator;
using System.Drawing;
using System.Threading;

namespace ImageCreatorTest
{
    static class TestHelper
    {
        public static RaytracingFrame3DData GetFrameData(string backgroundPath, Bitmap backgroundImage)
        {
            return new RaytracingFrame3DData()
            {
                GlobalObjektPropertys = new GlobalObjectPropertys()
                {
                    ThreadCount = 3,
                    SamplingCount = 1,
                    BackgroundImage = backgroundPath,
                },
                PixelRange = new ImagePixelRange(0, 0, backgroundImage.Width, backgroundImage.Height),
                ScreenWidth = backgroundImage.Width,
                ScreenHeight = backgroundImage.Height,
                StopTrigger = new CancellationTokenSource(),
                ProgressChanged = (text, progress) => { }
            };
        }

        public static Bitmap GetPixelRangeFromBitmap(this Bitmap image, ImagePixelRange pixelRange)
        {
            Bitmap small = new Bitmap(pixelRange.Width, pixelRange.Height);
            for (int x = 0; x < pixelRange.Width; x++)
                for (int y = 0; y < pixelRange.Height; y++)
                {
                    small.SetPixel(x, y, image.GetPixel(pixelRange.XStart + x, pixelRange.YStart + y));
                }
            return small;
        }

        public static int GetBlackPixelCount(Bitmap image)
        {
            int clearColorCounter = 0;
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    Color c = image.GetPixel(x, y);
                    if (PixelHelper.CompareTwoColors(c, Color.Black)) clearColorCounter++;
                }
            return clearColorCounter;
        }
    }
}
