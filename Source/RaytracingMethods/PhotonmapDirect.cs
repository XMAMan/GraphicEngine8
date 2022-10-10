using RaytracingColorEstimator;
using FullPathGenerator;
using GraphicGlobal;
using SubpathGenerator;

namespace RaytracingMethods
{
    public class PhotonmapDirect : IPixelEstimator
    {
        private PixelRadianceCalculator pixelRadianceCalculator;

        public bool CreatesLigthPaths { get; } = false;

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.pixelRadianceCalculator = PixelRadianceCreationHelper.CreatePixelRadianceCalculator(data,
                new SubPathSettings()
                {
                    EyePathType = PathSamplingType.NoMedia,
                    LightPathType = PathSamplingType.NoMedia,
                    MaxEyePathLength = 2
                },
                new FullPathSettings()
                {
                    UsePathTracing = false,
                    UseDirectLighting = false,
                    UseVertexMerging = true,
                    WithoutMis = true
                });

            this.pixelRadianceCalculator.FrameData.PhotonMaps = this.pixelRadianceCalculator.CreateSurfacePhotonmapWithMultipleThreads();
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            //this.pixelRadianceCalculator.PhotonMaps.GlobalSurfacePhotonmap.SearchRadius = this.pixelRadianceCalculator.GetPixelFootprint(x, y) * 2;

            return this.pixelRadianceCalculator.SampleSingleEyePath(x, y, rand);
        }
    }
}
