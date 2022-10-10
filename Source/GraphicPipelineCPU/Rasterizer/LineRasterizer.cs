using System;
using System.Drawing;

namespace GraphicPipelineCPU.Rasterizer
{
    class LineRasterizer
    {
        public delegate void PixelCallBack(Point pixel, float f); //f geht von 0 bis 1 während es von p1 nach p2 läuft

        public static void DrawLine(Point p1, Point p2, PixelCallBack pixelCallBack)
        {
            int length;
            float f;
            bool swapWasDone = false;

            if (Math.Abs(p1.X - p2.X) > Math.Abs(p1.Y - p2.Y))
            {
                if (p1.X > p2.X)
                {
                    Point tmp = p1;
                    p1 = p2;
                    p2 = tmp;
                    swapWasDone = true;
                }

                length = p2.X - p1.X + 1;

                if (length == 0) return;

                for (int x = p1.X; x <= p2.X; x++)
                {
                    f = (x - p1.X) / (float)length;           //f geht von 0 bis 1
                    int y = (int)(p1.Y * (1 - f) + p2.Y * f); //Y-Wert
                    pixelCallBack(new Point(x, y), swapWasDone ? (1-f) : f);
                }
            }
            else
            {
                if (p1.Y > p2.Y)
                {
                    Point tmp = p1;
                    p1 = p2;
                    p2 = tmp;
                    swapWasDone = true;
                }

                length = p2.Y - p1.Y + 1;

                if (length == 0) return;

                for (int y = p1.Y; y <= p2.Y; y++)
                {
                    f = (y - p1.Y) / (float)length;           //f geht von 0 bis 1
                    int x = (int)(p1.X * (1 - f) + p2.X * f); //Y-Wert
                    pixelCallBack(new Point(x, y), swapWasDone ? (1 - f) : f);
                }
            }
        }
    }
}
