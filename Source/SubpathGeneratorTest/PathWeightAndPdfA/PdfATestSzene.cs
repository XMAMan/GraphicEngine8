using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia.Media;
using RayCameraNamespace;
using RayObjects;
using RayObjects.RayObjects;
using RaytracingBrdf;
using RaytracingBrdf.SampleAndRequest;
using RaytracingColorEstimator;
using RaytracingLightSource;
using SubpathGenerator;
using TriangleObjectGeneration;

namespace SubpathGeneratorTest
{
    //Eine Szene, die aus lauter Vierecken besteht, welche in der XY-Ebene betrachet so aussehen: < _ - _ -
    class PdfATestSzene
    {
        public SubpathSampler SubpathSampler;
        public int ScreenWidth = 1;
        public int ScreenHeight = 1;
        public int PixX = 0;
        public int PixY = 0;
        public IRandom rand = new Rand(0);
        public List<RayQuad> Quads;
        public RayCamera Camera;

        public int MaxPathLength = 6; //Es wird Kamerapunkt und Lightpunkt mitgezählt

        public bool SceneHasMedia = false;
        public float ScatteringFromMedia = 0.1f * 4;
        public float AbsorbationFromMedia = 0.05f * 4;
        public float ScatteringFromGlobalMedia = 0;

        public PdfATestSzene(PathSamplingType pathSamplingType, BoundingBox mediaBox)
        {
            int plattenCount = this.MaxPathLength - 1;
            float groundSize = 1;

            //Die Kamera schaut von oben auf die erste Platte  drauf und hat den Y-Abstand von 1
            float imagePlaneSize = groundSize * 2;
            float imagePlaneDistance = 1;
            float foV = (float)(Math.Atan(imagePlaneSize / 2 / imagePlaneDistance) / (2 * Math.PI) * 360) * 2;
            var rayCamera = new RayCamera(new CameraConstructorData() { Camera = new Camera(new Vector3D(0, imagePlaneDistance, 0), new Vector3D(0, -1, 0), new Vector3D(1, 0, 0), foV), ScreenWidth = this.ScreenWidth, ScreenHeight = this.ScreenHeight, PixelRange = new ImagePixelRange(0, 0, 1, 1), SamplingMode = PixelSamplingMode.Equal });
            List<DrawingObject> drawingObjects = new List<DrawingObject>();

            for (int i = 0; i < plattenCount; i++)
            {
                var triangleData = TriangleObjectGenerator.CreateSquareXY(groundSize, groundSize, 1);
                if (i % 2 == 0) triangleData = TriangleObjectGenerator.GetFlippedNormalsObjectFromOtherObject(triangleData);
                var ground = new DrawingObject(triangleData, new ObjectPropertys() { Position = new Vector3D(i * 2, i % 2, 0), Orientation = new Vector3D(90, 0, 0), RefractionIndex = 1, TextureFile = "#FF0000", BrdfModel = BrdfModel.Diffus, NormalInterpolation = InterpolationMode.Flat });
                drawingObjects.Add(ground);
            }

            drawingObjects.Last().DrawingProps.RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 1 };

            var rayObjects= new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(drawingObjects, true);
            this.Quads = rayObjects.Cast<RayQuad>().OrderBy(x => x.CenterOfGravity.X).ToList();

            if (mediaBox != null)
            {
                this.SceneHasMedia = true;
                var mediaCube = new DrawingObject(TriangleObjectGenerator.CreateCube(mediaBox.XSize, mediaBox.YSize, mediaBox.ZSize), new ObjectPropertys() { Position = mediaBox.Center, TextureFile = "#FF0000", Size = 0.7f, RefractionIndex = 1, NormalInterpolation = InterpolationMode.Flat, MediaDescription = new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * this.ScatteringFromMedia, AbsorbationCoeffizent = new Vector3D(1, 1, 1) * this.AbsorbationFromMedia } });
                //drawingObjects.Add(mediaCube);
                var mediaRayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { mediaCube }, true);
                rayObjects.AddRange(mediaRayObjects);
            }

            //var rayObjects = new RayObjectCreationHelper().CreatePlanarObjects(drawingObjects, true);

            var intersectionFinder = new IntersectionFinder(rayObjects.Cast<IIntersecableObject>().ToList(), null);

            var globalMedia = new ParticipatingMediaBuilder().CreateGlobalMedia(this.ScatteringFromGlobalMedia == 0 ? null : new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * this.ScatteringFromGlobalMedia }); //Luft
            var mediaIntersectionFinder = PixelRadianceCreationHelper.CreateMediaIntersectionFinder(rayObjects, null, globalMedia);

            var lightCreationData = new ConstruktorDataForLightSourceSampler()
            {
                IntersectionFinder = intersectionFinder,
                LightDrawingObjects = rayObjects.Where(x => x.RayHeigh.Propertys.RaytracingLightSource != null).ToList(),
                RayCamera = rayCamera,
                ProgressChangedHandler = (s, f) => { },
                StopTriggerForColorEstimatorCreation = new CancellationTokenSource()
            };

            var lightSourceSampler = new LightSourceSampler(lightCreationData);            

            this.SubpathSampler = new SubpathSampler(new SubpathSamplerConstruktorData()
            {
                RayCamera = rayCamera,
                LightSourceSampler = lightSourceSampler,
                IntersectionFinder = intersectionFinder,
                MediaIntersectionFinder = mediaIntersectionFinder,
                PathSamplingType = pathSamplingType,
                MaxPathLength = plattenCount + 1,
                BrdfSampler = new BrdfSampler(),
                PhaseFunction = new PhaseFunction()
            });

            this.Camera = rayCamera;
        }
    }
}
