using BitmapHelper;
using GraphicMinimal;
using System;
using System.Drawing;

namespace TextureHelper
{
    public class ColorTexture
    {
        private Color[,] image = null; // Anstelle von Bitmap muss Color[,] verwendet werden da GDI+ kein parallelen Zugriff von mehreren Threads erlaubt [x,y]

        public ColorTexture(Bitmap bitmap)
        {
            this.image = BitmapHelp.LoadBitmap(bitmap);
        }

        public ColorTexture(string fileName)
        {
            this.image = BitmapHelp.LoadBitmap(fileName);
        }

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

        public Color ReadColorFromTexture(float texcoordU, float texcoordV, bool interpolate, TextureMode textureMode)
        {
            int texU = (int)(texcoordU * this.image.GetLength(0));
            int texV = (int)(texcoordV * this.image.GetLength(1));

            if (interpolate == false)
            {
                return GetPixel(this.image, texU, texV, textureMode);
            }

            return GetColorFrom4Texels(this.image, texcoordU * this.image.GetLength(0), texcoordV * this.image.GetLength(1), textureMode);
        }

        public Color ReadColorFromTexture(float texcoordU, float texcoordV, bool interpolate, TextureMode textureMode, int texUAdd, int texVAdd)
        {
            int texU = (int)(texcoordU * this.image.GetLength(0)) + texUAdd;
            int texV = (int)(texcoordV * this.image.GetLength(1)) + texVAdd;

            if (interpolate == false)
            {
                return GetPixel(this.image, texU, texV, textureMode);
            }

            return GetColorFrom4Texels(this.image, texcoordU * this.image.GetLength(0), texcoordV * this.image.GetLength(1), textureMode);
        }

        private static Color GetColorFrom4Texels(Color[,] texture, float texU, float texV, TextureMode textureMode)
        {
            float xWeight = GetLeftWeight(texU);
            float yWeight = GetLeftWeight(texV);

            int leftX = GetLeftIndex(texU);
            int topY = GetLeftIndex(texV);

            Vector3D c1 = PixelHelper.ColorToVector(GetPixel(texture, leftX, topY, textureMode)) * xWeight * yWeight;
            Vector3D c2 = PixelHelper.ColorToVector(GetPixel(texture, leftX + 1, topY, textureMode)) * (1 - xWeight) * yWeight;
            Vector3D c3 = PixelHelper.ColorToVector(GetPixel(texture, leftX, topY + 1, textureMode)) * xWeight * (1 - yWeight);
            Vector3D c4 = PixelHelper.ColorToVector(GetPixel(texture, leftX + 1, topY + 1, textureMode)) * (1 - xWeight) * (1 - yWeight);

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

        private static Color GetPixel(Color[,] texture, int texU, int texV, TextureMode textureMode)
        {
            switch (textureMode)
            {
                case TextureMode.Clamp:
                    {
                        if (texU >= texture.GetLength(0)) texU = texture.GetLength(0) - 1;
                        if (texU < 0) texU = 0;
                        if (texV >= texture.GetLength(1)) texV = texture.GetLength(1) - 1;
                        if (texV < 0) texV = 0;

                        return texture[texU, texV];
                    }
                case TextureMode.Repeat:
                    {
                        if (texU >= 0)
                            texU %= texture.GetLength(0);
                        else
                            texU = texture.GetLength(0) - (-texU) % texture.GetLength(0) - 1;

                        if (texV >= 0)
                            texV %= texture.GetLength(1);
                        else
                            texV = texture.GetLength(1) - (-texV) % texture.GetLength(1) - 1;

                        return texture[texU, texV];
                    }
            }

            throw new Exception("Unknown TextureMode: " + textureMode);
        }
    }
}
