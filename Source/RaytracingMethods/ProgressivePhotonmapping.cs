using GraphicMinimal;
using RaytracingColorEstimator;
using FullPathGenerator;
using SubpathGenerator;
using GraphicGlobal;
using Photonusmap;

namespace RaytracingMethods
{
    public class ProgressivePhotonmapping : IFrameEstimator
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
                    UseSpecularPathTracing = true,
                    UseVertexMerging = true
                });

            this.pixelRadianceCalculator.PixelPhotonenCounter = new PixelPhotonenCounter(this.pixelRadianceCalculator);
        }
        public ProgressivePhotonmapping() { }

        private ProgressivePhotonmapping(ProgressivePhotonmapping copy)
        {
            this.pixelRadianceCalculator = new PixelRadianceCalculator(copy.pixelRadianceCalculator);
        }

        public IFrameEstimator CreateCopy()
        {
            return new ProgressivePhotonmapping(this);
        }

        public void DoFramePrepareStep(ImagePixelRange range, int frameIterationCount, IRandom rand)
        {
            this.pixelRadianceCalculator.PixelPhotonenCounter.FrameIterationCount = frameIterationCount;
            this.pixelRadianceCalculator.FrameData.PhotonMaps = new Photonmaps() { GlobalSurfacePhotonmap = this.pixelRadianceCalculator.CreateSurfacePhotonmapWithSingleThread(rand) };
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            this.pixelRadianceCalculator.FrameData.PhotonMaps.GlobalSurfacePhotonmap.SearchRadius = this.pixelRadianceCalculator.PixelPhotonenCounter.GetSearchRadiusForPixel(x, y) * 2;
            
            //this.pixelRadianceCalculator.FrameData.PhotonMaps.GlobalSurfacePhotonmap.SearchRadius /= 85;
            //float pixelFootPrint = this.pixelRadianceCalculator.GetExactPixelFootprintArea(x, y);
            //this.pixelRadianceCalculator.FrameData.PhotonMaps.GlobalSurfacePhotonmap.SearchRadius = (float)Math.Sqrt(pixelFootPrint / Math.PI);

            var result = this.pixelRadianceCalculator.SampleSingleEyePath(x, y, rand);
            this.pixelRadianceCalculator.PixelPhotonenCounter.AddPhotonCounterForPixel(x, y, result.CollectedVertexMergingPhotonCount);

            //if (this.pixelRadianceCalculator.IsEdgePixel(x, y)) result.RadianceFromRequestetPixel = new Vector3D(1, 1, 0); //Das ist ein übelst cooler Effekt

            return result;
        }
    }
}
