using System;
using RayTracerGlobal;
using RaytracerMain;
using RaytracingColorEstimator;
using FullPathGenerator;
using GraphicGlobal;
using SubpathGenerator;
using Photonusmap;

namespace RaytracingMethods
{
    public class MediaBidirectionalPathTracing : IPixelEstimator
    {
        private PixelRadianceCalculator pixelRadianceCalculator;

        public bool CreatesLigthPaths { get; } = false;

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.pixelRadianceCalculator = PixelRadianceCreationHelper.CreatePixelRadianceCalculator(data,
                new SubPathSettings()
                {
                    EyePathType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                    LightPathType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling
                },
                new FullPathSettings()
                {
                    //WithoutMis = true,
                    UsePathTracing = true,
                    UseDirectLighting = true,
                      //UseMultipleDirectLighting = true, 
                    UseVertexConnection = true,
                });
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            return this.pixelRadianceCalculator.SampleSingleEyeAndLightPath(x, y, rand);
        }
    }
}
