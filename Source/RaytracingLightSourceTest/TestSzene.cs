using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using RayCameraNamespace;
using RayObjects;
using RaytracingLightSource;
using RaytracingLightSourceTest.Fullpathsampling;
using TriangleObjectGeneration;

namespace RaytracingLightSourceTest
{
    //Erzeugt ein Viereck, wo in der Mitte darüber die Lichtquelle ist
    class TestSzene
    {
        public RayCamera RayCamera;
        public LightSourceSampler LightSourceSampler;
        public int ImageSize = 1;
        public IntersectionFinder IntersectionFinder;
        public int PhotonCount = 1000;
        public float SearchRadius = 0.193769842f;
        public IRandom rand = new Rand(0);

        public TestSzene(LightsourceType lightsourceType, int imageSize)
        {
            this.ImageSize = imageSize;
            float groundSize = 1;
 
            float imagePlaneSize = groundSize * 2;
            float imagePlaneDistance = 0.4f;
            float foV = (float)(Math.Atan(imagePlaneSize / 2 / imagePlaneDistance) / (2 * Math.PI) * 360) * 2;
            this.RayCamera = new RayCamera(new CameraConstructorData() { Camera = new Camera(new Vector3D(0, imagePlaneDistance, 0), new Vector3D(0, -1, 0), new Vector3D(1, 0, 0), foV), ScreenWidth = imageSize, ScreenHeight = imageSize, PixelRange = new ImagePixelRange(0, 0, imageSize, imageSize), SamplingMode = PixelSamplingMode.Equal });

            var ground = new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSize, groundSize, 1), new ObjectPropertys() { Position = new Vector3D(0, 0, 0), Orientation = new Vector3D(90, 0, 0), TextureFile = "#AA0000", BrdfModel = BrdfModel.Diffus, NormalInterpolation = InterpolationMode.Flat });

            float lightPos = float.NaN;
            TriangleObject triangleLightsourceData = null;
            IRaytracingLightSource lightSourceDescription = null;
            switch (lightsourceType)
            {
                case LightsourceType.Surface:
                case LightsourceType.SurfaceWithSpot:
                case LightsourceType.ImportanceSurface:
                    triangleLightsourceData = TriangleObjectGenerator.CreateSquareXY(1, 1, 1);
                    if (lightsourceType == LightsourceType.Surface) lightSourceDescription = new DiffuseSurfaceLightDescription() { Emission = 10 };
                    if (lightsourceType == LightsourceType.SurfaceWithSpot) lightSourceDescription = new SurfaceWithSpotLightDescription() { Emission = 10, SpotDirection = new Vector3D(0, -1, 0), SpotMix = 0.9f, SpotCutoff = 20 };
                    if (lightsourceType == LightsourceType.ImportanceSurface) lightSourceDescription = new ImportanceSurfaceLightDescription() { Emission = 10, ImportanceSamplingMode = LightImportanceSamplingMode.IsVisibleFromCamera };
                    lightPos = 0.5f;
                    break;
                case LightsourceType.Sphere:
                case LightsourceType.SphereWithSpot:
                    triangleLightsourceData = TriangleObjectGenerator.CreateSphere(0.5f, 10, 10);
                    if (lightsourceType == LightsourceType.Sphere) lightSourceDescription = new DiffuseSphereLightDescription() { Emission = 10 };
                    if (lightsourceType == LightsourceType.SphereWithSpot) lightSourceDescription = new SphereWithSpotLightDescription() { Emission = 10, SpotDirection = new Vector3D(0, -1, 0), SpotCutoff = 20 };
                    lightPos = 1;
                    break;
            }

            var lightSource = new DrawingObject(triangleLightsourceData, new ObjectPropertys()
            {
                Position = new Vector3D(0, lightPos, 0),
                Orientation = new Vector3D(90, 0, 0),
                Size = 0.5f,
                TextureFile = "#FFFFFF",
                BrdfModel = BrdfModel.Diffus,
                NormalInterpolation = InterpolationMode.Flat,
                RaytracingLightSource = lightSourceDescription,
            });
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { ground, lightSource });
            this.IntersectionFinder = new IntersectionFinder(rayObjects.Cast<IIntersecableObject>().ToList(), null);

            var lightCreationData = new ConstruktorDataForLightSourceSampler()
            {
                IntersectionFinder = this.IntersectionFinder,
                LightDrawingObjects = rayObjects.Where(x => x.RayHeigh.Propertys.RaytracingLightSource != null).ToList(),
                RayCamera = this.RayCamera,
                ProgressChangedHandler = (s, f) => { },
                StopTriggerForColorEstimatorCreation = new CancellationTokenSource()
            };

            this.LightSourceSampler = new LightSourceSampler(lightCreationData);
        }
    }
}
