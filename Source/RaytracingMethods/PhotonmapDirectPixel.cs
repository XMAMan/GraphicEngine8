using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using RaytracingColorEstimator;
using FullPathGenerator;
using GraphicGlobal;
using SubpathGenerator;
using ParticipatingMedia;
using GraphicPipelineCPU.Rasterizer;
using System.Drawing;

namespace RaytracingMethods
{
    public class PhotonmapDirectPixel : IPixelEstimator
    {
        private Point pixRangeTopLeft;
        private ImageBuffer image;

        public PhotonmapDirectPixel() { }
        private PhotonmapDirectPixel(PhotonmapDirectPixel copy)
        {
            this.pixRangeTopLeft = copy.pixRangeTopLeft;
            this.image = copy.image;
        }
        public bool CreatesLigthPaths { get; } = false;

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.pixRangeTopLeft = data.PixelRange.TopLeft;
            var settings = data.GlobalObjektPropertys.PhotonmapPixelSettings;

            bool hasMedia = PixelRadianceCalculator.HasSceneAnyMedia(data);
            if (settings == PhotonmapDirectPixelSetting.ShowNoMediaCaustics) hasMedia = false;

            bool maxLightPathLengthIsOne = false;
            SubPathSettings subPathSettings = null;
            if (hasMedia == false)
            {
                subPathSettings = new SubPathSettings()
                {
                    //VCM / Photonmapping
                    EyePathType = PathSamplingType.NoMedia,
                    LightPathType = PathSamplingType.NoMedia,
                    MaxEyePathLength = maxLightPathLengthIsOne ? 1 : -1
                };
            }else
            {
                bool useMediaBeamTracer = settings == PhotonmapDirectPixelSetting.ShowGodRays;
                if (useMediaBeamTracer)
                {
                    subPathSettings = new SubPathSettings()
                    {
                        //MediaBeamTracer
                        EyePathType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                        LightPathType = PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                    };
                }else
                {
                    subPathSettings = new SubPathSettings()
                    {
                        //UPBP
                        EyePathType = PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling,
                        LightPathType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                        MaxEyePathLength = maxLightPathLengthIsOne ? 1 : -1
                    };                    
                }
            }            

            var pixelRadianceCalculator = PixelRadianceCreationHelper.CreatePixelRadianceCalculator(data,
                subPathSettings,
                new FullPathSettings()
                {
                    UsePathTracing = false,
                    UseDirectLighting = false,
                    UseVertexMerging = false,
                });

            //var lightPaths = pixelRadianceCalculator.CreateLightPathListWithMultipleThreads(data);
            var lightPaths = pixelRadianceCalculator.CreateLightPathListWithSingleThread(new Rand(0)); //Wenn ich den MediaBeamTracer nachstellen will

            if (settings == PhotonmapDirectPixelSetting.CountHowManyPhotonsAreInFieldOfView)
            {
                //System.IO.File.WriteAllText("photonmapPixel.txt", lightPaths.Where(x => x.Points.Length >= 2 && pixelRadianceCalculator.IsPointInVieldOfFiew(x.Points[1].Position)).Count() / (float)data.GlobalObjektPropertys.PhotonCount + "");
                System.IO.File.WriteAllText("photonmapPixel.txt", lightPaths.Where(x => x.Points.Any(y => y.Index > 0 && pixelRadianceCalculator.IsPointInFieldOfView(y.Position))).Count() / (float)data.GlobalObjektPropertys.PhotonCount + "");
            }


            this.image = GetPhotonmapDirectImage(data, lightPaths);
        }

        
        private static ImageBuffer GetPhotonmapDirectImage(RaytracingFrame3DData frameData, List<SubPath> lightPaths)
        {
            Rasterizer rasterizer = new Rasterizer(frameData.GlobalObjektPropertys.Camera, frameData.ScreenWidth, frameData.ScreenHeight, frameData.PixelRange);

            switch (frameData.GlobalObjektPropertys.PhotonmapPixelSettings)
            {
                case PhotonmapDirectPixelSetting.ShowGodRays:
                    {
                        var beams = lightPaths.SelectMany(x => x.Points).Where(x => x.LineToNextPoint != null && x.LineToNextPoint.HasScattering() && x.Index == 0).Select(x => x.LineToNextPoint).ToList(); //MediaBeamTracer
                        foreach (var b in beams)
                        {
                            rasterizer.DrawLine(b.StartPoint.Position, b.Ray.Start + b.Ray.Direction * b.LongRayLength, new Vector3D(1, 1, 1));
                        }
                    }
                    break;

                case PhotonmapDirectPixelSetting.ShowMediaShortBeams:
                    {
                        var beams = lightPaths.SelectMany(x => x.Points).Where(x => x.LineToNextPoint != null && x.LineToNextPoint.HasScattering()).Select(x => x.LineToNextPoint).ToList();
                        foreach (var b in beams)
                        {
                            rasterizer.DrawLine(b.StartPoint.Position, b.Ray.Start + b.Ray.Direction * b.ShortRayLength, new Vector3D(1, 1, 1));
                        }
                    }
                    break;

                case PhotonmapDirectPixelSetting.ShowMediaLongBeams:
                    {
                        var beams = lightPaths.SelectMany(x => x.Points).Where(x => x.LineToNextPoint != null && x.LineToNextPoint.HasScattering()).Select(x => x.LineToNextPoint).ToList();
                        foreach (var b in beams)
                        {
                            rasterizer.DrawLine(b.StartPoint.Position, b.Ray.Start + b.Ray.Direction * b.LongRayLength, new Vector3D(1, 1, 1));
                        }
                    }
                    break;

                case PhotonmapDirectPixelSetting.ShowNoMediaCaustics:
                    {
                        //var photons = lightPaths.Where(x => x.Points.Any(y => y.BrdfSampleEventOnThisPoint.IsSpecualarReflected == true)).SelectMany(x => x.Points).Where(x => x.DirectionToThisPoint != null && x.BrdfSampleEventOnThisPoint.IsSpecualarReflected && x.Index == 2).ToList();

                        //Spekularpfade (Lichtquelle strahlt Glasobjekt an)
                        foreach (var path in lightPaths.Where(x => x.Points.Length > 3 && x.Points[1].BrdfPoint.IsOnlySpecular && x.Points[2].BrdfPoint.IsOnlySpecular))
                        {

                            for (int i = 0; i < path.Points.Length - 1; i++)
                            {
                                rasterizer.DrawLine(path.Points[i].Position, path.Points[i + 1].Position, new Vector3D(1, 1, 1));
                                rasterizer.DrawPoint(path.Points[i].Position, new Vector3D(1, 0, 0));
                                rasterizer.DrawPoint(path.Points[i + 1].Position, new Vector3D(1, 0, 0));
                            }
                        }
                    }
                    break;

                case PhotonmapDirectPixelSetting.ShowPixelPhotons:
                    {
                        var photons = lightPaths.SelectMany(x => x.Points).Where(x => x.Index >= 0 /*&& x.IsLocatedOnSurvace == true*/).ToList();    //Direkt + Indirekt
                        for (int i = 0; i < photons.Count; i++)
                        {
                            //rasterizer.DrawPoint(photons[i].Position, photons[i].PathWeight);
                            rasterizer.DrawPoint(photons[i].Position, new Vector3D(1, 1, 1));
                        }
                    }
                    break;

                case PhotonmapDirectPixelSetting.ShowParticlePhotons:
                    {
                        var photons = lightPaths.SelectMany(x => x.Points).Where(x => x.LocationType == MediaPointLocationType.MediaParticle).ToList();
                        for (int i = 0; i < photons.Count; i++)
                        {
                            rasterizer.DrawPoint(photons[i].Position, new Vector3D(1, 1, 1));
                        }
                    }
                    break;

                case PhotonmapDirectPixelSetting.ShowDirectLightPhotons:
                    {
                        var photons = lightPaths.SelectMany(x => x.Points).Where(x => /*x.IsLocatedOnSurvace == true &&*/ x.Index == 1).ToList();  //Nur Direktes Licht
                        for (int i = 0; i < photons.Count; i++)
                        {
                            //rasterizer.DrawPoint(photons[i].Position, photons[i].PathWeight);
                            rasterizer.DrawPoint(photons[i].Position, new Vector3D(1, 1, 1));
                        }
                    }
                    break;
            }

            return rasterizer.ImageBuffer;
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            return new FullPathSampleResult() { RadianceFromRequestetPixel = this.image[x - this.pixRangeTopLeft.X, y - this.pixRangeTopLeft.Y] };
        }
    }

    class Rasterizer
    {
        public ImageBuffer ImageBuffer { get; private set; }
        private readonly float[,] depthBuffer;

        private readonly NoInterpolationRasterizer raster;

        public Rasterizer(Camera camera, int screenWidth, int screenHeight, ImagePixelRange range)
        {
            var cameraMatrix = Matrix4x4.LookAt(camera.Position, camera.Forward, camera.Up);
            var projectionMatrix = Matrix4x4.ProjectionMatrixPerspective(camera.OpeningAngleY, (float)screenWidth / (float)screenHeight, 0.1f, 20000.0f);
            var toWindowsSpaceMatrix = cameraMatrix * projectionMatrix;

            this.ImageBuffer = new ImageBuffer(range.Width, range.Height);
            this.depthBuffer = new float[range.Width, range.Height];
            this.raster = new NoInterpolationRasterizer(toWindowsSpaceMatrix, screenWidth, screenHeight, range);
            ClearScreen();
        }

        public void ClearScreen()
        {
            for (int x = 0; x < ImageBuffer.Width; x++)
                for (int y = 0; y < ImageBuffer.Height; y++)
                {
                    ImageBuffer[x, y] = new Vector3D(0, 0, 0);
                    depthBuffer[x, y] = 1.0f;
                }
        }

        public void DrawLine(Vector3D p1, Vector3D p2, Vector3D color)
        {
            this.raster.DrawLine(p1, p2, (pix,z) => { DrawPixel(pix, z, color); });
        }

        public void DrawPoint(Vector3D position, Vector3D color)
        {
            this.raster.DrawPixel(position, (pix, z) => { DrawPixel(pix, z, color); });
        }

        private void DrawPixel(Point pix, float windowZ, Vector3D color)
        {
            if (pix.X >= 0 && pix.X < this.depthBuffer.GetLength(0) && pix.Y >= 0 && pix.Y < this.depthBuffer.GetLength(1))
            {
                if (depthBuffer[(int)pix.X, (int)pix.Y] > windowZ)
                {
                    depthBuffer[(int)pix.X, (int)pix.Y] = windowZ;
                    ImageBuffer[(int)pix.X, (int)pix.Y] = color;
                }
            }
        }
    }
}
