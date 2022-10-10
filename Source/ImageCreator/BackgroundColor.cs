using BitmapHelper;
using GraphicMinimal;
using System.Drawing;

namespace ImageCreator
{
    public class BackgroundColor
    {
        private Vector3D backgroundColor = null;
        private float backgroundColorFactor;
        private Color[,] colorBuffer = null;
        private int screenWidth;
        private int screenHeight;

        public BackgroundColor(string backgroundColor, float backgroundColorFactor, int screenWidth, int screenHeight)
        {
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.backgroundColorFactor = backgroundColorFactor;

            if (backgroundColor.StartsWith("#") == false)
            {
                this.colorBuffer = BitmapHelp.LoadBitmap(new Bitmap(backgroundColor));
            }
            else
            {
                this.backgroundColor = PixelHelper.StringToColorVector(backgroundColor) * backgroundColorFactor;
            }
        }

        public Vector3D GetColor(int x, int y)
        {
            if (this.colorBuffer != null)
            {
                int px = x * colorBuffer.GetLength(0) / screenWidth;
                int py = y * colorBuffer.GetLength(1) / screenHeight;
                return PixelHelper.ColorToVector(this.colorBuffer[px,py]) * this.backgroundColorFactor;
            }
            return backgroundColor;
        }
    }
}
