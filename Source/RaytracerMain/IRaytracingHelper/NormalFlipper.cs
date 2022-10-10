using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphicGlobal;
using RaytracingColorEstimator;
using IntersectionTests;
using System.IO;
using TriangleObjectGeneration;
using GraphicMinimal;
using SubpathGenerator;

namespace RaytracerMain
{
    //Erzeugt aus einer Wavefront-Datei eine neue Datei, wo aber aller Flächennormalen aus Sicht der Kamera nach außen zeigen
    //Anwendung: Szene erst mit graphic.AddWaveFrontFileAndSplit(..) einladen, Kamera festlegen und dann mit folgender Zeile speichern:
    //File.WriteAllText(FilePaths.DataDirectory+"FlipperResult.obj", graphic.GetFlippedWavefrontFileFromCurrentSceneData(graphic.Width, graphic.Height));
    public class NormalFlipper
    {
        public static string GetFlippedWavefrontFile(Frame3DData data, int imageWidth, int imageHeight)
        {
            NormalFlipper flipper = new NormalFlipper(new RaytracingFrame3DData(data)
            {
                ScreenWidth = imageWidth,
                ScreenHeight = imageHeight,
                ProgressChanged = (text, percent)=> { },
                StopTrigger = new System.Threading.CancellationTokenSource(),
                PixelRange = new ImagePixelRange(0, 0, imageWidth, imageHeight)
            });
            return flipper.GetFlippedWavefrontFile(new Rand(0));
        }

        class TriangleFlippCounter
        {
            public int FlipYes = 0;
            public int FlipNo = 0;
            public IIntersecableObject Triangle1 { get; private set; }
            public TriangleObjectGeneration.WaveFrontObject.WaveFrontTriangle Triangle2 { get; private set; }

            public TriangleFlippCounter(IIntersecableObject triangle1, TriangleObjectGeneration.WaveFrontObject.WaveFrontTriangle triangle2)
            {
                this.Triangle1 = triangle1;
                this.Triangle2 = triangle2;
            }
        }

        private readonly PixelRadianceData pixelData;
        private readonly List<TriangleFlippCounter> flipCounter;
        private readonly RaytracingFrame3DData data;

        public NormalFlipper(RaytracingFrame3DData data)
        {
            this.data = data;
            this.pixelData = PixelRadianceCreationHelper.CreatePixelRadianceData(data, new SubPathSettings()
            {
                EyePathType = PathSamplingType.NoMedia,
                LightPathType = PathSamplingType.NoMedia
            }, null);

            var triangles1 = pixelData.IntersectionFinder.RayObjekteRawList;
            var triangles2 = data.DrawingObjects.SelectMany(x => x.GetTrianglesInWorldSpace()).ToList();
            var triangles3 = new WaveFrontObjFile(File.ReadAllText(GetSourceFileName())).Objects.SelectMany(x => x.Triangles).ToList();

            if (triangles1.Count != triangles2.Count) throw new Exception("Abnormal Error. Szenen mit Kugeln und Blobs gehen nicht");

            this.flipCounter = new List<TriangleFlippCounter>();
            for (int i = 0; i < triangles1.Count; i++)
            {
                var t = triangles3.FirstOrDefault(x => IsEqualTriangle(x.T, triangles2[i]));
                this.flipCounter.Add(new TriangleFlippCounter(triangles1[i], t));
            }
        }

        private static bool IsEqualTriangle(Triangle t1, Triangle t2)
        {
            float f = 0.0001f;
            return (t1.V[0].Position - t2.V[0].Position).Length() < f &&
                   (t1.V[1].Position - t2.V[1].Position).Length() < f &&
                   (t1.V[2].Position - t2.V[2].Position).Length() < f;
        }
        private string GetSourceFileName()
        {
            return this.data.DrawingObjects.First().TriangleData.Name.Split(':')[1];
        }

        private void IncreaseFlippCounter(ImagePixelRange range, IRandom rand)
        {
            for (int x = 0; x < range.Width; x++)
                for (int y = 0; y < range.Height; y++)
                {
                    var eyePath = this.pixelData.EyePathSampler.SamplePathFromCamera(range.XStart + x, range.YStart + y, rand);
                    var lightPath = this.pixelData.LightPathSampler.SamplePathFromLighsource(rand);

                    List<IntersectionPoint> points = new List<IntersectionPoint>();
                    points.AddRange(eyePath.Points.Select(p => p.SurfacePoint));
                    points.AddRange(lightPath.Points.Select(p => p.SurfacePoint));
                    foreach (var p in points)
                    {
                        if (p.IntersectedObject != null)
                        {
                            var counter = this.flipCounter.First(c => c.Triangle1 == p.IntersectedObject);
                            if (p.OrientedFlatNormal * p.FlatNormal > 0) counter.FlipNo++; else counter.FlipYes++;
                        }
                    }
                }
        }

        private string GetWavefrontResultContentFromFlipData()
        {
            StringBuilder str = new StringBuilder();
            var lines = File.ReadAllLines(GetSourceFileName());
            foreach (var line in lines)
            {
                var t = this.flipCounter.FirstOrDefault(x => x.Triangle2 != null && x.Triangle2.Line == line);
                if (t != null)
                {
                    if (t.FlipNo >= t.FlipYes)
                    {
                        str.Append(line + System.Environment.NewLine);
                    }
                    else
                    {
                        var fields = line.Split(' ');
                        string newline = fields[0] + " " + fields[3] + " " + fields[2] + " " + fields[1];
                        str.Append(newline + System.Environment.NewLine);
                    }
                }
                else
                {
                    str.Append(line + System.Environment.NewLine);
                }
            }

            return str.ToString();
        }

        public string GetFlippedWavefrontFile(IRandom rand)
        {
            IncreaseFlippCounter(this.data.PixelRange, rand);
            return GetWavefrontResultContentFromFlipData();
            //            File.WriteAllText("FlipperResult.obj", );
        }
    }
}
