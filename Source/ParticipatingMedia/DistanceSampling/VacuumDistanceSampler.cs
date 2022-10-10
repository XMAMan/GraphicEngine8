using GraphicGlobal;

namespace ParticipatingMedia.DistanceSampling
{
    public class VacuumDistanceSampler : IDistanceSampler
    {
        public RaySampleResult SampleRayPositionWithPdfFromRayMinToInfinity(Ray ray, float rayMin, float rayMax, IRandom rand, bool startPointIsOnParticleInMedia)
        {
            return new RaySampleResult()
            {
                RayPosition = rayMax,
                PdfL = 1,
                ReversePdfL = 1
            };
        }

        public DistancePdf GetSamplePdfFromRayMinToInfinity(Ray ray, float rayMin, float rayMax, float sampledRayPosition, bool startPointIsOnParticleInMedium, bool endPointIsOnParticleInMedium)
        {
            return new DistancePdf()
            {
                 PdfL = 1,
                 ReversePdfL = 1
            };
        }

        public RaySampleResult SampleRayPositionWithPdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, IRandom rand)
        {
            return new RaySampleResult()
            {
                RayPosition = rayMax,
                PdfL = 1,
                ReversePdfL = 1
            };
        }

        public DistancePdf GetSamplePdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, float sampledRayPosition)
        {
            return new DistancePdf()
            {
                PdfL = 1,
                ReversePdfL = 1
            };
        }
    }
}
