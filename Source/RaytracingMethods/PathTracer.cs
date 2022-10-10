using RaytracingColorEstimator;
using FullPathGenerator;
using GraphicGlobal;
using SubpathGenerator;

namespace RaytracingMethods
{
    public class PathTracer : IPixelEstimator
    {
        private PixelRadianceCalculator pixelRadianceCalculator;

        public bool CreatesLigthPaths { get; } = false;

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.pixelRadianceCalculator = PixelRadianceCreationHelper.CreatePixelRadianceCalculator(data,
                new SubPathSettings()
                {
                    EyePathType = PathSamplingType.NoMedia,
                    LightPathType = PathSamplingType.None
                },
                new FullPathSettings()
                {
                    UsePathTracing = true,
                    WithoutMis = true
                });
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            return this.pixelRadianceCalculator.SampleSingleEyePath(x, y, rand);
        }
    }
}
