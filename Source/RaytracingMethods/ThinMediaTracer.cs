using RaytracingColorEstimator;
using FullPathGenerator;
using GraphicGlobal;
using SubpathGenerator;

namespace RaytracingMethods
{
    //Wenn man eine Szene mit Godrays/gerichteten Lichtquellen hat, dann funktioniert das Verfahren hier nur schlecht oder sogar garnicht(Spiegelkornellbox)
    public class ThinMediaTracer : IPixelEstimator
    {
        private PixelRadianceCalculator pixelRadianceCalculator;

        public bool CreatesLigthPaths { get; } = false;

        private readonly bool useSegmentSamplingForDirectLightingOnEdge;
        private readonly bool useSingleScattering;

        public ThinMediaTracer(bool useSegmentSamplingForDirectLightingOnEdge, bool useSingleScattering)
        {
            this.useSegmentSamplingForDirectLightingOnEdge = useSegmentSamplingForDirectLightingOnEdge;
            this.useSingleScattering = useSingleScattering;
        }

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.pixelRadianceCalculator = PixelRadianceCreationHelper.CreatePixelRadianceCalculator(data,
                new SubPathSettings()
                {
                    EyePathType = useSingleScattering ? PathSamplingType.ParticipatingMediaWithoutDistanceSampling : PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling,
                    LightPathType = PathSamplingType.None
                },
                new FullPathSettings()
                {
                    UsePathTracing = true,
                    UseDirectLighting = true,
                    UseDirectLightingOnEdge = true,
                    //UseLightTracing = true,
                    //UseLightTracingOnEdge = true,
                    UseSegmentSamplingForDirectLightingOnEdge = useSegmentSamplingForDirectLightingOnEdge,
                    DoSingleScattering = useSingleScattering,
                });
        }


        

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {            
            return this.pixelRadianceCalculator.SampleSingleEyePath(x, y, rand);
        }
    }
}
