using BitmapHelper;
using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest._01_BasicTests.BasicTestHelper;
using GraphicGlobal;
using GraphicMinimal;
using PdfHistogram;
using Photonusmap;
using RayCameraNamespace;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace FullPathGeneratorTest.BasicTests.BasicTestHelper
{
    interface IFullPathTestData
    {
        SubpathSampler EyePathSampler { get; }
        SubpathSampler LightPathSampler { get; }
        int PixX { get; }
        int PixY { get; }
        int PhotonenCount { get; }
        float SizeFactor { get; }
    }

    static class PathContributionCalculator
    {
        public static PathContributionForEachPathSpace GetPathContributionForEachPathSpace(Sampler sampler, BoxTestScene testSzene, int sampleCount)
        {
            PathContributionForEachPathSpace pathContribution = new PathContributionForEachPathSpace();

            //Die Genauigkeit, die ich aus einer Photonmap erhalte erechnet sich aus 
            //Photonmap-Ausbeute = FrameCount * PhotonenCountPerFrame * SampleCountPerFrame
            //Wenn ich mit weniger Photonen auskommen will, dann muss ich dafür mehr Frame-Steps nehmen, damit die PhotonenAusbeute-Zahl Konstant bleibt
            //Die Gesamtrechenlast steigt, wenn ich nur wenige Photonen pro Frame aussende, da ich mit der erhöhten Frame-Zahl nun auch
            //mehr Eye-Subpfade erstelle. Der gesparte Speicherplatz pro Frame wird mit mehr Eye-Subpath-CPU bezahlt.
            //Resümee: Solange der Speicherplatz reicht sollte man ihn nutzen. Das Geizen dort führt zu unnötig längeren UnitTest-Ausführzeiten.
            int frameCount = 1;
            for (int j=0;j<frameCount;j++)
            {
                var frameData = PathContributionCalculator.CreateFrameDataWithPhotonmap(testSzene.LightPathSampler, testSzene.PhotonenCount / frameCount, testSzene.rand, testSzene.SizeFactor, sampler.PhotonSettings);

                for (int i = 0; i < sampleCount; i++)
                {
                    var eyePath = sampler.CreateEyePath ? testSzene.EyePathSampler.SamplePathFromCamera(testSzene.PixX, testSzene.PixY, testSzene.rand) : null;
                    var lightPath = sampler.CreateLightPath ? testSzene.LightPathSampler.SamplePathFromLighsource(testSzene.rand) : null;

                    List<FullPath> paths = sampler.SamplerMethod.SampleFullPaths(eyePath, lightPath, frameData, testSzene.rand);
                    foreach (var path in paths)
                    {
                        if (path.PixelPosition == null || ((int)path.PixelPosition.X == testSzene.PixX && (int)path.PixelPosition.Y == testSzene.PixY))
                        {
                            pathContribution.AddEntry(path, path.PathContribution / sampler.SamplerMethod.SampleCountForGivenPath(path) / (sampleCount * frameCount));
                        }

                    }
                }
            }
            

            return pathContribution;
        }

        public static PathContributionForEachPathSpace GetMisWeightedPathContributionForEachPathSpace(FullPathSampler fullPathSampler, IFullPathTestData testSzene, int sampleCount, PhotonmapSettings photonSettings = null)
        {
            PathContributionForEachPathSpace pathContribution = new PathContributionForEachPathSpace();

            IRandom rand = new Rand(0);

            var frameData = PathContributionCalculator.CreateFrameDataWithPhotonmap(testSzene.LightPathSampler, testSzene.PhotonenCount, rand, testSzene.SizeFactor, photonSettings); //(float)Math.Sqrt(testSzene.SizeFactor) Wenn ich eine doppelt so große Suchfläche will, dann darf ich nicht Radius nicht verdoppeln sondenr ich muss Wurzel aus 2 nehmen, um doppelte Fläche zu erhalten

            //Vector3D colorSum = new Vector3D(0, 0, 0);
            //int colorCounter = 0;
            //int backgroundCounter = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                SubPath eyePath = testSzene.EyePathSampler.SamplePathFromCamera(testSzene.PixX, testSzene.PixY, rand);
                SubPath lightPath = testSzene.LightPathSampler.SamplePathFromLighsource(rand);
                //float kernelFunction = frameData != null ? frameData.PhotonMaps.GlobalSurfacePhotonmap.KernelFunction(0, frameData.PhotonMaps.GlobalSurfacePhotonmap.SearchRadius) : 1;

                var result = fullPathSampler.SampleFullPaths(eyePath, lightPath, frameData, rand);
                //if (result.MainPixelHitsBackground) backgroundCounter++; else colorCounter++;
                List<FullPath> fullPaths = new List<FullPath>();
                foreach (var mainPath in result.MainPaths)
                {
                    mainPath.PixelPosition = new Vector2D(testSzene.PixX, testSzene.PixY);
                    fullPaths.Add(mainPath);
                }
                fullPaths.AddRange(result.LighttracingPaths);

                foreach (var path in fullPaths)
                {
                    if (path.PixelPosition == null || (path.PixelPosition.X >= testSzene.PixX && path.PixelPosition.X <= testSzene.PixX + 1 && path.PixelPosition.Y >= testSzene.PixY && path.PixelPosition.Y <= testSzene.PixY + 1))
                    {
                        pathContribution.AddEntry(path, path.PathContribution * path.MisWeight / sampleCount);
                        //colorSum += path.Radiance;
                    }
                }
            }

            //colorSum /= sampleCount;

            return pathContribution;
        }

        public static double GetMisWeightedPixelRadiance(BoxTestScene testSzene, FullPathSampler fullPathSampler, int sampleCount)
        {
            double pathContribution = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                var radiance = fullPathSampler.SampleFullPaths(testSzene.EyePathSampler.SamplePathFromCamera(testSzene.PixX, testSzene.PixY, testSzene.rand), testSzene.LightPathSampler.SamplePathFromLighsource(testSzene.rand), null, testSzene.rand);

                if (radiance.MainPixelHitsBackground == false)
                {
                    pathContribution += radiance.RadianceFromRequestetPixel.X;                    
                }
                pathContribution += radiance.LighttracingPaths.Where(x => (int)x.PixelPosition.X == testSzene.PixX && (int)x.PixelPosition.Y == testSzene.PixY).Sum(x => x.Radiance.X);
            }

            pathContribution /= sampleCount;

            return pathContribution;
        }        

        public static FullPathSampler CreateFullPathSampler(BoxTestScene testSzene, FullPathSettings settings)
        {
            FullPathKonstruktorData fullPathKonstruktorData = new FullPathKonstruktorData()
            {
                EyePathSamplingType = testSzene.EyePathSampler.PathSamplingType,
                LightPathSamplingType = testSzene.LightPathSampler.PathSamplingType,
                PointToPointConnector = testSzene.PointToPointConnector,
                RayCamera = testSzene.Camera,
                LightSourceSampler = testSzene.LightSourceSampler,
                MaxPathLength = testSzene.MaxPathLength,
            };

            FullPathSampler sut = new FullPathSampler(fullPathKonstruktorData, settings);
            return sut;
        }

        public static FullPathFrameData CreateFrameDataWithPhotonmap(SubpathSampler lightPathSampler, int photonenCount, IRandom rand, float szeneSizeFactor, PhotonmapSettings settings, SubPathFloatingErrorCleaner subPathCleaner = null)
        {
            if (settings == null) return null;
            //string randString = "AAEAAAD/////AQAAAAAAAAAEAQAAAA1TeXN0ZW0uUmFuZG9tAwAAAAVpbmV4dAZpbmV4dHAJU2VlZEFycmF5AAAHCAgIIAAAADUAAAAJAgAAAA8CAAAAOAAAAAgAAAAAnDxsEU+VniWpvPwk3SNsFV937EdFGfdbUxuseKUzgnbtCo1xWyG7BtpQaXbBoSc2T7yTbBP+Jn7RNP1LtlX9SfWgPjH/4VRqKJ/7Vzy1gmvfqnhZ0E9Je41rcRD7MpU+kGM+RzzkYC4sabUVJmcVH2kUIgL6TMcwCzjWBIq1hRZMXnV7nLVfJjxwdE9cDWA9coTxI1ZvDlPHiO5lLM5kButPo3AzCXcDUbEYJlT11SY4aIp633y3KTEUDzbqpmoENga1PaWZiRCVcJUuymYqHnlQPl9vquABM7LsBgs=";
            //List<SubPath> lightPaths = new List<SubPath>() { this.LightPathSampler.SamplePathFromLighsource(ObjectToStringConverter.ConvertStringToObject<Random>(randString)) };

            List<SubPath> lightPaths = new List<SubPath>();
            for (int i = 0; i < photonenCount; i++)
            {
                string randomObjectBase64Coded = rand.ToBase64String();
                
                var lightPath = lightPathSampler.SamplePathFromLighsource(rand);
                if (subPathCleaner != null) subPathCleaner.RemoveFloatingPointErrors(lightPath, lightPathSampler.PathSamplingType);
                lightPaths.Add(lightPath);
            }

            //string allLightPahts = string.Join("\r\n", lightPaths.Select(x => x.ToPathSpaceString()));

            //this.SearchRadius = PhotonMapSearchRadiusCalculator.GetSearchRadiusForPhotonmapWithPhotonDensity(lightPaths, this.IntersectionFinder, this.Camera);

            return new FullPathFrameData()
            {
                PhotonMaps = new Photonmaps()
                {
                    GlobalSurfacePhotonmap = settings.CreateSurfaceMap ? new PhotonMap(lightPaths, photonenCount, (t, f) => { }, 1, int.MaxValue) { SearchRadius = 0.010f * szeneSizeFactor } : null,
                    PointDataPointQueryMap = settings.CreatePointDataPointQueryMap ? new PointDataPointQueryMap(lightPaths, photonenCount, (t, f) => { }) { SearchRadius = 0.030f * szeneSizeFactor } : null,
                    PointDataBeamQueryMap = settings.CreatePointDataBeamQueryMap ? new PointDataBeamQueryMap(lightPaths, photonenCount, (t, f) => { }, 0.030f * szeneSizeFactor) : null,
                    BeamDataLineQueryMap = settings.CreateBeamDataLineQueryMap ? new BeamDataLineQueryMap(lightPaths, photonenCount, 0.030f * szeneSizeFactor, settings.BeamDataLineQueryReductionFactor, rand) : null
                }
            };
        }
    }
}
