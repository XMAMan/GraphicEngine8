using FullPathGenerator;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using GraphicGlobal;
using GraphicMinimal;
using ParticipatingMedia;
using ParticipatingMedia.Media;
using RayCameraNamespace;
using RayObjects;
using RaytracingBrdf;
using RaytracingBrdf.SampleAndRequest;
using RaytracingColorEstimator;
using RaytracingLightSource;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TriangleObjectGeneration;

namespace FullPathGeneratorTest
{
    class SkyTestSzene : IFullPathTestData
    {
        public SubpathSampler EyePathSampler { get; private set; }
        public SubpathSampler LightPathSampler { get; private set; }
        public int PixX { get; private set; } = 210;
        public int PixY { get; private set; } = 50;
        public int PhotonenCount { get; private set; } = 10000;
        public float SizeFactor { get; private set; } = 1;

        public FullPathKonstruktorData FullPathKonstruktorData;
        public int ScreenWidth = 420;
        public int ScreenHeight = 328;
        public int MaxPathLength = 7;        

        private const float EarthRadius = 6360000;
        private const float AtmosphereRadius = 6420000;

        public SkyTestSzene(PathSamplingType pathSamplingType)
        {
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(CreateDrawingObjects());

            var intersectionFinder = PixelRadianceCreationHelper.CreateIntersectionFinder(rayObjects, null);
            var globalMedia = new ParticipatingMediaBuilder().CreateGlobalMedia(null); //Weltraum
            var mediaIntersectionFinder = PixelRadianceCreationHelper.CreateMediaIntersectionFinder(rayObjects, null, globalMedia);

            var rayCamera = CreateCamera();

            var lightCreationData = new ConstruktorDataForLightSourceSampler()
            {
                IntersectionFinder = intersectionFinder,
                MediaIntersectionFinder = mediaIntersectionFinder,
                LightDrawingObjects = rayObjects.Where(x => x.RayHeigh.Propertys.RaytracingLightSource != null).ToList(),
                RayCamera = rayCamera,
                ProgressChangedHandler = (s, f) => { },
                StopTriggerForColorEstimatorCreation = new CancellationTokenSource()
            };
            var lightSourceSampler = new LightSourceSampler(lightCreationData);

            var eyePathSampler = new SubpathSampler(new SubpathSamplerConstruktorData()
            {
                RayCamera = rayCamera,
                LightSourceSampler = lightSourceSampler,
                IntersectionFinder = intersectionFinder,
                MediaIntersectionFinder = mediaIntersectionFinder,
                PathSamplingType = pathSamplingType,
                MaxPathLength = MaxPathLength,
                BrdfSampler = new BrdfSampler(),
                PhaseFunction = new PhaseFunction()
            });
            var lightPathSampler = new SubpathSampler(new SubpathSamplerConstruktorData()
            {
                RayCamera = rayCamera,
                LightSourceSampler = lightSourceSampler,
                IntersectionFinder = intersectionFinder,
                MediaIntersectionFinder = mediaIntersectionFinder,
                PathSamplingType = pathSamplingType,
                MaxPathLength = MaxPathLength - 1,
                BrdfSampler = new BrdfSampler(),
                PhaseFunction = new PhaseFunction()
            });

            FullPathKonstruktorData fullPathKonstruktorData = new FullPathKonstruktorData()
            {
                EyePathSamplingType = eyePathSampler.PathSamplingType,
                LightPathSamplingType = lightPathSampler.PathSamplingType,
                PointToPointConnector = new PointToPointConnector(new RayVisibleTester(intersectionFinder, mediaIntersectionFinder), rayCamera, eyePathSampler.PathSamplingType),
                RayCamera = rayCamera,
                LightSourceSampler = lightSourceSampler,
                MaxPathLength = MaxPathLength,
            };

            this.LightPathSampler = lightPathSampler;
            this.EyePathSampler = eyePathSampler;
            this.FullPathKonstruktorData = fullPathKonstruktorData;
        }

        private List<DrawingObject> CreateDrawingObjects()
        {
            List<DrawingObject> drawingObjects = new List<DrawingObject>
            {
                new DrawingObject(TriangleObjectGenerator.CreateSphere(EarthRadius, 10, 10), new ObjectPropertys() { TextureFile = "#00FF00" }),
                new DrawingObject(TriangleObjectGenerator.CreateSphere(AtmosphereRadius, 10, 10), new ObjectPropertys() { TextureFile = "#FFFFFF", RefractionIndex = 1, MediaDescription = new DescriptionForSkyMedia(), ShowFromTwoSides = true }),
                new DrawingObject(TriangleObjectGenerator.CreateSquareXY(1, 1, 1), new ObjectPropertys() { Orientation = new Vector3D(90, -45, 0), RaytracingLightSource = new FarAwayDirectionLightDescription() { Emission = 20 } })
            };

            return drawingObjects;
        }

        private RayCamera CreateCamera()
        {
            return new RayCamera(new CameraConstructorData() { Camera = new Camera(new Vector3D(0, EarthRadius + 100 / 1f, 0), Vector3D.Normalize(new Vector3D(1, 0.5f, 0)), 90.0f), ScreenWidth = this.ScreenWidth, ScreenHeight = this.ScreenHeight, PixelRange = new ImagePixelRange(0, 0, ScreenWidth, ScreenHeight), SamplingMode = PixelSamplingMode.Tent });
        }
    }
}
