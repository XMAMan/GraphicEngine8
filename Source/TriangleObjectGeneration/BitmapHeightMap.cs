using System;
using System.Collections.Generic;
using System.Drawing;
using GraphicGlobal;

namespace TriangleObjectGeneration
{
    static class BitmapHeightMap
    {
        //resolution = Aller wie viel Pixel wird ein Höhenwert gelesen?
        //size = Wie hoch ist das Bild im Verhältniss zur Breite/Höhe?
        public static TriangleList CreateSimpleHeightMapFromBitmap(Bitmap image, float size, int resolution)
        {
            string callingParameters = "CreateSimpleHeightMapFromBitmap:" + size + ":" + resolution;

            int[,] heightMapPixel = new int[image.Width, image.Height];

            int minGrayValue = 256;
            int maxGrayValue = 0;
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color C = image.GetPixel(x, y);
                    int grayValue = 255 - (int)(C.R * 0.2989 + C.G * 0.5866 + C.B * 0.1145);

                    if (grayValue < minGrayValue) minGrayValue = grayValue;
                    if (grayValue > maxGrayValue) maxGrayValue = grayValue;

                    heightMapPixel[x,y] = grayValue;
                }
            }

            float maxImageHeight = image.Width > image.Height ? image.Width / size : image.Height / size;

            TriangleList newObject = new TriangleList();
            for (int x = 0; x < image.Width - resolution; x += resolution)
                for (int y = 0; y < image.Height - resolution; y += resolution)
                {
                    try
                    {
                        newObject.AddQuad(new Vertex(x, y, (heightMapPixel[x,y] - minGrayValue) * maxImageHeight / (maxGrayValue - minGrayValue), x / (float)image.Width, y / (float)image.Height),
                                          new Vertex(x, (y + resolution), (heightMapPixel[x,y + resolution] - minGrayValue) * maxImageHeight / (maxGrayValue - minGrayValue), x / (float)image.Width, (y + resolution) / (float)image.Height),
                                          new Vertex((x + resolution), (y + resolution), (heightMapPixel[x + resolution,y + resolution] - minGrayValue) * maxImageHeight / (maxGrayValue - minGrayValue), (x + resolution) / (float)image.Width, (y + resolution) / (float)image.Height),
                                          new Vertex((x + resolution), y, (heightMapPixel[x + resolution,y] - minGrayValue) * maxImageHeight / (maxGrayValue - minGrayValue), (x + resolution) / (float)image.Width, y / (float)image.Height));
                    }
                    catch (Exception)
                    {
                        //...
                    }
                }

            newObject.TransformToCoordinateOrigin();
            newObject.SetNormals();
            newObject.Name = callingParameters;

            return newObject;
        }

        //Die kleine Mammutfunktion :-)
        public static TriangleList CreateHeightMapFromBitmap(Bitmap image, int heightResolution, int maxRectangleCount, float bumpFaktor)
        {
            TriangleList matObj = CreateHeightMapFromBitmapWithoutScaling(image, heightResolution, maxRectangleCount, bumpFaktor, true);
            int imageWidth = image.Width, imageHeight = image.Height;
            do
            {
                if (matObj == null)//Bild ist zur groß, um es in eine Bumpmap umzuwandeln, skaliere es runter
                {
                    imageWidth /= 2;
                    imageHeight /= 2;
                    if (imageWidth < 1 || imageHeight < 1) return CreateHeightMapFromBitmapWithoutScaling(image, heightResolution, maxRectangleCount, bumpFaktor, false);//Bild kann nicht in Bumpmap umgewandelt

                    Bitmap smallImage = new Bitmap(imageWidth, imageHeight);
                    Graphics grx = Graphics.FromImage(smallImage);
                    grx.DrawImage(image, new Rectangle(0, 0, smallImage.Width, smallImage.Height));
                    grx.Dispose();
                    matObj = CreateHeightMapFromBitmapWithoutScaling(smallImage, heightResolution, maxRectangleCount, bumpFaktor, true);
                }
            } while (matObj == null);
            return matObj;
        }


        private static TriangleList CreateHeightMapFromBitmapWithoutScaling(Bitmap image, int heightResolution, int maxRectangleCount, float bumpFactor, bool abortIfRectangleCountIsTooHigh)
        {
            string callingParameters = "CreateHeightMapFromBitmap:" + heightResolution + ":" + maxRectangleCount + ":" + bumpFactor + ":" + abortIfRectangleCountIsTooHigh;

            int x, y;
            int[,] heightMap = new int[image.Width, image.Height];
            int[,] rollbackHeightMap = new int[image.Width, image.Height];//Rollback, falls es zu viele Rechtecke sind

            //Schritt 1: Farbbild in schwarz-weiß-Bild umwandeln
            if (heightResolution > 255) heightResolution = 255;
            if (heightResolution < 1) heightResolution = 1;
            int minGrayValue = -1;
            int maxGrayValue = -1;
            for (y = 0; y < image.Height; y++)
                for (x = 0; x < image.Width; x++)
                {
                    Color C = image.GetPixel(x, y);
                    int grayValue = 255 - (int)(C.R * 0.2989 + C.G * 0.5866 + C.B * 0.1145);

                    if (minGrayValue == -1) minGrayValue = grayValue;
                    if (maxGrayValue == -1) maxGrayValue = grayValue;

                    if (grayValue < minGrayValue) minGrayValue = grayValue;
                    if (grayValue > maxGrayValue) maxGrayValue = grayValue;

                    heightMap[image.Width - x - 1,y] = grayValue;
                }

            //Schritt 2: Schwarzweißbild auf AnzahlHoehenwerte beschränken(Schwarzweißbild Quantisieren)
            for (y = 0; y < image.Height; y++)
                for (x = 0; x < image.Width; x++)
                {
                    int grayValue = heightMap[x,y];

                    grayValue = grayValue - minGrayValue;
                    grayValue = grayValue / ((maxGrayValue - minGrayValue) / heightResolution + 1);
                    if (grayValue >= heightResolution) grayValue = heightResolution - 1;
                    heightMap[x,y] = grayValue;
                    rollbackHeightMap[x,y] = grayValue;
                }

            //Testausgabe des Quantisierungsbildes
            /*Bitmap quantizationImage = new Bitmap(image.Width, image.Height);
            for (y=0;y<image.Height;y++)
                for (x = 0; x < image.Width; x++)
                {
                    int grayValue = heightMap[x][y];
                    grayValue = grayValue * (255 / heightResolution) + (255 / heightResolution) / 2;
                    quantizationImage.SetPixel(x, y, Color.FromArgb(255, grayValue, grayValue, grayValue));
                }
            quantizationImage.Save("test1.bmp");*/

            //Schritt 3: Quantisierungsbild in Rechtecke zerlegen
            bool finish = true;
            Point leftTop = new Point(0, 0);
            Point rightTop = new Point(0, 0);
            int line;
            List<Rectangle> rectangles = new List<Rectangle>();
            do
            {
                finish = true;

                //Schritt 3.1: Suche nächstes noch nicht bearbeitete Pixel
                for (y = 0; y < image.Height; y++)
                    for (x = 0; x < image.Width; x++)
                    {
                        if (heightMap[x,y] >= 0)
                        {
                            finish = false;
                            leftTop.X = x;
                            leftTop.Y = y;
                            goto Found;
                        }
                    }
            Found: ;

                //Schritt 3.2: Gehe vom gefunden Pixel so weit rechts, wie es geht
                for (rightTop = new Point(leftTop.X, leftTop.Y); rightTop.X < image.Width && heightMap[rightTop.X,rightTop.Y] == heightMap[leftTop.X,leftTop.Y]; rightTop.X++) ;
                rightTop.X--;

                //Schritt 3.3: Gehe so viel Zeilen wie es geht runter
                for (line = leftTop.Y; line < image.Height && (line - leftTop.Y) <= (rightTop.X - leftTop.X); line++)
                    for (x = leftTop.X; x <= rightTop.X; x++)
                        if (heightMap[leftTop.X,leftTop.Y] != heightMap[x,line]) goto RectangleFinish;
            RectangleFinish: ;
                line--;

                //Schritt 3.4: Trage gefundenes Rechteck in Liste ein
                Rectangle R = new Rectangle(leftTop.X, leftTop.Y, rightTop.X - leftTop.X + 1, line - leftTop.Y + 1);
                rectangles.Add(R);

                //Schritt 3.5: Markiere gefundenes Rechteck als bearbeitet
                for (y = R.Y; y < R.Y + R.Height; y++)
                    for (x = R.X; x < R.X + R.Width; x++)
                        heightMap[x,y] = -rectangles.Count;

                //Schritt 3.6: Gebe auf, wenn es zu viele Rechtecke sind. Unterteile das Bild dann in lauter Vierecke
                if (rectangles.Count > maxRectangleCount)
                {
                    if (abortIfRectangleCountIsTooHigh) return null;//Bild zu groß, es muss runter skalliert werden(von der Aufruffunktion)
                    rectangles.Clear();
                    for (y = 0; y < image.Height; y++)
                        for (x = 0; x < image.Width; x++)
                            heightMap[x,y] = rollbackHeightMap[x,y];
                    int RSize = (int)Math.Sqrt(maxRectangleCount);

                    int yCount = image.Height / RSize;
                    if (yCount * RSize == image.Height) yCount--;

                    int xCount = image.Width / RSize;
                    if (xCount * RSize == image.Width) xCount--;

                    //Mittelwertfarbe der einzelnen Rechtecke bestimmen
                    for (y = 0; y < yCount; y++)
                        for (x = 0; x < xCount; x++)
                        {
                            int sum = 0;
                            for (int y1 = y * RSize; y1 < y * RSize + RSize; y1++)
                                for (int x1 = x * RSize; x1 < x * RSize + RSize; x1++)
                                {
                                    sum += heightMap[x1,y1];
                                }
                            sum /= RSize * RSize;
                            for (int y1 = y * RSize; y1 < y * RSize + RSize; y1++)
                                for (int x1 = x * RSize; x1 < x * RSize + RSize; x1++)
                                    heightMap[x1,y1] = sum;

                            if (y == yCount - 1)//Untere Reihe
                            {
                                sum = 0;
                                for (int y1 = (y + 1) * RSize; y1 < image.Height; y1++)
                                    for (int x1 = x * RSize; x1 < x * RSize + RSize; x1++)
                                    {
                                        sum += heightMap[x1,y1];
                                    }
                                sum /= RSize * (image.Height - (y + 1) * RSize);
                                for (int y1 = (y + 1) * RSize; y1 < image.Height; y1++)
                                    for (int x1 = x * RSize; x1 < x * RSize + RSize; x1++)
                                        heightMap[x1,y1] = sum;
                            }
                            if (x == xCount - 1)//Rechte Spalte
                            {
                                sum = 0;
                                for (int y1 = y * RSize; y1 < y * RSize + RSize; y1++)
                                    for (int x1 = (x + 1) * RSize; x1 < image.Width; x1++)
                                    {
                                        sum += heightMap[x1,y1];
                                    }
                                sum /= RSize * (image.Width - (x + 1) * RSize);
                                for (int y1 = y * RSize; y1 < y * RSize + RSize; y1++)
                                    for (int x1 = (x + 1) * RSize; x1 < image.Width; x1++)
                                        heightMap[x1,y1] = sum;
                            }
                        }
                    //Testausgabe des neu erstelten Bildes
                    /*Bitmap simpleImage = new Bitmap(image.Width, image.Height);
                    for (y = 0; y < image.Height; y++)
                        for (x = 0; x < image.Width; x++)
                        {
                            int grayValue = heightMap[x][y];
                            grayValue = grayValue * (255 / heightResolution) + (255 / heightResolution) / 2;
                            simpleImage.SetPixel(x, y, Color.FromArgb(255, grayValue, grayValue, grayValue));
                        }
                    simpleImage.Save("test2.bmp");*/
                }

            } while (!finish);

            //Testausgabe der Rechtecke
            Color[] colors = new Color[] { Color.Blue, Color.Red, Color.Black, Color.Brown, Color.Green, Color.Yellow, Color.Violet };
            Bitmap rectangleImage = new Bitmap(image.Width, image.Height);
            int color = 0;
            for (int i = 0; i < rectangles.Count; i++)
            {
                color = (color + 1) % colors.Length;
                for (y = rectangles[i].Y; y < rectangles[i].Y + rectangles[i].Height; y++)
                    for (x = rectangles[i].X; x < rectangles[i].X + rectangles[i].Width; x++)
                    {
                        rectangleImage.SetPixel(x, y, colors[color]);
                    }
            }
            //rectangleImage.Save("test3.bmp");

            //Schritt 4: Nun die Rechteckkanten erstellen
            float S = 1.0f / image.Width * 5;//Sizefaktor
            bumpFactor *= image.Width / 100.0f;
            TriangleList newObject = new TriangleList();
            foreach (Rectangle R in rectangles)
            {
                //Schritt 4.1: Rechteckoberseiten
                newObject.AddQuad(
                    new Vertex(R.X * S, R.Y * S, rollbackHeightMap[R.X,R.Y] * bumpFactor * S, 1 - (float)R.X / image.Width, (float)R.Y / image.Height),
                    new Vertex((R.X + R.Width) * S, R.Y * S, rollbackHeightMap[R.X,R.Y] * bumpFactor * S, 1 - (float)(R.X + R.Width) / image.Width, (float)R.Y / image.Height),
                    new Vertex((R.X + R.Width) * S, (R.Y + R.Height) * S, rollbackHeightMap[R.X,R.Y] * bumpFactor * S, 1 - (float)(R.X + R.Width) / image.Width, (float)(R.Y + R.Height) / image.Height),
                    new Vertex(R.X * S, (R.Y + R.Height) * S, rollbackHeightMap[R.X,R.Y] * bumpFactor * S, 1 - (float)R.X / image.Width, (float)(R.Y + R.Height) / image.Height));

                //Schritt 4.2: Kanten(Rillen) - Oben
                Point start = new Point(R.X, R.Y - 1);
                int startHeight = -1, height = 0;
                for (x = R.X; x < R.X + R.Width; x++)
                {
                    if (start.Y < 0) height = 0; else height = rollbackHeightMap[x,start.Y];//Aktuelle Höhe

                    if (startHeight != height || x == R.X + R.Width - 1)
                    {
                        if (x < R.X + R.Width - 1) start.X = x;

                        if (startHeight != -1 && x < R.X + R.Width - 1)//neue Kante(Unterbrechung)
                        {
                            newObject.AddQuad(
                                new Vertex(start.X * S, (start.Y + 1) * S, height * bumpFactor * S, 1 - (float)start.X / image.Width, (float)(start.Y + 1) / image.Height),
                                new Vertex((x - 0) * S, (start.Y + 1) * S, height * bumpFactor * S, 1 - (float)(x - 1) / image.Width, (float)(start.Y + 1) / image.Height),
                                new Vertex((x - 0) * S, (start.Y + 1) * S, rollbackHeightMap[start.X,start.Y + 1] * bumpFactor * S, 1 - (float)(x - 1) / image.Width, (float)(start.Y + 1) / image.Height),
                                new Vertex(start.X * S, (start.Y + 1) * S, rollbackHeightMap[start.X,start.Y + 1] * bumpFactor * S, 1 - (float)start.X / image.Width, (float)(start.Y + 1) / image.Height));
                        }
                        if (x == R.X + R.Width - 1 && startHeight == height)//Rechte Ecke(Ende)
                        {
                            newObject.AddQuad(
                                new Vertex(start.X * S, (start.Y + 1) * S, height * bumpFactor * S, 1 - (float)start.X / image.Width, (float)(start.Y + 1) / image.Height),
                                new Vertex((x + 1) * S, (start.Y + 1) * S, height * bumpFactor * S, 1 - (float)(x + 1) / image.Width, (float)(start.Y + 1) / image.Height),
                                new Vertex((x + 1) * S, (start.Y + 1) * S, rollbackHeightMap[start.X,start.Y + 1] * bumpFactor * S, 1 - (float)(x + 1) / image.Width, (float)(start.Y + 1) / image.Height),
                                new Vertex(start.X * S, (start.Y + 1) * S, rollbackHeightMap[start.X,start.Y + 1] * bumpFactor * S, 1 - (float)start.X / image.Width, (float)(start.Y + 1) / image.Height));
                        }
                        if (x == R.X + R.Width - 1 && startHeight != height)//Rechte Ecke(Ende mit Unterbrechung)
                        {
                            newObject.AddQuad(
                                new Vertex((x - 0) * S, (start.Y + 1) * S, height * bumpFactor * S, 1 - (float)(x - 0) / image.Width, (float)(start.Y + 1) / image.Height),
                                new Vertex((x + 1) * S, (start.Y + 1) * S, height * bumpFactor * S, 1 - (float)(x + 1) / image.Width, (float)(start.Y + 1) / image.Height),
                                new Vertex((x + 1) * S, (start.Y + 1) * S, rollbackHeightMap[start.X,start.Y + 1] * bumpFactor * S, 1 - (float)(x + 1) / image.Width, (float)(start.Y + 1) / image.Height),
                                new Vertex((x - 0) * S, (start.Y + 1) * S, rollbackHeightMap[start.X,start.Y + 1] * bumpFactor * S, 1 - (float)(x - 0) / image.Width, (float)(start.Y + 1) / image.Height));

                        }
                        start = new Point(x, R.Y - 1);
                        if (start.Y < 0) startHeight = 0; else startHeight = rollbackHeightMap[x,start.Y];//Starthöhe
                    }
                }

                //Schritt 4.3: Kanten(Rillen) - Unten
                start = new Point(R.X, R.Y + R.Height);
                startHeight = -1; height = 0;
                for (x = R.X; x < R.X + R.Width; x++)
                {
                    if (start.Y >= image.Height) height = 0; else height = rollbackHeightMap[x,start.Y];//Aktuelle Höhe

                    if (startHeight != height || x == R.X + R.Width - 1)
                    {
                        if (x < R.X + R.Width - 1) start.X = x;

                        if (startHeight != -1 && x < R.X + R.Width - 1)//neue Kante(Unterbrechung)
                        {
                            newObject.AddQuad(
                                new Vertex(start.X * S, (start.Y - 0) * S, rollbackHeightMap[start.X,start.Y - 1] * bumpFactor * S, 1 - (float)start.X / image.Width, (float)(start.Y - 1) / image.Height),
                                new Vertex((x - 0) * S, (start.Y - 0) * S, rollbackHeightMap[start.X,start.Y - 1] * bumpFactor * S, 1 - (float)(x - 1) / image.Width, (float)(start.Y - 1) / image.Height),
                                new Vertex((x - 0) * S, (start.Y - 0) * S, height * bumpFactor * S, (float)(x - 1) / image.Width, 1 - (float)(start.Y + 1) / image.Height),
                                new Vertex(start.X * S, (start.Y - 0) * S, height * bumpFactor * S, (float)start.X / image.Width, 1 - (float)(start.Y + 1) / image.Height));
                        }
                        if (x == R.X + R.Width - 1 && startHeight == height)//Rechte Ecke(Ende)
                        {
                            newObject.AddQuad(
                                new Vertex(start.X * S, (start.Y - 0) * S, rollbackHeightMap[start.X,start.Y - 1] * bumpFactor * S, 1 - (float)start.X / image.Width, (float)(start.Y - 1) / image.Height),
                                new Vertex((x + 1) * S, (start.Y - 0) * S, rollbackHeightMap[start.X,start.Y - 1] * bumpFactor * S, 1 - (float)(x + 1) / image.Width, (float)(start.Y - 1) / image.Height),
                                new Vertex((x + 1) * S, (start.Y - 0) * S, height * bumpFactor * S, 1 - (float)(x + 1) / image.Width, (float)(start.Y - 1) / image.Height),
                                new Vertex(start.X * S, (start.Y - 0) * S, height * bumpFactor * S, 1 - (float)start.X / image.Width, (float)(start.Y - 1) / image.Height));
                        }
                        if (x == R.X + R.Width - 1 && startHeight != height)//Rechte Ecke(Ende mit Unterbrechung)
                        {   
                            newObject.AddQuad(
                                new Vertex((x - 0) * S, (start.Y - 0) * S, rollbackHeightMap[start.X,start.Y - 1] * bumpFactor * S, 1 - (float)(x - 0) / image.Width, (float)(start.Y - 1) / image.Height),
                                new Vertex((x + 1) * S, (start.Y - 0) * S, rollbackHeightMap[start.X,start.Y - 1] * bumpFactor * S, 1 - (float)(x + 1) / image.Width, (float)(start.Y - 1) / image.Height),
                                new Vertex((x + 1) * S, (start.Y - 0) * S, height * bumpFactor * S, 1 - (float)(x + 1) / image.Width, (float)(start.Y - 1) / image.Height),
                                new Vertex((x - 0) * S, (start.Y - 0) * S, height * bumpFactor * S, 1 - (float)(x - 0) / image.Width, (float)(start.Y - 1) / image.Height));

                        }
                        start = new Point(x, R.Y + R.Height);
                        if (start.Y >= image.Height) startHeight = 0; else startHeight = rollbackHeightMap[x,start.Y];//Starthöhe
                    }
                }

                //Schritt 4.4: Kanten(Rillen) - Links
                start = new Point(R.X - 1, R.Y);
                startHeight = -1; height = 0;
                for (y = R.Y; y < R.Y + R.Height; y++)
                {
                    if (start.X < 0) height = 0; else height = rollbackHeightMap[start.X,y];//Aktuelle Höhe

                    if (startHeight != height || y == R.Y + R.Height - 1)
                    {
                        if (y < R.Y + R.Height - 1) start.Y = y;

                        if (startHeight != -1 && y < R.Y + R.Height - 1)//neue Kante(Unterbrechung)
                        {
                            newObject.AddQuad(
                                new Vertex((start.X + 1) * S, start.Y * S, rollbackHeightMap[start.X + 1,start.Y] * bumpFactor * S, 1 - (float)(start.X + 1) / image.Width, (float)start.Y / image.Height),
                                new Vertex((start.X + 1) * S, (y - 0) * S, rollbackHeightMap[start.X + 1,start.Y] * bumpFactor * S, 1 - (float)(start.X + 1) / image.Width, (float)(y - 1) / image.Height),
                                new Vertex((start.X + 1) * S, (y - 0) * S, height * bumpFactor * S, 1 - (float)(start.X + 1) / image.Width, (float)(y - 1) / image.Height),
                                new Vertex((start.X + 1) * S, start.Y * S, height * bumpFactor * S, 1 - (float)(start.X + 1) / image.Width, (float)start.Y / image.Height));
                        }
                        if (y == R.Y + R.Height - 1 && startHeight == height)//Untere Ecke(Ende)
                        {
                            newObject.AddQuad(
                                new Vertex((start.X + 1) * S, start.Y * S, rollbackHeightMap[start.X + 1,start.Y] * bumpFactor * S, 1 - (float)(start.X + 1) / image.Width, (float)start.Y / image.Height),
                                new Vertex((start.X + 1) * S, (y + 1) * S, rollbackHeightMap[start.X + 1,start.Y] * bumpFactor * S, 1 - (float)(start.X + 1) / image.Width, (float)(start.Y + 1) / image.Height),
                                new Vertex((start.X + 1) * S, (y + 1) * S, height * bumpFactor * S, 1 - (float)(start.X + 1) / image.Width, (float)(y + 1) / image.Height),
                                new Vertex((start.X + 1) * S, start.Y * S, height * bumpFactor * S, 1 - (float)(start.X + 1) / image.Width, (float)start.Y / image.Height));
                        }
                        if (y == R.Y + R.Height - 1 && startHeight != height)//Untere Ecke(Ende mit Unterbrechung)
                        {                
                            newObject.AddQuad(
                                new Vertex((start.X + 1) * S, (y - 0) * S, rollbackHeightMap[start.X + 1,start.Y] * bumpFactor * S, 1 - (float)(start.X + 1) / image.Width, (float)(y - 0) / image.Height),
                                new Vertex((start.X + 1) * S, (y + 1) * S, rollbackHeightMap[start.X + 1,start.Y] * bumpFactor * S, 1 - (float)(start.X + 1) / image.Width, (float)(y + 1) / image.Height),
                                new Vertex((start.X + 1) * S, (y + 1) * S, height * bumpFactor * S, 1 - (float)(start.X + 1) / image.Width, (float)(y + 1) / image.Height),
                                new Vertex((start.X + 1) * S, (y - 0) * S, height * bumpFactor * S, 1 - (float)(start.X + 1) / image.Width, (float)(y - 0) / image.Height));

                        }
                        start = new Point(R.X - 1, y);
                        if (start.X < 0) startHeight = 0; else startHeight = rollbackHeightMap[start.X,y];//Starthöhe
                    }
                }

                //Schritt 4.5: Kanten(Rillen) - Rechts
                start = new Point(R.X + R.Width, R.Y);
                startHeight = -1; height = 0;
                for (y = R.Y; y < R.Y + R.Height; y++)
                {
                    if (start.X >= image.Width) height = 0; else height = rollbackHeightMap[start.X,y];//Aktuelle Höhe

                    if (startHeight != height || y == R.Y + R.Height - 1)
                    {
                        if (y < R.Y + R.Height - 1) start.Y = y;

                        if (startHeight != -1 && y < R.Y + R.Height - 1)//neue Kante(Unterbrechung)
                        {
                            newObject.AddQuad(
                                new Vertex((start.X - 0) * S, start.Y * S, height * bumpFactor * S, 1 - (float)(start.X - 1) / image.Width, (float)start.Y / image.Height),
                                new Vertex((start.X - 0) * S, (y - 0) * S, height * bumpFactor * S, 1 - (float)(start.X - 1) / image.Width, (float)(y - 1) / image.Height),
                                new Vertex((start.X - 0) * S, (y - 0) * S, rollbackHeightMap[start.X - 1,start.Y] * bumpFactor * S, 1 - (float)(start.X - 1) / image.Width, (float)(y - 1) / image.Height),
                                new Vertex((start.X - 0) * S, start.Y * S, rollbackHeightMap[start.X - 1,start.Y] * bumpFactor * S, 1 - (float)(start.X - 1) / image.Width, (float)start.Y / image.Height));
                        }
                        if (y == R.Y + R.Height - 1 && startHeight == height)//Untere Ecke(Ende)
                        {
                            newObject.AddQuad(
                                new Vertex((start.X - 0) * S, start.Y * S, height * bumpFactor * S, 1 - (float)(start.X - 1) / image.Width, (float)start.Y / image.Height),
                                new Vertex((start.X - 0) * S, (y + 1) * S, height * bumpFactor * S, 1 - (float)(start.X - 1) / image.Width, (float)(y + 1) / image.Height),
                                new Vertex((start.X - 0) * S, (y + 1) * S, rollbackHeightMap[start.X - 1,start.Y] * bumpFactor * S, 1 - (float)(start.X - 1) / image.Width, (float)(start.Y + 1) / image.Height),
                                new Vertex((start.X - 0) * S, start.Y * S, rollbackHeightMap[start.X - 1,start.Y] * bumpFactor * S, 1 - (float)(start.X - 1) / image.Width, (float)start.Y / image.Height));
                        }
                        if (y == R.Y + R.Height - 1 && startHeight != height)//Untere Ecke(Ende mit Unterbrechung)
                        {
                            newObject.AddQuad(
                                new Vertex((start.X - 0) * S, (y - 0) * S, height * bumpFactor * S, 1 - (float)(start.X - 1) / image.Width, (float)(y - 0) / image.Height),
                                new Vertex((start.X - 0) * S, (y + 1) * S, height * bumpFactor * S, 1 - (float)(start.X - 1) / image.Width, (float)(y + 1) / image.Height),
                                new Vertex((start.X - 0) * S, (y + 1) * S, rollbackHeightMap[start.X - 1,start.Y] * bumpFactor * S, 1 - (float)(start.X - 1) / image.Width, (float)(y + 1) / image.Height),
                                new Vertex((start.X - 0) * S, (y - 0) * S, rollbackHeightMap[start.X - 1,start.Y] * bumpFactor * S, 1 - (float)(start.X - 1) / image.Width, (float)(y - 0) / image.Height));

                        }
                        start = new Point(R.X + R.Width, y);
                        if (start.X >= image.Width) startHeight = 0; else startHeight = rollbackHeightMap[start.X,y];//Starthöhe
                    }
                }

            }

            newObject.TransformToCoordinateOrigin();
            newObject.SetNormals();
            newObject.Name = callingParameters;

            return newObject;
        }
    }
}
