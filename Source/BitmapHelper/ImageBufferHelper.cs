using System;
using System.Collections.Generic;
using GraphicMinimal;
using System.Drawing;

namespace BitmapHelper
{
    public static class ImageBufferHelper
    {
        //Es werden scaleFactor*scaleFactor Pixels zu ein neuen Pixel zusammen geführt
        public static ImageBuffer ScaleSizeDown(this ImageBuffer image, int scaleFactor, bool interpolate)
        {
            int xCount = image.Width / scaleFactor;
            int yCount = image.Height / scaleFactor;

            ImageBuffer small = new ImageBuffer(xCount, yCount);

            if (interpolate)
            {
                for (int xi = 0; xi < xCount; xi++)
                    for (int yi = 0; yi < yCount; yi++)
                    {
                        int count = 0;
                        Vector3D colorSum = new Vector3D(0, 0, 0);
                        for (int x = xi * scaleFactor; x < Math.Min(xi * scaleFactor + scaleFactor, image.Width); x++)
                            for (int y = yi * scaleFactor; y < Math.Min(yi * scaleFactor + scaleFactor, image.Height); y++)
                            {
                                count++;
                                colorSum += image[x, y];
                            }
                        small[xi, yi] = colorSum /= count;
                    }
            }else
            { //interpolate=false -> Mache Point-Sampling
                //Wenn ich nach dem ScaleDown noch FireFlys entfernen will, dann muss interpolate false sein
                for (int xi = 0; xi < xCount; xi++)
                    for (int yi = 0; yi < yCount; yi++)
                    {
                        small[xi, yi] = image[xi * image.Width / xCount, yi * image.Height / yCount];
                    }
            }

            return small;
        }

        public static ImageBuffer GetColorScaledImage(this ImageBuffer image, float scaleFactor)
        {
            ImageBuffer image1 = new ImageBuffer(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    if (image[x, y] != null)
                    {
                        image1[x, y] = image[x, y] * scaleFactor;
                    }
                }

            }
            return image1;
        }

        public static ImageBuffer GetGammaCorrectedImage(this ImageBuffer image, float gamma)
        {
            ImageBuffer image1 = new ImageBuffer(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    if (image[x, y] != null)
                    {
                        image1[x, y] = image[x, y].Pow(1 / gamma);
                    }
                }

            }
            return image1;
        }

        public static Vector3D GetSumOverAllPixels(this ImageBuffer image)
        {
            Vector3D sum = new Vector3D(0, 0, 0);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    sum += image[x, y]; //Ohne GammaCorrection und Clampling
                }
            }
            return sum;
        }

        public static Vector3D GetSumOverAllPixelsWithGammaAndClampling(this ImageBuffer image)
        {
            Vector3D sum = new Vector3D(0, 0, 0);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    sum += image[x, y].Pow(1 / 2.2).Clamp(0, 1);
                }
            }
            return sum;
        }

        public static ImageBuffer GetSubImageBufferArea(this ImageBuffer image, int startX, int startY, int width, int height)
        {
            ImageBuffer subBuffer = new ImageBuffer(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                    subBuffer[x, y] = image[startX + x, startY + y];
            }

            return subBuffer;
        }

        public static void WriteIntoSubarea(this ImageBuffer image, int startX, int startY, ImageBuffer imageBuffer)
        {
            for (int x = 0; x < imageBuffer.Width; x++)
                for (int y = 0; y < imageBuffer.Height; y++)
                    image[startX + x, startY + y] = imageBuffer[x, y];
        }

        public static IEnumerable<Vector3D> GetAllPixels(this ImageBuffer image)
        {
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                    yield return image[x, y];

        }

        //Das source-Bild wird in zwei Bereiche laut Maske unterteilt und jeder Bereich bekommt dann sein eigenen Helligkeits- und Gammafaktor
        public static ImageBuffer GammaAndBrighnessCorrectionTwoAreas(this ImageBuffer source, float brigthness1, float gamma1, float brightness2, float gamma2, string maskFile)
        {

            var mask = ImageBoolMask.CreateMaskFromBitmap(BitmapHelp.Resize(new Bitmap(maskFile), source.Width, source.Height));
            var image1 = GetMaskedValues(source, mask);
            image1 = image1.GetColorScaledImage(brigthness1).GetGammaCorrectedImage(gamma1);
            var image2 = GetMaskedValues(source, mask.Negate());
            image2 = image2.GetColorScaledImage(brightness2).GetGammaCorrectedImage(gamma2);
            return Add(image1, image2);
        }

        public static void AddFrame(this ImageBuffer image, ImageBuffer frame)
        {
            for (int x = 0; x < frame.Width; x++)
                for (int y = 0; y < frame.Height; y++)
                {
                    image[x, y] += frame[x, y];
                }
        }

        public static ImageBuffer Add(ImageBuffer buffer1, ImageBuffer buffer2)
        {
            if (buffer1.Width != buffer2.Width || buffer1.Height != buffer2.Height) throw new ArgumentException("Width and Height has to be the same");

            ImageBuffer sum = new ImageBuffer(buffer1.Width, buffer1.Height);

            for (int y = 0; y < buffer1.Height; y++)
                for (int x = 0; x < buffer1.Width; x++)
                {
                    sum[x, y] = buffer1[x, y] + buffer2[x, y];
                }

            return sum;
        }

        public static ImageBuffer GetMaskedValues(this ImageBuffer buffer, ImageBoolMask mask)
        {
            if (buffer.Width != mask.Width || buffer.Height != mask.Height) throw new ArgumentException("Width and Height has to be the same");

            ImageBuffer masked = new ImageBuffer(buffer.Width, buffer.Height);

            for (int y = 0; y < buffer.Height; y++)
                for (int x = 0; x < buffer.Width; x++)
                {
                    if (mask.Mask[x, y])
                        masked[x, y] = buffer[x, y];
                    else
                        masked[x, y] = new Vector3D(0, 0, 0);
                }

            return masked;
        }

        public static ImageBuffer RemoveFireFlys(this ImageBuffer image, Bitmap fireMaskFromUser = null)
        {
            if (fireMaskFromUser != null)
                return RemoveFireFlysWithMask(image, fireMaskFromUser);
            else
                return RemoveFireFlys(image);
        }

        private static ImageBuffer RemoveFireFlys(ImageBuffer image)
        {
            return RemoveMarkedFireFlys(image, ImageBoolMask.CreateFireflyMask(image));
        }

        private static ImageBuffer RemoveFireFlysWithMask(ImageBuffer image, Bitmap searchMask)
        {
            Bitmap scaledUser = searchMask;
            if (searchMask.Width != image.Width || searchMask.Height != image.Height)
            {
                scaledUser = BitmapHelp.Resize(searchMask, image.Width, image.Height);
                scaledUser.SetPixel(0, 0, searchMask.GetPixel(0, 0));
            }                

            return RemoveMarkedFireFlys(image, ImageBoolMask.CreateFireflyMask(ImageBoolMask.CreateMaskFromBitmap(scaledUser), image));
        }

        private static ImageBuffer RemoveMarkedFireFlys(ImageBuffer image, ImageBoolMask fireFlyPixels)
        {
            //Wenn der Nutzer ein Suchbereich vorgibt, dann suche nur dort nach FireFlys. Ansonsten suche überall
            //var sharpMask = this.fireMaskFromUser != null ? FireFlyMask.CreateSharpMask(this.fireMaskFromUser.Mask, this.image).Mask : FireFlyMask.CreateSharpMask(this.image).Mask;
            var sharpMask = fireFlyPixels.Mask;

            //Ersetzt Firefly-Pixel mit den Farbdurchschnittswert der Nachbarpixel
            ImageBuffer withoutFire = new ImageBuffer(image.Width, image.Height);

            for (int x = 0; x < image.Width; x++)
            {
                withoutFire[x, 0] = image[x, 0];
                withoutFire[x, image.Height - 1] = image[x, image.Height - 1];
            }
            for (int y = 0; y < image.Height; y++)
            {
                withoutFire[0, y] = image[0, y];
                withoutFire[image.Width - 1, y] = image[image.Width - 1, y];
            }

            for (int x = 1; x < image.Width - 1; x++)
                for (int y = 1; y < image.Height - 1; y++)
                {
                    if (sharpMask[x, y])
                    {
                        Vector3D sum = new Vector3D(0, 0, 0);
                        int count = 0;
                        for (int x1 = -1; x1 <= 1; x1++)
                            for (int y1 = -1; y1 <= 1; y1++)
                            {
                                if (sharpMask[x + x1, y + y1] == false && (x1 != 0 || y1 != 0))
                                {
                                    sum += image[x + x1, y + y1];
                                    count++;
                                }
                            }

                        if (count > 0)
                        {
                            sum /= count;
                            withoutFire[x, y] = sum;
                        }
                        else
                        {
                            withoutFire[x, y] = image[x, y];
                        }
                    }
                    else
                    {
                        withoutFire[x, y] = image[x, y];
                    }
                }

            return withoutFire;
        }
    }
}
