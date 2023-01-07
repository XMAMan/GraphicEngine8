using RaytracingColorEstimator;
using FullPathGenerator;
using GraphicGlobal;
using SubpathGenerator;
using GraphicMinimal;

namespace RaytracingMethods
{
    //Unifying points, beams, and paths in volumetric light transport simulation
    public class UPBP : IFrameEstimator
    {
        private PixelRadianceCalculator pixelRadianceCalculator;
        private readonly bool createPhotonmapForEachFrame = true;
        public bool CreatesLigthPaths { get; } = true;

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.pixelRadianceCalculator = PixelRadianceCreationHelper.CreatePixelRadianceCalculator(data,
                new SubPathSettings()
                {
                    EyePathType = PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling,
                    LightPathType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling
                },
                new FullPathSettings()
                {
                    UsePathTracing = true,                          
                    UseDirectLighting = true,
                    UseVertexConnection = true,
                    UseLightTracing = true,
                    UseVertexMerging = true,
                    UsePointDataPointQuery = true,
                    UsePointDataBeamQuery = true,
                    UseBeamDataLineQuery = true,
                });

            this.pixelRadianceCalculator.PixelPhotonenCounter = new PixelPhotonenCounter(this.pixelRadianceCalculator);

            if (this.createPhotonmapForEachFrame == false)
            {
                float beamDataLineQueryReductionFactor = this.pixelRadianceCalculator.GlobalProps.BeamDataLineQueryReductionFactor;
                this.pixelRadianceCalculator.FrameData.PhotonMaps = this.pixelRadianceCalculator.CreateVolumetricPhotonmap(new Rand(0), beamDataLineQueryReductionFactor, 1);
            }                
        }

        public UPBP() { }
        private UPBP(UPBP copy)
        {
            this.pixelRadianceCalculator = new PixelRadianceCalculator(copy.pixelRadianceCalculator);
            this.pixelRadianceCalculator.FrameData.PhotonMaps = copy.pixelRadianceCalculator.FrameData.PhotonMaps;
            
        }

        public IFrameEstimator CreateCopy()
        {
            return new UPBP(this);
        }

        public void DoFramePrepareStep(int frameIterationCount, IRandom rand)
        {
            this.pixelRadianceCalculator.PixelPhotonenCounter.FrameIterationCount = frameIterationCount;

            if (this.createPhotonmapForEachFrame)
            {
                float beamDataLineQueryReductionFactor = this.pixelRadianceCalculator.GlobalProps.BeamDataLineQueryReductionFactor;// 0.01f;
                this.pixelRadianceCalculator.FrameData.PhotonMaps = this.pixelRadianceCalculator.CreateVolumetricPhotonmap(rand, beamDataLineQueryReductionFactor, frameIterationCount + 1);
            }

            //float radiusAlpha = 0.7f;
            //this.photonmap.SearchRadius = this.pixelRadianceCalculator.GetSearchRadiusForPhotonmapWithPhotonDensity(this.photonmap) / (float)Math.Pow(iterationCount + 1, 0.5f * (1 - radiusAlpha));
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            float searchRadius = this.pixelRadianceCalculator.PixelPhotonenCounter.GetSearchRadiusForPixel(x, y);
            this.pixelRadianceCalculator.FrameData.PhotonMaps.GlobalSurfacePhotonmap.SearchRadius = searchRadius;
            this.pixelRadianceCalculator.FrameData.PhotonMaps.PointDataPointQueryMap.SearchRadius = searchRadius * this.pixelRadianceCalculator.GlobalProps.PhotonmapSearchRadiusFactor;
            
            var result = this.pixelRadianceCalculator.SampleSingleEyeAndLightPath(x, y, rand);
            //this.pixelRadianceCalculator.PixelPhotonenCounter.AddPhotonCounterForPixel(x, y, result.CollectedVertexMergingPhotonCount);
            return result;
        }

        public ImageBuffer DoFramePostprocessing(int frameIterationNumber, ImageBuffer frame)
        {
            return frame;
        }
    }
}
