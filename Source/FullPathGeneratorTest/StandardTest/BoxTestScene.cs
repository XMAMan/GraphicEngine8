using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using RayCameraNamespace;
using RayObjects;
using RayObjects.RayObjects;
using RaytracingBrdf;
using RaytracingLightSource;
using SubpathGenerator;
using TriangleObjectGeneration;
using RaytracingColorEstimator;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using ParticipatingMedia.Media;
using RaytracingBrdf.SampleAndRequest;

namespace FullPathGeneratorTest
{
    class BoxData
    {
        public int ScreenWidth = 3;
        public int ScreenHeight = 3;
        public int PixX = 1;
        public int PixY = 1;

        public PathSamplingType EyePathSamplingType = PathSamplingType.None;
        public PathSamplingType LightPathSamplingType = PathSamplingType.None;        
        public PixelSamplingMode PixelMode = PixelSamplingMode.Equal;
        public int PhotonenCount = 10000;

        public int MaxPathLength = 7;

        public bool CreateWalls = true;
        public BrdfModel WaendeMaterial = BrdfModel.Diffus;

        public bool CreateMediaBox = false;
        public float ScatteringFromMedia = 0.1f * 2;
        public float AbsorbationFromMedia = 0.025f * 2;
        public float AnisotrophyCoeffizient = 0.0f;

        public float ScatteringFromGlobalMedia = 0;

        public float SurfaceAlbedo = 0.8f;
    }

    //Cornellboxszene mit Luft an den Kanten (Die gesamt Szene muss aus axialen Quads bestehen wegen dem PdfA-Test)
    class BoxTestScene : IFullPathTestData
    {
        public PathSamplingType EyePathSamplingType;
        public PathSamplingType LightPathSamplingType;
        public SubpathSampler EyePathSampler { get; private set; }
        public SubpathSampler LightPathSampler { get; private set; }
        public int ScreenWidth = 3;
        public int ScreenHeight = 3;
        public int PixX { get; private set; } = 1;
        public int PixY { get; private set; } = 1;
        public IRandom rand = new Rand(0);
        public List<RayQuad> Quads;
        public RayCamera Camera;
        public LightSourceSampler LightSourceSampler;
        public IntersectionFinder IntersectionFinder;
        public MediaIntersectionFinder MediaIntersectionFinder;
        public PointToPointConnector PointToPointConnector;
        public float EmissionPerArea;
        public float LightSourceArea;
        public int MaxPathLength = 7;// * 3; //Bei Glas-Media-Objekt braucht man längere Pfadlängen
        public int PhotonenCount { get; private set; } = 10000;

        public float SizeFactor { get; private set; } = 10;

        public BoundingBox MediaBox;
        public bool SceneHasMedia;
        public float ScatteringFromMedia = float.NaN;
        public float AbsorbationFromMedia = float.NaN;
        public float ScatteringFromGlobalMedia = 0;
        public float AnisotrophyCoeffizient = float.NaN;

        public BoxTestScene(PathSamplingType pathSamplingType, bool useMediaBox = false, PixelSamplingMode pixelMode = PixelSamplingMode.Equal)
            :this(new BoxData() { EyePathSamplingType = pathSamplingType, LightPathSamplingType = pathSamplingType, CreateMediaBox = useMediaBox, PixelMode = pixelMode})
        {
        }

        public BoxTestScene(BoxData data)
        {
            this.ScreenWidth = data.ScreenWidth;
            this.ScreenHeight = data.ScreenHeight;
            this.PixX = data.PixX;
            this.PixY = data.PixY;
            this.MaxPathLength = data.MaxPathLength;

            if (data.CreateMediaBox) this.SizeFactor = 100;

            this.EyePathSamplingType = data.EyePathSamplingType;
            this.LightPathSamplingType = data.LightPathSamplingType;
            this.PhotonenCount = data.PhotonenCount;

            double groundSize = 0.9;       //Die Platten dürfen sich an den Kanten/Ecken nicht direkt berühren, da sonst der GeometryTerm gegen unendlich geht (Division durch Quadrad-Abstand)     
            float groundSizeF = (float)groundSize; //groundSize muss double sein, da nur so die Vorgabe-LightSourceArea mit der Ist-LightSourceArea übereinstimmt
            float emission = 10000 * SizeFactor * SizeFactor;
            this.LightSourceArea = (float)(groundSize * 2 * groundSize * 2 * SizeFactor * SizeFactor);
            this.EmissionPerArea = emission / LightSourceArea;

            List<DrawingObject> drawingObjects = new List<DrawingObject>();

            if (data.CreateWalls)
            {
                drawingObjects.Add(new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSizeF, groundSizeF, 1), new ObjectPropertys() { Position = new Vector3D(0, 0, -1) * SizeFactor, Size = SizeFactor, Orientation = new Vector3D(0, 0, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, Albedo = data.SurfaceAlbedo, BrdfModel = data.WaendeMaterial, GlossyPowExponent = 5000 }));//Fußboden
                drawingObjects.Add(new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSizeF, groundSizeF, 1), new ObjectPropertys() { Position = new Vector3D(0, 1, 0) * SizeFactor, Size = SizeFactor, Orientation = new Vector3D(90, 0, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, Albedo = data.SurfaceAlbedo, BrdfModel = data.WaendeMaterial, GlossyPowExponent = 5000 }));//Rückwand
                drawingObjects.Add(new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSizeF, groundSizeF, 1), new ObjectPropertys() { Position = new Vector3D(-1, 0, 0) * SizeFactor, Size = SizeFactor, Orientation = new Vector3D(0, 90, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, Albedo = data.SurfaceAlbedo, BrdfModel = data.WaendeMaterial, GlossyPowExponent = 5000 }));//Linke Wand
                drawingObjects.Add(new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSizeF, groundSizeF, 1), new ObjectPropertys() { Position = new Vector3D(+1, 0, 0) * SizeFactor, Size = SizeFactor, Orientation = new Vector3D(0, -90, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, Albedo = data.SurfaceAlbedo, BrdfModel = data.WaendeMaterial, GlossyPowExponent = 5000 }));//Rechte Wand
            }
            drawingObjects.Add(new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSizeF, groundSizeF, 1), new ObjectPropertys() { Position = new Vector3D(0, 0, +1) * SizeFactor, Size = SizeFactor, Orientation = new Vector3D(180, 0, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, Albedo = data.SurfaceAlbedo, RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = emission } }));//Decke

            //drawingObjects.Add(new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSizeF, groundSizeF, 1), new ObjectPropertys() { Position = new Vector3D(0, 0, +1) * SizeFactor, Size = SizeFactor, Orientation = new Vector3D(180, 0, 0), TextureFile = UnitTestHelper.FilePaths.DataDirectory + "room.hdr", NormalInterpolation = InterpolationMode.Flat, Albedo = 0.8f, RaytracingLightSource = new EnvironmentLightDescription() { Emission = 1000 } }));//Stilllife-Umgebungslicht
            //drawingObjects.Add(new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSizeF, groundSizeF, 1), new ObjectPropertys() { Position = new Vector3D(0, 0, +1) * SizeFactor, Size = SizeFactor, Orientation = new Vector3D(180, 0, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, Albedo = 0.8f, RaytracingLightSource = new EnvironmentLightDescription() { Emission = 1000 } }));//Weißes Umgebungslicht

            double imagePlaneSize = groundSize * 2;
            float imagePlaneDistance = 1.0f;
            float foV = (float)(Math.Atan(imagePlaneSize / 2 / imagePlaneDistance) / (2 * Math.PI) * 360) * 2;
            var rayCamera = new RayCamera(new CameraConstructorData() { Camera = new Camera(new Vector3D(0, -(groundSizeF + imagePlaneDistance), 0) * SizeFactor, new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), foV), ScreenWidth = this.ScreenWidth, ScreenHeight = this.ScreenHeight, PixelRange = new ImagePixelRange(PixX, PixY, 1, 1), SamplingMode = data.PixelMode });
            //var rayCamera = new RayCamera(new CameraConstructorData() { Camera = new Camera(new Vector3D(0, 0, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), 45), ScreenWidth = this.ScreenWidth, ScreenHeight = this.ScreenHeight, PixelRange = new ImagePixelRange(PixX, PixY, 1, 1), SamplingMode = data.PixelMode }); //Kamera in der Mitte der Box

            this.Quads = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(drawingObjects, true).Cast<RayQuad>().ToList();
            var rayObjects = this.Quads.SelectMany(x => x.DivideIntoTwoTriangles()).Cast<IRayObject>().ToList(); //Triangles
            //Lektion für heute: IEnumerable nur dann nehmen, wenn es nur einmal durchlaufen wird. Z.B. direkt im Foreach
            //Nicht aber wenn es als return zurück gegeben wird oder auf Variable gespeichert wird, die dann mehrmals verwendet wird
            //Hintergrund: Bei jeden Durchlauf wird der Ausdruck neu durchlaufen und somit alle Berechnungen/new-Aufrufe wiederholt ausgeführt

            this.SceneHasMedia = data.EyePathSamplingType != PathSamplingType.NoMedia || data.LightPathSamplingType != PathSamplingType.NoMedia;
            if (this.SceneHasMedia)
            {
                this.ScatteringFromMedia = data.ScatteringFromMedia / SizeFactor;
                this.AbsorbationFromMedia = data.AbsorbationFromMedia / SizeFactor;
                this.AnisotrophyCoeffizient = data.AnisotrophyCoeffizient;
                this.ScatteringFromGlobalMedia = data.ScatteringFromGlobalMedia / SizeFactor;
            }

            if (data.CreateMediaBox)
            {
                this.MediaBox = new BoundingBox(new Vector3D(-1, -1, -1) * 0.9f * SizeFactor, new Vector3D(1, 1, 1) * 0.9f * SizeFactor);
                
                var mediaCube = new DrawingObject(TriangleObjectGenerator.CreateCube(this.MediaBox.XSize, this.MediaBox.YSize, this.MediaBox.ZSize), new ObjectPropertys() { Position = this.MediaBox.Center, TextureFile = "#FFFFFF", Size = 0.5f, RefractionIndex = 1, NormalInterpolation = InterpolationMode.Flat, MediaDescription = new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * this.ScatteringFromMedia, AbsorbationCoeffizent = new Vector3D(1, 1, 1) * this.AbsorbationFromMedia, AnisotropyCoeffizient = this.AnisotrophyCoeffizient } });
                     
                var mediaRayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { mediaCube }, true);
                rayObjects.AddRange(mediaRayObjects);
            }else
            {
                this.MediaBox = null;
            }            

            this.IntersectionFinder = PixelRadianceCreationHelper.CreateIntersectionFinder(rayObjects, null);
            var globalMedia = new ParticipatingMediaBuilder().CreateGlobalMedia(this.ScatteringFromGlobalMedia == 0 ? null : new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * this.ScatteringFromGlobalMedia, AbsorbationCoeffizent = new Vector3D(1, 1, 1) * this.AbsorbationFromMedia, AnisotropyCoeffizient = data.AnisotrophyCoeffizient }); //Luft
            this.MediaIntersectionFinder = this.SceneHasMedia == false ? null : PixelRadianceCreationHelper.CreateMediaIntersectionFinder(rayObjects, null, globalMedia);


            var lightCreationData = new ConstruktorDataForLightSourceSampler()
            {
                IntersectionFinder = this.IntersectionFinder,
                MediaIntersectionFinder = this.MediaIntersectionFinder,
                LightDrawingObjects = rayObjects.Where(x => x.RayHeigh.Propertys.RaytracingLightSource != null).ToList(),
                RayCamera = rayCamera,
                ProgressChangedHandler = (s, f) => { },
                StopTriggerForColorEstimatorCreation = new CancellationTokenSource()
            };

            this.LightSourceSampler = new LightSourceSampler(lightCreationData);            

            this.EyePathSampler = new SubpathSampler(new SubpathSamplerConstruktorData()
            {
                RayCamera = rayCamera,
                LightSourceSampler = this.LightSourceSampler,
                IntersectionFinder = this.IntersectionFinder,
                MediaIntersectionFinder = this.MediaIntersectionFinder,
                PathSamplingType = data.EyePathSamplingType,
                MaxPathLength = this.MaxPathLength,
                BrdfSampler = new BrdfSampler(),
                PhaseFunction = new PhaseFunction()
            });
            this.LightPathSampler = new SubpathSampler(new SubpathSamplerConstruktorData()
            {
                RayCamera = rayCamera,
                LightSourceSampler = this.LightSourceSampler,
                IntersectionFinder = this.IntersectionFinder,
                MediaIntersectionFinder = this.MediaIntersectionFinder,
                PathSamplingType = data.LightPathSamplingType,
                MaxPathLength = this.MaxPathLength - 1,
                BrdfSampler = new BrdfSampler(),
                PhaseFunction = new PhaseFunction()
            });

            this.Camera = rayCamera;

            
            this.PointToPointConnector = new PointToPointConnector(new RayVisibleTester(this.IntersectionFinder, this.MediaIntersectionFinder), this.Camera, data.EyePathSamplingType);
        }

        //Achtung, wenn diese Zahl verändert wird, dann bei CheckPathPdfADifferenceToHistogram nochmal reinschauen
        public int SampleCountForPathPdfACheck = 100000;


        //Achtung, wenn samplecountForPathContributionCheck verändert wird, dann maxContributionError anpassen
        //Wenn sampleCount == 100000 muss maxContributionError = 5 sein
        //Wenn sampleCount == 20000  muss maxContributionError = 10 sein
        public int SamplecountForPathContributionCheck = 20000; //SampleCount für Test 5,6,7
        public int maxContributionError = 10;
    }
}
