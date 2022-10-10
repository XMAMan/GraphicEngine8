using GraphicMinimal;
using System;
using System.Drawing;

namespace GraphicPipelineCPU.DrawingHelper
{
    static class ColorHelper
    {
        //f==0 Return c1, f == 1, Return c2
        public static Color ColorAlphaBlending(Color c1, Color c2, float f)
        {
            //Es spielt für die RGB-Farben erstmal keine Rolle, was für ein Alphawert im Farbpuffer steht. Nur der Alpha-Wert
            //von der Farbe, die gerade geschrieben werden soll ist wichtig, da sie festlegt zu wie viel Prozent sie auf den schon
            //vorhandenen Farbwert addiert wird. Wenn ich aber dann den Farbpuffer als Bitmap im Paint oder als Bitmap im WinForm-Backgroundimage
            //mir anzeige, dann müssen alle Alphawerte 255 sein, da sonst beim Umrechnen eines RGBA-Wertes in ein RGB-Wert
            //die Farbe mit Alpha multipliziert wird. Steht dort dann Alpha < 255 dann führt das zu hellen stellen.
            //Das kann beim Stencilschatten in der ShadowsAndBlending-Scene passieren oder bei der Voronio-MarioTexture bei Rasterizer2D.
            //Wenn ich den Farbpuffer in eine Textur kopiere (Wie ich es bei der Voronio-MarioTexture mache), dann ist
            //der Alphawert schon wichtig, weil er dann beim Zeichnen in den Farbpuffer festlegt, wo der Hintergrund transparent
            //sein soll und wo nicht. Deswegen schreibe ich hier c2.A und nicht 255

            return Color.FromArgb(//(int)(c1.A * (1 - f) + c2.A * f), //So lange ich mit BitmapHelp.SetAlpha arbeite,
                                  c2.A, // gehen hier beide Varianten zur Alpha-Festlegung. Variante 2 finde ich leichter zu verstehen.
                                  (int)(c1.R * (1 - f) + c2.R * f),
                                  (int)(c1.G * (1 - f) + c2.G * f),
                                  (int)(c1.B * (1 - f) + c2.B * f));
        }
        
        public static bool IsBlackColor(this Color color)
        {
            return (color.R + color.G + color.B) / 255f < 0.1f;
        }

        public static bool IsBlackColor(this Vector4D rgba)
        {
            return (rgba.X + rgba.Y + rgba.Z) < 0.1f;
        }

        public static Color ToColor(this Vector4D rgba)
        {
            return Color.FromArgb(
                    (byte)(Math.Max(0, Math.Min(1, rgba.W)) * 255),
                    (byte)(Math.Max(0, Math.Min(1, rgba.X)) * 255),
                    (byte)(Math.Max(0, Math.Min(1, rgba.Y)) * 255),
                    (byte)(Math.Max(0, Math.Min(1, rgba.Z)) * 255)
                    );

        }

        public static Vector4D ToVector4D(this Color color)
        {
            return new Vector4D(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }

        public static Color MultWithBlendFactor(this Color color, Vector4D blendFactor)
        {
            return Color.FromArgb((int)(color.A * blendFactor.W), (int)(color.R * blendFactor.X), (int)(color.G * blendFactor.Y), (int)(color.B * blendFactor.Z));
        }
    }
}
