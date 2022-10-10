using System;
using System.Collections.Generic;
using System.Linq;
using IntersectionTests;
using GraphicMinimal;
using RayObjects;
using GraphicGlobal;
using RaytracingLightSource.Basics;
using RaytracingLightSource.Basics.LightDirectionSampler;

namespace RaytracingLightSource
{
    //Oberflächenlicht(Rechteck), was ein teil Diffuse und ein Teil in Richtung SpotDirection strahlt
    class SurfaceWithSpot : IRayLightSource
    {
        public IIntersectableRayDrawingObject RayDrawingObject { get; private set; }
        public float EmittingSurfaceArea { get; private set; }
        public float Emission { get; private set; }
        public float EmissionPerArea { get; protected set; }

        private readonly IEnumerable<IFlatRandomPointCreator> flats;
        private readonly IUniformRandomSurfacePointCreator uniformSampling;  //Für das Lighttracing
        private readonly ILightDirectionSampler lightDirectionSampler;       //Für das Lighttracing
        private readonly RectangleDirectLightSourceSampler directLightingSampler;

        private readonly bool useWithoutDiffuseDictionSamplerForLightPathCreation = false;
        private readonly ILightDirectionSampler withoutDiffuseDirectionSampler;

        public SurfaceWithSpot(IEnumerable<IFlatRandomPointCreator> flats)
        {
            this.RayDrawingObject = flats.First().RayHeigh;
            this.flats = flats;
            var flatListSampler = new FlatSurfaceListSamplingUniform(flats);
            this.uniformSampling = flatListSampler;
            this.EmittingSurfaceArea = flatListSampler.SurfaceArea;
            this.Emission = (this.RayDrawingObject.Propertys.RaytracingLightSource as IRaytracingLightSource).Emission;
            this.EmissionPerArea = this.Emission / this.EmittingSurfaceArea;

            Vector3D diffuseDirection = this.flats.First().Normal;
            Vector3D spotDirection = (this.RayDrawingObject.Propertys.RaytracingLightSource as SurfaceWithSpotLightDescription).SpotDirection;
            float spotCutoff = (this.RayDrawingObject.Propertys.RaytracingLightSource as SurfaceWithSpotLightDescription).SpotCutoff;
            float spotMix = (this.RayDrawingObject.Propertys.RaytracingLightSource as SurfaceWithSpotLightDescription).SpotMix;
            if (spotDirection == null) spotDirection = diffuseDirection;

            this.lightDirectionSampler = new MixTwoFunctionsLightDirectionSampler(new UniformOverHalfSphereLightDirectionSampler(diffuseDirection), new UniformOverThetaRangeLightDirectionSampler(spotDirection, 0, spotCutoff * Math.PI / 180), spotMix);

            this.useWithoutDiffuseDictionSamplerForLightPathCreation = (this.RayDrawingObject.Propertys.RaytracingLightSource as SurfaceWithSpotLightDescription).UseWithoutDiffuseDictionSamplerForLightPathCreation;
            this.withoutDiffuseDirectionSampler = new UniformOverThetaRangeLightDirectionSampler(spotDirection, 0, spotCutoff * Math.PI / 180);

            this.directLightingSampler = new RectangleDirectLightSourceSampler(flats, spotMix, new UniformOverThetaRangeLightDirectionSampler(-spotDirection, 0, spotCutoff * Math.PI / 180));

            //var mix1 = new MixTwoFunctionsLightDirectionSampler(new UniformOverThetaRangeLightDirectionSampler(25 * Math.PI / 180, 70 * Math.PI / 180), new UniformOverThetaRangeLightDirectionSampler(0, 1 * Math.PI / 180), 0.80f);
            //this.lightDirectionSampler = new MixTwoFunctionsLightDirectionSampler(new UniformOverHalfSphereLightDirectionSampler(), mix1, 0.30f); //0.90

            //this.lightDirectionSampler = new MixTwoFunctionsLightDirectionSampler(new UniformOverThetaRangeLightDirectionSampler(25 * Math.PI / 180, 70 * Math.PI / 180), new UniformOverThetaRangeLightDirectionSampler(0, 5 * Math.PI / 180), 0.40f);

            //this.lightDirectionSampler = new UniformOverThetaRangeLightDirectionSampler(0, 5 * Math.PI / 180);


        }

        public float GetDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            return this.directLightingSampler.GetDirectLightingPdfA(eyePoint, pointOnLight.Position);
        }

        public float GetMultipleDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            return this.directLightingSampler.GetDirectLightingPdfA(eyePoint, pointOnLight.Position);
        }

        public List<DirectLightingSampleResult> GetRandomPointOnLightList(Vector3D eyePoint, IRandom rand)
        {
            return new List<DirectLightingSampleResult>() { this.directLightingSampler.SampleToLightDirection(eyePoint, rand) };
        }

        public float GetEmissionForEyePathHitLightSourceDirectly(IntersectionPoint pointOnLight, Vector3D eyePoint, Vector3D directionFromEyeToLightPoint)
        {
            if (pointOnLight.FlatNormal * directionFromEyeToLightPoint < 0)
                return this.EmissionPerArea * this.lightDirectionSampler.GetPdfW(-directionFromEyeToLightPoint);
            return 0;
        }

        //Non-Uniformes Sampling für das DirectLighting
        public DirectLightingSampleResult GetRandomPointOnLight(Vector3D eyePoint, IRandom rand)
        {
            return this.directLightingSampler.SampleToLightDirection(eyePoint, rand);
        }

        public float GetPdfWFromLightDirectionSampling(IntersectionPoint pointOnLight, Vector3D direction)
        {
            return this.lightDirectionSampler.GetPdfW(direction);
        }

        public float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight)
        {
            return 1.0f / this.EmittingSurfaceArea;
        }

        //Uniform-Sampling fürs Lightpath erstellen
        public virtual SurfaceLightPointForLightPathCreation GetRandomPointForLightPathCreation(IRandom rand)
        {
            var point = this.uniformSampling.GetRandomPointOnSurface(rand);

            LightDirectionSamplerResult directionSample;
            if (this.useWithoutDiffuseDictionSamplerForLightPathCreation)
                directionSample = this.withoutDiffuseDirectionSampler.SampleDirection(rand.NextDouble(), rand.NextDouble(), rand.NextDouble());
            else
                directionSample = this.lightDirectionSampler.SampleDirection(rand.NextDouble(), rand.NextDouble(), rand.NextDouble());

            return new SurfaceLightPointForLightPathCreation()
            {
                PointOnLight = IntersectionPoint.CreatePointOnLight(point.Position, point.Color, point.Normal, (IIntersecableObject)point.PointSampler, this.RayDrawingObject),
                Direction = directionSample.Direction,
                PdfA = point.PdfA,//1.0f / this.EmittingSurvaceArea,
                PdfW = directionSample.PdfW,
                EmissionPerArea = this.EmissionPerArea * directionSample.PdfW,
            };
        }
    }
}
