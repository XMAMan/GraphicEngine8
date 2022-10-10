using FullPathGenerator;
using GraphicGlobal;
using RaytracingColorEstimator;
using SubpathGenerator;

namespace RaytracingMethods
{
    public class MediaPathTracer : IPixelEstimator
    {
        private PixelRadianceCalculator pixelRadianceCalculator;

        public bool CreatesLigthPaths { get; } = false;

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.pixelRadianceCalculator = PixelRadianceCreationHelper.CreatePixelRadianceCalculator(data,
                new SubPathSettings()
                {
                    EyePathType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                    LightPathType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling
                },
                new FullPathSettings()
                {
                    UsePathTracing = true,                    
                });
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            return this.pixelRadianceCalculator.SampleSingleEyePath(x, y, rand);
        }
    }
}
