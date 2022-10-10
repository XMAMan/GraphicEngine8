using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphicMinimal;
using System.Drawing;
using GraphicGlobal;
using RaytracingColorEstimator;
using FullPathGenerator;
using BitmapHelper;
using System.IO;
using FullPathGenerator.AnalyseHelper;

namespace ImageCreator
{
    public class ImageFullPathAnalyser : IMasterImageCreator
    {
        private readonly RaytracingFrame3DData data;
        private readonly ImageBuffer imageBuffer;
        private readonly IFrameEstimator masterFrame;
        private int renderedFramesCounter = 0;
        private readonly string outputFolder;

        public IRaytracerGlobalDrawingProps GlobalDrawingProps { get { return this.data.GlobalObjektPropertys; } }

        public float Progress => throw new NotImplementedException();

        public string ProgressText => throw new NotImplementedException();

        public RaytracerWorkingState State => throw new NotImplementedException();

        public ImageFullPathAnalyser(IFrameEstimator frameEstimator, RaytracingFrame3DData data, string outputFolder)
        {
            this.outputFolder = outputFolder;
            this.data = data;
            this.masterFrame = frameEstimator;
            this.imageBuffer = new ImageBuffer(data.PixelRange.Width, data.PixelRange.Height, new Vector3D(0,0,0));
        }

        public void StopRaytracing()
        {
            this.data.StopTrigger.Cancel();
        }

        public ImageBuffer GetProgressImage()
        {
            if (this.renderedFramesCounter == 0) return this.imageBuffer;

            return this.imageBuffer.GetColorScaledImage(1.0f / this.renderedFramesCounter);
        }



        public ImageBuffer GetImage(ImagePixelRange range)
        {
            string fileName = this.outputFolder + "\\Frames.dat";

            CreateFramesAndSaveItToFile(fileName);
            WriteResultTextToFile(GetFramesFromFile(fileName), this.outputFolder + "\\PixelRangeResult.txt");
            File.WriteAllText(this.outputFolder + "\\PathSpace.txt", GetPathSpaceContributions(GetFramesFromFile(fileName)));

            this.data.ProgressChanged("Erstelle Filter-Liste", 0);
            var filters = GetFilterList(GetAllPathsFromFile(fileName));

            GetImagesFromFileAndSaveItToFolder(filters, fileName);

            return this.imageBuffer.GetColorScaledImage(1.0f / this.renderedFramesCounter);
        }

        private void CreateFramesAndSaveItToFile(string fileName)
        {
            IRandom rand = new Rand(0);

            //Kopiere aus der PixelRangeResult.txt-Datei den entsprechenden Rand-FramePrepare-String
            //string framePrepare = "AAEAAAD/////AQAAAAAAAAAEAQAAAA1TeXN0ZW0uUmFuZG9tAwAAAAVpbmV4dAZpbmV4dHAJU2VlZEFycmF5AAAHCAgIHwAAADQAAAAJAgAAAA8CAAAAOAAAAAgAAAAA2JFEHPsiqkyss2RL2DaBU3/JcGUZGPwLac1cQYoUumu6mHAcwelAbUvWtWqFGgA5svixDV7aiytKHUlb3TvpWjFn4UeolA4XjEeHbdLsb0q4X9pG/EdZWGKwvlRtM0sWrqe6GklR+362IoULgfijLqVDnz5RqMwSShAfAQaFDQoSoJsyVlmLGoO8HTlZyGlVgyF2cP8mjk/FIyA8lbRMDCtrGmrER2gyuSrSKf28SmGlJvBdAsRjcf5uxXDYuVcIaYTqWQoaIEu7dTwLEI1YeUrin2MGH91p0aIOVQs=";
            //rand = new Rand(framePrepare);

            BinaryWriter stream = new BinaryWriter(File.Open(fileName, FileMode.Create));
            var text = File.CreateText(this.outputFolder + "\\RadianceSumWithGammaAndClamping.txt");

            for (int i = 0; i < this.GlobalDrawingProps.SamplingCount; i++)
            {
                string randomObjectBase64CodedForFramePrepare = rand.ToBase64String();

                Frame frame = new Frame(this.data.PixelRange, randomObjectBase64CodedForFramePrepare);

                try
                {
                    this.masterFrame.DoFramePrepareStep(this.data.PixelRange, i, rand);
                }
                catch (Exception ex)
                {
                    var debugData = new RaytracingDebuggingData(
                        new RaytracingDebuggingData.DoFramePrepareStepParameter() 
                        { 
                            PixelRange = data.PixelRange, 
                            FrameIterationNumber = i, 
                            RandomObjectBase64Coded = randomObjectBase64CodedForFramePrepare 
                        }, 
                        new Size(this.data.ScreenWidth, this.data.ScreenHeight),
                        this.data.GlobalObjektPropertys);
                    throw new Exception(debugData.ToXmlString(), ex);
                }

                ImageBuffer buffer = new ImageBuffer(data.PixelRange.Width, data.PixelRange.Height, new Vector3D(0,0,0));
                for (int x = 0; x < this.data.PixelRange.Width; x++)
                    for (int y = 0; y < this.data.PixelRange.Height; y++)
                    {
                        if (data.StopTrigger.IsCancellationRequested)
                        {
                            stream.Dispose();
                            return;
                        }

                        string randomObjectBase64Coded = rand.ToBase64String();

                        try
                        {
                            var result = this.masterFrame.GetFullPathSampleResult(this.data.PixelRange.XStart + x, this.data.PixelRange.YStart + y, rand);
                            result.MainPaths.ForEach(p => p.PixelPosition = new Vector2D(this.data.PixelRange.XStart + x, this.data.PixelRange.YStart + y));

                            List<FullPath> paths = new List<FullPath>();

                            paths.AddRange(result.MainPaths);
                            paths.AddRange(result.LighttracingPaths);

                            frame.AddPaths(paths);
                            foreach (var p in paths)
                            {
                                int rx = (int)Math.Floor(p.PixelPosition.X - data.PixelRange.XStart);
                                int ry = (int)Math.Floor(p.PixelPosition.Y - data.PixelRange.YStart);
                                if (rx >= 0 && rx < buffer.Width && ry >= 0 && ry < buffer.Height)
                                {
                                    buffer[rx, ry] += p.Radiance;
                                }
                            }

                            float progress = (x * this.data.PixelRange.Height + y) / (float)(this.data.PixelRange.Width * this.data.PixelRange.Height);
                            this.data.ProgressChanged("Frame: " + renderedFramesCounter, (renderedFramesCounter + progress) * 100.0f / this.GlobalDrawingProps.SamplingCount);
                        }
                        catch (Exception ex)
                        {
                            stream.Dispose();
                            var debugData = new RaytracingDebuggingData(
                                new RaytracingDebuggingData.DoFramePrepareStepParameter()
                                {
                                    PixelRange = data.PixelRange,
                                    FrameIterationNumber = i,
                                    RandomObjectBase64Coded = randomObjectBase64CodedForFramePrepare
                                },
                                new RaytracingDebuggingData.GetFullPathSampleResultParameter()
                                {
                                    PixX = this.data.PixelRange.XStart + x,
                                    PixY = this.data.PixelRange.YStart + y,
                                    RandomObjectBase64Coded = randomObjectBase64Coded
                                },
                                new Size(this.data.ScreenWidth, this.data.ScreenHeight),
                                this.data.GlobalObjektPropertys);
                            throw new Exception(debugData.ToXmlString(), ex);
                        }
                    }
                this.renderedFramesCounter++;
                this.imageBuffer.AddFrame(buffer);

                Vector3D allRadiance = GetProgressImage().GetSumOverAllPixelsWithGammaAndClampling();
                text.WriteLine($"{this.renderedFramesCounter}\t{allRadiance.ToShortString()}");
                text.Flush();

                byte[] bytes = ObjectToByteArrayConverter.ConvertObjectToByteArray(frame);
                stream.Write(bytes.Length);
                stream.Write(bytes);
                bytes = null; //Gebe den Frame-Speicher wieder frei
            }

            stream.Dispose();
            text.Dispose();
        }

        private IEnumerable<Frame> GetFramesFromFile(string fileName)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    int byteCount = reader.ReadInt32();
                    byte[] bytes = reader.ReadBytes(byteCount);
                    Frame frame = ObjectToByteArrayConverter.ConvertByteArrayToObject<Frame>(bytes);
                    yield return frame;
                }                
            }
        }

        private IEnumerable<FullPathSimple> GetAllPathsFromFile(string fileName)
        {
            foreach (var frame in GetFramesFromFile(fileName))
                for (int x = 0; x < frame.Pixels.GetLength(0); x++)
                    for (int y = 0; y < frame.Pixels.GetLength(1); y++)
                        foreach(var path in frame.Pixels[x, y].Paths)
                        {
                            yield return path;
                        }
        }

        private void GetImagesFromFileAndSaveItToFolder(List<IPathFilter> filters, string fileName)
        {
            int width = this.imageBuffer.Width;
            int height = this.imageBuffer.Height;

            List<ImageBuffer> imageBuffers = filters.Select(x => new ImageBuffer(width, height, new Vector3D(0,0,0))).ToList();

            int frameCounter = 0;
            foreach (var frame in GetFramesFromFile(fileName))
            {
                this.data.ProgressChanged($"Erstelle die Bilder", frameCounter * 100.0f / this.renderedFramesCounter);
                frameCounter++;
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                    {
                        foreach (var p in frame.Pixels[x, y].Paths)
                        {
                            for (int i=0;i< filters.Count;i++)
                            {
                                if (filters[i].DoesMatch(p))
                                {
                                    if (filters[i].UseMisWeight)
                                        imageBuffers[i][x, y] += p.PathContribution * p.MisWeight;
                                    else
                                        imageBuffers[i][x, y] += p.PathContribution;
                                }
                            }
                            
                        }
                    }
            }

            for (int i = 0; i < filters.Count; i++)
            {
                this.data.ProgressChanged($"Speichere {filters[i].Name + ".bmp"}", i * 100.0f / filters.Count);
                Bitmap image = Tonemapping.GetImage(imageBuffers[i].GetColorScaledImage(this.GlobalDrawingProps.BrightnessFactor / frameCounter), this.GlobalDrawingProps.Tonemapping);
                
                try
                {
                    image.Save(this.outputFolder + "\\" + filters[i].Name + ".bmp");
                }
                catch (PathTooLongException )
                {
                    //Der angegebene Pfad und/oder Dateiname ist zu lang. Der vollständig qualifizierte Dateiname muss kürzer als 260 Zeichen und der Pfadname kürzer als 248 Zeichen sein.
                }

            }
        }

        private void WriteResultTextToFile(IEnumerable<Frame> frames, string fileName)
        {
            StreamWriter text = new StreamWriter(fileName);
            Vector3D sum = new Vector3D(0, 0, 0);

            int i = 0;
            foreach(var frame in frames)
            {
                this.data.ProgressChanged("Erstelle PixelRangeResult.txt", i * 100.0f / this.renderedFramesCounter);

                text.WriteLine("Iteration=" + i);
                text.WriteLine(frame.RandomFrameInitValue);

                Vector3D rangeSum = new Vector3D(0, 0, 0);
                for (int x = 0; x < frame.Pixels.GetLength(0); x++)
                    for (int y = 0; y < frame.Pixels.GetLength(1); y++)
                    {
                        foreach (var p in frame.Pixels[x, y].Paths)
                        {
                            rangeSum += p.Radiance;
                            bool isFireFly = p.Radiance.Length() > 10;
                            text.WriteLine($"[{x};{y}] {p.SamplingMethod}\t{p.PathSpace}\t{p.Radiance}\tMIS={p.MisWeight}\t" + (isFireFly ? "Fire" : ""));
                            //if (isFireFly) 
                                //text.WriteLine(p.PathPointInformations);
                        }
                    }

                text.WriteLine("PixelRangeSum=" + rangeSum);
                text.WriteLine("PixelRangeAvg=" + rangeSum / (frame.Pixels.GetLength(0) * frame.Pixels.GetLength(1)));

                sum += rangeSum;
                i++;
            }

            text.WriteLine("MasterAverage=" + sum / i);

            text.Dispose();
        }

        private string GetPathSpaceContributions(IEnumerable<Frame> frames)
        {
            PathContributionForEachPathSpace all = new PathContributionForEachPathSpace();
            Dictionary<SamplingMethod, PathContributionForEachPathSpace> singleNoMis = new Dictionary<SamplingMethod, PathContributionForEachPathSpace>();

            int i = 0;
            foreach (var frame in frames)
            {
                this.data.ProgressChanged("Erstelle PathSpace.txt", i * 100.0f / this.renderedFramesCounter);
                i++;

                for (int x = 0; x < frame.Pixels.GetLength(0); x++)
                    for (int y = 0; y < frame.Pixels.GetLength(1); y++)
                    {
                        foreach (var path in frame.Pixels[x, y].Paths)
                        {
                            all.AddEntry(ShortSpace(path.PathSpace), path.Radiance / this.renderedFramesCounter);

                            if (singleNoMis.ContainsKey(path.SamplingMethod) == false) singleNoMis.Add(path.SamplingMethod, new PathContributionForEachPathSpace());
                            singleNoMis[path.SamplingMethod].AddEntry(ShortSpace(path.PathSpace), path.PathContribution * path.SingleMisWeight / this.renderedFramesCounter);
                        }
                    }
            }

            StringBuilder str = new StringBuilder();
            str.AppendLine(GetPathSpaceString(all, "All with MIS"));
            foreach (var method in singleNoMis.Keys) str.AppendLine(GetPathSpaceString(singleNoMis[method], method.ToString()));

            return str.ToString();
        }

        private string ShortSpace(string space)
        {
            if (space.Length > 10) return "Long-Path";
            return space;
        }

        private string GetPathSpaceString(PathContributionForEachPathSpace space, string header)
        {
            return header + Environment.NewLine + "Sum=" + space.SumOverAllPathSpaces().ToShortString() + Environment.NewLine + space.ToString();
        }

        private List<IPathFilter> GetFilterList(IEnumerable<FullPathSimple> paths)
        {
            List<IPathFilter> filters = new List<IPathFilter>
            {
                new TakeAll(),
                //new FullBDPT(),
                new MediaFullBDPT()
            };
            filters.AddRange(AllFromSamplingMethod.GetAllFilter(paths)); //DirectLighting_MIS/NoMis
            //filters.AddRange(PathSpaceFromSamplingMethod.GetAllFilter(paths)); //C D D D L
            
            return filters;
        }

        public Vector3D GetColorFromSinglePixelForDebuggingPurpose(RaytracingDebuggingData debuggingData)
        {
            return null;
        }

        public void SaveToFolder()
        {
        }

        [Serializable]
        class FullPathSimple
        {
            public SamplingMethod SamplingMethod { get; private set; }
            public string PathSpace { get; private set; }
            public string PathPointInformations { get; private set; }            
            public Vector3D PathContribution { get; private set; }
            public float MisWeight { get; private set; } //MIS-Gewich über alle verwendenen Fullpath-Sampler
            public float SingleMisWeight { get; private set; }//Wenn man so tut, als ob man nur mit ein FullPathSampler arbeiten würde

            public Vector3D Radiance { get { return this.PathContribution * this.MisWeight; } }

            public FullPathSimple(FullPath path)
            {
                this.SamplingMethod = path.SamplingMethod;
                this.PathSpace = path.GetPathSpaceString();
                //this.PathPointInformations = path.GetLocationAndPathWeightInformation();

                this.PathContribution = path.PathContribution;
                this.MisWeight = path.MisWeight;
                this.SingleMisWeight = 1.0f / path.Sampler.SampleCountForGivenPath(path);
            }
        }

        [Serializable]
        class Pix
        {
            public List<FullPathSimple> Paths = new List<FullPathSimple>();
            public void AddPath(FullPath path)
            {
                this.Paths.Add(new FullPathSimple(path));
            }
        }

        [Serializable]
        class Frame
        {
            public Pix[,] Pixels { get; private set; }
            private readonly ImagePixelRange pixelRange;
            public string RandomFrameInitValue { get; private set; }
            public Frame(ImagePixelRange pixelRange, string randomFrameInitValue)
            {
                this.Pixels = new Pix[pixelRange.Width, pixelRange.Height];
                for (int x = 0; x < pixelRange.Width; x++)
                    for (int y = 0; y < pixelRange.Height; y++)
                        Pixels[x, y] = new Pix();

                this.pixelRange = pixelRange;
                this.RandomFrameInitValue = randomFrameInitValue;
            }

            public void AddPaths(List<FullPath> paths)
            {
                foreach (var p in paths)
                {
                    int pixX = (int)Math.Floor(p.PixelPosition.X - pixelRange.XStart);
                    int pixY = (int)Math.Floor(p.PixelPosition.Y - pixelRange.YStart);
                    if (pixX >= 0 && pixX < pixelRange.Width && pixY >= 0 && pixY < pixelRange.Height)
                    {
                        Pixels[pixX, pixY].AddPath(p);
                    }
                }
            }
        }

        interface IPathFilter
        {
            bool DoesMatch(FullPathSimple path);
            string Name { get; }
            bool UseMisWeight { get; }
        }

        class TakeAll : IPathFilter
        {
            public string Name { get; private set; } = "All";
            public bool UseMisWeight => true;
            public bool DoesMatch(FullPathSimple path)
            {
                return true;
            }
        }

        class FullBDPT : IPathFilter
        {
            public string Name { get; private set; } = "BDPT";
            public bool UseMisWeight => true;
            public bool DoesMatch(FullPathSimple path)
            {
                return path.SamplingMethod == SamplingMethod.DirectLighting ||
                       path.SamplingMethod == SamplingMethod.MultipleDirectLighting ||
                       path.SamplingMethod == SamplingMethod.LightTracing ||
                       path.SamplingMethod == SamplingMethod.PathTracing ||
                       path.SamplingMethod == SamplingMethod.SpecularPathtracing ||
                       path.SamplingMethod == SamplingMethod.VertexConnection;
            }
        }

        class MediaFullBDPT : IPathFilter
        {
            public string Name { get; private set; } = "MediaBDPT";
            public bool UseMisWeight => true;
            public bool DoesMatch(FullPathSimple path)
            {
                return (path.SamplingMethod == SamplingMethod.DirectLighting ||
                       path.SamplingMethod == SamplingMethod.MultipleDirectLighting ||
                       path.SamplingMethod == SamplingMethod.LightTracing ||
                       path.SamplingMethod == SamplingMethod.PathTracing ||
                       path.SamplingMethod == SamplingMethod.SpecularPathtracing ||
                       path.SamplingMethod == SamplingMethod.VertexConnection) && path.PathSpace.Contains("P");
            }
        }

        //Über alle Pathspaces von ein einzelner Fullpath-Sampler
        class AllFromSamplingMethod : IPathFilter
        {
            public string Name { get; private set; }
            public bool UseMisWeight { get; private set; }

            private readonly SamplingMethod method;
            public AllFromSamplingMethod(SamplingMethod method, bool useMisWeight)
            {
                this.Name = method.ToString() + "_" + (useMisWeight ? "MISWeighted" : "NoMIS");
                this.method = method;
                this.UseMisWeight = useMisWeight;
            }
            public bool DoesMatch(FullPathSimple path)
            {
                return path.SamplingMethod == this.method;
            }

            public static List<AllFromSamplingMethod> GetAllFilter(IEnumerable<FullPathSimple> paths)
            {
                return paths.Select(x => x.SamplingMethod).Distinct().SelectMany(x => new[] 
                { 
                    new AllFromSamplingMethod(x, true),     //Mit MIS-Gewicht
                    new AllFromSamplingMethod(x, false)     //Ohne MIS-Gewicht
                }).ToList();
            }
        }

        class PathSpaceFromSamplingMethod : IPathFilter
        {
            public string Name { get; private set; }
            public bool UseMisWeight { get; private set; }

            private readonly SamplingMethod method;
            private readonly string pathSpace;
            public PathSpaceFromSamplingMethod(SamplingMethod method, string pathSpace, bool useMisWeight)
            {
                this.Name = method.ToString() + "_" + pathSpace;
                this.method = method;
                this.pathSpace = pathSpace;
                this.UseMisWeight = useMisWeight;
            }
            public bool DoesMatch(FullPathSimple path)
            {
                return path.SamplingMethod == this.method && path.PathSpace == this.pathSpace;
            }

            public static List<PathSpaceFromSamplingMethod> GetAllFilter(IEnumerable<FullPathSimple> paths)
            {
                return paths.Select(x => new PathSpaceFromSamplingMethod(x.SamplingMethod, x.PathSpace, true)).GroupBy(x => x.Name).Select(x => x.First()).ToList();
            }
        }
    }
}
