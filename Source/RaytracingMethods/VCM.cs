using GraphicMinimal;
using RaytracingColorEstimator;
using FullPathGenerator;
using SubpathGenerator;
using GraphicGlobal;
using Photonusmap;

namespace RaytracingMethods
{
    public  class VCM : IFrameEstimator
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
                    UseVertexMerging = true,
                    UseLightTracing = true,
                });

            this.pixelRadianceCalculator.PixelPhotonenCounter = new PixelPhotonenCounter(this.pixelRadianceCalculator);
        }

        public VCM() { }
        private VCM(VCM copy)
        {
            this.pixelRadianceCalculator = new PixelRadianceCalculator(copy.pixelRadianceCalculator);
        }

        public IFrameEstimator CreateCopy()
        {
            return new VCM(this);
        }

        public void DoFramePrepareStep(int frameIterationCount, IRandom rand)
        {
            this.pixelRadianceCalculator.PixelPhotonenCounter.FrameIterationCount = frameIterationCount;
            this.pixelRadianceCalculator.FrameData.PhotonMaps = new Photonmaps() { GlobalSurfacePhotonmap = this.pixelRadianceCalculator.CreateSurfacePhotonmapWithSingleThread(rand) };

            //float radiusAlpha = 0.7f;
            //this.photonmap.SearchRadius = this.pixelRadianceCalculator.GetSearchRadiusForPhotonmapWithPhotonDensity(this.photonmap) / (float)Math.Pow(iterationCount + 1, 0.5f * (1 - radiusAlpha));
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            this.pixelRadianceCalculator.FrameData.PhotonMaps.GlobalSurfacePhotonmap.SearchRadius = this.pixelRadianceCalculator.PixelPhotonenCounter.GetSearchRadiusForPixel(x, y);
            var result = this.pixelRadianceCalculator.SampleSingleEyeAndLightPath(x, y, rand);
            this.pixelRadianceCalculator.PixelPhotonenCounter.AddPhotonCounterForPixel(x, y, result.CollectedVertexMergingPhotonCount);
            return result;
        }
    }
}
