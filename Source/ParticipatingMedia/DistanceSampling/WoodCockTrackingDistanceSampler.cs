using System;
using GraphicGlobal;
using RayTracerGlobal;

namespace ParticipatingMedia.DistanceSampling
{
    //Quelle: 'Volumetric Path Tracing - Steve Marschner 2012'
    //Idee: Fülle das Medium mit NoOp-Partikeln auf, bis es überall mit Dichte von 'maxAttenuationCoeffizient' gefüllt ist
    //Durchlaufe nun Medium so, als ob es gleichmäßige Dichte hat. Nehme Sample nur an, wenn RejectionSchritt erfüllt ist
    public class WoodCockTrackingDistanceSampler : IDistanceSampler
    {
        private IMediaOnWaveLength mediaOnWave;
        private double maxAttenuationCoeffizient;
        public WoodCockTrackingDistanceSampler(IMediaOnWaveLength mediaOnWave)
        {
            this.mediaOnWave = mediaOnWave;
            this.maxAttenuationCoeffizient = mediaOnWave.MaxExtinctionCoeffizient.Z;
        }

        public RaySampleResult SampleRayPositionWithPdfFromRayMinToInfinity(Ray ray, float rayMin, float rayMax, IRandom rand, bool startPointIsOnParticleInMedia)
        {
            double rayPosition = rayMin;
            //while (true)
            for (int i=0;i<1000;i++)
            {
                double u = rand.NextDouble();
                rayPosition -= Math.Log(u) / this.maxAttenuationCoeffizient;
                rayPosition = Math.Max(MagicNumbers.MinAllowedPathPointDistance, rayPosition);
                if (rayPosition > rayMax)
                {
                    //Gesampelte Distanz liegt hinter der Media-Grenze
                    float att = this.mediaOnWave.EvaluateAttenuationOnWave(ray, rayMin, rayMax);
                    return new RaySampleResult()
                    {
                        RayPosition = rayMax,
                        PdfL = att,
                        ReversePdfL = startPointIsOnParticleInMedia ? (this.mediaOnWave.ExtinctionCoeffizientOnWave(ray.Start + ray.Direction * rayMin) * att) : att
                    };
                }
                float ot = this.mediaOnWave.ExtinctionCoeffizientOnWave(ray.Start + ray.Direction * (float)rayPosition);
                if (rand.NextDouble() < ot / this.maxAttenuationCoeffizient)
                {
                    //Gesampelte Distanz liegt vor der Media-Grenze
                    float att = this.mediaOnWave.EvaluateAttenuationOnWave(ray, rayMin, (float)rayPosition);
                    float pdfL = ot * att;

                    return new RaySampleResult()
                    {
                        RayPosition = (float)rayPosition,
                        PdfL = pdfL,
                        ReversePdfL = startPointIsOnParticleInMedia ? pdfL : att
                    };
                }
            }

            throw new Exception("Unendlichkeitsschleife beim WoodCockTracking");
        }

        public DistancePdf GetSamplePdfFromRayMinToInfinity(Ray ray, float rayMin, float rayMax, float sampledRayPosition, bool startPointIsOnParticleInMedium, bool endPointIsOnParticleInMedium)
        {
            float goThroughMediaPdf = Math.Max(this.mediaOnWave.EvaluateAttenuationOnWave(ray, rayMin, sampledRayPosition), 1e-35f);
            float stopInMediaPdf = goThroughMediaPdf * this.mediaOnWave.ExtinctionCoeffizientOnWave(ray.Start + ray.Direction * sampledRayPosition);

            return new DistancePdf()
            {
                PdfL = endPointIsOnParticleInMedium ? stopInMediaPdf : goThroughMediaPdf,
                ReversePdfL = startPointIsOnParticleInMedium ? stopInMediaPdf : goThroughMediaPdf
            };
        }

        //Idee: Sample so, als ob Medium mit lauter No-Op-Partikeln gefüllt ist und nimm das Sample mit Gewicht (Scattering-Term)
        //Quelle für Idee: Unbiased Global Illumination with Participating Media - Raab et al (2008)
        public RaySampleResult SampleRayPositionWithPdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, IRandom rand)
        {
            double u = rand.NextDouble();
            double pv = 1 - Math.Exp(-this.maxAttenuationCoeffizient * (rayMax - rayMin));
            double rayPosition = rayMin - Math.Log(1 - u * pv) / this.maxAttenuationCoeffizient;

            //Gesampelte Distanz liegt vor der Media-Grenze
            float att = this.mediaOnWave.EvaluateAttenuationOnWave(ray, rayMin, (float)rayPosition);
            float ot = this.mediaOnWave.ExtinctionCoeffizientOnWave(ray.Start + ray.Direction * (float)rayPosition);
            double pdfL = this.maxAttenuationCoeffizient * att / pv;

            return new RaySampleResult()
            {
                RayPosition = (float)rayPosition,
                PdfL = (float)pdfL,
            };
        }

        public DistancePdf GetSamplePdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, float sampledRayPosition)
        {
            double pv = 1 - Math.Exp(-this.maxAttenuationCoeffizient * (rayMax - rayMin));
            float att = this.mediaOnWave.EvaluateAttenuationOnWave(ray, rayMin, sampledRayPosition);
            float ot = this.mediaOnWave.ExtinctionCoeffizientOnWave(ray.Start + ray.Direction * sampledRayPosition);
            double pdfL = this.maxAttenuationCoeffizient * att / pv;

            return new DistancePdf()
            {
                PdfL = (float)pdfL,
            };
        }
    }






    //Diese Klasse verwende ich erstmal behelfsmäßig, damit mein DirectLightingOnEdge den Media-Distancesampler verwenden kann und somit der FullPathSampler-Test grün wird
    public class WoodCockTrackingDistanceSamplerWithEqualSegmentSampling : IDistanceSampler
    {
        private IMediaOnWaveLength mediaOnWave;
        private double maxExtinctionCoeffizient;
        public WoodCockTrackingDistanceSamplerWithEqualSegmentSampling(IMediaOnWaveLength mediaOnWave)
        {
            this.mediaOnWave = mediaOnWave;
            this.maxExtinctionCoeffizient = mediaOnWave.MaxExtinctionCoeffizient.Z;
        }

        public RaySampleResult SampleRayPositionWithPdfFromRayMinToInfinity(Ray ray, float rayMin, float rayMax, IRandom rand, bool startPointIsOnParticleInMedia)
        {
            //Distanzsampling ist nicht möglich, wenn nicht genug Platz zwischen rayMin und rayMax ist
            if (rayMax - rayMin < 2 * MagicNumbers.MinAllowedPathPointDistance)
            {
                //Gesampelte Distanz liegt hinter der Media-Grenze
                float att = this.mediaOnWave.EvaluateAttenuationOnWave(ray, rayMin, rayMax);
                return new RaySampleResult()
                {
                    RayPosition = rayMax,
                    PdfL = att,
                    ReversePdfL = startPointIsOnParticleInMedia ? (this.mediaOnWave.ExtinctionCoeffizientOnWave(ray.Start + ray.Direction * rayMin) * att) : att
                };
            }

            double rayPosition = rayMin;
            //while (true)
            for (int i = 0; i < 1000; i++)
            {
                double u = rand.NextDouble();
                rayPosition -= Math.Log(u) / this.maxExtinctionCoeffizient;

                if (rayPosition > rayMax || i == 999)
                {
                    //Gesampelte Distanz liegt hinter der Media-Grenze
                    float att = this.mediaOnWave.EvaluateAttenuationOnWave(ray, rayMin, rayMax); 
                    return new RaySampleResult()
                    {
                        RayPosition = rayMax,
                        PdfL = att,
                        ReversePdfL = startPointIsOnParticleInMedia ? (this.mediaOnWave.ExtinctionCoeffizientOnWave(ray.Start + ray.Direction * rayMin) * att) : att
                    };
                }

                //Ein Media-Parikel, was an diesen Punkt erzeugt werden wird muss stets den Minimal-Abstand zu den Nachbarpunkten einhalten
                //Mache das Clamping bevor der ExtinctionCoeffizient in der ot-Variable gespeichert wird, um sicherzustellen, dass rayPosition == Der Position, wo ot 
                //abgefragt wird, da sonst PhaseFunction-Direction-Sampling nicht geht, wenn es an Position stattfindet, wo ot == 0 ist. 
                if (rayPosition < rayMin + MagicNumbers.MinAllowedPathPointDistance) rayPosition = rayMin + MagicNumbers.MinAllowedPathPointDistance;
                if (rayPosition > rayMax - MagicNumbers.MinAllowedPathPointDistance) rayPosition = rayMax - MagicNumbers.MinAllowedPathPointDistance;

                float ot = this.mediaOnWave.ExtinctionCoeffizientOnWave(ray.Start + ray.Direction * (float)rayPosition);
                if (rand.NextDouble() < ot / this.maxExtinctionCoeffizient)
                {
                    //Gesampelte Distanz liegt vor der Media-Grenze
                    float att = this.mediaOnWave.EvaluateAttenuationOnWave(ray, rayMin, (float)rayPosition);
                    float pdfL = ot * att;                    

                    return new RaySampleResult()
                    {
                        RayPosition = (float)rayPosition,                        
                        PdfL = pdfL,
                        ReversePdfL = startPointIsOnParticleInMedia ? pdfL : att
                    };
                }
            }

            throw new Exception("Unendlichkeitsschleife beim WoodCockTracking");
        }

        public DistancePdf GetSamplePdfFromRayMinToInfinity(Ray ray, float rayMin, float rayMax, float sampledRayPosition, bool startPointIsOnParticleInMedium, bool endPointIsOnParticleInMedium)
        {
            float goThroughMediaPdf = Math.Max(this.mediaOnWave.EvaluateAttenuationOnWave(ray, rayMin, sampledRayPosition), 1e-35f);
            float stopInMediaPdf = goThroughMediaPdf * this.mediaOnWave.ExtinctionCoeffizientOnWave(ray.Start + ray.Direction * sampledRayPosition);

            return new DistancePdf()
            {
                PdfL = endPointIsOnParticleInMedium ? stopInMediaPdf : goThroughMediaPdf,
                ReversePdfL = startPointIsOnParticleInMedium ? stopInMediaPdf : goThroughMediaPdf
            };
        }

        //Sample gleichmäßig, da das 'echte' Woodcooksampling noch nicht richtig klappt (Siehe Klasse oben)
        public RaySampleResult SampleRayPositionWithPdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, IRandom rand)
        {
            return new RaySampleResult()
            {
                RayPosition = (float)(rand.NextDouble() * (rayMax - rayMin) + rayMin), //Gleichmäßig sampeln
                PdfL = 1.0f / (rayMax - rayMin),
            };
        }

        public DistancePdf GetSamplePdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, float sampledRayPosition)
        {
            return new DistancePdf()
            {
                PdfL = 1.0f / (rayMax - rayMin),
            };
        }
    }
}
