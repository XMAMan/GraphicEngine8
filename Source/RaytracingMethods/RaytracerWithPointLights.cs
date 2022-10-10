using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using RaytracingBrdf;
using RaytracingBrdf.SampleAndRequest;
using RaytracingColorEstimator;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RaytracingMethods
{
    public class RaytracerWithPointLights : IPixelEstimator
    {
        private PixelRadianceCalculator pixelRadianceCalculator;
        private List<RayLightSource> rayLightSources;
        private RayVisibleTester visibleTester;

        public bool CreatesLigthPaths { get; } = false;

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.pixelRadianceCalculator = PixelRadianceCreationHelper.CreatePixelRadianceCalculator(data,
                new SubPathSettings()
                {
                    EyePathType = PathSamplingType.NoMedia,
                },
                new FullPathSettings());

            this.visibleTester = new RayVisibleTester(this.pixelRadianceCalculator.IntersectionFinder, null);

            //Die einzelnen Dreicke von den Raytracing-Lichtquellen
            var rayobjectRayLightSources = this.pixelRadianceCalculator.GetIIntersecableObjectList()
                .Where(x => (x.RayHeigh.Propertys as ObjectPropertys).RaytracingLightSource != null)
                .ToList();

            this.rayLightSources = data.DrawingObjects
                .Where(x => x.DrawingProps.RaytracingLightSource != null && rayobjectRayLightSources.Any(y => y.RayHeigh.Propertys.RaytracingLightSource == x.DrawingProps.RaytracingLightSource))
                .Select(x => new RayLightSource(
                    x.DrawingProps.RaytracingLightSource,
                    x.GetBoundingBoxFromObject().Center, //Der Raytracer nimmt das Zentrum der Boundingbox
                    rayobjectRayLightSources.First(y => y.RayHeigh.Propertys.RaytracingLightSource == x.DrawingProps.RaytracingLightSource).RayHeigh
                    ))
                .ToList();
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            Vector3D color = GetPixelColor(x, y);
            return new FullPathSampleResult() { RadianceFromRequestetPixel = color != null ? GetPixelColor(x, y) : new Vector3D(0, 0, 0), MainPixelHitsBackground = color == null };
        }

        private Vector3D GetPixelColor(int x, int y)
        {
            IntersectionPoint point = this.pixelRadianceCalculator.GetFirstEyePoint(x, y);
            if (point == null) return null;

            var brdfPoint = GetRefractedPoint(new BrdfPoint(point, Vector3D.Normalize(point.Position - this.pixelRadianceCalculator.GlobalProps.Camera.Position), float.NaN, float.NaN), this.pixelRadianceCalculator.GlobalProps.RecursionDepth);
            return GetColorWithRaytracerFormelAndPointLights(brdfPoint);
        }

        //Suche den nächsten Diffuse-Punkt indem der Strahl depth gebrochen/reflektiert wird
        private BrdfPoint GetRefractedPoint(BrdfPoint point, int depth)
        {
            if (point.SurfacePoint.BrdfModel != BrdfModel.TextureGlass && point.SurfacePoint.BrdfModel != BrdfModel.Mirror) return point;

            float refractionIndexCurrentMedium = 1;
            float refractionIndexNextMedium = point.SurfacePoint.RefractionIndex;
            bool isOutside = true;

            BrdfPoint runningPoint = new BrdfPoint(point.SurfacePoint, point.DirectionToThisPoint, refractionIndexCurrentMedium, refractionIndexNextMedium);
            for (int i = 0; i < depth; i++)
            {
                var r = runningPoint.Brdf.SampleDirection(runningPoint.DirectionToThisPoint, 0, 0, 1); //u3 = Materialauswahl; Nimm immer den Refracted-Ray
                if (r == null) return runningPoint;
                var newDirection = new BrdfSampleEvent() { Brdf = r.BrdfWeightAfterSampling, ExcludedObject = runningPoint.SurfacePoint.IntersectedObject, RayWasRefracted = r.RayWasRefracted, Ray = new Ray(runningPoint.SurfacePoint.Position, r.SampledDirection) };

                if (newDirection.RayWasRefracted) isOutside = !isOutside;

                var p = this.pixelRadianceCalculator.IntersectionFinder.GetIntersectionPoint(newDirection.Ray, 0, newDirection.ExcludedObject, float.MaxValue);
                if (p == null) return runningPoint;
                if (isOutside)
                {
                    refractionIndexCurrentMedium = 1;
                    refractionIndexNextMedium = p.RefractionIndex;
                }
                else
                {
                    refractionIndexCurrentMedium = p.RefractionIndex;
                    refractionIndexNextMedium = 1;
                }
                runningPoint = new BrdfPoint(p, newDirection.Ray.Direction, refractionIndexCurrentMedium, refractionIndexNextMedium);
                if (runningPoint.SurfacePoint.BrdfModel != BrdfModel.TextureGlass && runningPoint.SurfacePoint.BrdfModel != BrdfModel.Mirror) return runningPoint;
            }

            return runningPoint;
        }

        private Vector3D GetColorWithRaytracerFormelAndPointLights(BrdfPoint point)
        {
            if (point.SurfacePoint.IsLocatedOnLightSource) return point.SurfacePoint.Color;

            //Vector3D camToPoint = point.Position - this.camera.Position;
            //Vector3D camToPointDirection = Vector3D.Normalize(camToPoint);
            //float geometryTermToCamera = Math.Max(this.camera.Forward * camToPointDirection, 0.0f) * Math.Max(point.ShadedNormal * (-camToPointDirection), 0.0f) / camToPoint.QuadratBetrag();

            float diffuseBrdf = 1.0f / (float)Math.PI;

            Vector3D sum = new Vector3D(0, 0, 0);
            foreach (var light in this.rayLightSources)
            {
                Vector3D toLight = light.Position - point.SurfacePoint.Position;
                Vector3D toLightDirection = Vector3D.Normalize(toLight);
                var lightPoint = this.visibleTester.GetPointOnIntersectableLight(point.SurfacePoint, 0, light.Position, light.RayHeigh);
                if (lightPoint != null)
                {
                    float geometryTerm = Math.Max(point.SurfacePoint.ShadedNormal * toLightDirection, 0.0f) * Math.Max(lightPoint.ShadedNormal * (-toLightDirection), 0.0f) / (lightPoint.Position - point.SurfacePoint.Position).SquareLength();

                    sum += point.SurfacePoint.Color * point.Albedo * point.DiffusePortion * geometryTerm * diffuseBrdf * light.LightSource.Emission;
                }
            }
            return sum;
        }

        class RayLightSource
        {
            public Vector3D Position { get; private set; }
            public IRaytracingLightSource LightSource;
            public IIntersectableRayDrawingObject RayHeigh { get; private set; }

            public RayLightSource(IRaytracingLightSource source, Vector3D position, IIntersectableRayDrawingObject rayHeigh)
            {
                this.LightSource = source;
                this.Position = position;
                this.RayHeigh = rayHeigh;
            }

        }
    }
}
