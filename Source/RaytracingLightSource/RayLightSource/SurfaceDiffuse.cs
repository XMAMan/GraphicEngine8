using System.Collections.Generic;
using System.Linq;
using IntersectionTests;
using GraphicMinimal;
using RayObjects;
using GraphicGlobal;
using RaytracingBrdf.BrdfFunctions;

namespace RaytracingLightSource
{
    //Oberflächenlicht ohne SpotCutoff aus Dreiecken bestehend
    class SurfaceDiffuse : IRayLightSource
    {
        public IIntersectableRayDrawingObject RayDrawingObject { get; private set; }
        public float EmittingSurfaceArea { get; private set; }
        public float Emission { get; private set; }
        public float EmissionPerArea { get; protected set; }

        private readonly IEnumerable<IFlatRandomPointCreator> flats;
        private readonly IUniformRandomSurfacePointCreator uniformSampling;

        public SurfaceDiffuse(IEnumerable<IFlatRandomPointCreator> flats)
        {
            this.RayDrawingObject = flats.First().RayHeigh;
            this.flats = flats;
            var flatListSampler = new FlatSurfaceListSamplingUniform(flats);
            this.uniformSampling = flatListSampler;
            this.EmittingSurfaceArea = flatListSampler.SurfaceArea;
            this.Emission = this.RayDrawingObject.Propertys.RaytracingLightSource.Emission;
            this.EmissionPerArea = this.Emission / this.EmittingSurfaceArea;
        }

        public float GetDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            return new FlatSurfaceListSamplingNonUniform(this.flats, eyePoint).PdfA(pointOnLight.IntersectedObject as IFlatRandomPointCreator);
        }

        public float GetMultipleDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            var flat = pointOnLight.IntersectedObject as IFlatObject;
            return 1.0f / flat.SurfaceArea;
        }

        public List<DirectLightingSampleResult> GetRandomPointOnLightList(Vector3D eyePoint, IRandom rand)
        {
            List<DirectLightingSampleResult> directionList = new List<DirectLightingSampleResult>();

            foreach (var flat in this.flats)
            {
                if (flat.IsPointAbovePlane(eyePoint))
                {
                    var point = flat.GetRandomPointOnSurface(rand);
                    directionList.Add(new DirectLightingSampleResult()
                    {
                        DirectionToLightPoint = Vector3D.Normalize(point.Position - eyePoint),
                        PdfA = point.PdfA,
                        LightSource = this.RayDrawingObject,
                        IsLightIntersectable = true,
                    });
                }
            }

            return directionList;
        }

        public float GetEmissionForEyePathHitLightSourceDirectly(IntersectionPoint pointOnLight, Vector3D eyePoint, Vector3D directionFromEyeToLightPoint)
        {
            if (pointOnLight.FlatNormal * directionFromEyeToLightPoint < 0)
                return this.EmissionPerArea;
            return 0;
        }

        //Non-Uniformes Sampling für das DirectLighting
        public DirectLightingSampleResult GetRandomPointOnLight(Vector3D eyePoint, IRandom rand)
        {
            var sampling = new FlatSurfaceListSamplingNonUniform(this.flats, eyePoint);
            var point = sampling.GetRandomPointOnSurface(rand);
            if (point == null) return null; //Wenn keine Fläche zum Hitpoint zeigt, dann gebe Null zurück

            return new DirectLightingSampleResult()
            {
                DirectionToLightPoint = Vector3D.Normalize(point.Position - eyePoint),
                PdfA = point.PdfA,
                LightSource = this.RayDrawingObject,
                IsLightIntersectable = true,
            };
        }

        public virtual float GetPdfWFromLightDirectionSampling(IntersectionPoint pointOnLight, Vector3D direction)
        {
            return BrdfDiffuseCosinusWeighted.PDFw(pointOnLight.OrientedFlatNormal, direction);
        }

        public virtual float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight)
        {
            return 1.0f / this.EmittingSurfaceArea;
        }

        //Uniform-Sampling fürs Lightpath erstellen
        public virtual SurfaceLightPointForLightPathCreation GetRandomPointForLightPathCreation(IRandom rand)
        {
            var point = this.uniformSampling.GetRandomPointOnSurface(rand);
            Vector3D direction = BrdfDiffuseCosinusWeighted.SampleDirection(rand.NextDouble(), rand.NextDouble(), point.Normal);

            return new SurfaceLightPointForLightPathCreation()
            {
                PointOnLight = IntersectionPoint.CreatePointOnLight(point.Position, point.Color, point.Normal, (IIntersecableObject)point.PointSampler, this.RayDrawingObject),
                Direction = direction,
                PdfA = point.PdfA,//1.0f / this.EmittingSurvaceArea,
                PdfW = BrdfDiffuseCosinusWeighted.PDFw(point.Normal, direction),
                EmissionPerArea = this.EmissionPerArea,
            };
        }
    }
}
