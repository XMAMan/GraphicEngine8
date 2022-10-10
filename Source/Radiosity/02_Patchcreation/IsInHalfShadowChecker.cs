using GraphicGlobal;
using IntersectionTests;
using RayObjects;
using RaytracingLightSource;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radiosity._02_Patchcreation
{
    class IsInHalfShadowChecker
    {
        private readonly LightSourceSampler lightSourceSampler;
        private readonly IntersectionFinder intersectionFinder;
        private readonly int sampleCountForShadowTest;

        public IsInHalfShadowChecker(LightSourceSampler lightSourceSampler, IntersectionFinder intersectionFinder, int sampleCountForShadowTest)
        {
            this.lightSourceSampler = lightSourceSampler;
            this.intersectionFinder = intersectionFinder;
            this.sampleCountForShadowTest = sampleCountForShadowTest;
        }

        public bool IsInHalfShadow(IFlatObject flatObject, IFlatObject flatObjectSmall, IRandom rand)
        {
            var shadowResult = GetIrradiancePoints(flatObject, flatObjectSmall, this.sampleCountForShadowTest, rand);

            float p = shadowResult.IsInHalfShadow() ? shadowResult.GetVarianze() : 0;

            return p > 1;
        }

        //Schaut wie viel Prozent der Schattenstrahlen flatObjectSmall treffen
        private ShadowRayTestCounter GetIrradiancePoints(IFlatObject flatObject, IFlatObject flatObjectSmall, int samplePointCount, IRandom rand)
        {
            ShadowRayTestCounter counter = new ShadowRayTestCounter();
            for (int i = 0; i < samplePointCount; i++)
            {
                counter.Add(GetIrradianceForSurfacePoint(flatObject, flatObjectSmall.GetRandomPointOnSurface(rand), rand));
            }
            return counter;
        }

        //Sendet zur jeder Lichtquelle in der Scene ein Schattenstrahl und ermittelt, wie sehr point im Schatten liegt oder nicht
        private ShadowRayTestCounter GetIrradianceForSurfacePoint(IFlatObject flatObjectBig, SurfacePoint point, IRandom rand)
        {
            ShadowRayTestCounter counter = new ShadowRayTestCounter();
            float contributionSum = 0;
            var toLightDirections = lightSourceSampler.GetRandomPointOnLightList(point.Position, rand); //Muliple-Ligthsource-Sampling
            List<IIntersectableRayDrawingObject> rayHeighList = new List<IIntersectableRayDrawingObject>();
            foreach (var toLightDirection in toLightDirections)
            {
                //Shadow-Ray-Test
                var lightPoint = intersectionFinder.GetIntersectionPoint(new Ray(point.Position, toLightDirection.DirectionToLightPoint), (float)rand.NextDouble(), flatObjectBig);

                if (lightPoint != null && lightPoint.IntersectedRayHeigh == toLightDirection.LightSource)
                {
                    float distanceSqrt = (point.Position - lightPoint.Position).SquareLength();
                    float lambda1 = point.Normal * toLightDirection.DirectionToLightPoint;
                    float lambda2 = lightPoint.OrientedFlatNormal * (-toLightDirection.DirectionToLightPoint);
                    if (distanceSqrt > 0.01f && lambda1 > 0 && lambda2 > 0)
                    {
                        rayHeighList.Add(toLightDirection.LightSource);
                        counter.NoShadow++;

                        float geometryTerm = lambda1 * lambda2 / distanceSqrt;

                        var contribution = geometryTerm * lightSourceSampler.GetEmissionForEyePathHitLightSourceDirectly(lightPoint, point.Position, toLightDirection.DirectionToLightPoint) / toLightDirection.PdfA;
                        contributionSum += contribution;
                    }
                }
                else
                {
                    counter.InShadow++;
                }
            }

            counter.Irradiance.Add(contributionSum);
            return counter;
        }
    }

    class ShadowRayTestCounter
    {
        public int NoShadow = 0;
        public int InShadow = 0;
        public List<float> Irradiance = new List<float>();

        public void Add(ShadowRayTestCounter counter)
        {
            this.NoShadow += counter.NoShadow;
            this.InShadow += counter.InShadow;
            this.Irradiance.AddRange(counter.Irradiance);
        }

        public bool IsInHalfShadow()
        {
            float p = (float)this.NoShadow / Math.Max((float)(this.InShadow + this.NoShadow), 1);
            float bias = 0.2f;
            return p > bias && p < 1 - bias;
        }

        public float GetVarianze()
        {
            float max = this.Irradiance.Max();
            float avg = this.Irradiance.Select(x => x / max).Average();
            return this.Irradiance.Sum(x => Sqr(x / max - avg));
        }

        private static float Sqr(float f)
        {
            return f * f;
        }

        public float GetPercent()
        {
            float p = (float)this.NoShadow / Math.Max((float)(this.InShadow + this.NoShadow), 1);
            float bias = 0.2f;
            if (p < bias || p > 1 - bias) p = 0;
            return p;
        }
    }
}
