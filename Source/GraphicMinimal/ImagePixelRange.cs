using System;
using System.Collections.Generic;
using System.Drawing;

namespace GraphicMinimal
{
    //Gibt ein rechteckigen Unterbereich aus dem Bild, was ScreenWidth und ScreenHeight groß ist, an
    [Serializable]
    public class ImagePixelRange
    {
        public int XStart { get; set; }
        public int YStart { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public int Left { get { return this.XStart; } }
        public int Top { get { return this.YStart; } }
        public int Right { get { return this.XStart + this.Width; } }
        public int Bottom { get { return this.YStart + this.Height; } }

        public ImagePixelRange() { }//Braucht der XML-Serialisierer

        public ImagePixelRange(int x, int y, int width, int height)
        {
            this.XStart = x;
            this.YStart = y;
            this.Width = width;
            this.Height = height;
        }

        public ImagePixelRange(Point leftUp, Point rightDown)
        {
            this.XStart = leftUp.X;
            this.YStart = leftUp.Y;
            this.Width = rightDown.X - leftUp.X;
            this.Height = rightDown.Y - leftUp.Y;

            if (this.Width <= 0) throw new ArgumentException("rightDown.X has to be greater than leftUp.X");
            if (this.Height <= 0) throw new ArgumentException("rightDown.Y has to be greater than leftUp.Y");
        }

        //Sucht im Bitmap nach ein einfarbigen Rechteck und nimmt dieses als ImagePixelRange
        public ImagePixelRange(Bitmap imageWithRectangle)
        {
            Rectangle rec = SearchOneColorRectangle(imageWithRectangle);

            this.XStart = rec.X;
            this.YStart = rec.Y;
            this.Width = rec.Width;
            this.Height = rec.Height;
        }       
        
        private Rectangle SearchOneColorRectangle(Bitmap image)
        {
            for (int x = 1; x < image.Width - 1; x++)
                for (int y = 1; y < image.Height - 1; y++)
                {
                    if (Compare(image.GetPixel(x, y), image.GetPixel(x + 1, y)) == false) continue;
                    if (Compare(image.GetPixel(x, y), image.GetPixel(x, y + 1)) == false) continue;
                    if (Compare(image.GetPixel(x, y), image.GetPixel(x - 1, y)) == true) continue;
                    if (Compare(image.GetPixel(x, y), image.GetPixel(x, y - 1)) == true) continue;

                    Point start = new Point(x, y);
                    Point? end = GetOtherEdgePoint(image, start);
                    if (end == null) continue;

                    return new Rectangle(start.X, start.Y, end.Value.X - start.X + 1, end.Value.Y - start.Y + 1);
                }

            throw new Exception("No Rectangle found");
        }

        private Point? GetOtherEdgePoint(Bitmap image, Point start)
        {
            int minEdgeLength = 5;

            Color color = image.GetPixel(start.X, start.Y);
            int x = start.X;
            for (x=start.X + 1;x<image.Width;x++)
            {
                if (Compare(image.GetPixel(x, start.Y), color) == false) break;
            }
            x--;

            if (x - start.X < minEdgeLength) return null;                    

            int y = start.Y;
            for (y = start.Y + 1; y < image.Height; y++)
            {
                if (Compare(image.GetPixel(start.X, y), color) == false) break;
            }
            y--;

            if (y - start.Y < minEdgeLength) return null;

            Point endPoint = new Point(x, y);
            for (x = start.X + 1; x < endPoint.X; x++)
            {
                if (Compare(image.GetPixel(x, endPoint.Y), color) == false) return null;

                if (Compare(image.GetPixel(x, start.Y - 1), color) == true) return null;
                if (Compare(image.GetPixel(x, start.Y + 1), color) == true) return null;
                if (Compare(image.GetPixel(x, endPoint.Y - 1), color) == true) return null;
                if (Compare(image.GetPixel(x, endPoint.Y + 1), color) == true) return null;
            }
            for (y = start.Y + 1; y < endPoint.Y; y++)
            {
                if (Compare(image.GetPixel(endPoint.X, y), color) == false) return null;

                if (Compare(image.GetPixel(start.X - 1, y), color) == true) return null;
                if (Compare(image.GetPixel(start.X + 1, y), color) == true) return null;
                if (Compare(image.GetPixel(endPoint.X - 1, y), color) == true) return null;
                if (Compare(image.GetPixel(endPoint.X + 1, y), color) == true) return null;
            }

            return new Point?(endPoint);
        }

        private static bool Compare(Color c1, Color c2)
        {
            return c1.R == c2.R && c1.G == c2.G && c1.B == c2.B;
        }

        //Unterteilt diesen Pixelrange in lauter Kästchen der Größe size.Width*size.Height. Die Kästchen am rechten und unteren
        //Rand sind abgeschnitten
        public List<ImagePixelRange> GetRandomPixelRangesInsideFromHere(Size size, Random rand)
        {
            List<ImagePixelRange> ranges = new List<ImagePixelRange>();

            for (int x = 0; x < Math.Max((int)Math.Ceiling((float)(this.Width) / size.Width), 1); x++)
                for (int y = 0; y < Math.Max((int)Math.Ceiling((float)(this.Height) / size.Height), 1); y++)
                {
                    int minX = this.XStart + x * size.Width;
                    int minY = this.YStart + y * size.Height;
                    int maxX = Math.Min(minX + size.Width, this.Right);
                    int maxY = Math.Min(minY + size.Height, this.Bottom);
                    var newRange = new ImagePixelRange(minX, minY, maxX - minX, maxY - minY);
                    ranges.Add(newRange);

                }

            for (int i = 0; i < ranges.Count; i++)
            {
                int j = rand.Next(ranges.Count);
                var temp = ranges[i];
                ranges[i] = ranges[j];
                ranges[j] = temp;
            }

            return ranges;
        }
    }
}
