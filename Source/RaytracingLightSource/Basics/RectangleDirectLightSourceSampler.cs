using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using RayObjects;
using RayTracerGlobal;
using RaytracingLightSource.Basics.LightDirectionSampler;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RaytracingLightSource.Basics
{
    //Wenn die Lichtquelle ein SurfaceWithSpot oder ein ImportanceSurfaceWithSpot ist, dann kann hiermit für ein gegebenen EyePoint ein Richtungsvektor zur Lichtquelle gesampelt werden
    class RectangleDirectLightSourceSampler
    {
        private readonly IIntersectableRayDrawingObject rayDrawingObject;
        private readonly IUniformRandomSurfacePointCreator uniformSampling; //Für diffuses Licht
        private readonly float spotMixFactor;
        private readonly ILightDirectionSampler spotDirectionSampler;
        private readonly Plane lightPlane;
        private readonly float diffusePdfA;

        //flats = Menge von Dreiecken/Quads, welche alle in einer Ebene liegen
        //spotMixFactor = 0..1 Wie viel Prozent wird in SpotRichtung gestrahlt? Der Rest geht geht diffuse
        //In diese Richtung zeigt der Spot
        //Hiermit wird SpotDirection gesampelt (Die Normale zeigt aber entgegengesetzt zur Lichtquellen-Normale da ich hiermit vom EyePoint zur Lichtquelle sampeln will)
        public RectangleDirectLightSourceSampler(IEnumerable<IFlatRandomPointCreator> flats, float spotMixFactor, ILightDirectionSampler spotDirectionSampler)
        {
            this.rayDrawingObject = flats.First().RayHeigh;
            this.uniformSampling = new FlatSurfaceListSamplingUniform(flats);
            this.spotDirectionSampler = spotDirectionSampler;
            this.spotMixFactor = spotMixFactor;
            this.lightPlane = new Plane(flats.First().Normal, flats.First().CenterOfGravity);
            this.diffusePdfA = 1.0f / this.uniformSampling.SurfaceArea;
        }

        public DirectLightingSampleResult SampleToLightDirection(Vector3D eyePoint, IRandom rand)
        {
            if (rand.NextDouble() < this.spotMixFactor)
            {
                var spotDirection = this.spotDirectionSampler.SampleDirection(rand.NextDouble(), rand.NextDouble(), double.NaN);
                this.lightPlane.GetIntersectionPointWithRay(new Ray(eyePoint, spotDirection.Direction), out float distance);

                float distSqr = Math.Max(distance * distance, MagicNumbers.MinAllowedPathPointSqrDistance);
                float pdfA = spotDirection.PdfW * Math.Abs((-spotDirection.Direction) * this.lightPlane.Normal) / distSqr;

                return new DirectLightingSampleResult()
                {
                    DirectionToLightPoint = spotDirection.Direction,
                    PdfA = this.spotMixFactor * pdfA + (1 - this.spotMixFactor) * diffusePdfA,
                    LightSource = this.rayDrawingObject,
                    IsLightIntersectable = true
                };
                
            }else
            {
                var diffusePoint = this.uniformSampling.GetRandomPointOnSurface(rand);
                return new DirectLightingSampleResult()
                {
                    DirectionToLightPoint = Vector3D.Normalize(diffusePoint.Position - eyePoint),
                    PdfA = GetDirectLightingPdfA(eyePoint, diffusePoint.Position),
                    LightSource = this.rayDrawingObject,
                    IsLightIntersectable = true
                };
            }
        }

        public float GetDirectLightingPdfA(Vector3D eyePoint, Vector3D pointOnLight)
        {
            Vector3D toLightDirection = Vector3D.Normalize(pointOnLight - eyePoint);
            float distanceSqrt = Math.Max((pointOnLight - eyePoint).SquareLength(), MagicNumbers.MinAllowedPathPointSqrDistance);
            float spotPdfA = this.spotDirectionSampler.GetPdfW(toLightDirection) * Math.Abs((-toLightDirection) * this.lightPlane.Normal) / distanceSqrt;
            return (1 - this.spotMixFactor) * diffusePdfA + this.spotMixFactor * spotPdfA;
        }
    }
}
