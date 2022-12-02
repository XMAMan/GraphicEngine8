using GraphicGlobal;
using GraphicMinimal;
using ImageCreator;
using RaytracingColorEstimator;
using System.Collections.Generic;
using System.Threading;

namespace RaytracerMain
{
    class RaytracingHelper
    {
        private readonly IPixelEstimator pixelEstimator = null;
        public RaytracingHelper(IPixelEstimator pixelEstimator)
        {
            this.pixelEstimator = pixelEstimator;
        }

        public Vector3D GetColorFromSinglePixelForDebuggingPurpose(Frame3DData data, RaytracingDebuggingData debuggingData)
        {
            var raytracingFrameData = CreateRaytracingData(data, debuggingData.ScreenSize.Width, debuggingData.ScreenSize.Height, debuggingData.PixelRange);
            var imageCreator = this.pixelEstimator.CreateImageCreator(raytracingFrameData);
            return imageCreator.GetColorFromSinglePixelForDebuggingPurpose(debuggingData);
        }

        #region SinglePixel
        public Vector3D GetColorFromSinglePixel(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            BackgroundColor backgroundColor = new BackgroundColor(data.GlobalObjektPropertys.BackgroundImage, data.GlobalObjektPropertys.BackgroundColorFactor, imageWidth, imageHeight);
            return GetSinglePixelAnalyser(data, imageWidth, imageHeight, pixelRange, pixX, pixY, sampleCount).GetColorFromSinglePixel(backgroundColor);
        }

        public List<Vector3D> GetNPixelSamples(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            BackgroundColor backgroundColor = new BackgroundColor(data.GlobalObjektPropertys.BackgroundImage, data.GlobalObjektPropertys.BackgroundColorFactor, imageWidth, imageHeight);
            return GetSinglePixelAnalyser(data, imageWidth, imageHeight, pixelRange, pixX, pixY, sampleCount).GetNPixelSamples(backgroundColor);
        }

        public string GetFullPathsFromSinglePixel(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            return GetSinglePixelAnalyser(data, imageWidth, imageHeight, pixelRange, pixX, pixY, sampleCount).GetFullPathsFromSinglePixel();
        }
        public string GetPathContributionsForSinglePixel(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            return GetSinglePixelAnalyser(data, imageWidth, imageHeight, pixelRange, pixX, pixY, sampleCount).GetPathContributionsForSinglePixel();
        }
        #endregion
        public float GetBrightnessFactor(Frame3DData data, int imageWidth, int imageHeight)
        {
            var rayMainData = CreateRaytracingData(data, imageWidth, imageHeight, null);
            IPixelEstimator pixelEstimator = this.pixelEstimator.CreateColorEstimator(rayMainData);

            return BrightnessFactorCalculator.GetBrightnessFactor(pixelEstimator, imageWidth, imageHeight, data.GlobalObjektPropertys.SamplingCount);
        }

        public string GetFlippedWavefrontFile(Frame3DData data, int imageWidth, int imageHeight)
        {
            return NormalFlipper.GetFlippedWavefrontFile(data, imageWidth, imageHeight);
        }

        private SinglePixelAnalyser GetSinglePixelAnalyser(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            var rayMainData = CreateRaytracingData(data, imageWidth, imageHeight, pixelRange);
            IPixelEstimator colorEstimator = this.pixelEstimator.CreateColorEstimator(rayMainData);
            return new SinglePixelAnalyser(colorEstimator, pixelRange, pixX, pixY, sampleCount);
        }

        private static RaytracingFrame3DData CreateRaytracingData(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange)
        {
            return new RaytracingFrame3DData(data)
            {
                ScreenWidth = imageWidth,
                ScreenHeight = imageHeight,
                ProgressChanged = (text, percent) => { },
                StopTrigger = new CancellationTokenSource(),
                PixelRange = pixelRange == null ? new ImagePixelRange(0, 0, imageWidth, imageHeight) : pixelRange
            };
        }
    }
}
