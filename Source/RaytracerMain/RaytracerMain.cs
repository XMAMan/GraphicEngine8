using System;
using System.Collections.Generic;
using GraphicMinimal;
using GraphicGlobal;
using System.Drawing;
using RaytracingColorEstimator;
using System.Windows.Forms;

namespace RaytracerMain
{
    //Zeichenpanel, was ein Raytracingbild erzeugt und während des Renderns anzeigt
    public class RaytracerMain : IDrawingPanel, IDrawingAsynchron, IRaytracingHelper
    {
        private readonly DrawingAsynchronHelper asynchronHelper;   //Hiermit wird das IDrawingAsynchron-Interface implementiert
        private readonly RaytracingHelper raytracingHelper = null; //Hiermit wird das IRaytracingHelper-Interface implementiert
        public Control DrawingControl { get; private set; }        //IDrawingPanel

        public RaytracerMain(IPixelEstimator pixelEstimator)
        {
            this.asynchronHelper = new DrawingAsynchronHelper(pixelEstimator, UpdateProgressImage);
            this.raytracingHelper = new RaytracingHelper(pixelEstimator);

            this.DrawingControl = new PanelWithoutFlickers() { Dock = DockStyle.Fill, BackgroundImageLayout = ImageLayout.Stretch };
            this.DrawingControl.Paint += (sender, obj) =>
            {
                if (this.DrawingControl.BackgroundImage != null) obj.Graphics.DrawImage(this.DrawingControl.BackgroundImage, new Rectangle(0, 0, this.DrawingControl.Width, this.DrawingControl.Height));
            };
        }

        #region IDrawingAsynchron
        public string ProgressText { get => this.asynchronHelper.ProgressText; }
        public float ProgressPercent { get => this.asynchronHelper.ProgressPercent; }
        public bool IsRaytracingNow { get => this.asynchronHelper.IsRaytracingNow; }        

        public void UpdateProgressImage(float brightnessFactor, TonemappingMethod tonemapping)
        {
            var image = this.asynchronHelper.GetProgressImage(brightnessFactor, tonemapping);
            if (image != null)
            {
                this.DrawingControl.BackgroundImage = image;
                this.DrawingControl.Invalidate();
            }            
        }

        public void StartRaytracing(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, Action<RaytracerResultImage> renderingFinish, Action<Exception> exceptionOccured)
        {
            this.asynchronHelper.StartRaytracing(data, imageWidth, imageHeight, pixelRange, renderingFinish, exceptionOccured);
        }

        public void StartImageAnalyser(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, string outputFolder, Action<RaytracerResultImage> renderingFinish, Action<Exception> exceptionOccured)
        {
            this.asynchronHelper.StartImageAnalyser(data, imageWidth, imageHeight, pixelRange, outputFolder, renderingFinish, exceptionOccured);
        }

        public void StopRaytracing()
        {
            this.asynchronHelper.StopRaytracing();
        }

        public RaytracerResultImage GetRaytracingImageSynchron(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange)
        {
            return this.asynchronHelper.GetRaytracingImageSynchron(data, imageWidth, imageHeight, pixelRange);
        }
        
        public void SaveCurrentRaytracingDataToFolder()
        {
            this.asynchronHelper.SaveCurrentRaytracingDataToFolder();
        }
        #endregion
        //....................................................................................

        #region IRaytracingHelper
        //Für synchrone Verfahren die ohne Stoptrigger arbeiten (einzelnes Pixel analysieren; Helligkeitswert bestimmen)
        public Vector3D GetColorFromSinglePixelForDebuggingPurpose(Frame3DData data, RaytracingDebuggingData debuggingData)
        {
            return this.raytracingHelper.GetColorFromSinglePixelForDebuggingPurpose(data, debuggingData);
        }        
        public Vector3D GetColorFromSinglePixel(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            return this.raytracingHelper.GetColorFromSinglePixel(data, imageWidth, imageHeight, pixelRange, pixX, pixY, sampleCount);
        }
        public List<Vector3D> GetNPixelSamples(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            return this.raytracingHelper.GetNPixelSamples(data, imageWidth, imageHeight, pixelRange, pixX, pixY, sampleCount);
        }
        public string GetFullPathsFromSinglePixel(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            return this.raytracingHelper.GetFullPathsFromSinglePixel(data, imageWidth, imageHeight, pixelRange, pixX, pixY, sampleCount);
        }
        public string GetPathContributionsForSinglePixel(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            return this.raytracingHelper.GetPathContributionsForSinglePixel(data, imageWidth, imageHeight, pixelRange, pixX, pixY, sampleCount);
        }
        public float GetBrightnessFactor(Frame3DData data, int imageWidth, int imageHeight)
        {
            return this.raytracingHelper.GetBrightnessFactor(data, imageWidth, imageHeight);
        }

        public string GetFlippedWavefrontFile(Frame3DData data, int imageWidth, int imageHeight)
        {
            return this.raytracingHelper.GetFlippedWavefrontFile(data, imageWidth, imageHeight);
        }        
        #endregion
    }
}
