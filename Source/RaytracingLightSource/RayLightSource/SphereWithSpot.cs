using System;
using System.Collections.Generic;
using System.Linq;
using IntersectionTests;
using GraphicMinimal;
using GraphicGlobal;
using RayObjects.RayObjects;
using RaytracingBrdf.BrdfFunctions;

namespace RaytracingLightSource
{
    //Das ist hier eine Punktlichtquelle mit einer Streuung. Der Pathtracer kann sie also nicht treffen und bei DirectLighting muss ich nicht sampeln, da es nur
    //ein möglichen Pfad gibt, der treffen würde und somit auch Emission statt EmissionPerArea.
    class SphereWithSpot : IRayLightSource, ISphereRayLightSource
    {
        public IIntersectableRayDrawingObject RayDrawingObject { get; private set; }
        public float EmittingSurfaceArea { get; private set; }
        public float Emission { get; private set; }
        public float EmissionPerArea { get; protected set; }

        public Vector3D Center { get; private set; }
        public float Radius { get; private set; }

        private readonly float spotCutoff;
        private readonly Vector3D spotDirection;
        private readonly float spotCutoffCosinus;
        private readonly IntersectionFinder intersectionFinder;

        public SphereWithSpot(RaySphere sphere)
        {
            this.RayDrawingObject = sphere.RayHeigh;
            this.Center = sphere.Center;
            this.Radius = sphere.Radius;
            this.spotCutoff = (sphere.RayHeigh.Propertys.RaytracingLightSource as IRaytracingSphereWithSpotLight).SpotCutoff;
            this.spotDirection = (sphere.RayHeigh.Propertys.RaytracingLightSource as IRaytracingSphereWithSpotLight).SpotDirection;
            this.spotCutoffCosinus = (float)Math.Cos(spotCutoff * Math.PI / 180);
            this.EmittingSurfaceArea = CalculateEmittingSurvaceAreaWithSpotCutoff(sphere.Radius, this.spotCutoff);
            this.intersectionFinder = new IntersectionFinder(new List<IIntersecableObject>() { sphere }, (text, zahl) => { });
            this.Emission = (this.RayDrawingObject.Propertys.RaytracingLightSource as IRaytracingLightSource).Emission;
            this.EmissionPerArea = this.Emission / this.EmittingSurfaceArea;
        }

        public SphereWithSpot(IEnumerable<RayTriangle> triangles)
        {
            this.RayDrawingObject = triangles.First().RayHeigh;
            var box = IntersectionHelper.GetBoundingBoxFromIVolumeObjektCollection(triangles.Cast<IIntersecableObject>());
            this.Center = box.Center;
            this.Radius = box.RadiusInTheBox;
            this.spotCutoff = (triangles.First().RayHeigh.Propertys.RaytracingLightSource as IRaytracingSphereWithSpotLight).SpotCutoff;
            this.spotDirection = (triangles.First().RayHeigh.Propertys.RaytracingLightSource as IRaytracingSphereWithSpotLight).SpotDirection;
            this.spotCutoffCosinus = (float)Math.Cos(spotCutoff * Math.PI / 180);
            this.EmittingSurfaceArea = CalculateEmittingSurvaceAreaWithSpotCutoff(box.RadiusInTheBox, this.spotCutoff);
            this.intersectionFinder = new IntersectionFinder(triangles.Cast<IIntersecableObject>().ToList(), (text, zahl) => { });
            this.Emission = (this.RayDrawingObject.Propertys.RaytracingLightSource as IRaytracingLightSource).Emission;
            this.EmissionPerArea = this.Emission / this.EmittingSurfaceArea;
        }

        private static float CalculateEmittingSurvaceAreaWithSpotCutoff(float radius, float spotCutoff)
        {
            //Mantelfläche: 2*PI*r*h mit h=r(1-cos(w)) https://de.wikipedia.org/wiki/Kugelsegment
            float h = radius * (1 - (float)Math.Cos(spotCutoff * Math.PI / 180));
            return 2 * (float)Math.PI * radius * h;
        }

        public float GetDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            Vector3D toHitpointDirection = Vector3D.Normalize(eyePoint - this.Center);

            if (IsInSpotCuttoff(toHitpointDirection) == false) return 0;

            return 1; //Pdf ist 1, da kein Sampling, da das hier Richtungslicht ist
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

        //Änderung am 27.7.2017: GetEmissionForEyePathHitLightSourceDirectly gibt EmissionPerArea anstatt Emission zurück; GetRandomPointOnLight PdfA ist 1 / SurfacArea anstatt 1
        //  -> Es scheint erstmal zu gehen nur hat man bei 7000 Samples viele Fireflys. Fireflys verschwinden auch nicht. Lösung ist nicht parktikabel für Pathtracing

        public float GetEmissionForEyePathHitLightSourceDirectly(IntersectionPoint pointOnLight, Vector3D eyePoint, Vector3D directionFromEyeToLightPoint)
        {
            Vector3D direction = Vector3D.Normalize(eyePoint - this.Center);
            //if (IsInSpotCuttoff(direction)) return this.Emission; //Hier steht absichtlich nicht EmissionPerArea, da sonst DirectLighting viel zu dunkel bei Szene 0 oder hell bei Szene 1 ist
            if (IsInSpotCuttoff(direction)) return this.EmissionPerArea;
            return 0;
        }

        public DirectLightingSampleResult GetRandomPointOnLight(Vector3D eyePoint, IRandom rand)
        {
            Vector3D toHitpointDirection = Vector3D.Normalize(eyePoint - this.Center);

            if (IsInSpotCuttoff(toHitpointDirection) == false) return null; // Hitpoint liegt außerhalb vom SpotCutoff

            Vector3D lightPoint = IntersectionHelper.GetIntersectionPointBetweenRayAndSphere(new Ray(eyePoint, -toHitpointDirection), this.Center, this.Radius);
            if (lightPoint == null) return null;

            return new DirectLightingSampleResult()
            {
                DirectionToLightPoint = -toHitpointDirection,
                //PdfA = 1.0f,                   //Pdf ist 1, da kein Sampling, da das hier Richtungslicht ist
                PdfA = 1.0f * (lightPoint - eyePoint).SquareLength(),
                LightSource = this.RayDrawingObject,
                IsLightIntersectable = true,
            };
        }

        public float GetPdfWFromLightDirectionSampling(IntersectionPoint pointOnLight, Vector3D direction)
        {
            return 1;
        }

        public float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight)
        {
            return 1.0f / this.EmittingSurfaceArea;
        }

        public virtual SurfaceLightPointForLightPathCreation GetRandomPointForLightPathCreation(IRandom rand)
        {
            Vector3D direction = null;
            IntersectionPoint lightPoint = null;

            //In der GetIntersectionPointFromDirection ist irgentwo noch ein Fehler. Er findet nicht immer einen Schnittpunkt mit ein Dreieck obwohl es im SpotCutoff liegt
            for (int i = 0; i < 100; i++)
            {
                //Funktioniert auch:
                //double d = rand.NextDouble() * (1 - this.spotCutoffCosinus) + this.spotCutoffCosinus;
                //direction = BrdfDiffuseCosinusWeighted.SampleDirection((float)(2 * Math.PI * (float)rand.NextDouble()), (float)(d * d), this.spotDirection); 

                direction = BrdfDiffuseUniformWeighted.SampleDirection(rand.NextDouble(), rand.NextDouble() * (1 - this.spotCutoffCosinus) + this.spotCutoffCosinus, this.spotDirection);

                //direction = new Vector3D(0.5825704f, 0.7007478f, 0.4021665f); //Liefert keinen Schnittpunkt, obwohl es im SpotCutoff liegt

                if (IsInSpotCuttoff(direction))
                {
                    lightPoint = this.intersectionFinder.GetIntersectionPoint(new Ray(this.Center, direction), 0); 
                }

                if (lightPoint != null) break;
            }

            //if (lightPoint == null) throw new Exception("Konnte LightPoint auf SphereLightSourceWithSpotCutoff nicht bilden " + direction.ToString());
            if (lightPoint == null)
            {
                lightPoint = this.intersectionFinder.GetIntersectionPoint(new Ray(this.Center, this.spotDirection), 0); 
            }

            return new SurfaceLightPointForLightPathCreation()
            {
                PointOnLight = IntersectionPoint.CreatePointOnLight(lightPoint.Position, lightPoint.Color, direction, lightPoint.IntersectedObject, this.RayDrawingObject),
                Direction = direction,
                PdfA = 1.0f / this.EmittingSurfaceArea,
                PdfW = 1,
                EmissionPerArea = this.EmissionPerArea,
            };
        }

        private bool IsInSpotCuttoff(Vector3D toHitpointDirection)
        {
            return IsInSpotCuttoff(this.spotCutoffCosinus, this.spotDirection, toHitpointDirection);
        }

        private static bool IsInSpotCuttoff(float spotCutoffCosinus, Vector3D spotDirection, Vector3D direction)
        {
            float spot = Math.Max(spotDirection * direction, 0.0f);
            return spot >= spotCutoffCosinus;
        }
    }
}
