using System;
using System.Collections.Generic;
using GraphicMinimal;
using System.Drawing;

namespace BitmapHelper
{
    public static class PixelHelper
    {
        //f muss im Bereich von min bis max liegen
        public static float GetZeroToOne(float min, float max, float f)
        {
            return (f - min) / (max - min);
        }

        public static float Lerp(float start, float end, float fFromZeroToOne)
        {
            return start * (1 - fFromZeroToOne) + end * fFromZeroToOne;
        }

        public static Color VectorToColor(Vector3D color)
        {
            color.X = Clamp(color.X, 0, 1);
            color.Y = Clamp(color.Y, 0, 1);
            color.Z = Clamp(color.Z, 0, 1);
            return Color.FromArgb((byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255));
        }

        public static string VectorToHexColor(Vector3D color)
        {
            color.X = Clamp(color.X, 0, 1);
            color.Y = Clamp(color.Y, 0, 1);
            color.Z = Clamp(color.Z, 0, 1);
            return "#" + ((byte)(color.X * 255)).ToString("X2") + ((byte)(color.Y * 255)).ToString("X2") + ((byte)(color.Z * 255)).ToString("X2");
        }

        public static Vector3D Clamp(Vector3D f, float min, float max)
        {
            return new Vector3D(Clamp(f.X, min, max), Clamp(f.Y, min, max), Clamp(f.Z, min, max));
        }

        public static float Clamp(float f, float min, float max)
        {
            if (f < min) f = min;
            if (f > max) f = max;
            return f;
        }

        private static float Clamp(float x)
        {
            return x < 0 ? 0 : x > 1 ? 1 : x;
        }

        public static Vector3D ColorToVector(Color color)
        {
            return new Vector3D(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
        }

        public static float ColorToGray(Vector3D color)
        {
            return color * new Vector3D(0.2126f, 0.7152f, 0.0722f);
        }

        public static bool IsColorString(string colorString)
        {
            return (colorString.Length == 7 || colorString.Length == 9) && colorString[0] == '#';
        }

        public static bool IsGrayColor(Vector3D rgb)
        {
            return rgb.X > 0 && rgb.X < 1 && rgb.X == rgb.Y && rgb.X == rgb.Z;
        }

        public static Color StringToColor(string colorString)
        {
            int[] color = new int[] { 255, 255, 255, 255 };

            color[0] = int.Parse(colorString.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
            color[1] = int.Parse(colorString.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
            color[2] = int.Parse(colorString.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
            if (colorString.Length == 9) color[3] = int.Parse(colorString.Substring(7, 2), System.Globalization.NumberStyles.HexNumber);
            return Color.FromArgb(color[3], color[0], color[1], color[2]);
        }

        public static float[] StringToColorArray(string colorString)
        {
            float[] color = new float[] { 1, 1, 1, 1 };  // RGBA
            color[0] = int.Parse(colorString.Substring(1, 2), System.Globalization.NumberStyles.HexNumber) / 255.0f;
            color[1] = int.Parse(colorString.Substring(3, 2), System.Globalization.NumberStyles.HexNumber) / 255.0f;
            color[2] = int.Parse(colorString.Substring(5, 2), System.Globalization.NumberStyles.HexNumber) / 255.0f;
            if (colorString.Length == 9) color[3] = int.Parse(colorString.Substring(7, 2), System.Globalization.NumberStyles.HexNumber) / 255.0f;
            return color;
        }

        //Blender verwendet Gamma-Corrected Hex-Strings. Will ich also von ein Hex-String von mir zu Blender-Hex übersetzen, brauche ich diese Funktion hier
        public static string ColorStringToGammaCorrectColorString(string colorString)
        {
            Vector3D v = StringToColorVector(colorString);
            string hex = VectorToHexColor(new Vector3D(
                (float)Math.Pow(Clamp(v.X), 1 / 2.2565282907036257037449337419823),
                (float)Math.Pow(Clamp(v.Y), 1 / 2.2565282907036257037449337419823),
                (float)Math.Pow(Clamp(v.Z), 1 / 2.2565282907036257037449337419823)));
            return hex;
        }

        public static Vector3D StringToColorVector(string colorString)
        {
            float[] color = StringToColorArray(colorString);
            return new Vector3D(color[0], color[1], color[2]);
        }

        //DirectX speichert den Tiefenpuffer im UNorm-Format
        public static int ConvertFloatToUnsignedNormalizedInteger(float f, int bitCount)
        {
            f = Clamp(f, 0, 1);
            int maxInt = 1 << bitCount;
            return (int)(f * maxInt);
        }

        //DirectX speichert den Tiefenpuffer im UNorm-Format
        public static float ConvertUnsignedNormalizedIntegerToFloat(int f, int bitCount)
        {
            int maxInt = 1 << bitCount;
            return (float)f / maxInt;
        }

        public static bool CompareTwoColors(Color c1, Color c2)
        {
            return c1.R == c2.R && c1.G == c2.G && c1.B == c2.B;
        }

        public static bool CompareTwoColors(Color c1, Color c2, float bias)
        {
            Vector3D cv1 = new Vector3D(c1.R / 255f, c1.G / 255f, c1.B / 255f);
            Vector3D cv2 = new Vector3D(c2.R / 255f, c2.G / 255f, c2.B / 255f);
            return (cv1 - cv2).Length() < bias;
        }

        //f muss im Bereich von 0 bis 1 liegen. Farbe ergibt schönen Regenbogeneffekt
        public static Color ConvertFloatToColor(float f)
        {
            f = Math.Max(0, Math.Min(f, 1));
            //return Color.FromArgb(CalcRainbowColor(f), CalcRainbowColor(f + 2 / 3f), CalcRainbowColor(f + 1 / 3f));
            return HsvToRgb(f * 255, 1, 1);
        }

        //https://stackoverflow.com/questions/1335426/is-there-a-built-in-c-net-system-api-for-hsv-to-rgb
        //h = Farbton; Geht von 0 bis 360
        //S = Sättigung; Geht von 0 bis 1
        //V = Helligkeit; Geht von 0 bis 1
        public static Color HsvToRgb(double h, double S, double V)
        {
            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            int r = Clamp0To255((int)(R * 255.0));
            int g = Clamp0To255((int)(G * 255.0));
            int b = Clamp0To255((int)(B * 255.0));
            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        private static int Clamp0To255(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }

        //https://www.symcon.de/forum/threads/32155-Einhundert-RGB-Farben-auf-Slider
        //f muss im Bereich von 0 bis 1 liegen
        public static Color GetRainbowColor(float f)
        {
            f = Math.Max(0, Math.Min(f, 1));
            return Color.FromArgb(CalcRainbowColor(f), CalcRainbowColor(f + 2 / 3f), CalcRainbowColor(f + 1 / 3f));
        }

        private static int CalcRainbowColor(float f)
        {
            double result = 255 * Math.Cos(2 * Math.PI * f) + 127;
            return (int)(Math.Max(0, Math.Min(result, 255)));
        }

        public static Vector3D[] GetColorArray(int stepsPerWavelength)
        {
            List<Vector3D> colors = new List<Vector3D>();
            for (int r=0;r<=stepsPerWavelength;r++)
                for (int g = 0; g <= stepsPerWavelength; g++)
                    for (int b = 0; b <= stepsPerWavelength; b++)
                    {
                        colors.Add(new Vector3D(r / (float)stepsPerWavelength, g / (float)stepsPerWavelength, b / (float)stepsPerWavelength));
                    }
            return colors.ToArray();
        }
    }
}
