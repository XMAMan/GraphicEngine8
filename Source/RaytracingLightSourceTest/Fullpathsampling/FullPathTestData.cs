using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using RayCameraNamespace;
using RayObjects;
using RaytracingBrdf;
using RaytracingLightSource;
using SubpathGenerator;
using TriangleObjectGeneration;
using ParticipatingMedia;
using RaytracingColorEstimator;
using IntersectionTests.Ray_3D_Object;
using ParticipatingMedia.Media;
using RayObjects.RayObjects;
using RaytracingBrdf.SampleAndRequest;

namespace RaytracingLightSourceTest.Fullpathsampling
{
    enum LightsourceType { Surface, Sphere, SphereWithSpot, SurfaceWithSpot, ImportanceSurface, ImportanceSurfaceWithSpot, DirectionFromInfinity, Environment, HdrMap, Motion }

    //Test der Lichtquellen unter Mitbenutzung des Sub- und Fullpath-Samplers
    //Aufbau der Szene: In XZ-Ebene liegt Ebene mit Kantenlänge 1
    class FullPathTestData
    {
        private static readonly string DataDirectory = UnitTestHelper.FilePaths.DataDirectory;

        private readonly float groundSize = 0.5f;
        private readonly float cameraToGroundDistance = 0.2f;
        private readonly float distanceBetweenLightSourceAndCamera = 0.01f;
        private readonly float emission;
        private readonly int maxPathLength = 3; //Nur direktes Licht (Camera-Fußboden/Partikel-Lichtquelle)
        //private readonly int photonenCount = 3000;

        public IFullPathSamplingMethod PathtracingSampler;
        public IFullPathSamplingMethod DirectLightingSampler;
        public IFullPathSamplingMethod DirectLightingOnEdgeSampler;
        public IFullPathSamplingMethod LightTracingSampler;
        
        public SubpathSampler EyePathSampler;
        public SubpathSampler LightPathSampler;

        public RayCamera Camera;
        public LightSourceSampler LightSourceSampler;
        public IntersectionFinder IntersectionFinder;

        public FullPathTestData(LightsourceType lightsourceType, int imagePixelSize, bool withMediaSphere, bool withGlobalMedia, PathSamplingType pathSamplingType, float emission = 1000)
        {
            this.emission = emission;
            var rayCamera = CreateCamera(imagePixelSize); //Kamera erstellen

            var rayObjects = CreateRayObjects(lightsourceType, withMediaSphere);
            this.IntersectionFinder = PixelRadianceCreationHelper.CreateIntersectionFinder(rayObjects, null);
            MediaIntersectionFinder mediaIntersectionFinder = pathSamplingType == PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling ? CreateMediaIntersectionFinder(rayObjects, withGlobalMedia) : null;

            this.LightSourceSampler = CreateLightSourceSampler(rayCamera, this.IntersectionFinder, mediaIntersectionFinder, rayObjects);
            

            var eyePathSampler = new SubpathSampler(new SubpathSamplerConstruktorData()
            {
                RayCamera = rayCamera,
                LightSourceSampler = this.LightSourceSampler,
                IntersectionFinder = this.IntersectionFinder,
                MediaIntersectionFinder = mediaIntersectionFinder,
                PathSamplingType = pathSamplingType,
                MaxPathLength = maxPathLength,
                BrdfSampler = new BrdfSampler(),
                PhaseFunction = new PhaseFunction()
            });
            var lightPathSampler = new SubpathSampler(new SubpathSamplerConstruktorData()
            {
                RayCamera = rayCamera,
                LightSourceSampler = this.LightSourceSampler,
                IntersectionFinder = this.IntersectionFinder,
                MediaIntersectionFinder = mediaIntersectionFinder,
                PathSamplingType = pathSamplingType,
                MaxPathLength = maxPathLength - 1,
                BrdfSampler = new BrdfSampler(),
                PhaseFunction = new PhaseFunction()
            });

            PointToPointConnector pointToPointConnector = new PointToPointConnector(new RayVisibleTester(this.IntersectionFinder, mediaIntersectionFinder), rayCamera, pathSamplingType);

            this.Camera = rayCamera;
            this.EyePathSampler = eyePathSampler;
            this.LightPathSampler = lightPathSampler;
            this.PathtracingSampler = new PathTracing(this.LightSourceSampler, pathSamplingType);  
            this.DirectLightingSampler = new DirectLighting(this.LightSourceSampler, maxPathLength - 2, pointToPointConnector, pathSamplingType);
            this.DirectLightingOnEdgeSampler = mediaIntersectionFinder != null ? new DirectLightingOnEdge(this.LightSourceSampler, pointToPointConnector, pathSamplingType, maxPathLength, true) : null;
            this.LightTracingSampler = new LightTracing(rayCamera, pointToPointConnector, pathSamplingType, false);
        }

        private MediaIntersectionFinder CreateMediaIntersectionFinder(List<IRayObject> rayObjects, bool withGlobalMedia)
        {
            IParticipatingMediaDescription mediaDescription = null;
            if (withGlobalMedia)
            {
                mediaDescription = new DescriptionForHomogeneousMedia()
                {
                    ScatteringCoeffizent = new Vector3D(1, 1, 1) * 0.2f * 5,
                    AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 0.1f * 5,
                    AnisotropyCoeffizient = 0.0f
                };
            }
            var globalMedia = new ParticipatingMediaBuilder().CreateGlobalMedia(mediaDescription);
            
            return PixelRadianceCreationHelper.CreateMediaIntersectionFinder(rayObjects, null, globalMedia);
        }

        private RayCamera CreateCamera(int imagePixelSize)
        {
            float imagePlaneSize = groundSize * 2;
            float foV = (float)(Math.Atan(imagePlaneSize / 2 / cameraToGroundDistance) / (2 * Math.PI) * 360) * 2;
            return new RayCamera(new CameraConstructorData() { Camera = new Camera(new Vector3D(0, cameraToGroundDistance, 0), new Vector3D(0, -1, 0), new Vector3D(1, 0, 0), foV), ScreenWidth = imagePixelSize, ScreenHeight = imagePixelSize, PixelRange = new ImagePixelRange(imagePixelSize / 2, imagePixelSize / 2, 1, 1), SamplingMode = PixelSamplingMode.Equal });
        }

        

        private List<IRayObject> CreateRayObjects(LightsourceType lightsourceType, bool withMediaSphere)
        {
            List<DrawingObject> drawingObjects = new List<DrawingObject>
            {
                CreateGround(), //Fußboden erstellen
                CreateLightObject(lightsourceType)//Lichtquelle erstellen
            };
            if (withMediaSphere) drawingObjects.Add(CreateAtmosphaereSphere()); //Atmosphärenkugel erstellen

            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(drawingObjects);
            return rayObjects;
        }

        private DrawingObject CreateAtmosphaereSphere()
        {
            float sphereRadius = this.groundSize * 3;
            return new DrawingObject(TriangleObjectGenerator.CreateSphere(sphereRadius, 10, 10), new ObjectPropertys()
            {
                Position = new Vector3D(0, 0, 0),
                TextureFile = "#FFFFFF",
                RefractionIndex = 1,
                MediaDescription = new DescriptionForHomogeneousMedia()
                {
                    ScatteringCoeffizent = new Vector3D(1, 1, 1) * 0.2f,
                    AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 0.1f,
                    AnisotropyCoeffizient = 0.0f
                }
            });
        }

        private DrawingObject CreateGround()
        {
            return new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSize, groundSize, 1), new ObjectPropertys() { Position = new Vector3D(0, 0, 0), Orientation = new Vector3D(90, 0, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, Albedo = 0.8f });
        }

        private DrawingObject CreateLightObject(LightsourceType lightsourceType)
        {
            float lightYPos = float.NaN;
            TriangleObject triangleLightsourceData = null;
            IRaytracingLightSource lightSourceDescription = null;
            MotionBlurMovementDescription motionDesc = null;
            string textureFile = "#FFFFFF";
            switch (lightsourceType)
            {
                case LightsourceType.Surface:
                case LightsourceType.SurfaceWithSpot:
                case LightsourceType.ImportanceSurface:
                case LightsourceType.ImportanceSurfaceWithSpot:
                    triangleLightsourceData = TriangleObjectGenerator.CreateSquareXY(groundSize, groundSize, 1);
                    if (lightsourceType == LightsourceType.Surface) lightSourceDescription = new DiffuseSurfaceLightDescription() { Emission = emission };
                    if (lightsourceType == LightsourceType.SurfaceWithSpot) lightSourceDescription = new SurfaceWithSpotLightDescription() { Emission = emission, SpotDirection = new Vector3D(0, -1, 0), SpotMix = 0.9f, SpotCutoff = 10 };
                    if (lightsourceType == LightsourceType.ImportanceSurface) lightSourceDescription = new ImportanceSurfaceLightDescription() { Emission = emission, CellSurfaceCount = 30, CellDirectionCount = 15, ImportanceSamplingMode = LightImportanceSamplingMode.IsVisibleFromCamera };
                    if (lightsourceType == LightsourceType.ImportanceSurfaceWithSpot) lightSourceDescription = new ImportanceSurfaceWithSpotLightDescription() { Emission = emission, CellSurfaceCount = 50, SpotDirection = new Vector3D(0, -1, 0), SpotMix = 1.0f, SpotCutoff = 10, ImportanceSamplingMode = LightImportanceSamplingMode.IsVisibleFromCamera };
                    lightYPos = cameraToGroundDistance + distanceBetweenLightSourceAndCamera;
                    break;
                case LightsourceType.Sphere:
                case LightsourceType.SphereWithSpot:
                    float sphereRadius = 0.5f;
                    triangleLightsourceData = TriangleObjectGenerator.CreateSphere(sphereRadius, 10, 10);
                    if (lightsourceType == LightsourceType.Sphere) lightSourceDescription = new DiffuseSphereLightDescription() { Emission = emission };
                    if (lightsourceType == LightsourceType.SphereWithSpot) lightSourceDescription = new SphereWithSpotLightDescription() { Emission = emission, SpotDirection = new Vector3D(0, -1, 0), SpotCutoff = 20 };
                    lightYPos = cameraToGroundDistance + distanceBetweenLightSourceAndCamera + sphereRadius;
                    break;
                case LightsourceType.DirectionFromInfinity:
                    triangleLightsourceData = TriangleObjectGenerator.CreateSquareXY(groundSize, groundSize, 1);
                    lightSourceDescription = new FarAwayDirectionLightDescription() { Emission = emission };
                    lightYPos = -1; //Y-Position ist egal
                    break;
                case LightsourceType.Environment:
                    triangleLightsourceData = TriangleObjectGenerator.CreateSphere(1, 10, 10); //Objekt ist egal
                    lightSourceDescription = new EnvironmentLightDescription() { Emission = emission };
                    lightYPos = -1; //Y-Position ist egal
                    break;
                case LightsourceType.HdrMap:
                    triangleLightsourceData = TriangleObjectGenerator.CreateSphere(1, 10, 10); //Objekt ist egal
                    lightSourceDescription = new EnvironmentLightDescription() { Emission = emission };
                    lightYPos = -1; //Y-Position ist egal
                    textureFile = DataDirectory + "room.hdr";
                    break;
                case LightsourceType.Motion:
                    triangleLightsourceData = TriangleObjectGenerator.CreateSquareXY(groundSize, groundSize, 10);
                    lightYPos = cameraToGroundDistance + distanceBetweenLightSourceAndCamera;
                    lightSourceDescription = new DiffuseSurfaceLightDescription() { Emission = emission };
                    motionDesc = new TranslationMovementEulerDescription() { Factor = 5, PositionStart = new Vector3D(0, lightYPos, 0), PositionEnd = new Vector3D(0, lightYPos, 0) };
                    break;
                    
            }


            var lightSource = new DrawingObject(triangleLightsourceData, new ObjectPropertys()
            {
                Position = new Vector3D(0, lightYPos, 0),
                Orientation = new Vector3D(90, 0, 0),
                Size = 0.5f,
                TextureFile = textureFile,
                BrdfModel = BrdfModel.Diffus,
                NormalInterpolation = InterpolationMode.Flat,
                RaytracingLightSource = lightSourceDescription,
                MotionBlurMovment = motionDesc,
                //CreateQuads = lightsourceType == LightsourceType.ImportanceSurfaceWithSpot
            }); ;

            return lightSource;
        }

        private LightSourceSampler CreateLightSourceSampler(RayCamera rayCamera, IntersectionFinder intersectionFinder, MediaIntersectionFinder mediaIntersectionFinder, List<IRayObject> rayObjects)
        {
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
            return lightSourceSampler;
        }
    }
}
