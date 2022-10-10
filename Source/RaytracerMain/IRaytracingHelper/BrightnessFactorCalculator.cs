using GraphicGlobal;
using GraphicMinimal;
using RaytracingColorEstimator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RaytracerMain
{
    class BrightnessFactorCalculator
    {
        public static float GetBrightnessFactor(IPixelEstimator pixelEstimator, int imageWidth, int imageHeight, int sampleCount)
        {
            if (pixelEstimator.CreatesLigthPaths) throw new Exception("Bitte ein Verfahren nutzen, was auf Pixelbasis arbeitet");

            IRandom rand = new Rand(0);
            int pixelCount = 50;
            List<float> colors = new List<float>();
            for (int i = 0; i < pixelCount; i++)
            {
                int pixX = (int)(rand.NextDouble() * imageWidth);
                int pixY = (int)(rand.NextDouble() * imageHeight);

                Vector3D colorSum = null;
                for (int j = 0; j < sampleCount; j++)
                {
                    Vector3D color = pixelEstimator.GetFullPathSampleResult(pixX, pixY, rand).RadianceFromRequestetPixel;
                    if (color != null)
                    {
                        if (colorSum == null) colorSum = new Vector3D(0, 0, 0);
                        colorSum += color;
                    }
                    else
                    {
                        break;
                    }
                }
                if (colorSum != null)
                {
                    colorSum /= sampleCount;
                    if (colorSum.Max() > 1e-6f)
                    {
                        colors.Add(colorSum.Length());
                    }
                }
            }

            colors = colors.Where(x => x != 0).ToList();
            colors.Sort();
            if (colors.Any() == false) return 1;
            float median = colors[colors.Count / 2];

            return (1.0f / median) * 1;
        }
    }
}
