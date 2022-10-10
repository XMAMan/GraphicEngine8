using RaytracingColorEstimator;
using FullPathGenerator;
using GraphicGlobal;
using SubpathGenerator;

namespace RaytracingMethods
{
    public class FullBidirectionalPathTracing : IPixelEstimator
    {
        private PixelRadianceCalculator pixelRadianceCalculator;

        public bool CreatesLigthPaths { get; } = true;

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.pixelRadianceCalculator = PixelRadianceCreationHelper.CreatePixelRadianceCalculator(data,
                new SubPathSettings()
                {
                    EyePathType = PathSamplingType.NoMedia,
                    LightPathType = PathSamplingType.NoMedia
                },
                new FullPathSettings()
                {
                    UsePathTracing = true,
                    UseDirectLighting = true,
                      //UseMultipleDirectLighting = true,
                    UseVertexConnection = true,
                    UseLightTracing = true,
                });
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            return this.pixelRadianceCalculator.SampleSingleEyeAndLightPath(x, y, rand);
        }
    }
}
