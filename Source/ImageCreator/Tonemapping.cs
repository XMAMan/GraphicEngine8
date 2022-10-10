using System;
using System.Drawing;
using GraphicMinimal;
using BitmapHelper;

namespace ImageCreator
{
    public class Tonemapping
    {
        public static Bitmap GetImage(ImageBuffer bild, TonemappingMethod verfahren)
        {
            if (bild == null) return new Bitmap(1, 1);
            switch (verfahren)
            {
                case TonemappingMethod.None: return Tonemapping_None(bild);
                case TonemappingMethod.GammaOnly: return Tonemapping_SimpleExponentiell(bild);
                case TonemappingMethod.Reinhard: return Tonemapping_Reinhard(bild);
                case TonemappingMethod.Ward: return Tonemapping_Ward(bild);
                case TonemappingMethod.HaarmPeterDuikersCurve: return Tonemapping_HaarmPeterDuikersCurve(bild);
                case TonemappingMethod.JimHejlAndRichardBurgessDawson: return Tonemapping_JimHejlAndRichardBurgessDawson(bild);
                case TonemappingMethod.Uncharted2Tonemap: return Tonemapping_Uncharted2Tonemap(bild);
                case TonemappingMethod.ACESFilmicToneMappingCurve: return Tonemapping_ACESFilmicToneMappingCurve(bild);                  
            }
            throw new Exception("Tonemappingverahren wird nicht unterstützt");
        }

        #region Tonemapping None
        private static Bitmap Tonemapping_None(ImageBuffer bild)
        {
            Bitmap newBild = new Bitmap(bild.Width, bild.Height);

            for (int x = 0; x < bild.Width; x++)
                for (int y = 0; y < bild.Height; y++)
                    if (bild[x, y] != null)
                    {
                        newBild.SetPixel(x, y, ConvertVectorToColor(bild[x, y]));
                    }
                    else
                    {
                        newBild.SetPixel(x, y, Color.Black);
                    }

            return newBild;
        }
        #endregion

        #region Tonemapping SimpleExponentiell
        private static Bitmap Tonemapping_SimpleExponentiell(ImageBuffer bild)
        {
            Bitmap newBild = new Bitmap(bild.Width, bild.Height);

            for (int x = 0; x < bild.Width; x++)
                for (int y = 0; y < bild.Height; y++)
                    if (bild[x, y] != null)
                    {
                        Vector3D pix = new Vector3D(GammaCorrection(bild[x, y].X),
                                                GammaCorrection(bild[x, y].Y),
                                                GammaCorrection(bild[x, y].Z));

                        newBild.SetPixel(x, y, ConvertVectorToColor(pix));
                    }
                    else
                    {
                        newBild.SetPixel(x, y, Color.Black);
                    }

            return newBild;
        }

        private static float GammaCorrection(float f)
        {
            return (float)Math.Pow(f, 1 / 2.2); //Ich brauch hier nicht clampen, da ConvertVectorToColor das macht
        }

        
        #endregion

        #region Tonemapping Reinhard
        //http://filmicworlds.com/blog/filmic-tonemapping-operators/

        private static Bitmap Tonemapping_Reinhard(ImageBuffer bild)
        {
            Bitmap newBild = new Bitmap(bild.Width, bild.Height);

            for (int x = 0; x < bild.Width; x++)
                for (int y = 0; y < bild.Height; y++)
                    if (bild[x, y] != null)
                    {
                        Vector3D pix = new Vector3D(ReinardPixel(bild[x, y].X),
                                                ReinardPixel(bild[x, y].Y),
                                                ReinardPixel(bild[x, y].Z));

                        newBild.SetPixel(x, y, ConvertVectorToColor(pix));
                    }
                    else
                    {
                        newBild.SetPixel(x, y, Color.Black);
                    }

            return newBild;
        }

        private static float ReinardPixel(float f)
        {
            f = f / (1 + f);
            return (float)Math.Pow(f, 1 / 2.2f);
        }

        #endregion

        #region Tonemapping HaarmPeterDuikersCurve

        private static Bitmap Tonemapping_HaarmPeterDuikersCurve(ImageBuffer bild)
        {
            Bitmap newBild = new Bitmap(bild.Width, bild.Height);

            for (int x = 0; x < bild.Width; x++)
                for (int y = 0; y < bild.Height; y++)
                    if (bild[x, y] != null)
                    {
                        Vector3D pix = new Vector3D(HaarmPeterDuikersCurvePixel(bild[x, y].X),
                                                HaarmPeterDuikersCurvePixel(bild[x, y].Y),
                                                HaarmPeterDuikersCurvePixel(bild[x, y].Z));

                        newBild.SetPixel(x, y, ConvertVectorToColor(pix));
                    }
                    else
                    {
                        newBild.SetPixel(x, y, Color.Black);
                    }

            return newBild;
        }

        private static float[] filmLut = new float[] { 0.01176471f, 0.01176471f, 0.01568628f, 0.01568628f, 0.01960784f, 0.01960784f, 0.01960784f, 0.02352941f, 0.02352941f, 0.02745098f, 0.02745098f, 0.02745098f, 0.03137255f, 0.03529412f, 0.03529412f, 0.03921569f, 0.03921569f, 0.04313726f, 0.04705882f, 0.04705882f, 0.05098039f, 0.05490196f, 0.05882353f, 0.05882353f, 0.0627451f, 0.06666667f, 0.07058824f, 0.07450981f, 0.07450981f, 0.08235294f, 0.09019608f, 0.09411765f, 0.09411765f, 0.09803922f, 0.1019608f, 0.1098039f, 0.1137255f, 0.1215686f, 0.1215686f, 0.1333333f, 0.1333333f, 0.1372549f, 0.145098f, 0.1490196f, 0.1568628f, 0.1607843f, 0.1686275f, 0.1764706f, 0.1803922f, 0.1882353f, 0.1960784f, 0.2039216f, 0.2117647f, 0.2156863f, 0.2235294f, 0.2313726f, 0.2392157f, 0.2470588f, 0.254902f, 0.2627451f, 0.2705882f, 0.2784314f, 0.2862745f, 0.2941177f, 0.2941177f, 0.3019608f, 0.3098039f, 0.3176471f, 0.3254902f, 0.3333333f, 0.3411765f, 0.3529412f, 0.3607843f, 0.3686275f, 0.3764706f, 0.3843137f, 0.3921569f, 0.4039216f, 0.4117647f, 0.4196078f, 0.427451f, 0.4352941f, 0.4470588f, 0.454902f, 0.4627451f, 0.4705882f, 0.4784314f, 0.4901961f, 0.5058824f, 0.5137255f, 0.5137255f, 0.5254902f, 0.5411765f, 0.5490196f, 0.5568628f, 0.5686275f, 0.5764706f, 0.5764706f, 0.5843138f, 0.5921569f, 0.6f, 0.6117647f, 0.6196079f, 0.627451f, 0.6431373f, 0.6509804f, 0.6588235f, 0.6666667f, 0.6745098f, 0.6862745f, 0.6941177f, 0.7019608f, 0.7098039f, 0.7176471f, 0.7254902f, 0.7333333f, 0.7411765f, 0.7450981f, 0.7529412f, 0.7607843f, 0.7686275f, 0.7764706f, 0.7843137f, 0.7882353f, 0.7960784f, 0.8039216f, 0.8117647f, 0.8156863f, 0.8235294f, 0.8313726f, 0.8352941f, 0.8431373f, 0.8470588f, 0.854902f, 0.8588235f, 0.8666667f, 0.8705882f, 0.8784314f, 0.8823529f, 0.8862745f, 0.8901961f, 0.8980392f, 0.9019608f, 0.9058824f, 0.9098039f, 0.9137255f, 0.9176471f, 0.9215686f, 0.9254902f, 0.9294118f, 0.9333333f, 0.9372549f, 0.9411765f, 0.9411765f, 0.945098f, 0.9490196f, 0.9490196f, 0.9490196f, 0.9529412f, 0.9568627f, 0.9568627f, 0.9607843f, 0.9607843f, 0.9647059f, 0.9647059f, 0.9686275f, 0.9686275f, 0.9686275f, 0.9686275f, 0.972549f, 0.972549f, 0.9764706f, 0.9764706f, 0.9764706f, 0.9803922f, 0.9803922f, 0.9803922f, 0.9843137f, 0.9843137f, 0.9843137f, 0.9843137f, 0.9843137f, 0.9882353f, 0.9882353f, 0.9882353f, 0.9882353f, 0.9921569f, 0.9921569f, 0.9921569f, 0.9921569f, 0.9921569f, 0.9921569f, 0.9921569f, 0.9921569f, 0.9960784f, 0.9960784f, 0.9960784f, 0.9960784f, 0.9960784f, 0.9960784f, 0.9960784f, 0.9960784f, 0.9960784f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        //http://filmicworlds.com/blog/filmic-tonemapping-operators/
        private static float HaarmPeterDuikersCurvePixel(float f)
        {
            float ld = 0.002f;
            float linReference = 0.18f;
            float logReference = 444;
            float logGamma = 0.45f;

            float logColor = (float)(Math.Log10(0.4f * f / linReference) / ld * logGamma + logReference) / 1023.0f;
            logColor = Clamp(logColor);

            float filmLutWidth = filmLut.Length;
            float padding = 0.5f / filmLutWidth;

            //  apply response lookup and color grading for target display
            float retColor = filmLut[(int)(PixelHelper.Lerp(padding, 1 - padding, logColor) * filmLutWidth)];
            return retColor;
        }

        private static float Clamp(float x)
        {
            return x < 0 ? 0 : x > 1 ? 1 : x;
        }

        #endregion

        #region Tonemapping JimHejlAndRichardBurgessDawson

        private static Bitmap Tonemapping_JimHejlAndRichardBurgessDawson(ImageBuffer bild)
        {
            Bitmap newBild = new Bitmap(bild.Width, bild.Height);

            for (int x = 0; x < bild.Width; x++)
                for (int y = 0; y < bild.Height; y++)
                    if (bild[x, y] != null)
                    {
                        Vector3D pix = new Vector3D(JimHejlAndRichardBurgessDawsonPixel(bild[x, y].X),
                                                JimHejlAndRichardBurgessDawsonPixel(bild[x, y].Y),
                                                JimHejlAndRichardBurgessDawsonPixel(bild[x, y].Z));

                        newBild.SetPixel(x, y, ConvertVectorToColor(pix));
                    }
                    else
                    {
                        newBild.SetPixel(x, y, Color.Black);
                    }

            return newBild;
        }

        //http://filmicworlds.com/blog/filmic-tonemapping-operators/
        private static float JimHejlAndRichardBurgessDawsonPixel(float f)
        {
            float x = Math.Max(0, f - 0.004f);
            return (x * (6.2f * x + 0.5f)) / (x * (6.2f * x + 1.7f) + 0.06f);
        }

        #endregion

        #region Tonemapping Uncharted2Tonemap

        private static Bitmap Tonemapping_Uncharted2Tonemap(ImageBuffer bild)
        {
            Bitmap newBild = new Bitmap(bild.Width, bild.Height);

            for (int x = 0; x < bild.Width; x++)
                for (int y = 0; y < bild.Height; y++)
                    if (bild[x, y] != null)
                    {
                        Vector3D pix = new Vector3D(Uncharted2TonemapPixel(bild[x, y].X),
                                                Uncharted2TonemapPixel(bild[x, y].Y),
                                                Uncharted2TonemapPixel(bild[x, y].Z));

                        newBild.SetPixel(x, y, ConvertVectorToColor(pix));
                    }
                    else
                    {
                        newBild.SetPixel(x, y, Color.Black);
                    }

            return newBild;
        }

        //http://filmicworlds.com/blog/filmic-tonemapping-operators/
        private static float Uncharted2TonemapPixel(float f)
        {
            float W = 11.2f;

            float exposureBias = 2.0f;
            float curr = Uncharted2(exposureBias * f);

            float whiteScale = 1.0f / Uncharted2(W);
            float color = curr * whiteScale;

            float retColor = (float)Math.Pow(color, 1 / 2.2f);
            return retColor;
        }

        private static float Uncharted2(float x)
        {
            float A = 0.15f;
            float B = 0.50f;
            float C = 0.10f;
            float D = 0.20f;
            float E = 0.02f;
            float F = 0.30f;
            
            return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
        }

        #endregion

        #region Tonemapping ACESFilmicToneMappingCurve

        private static Bitmap Tonemapping_ACESFilmicToneMappingCurve(ImageBuffer bild)
        {
            Bitmap newBild = new Bitmap(bild.Width, bild.Height);

            for (int x = 0; x < bild.Width; x++)
                for (int y = 0; y < bild.Height; y++)
                    if (bild[x, y] != null)
                    {
                        Vector3D pix = new Vector3D(ACESFilmicToneMappingCurvePixel(bild[x, y].X),
                                                ACESFilmicToneMappingCurvePixel(bild[x, y].Y),
                                                ACESFilmicToneMappingCurvePixel(bild[x, y].Z));

                        newBild.SetPixel(x, y, ConvertVectorToColor(pix));
                    }
                    else
                    {
                        newBild.SetPixel(x, y, Color.Black);
                    }

            return newBild;
        }

        //https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
        //Krzysztof Narkowicz says: you need to multiply by exposure before the tone mapping and do the gamma correction after.
        private static float ACESFilmicToneMappingCurvePixel(float x)
        {
            float a = 2.51f;
            float b = 0.03f;
            float c = 2.43f;
            float d = 0.59f;
            float e = 0.14f;
            return GammaCorrection((x * (a * x + b)) / (x * (c * x + d) + e));
        }

        #endregion

        #region Tonemapping Ward

        private static Bitmap Tonemapping_Ward(ImageBuffer bild)
        {
            float tonemapScaling = CalculateToneMapScaling(bild);

            Bitmap image = new Bitmap(bild.Width, bild.Height);
            for (int x = 0; x < bild.Width; x++)
                for (int y = 0; y < bild.Height; y++)
                    if (bild[x, y] != null)
                    {
                        // tonemap
                        Vector3D mapped = bild[x, y] * tonemapScaling * 2;

                        // gamma encode
                        mapped.X = (float)Math.Pow((mapped.X > 0.0 ? mapped.X : 0.0), GAMMA_ENCODE);
                        mapped.Y = (float)Math.Pow((mapped.Y > 0.0 ? mapped.Y : 0.0), GAMMA_ENCODE);
                        mapped.Z = (float)Math.Pow((mapped.Z > 0.0 ? mapped.Z : 0.0), GAMMA_ENCODE);

                        image.SetPixel(x, y, ConvertVectorToColor(mapped));

                        // quantize
                        /*mapped.x = Math.Min((float)Math.Floor((mapped.x * 255.0) + 0.5), 255);
                        mapped.y = Math.Min((float)Math.Floor((mapped.y * 255.0) + 0.5), 255);
                        mapped.z = Math.Min((float)Math.Floor((mapped.z * 255.0) + 0.5), 255);

                        byte r = (byte)(mapped.x <= 255.0 ? mapped.x : 255.0);
                        byte g = (byte)(mapped.y <= 255.0 ? mapped.y : 255.0);
                        byte b = (byte)(mapped.z <= 255.0 ? mapped.z : 255.0);

                        image.SetPixel(x, y, Color.FromArgb(r, g, b));*/
                    }
                    else
                    {
                        image.SetPixel(x, y, Color.Black);
                    }

            return image;
        }

        // guess of average screen maximum brightness
        static float DISPLAY_LUMINANCE_MAX = 200.0f;

        // ITU-R BT.709 standard RGB luminance weighting
        static Vector3D RGB_LUMINANCE = new Vector3D(0.2126f, 0.7152f, 0.0722f);

        // ITU-R BT.709 standard gamma
        static float GAMMA_ENCODE = 0.45f;

        private static float CalculateToneMapScaling(ImageBuffer buffer)
        {
            // calculate log mean luminance
            double logMeanLuminance = 1e-4;

            int pixelCounter = 1;
            double sumOfLogs = 0.0;
            for (int x = 0; x < buffer.Width; x++)
                for (int y = 0; y < buffer.Height; y++)
                    if (buffer[x, y] != null)
                    {
                        double Y = buffer[x, y] * RGB_LUMINANCE; // ITU-R BT.709 standard RGB luminance weighting
                        sumOfLogs += Math.Log10(Y > 1e-4 ? Y : 1e-4);
                        pixelCounter++;
                    }

            logMeanLuminance = Math.Pow(10.0, sumOfLogs / (buffer.Width * buffer.Height) /*pixelCounter*/);

            // (what do these mean again? (must check the tech paper...))
            float a = 1.219f + (float)Math.Pow(DISPLAY_LUMINANCE_MAX * 0.25f, 0.4f);
            float b = 1.219f + (float)Math.Pow(logMeanLuminance, 0.4f);

            return (float)Math.Pow(a / b, 2.5) / DISPLAY_LUMINANCE_MAX;
        }

        #endregion

        private static Color ConvertVectorToColor(Vector3D col)
        {
            if (col == null) return Color.Black;
            if (float.IsNaN(col.X) || float.IsNaN(col.Y) || float.IsNaN(col.Z)) return Color.Black;

            float x = col.X;
            float y = col.Y;
            float z = col.Z;

            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (z < 0) z = 0;

            if (x > 1) x = 1;
            if (y > 1) y = 1;
            if (z > 1) z = 1;

            return Color.FromArgb((int)(x * 255), (int)(y * 255), (int)(z * 255));
        }
    }
}
