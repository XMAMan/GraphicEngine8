using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia.Media;
using RayTracerGlobal;
using RaytracingBrdf.SampleAndRequest;
using System;

namespace RaytracingBrdf
{
    public interface IPhaseFunctionSampler
    {
        BrdfSampleEvent SampleDirection(MediaIntersectionPoint mediaPoint, Vector3D directionToMediaPoint, IRandom rand);
        BrdfEvaluateResult EvaluateBsdf(Vector3D directionToBrdfPoint, MediaIntersectionPoint brdfPoint, Vector3D outDirection);
    }

    public class PhaseFunction : IPhaseFunctionSampler
    {
        private readonly bool createAbsorbationEvent;
        public PhaseFunction(bool createAbsorbationEvent = true)
        {
            this.createAbsorbationEvent = createAbsorbationEvent;
        }

        public BrdfSampleEvent SampleDirection(MediaIntersectionPoint mediaPoint, Vector3D directionToMediaPoint, IRandom rand)
        {
            IParticipatingMedia medium = mediaPoint.CurrentMedium;

            Vector3D os = medium.GetScatteringCoeffizient(mediaPoint.Position);
            Vector3D oa = medium.GetAbsorbationCoeffizient(mediaPoint.Position);
            float continuationPdf = MediaContinuationPdf(os, oa);

            if (os.Max() == 0) return null; //Es gibt hier keine Scatterteilchen, welche die Richtung ändern könnten

            if (rand != null && continuationPdf != 1 && rand.NextDouble() >= continuationPdf)      // Absorbation
                return null;

            var phaseResult = medium.PhaseFunction.SampleDirection(mediaPoint.Position, directionToMediaPoint, rand);
            return new BrdfSampleEvent()
            {
                Brdf = os * phaseResult.BrdfDividedByPdfW / continuationPdf,
                ExcludedObject = null,
                IsSpecualarReflected = false,
                PdfW = phaseResult.PdfW * continuationPdf,
                PdfWReverse = phaseResult.PdfWReverse * continuationPdf,
                Ray = phaseResult.Ray,
                RayWasRefracted = false
            };
        }

        //Gibt das Produk aus Phasenfunktion und Scattering-Coeffizient zurück
        public BrdfEvaluateResult EvaluateBsdf(Vector3D directionToBrdfPoint, MediaIntersectionPoint brdfPoint, Vector3D outDirection)
        {
            var medium = brdfPoint.CurrentMedium;

            Vector3D os = medium.GetScatteringCoeffizient(brdfPoint.Position);
            Vector3D oa = medium.GetAbsorbationCoeffizient(brdfPoint.Position);
            float continuationPdf = MediaContinuationPdf(os, oa);

            if (os.Max() == 0) return null; //Es gibt hier keine Scatterteilchen, welche die Richtung ändern könnten
            
            var phaseResult = medium.PhaseFunction.GetBrdf(directionToBrdfPoint, brdfPoint.Position, outDirection);

            return new BrdfEvaluateResult()
            {
                Brdf = phaseResult.Brdf * os,
                PdfW = phaseResult.PdfW * continuationPdf,
                PdfWReverse = phaseResult.PdfWReverse * continuationPdf,
                CosThetaOut = 1
            };
        }

        private float MediaContinuationPdf(Vector3D os, Vector3D oa)
        {
            if (this.createAbsorbationEvent == false) return 1;

            //SmallUPBP->Scene.hxx->Zeile 1219 hier steht die Formel für die Media-ContinationPdf
            float continuationPdf = Math.Max(Math.Max(os.X / (os.X + oa.X), os.Y / (os.Y + oa.Y)), os.Z / (os.Z + oa.Z));
            if (float.IsNaN(continuationPdf) || float.IsInfinity(continuationPdf))
                continuationPdf = 1;
            else
                continuationPdf = Math.Max(continuationPdf, MagicNumbers.MediaMinContinuationPdf); //Stelle sicher, dass eine möglichst hohe ContinationPdf genommen wird um Rauschen zu vermindern

            return continuationPdf;
        }
    }
}
