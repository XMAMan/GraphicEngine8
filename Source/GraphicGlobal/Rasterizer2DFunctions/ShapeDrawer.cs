using GraphicMinimal;
using System;
using System.Drawing;

namespace GraphicGlobal.Rasterizer2DFunctions
{
    public static class ShapeDrawer
    {
        public static void DrawCircle(Vector2D pos, int radius, Action<Point> pixelCallback)
        {
            int x, y, d, dx, dxy, px = (int)pos.X, py = (int)pos.Y;
            x = 0; y = radius; d = 1 - radius;
            dx = 3; dxy = -2 * radius + 5;
            while (y >= x)
            {
                pixelCallback(new Point(px + x, py + y));
                pixelCallback(new Point(px + y, py + x));
                pixelCallback(new Point(px + y, py - x));
                pixelCallback(new Point(px + x, py - y));
                pixelCallback(new Point(px - x, py - y));
                pixelCallback(new Point(px - y, py - x));
                pixelCallback(new Point(px - y, py + x));
                pixelCallback(new Point(px - x, py + y));

                if (d < 0) { d = d + dx; dx = dx + 2; dxy = dxy + 2; x++; }
                else { d = d + dxy; dx = dx + 2; dxy = dxy + 4; x++; y--; }
            }
        }

        //Quelle: http://stackoverflow.com/questions/1201200/fast-algorithm-for-drawing-filled-circles
        public static void DrawFillCircle(Vector2D pos, int radius, Action<Point> pixelCallback)
        {
            int x0 = (int)pos.X, y0 = (int)pos.Y;
            int x = radius;
            int y = 0;
            int xChange = 1 - (radius << 1);
            int yChange = 0;
            int radiusError = 0;

            while (x >= y)
            {
                for (int i = x0 - x; i <= x0 + x; i++)
                {
                    pixelCallback(new Point(i, y0 + y));
                    pixelCallback(new Point(i, y0 - y));
                }
                for (int i = x0 - y; i <= x0 + y; i++)
                {
                    pixelCallback(new Point(i, y0 + x));
                    pixelCallback(new Point(i, y0 - x));
                }

                y++;
                radiusError += yChange;
                yChange += 2;
                if (((radiusError << 1) + xChange) > 0)
                {
                    x--;
                    radiusError += xChange;
                    xChange += 2;
                }
            }
        }


        public static void DrawLine(Point p1, Point p2, Action<Vector2D> pixelCallBack)
        {
            int length;
            float f;

            if (Math.Abs(p1.X - p2.X) > Math.Abs(p1.Y - p2.Y))
            {
                if (p1.X > p2.X)
                {
                    Point tmp = p1;
                    p1 = p2;
                    p2 = tmp;
                }

                length = p2.X - p1.X + 1;

                if (length == 0) return;

                for (int x = p1.X; x <= p2.X; x++)
                {
                    f = (x - p1.X) / (float)length;           //f geht von 0 bis 1
                    int y = (int)(p1.Y * (1 - f) + p2.Y * f); //Y-Wert
                    pixelCallBack(new Vector2D(x, y));
                }
            }
            else
            {
                if (p1.Y > p2.Y)
                {
                    Point tmp = p1;
                    p1 = p2;
                    p2 = tmp;
                }

                length = p2.Y - p1.Y + 1;

                if (length == 0) return;

                for (int y = p1.Y; y <= p2.Y; y++)
                {
                    f = (y - p1.Y) / (float)length;           //f geht von 0 bis 1
                    int x = (int)(p1.X * (1 - f) + p2.X * f); //Y-Wert
                    pixelCallBack(new Vector2D(x, y));
                }
            }
        }
    }
}
