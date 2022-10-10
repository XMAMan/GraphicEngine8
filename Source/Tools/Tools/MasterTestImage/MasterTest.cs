using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GraphicPanels;
using GraphicMinimal;
using System.Threading;
using System.Threading.Tasks;
using BitmapHelper;
using Tools.Tools.SceneEditor;

namespace Tools
{
    internal class MasterTest
    {
        private int width;
        private int height;
        private Action<Exception> exceptionHandler;
        private Action<Bitmap> resultHandler;
        private TaskScheduler uiContext = null;
        private GraphicPanel3D currentPanel = null;
        private string progressPrefix = "";
        private bool finish = false;
        

        public string ProgressText
        {
            get
            {
                if (this.finish) return progressPrefix;

                if (this.currentPanel != null)
                {
                    return this.progressPrefix + " " + this.currentPanel.ProgressText + ": " + String.Format("{0:F3}", this.currentPanel.ProgressPercent) + "%";
                }
                return "";
            }
        }

        public MasterTest(int width, int height, Action<Bitmap> resultHandler, Action<Exception> exceptionHandler)
        {
            this.width = width;
            this.height = height;
            this.exceptionHandler = exceptionHandler;
            this.resultHandler = resultHandler;
        }

        public void GetResultAsynchron()
        {
            DateTime startTime = DateTime.Now;
            Bitmap result = null;
            this.uiContext = TaskScheduler.FromCurrentSynchronizationContext();
            Task task = new Task(() =>
            {
                result = GetResultSynchron();
            });

            task.ContinueWith(t =>
            {
                if (t.Exception == null)
                {
                    resultHandler(result);
                }
                else
                {
                    exceptionHandler(t.Exception.InnerException);
                }
                this.finish = true;
                this.progressPrefix = "Fertig: " + TimeSpanToString(DateTime.Now - startTime);
            }, this.uiContext);

            task.Start();
        }

        private static string TimeSpanToString(TimeSpan diff)
        {
            List<KeyValuePair<int, string[]>> list = new List<KeyValuePair<int, string[]>>()
            {
                new KeyValuePair<int, string[]>(diff.Days, new string[]{"Day","Days"}),
                new KeyValuePair<int, string[]>(diff.Hours, new string[]{"Hour", "Hours"}),
                new KeyValuePair<int, string[]>(diff.Minutes, new string[]{"Minute", "Minutes"}),
                new KeyValuePair<int, string[]>(diff.Seconds, new string[]{"Second","Seconds"}),
            };

            try
            {
                int i1 = list.FindIndex(x => x.Key > 0);
                int i2 = list.FindIndex(i1 + 1, x => x.Key > 0);
                if (i1 == -1) return (int)diff.TotalSeconds + " Seconds";
                string s1 = list[i1].Key + " " + (list[i1].Key == 0 ? list[i1].Value[0] : list[i1].Value[1]);
                string s2 = i2 >= 0 ? (list[i2].Key + " " + (list[i2].Key == 0 ? list[i2].Value[0] : list[i2].Value[1])) : "";
                return s1 + ", " + s2;
            }
            catch
            {
                return (int)diff.TotalSeconds + " Seconds";
            }
        }

        private Bitmap GetResultSynchron()
        {
            List<List<Bitmap>> images = GetAllImages();

            Bitmap result = new Bitmap(images.Count * this.width, images[0].Count * this.height);
            Graphics grx = Graphics.FromImage(result);
            int px = 0, py = 0;
            for (int x = 0; x < images.Count; x++)
            {
                py = 0;
                for (int y = 0; y < images[x].Count; y++)
                {
                    grx.DrawImage(images[x][y], px, py);
                    py += images[x][y].Height;
                }
                px += images[x].Max(img => img.Width);
            }
            grx.Dispose();
            return result;
        }

        class InputData
        {
            public List<Mode3D> Modes = new List<Mode3D>()
            {
                //Mode3D.OpenGL_Version_3_0,
                //Mode3D.Direct3D_11,
                //Mode3D.CPU,
                Mode3D.PathTracer,
                Mode3D.FullBidirectionalPathTracing,
                Mode3D.Photonmapping,
                Mode3D.ProgressivePhotonmapping,
                Mode3D.VertexConnectionMerging,
                Mode3D.RadiositySolidAngle,
                Mode3D.RadiosityHemicube,
                Mode3D.MediaFullBidirectionalPathTracing
            };

            public int ImageStandardWidth = 420;
            public int ImageStandardHeight = 328;

            //Bei den PixelRange angeben wurde davon ausgegangen, dass das Bild 'ImageStandardWidth' breit und 'ImageStandardHeight' hoch ist
            public List<SceneData> NoMediaScenes = new List<SceneData>
            {
                new SceneData(){ AddSceneMethod = Scenes.AddTestszene1_RingSphere, Name = "RingSphere", Ranges = new List<ImagePixelRange>(){new ImagePixelRange(new Point(220,151), new Point(255,172)), new ImagePixelRange(new Point(188,111), new Point(222,128)), new ImagePixelRange(new Point(56,164), new Point(90,183)), new ImagePixelRange(new Point(272,233), new Point(313,254)) }},
                new SceneData(){ AddSceneMethod = Scenes.AddTestszene2_NoWindowRoom, Name = "NoWindowRoom", Ranges = new List<ImagePixelRange>(){new ImagePixelRange(new Point(272,157), new Point(318,205)), new ImagePixelRange(new Point(214,50), new Point(245,80)), new ImagePixelRange(new Point(158,125), new Point(195,152)), new ImagePixelRange(new Point(305,212), new Point(331,230)) }},
                new SceneData(){ AddSceneMethod = Scenes.AddTestszene5_Cornellbox, Name = "Cornellbox", Ranges = new List<ImagePixelRange>(){new ImagePixelRange(new Point(116,200), new Point(146,230)), new ImagePixelRange(new Point(202,133), new Point(228,157)), new ImagePixelRange(new Point(97,40), new Point(117,70)), new ImagePixelRange(new Point(297,113), new Point(326,131)) } }
            };

            //Bei den PixelRange angeben wurde davon ausgegangen, dass das Bild 'ImageStandardWidth' breit und 'ImageStandardHeight' hoch ist
            public List<SceneData> WithMediaScenes = new List<SceneData>
            {
                new SceneData(){ AddSceneMethod = Scenes.AddTestszene19_StillLife, Name = "StillLife", Ranges = new List<ImagePixelRange>(){}, ModeForScene = Mode3D.UPBP, ImageRatio = new Size(160, 70) },
                new SceneData(){ AddSceneMethod = Scenes.AddTestszene5_MirrorCornellbox, Name = "Mirrorbox", Ranges = new List<ImagePixelRange>(){}, ModeForScene = Mode3D.MediaFullBidirectionalPathTracing},
                new SceneData(){ AddSceneMethod = Scenes.TestSzene18_CloudsForTestImage, Name = "Clouds", Ranges = new List<ImagePixelRange>(){}, ModeForScene = Mode3D.ThinMediaMultipleScattering},
            };
        }

        private bool UseMedia(Mode3D mode)
        {
            return mode == Mode3D.MediaBidirectionalPathTracing ||
                   mode == Mode3D.MediaFullBidirectionalPathTracing ||
                   mode == Mode3D.UPBP ||
                   mode == Mode3D.ThinMediaMultipleScattering;
        }

        class SceneData
        {
            public Action<GraphicPanel3D> AddSceneMethod;
            public List<ImagePixelRange> Ranges;
            public string Name;
            public Mode3D ModeForScene = Mode3D.CPU; //Die Media-Szenen sollen jeweils mir ihren eigenen Modus gerendert werden
            public Size ImageRatio = new Size(420, 328); //Laut diesen Seitenverhältnis wird das Bild erzeugt 
        }

        private List<List<Bitmap>> GetAllImages()
        {
            DateTime startTime = DateTime.Now;
            InputData data = new InputData();
            int maxCount = Area == ImageArea.PartialAreasOnly ? data.Modes.Count * data.NoMediaScenes.SelectMany(x => x.Ranges).Count() : data.Modes.Count * data.NoMediaScenes.Count;
            int counter = 0;

            List<List<Bitmap>> resultList = new List<List<Bitmap>>();
            foreach (var mode in data.Modes)
            {
                startTime = DateTime.Now;
                List<Bitmap> images = new List<Bitmap>();

                Mode3D modeForRendering = mode;
                List<SceneData> scenes = null;
                if (UseMedia(mode)) scenes = data.WithMediaScenes; else scenes = data.NoMediaScenes;

                foreach (var scene in scenes)
                {
                    Size size = BitmapHelp.GetImageSizeWithSpecificWidthToHeightRatio(this.width, this.height, scene.ImageRatio.Width, scene.ImageRatio.Height);

                    GraphicPanel3D panel = new GraphicPanel3D() { Width = size.Width, Height = size.Height };
                    
                    panel.GlobalSettings.Tonemapping = TonemappingMethod.None;
                    scene.AddSceneMethod(panel);

                    if (UseMedia(mode)) modeForRendering = scene.ModeForScene; else modeForRendering = mode;
                    panel.Mode = modeForRendering;
                    SetGlobalSettings(panel, modeForRendering);                    

                    this.currentPanel = panel;                   

                    List<ImagePixelRange> ranges = scene.Ranges.ToList();
                    if (Area == ImageArea.All)
                    {
                        ranges = new List<ImagePixelRange>() { new ImagePixelRange(0, 0, size.Width, size.Height) };
                    }

                    List<Bitmap> bitmaps = new List<Bitmap>();
                    for (int i=0;i<ranges.Count;i++)
                    {
                        counter++;
                        this.progressPrefix = "[" + counter + "/" + maxCount+"] " + modeForRendering.ToString() +" "+ scene.Name + " (" + (i+1) + "/" + ranges.Count+")";
                        ImagePixelRange scaledRange = Area == ImageArea.All ? null : GetScaledPixelRange(data, ranges[i]);
                        Bitmap result = panel.GetSingleImage(size.Width, size.Height, scaledRange);
                        if (size.Height < this.height) result = BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>() { new Bitmap(this.width, this.height - size.Height), result });
                        bitmaps.Add(result);
                    }

                    panel.Dispose();
                    this.currentPanel = null;
                    images.Add(BitmapHelp.TransformBitmapListToRow(bitmaps));
                }

                images.Insert(0, GetBitmapText(mode.ToString() + "\n" + TimeSpanToString(DateTime.Now - startTime), images.Any() ? images.Max(x => x.Width) : 100, 10));
                resultList.Add(images);
            }

            return resultList;
        }

        private ImagePixelRange GetScaledPixelRange(InputData data, ImagePixelRange range)
        {
            if (this.width == data.ImageStandardWidth && this.height == data.ImageStandardHeight) return range;

            float fx = (float)this.width / (float)data.ImageStandardWidth;
            float fy = (float)this.height / (float)data.ImageStandardHeight;

            int x = Bound(0, this.width, (int)(range.XStart * fx));
            int y = Bound(0, this.height, (int)(range.YStart * fy));
            int w = (int)(data.ImageStandardWidth * fx);
            int h = (int)(data.ImageStandardHeight * fy);
            if (x + w > this.width) w = this.width - x;
            if (y + h > this.height) w = this.height - y;

            return new ImagePixelRange(x,y,w,h);
        }

        private int Bound(int min, int max, int value)
        {
            if (value < min) return min;
            if (value >= max) return max - 1;
            return value;
        }

        private static Bitmap GetBitmapText(string text, int maxWidth, float textSize)
        {
            Bitmap image1 = new Bitmap(1, 1);
            Graphics grx1 = Graphics.FromImage(image1);
            Font font = new Font("Consolas", textSize);
            SizeF sizef = grx1.MeasureString(text, font);
            grx1.Dispose();

            Bitmap result = new Bitmap(Math.Min((int)sizef.Width, maxWidth), (int)sizef.Height);
            Graphics grx = Graphics.FromImage(result);

            grx.DrawString(text, new Font("Consolas", textSize), Brushes.Black, 0, 0);
            grx.Dispose();
            return result;
        }

        public static Accuracy Quality = Accuracy.Normal;        
        public enum Accuracy
        {
            High,  //Wenn man Teilbereiche rendert
            Normal,   //Wenn man das ganze Bild rendert  
            Middle,
            Quick
        }

        public static ImageArea Area = ImageArea.All;
        public enum ImageArea
        {
            All,
            PartialAreasOnly
        }

        private void SetGlobalSettings(GraphicPanel3D panel, Mode3D mode)
        {            
            panel.GlobalSettings.AutoSaveMode = RaytracerAutoSaveMode.Disabled;

            int[] sampleCountAll, sampleCountPT, sampleCountPhoton;
            if (Area == ImageArea.All)
            {
                //Wenn man das ganze Bild rendert
                sampleCountAll = new int[] { 1000, 300, 10, 1 };
                sampleCountPT = new int[] { 10000, 7000, 400, 100 };
                sampleCountPhoton = new int[] { 400, 150, 10, 1 };
            }else
            {
                //Wenn man Teilbereiche rendert
                sampleCountAll = new int[] { 10000, 1000, 10, 1 };
                sampleCountPT = new int[] { 100000, 10000, 400, 100 };
                sampleCountPhoton = new int[] { 4000, 400, 10, 1 };
            }
            
            
            panel.GlobalSettings.SamplingCount = sampleCountAll[(int)Quality];
            if (mode == Mode3D.PathTracer) panel.GlobalSettings.SamplingCount = sampleCountPT[(int)Quality];
            if (mode == Mode3D.Photonmapping) panel.GlobalSettings.SamplingCount = sampleCountPhoton[(int)Quality];
            if (mode == Mode3D.RadiositySolidAngle || mode == Mode3D.RadiosityHemicube) panel.GlobalSettings.SamplingCount = 1;

            panel.GlobalSettings.RadiositySettings.MaxAreaPerPatch = 0.01f;
            panel.GlobalSettings.RadiositySettings.RadiosityColorMode = RadiosityColorMode.WithColorInterpolation;
            panel.GlobalSettings.ShadowsForRasterizer = RasterizerShadowMode.Shadowmap;
            panel.GlobalSettings.PhotonCount = 100000;
            //panel.GlobalSettings.ThreadCount = 1;
        }
    }
}
