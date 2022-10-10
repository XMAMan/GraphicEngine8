using System.Collections.Generic;
using System.Linq;
using IntersectionTests;
using GraphicMinimal;
using RayObjects;
using GraphicGlobal;
using RayObjects.RayObjects;
using RaytracingBrdf.BrdfFunctions;

namespace RaytracingLightSource
{
    class SphereDiffuse : IRayLightSource, ISphereRayLightSource
    {
        public IIntersectableRayDrawingObject RayDrawingObject { get; private set; }
        public float EmittingSurfaceArea { get; protected set; }
        public float Emission { get; private set; }
        public float EmissionPerArea { get; protected set; }

        public Vector3D Center { get; private set; }
        public float Radius { get; private set; }

        private readonly ISphereSamplerForDirectLighting sphereSamplerForDirectLighting;
        private readonly IUniformRandomSurfacePointCreator randomSurvacePoint;

        public SphereDiffuse(RaySphere sphere)
        {
            this.RayDrawingObject = sphere.RayHeigh;
            this.Center = sphere.Center;
            this.Radius = sphere.Radius;
            this.randomSurvacePoint = sphere;
            this.EmittingSurfaceArea = sphere.SurfaceArea;
            this.Emission = (this.RayDrawingObject.Propertys.RaytracingLightSource as IRaytracingLightSource).Emission;
            this.EmissionPerArea = this.Emission / this.EmittingSurfaceArea;            
            this.sphereSamplerForDirectLighting = new SphereSamplingShirley(this.Center, this.Radius);
        }

        public SphereDiffuse(IEnumerable<IFlatRandomPointCreator> flats)
        {
            this.RayDrawingObject = flats.First().RayHeigh;
            var box = IntersectionHelper.GetBoundingBoxFromIVolumeObjektCollection(flats.Cast<IIntersecableObject>());
            this.Center = box.Center;
            this.Radius = box.RadiusInTheBox;
            var triangleListSampler = new FlatSurfaceListSamplingUniform(flats);
            this.randomSurvacePoint = triangleListSampler;
            this.EmittingSurfaceArea = triangleListSampler.SurfaceArea;
            this.Emission = (this.RayDrawingObject.Propertys.RaytracingLightSource as IRaytracingLightSource).Emission;
            this.EmissionPerArea = this.Emission / this.EmittingSurfaceArea;
            this.sphereSamplerForDirectLighting = new SphereSamplingShirley(this.Center, this.Radius);
        }



        public float GetMultipleDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            return GetDirectLightingPdfA(eyePoint, pointOnLight, pathCreationTime); //Da beim Mulitple-Verfahren bei einer Kugel auch nur ein DirectLight-Point erzeugt wird, ist die PdfA gleich groß
        }

        public List<DirectLightingSampleResult> GetRandomPointOnLightList(Vector3D eyePoint, IRandom rand)
        {
            var direction = GetRandomPointOnLight(eyePoint, rand);
            List<DirectLightingSampleResult> list = new List<DirectLightingSampleResult>();
            if (direction != null) list.Add(direction);
            return list;
        }

        public virtual float GetEmissionForEyePathHitLightSourceDirectly(IntersectionPoint pointOnLight, Vector3D eyePoint, Vector3D directionFromEyeToLightPoint)
        {
            return this.EmissionPerArea;
        }

        public virtual float GetDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            return this.sphereSamplerForDirectLighting.PdfA(eyePoint, pointOnLight);
        }

        public virtual float GetPdfWFromLightDirectionSampling(IntersectionPoint pointOnLight, Vector3D direction)
        {
            return BrdfDiffuseCosinusWeighted.PDFw(pointOnLight.OrientedFlatNormal, direction);
        }

        public float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight)
        {
            return 1.0f / this.EmittingSurfaceArea;
        }

        public virtual DirectLightingSampleResult GetRandomPointOnLight(Vector3D eyePoint, IRandom rand)
        {
            Vector3D lightPoint = this.sphereSamplerForDirectLighting.SamplePointOnSphere((float)rand.NextDouble(), (float)rand.NextDouble(), eyePoint);

            if (lightPoint == null) return null;
            Vector3D direction = Vector3D.Normalize(lightPoint - eyePoint);

            Vector3D normal = Vector3D.Normalize(lightPoint - this.Center);
            float pdfA = GetDirectLightingPdfA(eyePoint, new IntersectionPoint(new Vertex(lightPoint, normal), null, null, null, normal, null, null, null), 0);
            if (pdfA == 0) return null;

            return new DirectLightingSampleResult()
            {
                DirectionToLightPoint = direction,
                PdfA = pdfA,
                LightSource = this.RayDrawingObject,
                IsLightIntersectable = true,
            };
        }
        
        public virtual SurfaceLightPointForLightPathCreation GetRandomPointForLightPathCreation(IRandom rand)
        {
            var point = this.randomSurvacePoint.GetRandomPointOnSurface(rand);
            Vector3D direction = BrdfDiffuseCosinusWeighted.SampleDirection(rand.NextDouble(), rand.NextDouble(), point.Normal);
            
            return new SurfaceLightPointForLightPathCreation()
            {
                PointOnLight = IntersectionPoint.CreatePointOnLight(point.Position, point.Color, point.Normal, (IIntersecableObject)point.PointSampler, this.RayDrawingObject),
                Direction = direction,
                PdfA = point.PdfA,
                PdfW = BrdfDiffuseCosinusWeighted.PDFw(point.Normal, direction),
                EmissionPerArea = this.EmissionPerArea,
            };
        }
    }
}
