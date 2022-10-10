using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using RaytracingLightSource.Basics;
using System;
using System.Collections.Generic;
using RayObjects.RayObjects;

namespace RaytracingLightSource
{

    interface IEnvironmentLightSource
    {
        IntersectionPoint GetIntersectionPoint(Ray ray);
    }


    //Unendlich große Kugel, welche aber vom Abstand immer so leuchtet, als hätte sie ein festen Abstand zu allen Szenenpunkten
    //Will man weißes Licht, muss man den RayObject, welches dieser Lichtquelle als Anlegepunkt hat, bei TexturFile ="#FFFFFF" zuweisen, ansonsten eine Textur
    class EnvironmentLightSourceWithEqualSampling : IRayLightSource, IEnvironmentLightSource
    {
        private readonly RaySphere sphereSampler;

        //Da ich beim Richtungssampling immer die gleiche PdfA unabhängig vom Szenenradius will, nutze ich die
        //Einheitskugel um eine zufällige Richtung fürs DirectLighting zu bestimmen
        //Frage: Die Discsampler-PdfA ist abhängig vom Szenenradius. Ist damit nicht Lightracing Szenenabhängig? Vielleicht MUSS die PdfA 
        //Szenenabhängig bleiben, da nur die Distanz keine Rolle spielen soll aber der Rest schon.
        private readonly float surfaceAreaUnitSphere = 4.0f * (float) Math.PI;

        //Lichtquelle hängt an beliebigen Objekt, welches dann auch nicht in der Szene sichtbar ist
        public EnvironmentLightSourceWithEqualSampling(IRayObject rayObject, IntersectionFinder intersectionFinder, MediaIntersectionFinder mediaIntersectionFinder)
        {
            
            this.sphereSampler = CreateSphereSampler(IntersectionHelper.GetBoundingBoxFromSzene(intersectionFinder, mediaIntersectionFinder), rayObject);
            this.EmittingSurfaceArea = this.sphereSampler.SurfaceArea;
            this.Emission = (rayObject.RayHeigh.Propertys.RaytracingLightSource as EnvironmentLightDescription).Emission;
            this.EmissionPerArea = this.Emission;// / this.EmittingSurvaceArea;
            this.RayDrawingObject = rayObject.RayHeigh;
        }

        //Damit kann PathTracing die Lichtquelle treffen
        public IntersectionPoint GetIntersectionPoint(Ray ray)
        {
            var simplePoint = this.sphereSampler.GetSimpleIntersectionPoint(ray, 0);
            var point = this.sphereSampler.TransformSimplePointToIntersectionPoint(simplePoint);

            return point;
        }

        private static RaySphere CreateSphereSampler(BoundingBox boxFromSzene, IRayObject rayObject)
        {
            return new RaySphere(boxFromSzene.Center, boxFromSzene.RadiusOutTheBox + 1, rayObject.RayHeigh);
        }

        public IIntersectableRayDrawingObject RayDrawingObject { get; private set; } = null;
        public float EmittingSurfaceArea { get; private set; }
        public float Emission { get; private set; } //Wie viel Photonen pro Sekunde sendet die gesamte Lichtfläche aus
        public float EmissionPerArea { get; private set; } //Leuchtkraft pro Fläche => Entspricht Emission / EmittingSurvaceArea (So viel Leuchtet ein einzelner Punkt auf der Fläche. Man darf aber immer nur zwischen zwei Flächen die Lichtenergie austauschen.)
        public float GetDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime) //Berechnet die Wahrscheinlichkeit, einen Punkt auf der Lichtquelle zu erzeugen, welcher den eyePoint beleuchtet. d.h. die Fläche der Lichtquelle, welche vom eyePoint aus zu sehen ist(Ohne VisibleTest)
        {
            return 1.0f / this.surfaceAreaUnitSphere; 
        }
        public float GetMultipleDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime) //Berechnet die Wahrscheinlichkeit, das einer von den vielen DirectLight-Samples dem pointOnLight entspricht
        {
            return 1.0f / this.surfaceAreaUnitSphere;
        }

        public List<DirectLightingSampleResult> GetRandomPointOnLightList(Vector3D eyePoint, IRandom rand)
        {
            return new List<DirectLightingSampleResult>() { GetRandomPointOnLight(eyePoint, rand) };
        }
        public DirectLightingSampleResult GetRandomPointOnLight(Vector3D eyePoint, IRandom rand) //Gibt null zurück, wenn eyepoint außerhalb vom Spotcutoff oder keine Fläche zum eyepoint zeigt
        {
            var point = this.sphereSampler.GetRandomPointOnSurface(rand);
            
            return new DirectLightingSampleResult()
            {
                DirectionToLightPoint = Vector3D.Normalize(point.Position - eyePoint),
                PdfA = 1.0f / this.surfaceAreaUnitSphere, //PdfA soll unabhängig vom Szenenradius sein
                LightSource = this.RayDrawingObject,
                IsLightIntersectable = false,
                LightSourceIsInfinityAway = true,
                LightPointIfNotIntersectable = new IntersectionPoint(new Vertex(point.Position, -point.Normal), point.Color, null, null, -point.Normal, null, null, this.RayDrawingObject),
            };
        }

        public float GetEmissionForEyePathHitLightSourceDirectly(IntersectionPoint pointOnLight, Vector3D eyePoint, Vector3D directionFromEyeToLightPoint)//Falls sich beim Brdf-Sampling beim eyePoint jemand die Lichtquelle am pointOnLight trifft
        {
            return this.EmissionPerArea;
        }

        public float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight)
        {
            //Discsampler-PdfA
            return (float)(1.0 / (this.sphereSampler.Radius * this.sphereSampler.Radius * Math.PI));
        }

        public float GetPdfWFromLightDirectionSampling(IntersectionPoint pointOnLight, Vector3D direction)
        {
            return 1.0f / this.surfaceAreaUnitSphere;
        }

        public SurfaceLightPointForLightPathCreation GetRandomPointForLightPathCreation(IRandom rand) //Zum erstellen von LightSub-Pahts
        {
            var spherePoint = this.sphereSampler.GetRandomPointOnSurface(rand);
            Vector3D toLight = Vector3D.Normalize(spherePoint.Position - this.sphereSampler.Center);
            var discSampler = new DiscSampler(this.sphereSampler.Center + toLight * this.sphereSampler.Radius, -toLight, this.sphereSampler.Radius);
            var discPoint = discSampler.SamplePointOnDisc(rand.NextDouble(), rand.NextDouble());

            return new SurfaceLightPointForLightPathCreation()
            {
                PointOnLight = IntersectionPoint.CreatePointOnLight(discPoint, spherePoint.Color, -toLight, null, this.RayDrawingObject),
                Direction = -toLight,
                PdfA = discSampler.PdfA,
                PdfW = 1.0f / this.surfaceAreaUnitSphere,
                EmissionPerArea = this.EmissionPerArea,
                LightSourceIsInfinityAway = true,
            };
        }
    }

    
}
