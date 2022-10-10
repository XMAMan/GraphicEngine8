using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using GraphicGlobal;

namespace TriangleObjectGeneration
{
    static class PerlinNoiseHeightMap
    {
        public static TriangleList CreatePerlinNoiseHeightMap(int width, int height, float bumpFactor)
        {
            string callingParameters = "CreatePerlinNoiseHeightMap:" + width + ":" + height + ":" + bumpFactor;

            float frequence = 50; //Um so höher, um so mehr Huckel pro Wegstrecke
            float[][] heighmap1 = CreateRandomHeightMapParallel(width, height, 255, 0, 1 * frequence, 0.7f, 3);
            float[][] heighmap2 = CreateRandomHeightMapParallel(width, height, 255, 0, 1 * frequence, 1.9f, 3);
            float[][] heighmap = MultiplyHighmap(heighmap1, heighmap2);
            float min = heighmap.Select(x => x.Min()).Min();
            float max = heighmap.Select(x => x.Max()).Max();

            int resolution = 1; // Aller wie viel Pixel wird ein Höhenwert gelesen?
            float maxImageHeight = bumpFactor;

            TriangleList newObject = new TriangleList();

            for (int x = 0; x < heighmap.Length - resolution; x += resolution)
                for (int y = 0; y < heighmap[0].Length - resolution; y += resolution)
                {
                    try
                    {
                        newObject.AddQuad(new Vertex(x, y, (heighmap[x][y] - min) * maxImageHeight / (max - min), x / (float)heighmap.Length, y / (float)heighmap[0].Length),
                                          new Vertex(x, (y + resolution), (heighmap[x][y + resolution] - min) * maxImageHeight / (max - min), x / (float)heighmap.Length, (y + resolution) / (float)heighmap[0].Length),
                                          new Vertex((x + resolution), (y + resolution), (heighmap[x + resolution][y + resolution] - min) * maxImageHeight / (max - min), (x + resolution) / (float)heighmap[0].Length, (y + resolution) / (float)heighmap[0].Length),
                                          new Vertex((x + resolution), y, (heighmap[x + resolution][y] - min) * maxImageHeight / (max - min), (x + resolution) / (float)heighmap.Length, y / (float)heighmap[0].Length));

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

        private static float Noise(int x)
        {
            x = (x << 13) ^ x;
            return (1.0f - ((x * (x * x * 15731) + 1376312589) & 0x7fffffff) / 1073741824.0f);
        }

        private static float CosInterpolate(float v1, float v2, float a)
        {
            var angle = a * (float)Math.PI;
            var prc = (1.0f - Vector3D.LookupCos(angle)) * 0.5f;
            return v1 * (1.0f - prc) + v2 * prc;
        }

        private static float PerlinNoise2D(int seed, float persistence, int octave, float x, float y)
        {
            var freq = (float)Math.Pow(2.0f, octave);
            var amp = (float)Math.Pow(persistence, octave);
            var tx = x * freq;
            var ty = y * freq;
            var txi = (int)tx;
            var tyi = (int)ty;
            var fracX = tx - txi;
            var fracY = ty - tyi;

            var v1 = Noise(txi + tyi * 57 + seed);
            var v2 = Noise(txi + 1 + tyi * 57 + seed);
            var v3 = Noise(txi + (tyi + 1) * 57 + seed);
            var v4 = Noise(txi + 1 + (tyi + 1) * 57 + seed);

            var i1 = CosInterpolate(v1, v2, fracX);
            var i2 = CosInterpolate(v3, v4, fracX);
            var f = CosInterpolate(i1, i2, fracY) * amp;
            return f;
        }

        private static float[][] CreateRandomHeightMapParallel(int heightMapWidth, int heightMapHeight, float maxHeight, int seed, float noiseSize, float persistence, int octaves)
        {
            float[][] _heightMap = new float[heightMapWidth][];
            for (var x = 0; x < heightMapWidth; x++)
            {
                _heightMap[x] = new float[heightMapWidth];
                var tasks = new List<Action>();
                int x1 = x;

                for (var y = 0; y < heightMapHeight; y++)
                {
                    int y1 = y;
                    tasks.Add(() =>
                    {
                        var xf = (x1 / (float)heightMapWidth) * noiseSize;
                        var yf = (y1 / (float)heightMapHeight) * noiseSize;

                        var total = 0.0f;
                        for (var i = 0; i < octaves; i++)
                        {
                            var f = PerlinNoise2D(seed, persistence, i, xf, yf);
                            total += f;
                        }
                        var b = (int)(128 + total * 128.0f);
                        if (b < 0) b = 0;
                        if (b > 255) b = 255;

                        _heightMap[x1][y1] = (b / 255.0f) * maxHeight;
                    });
                }

                System.Threading.Tasks.Parallel.Invoke(tasks.ToArray());
            }

            return _heightMap;
        }

        private static float[][] MultiplyHighmap(float[][] lhs, float[][] rhs)
        {
            float lhsMax = lhs.Select(x => x.Max()).Max();
            float rhsMax = rhs.Select(x => x.Max()).Max();

            var hm = new float[lhs.Length][];

            for (int x = 0; x < lhs.Length; x++)
            {
                hm[x] = new float[lhs[0].Length];
                for (int y = 0; y < lhs[0].Length; y++)
                {
                    var a = lhs[x][y] / lhsMax;
                    var b = 1.0f;
                    if (x >= 0 && y >= 0 && x < rhs.Length && y < rhs[0].Length)
                    {
                        b = rhs[x][y] / rhsMax;
                    }
                    hm[x][y] = a * b * lhsMax;
                }
            }
            return hm;
        }
    }
}
