using RaytracingColorEstimator;
using FullPathGenerator;
using GraphicGlobal;
using SubpathGenerator;
using Photonusmap;
using GraphicMinimal;

namespace RaytracingMethods
{
    //Media(SingleScattering mit konstanter Beammap) über Beam2Beam; Der Rest ohne Media und BDPT
    public class MediaBeamTracer : IFrameEstimator
    {
        private PixelRadianceCalculator pixelRadianceCalculator;
        private readonly bool createNewBeamMapInEachFrame = false;

        public bool CreatesLigthPaths { get; } = true;

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.pixelRadianceCalculator = PixelRadianceCreationHelper.CreatePixelRadianceCalculator(data,
                new SubPathSettings()
                {
                    EyePathType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                    LightPathType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling
                },
                new FullPathSettings()
                {
                    UsePathTracing = true,
                    UseDirectLighting = true,
                    //UseMultipleDirectLighting = true,
                    UseVertexConnection = true,
                    UseLightTracing = true,
                    UseBeamDataLineQuery = true,
                    DoSingleScattering = true
                });

            //this.pixelRadianceCalculator.PixelPhotonenCounter = new PixelPhotonenCounter(this.pixelRadianceCalculator, this.ScreenWidth, this.ScreenHeight);
            if (this.createNewBeamMapInEachFrame == false)
            {
                data.ProgressChanged("Erstelle Beammap", 0);                
                CreateBeamMap(new Rand(0));
                data.ProgressChanged("Fertig mit der Beammap-Erstellung", 1000);
            }
        }


        public MediaBeamTracer() { }
        private MediaBeamTracer(MediaBeamTracer copy)
        {
            this.pixelRadianceCalculator = new PixelRadianceCalculator(copy.pixelRadianceCalculator);

            //Nimm die eine Beam-Map und rette sie rüber
            if (this.createNewBeamMapInEachFrame == false)
                this.pixelRadianceCalculator.FrameData = copy.pixelRadianceCalculator.FrameData;
        }

        public IFrameEstimator CreateCopy()
        {
            return new MediaBeamTracer(this);
        }

        public void DoFramePrepareStep(int frameIterationCount, IRandom rand)
        {
            //this.pixelRadianceCalculator.PixelPhotonenCounter.FrameIterationCount = frameIterationCount;
            if (this.createNewBeamMapInEachFrame) CreateBeamMap(rand);
        }

        private void CreateBeamMap(IRandom rand)
        {
            this.pixelRadianceCalculator.FrameData.PhotonMaps = this.pixelRadianceCalculator.CreateVolumetricBeammap(rand, this.pixelRadianceCalculator.GlobalProps.SearchRadiusForMediaBeamTracer);
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
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
