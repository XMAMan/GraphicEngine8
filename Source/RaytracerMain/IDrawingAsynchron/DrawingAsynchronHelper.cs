using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;
using ImageCreator;
using RaytracingColorEstimator;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace RaytracerMain
{
    //Hilft bei der Implementierung des IDrawingAsynchron-Interfaces
    class DrawingAsynchronHelper
    {
        //Hier werden keine Scenendaten gespeichert sondern nur die Klassen, welche ein Frame3DData-Objekt mit Hilfe von ein
        //IPixelEstimator-Objekt in ein Bild umwandeln
        public delegate void UpdateProgressImage(float brightnessFactor, TonemappingMethod tonemapping);
        private UpdateProgressImage updateProgressImage;

        private readonly IPixelEstimator pixelEstimator = null;

        private IMasterImageCreator imageCreator = null; //Um während des Renderns ein Fortschrittsbild oder Stopsignal triggern zu können ist eine Klassenvariable nötig
        private CancellationTokenSource stopTrigger = null;//Stoppen der ColorEstimatorerstellung,Der PhotonMaperstellung wärend eines FramePrePareSchritts, Der Pixel-Loop


        public string ProgressText { get; private set; }
        public float ProgressPercent { get; private set; }
        public bool IsRaytracingNow { get; private set; }

        public DrawingAsynchronHelper(IPixelEstimator pixelEstimator, UpdateProgressImage updateProgressImage)
        {
            this.ProgressText = "";
            this.ProgressPercent = 0;
            this.IsRaytracingNow = false;
            this.pixelEstimator = pixelEstimator;
            this.updateProgressImage = updateProgressImage;
        }

        public void StartRaytracing(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, Action<RaytracerResultImage> renderingFinish, Action<Exception> exceptionOccured)
        {
            if (this.IsRaytracingNow) return;
            var frameData = CreateRaytracingDataWithStopTrigger(data, imageWidth, imageHeight, pixelRange);

            GetResultImageAsynchron(() => { return GetImageSynchron(frameData); }, renderingFinish, exceptionOccured);
        } 

        public void StartImageAnalyser(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange, string outputFolder, Action<RaytracerResultImage> renderingFinish, Action<Exception> exceptionOccured)
        {
            if (this.IsRaytracingNow) return;
            var frameData = CreateRaytracingDataWithStopTrigger(data, imageWidth, imageHeight, pixelRange);

            GetResultImageAsynchron(() => { return GetResultFromImageAnalyser(frameData, outputFolder); }, renderingFinish, exceptionOccured);
        }

        public void StopRaytracing()
        {
            if (this.imageCreator != null)
            {
                this.imageCreator.StopRaytracing(); //Stoppe die Get-Pixel-Loop
            }

            if (this.stopTrigger != null)
            {
                this.stopTrigger.Cancel(); //Stoppe den ColorEstimator-Erstellungsprozess/Pixel-Loop
            }
        }

        public RaytracerResultImage GetRaytracingImageSynchron(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange)
        {
            if (this.IsRaytracingNow) return null;
            var frameData = CreateRaytracingDataWithStopTrigger(data, imageWidth, imageHeight, pixelRange);

            return GetImageSynchron(frameData);
        }

        public void SaveCurrentRaytracingDataToFolder()  //Damit man wärend des Raytracens eine Sicherung der Daten manuell anlegen kann
        {
            if (this.imageCreator == null) throw new Exception("Save nicht möglich. Es läuft momnetan kein Raytracing");
            this.imageCreator.SaveToFolder();
        }
        public Bitmap GetProgressImage(float brightnessFactor, TonemappingMethod tonemapping)
        {
            IMasterImageCreator flatCopy = this.imageCreator;
            if (flatCopy == null || (this.stopTrigger != null && this.stopTrigger.IsCancellationRequested)) return null;//throw new Exception("Update nicht möglich. Es läuft momnetan kein Raytracing");
            var progressImage = flatCopy.GetProgressImage();
            if (progressImage == null) return null;

            return TransformRawImageToBitmap(progressImage, brightnessFactor, tonemapping);
        }

        

        

        private void ProgressChangedHandler(string progressText, float progressPercent)
        {
            this.ProgressText = progressText;
            this.ProgressPercent = progressPercent;
        }

        private Bitmap TransformRawImageToBitmap(ImageBuffer rawImage, float brightnessFactor, TonemappingMethod tonemapping)
        {
            return Tonemapping.GetImage(rawImage.GetColorScaledImage(brightnessFactor), tonemapping);
        }

        private RaytracerResultImage GetResultFromImageAnalyser(RaytracingFrame3DData frameData, string outputFolder)
        {
            ProgressChangedHandler("Start", 0);
            DateTime startTime = DateTime.Now;

            var colorEstimator = this.pixelEstimator.CreateColorEstimator(frameData);

            IFrameEstimator frameEstimator;
            if (colorEstimator.CreatesLigthPaths == false)
                frameEstimator = new PixelToFrameTranslator(colorEstimator);
            else
            {
                frameEstimator = colorEstimator as IFrameEstimator;
                if (frameEstimator == null) frameEstimator = new PixelToFrameTranslator(colorEstimator);
            }
            this.imageCreator = new ImageFullPathAnalyser(frameEstimator, frameData, outputFolder);

            var rawImage = imageCreator.GetImage(frameData.PixelRange);
            string renderTime = ((int)(DateTime.Now - startTime).TotalSeconds) + " seconds";
            this.updateProgressImage(frameData.GlobalObjektPropertys.BrightnessFactor, frameData.GlobalObjektPropertys.Tonemapping);
            this.imageCreator = null;
            this.IsRaytracingNow = false;
            ProgressChangedHandler("Fertig", 100);
            Bitmap colorImage = TransformRawImageToBitmap(rawImage, frameData.GlobalObjektPropertys.BrightnessFactor, frameData.GlobalObjektPropertys.Tonemapping);
            return new RaytracerResultImage(colorImage, renderTime, rawImage);
        }

        private RaytracerResultImage GetImageSynchron(RaytracingFrame3DData frameData)
        {
            ProgressChangedHandler("Start", 0);
            DateTime startTime = DateTime.Now;
            this.imageCreator = this.pixelEstimator.CreateImageCreator(frameData);
            var rawImage = imageCreator.GetImage(frameData.PixelRange);

            string renderTime = ((int)(DateTime.Now - startTime).TotalSeconds) + " seconds";
            this.updateProgressImage(frameData.GlobalObjektPropertys.BrightnessFactor, frameData.GlobalObjektPropertys.Tonemapping);
            this.imageCreator = null;
            this.IsRaytracingNow = false;
            ProgressChangedHandler("Fertig", 100);
            return new RaytracerResultImage(TransformRawImageToBitmap(rawImage, frameData.GlobalObjektPropertys.BrightnessFactor, frameData.GlobalObjektPropertys.Tonemapping), renderTime, rawImage);
        }

        //Erzeugt ein RaytracerResultImage und gibt es über die renderingFinish zurück
        private void GetResultImageAsynchron(Func<RaytracerResultImage> imageCreationAction, Action<RaytracerResultImage> renderingFinish, Action<Exception> exceptionOccured)
        {
            RaytracerResultImage result = null;
            var context = TaskScheduler.FromCurrentSynchronizationContext();
            Task task = new Task(() =>
            {
                result = imageCreationAction();
            });

            task.ContinueWith(t =>
            {
                this.IsRaytracingNow = false;
                this.ProgressText = "";
                this.ProgressPercent = 0;
                this.imageCreator = null;

                if (t.Exception == null)
                {
                    renderingFinish(result);
                }
                else
                {
                    exceptionOccured(t.Exception.InnerException);
                }
            }, context);

            task.Start();
        }

        private RaytracingFrame3DData CreateRaytracingDataWithStopTrigger(Frame3DData data, int imageWidth, int imageHeight, ImagePixelRange pixelRange)
        {
            this.IsRaytracingNow = true;

            this.stopTrigger = new CancellationTokenSource();

            return new RaytracingFrame3DData(data)
            {
                ScreenWidth = imageWidth,
                ScreenHeight = imageHeight,
                ProgressChanged = ProgressChangedHandler,
                StopTrigger = this.stopTrigger,
                PixelRange = pixelRange
            };
        }
    }
}
