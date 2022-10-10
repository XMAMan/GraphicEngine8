using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using RaytracingLightSource.Basics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RayTracerGlobal;
using RayObjects.RayObjects;

namespace RaytracingLightSource
{
    //Richtungslicht, was unendlich weit weg ist und wo die Lichstrahlen parallel verlaufen (Entspricht der Sonne)
    //Umsetzung: Ich platziere eine Scheibe mit dem Durchmesser der Szene und der Normale von den Dreieck, aus dem diese Lichtquelle gebaut wird
    //was gedanklich genug Abstand zur Szenen-Bounding-Box-Kugel hat
    //Die Emission ist nun so, dass sie unabhängig vom Abstand zur Szene ist, indem mit Mal Abstand-Quadrad multipliert wird
    class FarAwayDiretionLightSource : IRayLightSource
    {
        private readonly Vector3D lightingDirection;
        private readonly DiscSampler discSamplerForLightracing;

        //Richtungslicht muss einfarbig sein, da meine Textur ja über 2 Dreieck angegeben ist ich zum sampeln aber eine Disc nehme.
        //Ich müsste die Farbe in FarAwayDirectionLightDescription übergeben, wenn ich was anders als weiß verwenden will
        private readonly Vector3D color = new Vector3D(1, 1, 1); 

        //Lichtquelle wird über 2 Dreiecke, welche eine Ebene definieren, beschrieben. 
        public FarAwayDiretionLightSource(IEnumerable<RayTriangle> triangles, IntersectionFinder intersectionFinder, MediaIntersectionFinder mediaIntersectionFinder)
        {
            CheckInputData(triangles);

            this.lightingDirection = triangles.First().Normal;

            var box = IntersectionHelper.GetBoundingBoxFromSzene(intersectionFinder, mediaIntersectionFinder);
            this.discSamplerForLightracing = new DiscSampler(box.Center - lightingDirection * (box.RadiusOutTheBox + MagicNumbers.MinAllowedPathPointDistance * 2), lightingDirection, box.RadiusOutTheBox);;
            
            this.EmittingSurfaceArea = this.discSamplerForLightracing.Radius * this.discSamplerForLightracing.Radius * (float)Math.PI;
            this.Emission = (triangles.First().RayHeigh.Propertys.RaytracingLightSource as FarAwayDirectionLightDescription).Emission;
            this.EmissionPerArea = this.Emission;// / this.EmittingSurvaceArea;
            this.RayDrawingObject = triangles.First().RayHeigh;
            //this.color = triangles.First().GetRandomPointOnSurface(new Rand(0)).Color; //Farbinforamtion muss aus FarAwayDirectionLightDescription und nicht aus den Dreiecken kommen weil es einfarbig sein muss
        }

        private void CheckInputData(IEnumerable<RayTriangle> triangles)
        {
            Debug.Assert(triangles.Count() == 2, "The " +nameof(FarAwayDiretionLightSource) + "light must consist of exactly two triangles");
            Vector3D normal1 = triangles.First().Normal;
            Vector3D normal2 = triangles.Last().Normal;
            Debug.Assert(normal1 * normal2 > 0.99f, "The normals of the two triangles must point in the same direction");
        }

        public IIntersectableRayDrawingObject RayDrawingObject { get; private set; } = null;
        public float EmittingSurfaceArea { get; private set; } = 1;
        public float Emission { get; private set; } //Wie viel Photonen pro Sekunde sendet die gesamte Lichtfläche aus
        public float EmissionPerArea { get; private set; } //Leuchtkraft pro Fläche => Entspricht Emission / EmittingSurvaceArea (So viel Leuchtet ein einzelner Punkt auf der Fläche. Man darf aber immer nur zwischen zwei Flächen die Lichtenergie austauschen.)
        public float GetDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime) //Berechnet die Wahrscheinlichkeit, einen Punkt auf der Lichtquelle zu erzeugen, welcher den eyePoint beleuchtet. d.h. die Fläche der Lichtquelle, welche vom eyePoint aus zu sehen ist(Ohne VisibleTest)
        {
            return 1;
        }
        public float GetMultipleDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime) //Berechnet die Wahrscheinlichkeit, das einer von den vielen DirectLight-Samples dem pointOnLight entspricht
        {
            return 1;
        }

        public List<DirectLightingSampleResult> GetRandomPointOnLightList(Vector3D eyePoint, IRandom rand)
        {
            return new List<DirectLightingSampleResult>() { GetRandomPointOnLight(eyePoint, rand) };
        }
        public DirectLightingSampleResult GetRandomPointOnLight(Vector3D eyePoint, IRandom rand) //Gibt null zurück, wenn eyepoint außerhalb vom Spotcutoff oder keine Fläche zum eyepoint zeigt
        {
            return new DirectLightingSampleResult()
            {
                DirectionToLightPoint = -this.lightingDirection,
                PdfA = 1,
                LightSource = this.RayDrawingObject,
                IsLightIntersectable = false,
                LightSourceIsInfinityAway = true,
                //Beginne mit Abstand von 1 über dem EyePoint eine virtuelle Lichtquelle zu platzieren
                LightPointIfNotIntersectable = new IntersectionPoint(new Vertex(eyePoint - this.lightingDirection, this.lightingDirection), new Vector3D(1, 1, 1), null, this.lightingDirection, this.lightingDirection, null, null, this.RayDrawingObject),
            };
        }

        public float GetEmissionForEyePathHitLightSourceDirectly(IntersectionPoint pointOnLight, Vector3D eyePoint, Vector3D directionFromEyeToLightPoint)//Falls sich beim Brdf-Sampling beim eyePoint jemand die Lichtquelle am pointOnLight trifft
        {
            return this.EmissionPerArea;
        }

        public float GetPdfWFromLightDirectionSampling(IntersectionPoint pointOnLight, Vector3D direction)
        {
            return 1;
        }

        public float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight)
        {
            return 1.0f / this.EmittingSurfaceArea;
        }

        public SurfaceLightPointForLightPathCreation GetRandomPointForLightPathCreation(IRandom rand) //Zum erstellen von LightSub-Pahts
        {
            double u1 = rand.NextDouble();
            double u2 = rand.NextDouble();

            Vector3D virutalLightPoint = this.discSamplerForLightracing.SamplePointOnDisc(u1, u2);

            return new SurfaceLightPointForLightPathCreation()
            {
                PointOnLight = IntersectionPoint.CreatePointOnLight(virutalLightPoint, this.color, this.lightingDirection, null, this.RayDrawingObject),
                Direction = this.lightingDirection,
                //PdfA = this.discSamplerForLightracing.PdfA,
                PdfA = 1.0f / this.EmittingSurfaceArea,
                PdfW = 1, 
                EmissionPerArea = this.EmissionPerArea,
                LightSourceIsInfinityAway = true,
            };
        }
    }
}
