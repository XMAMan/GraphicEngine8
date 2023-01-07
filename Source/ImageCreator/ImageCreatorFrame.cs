using System;
using System.Linq;
using GraphicMinimal;
using System.Drawing;
using System.Threading.Tasks;
using GraphicGlobal;
using RaytracingColorEstimator;
using FullPathGenerator;

namespace ImageCreator
{
    public class ImageCreatorFrame : IImageCreatorFrame
    {
        //Dieses Objekt gibt es für jeden Task einmal und es wird einmal initial am Anfang angelegt und dann für jeden Frame neu verwendet
        class TaskData
        {
            public IFrameEstimator FrameEstimator;
            public IRandom Rand;
            public ImageBuffer Frame;
            public int FrameIterationNumber; //Diese Zahl wird bei DoFramePrepareStep übergeben
            public long PixelCount; //So viele Pixel hat der Task bereits geschaft            
        }

        private readonly IFrameEstimator masterEstimator;
        private readonly RaytracingFrame3DData data;
        private readonly BackgroundColor backgroundColor;
        private ImageBufferSum bufferSum;
        private CachedProgressImage cachedProgressImage;
        private readonly TaskData[] taskDatas;
        private ImagePixelRange imageRange = null; //Dieser Parameter wurde bei GetImage übergeben
        private bool stopTriggerIsSet = false;
        private long intialPixelCount = 0;

        public ImageCreatorFrame(IFrameEstimator frameEstimator, RaytracingFrame3DData data)
        {
            this.masterEstimator = frameEstimator;
            this.data = data;
            this.backgroundColor = new BackgroundColor(data.GlobalObjektPropertys.BackgroundImage, data.GlobalObjektPropertys.BackgroundColorFactor, data.ScreenWidth, data.ScreenHeight);
            this.taskDatas = new TaskData[this.data.GlobalObjektPropertys.ThreadCount];
        }

        public ImageBufferSum GetImageBufferSum()
        {
            //return this.bufferSum;
            return this.bufferSum?.GetCopy();
        }

        public ImageBuffer GetImage(ImagePixelRange range)
        {
            return GetImageFromInitialData(new ImageBufferSum(range.Width, range.Height, new Vector3D(0, 0, 0)), range);
        }

        //range Bereich innerhalb von RaytracingFrame3DData.PixelRange (Immer im Bezug auf die Koordinaten (0,0))
        public ImageBuffer GetImageFromInitialData(ImageBufferSum initialData, ImagePixelRange range)
        {
            this.bufferSum = initialData;
            this.cachedProgressImage = new CachedProgressImage(this.bufferSum);
            this.imageRange = range;
            this.stopTriggerIsSet = false;
            this.intialPixelCount = (long)initialData.FrameCount * (this.imageRange.Width * this.imageRange.Height);

            for (int i = 0; i < this.taskDatas.Length; i++)
            {
                this.taskDatas[i] = new TaskData()
                {
                    FrameEstimator = this.masterEstimator.CreateCopy(),
                    Rand = new Rand(initialData.FrameCount * this.data.GlobalObjektPropertys.ThreadCount + i),
                    FrameIterationNumber = initialData.FrameCount,
                    PixelCount = 0
                };
            }

            Task<TaskData>[] renderTasks = new Task<TaskData>[Math.Min(this.taskDatas.Length, this.data.GlobalObjektPropertys.SamplingCount)];

            int startedFrames = 0;

            for (int i = 0; i < renderTasks.Length; i++)
            {
                startedFrames++;
                renderTasks[i] = CreateAndStartTask(this.taskDatas[i]);
            }

            while (true)
            {
                try
                {
                    if (this.bufferSum.FrameCount >= this.data.GlobalObjektPropertys.SamplingCount) break;

                    int index = Task.WaitAny(renderTasks, this.data.StopTrigger.Token);

                    if (renderTasks.Any(x => x != null && x.IsFaulted))
                    {
                        throw new Exception(renderTasks.First(x => x != null && x.IsFaulted).Exception.ToString());
                    }

                    if (index != -1) //Ein Task ist angeblich fertig
                    {
                        do
                        {
                            var task = renderTasks.FirstOrDefault(x => x != null && x.IsCompleted); //Schaue welcher als nächstes fertig ist
                            if (task != null)
                            {
                                index = renderTasks.ToList().IndexOf(task);

                                this.bufferSum.AddFrame(this.taskDatas[index].Frame);

                                if (startedFrames < this.data.GlobalObjektPropertys.SamplingCount)
                                {
                                    this.taskDatas[index].FrameIterationNumber++;
                                    startedFrames++;
                                    renderTasks[index] = CreateAndStartTask(this.taskDatas[index]);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break; //Keine weiteren fertigen Threads gefunden. Dann lege dich wieder schlafen
                            }
                        } while (true);
                    }
                }
                catch (OperationCanceledException)
                {
                    this.stopTriggerIsSet = true;
                    break; //Stopptrigger wurde benutzt
                }
            }
            Task.WaitAll(renderTasks);

            return this.bufferSum.GetScaledImage(); //Stopptrigger wurde benutzt oder Bild ist fertig
        }
        private Task<TaskData> CreateAndStartTask(TaskData input)
        {
            return Task<TaskData>.Factory.StartNew((object obj) =>
            {
                TaskData taskData = obj as TaskData;

                string randomObjectBase64CodedForFramePrepare = DoFramePrepareStep(taskData);
                taskData.Frame = DoFramePixelLoopStep(taskData, randomObjectBase64CodedForFramePrepare);
                taskData.Frame = taskData.FrameEstimator.DoFramePostprocessing(taskData.FrameIterationNumber, taskData.Frame);

                return taskData;
            }, input);
        }

        private string DoFramePrepareStep(TaskData taskData)
        {
            string randomObjectBase64Coded = taskData.Rand.ToBase64String();
            try
            {
                taskData.FrameEstimator.DoFramePrepareStep(taskData.FrameIterationNumber, taskData.Rand);
            }
            catch (RandomException createLightPahtsException)
            {
                var debugData = new RaytracingDebuggingData(new RaytracingDebuggingData.DoFramePrepareStepParameter()
                {                    
                    FrameIterationNumber = taskData.FrameIterationNumber,
                    RandomObjectBase64Coded = createLightPahtsException.RandomObjectBase64Coded
                },
                new Size(this.data.ScreenWidth, this.data.ScreenHeight),
                this.imageRange,
                this.data.GlobalObjektPropertys);
                throw new Exception(debugData.ToXmlString(), createLightPahtsException);
            }
            catch (Exception ex)
            {
                var debugData = new RaytracingDebuggingData(new RaytracingDebuggingData.DoFramePrepareStepParameter()
                {
                    FrameIterationNumber = taskData.FrameIterationNumber,
                    RandomObjectBase64Coded = randomObjectBase64Coded
                },
                new Size(this.data.ScreenWidth, this.data.ScreenHeight),
                this.imageRange,
                this.data.GlobalObjektPropertys);
                throw new Exception(debugData.ToXmlString(), ex);
            }

            return randomObjectBase64Coded;
        }

        private ImageBuffer DoFramePixelLoopStep(TaskData taskData, string randomObjectBase64CodedForFramePrepare)
        {
            var image = new ImageBuffer(this.imageRange.Width, this.imageRange.Height, new Vector3D(0, 0, 0));

            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    //Achtung: Wenn jemand bei ein CancellationTokenSource die Cancel-Methode ruft, führt das dazu das
                    //zuerst IsCancellationRequested == true wird und dann danach erst wirft 
                    //Task.WaitAny(renderTasks, this.data.StopTrigger.Token) eine OperationCanceledException
                    //Dadurch wird hier dann beim Stop ein halbfertiges Bild zurück gegeben was weiter oben dann
                    //auf die ImagerBufferSum drauf addiert wird (im Glaube daran das Bild sei 100% fertig).
                    //Beim Laden vom ImageBufferSum-Objekt von der Platte ist der FrameCounter dann 1 und die Daten enthalten
                    //aber nur ein halbfertiges Bild was auch nicht weiter gerendert wird.
                    //if (this.data.StopTrigger.IsCancellationRequested) return image;
                    if (this.stopTriggerIsSet) return image;
                    string randomObjectBase64Coded = taskData.Rand.ToBase64String();
                    try
                    {
                        FullPathSampleResult sampleResult = taskData.FrameEstimator.GetFullPathSampleResult(this.imageRange.XStart + x, this.imageRange.YStart + y, taskData.Rand);
                        if (sampleResult.MainPixelHitsBackground)
                        {
                            image[x, y] += backgroundColor.GetColor(this.imageRange.XStart + x, this.imageRange.YStart + y);
                        }
                        else
                        {
                            image[x, y] += sampleResult.RadianceFromRequestetPixel;
                        }

                        foreach (var lightPath in sampleResult.LighttracingPaths)
                        {
                            int rx = (int)Math.Floor(lightPath.PixelPosition.X - this.imageRange.XStart);
                            int ry = (int)Math.Floor(lightPath.PixelPosition.Y - this.imageRange.YStart);
                            if (rx >= 0 && rx < image.Width && ry >= 0 && ry < image.Height)
                            {
                                image[rx, ry] += lightPath.Radiance;
                            }
                        }

                        taskData.PixelCount++;
                    }
                    catch (Exception ex)
                    {
                        var debugData = new RaytracingDebuggingData(
                            new RaytracingDebuggingData.DoFramePrepareStepParameter()
                            {
                                FrameIterationNumber = taskData.FrameIterationNumber,
                                RandomObjectBase64Coded = randomObjectBase64CodedForFramePrepare
                            },
                            new RaytracingDebuggingData.GetFullPathSampleResultParameter()
                            {
                                PixX = this.imageRange.XStart + x,
                                PixY = this.imageRange.YStart + y,
                                RandomObjectBase64Coded = randomObjectBase64Coded
                            },
                            new Size(this.data.ScreenWidth, this.data.ScreenHeight),
                            this.imageRange,
                            this.data.GlobalObjektPropertys);
                        throw new Exception(debugData.ToXmlString(), ex);
                    }
                }

            return image;
        }

        public float Progress
        {
            get
            {
                if (this.taskDatas.Last() == null) return 0;

                return (float)((this.intialPixelCount + this.taskDatas.Sum(x => x.PixelCount)) / (double)((long)this.imageRange.Width * this.imageRange.Height * this.data.GlobalObjektPropertys.SamplingCount));
            }
        }

        public string ProgressText
        {
            get
            {
                if (this.bufferSum == null) return "Frame: 0";

                return "Frame: " + this.bufferSum.FrameCount; ;
            }
        }

        public ImageBuffer GetProgressImage()
        {
            if (this.cachedProgressImage == null) return null;

            return this.cachedProgressImage.GetProgressImage();
            //return this.bufferSum.GetScaledImage();
        }

        public Vector3D GetColorFromSinglePixelForDebuggingPurpose(RaytracingDebuggingData debuggingData)
        {
            IRandom randPrepare = new Rand(debuggingData.FramePrepareData.RandomObjectBase64Coded);
            IRandom randPixel = debuggingData.PixelData == null ? null : new Rand(debuggingData.PixelData.RandomObjectBase64Coded);

            var frameEstimator = this.masterEstimator.CreateCopy();
            frameEstimator.DoFramePrepareStep(debuggingData.FramePrepareData.FrameIterationNumber, randPrepare); //Wenn die Exception im PrepareStep passierte
            if (randPixel != null)
            {
                return frameEstimator.GetFullPathSampleResult(debuggingData.PixelData.PixX, debuggingData.PixelData.PixY, randPixel).RadianceFromRequestetPixel; //Wenn die Exception im PixelLoop-Step passierte
            }

            return null; //Es gibt keine Pixelfarbe, da der Fehler im FramePrepareStep passierte
        }
    }
}
