using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using RayObjects.RayObjects;
using RaytracingLightSource.Basics;
using System;
using System.Collections.Generic;

namespace RaytracingLightSource
{
    //Kugel, welche die Szene gerade so umschließt. Auf dieser Kugel befindet sich eine Textur, die leuchtet
    class EnvironmentLightSourceWithImageImportanceSampling : IRayLightSource, IEnvironmentLightSource
    {
        private readonly Vector3D center;
        private readonly float radius;
        private readonly SphereWithImageSampler envMap;
        private readonly IRayObject rayObject;

        //Lichtquelle hängt an beliebigen Objekt, welches dann auch nicht in der Szene sichtbar ist
        public EnvironmentLightSourceWithImageImportanceSampling(IRayObject rayObject, IntersectionFinder intersectionFinder, MediaIntersectionFinder mediaIntersectionFinder, Vector3D cameraUpVector)
        {
            this.rayObject = rayObject;
            var boxFromSzene = IntersectionHelper.GetBoundingBoxFromSzene(intersectionFinder, mediaIntersectionFinder);
            this.center = boxFromSzene.Center;
            this.radius = boxFromSzene.RadiusOutTheBox + 1;

            var props = (rayObject.RayHeigh.Propertys.RaytracingLightSource as EnvironmentLightDescription);
            if (props.CameraUpVector == null) props.CameraUpVector = cameraUpVector;
            this.envMap = new SphereWithImageSampler(BitmapHelp.LoadHdrImage(rayObject.RayHeigh.Propertys.Color.As<ColorFromTexture>().TextureFile), props.Rotate, props.CameraUpVector);

            this.EmittingSurfaceArea = 4.0f * this.radius * this.radius * (float)Math.PI;
            this.Emission = props.Emission;
            this.EmissionPerArea = this.Emission;// / this.EmittingSurvaceArea;
            this.RayDrawingObject = rayObject.RayHeigh;
        }

        public IntersectionPoint GetIntersectionPoint(Ray ray)
        {
            Vector3D pointOnLight = ray.Start + ray.Direction;
            Vector3D toLight = ray.Direction;            
            Vector3D color = this.envMap.GetColorFromPointOnSphere(toLight);
            return new IntersectionPoint(new Vertex(pointOnLight, -toLight), color, null, -toLight, -toLight, null, this.rayObject, this.rayObject.RayHeigh);
        }

        public IIntersectableRayDrawingObject RayDrawingObject { get; private set; } = null;

        public float EmittingSurfaceArea { get; private set; }

        public float Emission { get; private set; } //Wie viel Photonen pro Sekunde sendet die gesamte Lichtfläche aus

        public float EmissionPerArea { get; private set; } //Leuchtkraft pro Fläche => Entspricht Emission / EmittingSurvaceArea (So viel Leuchtet ein einzelner Punkt auf der Fläche. Man darf aber immer nur zwischen zwei Flächen die Lichtenergie austauschen.)

        public float GetEmissionForEyePathHitLightSourceDirectly(IntersectionPoint pointOnLight, Vector3D eyePoint, Vector3D directionFromEyeToLightPoint)
        {
            return this.EmissionPerArea;
        }

        public float GetDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            Vector3D toLight = Vector3D.Normalize(pointOnLight.Position - eyePoint);
            return (float)this.envMap.GetPdfAFromPointOnSphere(toLight);
        }

        public float GetMultipleDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            return GetDirectLightingPdfA(eyePoint, pointOnLight, pathCreationTime);
        }

        public DirectLightingSampleResult GetRandomPointOnLight(Vector3D eyePoint, IRandom rand)
        {
            var point = this.envMap.SamplePointOnSphere(rand.NextDouble(), rand.NextDouble());
            Vector3D pointOnLight = eyePoint + point.Position; //Platziere mit Abstand von 1 die Lichtquelle über dem EyePoint

            Vector3D normal = -point.Position; //Der Point-Sampler verwendet eine Einheitskugel. Also ist die Position eine normierte Richtung

            return new DirectLightingSampleResult()
            {
                DirectionToLightPoint = -normal,
                PdfA = (float)point.PdfA,
                //PdfA = (float)this.envMap.GetPdfAFromPointOnSphere(point.Position),
                LightSource = this.RayDrawingObject,
                IsLightIntersectable = false,
                LightSourceIsInfinityAway = true,
                LightPointIfNotIntersectable = new IntersectionPoint(new Vertex(pointOnLight, normal), point.Color, null, normal, normal, null, null, this.RayDrawingObject),
            };
        }

        public List<DirectLightingSampleResult> GetRandomPointOnLightList(Vector3D eyePoint, IRandom rand)
        {
            return new List<DirectLightingSampleResult>() { GetRandomPointOnLight(eyePoint, rand) };
        }

        public float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight)
        {
            //Discsampler-PdfA
            return (float)(1.0 / (this.radius * this.radius * Math.PI));
        }

        public float GetPdfWFromLightDirectionSampling(IntersectionPoint pointOnLight, Vector3D direction)
        {
            //Vector3D toLight = Vector3D.Normalize(pointOnLight.Position - this.center);
            Vector3D toLight = -direction;
            return (float)this.envMap.GetPdfAFromPointOnSphere(toLight);
        }

        public SurfaceLightPointForLightPathCreation GetRandomPointForLightPathCreation(IRandom rand)
        {
            var spherePoint = this.envMap.SamplePointOnSphere(rand.NextDouble(), rand.NextDouble());
            Vector3D toLight = spherePoint.Position;
            var discSampler = new DiscSampler(this.center + toLight * this.radius, -toLight, this.radius);
            var discPoint = discSampler.SamplePointOnDisc(rand.NextDouble(), rand.NextDouble());

            return new SurfaceLightPointForLightPathCreation()
            {
                PointOnLight = IntersectionPoint.CreatePointOnLight(discPoint, spherePoint.Color, -toLight, null, this.RayDrawingObject),
                Direction = -toLight,
                PdfA = discSampler.PdfA,
                PdfW = (float)spherePoint.PdfA,
                //PdfW = (float)this.envMap.GetPdfAFromPointOnSphere(toLight),
                EmissionPerArea = this.EmissionPerArea,
                LightSourceIsInfinityAway = true,
            };
        }

    }
}
