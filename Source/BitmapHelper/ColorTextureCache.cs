using System;
using System.Collections.Generic;
using System.IO;
using GraphicMinimal;
using System.Drawing;

namespace BitmapHelper
{
    public class ColorTextureCache
    {
        private Func<Bitmap, int> bitmapToIntConverter;
        private Dictionary<string, IColorTextureCacheEntry> cache = new Dictionary<string, IColorTextureCacheEntry>();

        
        public ColorTextureCache(Func<Bitmap, int> bitmapToIntConverter)
        {
            this.bitmapToIntConverter = bitmapToIntConverter;
        }

        public IColorTextureCacheEntry GetEntry(IColorSource color)
        {
            string key = color.ToString();
            if (this.cache.ContainsKey(key) == false)
            {
                if (color.Type == ColorSource.ColorString)
                {
                    this.cache.Add(key, new ColorEntry(color.As<ColorFromRgb>().Rgb));
                }
                else if (color.Type == ColorSource.Texture)
                {
                    string textureFile = color.As<ColorFromTexture>().TextureFile;

                    if (File.Exists(textureFile) == false) throw new FileNotFoundException("Datei nicht gefunden: " + textureFile, textureFile);
                    if (BitmapHelp.IsHdrImageName(textureFile))
                        this.cache.Add(textureFile, new ColorEntry(new Vector3D(1, 1, 1)));
                    else
                        this.cache.Add(textureFile, new TextureEntry(this.bitmapToIntConverter(new Bitmap(textureFile))));
                }
                else
                {
                    this.cache.Add(key, new ColorEntry(new Vector3D(1, 1, 1)));
                }
            }

            return this.cache[key];
        }
    }

    public interface IColorTextureCacheEntry { }

    public class ColorEntry : IColorTextureCacheEntry
    {
        public float[] Color;

        public ColorEntry(Vector3D rgb)
        {
            this.Color = new float[] { rgb.X, rgb.Y, rgb.Z, 1 };
        }
    }

    public class TextureEntry : IColorTextureCacheEntry
    {
        public int TextureId;

        public TextureEntry(int textureId)
        {
            this.TextureId = textureId;
        }
    }
}
