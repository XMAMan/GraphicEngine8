using BitmapHelper;
using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Rasterizer
{
    //Cached den Aufruf von IGraphicPipeline.GetTextureId für eine Normalmap/Parallaxmap aus Farbbild/Bumpmapbild
    public class BumpTextureCache
    {
        private Func<Bitmap, int> bitmapToIntConverter;
        private Dictionary<INormalSource, int> cache = new Dictionary<INormalSource, int>();

        public BumpTextureCache(Func<Bitmap, int> bitmapToIntConverter)
        {
            this.bitmapToIntConverter = bitmapToIntConverter;
        }

        public int GetEntry(INormalSource normalSource)
        {
            if (normalSource.Type == NormalSource.ObjectData) return default(int); //Normale soll nicht aus Textur kommen

            if (this.cache.ContainsKey(normalSource) == false)
            {
                if (normalSource is NormalMapFromFile && (normalSource as NormalMapFromFile).ConvertNormalMapFromColor) //Erzeuge Bumpmap aus Farbbild
                {
                    string textureFile = normalSource.As<NormalMapFromFile>().FileName;
                    int bumpmapID = this.bitmapToIntConverter(GetBumpmapFromColorCache(textureFile));
                    this.cache.Add(normalSource, bumpmapID);
                }
                else //Erzeuge Bumpmap aus Bumpmapfile
                {
                    string bumpmapFile = null;
                    if (normalSource.Type == NormalSource.Normalmap) bumpmapFile = normalSource.As<NormalFromMap>().FileName;
                    if (normalSource.Type == NormalSource.Parallax) bumpmapFile = normalSource.As<NormalFromParallax>().FileName;

                    if (File.Exists(bumpmapFile))
                    {
                        int bumpmapID = this.bitmapToIntConverter(new Bitmap(bumpmapFile));
                        this.cache.Add(normalSource, bumpmapID);
                    }
                    else
                    {
                        return default(int);
                    }
                }
            }

            return this.cache[normalSource];
        }

        private static Dictionary<string, Bitmap> colorCache = new Dictionary<string, Bitmap>();
        private static Bitmap GetBumpmapFromColorCache(string cacheKey)
        {
            if (colorCache.ContainsKey(cacheKey) == false)
            {
                colorCache[cacheKey] = BitmapHelp.GetBumpmapFromColor(new Bitmap(cacheKey));
            }

            return colorCache[cacheKey];
        }
    }
}
