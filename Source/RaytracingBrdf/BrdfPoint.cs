using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using RayTracerGlobal;
using RaytracingBrdf.BrdfFunctions;
using RaytracingBrdf.SampleAndRequest;
using System;

namespace RaytracingBrdf
{
    //Entsteht durch ein Subpathsampler. Liegt auf einer Seite von eine Oberfläche, wo ein Strahl hinzeigt. 
    //BrdfPoint = Differentiales Oberflächenelement wo ein Strahl hinzeigt
    public class BrdfPoint
    {
        public IntersectionPoint SurfacePoint { get; private set; }
        public Vector3D DirectionToThisPoint { get; set; }
        public float RefractionIndexFromRayComesFrom { get; private set; }
        public float RefractionIndexOtherSide { get; private set; }
        public IBrdf Brdf { get; private set; }

        private bool createAbsorbationEvent; //Soll beim Richtungssampeln laut der ContinuationPdf ein Absorbationsevent gesampelt werden um somit per Russia Rollete die Subpfadlänge zu begrenzen?

        public BrdfPoint(IntersectionPoint surfacePoint, Vector3D directionToThisPoint, float refractionIndexFromRayComesFrom, float refractionIndexOtherSide, bool createAbsorbationEvent = true)
        {
            this.SurfacePoint = surfacePoint;
            this.DirectionToThisPoint = directionToThisPoint;
            this.RefractionIndexFromRayComesFrom = refractionIndexFromRayComesFrom;
            this.RefractionIndexOtherSide = refractionIndexOtherSide;
            this.Brdf = BrdfFactory.CreateBrdf(surfacePoint, directionToThisPoint, refractionIndexFromRayComesFrom, refractionIndexOtherSide);
            this.createAbsorbationEvent = createAbsorbationEvent;
        }

        //Wie viel Prozent des Lichtes wird reflektiert/gebrochen? Der Rest wird absorbiert
        public float ContinuationPdf
        {
            get
            {
                return createAbsorbationEvent ? this.Brdf.ContinuationPdf : 1; //Führt zu langen BBB-Pfaden wenn das Glas MirrorColor von 1 hat
                //return Math.Min(MagicNumbers.MaxSurfaceContinuationPdf, this.Brdf.ContinuationPdf); //Hiermit habe ich probiert die Fireflys zu verringern aber es hat nicht geklappt. Außerdem fallen viele Media-Fullpathtests wegen Missing-Path um
            }
        }

        //Um diesen Faktor wird die Diffuse Brdf immer noch gewichtet
        public float Albedo
        {
            get
            {
                return this.SurfacePoint.Propertys.Albedo;
            }
        }

        //Gibt an, zu wie viel Prozent das Licht komplett diffuse reflektiert wird (Bei der FresnelFliese ist diese Zahl Variabel und kann erst hier berechnet werden)
        public float DiffusePortion
        {
            get
            {
                return this.Brdf.DiffuseFactor;
            }
        }

        //Nur true, wenn Brdf-Funktion Dirac Delta-Funktion enthält
        public bool IsOnlySpecular
        {
            get
            {
                return this.Brdf.IsSpecularBrdf;
            }
        }

        //Um den SubpathSampler testbar zu machen, muss ich IBrdfSampler von außen reingeben
        public BrdfSampleEvent SampleDirection(IBrdfSampler sampler, IRandom rand)
        {
            return sampler.CreateDirection(this, rand);
        }


        //Gibt an, wie viel Prozent des Lichtes in Richtung 'outDirection' fliegt und wie hoch die Wahrscheinlichkeit ist,
        //wenn ich eine Richtung sample, dass sie in Richtung outDirection zeigt
        public BrdfEvaluateResult Evaluate(Vector3D outDirection)
        {
            if (this.IsOnlySpecular) return null;     // Die Wahrscheinlichkeit, dass der Reflektierte directionToBrdfPoint-Vektor == outDirection geht gegen 0 wegen die DiracDelta-Funktion

            float inDot = (-this.DirectionToThisPoint) * this.SurfacePoint.ShadedNormal;
            float outDot = outDirection * this.SurfacePoint.ShadedNormal;

            bool inAndOutOnDifferentSides = (inDot < 0.0) ^ (outDot < 0.0);
            if (inAndOutOnDifferentSides && this.Brdf.CanCreateRefractedRays == false) return null; //Bei Nicht-Glas-Punkt muss In- und Out auf gleicher Seite liegen

            var result = new BrdfEvaluateResult()
            {
                Brdf = this.Brdf.Evaluate(this.DirectionToThisPoint, outDirection),
                PdfW = this.Brdf.PdfW(this.DirectionToThisPoint, outDirection),
                PdfWReverse = this.Brdf.PdfW(-outDirection, -this.DirectionToThisPoint),
                CosThetaOut = Math.Abs(outDot) //Bei Microfacet-Glas kann es beim Lighttracing passieren, dass das hier Minus ist, da die Normale vom LightSub-Path in die andere Richtung zeigt wie die Normale, die die Kamera sieht
            };

            float continuationPdf = Math.Min(1, Math.Max(MagicNumbers.MinSurfaceContinuationPdf, this.ContinuationPdf));

            result.PdfW *= continuationPdf; //PathSelectionPdf ist bereits in PdfW durch GetResult1 enthalten
            result.PdfWReverse *= continuationPdf;

            return result;
        }
    }
}
