using System;

namespace RaytracingColorEstimator
{
    //Zählt für jedes Pixel, wie viel Photonen es empfangen hat und berechent draus den Suchradius
    public class PixelPhotonenCounter
    {
        public int FrameIterationCount = 0;
        private readonly PixelHitpoint[,] pixelSize;

        public PixelPhotonenCounter(PixelRadianceCalculator pixelRadianceCalculator)
        {
            this.pixelSize = new PixelHitpoint[pixelRadianceCalculator.ScreenWidth, pixelRadianceCalculator.ScreenHeight];
            for (int x = 0; x < pixelRadianceCalculator.ScreenWidth; x++)
                for (int y = 0; y < pixelRadianceCalculator.ScreenHeight; y++)
                {
                    this.pixelSize[x, y] = new PixelHitpoint(pixelRadianceCalculator.GetPixelFootprint(x, y));

                    /*float pixelFootprintArea = pixelRadianceCalculator.GetExactPixelFootprintArea(x, y);
                    float radius = (float)Math.Sqrt(pixelFootprintArea / Math.PI);
                    this.pixelSize[x, y] = new PixelHitpoint(radius);*/
                }
        }

        public PixelPhotonenCounter(PixelPhotonenCounter copy)
        {
            this.FrameIterationCount = copy.FrameIterationCount;
            this.pixelSize = new PixelHitpoint[copy.pixelSize.GetLength(0), copy.pixelSize.GetLength(1)];
            for (int x = 0; x < this.pixelSize.GetLength(0); x++)
                for (int y = 0; y < this.pixelSize.GetLength(1); y++)
                {
                    this.pixelSize[x, y] = new PixelHitpoint(copy.pixelSize[x,y]);
                }
        }

        public float GetSearchRadiusForPixel(int x, int y)
        {
            return this.pixelSize[x, y].GetSearchRadius(this.FrameIterationCount);
        }

        public void AddPhotonCounterForPixel(int x, int y, int photonsToAddCount)
        {
            //https://github.com/roosephu/amcmcppm/blob/master/main.cpp
            if (photonsToAddCount > 0)
            {
                float radiusAlpha = 0.7f;
                float g = (this.pixelSize[x, y].ReceivedPhotons + 1) / (this.pixelSize[x, y].ReceivedPhotons + 1 / radiusAlpha);
                this.pixelSize[x, y].RadiusSqrt *= g;

                if (this.pixelSize[x, y].RadiusSqrt == 0) throw new Exception("Photonmapsuchradius darf nicht 0 sein");
            }

            this.pixelSize[x, y].ReceivedPhotons += photonsToAddCount;
        }

        class PixelHitpoint
        {
            private readonly float pixelFootprint;
            public int ReceivedPhotons = 0;
            public float RadiusSqrt;

            public PixelHitpoint(float pixelFootprint)
            {
                this.pixelFootprint = pixelFootprint;
                this.RadiusSqrt = pixelFootprint * pixelFootprint;

                //if (this.RadiusSqrt == 0) throw new Exception("Photonmapsuchradius darf nicht 0 sein");
            }

            public PixelHitpoint(PixelHitpoint copy)
            {
                this.pixelFootprint = copy.pixelFootprint;
                this.ReceivedPhotons = copy.ReceivedPhotons;
                this.RadiusSqrt = copy.RadiusSqrt;
            }

            public float GetSearchRadius(int frameIteration)
            {
                //float radiusAlpha = 0.7f;
                //return this.pixelFootprint * 1 / (float)Math.Pow((float)(this.ReceivedPhotons + 1) / 100.0f, 0.5f * (1 - radiusAlpha));

                //if (this.RadiusSqrt == 0) throw new Exception("Photonmapsuchradius darf nicht 0 sein");

                return (float)Math.Sqrt(this.RadiusSqrt);

                //return this.pixelFootprint * (float)Math.Pow(frameIteration, 0.5f * (1 - radiusAlpha)); //SmallVCM / SmallUPBP

                //float radiusSurf = mSurfRadiusInitial * std::pow(effectiveIteration, (mSurfRadiusAlpha - 1) * 0.5f);

                //return this.pixelFootprint * 2 * Math.Min((1 / (float)Math.Pow(this.ReceivedPhotons, 0.2f)), 1); //Geht zwar aber sieht trotzdem noch recht streuselig aus
                //return this.pixelFootprint * 4 * Math.Min((1 / (float)Math.Pow(frameIteration, 0.2f)), 1);
            }
        }
    }
}
