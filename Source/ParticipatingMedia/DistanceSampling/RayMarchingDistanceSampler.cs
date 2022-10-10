using System;
using GraphicGlobal;

namespace ParticipatingMedia.DistanceSampling
{
    //Quelle für Biased RayMarching 'Volumetric Path Tracing - Steve Marschner 2012'
    //Idee, das man das auch unbiased betreiben kann: 'Unbiased Global Illumination with Participating Media - Raab et al (2008)'
    //Vorgehen: Wenn ich eine Zufallszahl im Bereich von 0 bis 1 gleichmäßig erzeuge und sage, dass ist der Attenuation-Term,
    //dann führt das dazu, dass der zugehörige Distanzwert zu diesen Attenuation-Wert mit der Pdf ExtectionCoeffizient(t)*Attenuation(t) gesampelt wird
    //Ich suche nun nach dem Distanzwert, dessen Attenuation dem Zufallswert entspricht. Anstatt aber das t, welches der 
    //Gleichung u=e^(-OpticalDeep(t)) genügt zu suchen, stelle ich die Gleichung nach -ln(u)=OpticalDeep(t) um und berechne nun per Zufallsriemanintegral
    //den Attenuation-Term und schaue, ob er der Gleichung entspricht
    public class RayMarchingDistanceSampler : IDistanceSampler
    {
        private IMediaOnWaveLength mediaOnWave;
        public RayMarchingDistanceSampler(IMediaOnWaveLength mediaOnWave)
        {
            this.mediaOnWave = mediaOnWave;
        }

        public RaySampleResult SampleRayPositionWithPdfFromRayMinToInfinity(Ray ray, float rayMin, float rayMax, IRandom rand, bool startPointIsOnParticleInMedia)
        {
            float maxdistance = rayMax - rayMin;
            double attenuation = rand.NextDouble();
            double opticalDeeph = -Math.Log(attenuation);
            double opticalDeephSum = 0;
            double sampleDistance = 0;
            double stepWidth = maxdistance / 20;
            float ot = float.NaN;
            while (sampleDistance < maxdistance && opticalDeephSum < opticalDeeph)
            {
                ot = this.mediaOnWave.ExtinctionCoeffizientOnWave(ray.Start + ray.Direction * ((float)(rayMin + sampleDistance + stepWidth * rand.NextDouble())));
                opticalDeephSum += ot * stepWidth;
                sampleDistance += stepWidth;
            }

            //Was führte zum Abbruch? OpticalDeeph-Sum oder MaxSampleDistance?
            if (opticalDeephSum > opticalDeeph)
            {
                //Gesampelte Distanz liegt vor der Media-Grenze
                sampleDistance -= (opticalDeephSum - opticalDeeph) / ot;
                
                sampleDistance += rayMin;
                float att = (float)attenuation;// this.mediaOnWave.EvaluateAttenuation(ray, rayMin, (float)sampleDistance);
                float pdfL = this.mediaOnWave.ExtinctionCoeffizientOnWave(ray.Start + ray.Direction * (float)sampleDistance) * att;

                return new RaySampleResult()
                {
                    RayPosition = (float)sampleDistance,
                    PdfL = pdfL,
                    ReversePdfL = startPointIsOnParticleInMedia ? pdfL : att
                };
            }
            else
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

        public RaySampleResult SampleRayPositionWithPdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, IRandom rand)
        {
            throw new NotImplementedException();
        }

        public DistancePdf GetSamplePdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, float sampledRayPosition)
        {
            throw new NotImplementedException();
        }
    }
}
