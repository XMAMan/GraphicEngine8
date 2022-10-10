using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using ParticipatingMediaTest.MediaMocks;
using RayCameraNamespace;
using RayObjects.RayObjects;
using RaytracingBrdf.SampleAndRequest;
using RaytracingLightSource;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SubpathGeneratorTest.PathPointPropertys
{
    static class SubpathTestHelper
    {
        private static readonly int maxPathLength = 15;

        public static void CheckSegment(VolumeSegment segment, float rayMin, float rayMax, float expectedScatteringCoeffizient)
        {
            //Assert.AreEqual(rayMin, segment.RayMin);
            //Assert.AreEqual(rayMax, segment.RayMax);
            Assert.IsTrue(Math.Abs(rayMin - segment.RayMin) < 0.00001f);
            Assert.IsTrue(Math.Abs(rayMax - segment.RayMax) < 0.00001f);
            Assert.AreEqual(expectedScatteringCoeffizient, segment.Media.GetScatteringCoeffizient(null).X);
            Assert.AreEqual(expectedScatteringCoeffizient, segment.PoinOnRayMin.CurrentMedium.GetScatteringCoeffizient(null).X);
        }

        public static void CheckCameraNoMediaPoint(PathPoint point, Vector3D expectedPosition)
        {
            Assert.AreEqual(MediaPointLocationType.Camera, point.LocationType);
            Assert.AreEqual(expectedPosition, point.Position);
            Assert.IsNull(point.MediaPoint);
            Assert.AreEqual(0, point.Index);
            //Assert.IsFalse(point.MediaPoint.CurrentMedium.HasScatteringSomeWhereInMedium());
        }

        public static void CheckCameraInMediaPoint(PathPoint point, Vector3D expectedPosition, float expectedScatteringCoeffizient)
        {
            Assert.AreEqual(MediaPointLocationType.Camera, point.LocationType);
            Assert.AreEqual(expectedPosition, point.Position);
            Assert.IsNotNull(point.MediaPoint);
            Assert.AreEqual(expectedScatteringCoeffizient, point.SurrondingMedia.GetScatteringCoeffizient(expectedPosition).X);
            Assert.AreEqual(0, point.Index);
            if (expectedScatteringCoeffizient > 0) Assert.IsTrue(point.MediaPoint.CurrentMedium.HasScatteringSomeWhereInMedium());
        }

        public static void CheckSurfacePointWithoutMedia(PathPoint point, Vector3D expectedPosition, Vector3D expectedNormal, int expectedIndex)
        {
            Assert.AreEqual(MediaPointLocationType.Surface, point.LocationType);
            Assert.AreEqual(expectedPosition, point.Position);
            Assert.IsNull(point.MediaPoint);
            Assert.AreEqual(expectedPosition, point.SurfacePoint.Position);
            Assert.AreEqual(expectedNormal, point.Normal);
            Assert.IsFalse(point.IsLocatedOnLightSource);
            Assert.IsTrue(point.IsDiffusePoint);
            Assert.AreEqual(expectedIndex, point.Index);
            Assert.IsNotNull(point.IntersectedObject);
            Assert.IsFalse(point.IsSpecularSurfacePoint);
        }
        public static void CheckSpecurlarPointWithoutMedia(PathPoint point, Vector3D expectedPosition, Vector3D expectedNormal, int expectedIndex)
        {
            Assert.AreEqual(MediaPointLocationType.Surface, point.LocationType);
            Assert.AreEqual(expectedPosition, point.Position);
            Assert.IsNull(point.MediaPoint);
            Assert.AreEqual(expectedPosition, point.SurfacePoint.Position);
            Assert.AreEqual(expectedNormal, point.Normal);
            Assert.IsFalse(point.IsLocatedOnLightSource);
            Assert.IsFalse(point.IsDiffusePoint);
            Assert.AreEqual(expectedIndex, point.Index);
            Assert.IsNotNull(point.IntersectedObject);
            Assert.IsTrue(point.IsSpecularSurfacePoint);
        }


        public static void CheckSurfacePointWithMedia(PathPoint point, Vector3D expectedPosition, Vector3D expectedNormal, float expectedScatteringCoeffizient, int expectedIndex)
        {
            Assert.AreEqual(MediaPointLocationType.Surface, point.LocationType);
            Assert.AreEqual(expectedPosition, point.Position);
            Assert.AreEqual(expectedPosition, point.SurfacePoint.Position);
            Assert.AreEqual(expectedPosition, point.MediaPoint.Position);
            Assert.AreEqual(expectedScatteringCoeffizient, point.SurrondingMedia.GetScatteringCoeffizient(expectedPosition).X);
            Assert.AreEqual(expectedNormal, point.Normal);            
            Assert.IsTrue(point.IsDiffusePoint);
            Assert.AreEqual(expectedIndex, point.Index);
            Assert.IsNotNull(point.IntersectedObject);
            Assert.IsFalse(point.IsSpecularSurfacePoint);

            Assert.IsFalse(point.IsLocatedOnLightSource);
        }

        public static void CheckEnvironmentLightPoint(PathPoint point, int expectedIndex)
        {
            Assert.IsTrue(point.IsLocatedOnInfinityAwayLightSource);
            Assert.IsTrue(point.IsLocatedOnLightSource);
            Assert.AreEqual(expectedIndex, point.Index);
        }

        public static void CheckSpecularPointWithMedia(PathPoint point, Vector3D expectedPosition, Vector3D expectedNormal, float expectedScatteringCoeffizient, int expectedIndex)
        {
            Assert.AreEqual(MediaPointLocationType.Surface, point.LocationType);
            Assert.AreEqual(expectedPosition, point.Position);
            Assert.AreEqual(expectedPosition, point.SurfacePoint.Position);
            Assert.AreEqual(expectedPosition, point.MediaPoint.Position);
            Assert.AreEqual(expectedScatteringCoeffizient, point.SurrondingMedia.GetScatteringCoeffizient(expectedPosition).X);
            Assert.AreEqual(expectedNormal, point.Normal);
            Assert.IsFalse(point.IsDiffusePoint);
            Assert.AreEqual(expectedIndex, point.Index);
            Assert.IsNotNull(point.IntersectedObject);
            Assert.IsTrue(point.IsSpecularSurfacePoint);

            Assert.IsFalse(point.IsLocatedOnLightSource);
        }

        public static void CheckAirBorderPoint(PathPoint point, Vector3D expectedPosition, Vector3D expectedNormal, int expectedIndex)
        {
            Assert.AreEqual(MediaPointLocationType.NullMediaBorder, point.LocationType);
            //Assert.AreEqual(expectedPosition, point.Position);
            //Assert.AreEqual(expectedPosition, point.SurfacePoint.Position);
            //Assert.AreEqual(expectedPosition, point.MediaPoint.Position);
            Assert.IsTrue((expectedPosition - point.Position).Length() < 0.0001f);
            Assert.IsTrue((expectedPosition - point.SurfacePoint.Position).Length() < 0.0001f);
            Assert.IsTrue((expectedPosition - point.MediaPoint.Position).Length() < 0.0001f);

            Assert.AreEqual(1, point.SurfacePoint.RefractionIndex);
            //Assert.AreEqual(expectedNormal, point.Normal);
            Assert.IsTrue((expectedNormal - point.Normal).Length() < 0.0001f);

            Assert.IsFalse(point.IsDiffusePoint);
            Assert.AreEqual(expectedIndex, point.Index);
            Assert.IsNotNull(point.IntersectedObject);
            Assert.IsFalse(point.IsSpecularSurfacePoint);

            Assert.IsFalse(point.IsLocatedOnLightSource);
        }

        public static void CheckSpecularBorderPoint(PathPoint point, Vector3D expectedPosition, Vector3D expectedNormal, float expectedRefractionIndex, int expectedIndex)
        {
            Assert.AreEqual(MediaPointLocationType.MediaBorder, point.LocationType);
            //Assert.AreEqual(expectedPosition, point.Position);
            //Assert.AreEqual(expectedPosition, point.SurfacePoint.Position);
            //Assert.AreEqual(expectedPosition, point.MediaPoint.Position);
            Assert.IsTrue((expectedPosition - point.Position).Length() < 0.0001f);
            Assert.IsTrue((expectedPosition - point.SurfacePoint.Position).Length() < 0.0001f);
            Assert.IsTrue((expectedPosition - point.MediaPoint.Position).Length() < 0.0001f);

            Assert.AreEqual(expectedRefractionIndex, point.SurfacePoint.RefractionIndex);
            //Assert.AreEqual(expectedNormal, point.Normal);
            Assert.IsTrue((expectedNormal - point.Normal).Length() < 0.0001f);

            Assert.IsFalse(point.IsDiffusePoint);
            Assert.AreEqual(expectedIndex, point.Index);
            Assert.IsNotNull(point.IntersectedObject);
            Assert.IsTrue(point.IsSpecularSurfacePoint);

            Assert.IsFalse(point.IsLocatedOnLightSource);
        }

        public static void CheckDiffuseLightSourcePointWithmedia(PathPoint point, Vector3D expectedPosition, Vector3D expectedNormal, float expectedScatteringCoeffizient, int expectedIndex)
        {
            Assert.AreEqual(MediaPointLocationType.Surface, point.LocationType);
            Assert.AreEqual(expectedPosition, point.Position);
            Assert.AreEqual(expectedPosition, point.SurfacePoint.Position);
            Assert.AreEqual(expectedPosition, point.MediaPoint.Position);
            Assert.AreEqual(expectedScatteringCoeffizient, point.SurrondingMedia.GetScatteringCoeffizient(expectedPosition).X);
            Assert.AreEqual(expectedNormal, point.Normal);            
            Assert.IsTrue(point.IsDiffusePoint);
            Assert.AreEqual(expectedIndex, point.Index);
            Assert.IsNotNull(point.IntersectedObject);
            Assert.IsFalse(point.IsSpecularSurfacePoint);

            Assert.IsTrue(point.IsLocatedOnLightSource);
        }

        public static void CheckMediaInfinityPoint(PathPoint point, float expectedScatteringCoeffizient, int expectedIndex)
        {
            Assert.AreEqual(MediaPointLocationType.MediaInfinity, point.LocationType);
            Assert.IsTrue(point.Position.Length() > 10000);
            Assert.IsNull(point.SurfacePoint);
            Assert.IsTrue(point.MediaPoint.Position.Length() > 10000);
            Assert.IsNull(point.Normal);
            Assert.IsFalse(point.IsLocatedOnLightSource);
            Assert.IsFalse(point.IsDiffusePoint);
            Assert.AreEqual(expectedIndex, point.Index);
            Assert.IsNotNull(point.SurrondingMedia);
            Assert.IsFalse(point.IsSpecularSurfacePoint);
            Assert.AreEqual(expectedScatteringCoeffizient, point.SurrondingMedia.GetScatteringCoeffizient(point.Position).X);
        }

        public static void CheckMediaParticlePoint(PathPoint point, Vector3D expectedPosition, float expectedScatteringCoeffizient, int expectedIndex)
        {
            Assert.AreEqual(MediaPointLocationType.MediaParticle, point.LocationType);
            Assert.AreEqual(expectedPosition, point.Position);
            Assert.IsNull(point.SurfacePoint);
            Assert.AreEqual(expectedPosition, point.MediaPoint.Position);
            Assert.IsNull(point.Normal);
            Assert.IsFalse(point.IsLocatedOnLightSource);
            Assert.IsTrue(point.IsDiffusePoint);
            Assert.AreEqual(expectedIndex, point.Index);
            Assert.IsNotNull(point.SurrondingMedia);
            Assert.IsFalse(point.IsSpecularSurfacePoint);
            Assert.AreEqual(expectedScatteringCoeffizient, point.SurrondingMedia.GetScatteringCoeffizient(point.Position).X);
        }

        public static SubpathSampler CreateSubPathSampler(Vector3D startPoint, IntersectionFinderData intersectionData, SubpathGenerator.PathSamplingType pathSamplingType, List<BrdfMockData> surfaceBrdfMockData, DirectionSamplingMockData phaseDirectionData)
        {
            var camera = new RayCamera(new CameraConstructorData() { Camera = new Camera(startPoint, new Vector3D(1, 0, 0), new Vector3D(0, 1, 0), 45), ScreenWidth = 3, ScreenHeight = 3, PixelRange = new ImagePixelRange(0, 0, 3, 3), SamplingMode = PixelSamplingMode.None });
            var lightSourceData = new ConstruktorDataForLightSourceSampler()
            {
                LightDrawingObjects = intersectionData.RayObjects.Where(x => x.RayHeigh.Propertys.RaytracingLightSource != null).Cast<IRayObject>().ToList(),
                IntersectionFinder = intersectionData.NoMediaIntersectionFinder,
                MediaIntersectionFinder = intersectionData.MediaIntersectionFinder,
                RayCamera = camera,
                ProgressChangedHandler = (text, zahl) => { },
                StopTriggerForColorEstimatorCreation = null,
            };
            //return new MediaSubPathPointsSampler(intersectionData.MediaIntersectionFinder, lightSourceData.LightDrawingObjects.Any() ? new LightSourceSampler(lightSourceData) : null, maxPathLength, pathSamplingType, new SurfaceBrdfSamplerFactoryMock(surfaceBrdfMockData).CreateBrdfSampler(BrdfSamplerType.Standard), new PhaseFunctionMock(phaseDirectionData), camera);

            return new SubpathSampler(new SubpathSamplerConstruktorData()
            {
                RayCamera = new RayCamera(new CameraConstructorData() { Camera = new Camera(startPoint, new Vector3D(1, 0, 0), new Vector3D(0, 1, 0), 45), ScreenWidth = 3, ScreenHeight = 3, PixelRange = new ImagePixelRange(0, 0, 3, 3), SamplingMode = PixelSamplingMode.None }),
                LightSourceSampler = new LightSourceSampler(lightSourceData),
                IntersectionFinder = intersectionData.NoMediaIntersectionFinder,
                MediaIntersectionFinder = pathSamplingType == PathSamplingType.NoMedia ? null : intersectionData.MediaIntersectionFinder,
                PathSamplingType = pathSamplingType,
                MaxPathLength = maxPathLength,
                BrdfSampler = surfaceBrdfMockData == null ? new BrdfSampler() : (IBrdfSampler)new DirectionSamplerMocks(surfaceBrdfMockData),
                PhaseFunction = new PhaseFunctionMock(phaseDirectionData)
            });
        }
    }
}
