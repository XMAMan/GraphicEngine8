using System;
using GraphicGlobal;
using GraphicMinimal;
using RayTracerGlobal;

namespace ParticipatingMedia.DistanceSampling
{
    public class HomogenDistanceSampler : IDistanceSampler
    {
        private float minPositiveAttenuationCoeffizent;
        private bool hasScattering;

        public HomogenDistanceSampler(Vector3D attenuationCoeffizent, bool hasScattering)
        {
            this.minPositiveAttenuationCoeffizent = attenuationCoeffizent.X;
            if (attenuationCoeffizent.Y > 0 && (this.minPositiveAttenuationCoeffizent == 0 || attenuationCoeffizent.Y < this.minPositiveAttenuationCoeffizent)) this.minPositiveAttenuationCoeffizent = attenuationCoeffizent.Y;
            if (attenuationCoeffizent.Z > 0 && (this.minPositiveAttenuationCoeffizent == 0 || attenuationCoeffizent.Z < this.minPositiveAttenuationCoeffizent)) this.minPositiveAttenuationCoeffizent = attenuationCoeffizent.Z;
            this.hasScattering = hasScattering;
        }

        public RaySampleResult SampleRayPositionWithPdfFromRayMinToInfinity(Ray ray, float rayMin, float rayMax, IRandom rand, bool startPointIsOnParticleInMedia)
        {
            float maxSampleDistance = rayMax - rayMin;

            var distanceResult = SampleDistance(maxSampleDistance, rand, startPointIsOnParticleInMedia);
            return new RaySampleResult()
            {
                RayPosition = rayMin + distanceResult.Distance,
                PdfL = distanceResult.PdfL,
                ReversePdfL = distanceResult.PdfLReverse
            };
        }

        public DistancePdf GetSamplePdfFromRayMinToInfinity(Ray ray, float rayMin, float rayMax, float sampledRayPosition, bool startPointIsOnParticleInMedium, bool endPointIsOnParticleInMedium)
        {
            if (this.minPositiveAttenuationCoeffizent > 0 && this.hasScattering)
            {
                float distance = Math.Min(rayMax - rayMin, sampledRayPosition - rayMin); //Wenn ich eine MediaSubline (BeamBeam-FullpathSampler) erzeuge, will ich nur bis zur Hälfte von ein Segment gehen
                float goThroughMediaPdf = Math.Max(EvaluateAttenuationInOneDim(this.minPositiveAttenuationCoeffizent, distance), 1e-35f);
                float stopInMediaPdf = goThroughMediaPdf * this.minPositiveAttenuationCoeffizent;

                return new DistancePdf()
                {
                    PdfL = endPointIsOnParticleInMedium ? stopInMediaPdf : goThroughMediaPdf,
                    ReversePdfL = startPointIsOnParticleInMedium ? stopInMediaPdf : goThroughMediaPdf
                };
            }
            else
            {
                return new DistancePdf()
                {
                    PdfL = 1,
                    ReversePdfL = 1
                };
            }
        }

        //Wenn startpointIsInMedium == false ist, dann heißt das, der Strahl dringt vom Media-Border-Punkt gerade in das Medium ein
        private DistanceSamplingResult SampleDistance(float maxdistance, IRandom rand, bool startPointIsOnParticleInMedia)
        {
            if (this.minPositiveAttenuationCoeffizent > 0 && this.hasScattering) //Gibt es Scattering-Teilchen? Nur wenn ja, macht es Sinn zu sampeln, da Photon bei nächsten Media-Surface weiter fliegen soll
            {
                //rand.NextDouble() == Math.Exp(-Distance * this.minPositiveAttenuationCoeffizent)
                float s = Math.Max(-(float)Math.Log(rand.NextDouble()) / this.minPositiveAttenuationCoeffizent, MagicNumbers.MinAllowedPathPointDistance);
                //float s = -(float)Math.Log(rand.NextDouble()) / this.minPositiveAttenuationCoeffizent;

                if (s < maxdistance) //Gesampelte Distanz liegt vor der Media-Grenze
                {
                    float att = EvaluateAttenuationInOneDim(this.minPositiveAttenuationCoeffizent, s);

                    float pdfL = this.minPositiveAttenuationCoeffizent * att;

                    return new DistanceSamplingResult()
                    {
                        Distance = s,
                        PdfL = pdfL,
                        PdfLReverse = startPointIsOnParticleInMedia ? pdfL : att
                    };
                }
                else //Gesampelte Distanz liegt hinter der Media-Grenze
                {
                    float att = EvaluateAttenuationInOneDim(this.minPositiveAttenuationCoeffizent, maxdistance);

                    return new DistanceSamplingResult()
                    {
                        Distance = maxdistance,
                        PdfL = att,
                        PdfLReverse = startPointIsOnParticleInMedia ? (this.minPositiveAttenuationCoeffizent * att) : att
                    };
                }
            }
            else //Kein sampeln möglich, da keine Scattering-Teilchen vorhanden
            {
                return new DistanceSamplingResult()
                {
                    Distance = maxdistance,
                    PdfL = 1,
                    PdfLReverse = 1
                };
            }
        }

        public RaySampleResult SampleRayPositionWithPdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, IRandom rand)
        {
            if (this.minPositiveAttenuationCoeffizent > 0 && this.hasScattering)
            {
                float distance = rayMax - rayMin;
                double u = rand.NextDouble();
                double cdfSMax = 1 - Math.Exp(-this.minPositiveAttenuationCoeffizent * distance);
                double s = -Math.Log(1 - u * cdfSMax) / this.minPositiveAttenuationCoeffizent;
                return new RaySampleResult()
                {
                    RayPosition = rayMin + (float)s,
                    PdfL = this.minPositiveAttenuationCoeffizent * (float)(Math.Exp(-this.minPositiveAttenuationCoeffizent * s) / cdfSMax),
                };
            }
            else
            {
                return new RaySampleResult()
                {
                    RayPosition = rayMax,
                    PdfL = 1,
                };
            }

        }

        public DistancePdf GetSamplePdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, float sampledRayPosition)
        {
            float distance = rayMax - rayMin;
            double cdfSMax = 1 - Math.Exp(-this.minPositiveAttenuationCoeffizent * distance);
            return new DistancePdf()
            {
                PdfL = this.minPositiveAttenuationCoeffizent * (float)(Math.Exp(-this.minPositiveAttenuationCoeffizent * (sampledRayPosition - rayMin)) / cdfSMax),
            };
        }

        class DistanceSamplingResult
        {
            public float Distance;
            public float PdfL;      //Pdf with respect to differential distance measure dp / dL
            public float PdfLReverse;
        }

        private static float EvaluateAttenuationInOneDim(float attenuationCoeffizent, float distance)
        {
            return (float)Math.Exp(-attenuationCoeffizent * distance);
        }
    }
}
