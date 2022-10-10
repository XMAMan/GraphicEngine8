using GraphicMinimal;
using RaytracingColorEstimator;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace ImageCreator
{
    //ImageCreator, der Frame oder Pixelmäßig arbeitet und manuelles und automatischen Speichern anbietet.
    //Außerdem wird die Renderzeit über die Sampleanzahl oder MaxRendertime bestimmt
    public class MasterImageCreator : IMasterImageCreator
    {
        private readonly RaytracingFrame3DData data;
        private ImageCreatorWithSave imageCreator;
        private int autoSaveTimeInSeconds; //Aller so viele Sekunden wird AutoSave durchgeführt
        private DateTime startRenderTime;

        public RaytracerWorkingState State { get { return this.imageCreator.State; } }

        private MasterImageCreator(RaytracingFrame3DData data)
        {
            if (data.GlobalObjektPropertys.AutoSaveMode != RaytracerAutoSaveMode.Disabled && Directory.Exists(data.GlobalObjektPropertys.SaveFolder) == false) throw new DirectoryNotFoundException(data.GlobalObjektPropertys.SaveFolder);
            this.data = data;
        }

        public static MasterImageCreator CreateInPixelMode(IPixelEstimator pixelEstimator, RaytracingFrame3DData data)
        {
            MasterImageCreator creator = new MasterImageCreator(data);

            var saveAreaSize = new Size(100, 100);
            creator.autoSaveTimeInSeconds = 10; //10 Sekunden
            if (data.GlobalObjektPropertys.AutoSaveMode == RaytracerAutoSaveMode.FullScreen)
            {
                creator.autoSaveTimeInSeconds = 3600; //Einmal pro Stunde
            }
            creator.imageCreator = new ImageCreatorWithSave(new ImageCreatorPixel(pixelEstimator, data), data.GlobalObjektPropertys.SaveFolder, saveAreaSize, data.GlobalObjektPropertys.AutoSaveMode, data.StopTrigger);
            
            return creator;
        }

        public static MasterImageCreator CreateInFrameMode(IFrameEstimator frameEstimator, RaytracingFrame3DData data)
        {
            MasterImageCreator creator = new MasterImageCreator(data);

            var saveAreaSize = new Size(400, 400);
            creator.autoSaveTimeInSeconds = 100; //100 Sekunden
            if (data.GlobalObjektPropertys.AutoSaveMode == RaytracerAutoSaveMode.FullScreen)
            {
                creator.autoSaveTimeInSeconds = 3600; //Einmal pro Stunde
            }
            creator.imageCreator = new ImageCreatorWithSave(new ImageCreatorFrame(frameEstimator, data), data.GlobalObjektPropertys.SaveFolder, saveAreaSize, data.GlobalObjektPropertys.AutoSaveMode, data.StopTrigger);

            return creator;
        }

        public ImageBuffer GetImage(ImagePixelRange range)
        {
            this.startRenderTime = DateTime.Now;
            bool isFinish = false;

            Task autoSaveThread = Task.Run(() =>
            {
                Task[] tasks = new Task[] {  };
                DateTime lastAutoSave = DateTime.Now;
                while (true)
                {
                    try
                    {
                        Task.WaitAny(tasks, 1000, this.data.StopTrigger.Token); //Warte 1 Sekunde oder auf das Stopsignal
                    }
                    catch (OperationCanceledException) { } //Stopptrigger wurde benutzt
                    if (this.data.StopTrigger.IsCancellationRequested || isFinish) break;

                    if (this.data.GlobalObjektPropertys.AutoSaveMode != RaytracerAutoSaveMode.Disabled && (DateTime.Now - lastAutoSave).TotalSeconds > this.autoSaveTimeInSeconds)
                    {
                        this.imageCreator.SaveToFolder();
                        lastAutoSave = DateTime.Now;
                    }

                    //Abbruch bei erreichen der MaxRenderTimeInSeconds
                    if (this.data.GlobalObjektPropertys.MaxRenderTimeInSeconds != int.MaxValue && (DateTime.Now - this.startRenderTime).TotalSeconds < this.data.GlobalObjektPropertys.MaxRenderTimeInSeconds)
                    {
                        StopRaytracing();
                    }

                    UpdateProgressText();
                }
            });
            var image = this.imageCreator.GetImage(range);
            isFinish = true;
            autoSaveThread.Wait();
            this.data.ProgressChanged("Fertig", 100);
            return image;
        }

        

        private void UpdateProgressText()
        {
            if (this.data.GlobalObjektPropertys.MaxRenderTimeInSeconds == int.MaxValue)
            {
                float progress = this.Progress * 100;
                this.data.ProgressChanged(this.ProgressText, this.Progress * 100);
            }else
            {
                int renderTimeInSecondsSoFar = (int)(DateTime.Now - this.startRenderTime).TotalSeconds;
                this.data.ProgressChanged(this.ProgressText, renderTimeInSecondsSoFar * 100.0f / this.data.GlobalObjektPropertys.MaxRenderTimeInSeconds);
            }                
        }

        public float Progress
        {
            get { return this.imageCreator.Progress; }
        }

        public string ProgressText
        {
            get
            {
                return this.imageCreator.ProgressText;
            }
        }

        public ImageBuffer GetProgressImage()
        {
            return this.imageCreator.GetProgressImage();
        }

        public void SaveToFolder()
        {
            this.imageCreator.SaveToFolder();
        }

        public void StopRaytracing()
        {
            this.data.StopTrigger.Cancel();
        }

        public Vector3D GetColorFromSinglePixelForDebuggingPurpose(RaytracingDebuggingData debuggingData)
        {
            return this.imageCreator.GetColorFromSinglePixelForDebuggingPurpose(debuggingData);
        }
    }
}
