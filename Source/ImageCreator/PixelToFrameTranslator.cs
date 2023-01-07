using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;
using RaytracingColorEstimator;

namespace ImageCreator
{
    public class PixelToFrameTranslator : IFrameEstimator
    {
        
        private IPixelEstimator pixelEstimator;

        public bool CreatesLigthPaths { get; } = false;

        public PixelToFrameTranslator(IPixelEstimator pixelEstimator)
        {
            this.pixelEstimator = pixelEstimator;
        }

        public void BuildUp(RaytracingFrame3DData data)
        {
            //Ich muss nichts tun da der Pixelestimator schon gebaut wurde
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            return this.pixelEstimator.GetFullPathSampleResult(x, y, rand);
        }

        public void DoFramePrepareStep(int frameIterationCount, IRandom rand)
        {
        }

        public ImageBuffer DoFramePostprocessing(int frameIterationNumber, ImageBuffer frame)
        {
            return frame;
        }

        public IFrameEstimator CreateCopy()
        {
            return new PixelToFrameTranslator(this.pixelEstimator);
        }
    }
}
