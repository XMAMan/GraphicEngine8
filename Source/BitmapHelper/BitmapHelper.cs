using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using GraphicMinimal;
using System.IO;

namespace BitmapHelper
{
    public static class BitmapHelp
    {
        public static ImageBuffer LoadOpenEXRImage(string fileName)
        {
            return OpenEXRLoader.LoadHdrImage(fileName);
        }        

        public static ImageBuffer LoadHdrImage(string fileName)
        {
            if (File.Exists(fileName) == false) throw new FileNotFoundException(fileName);

            if (fileName.EndsWith(".exr"))
                return OpenEXRLoader.LoadHdrImage(fileName);
            else if (fileName.EndsWith(".hdr"))
                return new RgbeFile(fileName).GetAsImageBuffer();
            else
                return new ImageBuffer(new Bitmap(fileName));
        }

        public static bool IsHdrImageName(string fileName)
        {
            return fileName.EndsWith(".exr") || fileName.EndsWith(".hdr");
        }

        public static Color[,] LoadBitmap(string fileName)
        {
            if (File.Exists(fileName) == false) throw new FileNotFoundException(fileName);
            return LoadBitmap(new Bitmap(fileName));
        }

        public static Color[,] LoadBitmap(Bitmap image)
        {
            Color[,] texture = new Color[image.Width, image.Height];
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    texture[x,y] = image.GetPixel(x, y);
                }
            }
            return texture;
        }

        public static Bitmap ConvertImageArrayToBitmap(Color[,] image)
        {
            Bitmap ret = new Bitmap(image.GetLength(0), image.GetLength(1));
            for (int x = 0; x < ret.Width; x++)
            {
                for (int y = 0; y < ret.Height; y++)
                {
                    ret.SetPixel(x, y, image[x,y]);
                }
            }
            return ret;
        }

        public static Bitmap ConvertByteArrayFromDepthTextureToBitmap(byte[] data, int width, int height, bool flipYValues)
        {
            float[,] depthValues = ConvertByteArrayFromDepthTextureToFloatArray(data, width, height);

            return ConvertDepthValuesToBitmap(depthValues, flipYValues);
        }
        private static float[,] ConvertByteArrayFromDepthTextureToFloatArray(byte[] data, int width, int height)
        {
            int i = 0;
            float[,] depthValues = new float[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float depth = PixelHelper.ConvertUnsignedNormalizedIntegerToFloat(data[i + 2] << 16 | data[i + 1] << 8 | data[i + 0], 24);
                    i += 4;
                    depthValues[x, y] = depth;
                }
            }

            return depthValues;
        }

        public static float[,] ConvertFlatArrayTo2DArray(float[] data, int width, int height)
        {
            if (data.Length != (width * height)) throw new ArgumentException("data.Length must be width*height");
            float[,] ret = new float[width, height];
            int index = 0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    ret[x, y] = data[index++];
                }
            return ret;
        }

        public static Bitmap ConvertDepthValuesToBitmap(float[,] depthValues, bool flipYValues)
        {
            int width = depthValues.GetLength(0);
            int height = depthValues.GetLength(1);

            float minDepth = float.MaxValue;
            float maxDepth = float.MinValue;

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float depth = depthValues[x, y];
                    if (depth < minDepth) minDepth = depth;
                    if (depth > maxDepth) maxDepth = depth;
                }

            System.Drawing.Bitmap result = new System.Drawing.Bitmap(width, height);
            for (int y = 0; y < result.Height; y++)
                for (int x = 0; x < result.Width; x++)
                {
                    if (minDepth - maxDepth == 0)
                    {
                        result.SetPixel(x, y, Color.White);
                    }
                    else
                    {
                        float depth = depthValues[x, y];
                        int d = (int)((depth - minDepth) / (maxDepth - minDepth) * 255);
      
                        result.SetPixel(x, flipYValues ? height - y - 1 : y, Color.FromArgb(d, d, d));
                    }
                }
            return result;
        }

        

        //Erzeuge Bumpmap, die doppelt so groß wie die Textur ist und um ein halben Pixel nach rechts/unten versetzt ist
        class BumpmapFromColorTexturCreator
        {
            private int numberOfDiverentHeightValues;      
            private float heightScaleFactor;
            private float[,] heightMap;

            private BumpmapFromColorTexturCreator() { }

            public static Bitmap GetBumpmapFromTexture(Bitmap image)
            {
                return new BumpmapFromColorTexturCreator().GetBumpmapFromTexture1(image);
            }

            private Bitmap GetBumpmapFromTexture1(Bitmap image)
            {
                this.numberOfDiverentHeightValues = 3; //Beeinflußt, wie viel verschiedene Alpha-Werte (Höhenwerte) gespeichert werden
                this.heightScaleFactor = Math.Max(image.Width, image.Height) * 0.1f; //Beeinflußt, wo steil/flach die Normalen wirken (RGB-Werte)       
                bool useOldStyle = true;    //Wenn true wird die Bumpmap um 1 Pixel nach rechts unten verschoben; Wenn false, sieht das Bild wie CreateHeightmapFromImage aus

                //Schritt 1: Erzeuge Heightmap
                this.heightMap = new float[image.Width, image.Height]; //Höhenwerte gehen von 0 bis heightScaleFactor
                float maxHeight = float.MinValue;
                for (int x = 0; x < image.Width; x++)
                    for (int y = 0; y < image.Height; y++)
                    {
                        Color C = image.GetPixel(x, y);
                        int grayValue = 255 - (int)(C.R * 0.2989 + C.G * 0.5866 + C.B * 0.1145); //Zahl zwischen 0 und 255
                        this.heightMap[x, y] = grayValue / 255f * this.heightScaleFactor;
                        if (this.heightMap[x, y] > maxHeight) maxHeight = this.heightMap[x, y];
                    }

                //Schritt 2: Verschiebe alles nach oben so dass die Parallaxmap immer als obersten Wert die 1 hat
                float delta = this.heightScaleFactor - maxHeight;
                for (int x = 0; x < image.Width; x++)
                    for (int y = 0; y < image.Height; y++)
                    {
                        this.heightMap[x, y] += delta;
                    }

                //Schritt 3: Erzeuge doppelt so große Bumpmap (Es entsteht ein ein Pixel breiter Rand)
                Bitmap bumpmap = new Bitmap(image.Width * 2, image.Height * 2);
                for (int y = 0; y < image.Height - 1; y++)
                    for (int x = 0; x < image.Width - 1; x++)
                    {
                        Vector3D p00 = new Vector3D(x * 2 + 1, y * 2 + 1, heightMap[x, y]); //Versetze um halben Pixel nach rechts unten (Desswegen +1 bei x und y)
                        Vector3D p10 = new Vector3D(x * 2 + 2, y * 2 + 1, heightMap[x + 1, y]); //Rechts daneben von p00
                        Vector3D p01 = new Vector3D(x * 2 + 1, y * 2 + 2, heightMap[x, y + 1]); //Darunter von p00
                        Vector3D normal = Vector3D.Cross(Vector3D.Normalize(p10 - p00), Vector3D.Normalize(p01 - p00));
                        if (normal.Z < 0) throw new Exception("Die Normale muss immer nach oben zeigen");

                        Vector3D N = (normal / 2.0f + new Vector3D(0.5f, 0.5f, 0.5f)) * 255;
                        bumpmap.SetPixel(x * 2, y * 2, Color.FromArgb(GetHeightValue(x, y), (int)N.X, (int)N.Y, (int)N.Z));
                        bumpmap.SetPixel(x * 2 + 1, y * 2, Color.FromArgb(GetHeightValue(x + 1, y), (int)N.X, (int)N.Y, (int)N.Z));
                        bumpmap.SetPixel(x * 2, y * 2 + 1, Color.FromArgb(GetHeightValue(x, y + 1), (int)N.X, (int)N.Y, (int)N.Z));
                        bumpmap.SetPixel(x * 2 + 1, y * 2 + 1, Color.FromArgb(GetHeightValue(x + 1, y + 1), (int)N.X, (int)N.Y, (int)N.Z));
                    }

                // Schritt 4:  Verschiebe ganzes Bild um ein Pixel nach rechts unten
                // Damit sieht der Decal-Rand grau aus. Wenn ich das nicht mache, ist er komplett gelb oder blau
                // Dieser Verschiebungsschritt ist vielleicht unlogisch aber er produziert ein Bild, was wie der alte Algorithmus aussieht
                // Wenn ich diese Verschiebung nicht mache, sieht das Bild aus wie wenn ich es mit der Heighmap erzeugt hätte
                if (useOldStyle)
                {
                    for (int y = bumpmap.Height - 1; y > 0; y--)
                        for (int x = bumpmap.Width - 1; x > 0; x--)
                        {
                            bumpmap.SetPixel(x, y, bumpmap.GetPixel(x - 1, y - 1));
                        }
                }
                
                //Schritt 5: Beschreibe noch den kompletten Rand mit Werten
                for (int x = 1; x < bumpmap.Width - 1; x++)
                {
                    bumpmap.SetPixel(x, 0, bumpmap.GetPixel(x, 1));
                    bumpmap.SetPixel(x, bumpmap.Height - 1, bumpmap.GetPixel(x, bumpmap.Height - 2));
                }
                for (int y = 0; y < bumpmap.Height - 1; y++)
                {
                    bumpmap.SetPixel(0, y, bumpmap.GetPixel(1, y));
                    bumpmap.SetPixel(bumpmap.Width - 1, y, bumpmap.GetPixel(bumpmap.Width - 2, y));
                }
                bumpmap.SetPixel(0, 0, bumpmap.GetPixel(1, 1));
                bumpmap.SetPixel(bumpmap.Width - 1, 0, bumpmap.GetPixel(bumpmap.Width - 2, 1));
                bumpmap.SetPixel(0, bumpmap.Height - 1, bumpmap.GetPixel(1, bumpmap.Height - 2));
                bumpmap.SetPixel(bumpmap.Width - 1, bumpmap.Height - 1, bumpmap.GetPixel(bumpmap.Width - 2, bumpmap.Height - 2));

                return bumpmap;
            }

            private int GetHeightValue(int x, int y)
            {
                int height = (int)(this.heightMap[x, y] / this.heightScaleFactor * 255); //Zahl zwischen 0 und 255

                //Quantisiere die Höhe auf Zahl zwischen 0 und numberOfDiverentHeightValues
                height = (int)((((int)((float)height / 255.0f * this.numberOfDiverentHeightValues)) / (float)this.numberOfDiverentHeightValues) * 255);

                return height; //0 bis anzahlHöhenStufen-1
            }
        }

        public static Bitmap GetBumpmapFromColor(Bitmap image)
        {
            return BumpmapFromColorTexturCreator.GetBumpmapFromTexture(image);            
        }

        public static Bitmap GetAlphaChannel(Bitmap image)
        {
            Bitmap image1 = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    int a = image.GetPixel(x, y).A;
                    image1.SetPixel(x, y, Color.FromArgb(a, a, a));
                }
            return image1;
        }

        //BitmapHelper.BitmapHelp.MergeNormalMapAndHighMapIntoSingleBitmap(new Bitmap("toy_box_normal.png"), new Bitmap("toy_box_disp.png")).Save("toy_box_NormalAndHigh.png");
        public static Bitmap MergeNormalMapAndHeighMapIntoSingleBitmap(Bitmap normalmap, Bitmap heightMap)
        {
            if (normalmap.Width != heightMap.Width || normalmap.Height != heightMap.Height) throw new ArgumentException("The images must be the same size");

            Bitmap newImage = new Bitmap(normalmap.Width, normalmap.Height);
            for (int x = 0; x < newImage.Width; x++)
                for (int y = 0; y < newImage.Height; y++)
                {
                    Color c1 = normalmap.GetPixel(x, y);
                    Color c2 = heightMap.GetPixel(x, y);

                    newImage.SetPixel(x, y, Color.FromArgb(255 - c2.R, c1.R, c1.G, c1.B));
                }
            return newImage;

        }

        public static Rectangle SearchRectangleInBitmap(Bitmap image, Color backgroundColor)
        {
            int minX = 0, maxX = 0, minY = 0, maxY = 0;
            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                    if (PixelHelper.CompareTwoColors(image.GetPixel(x, y), backgroundColor) == false)
                    {
                        minY = y;
                        goto End1;
                    }
        End1:
            for (int y = image.Height - 1; y >= 0; y--)
                for (int x = 0; x < image.Width; x++)
                    if (PixelHelper.CompareTwoColors(image.GetPixel(x, y), backgroundColor) == false)
                    {
                        maxY = y;
                        goto End2;
                    }
        End2:
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                    if (PixelHelper.CompareTwoColors(image.GetPixel(x, y), backgroundColor) == false)
                    {
                        minX = x;
                        goto End3;
                    }
        End3:
            for (int x = image.Width - 1; x >= 0; x--)
                for (int y = 0; y < image.Height; y++)
                    if (PixelHelper.CompareTwoColors(image.GetPixel(x, y), backgroundColor) == false)
                    {
                        maxX = x;
                        goto End4;
                    }
        End4:
            return new Rectangle(minX, minY, maxX - minX + 2, maxY - minY + 2);
        }

        //BitmapHelper.BitmapHelp.TransformBlackColorToDarkestBlack(new Bitmap("Fire2.jpg"), 0.3f).Save("Fire3.jpg");
        public static Bitmap TransformBlackColorToDarkestBlack(Bitmap image, float bias = 0.1f)
        {
            Bitmap newImage = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    Color c = image.GetPixel(x, y);
                    if ((c.R + c.G + c.B) / 255.0f < bias)
                        newImage.SetPixel(x, y, Color.Black); //Black
                    else
                        newImage.SetPixel(x, y, Color.FromArgb(255, c.R, c.G, c.B));
                }
            return newImage;
        }

        public static Bitmap TransformWhiteColorToWhitestWhit(Bitmap image, float bias = 0.1f)
        {
            Bitmap newImage = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    Color c = image.GetPixel(x, y);
                    if ((c.R + c.G + c.B) / 255.0f > bias)
                        newImage.SetPixel(x, y, Color.White); //White
                    else
                        newImage.SetPixel(x, y, Color.FromArgb(255, c.R, c.G, c.B));
                }
            return newImage;
        }


        public static Bitmap TransformColorToMaxAlpha(Bitmap image, Color color)
        {
            Bitmap newImage = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    Color c = image.GetPixel(x, y);
                    if (PixelHelper.CompareTwoColors(c, color))
                        newImage.SetPixel(x, y, Color.FromArgb(0, c.R, c.G, c.B)); //Transparent
                    else
                        newImage.SetPixel(x, y, Color.FromArgb(255, c.R, c.G, c.B));
                }
            return newImage;
        }

        public static Bitmap TransformColorToMaxAlpha(Bitmap image, Color color, float colorBias)
        {
            Bitmap newImage = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    Color c = image.GetPixel(x, y);
                    if (PixelHelper.CompareTwoColors(c, color, colorBias))
                        newImage.SetPixel(x, y, Color.FromArgb(0, c.R, c.G, c.B)); //Transparent
                    else
                        newImage.SetPixel(x, y, Color.FromArgb(255, c.R, c.G, c.B));
                }
            return newImage;
        }

        //Wenn ich eine Textur oder Farbpuffer (Beides ist im RGBA-Farbraum) auf den Bildschirm anzeigen will
        //dann muss Alpha überall 255 sein, da sonst Paint oder das WinForm-Control Alpha mit RGB multipliziert, um somit
        //den RGBA-Wert in ein RGB-Wert umzurechnen, was dann aber zu Farbveränderten Stellen führt, wenn Alpha < 255 ist
        public static Bitmap SetAlpha(Bitmap image, int alpha)
        {
            Bitmap newImage = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    Color c = image.GetPixel(x, y);
                    newImage.SetPixel(x, y, Color.FromArgb(alpha, c.R, c.G, c.B));
                }
            return newImage;
        }

        public static Bitmap ScaleColor(Bitmap image, Vector3D scaleFactor)
        {
            Bitmap newImage = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    Vector3D color = PixelHelper.ColorToVector(image.GetPixel(x, y));
                    Color scaledColor = PixelHelper.VectorToColor(Vector3D.Mult(color, scaleFactor));
                    newImage.SetPixel(x, y, scaledColor);
                }
            return newImage;
        }

        public static Bitmap Resize(Bitmap image, int newWidth, int newHeight)
        {
            Bitmap newImage = new Bitmap(newWidth, newHeight);

            //Wenn ich so die Firefly-Searchmask runter skaliere, dann bildet er eine Mischfarbe zwischen der SearchColor und dem Hintergrund
            //Graphics grx = Graphics.FromImage(newImage);
            //grx.DrawImage(image, 0, 0, newWidth, newHeight);
            //grx.Dispose();

            //Point-Sampling
            for (int x = 0; x < newWidth; x++)
                for (int y = 0; y < newHeight; y++)
                {
                    newImage.SetPixel(x,y, image.GetPixel(x * image.Width / newWidth, y * image.Height / newHeight));
                }

            return newImage;
        }

        public static Bitmap AddToColor(Bitmap image, Vector3D add)
        {
            Bitmap newImage = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    Vector3D color = PixelHelper.ColorToVector(image.GetPixel(x, y));
                    Color scaledColor = PixelHelper.VectorToColor(color + add);
                    newImage.SetPixel(x, y, scaledColor);
                }
            return newImage;
        }

        public static List<Vertex2D[]> GetVoronoiPolygons(Size imageSize, List<Point> cellPoints)
        {
            return FortuneVoronio.Voronoi.GetVoronoiPolygons(imageSize, cellPoints);
        }

        public static Bitmap TransformBitmapListToRow(List<Bitmap> images, bool createBorder = false)
        {
            int borderWidth = 5;

            images = images.Where(x => x != null).ToList();
            if (images.Any() == false) return new Bitmap(1, 1);
            int height = images.Max(x => x.Height);
            int width = images.Sum(x => x.Width);
            if (createBorder && images.Count > 1) width += (images.Count - 1) * borderWidth;
            Bitmap imgage = new Bitmap(width, height);

            Graphics grx = Graphics.FromImage(imgage);
            grx.Clear(Color.White);
            int xPos = 0;
            foreach (var img in images)
            {
                //Wenn ich den Rasterizer2D-Test mit OpenGL1 + 3 ausführe, dann bekomme ich ein Zoom bei OpenGL1. Führe ich die 1 nur einzeln aus oder alles im Debug, dann ist kein Zoom zu sehen
                //Dieser Zoom betrifft alle Rasterizer-Tests. Mal hat OpenGL1 ein Zoom, mal die anderen
                //grx.DrawImage(img, new Point(xPos, 0));

                MyDrawImage(imgage, img, new Point(xPos, 0), false);                 
                xPos += img.Width;
                grx.DrawLine(Pens.Black, xPos, 0, xPos, height);

                if (createBorder)
                {
                    grx.FillRectangle(Brushes.Black, xPos, 0, xPos + borderWidth, height);
                    xPos += borderWidth;
                }             
            }
            grx.Dispose();

            return imgage;
        }

        public static Bitmap TransformBitmapListToCollum(List<Bitmap> images)
        {
            images = images.Where(x => x != null).ToList();
            int width = images.Max(x => x.Width);
            int height = images.Sum(x => x.Height);
            Bitmap imgage = new Bitmap(width, height);

            Graphics grx = Graphics.FromImage(imgage);
            grx.Clear(Color.White);
            int yPos = 0;
            foreach (var img in images)
            {
                grx.DrawImage(img, new Point(0, yPos));
                yPos += img.Height;
                grx.DrawLine(Pens.Black, 0, yPos, width, yPos);
            }
            grx.Dispose();

            return imgage;
        }

        public static Bitmap GetBitmapText(string text, float size, Color color, Color backColor, string fontName = "Consolas") //  Lucida Sans Typewriter
        {
            Bitmap image1 = new Bitmap(1, 1);
            image1.SetResolution(96, 96);
            Graphics grx1 = Graphics.FromImage(image1);
            grx1.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
            grx1.TextContrast = 4;
            Font font = new Font(fontName, size, FontStyle.Regular);
            SizeF sizef = grx1.MeasureString(text, font);
            grx1.Dispose();

            Bitmap image = new Bitmap((int)sizef.Width, (int)sizef.Height);
            image.SetResolution(96, 96);
            Graphics grx = Graphics.FromImage(image);
            grx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
            grx.TextContrast = 4;
            grx.Clear(backColor);
            grx.DrawString(text, font, new SolidBrush(color), 0, 0);
            grx.Dispose();

            return image;
        }

        public static Bitmap GetBitmapText(string text, int maxWidth, float textSize)
        {
            Bitmap image1 = new Bitmap(1, 1);
            image1.SetResolution(96, 96);
            Graphics grx1 = Graphics.FromImage(image1);
            grx1.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
            grx1.TextContrast = 4;
            Font font = new Font("Consolas", textSize);
            SizeF sizef = grx1.MeasureString(text, font);
            grx1.Dispose();

            Bitmap result = new Bitmap(Math.Min((int)sizef.Width, maxWidth), (int)sizef.Height);
            result.SetResolution(96, 96);
            Graphics grx = Graphics.FromImage(result);
            grx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
            grx.TextContrast = 4;
            grx.DrawString(text, new Font("Consolas", textSize), Brushes.Black, 0, 0);
            grx.Dispose();
            return result;
        }

        public static Bitmap WriteToBitmap(Bitmap bitmap, string text, Color color)
        {
            bitmap.SetResolution(96, 96);
            Graphics grx = Graphics.FromImage(bitmap);
            grx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
            grx.TextContrast = 4;
            grx.DrawString(text, new Font("Arial", 10), new SolidBrush(color), new PointF(0, 0));
            grx.Dispose();
            return bitmap;
        }

        public static Bitmap ScaleImageUp(Bitmap image, int size) //size = Vergrößerungsfaktor
        {
            Bitmap newImage = new Bitmap(image.Width * size, image.Height * size);
            for (int x = 0; x < newImage.Width; x++)
                for (int y = 0; y < newImage.Height; y++)
                {
                    newImage.SetPixel(x, y, image.GetPixel(x / size, y / size));
                }
            return newImage;
        }

        public static Bitmap ScaleImageDown(Bitmap image, int factor) //factor*factor = Wie viel Pixel werden jeweils zusammengefasst?
        {
            Bitmap newImage = new Bitmap(image.Width / factor, image.Height / factor);
            for (int x = 0; x < newImage.Width; x++)
                for (int y = 0; y < newImage.Height; y++)
                {
                    int r = 0, g = 0, b = 0;
                    for (int x1 = x * factor; x1 < x*factor +factor;x1++)
                        for (int y1 = y * factor; y1 < y * factor + factor; y1++)
                        {
                            Color c = image.GetPixel(x1, y1);
                            r += c.R;
                            g += c.G;
                            b += c.B;
                        }
                    r /= (factor * factor);
                    g /= (factor * factor);
                    b /= (factor * factor);
                    newImage.SetPixel(x, y, Color.FromArgb((byte)r, (byte)g, (byte)b));
                }                
            return newImage;
        }

        public static Bitmap ScaleImageDownWithoutColorInterpolation(Bitmap image, int factor)
        {
            Bitmap newImage = new Bitmap(image.Width / factor, image.Height / factor);
            for (int x = 0; x < newImage.Width; x++)
                for (int y = 0; y < newImage.Height; y++)
                {
                    Color c = image.GetPixel(x * factor, y * factor);
                    newImage.SetPixel(x, y, c);
                }
            return newImage;
        }

        //Ich habe ein Bild, dessen Breite ratioWidth und dessen Höhe ratioHeight ist. Nun möchte ich, dass das Breite-Zu-Höhe verhältnis beibehalten wird
        //und dieses Bild mit maximaler größe in den Rahmen maxWidth * maxHeight eingespannt wird. Da der Rahmen ein anders Seitenverhältnis haben kann, bedeutet dass,
        //das unten oder an der rechte Seite dann ein weißer Rand entstehen wird, wenn mein Bild in den Rahmen gespannt wird
        public static Size GetImageSizeWithSpecificWidthToHeightRatio(int maxWidth, int maxHeight, int ratioWidth, int ratioHeight)
        {
            float xSize = (float)maxWidth / (float)ratioWidth;
            float ySize = (float)maxHeight / (float)ratioHeight;
            float s = Math.Min(xSize, ySize);
            return new Size((int)(ratioWidth * s), (int)(ratioHeight * s));
        }

        public static Bitmap GetEmptyImage(int widht, int height, Color color)
        {
            Bitmap image = new Bitmap(widht, height);
            Graphics grx = Graphics.FromImage(image);
            grx.Clear(color);
            grx.Dispose();
            return image;
        }

        public static Vector3D GetPixelRangeColor(Bitmap image, ImagePixelRange range)
        {
            Vector3D sum = new Vector3D(0, 0, 0);
            for (int x = range.XStart; x < range.XStart + range.Width; x++)
                for (int y = range.YStart; y < range.YStart + range.Height; y++)
                {
                    Vector3D c = PixelHelper.ColorToVector(image.GetPixel(x, y));
                    sum += c;
                }
            return sum;
        }

        public static Bitmap GetImageFromPixelRange(Bitmap image, ImagePixelRange range)
        {
            Bitmap subArea = new Bitmap(range.Width, range.Height);

            for (int x = 0; x < subArea.Width; x++)
                for (int y = 0; y < subArea.Height; y++)
                    subArea.SetPixel(x, y, image.GetPixel(range.XStart + x, range.YStart + y));

            return subArea;
        }

        public static Bitmap GetCubemapImage(Bitmap[] images, bool flipXY)
        {
            int width = images[0].Width;
            int height = images[0].Height;

            Bitmap result = new Bitmap(4 * width, 3 * height);
            Graphics grx = Graphics.FromImage(result);
            //Wenn ich das über DrawImage mache, sieht die von DirectX erzeugte Cubemap falsch aus
            //Vermutlich verwendet sie intern ein Format, womit DrawImage nicht umgehen kann weil sie über Byte-Array-Kopieren intern arbeitet
            //grx.DrawImage(images[0], new Point(2 * width, 1 * height)); //Right
            //grx.DrawImage(images[1], new Point(0 * width, 1 * height)); //Left
            //grx.DrawImage(images[2], new Point(1 * width, 0 * height)); //Top
            //grx.DrawImage(images[3], new Point(1 * width, 2 * height)); //Bottom
            //grx.DrawImage(images[4], new Point(3 * width, 1 * height)); //Back
            //grx.DrawImage(images[5], new Point(1 * width, 1 * height)); //Front
            MyDrawImage(result, images[0], new Point(2 * width, 1 * height), flipXY); //Right
            MyDrawImage(result, images[1], new Point(0 * width, 1 * height), flipXY); //Left
            MyDrawImage(result, images[2], new Point(1 * width, 0 * height), flipXY); //Top
            MyDrawImage(result, images[3], new Point(1 * width, 2 * height), flipXY); //Bottom
            MyDrawImage(result, images[4], new Point(3 * width, 1 * height), flipXY); //Back
            MyDrawImage(result, images[5], new Point(1 * width, 1 * height), flipXY); //Front

            float textSize = 10;
            Brush brush = Brushes.Black;
            var s1 = GetTextPixelSize("Right", textSize);
            var s2 = GetTextPixelSize("Left", textSize);
            var s3 = GetTextPixelSize("Top", textSize);
            var s4 = GetTextPixelSize("Bottom", textSize);
            var s5 = GetTextPixelSize("Back", textSize);
            var s6 = GetTextPixelSize("Front", textSize);
            grx.DrawString("Right", new Font("Consolas", textSize), brush, 2 * width + width / 2 - s1.Width / 2, 1 * height + height / 2 - s1.Height / 2);
            grx.DrawString("Left", new Font("Consolas", textSize), brush, 0 * width + width / 2 - s2.Width / 2, 1 * height + height / 2 - s2.Height / 2);
            grx.DrawString("Top", new Font("Consolas", textSize), brush, 1 * width + width / 2 - s3.Width / 2, 0 * height + height / 2 - s3.Height / 2);
            grx.DrawString("Bottom", new Font("Consolas", textSize), brush, 1 * width + width / 2 - s4.Width / 2, 2 * height + height / 2 - s4.Height / 2);
            grx.DrawString("Back", new Font("Consolas", textSize), brush, 3 * width + width / 2 - s5.Width / 2, 1 * height + height / 2 - s5.Height / 2);
            grx.DrawString("Front", new Font("Consolas", textSize), brush, 1 * width + width / 2 - s6.Width / 2, 1 * height + height / 2 - s6.Height / 2);

            grx.Dispose();

            return result;
        }

        private static void MyDrawImage(Bitmap image1, Bitmap image2, Point point, bool flipXY)
        {
            for (int x = 0; x < image2.Width; x++)
                for (int y = 0; y < image2.Height; y++)
                {
                    if (flipXY)
                        image1.SetPixel(point.X + (image2.Width - x - 1), point.Y + (image2.Height - y - 1), image2.GetPixel(x, y));
                    else
                        image1.SetPixel(point.X + x, point.Y + y, image2.GetPixel(x, y));
                }
                    
        }

        public static SizeF GetTextPixelSize(string text, float textSize)
        {
            Bitmap image = new Bitmap(1, 1);
            Graphics grx1 = Graphics.FromImage(image);
            Font font = new Font("Consolas", textSize);
            SizeF sizef = grx1.MeasureString(text, font);
            grx1.Dispose();
            return sizef;
        }

        public static void WriteIntoBitmapFile(string bitmapFile, Point position, Bitmap bitmap)
        {
            Bitmap master1 = new Bitmap(bitmapFile);
            Bitmap master = new Bitmap(master1);
            master1.Dispose();

            master.SetResolution(96, 96);
            Graphics grx = Graphics.FromImage(master);
            grx.DrawImage(bitmap, position);
            grx.Dispose();

            master.Save(bitmapFile);
            master.Dispose();
        }

        public static Bitmap CreateCopy(Bitmap bitmap)
        {
            Bitmap copy = new Bitmap(bitmap.Width, bitmap.Height);
            for (int x = 0; x < bitmap.Width; x++)
                for (int y = 0; y < bitmap.Height; y++)
                {
                    Color c = bitmap.GetPixel(x, y);
                    copy.SetPixel(x, y, c);
                }
            return copy;
        }

        public static Bitmap ScaleInHSLSpace(Bitmap image, float hueScale, float saturationScale, float ligthnessScale)
        {
            Bitmap copy = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    Color c = image.GetPixel(x, y);
                    float h = c.GetHue() * hueScale;
                    float s = c.GetSaturation() * saturationScale;
                    float l = c.GetBrightness() * ligthnessScale;
                    
                    copy.SetPixel(x, y, PixelHelper.HsvToRgb(h, s, l));
                }
            return copy;
        }

        public static Bitmap GetSubImage(Bitmap image, Rectangle search)
        {
            Bitmap subImage = new Bitmap(search.Width, search.Height);

            for (int x=0;x<subImage.Width;x++)
                for (int y=0;y<subImage.Height;y++)
                {
                    int x1 = search.Left + x;
                    int y1 = search.Top + y;

                    if (x1 >= 0 && x1 < image.Width && y1 >= 0 && y1 < image.Height)
                    {
                        subImage.SetPixel(x, y, image.GetPixel(x1, y1));
                    }
                }

            return subImage;
        }
    }
}
