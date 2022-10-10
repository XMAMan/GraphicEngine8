using BitmapHelper;
using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper.Helper3D;
using System;
using System.Drawing;

//Spezialthemenkomplex: Texturefilter / Antialiasing (17.3.2015)
//Gelerntes Wissen: Ein Texturefilter ist eine Funktion, welche für ein gegebenes Pixel sagt, welche Texel ich mit welcher Wichtung auslesen muss
//                  Antialiasing ist ein Verfahren, um ein Signal mit hoher Frequenz richtig abzutasten. 
//                      Beispiele: Ich habe ein Ton mit 20 MHz. Mein Micro tastet aber nur alle 15Mhz ein Sample(Lautstärkewert) ab. Der Ton wird dadruch verzehrt, da ich die Tonwelle nicht richtig erkenne. 
//                                 Eine Vektorgrafiklinie soll gezeichent werden. Manche Pixel werden aber nur zur Hälfte/Viertel von Linie bedeckt.
//                  Verfahren: Ich berechne den Farbwert von Subpixeln (z.B. 4 Subpixel pro Bildschirmpixel) und bilde den Durchschnitt davon -> Verfahren nentn sich Quincunx
//                             Stochatisches Verfahren Ich wähle N zufällige Subpixel aus und bilde davon den Mittelwert.
//Beim Antialiasing wird also der gesamte Bildschirm betrachtet und verbessert (Kantenglättung, Haarlinien sichtbar machen).
//Beim Texturfilter wird eine einzelne Textur (Dreieck) verbessert
//Texturfilter verbessern also texturen(Wichtig). Antialiasing(Unwichtig) verbessert lediglich die Kanten und macht wenn überhaupt nur beim Raytracing Sinn.
//Arten von Texturfiltern
// +Anwendungsfall Minification (Textur ist ganz weit weg)
//    -Point    = Wähle für jedes Pixel genau ein Texel. Nimm das nächste (Integerrundung)
//    -Bilenear = Nimm für jedes Pixel 4 Texel. Die Texel werden entsprechend gewichtet (Summe der Wichtungen ist 1)
//    -Trilenar = Bestimme die MIP-Stufe als float-Zahl. Nimm nun die nächst größere/kleinere MIP-Stufe. Lese aus beiden MIP-Stufen Bilenear und wichte mit Abstand zur MIP-Stufe.
//    -Ansitrop = Das Pixel wird in den Texturraum projektziert. Dadurch erhalte ich ein konvexes Viereck (Footprint genannt). Der Mittelwert dieses Vierecks ist der Farbwert des Pixels.
//          -Algorithmus: Footprint Assembly = Das konvexe Viereck wird durch ein Parallelogram angenähert. Über dieses 
//                        Parallelogram werden N (Lange Paralleloramm-Seite / Kurze Seite) Vierecke gelegt. Jedes Viereck wird 
//                        aus der MIP-Map mit bilenearen Filter ausgelesen.
//                        Offene Frage: Was bedeutet TAP-Stufe?
//    -MIP-Map  = Verkleinerte Versionen von einer Textur. Wird bei Minification benötigt, damit ich schneller den Mittelwert von ein Viereck mit der Kantenlänge 2^N bilden kann
//                Verfahren: 
//                  Mittelwertbildung von jeweils 4 Pixeln
//                  Fouriersynthese
// +Anwendungsfall Magnification (Ich stehe nah vor einer Wand)
//   -Nearest  = Wähle nächstes Pixel. Führt zur deutlichen sichtbaren Texelkanten.
//   -Bilenear = Wähle 4 Texel pro Pixel. Texelkanten werden abgeschächt.
//http://alt.3dcenter.org/artikel/grafikfilter/ -> Was sind Texturfilter überhaupt? Bei CPU + Raytracer einbauen
//http://de.wikipedia.org/wiki/Antialiasing_(Computergrafik) -> Ähnliches Themengebiet wie Texturfilter. Bei CPU + Raytracer einbauen(vielleicht auch OpenGL/DirectX)


namespace GraphicPipelineCPU.Textures
{
    //Texture für RGBA-Werte (Farbe oder Bumpmap)
    class ColorTexture : ITexture2D
    {
        //Intern muss ich als Pixel-Datentyp Color und kein Float nehmen, da die Texturen/Farbpuffer bei OpenGL/DirectX auch nur 8 Bit pro Farbkanal haben
        //Ich würde ein anderes Bild erhalten, wenn ich hier mit float statt Color rechne.
        private Color[,] image = null;

        public ColorTexture(int id, int width, int height)
        {
            this.Id = id;
            this.image = new Color[width, height];
        }

        public ColorTexture(int id, Bitmap bitmap)
        {
            this.Id = id;
            this.image = BitmapHelp.LoadBitmap(bitmap);
        }

        public int Id { get; private set; } //Wenn diese Texture in ein Framebuffer hängt, dann kann ich die ID nutzen, um die Texturdaten auszulesen
        public int Width
        {
            get
            {
                return this.image.GetLength(0);
            }
        }

        public int Height
        {
            get
            {
                return this.image.GetLength(1);
            }
        }

        public Color this[int x, int y]
        {
            get
            {
                return this.image[x, y];
            }
            set
            {
                this.image[x, y] = value;
            }
        }

        public void SetForEachTexel(Color value)
        {
            for (int x = 0; x < this.Width; x++)
                for (int y = 0; y < this.Height; y++)
                {
                    this.image[x, y] = value;
                }
        }

        public Size GetSize()
        {
            return new Size(this.image.GetLength(0), this.image.GetLength(1));
        }

        public Bitmap GetAsBitmap()
        {
            return BitmapHelp.ConvertImageArrayToBitmap(this.image);
        }

        public Color TextureMappingPoint(TextureMode mode, float texcoordU, float texcoordV)
        {
            return GetPixel(mode, (int)(texcoordU * this.image.GetLength(0)), (int)(texcoordV * this.image.GetLength(1)));
        }

        public Color TextureMappingLinear(TextureMode mode, float texcoordU, float texcoordV)
        {
            return GetColorFrom4Texels(mode, texcoordU * this.image.GetLength(0), texcoordV * this.image.GetLength(1));
        }

        //point = Texturkoodinaten von den Punkt, den die Pixelmitte sieht
        //footprint = Texturkoodinaten von den 4 Eckpunkten eines Pixels
        public Color TextureMapping(TextureMode mode, Vector2D point, Footprint footprint, TextureFilter filter)
        {
            if (filter == TextureFilter.Point)
            {
                return TextureMappingPoint(mode, point.X, point.Y);
                //return TextureMappingPoint(image, footprint.CenterPoint.x, footprint.CenterPoint.y);
            }

            if (filter == TextureFilter.Linear)
            {
                return GetColorFrom4Texels(mode, point.X * this.image.GetLength(0), point.Y * this.image.GetLength(1));
            }

            if (filter == TextureFilter.Anisotroph)
            {
                //Testausgabe des Footprints für ein einzelnen Pixel
                bool createTestImage = false;
                if (createTestImage && footprint.WindowPixelPosition.X == 51 && footprint.WindowPixelPosition.Y == 0)
                {
                    GetFootprintAsBitmap(mode, footprint, 300).Save($"..\\Texel_{footprint.WindowPixelPosition.X}_{footprint.WindowPixelPosition.Y}.bmp");
                }

                return GetFootprintColorFromAllSamplePointsInside(mode, footprint, 16);
            }

            throw new ArgumentException("Invalid Argument " + filter.ToString());
        }

        private Color GetFootprintColorFromAllSamplePointsInside(TextureMode mode, Footprint footprint, int sampleCount)
        {
            Random rand = new Random(footprint.WindowPixelPosition.X * footprint.WindowPixelPosition.Y);
            Vector3D sum = new Vector3D(0, 0, 0);
            for (int i = 0; i < sampleCount; i++)
            {
                Vector2D point = footprint.GetRandomPoint(rand);
                sum += PixelHelper.ColorToVector(GetPixel(mode, (int)(point.X * this.Width), (int)(point.Y * this.Height)));
            }
            return PixelHelper.VectorToColor(sum / sampleCount);
        }

        //Erzeugt ein outputSize*outputSize großes Bitmap
        private Bitmap GetFootprintAsBitmap(TextureMode mode, Footprint footprint, int outputSize)
        {
            Bitmap image = new Bitmap(outputSize, outputSize);
            float scale = outputSize / Math.Max(footprint.BoundingRectangle.Width, footprint.BoundingRectangle.Height); //Um diesen Faktor muss ich das Footprint vergrößern damit es ins Bild passt
            Vector2D leftTop = new Vector2D(footprint.BoundingRectangle.Left, footprint.BoundingRectangle.Top);

            for (int x=0;x<image.Width;x++)
                for (int y=0;y<image.Height;y++)
                {
                    //x,y = Bitmapkoordinate
                    Vector2D p = new Vector2D(x / scale + leftTop.X, y / scale + leftTop.Y); //Footprintkoordinate

                    if (footprint.IsPointInside(p.X, p.Y))
                    {
                        Color color = GetPixel(mode, (int)(p.X * this.Width), (int)(p.Y * this.Height));
                        image.SetPixel(x, y, color);
                    }else
                    {
                        image.SetPixel(x, y, Color.Green); //Außerhalb des Footprints wird grün gezeichnet
                    }
                }

            return image;
        }

 
        private Color GetColorFrom4Texels(TextureMode mode, float texU, float texV)
        {
            float xWeight = GetLeftWeight(texU);
            float yWeight = GetLeftWeight(texV);

            int leftX = GetLeftIndex(texU);
            int topY = GetLeftIndex(texV);

            Vector3D c1 = PixelHelper.ColorToVector(GetPixel(mode, leftX, topY)) * xWeight * yWeight;
            Vector3D c2 = PixelHelper.ColorToVector(GetPixel(mode, leftX + 1, topY)) * (1 - xWeight) * yWeight;
            Vector3D c3 = PixelHelper.ColorToVector(GetPixel(mode, leftX, topY + 1)) * xWeight * (1 - yWeight);
            Vector3D c4 = PixelHelper.ColorToVector(GetPixel(mode, leftX + 1, topY + 1)) * (1 - xWeight) * (1 - yWeight);

            return PixelHelper.VectorToColor(c1 + c2 + c3 + c4);
        }

        private static int GetLeftIndex(float f)
        {
            int index = (int)f;
            f -= index;
            if (f >= 0.5f)
                return index;
            else
                return index - 1;
        }

        private static float GetLeftWeight(float f)
        {
            f -= (int)f;
            if (f >= 0.5f)
                return 1 - (f - 0.5f);
            else
                return 1 - (f + 0.5f);
        }

        private Color GetPixel(TextureMode mode, int x, int y)
        {
            if (mode == TextureMode.Repeat)
            {
                if (x >= 0)
                    x = x % this.image.GetLength(0);
                else
                    x = this.image.GetLength(0) - (-x) % this.image.GetLength(0) - 1;

                if (y >= 0)
                    y = y % this.image.GetLength(1);
                else
                    y = this.image.GetLength(1) - (-y) % this.image.GetLength(1) - 1;
                return this.image[x, y];
            }
            if (mode == TextureMode.Clamp)
            {
                if (x < 0) x = 0;
                if (x >= this.image.GetLength(0)) x = this.image.GetLength(0) - 1;
                if (y < 0) y = 0;
                if (y >= this.image.GetLength(1)) y = this.image.GetLength(1) - 1;
                return this.image[x, y];
            }
            throw new Exception("Invalid Argument " + mode.ToString());
        }
    }
}
