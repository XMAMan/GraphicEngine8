using System.Drawing;
using GraphicMinimal;

namespace BitmapHelper
{
    //Suche FireFlys nur innerhalb der Maske
    //Die Farbe der Maske wird über das Pixel [0,0] definiert
    public class ImageBoolMask
    {
        public bool[,] Mask { get; private set; }

        public int Width { get => this.Mask.GetLength(0); }
        public int Height { get => this.Mask.GetLength(1); }


        public ImageBoolMask(bool[,] mask)
        {
            this.Mask = mask;
        }

        public Bitmap ToBitmap()
        {
            Bitmap image = new Bitmap(this.Mask.GetLength(0), this.Mask.GetLength(1));

            for (int x=0; x<this.Mask.GetLength(0); x++)
                for (int y=0; y<this.Mask.GetLength(1); y++)
                {
                    image.SetPixel(x, y, Mask[x, y] ? Color.Red : Color.White);
                }

            return image;
        }

        public ImageBoolMask Negate()
        {
            bool[,] mask = new bool[this.Width, this.Height];

            for (int x = 0; x < this.Width; x++)
                for (int y = 0; y < this.Height; y++)
                {
                    mask[x, y] = !this.Mask[x, y];
                }

            return new ImageBoolMask(mask);
        }

        public static ImageBoolMask CreateMaskFromBitmap(Bitmap image)
        {
            bool[,] mask = new bool[image.Width, image.Height];

            Color maskColor = image.GetPixel(0, 0);
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    mask[x, y] = PixelHelper.CompareTwoColors(image.GetPixel(x, y), maskColor);
                }

            return new ImageBoolMask(mask);
        }


        //searchArea = Nur innerhalb des vom angegebenen Suchbereichs wird nach FireFlys (Weißen Pixeln) gesucht
        public static ImageBoolMask CreateFireflyMask(ImageBoolMask searchArea, ImageBuffer image) //Weiße Pixel innerhalb einer Suchmaske
        {
            bool[,] sharpMask = new bool[image.Width, image.Height];

            for (int x = 1; x < image.Width - 1; x++)
                for (int y = 1; y < image.Height - 1; y++)
                {
                    float c = PixelHelper.ColorToGray(image[x, y]);

                    sharpMask[x, y] = c > 1 && searchArea.Mask[x, y]; //Markiere alle weißen Pixel innerhalb der Maske
                }

            return new ImageBoolMask(sharpMask);
        }

        //Markiert weiße Pixel oder Pixel, die 30% heller als die Nachbarpixel sind
        public static ImageBoolMask CreateFireflyMask(ImageBuffer image)
        {
            bool[,] sharpMask = new bool[image.Width, image.Height];

            float bias = 0.3f;
            for (int x = 1; x < image.Width - 1; x++)
                for (int y = 1; y < image.Height - 1; y++)
                {
                    float c = PixelHelper.ColorToGray(image[x, y]);

                    Vector3D sum = new Vector3D(0, 0, 0);
                    int count = 0;
                    for (int x1 = -1; x1 <= 1; x1++)
                        for (int y1 = -1; y1 <= 1; y1++)
                        {
                            if (x1 != 0 || y1 != 0)
                            {
                                sum += image[x + x1, y + y1];
                                count++;
                            }
                        }
                    sum /= count;

                    sharpMask[x, y] = c > 1 || (image[x, y] - sum).Max() > bias;
                }

            return new ImageBoolMask(sharpMask);
        }
    }
}
