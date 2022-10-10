using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using System.Drawing;
using System.Threading.Tasks;
using GraphicGlobal;
using RaytracingColorEstimator;

namespace ImageCreator
{
    public class ImageCreatorPixel : IImageCreatorPixel
    {
        private readonly IPixelEstimator pixelEstimator;
        private readonly RaytracingFrame3DData data;
        private readonly BackgroundColor backgroundColor;
        private ImageBuffer renderBuffer;
        private readonly TaskData[] taskDatas;
        private ImagePixelRange imageRange = null; //Dieser Parameter wurde bei GetImage übergeben

        public ImageCreatorPixel(IPixelEstimator pixelEstimator, RaytracingFrame3DData data)
        {
            this.pixelEstimator = pixelEstimator;
            this.data = data;
            this.backgroundColor = new BackgroundColor(data.GlobalObjektPropertys.BackgroundImage, data.GlobalObjektPropertys.BackgroundColorFactor, data.ScreenWidth, data.ScreenHeight);
            this.taskDatas = new TaskData[this.data.GlobalObjektPropertys.ThreadCount];
        }

        public ImageBuffer GetImage(ImagePixelRange range)
        {
            return GetImageFromInitialData(new ImageBuffer(range.Width, range.Height), range);
        }

        //range Bereich innerhalb von RaytracingFrame3DData.PixelRange (Immer im Bezug auf die Koordinaten (0,0))
        public ImageBuffer GetImageFromInitialData(ImageBuffer initialData, ImagePixelRange range)
        {
            this.renderBuffer = initialData;
            this.imageRange = range;

            List<ImagePixelRange> rangesToRender = range.GetRandomPixelRangesInsideFromHere(new Size(20, 20), new Random(0));

            for (int i = 0; i < this.taskDatas.Length; i++)
            {
                this.taskDatas[i] = new TaskData() { Rand = new Rand(i), PixelCount = 0 };
            }

            //Lege nicht mehr RenderTasks an als wie es rangesToRender-Einträge gibt
            Task<TaskData>[] renderTasks = new Task<TaskData>[Math.Min(this.taskDatas.Length, rangesToRender.Count)];
            for (int i=0;i< renderTasks.Length; i++)
            {
                this.taskDatas[i].Range = rangesToRender[0];
                rangesToRender.RemoveAt(0);
                renderTasks[i] = CreateAndStartTask(this.taskDatas[i]);
            }

            while (true)
            {
                try
                {
                    if (rangesToRender.Any() == false) break;

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

                                if (rangesToRender.Any())
                                {
                                    this.taskDatas[index].Range = rangesToRender[0];
                                    rangesToRender.RemoveAt(0);
                                    renderTasks[index] = CreateAndStartTask(this.taskDatas[index]);
                                }else
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
                    break; //Stopptrigger wurde benutzt
                }
            }
            Task.WaitAll(renderTasks);

            return this.renderBuffer; //Stopptrigger wurde benutzt
        }

        private Task<TaskData> CreateAndStartTask(TaskData input)
        {
            return Task<TaskData>.Factory.StartNew((object obj) =>
            {
                TaskData d = obj as TaskData;

                for (int y = d.Range.Top; y < d.Range.Bottom; y++)
                    for (int x=d.Range.Left;x<d.Range.Right;x++)
                    {
                        if (this.renderBuffer[x - this.imageRange.XStart, y - this.imageRange.YStart] == null)
                        {
                            this.renderBuffer[x - this.imageRange.XStart, y - this.imageRange.YStart] = GetColorFromOnePixel(d.Rand, x, y);
                            if (this.data.StopTrigger.IsCancellationRequested) return d;
                        }                        
                        d.PixelCount++;
                    }

                return d;
            }, input);
        }

        private Vector3D GetColorFromOnePixel(IRandom rand, int x, int y)
        {
            Vector3D pixSum = new Vector3D(0, 0, 0);

            for (int i = 0; i < this.data.GlobalObjektPropertys.SamplingCount; i++)
            {
                if (this.data.StopTrigger.IsCancellationRequested)
                {
                    return null;
                }

                string randomObjectBase64Coded = rand.ToBase64String();
                try
                {
                    var result = this.pixelEstimator.GetFullPathSampleResult(x, y, rand);

                    if (result.MainPixelHitsBackground)
                    {
                        pixSum += this.backgroundColor.GetColor(x, y);
                    }
                    else
                    {
                        pixSum += result.RadianceFromRequestetPixel;
                    }
                }
                catch (Exception ex)
                {
                    var debugData = new RaytracingDebuggingData(new RaytracingDebuggingData.GetFullPathSampleResultParameter()
                    {
                        PixX = x,
                        PixY = y,
                        RandomObjectBase64Coded = randomObjectBase64Coded
                    },
                    new Size(this.data.ScreenWidth, this.data.ScreenHeight),
                    this.data.GlobalObjektPropertys);
                    throw new Exception(debugData.ToXmlString(), ex);
                }
            }

            return pixSum / this.data.GlobalObjektPropertys.SamplingCount;
        }

        class TaskData
        {
            public ImagePixelRange Range;
            public IRandom Rand;
            public int PixelCount; //So viele Pixel hat der Task bereits geschaft
        }

        public float Progress 
        { 
            get
            {
                if (this.taskDatas.Last() == null) return 0;

                return this.taskDatas.Sum(x => x.PixelCount) / (float)(this.imageRange.Width * this.imageRange.Height);
            }
        }

        public string ProgressText 
        {
            get
            {
                return "Render";
            }
        }

        public ImageBuffer GetProgressImage()
        {
            return this.renderBuffer;
        }

        //Wenn ein Fehler aufgetreten ist, dann kann man einfach den Text aus der Exception nehmen und mit GetColorFromSinglePixelForDebuggingPurpose dann den Fehler nachstellen
        public Vector3D GetColorFromSinglePixelForDebuggingPurpose(RaytracingDebuggingData debuggingData)
        {
            IRandom rand = new Rand(debuggingData.PixelData.RandomObjectBase64Coded);
            return this.pixelEstimator.GetFullPathSampleResult(debuggingData.PixelData.PixX, debuggingData.PixelData.PixY, rand).RadianceFromRequestetPixel;
        }
    }
}
