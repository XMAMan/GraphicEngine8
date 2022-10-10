using System.Collections.Generic;
using IntersectionTests;
using GraphicMinimal;
using RayObjects;
using GraphicGlobal;
using RayObjects.RayObjects;
using RaytracingBrdf.BrdfFunctions;

namespace RaytracingLightSource.RayLightSource
{
    class SurfaceWithMotion : IRayLightSource
    {
        public IIntersectableRayDrawingObject RayDrawingObject { get; private set; }
        public float EmittingSurfaceArea { get; private set; }
        public float Emission { get; private set; }
        public float EmissionPerArea { get; protected set; }

        private readonly IEnumerable<IFlatRandomPointCreator> flats;
        private readonly IUniformRandomSurfacePointCreator uniformSampling;
        private readonly RayMotionObject rayMotionObject;

        public SurfaceWithMotion(RayMotionObject rayMotionObject)
        {
            this.rayMotionObject = rayMotionObject;
            this.RayDrawingObject = rayMotionObject.RayHeigh;
            this.flats = rayMotionObject.LocalSpaceTriangles;
            var flatListSampler = new FlatSurfaceListSamplingUniform(flats);
            this.uniformSampling = flatListSampler;
            this.EmittingSurfaceArea = rayMotionObject.SurfaceArea; //flatListSampler.SurfaceArea ;
            this.Emission = (this.RayDrawingObject.Propertys.RaytracingLightSource as DiffuseSurfaceLightDescription).Emission;
            this.EmissionPerArea = this.Emission / this.EmittingSurfaceArea;
        }

        public float GetDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            Vector3D lokalEyePoint = Matrix4x4.MultPosition(this.rayMotionObject.Movement.GetMotionMatrizes(pathCreationTime).WorldToObjectMatrix, eyePoint);
            IFlatRandomPointCreator flat;
            if (pointOnLight is IntersectionPointWithRayMotionObject)
            {
                flat = (pointOnLight as IntersectionPointWithRayMotionObject).IntersectedTriangle as IFlatRandomPointCreator;
            }else
            {
                flat = pointOnLight.IntersectedObject as IFlatRandomPointCreator;
            }
            return new FlatSurfaceListSamplingNonUniform(this.flats, lokalEyePoint).PdfA(flat) / (this.RayDrawingObject.Propertys.Size * this.RayDrawingObject.Propertys.Size);
        }

        public float GetMultipleDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            var flat = pointOnLight.IntersectedObject as IFlatObject;
            return (1.0f / flat.SurfaceArea) / (this.RayDrawingObject.Propertys.Size * this.RayDrawingObject.Propertys.Size);
        }

        public List<DirectLightingSampleResult> GetRandomPointOnLightList(Vector3D eyePoint, IRandom rand)
        {
            var m = this.rayMotionObject.Movement.GetMotionMatrizes((float)rand.NextDouble());
            Vector3D lokalEyePoint = Matrix4x4.MultPosition(m.WorldToObjectMatrix, eyePoint);

            List<DirectLightingSampleResult> directionList = new List<DirectLightingSampleResult>();

            foreach (var flat in this.flats)
            {
                if (flat.IsPointAbovePlane(lokalEyePoint))
                {
                    var point = flat.GetRandomPointOnSurface(rand);
                    directionList.Add(new DirectLightingSampleResult()
                    {
                        DirectionToLightPoint =  Vector3D.Normalize(Matrix4x4.MultPosition(m.ObjectToWorldMatrix, point.Position) - eyePoint),
                        PdfA = point.PdfA / (this.RayDrawingObject.Propertys.Size * this.RayDrawingObject.Propertys.Size),
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
            var m = this.rayMotionObject.Movement.GetMotionMatrizes((float)rand.NextDouble());
            Vector3D lokalEyePoint = Matrix4x4.MultPosition(m.WorldToObjectMatrix, eyePoint);

            var sampling = new FlatSurfaceListSamplingNonUniform(this.flats, lokalEyePoint);
            var point = sampling.GetRandomPointOnSurface(rand);
            if (point == null) return null; //Wenn keine Fläche zum Hitpoint zeigt, dann gebe Null zurück

            return new DirectLightingSampleResult()
            {
                DirectionToLightPoint = Vector3D.Normalize(Matrix4x4.MultPosition(m.ObjectToWorldMatrix, point.Position) - eyePoint),
                PdfA = point.PdfA / (this.RayDrawingObject.Propertys.Size * this.RayDrawingObject.Propertys.Size),
                LightSource = this.RayDrawingObject,
                IsLightIntersectable = true,
            };
        }

        public float GetPdfWFromLightDirectionSampling(IntersectionPoint pointOnLight, Vector3D direction)
        {
            return BrdfDiffuseCosinusWeighted.PDFw(pointOnLight.OrientedFlatNormal, direction);
        }

        public float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight)
        {
            return 1.0f / this.EmittingSurfaceArea;
        }

        //Uniform-Sampling fürs Lightpath erstellen
        public virtual SurfaceLightPointForLightPathCreation GetRandomPointForLightPathCreation(IRandom rand)
        {
            var m = this.rayMotionObject.Movement.GetMotionMatrizes((float)rand.NextDouble());

            var point = this.uniformSampling.GetRandomPointOnSurface(rand);
            Vector3D worldNormal = Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, point.Normal));
            Vector3D direction = BrdfDiffuseCosinusWeighted.SampleDirection(rand.NextDouble(), rand.NextDouble(), worldNormal);
            Vector3D worldPosition = Matrix4x4.MultPosition(m.ObjectToWorldMatrix, point.Position);

            return new SurfaceLightPointForLightPathCreation()
            {
                PointOnLight = new IntersectionPointWithRayMotionObject(new Vertex(worldPosition, worldNormal), point.Color, null, worldNormal, worldNormal, null, this.rayMotionObject, this.RayDrawingObject, (IIntersecableObject)point.PointSampler),
                Direction = direction,
                PdfA = point.PdfA / (this.RayDrawingObject.Propertys.Size * this.RayDrawingObject.Propertys.Size),//1.0f / this.EmittingSurvaceArea,
                PdfW = BrdfDiffuseCosinusWeighted.PDFw(worldNormal, direction),
                EmissionPerArea = this.EmissionPerArea,
            };
        }
    }
}
