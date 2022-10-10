using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia.Media;
using RayObjects;
using RayObjects.RayObjects;
using RaytracingColorEstimator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriangleObjectGeneration;

namespace FullPathGeneratorTest.SubPathConnectors
{
    class TestInput
    {
        public enum Block 
        { 
            No, 
            Solid,  //Ohne Media Gefüllt
            Glas,   //Mit Medium gefüllt was Refractionindex hat
            Air     //mit Medium gefülllt, wo der Refractionindex 0 ist
        }

        public float GroundYRotation = 0;
        public float LightYRotation = 0;
        public bool IsLightIntersectable = true; //Wenn false, ist Lichtobjekt kein Element des IntersectionFinders
        public Block Blocking = Block.No;
        public bool HasGlobalMedia = false;
        public bool HasAtmosphereSpere = false;
        public bool CreateMediaIntersectionFinder = false;
    }

    //Stellt das Testmaterial für den RayVisibleTester bereit
    class TestData
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;
        private readonly float groundSize = 0.5f;

        public RayVisibleTester RayVisibleTester { get; private set; }
        public IntersectionPoint EyePoint { get; private set; }
        public MediaIntersectionPoint EyeMediaPoint { get; private set; }
        public IntersectionPoint LightPoint { get; private set; }
        public MediaIntersectionPoint LightMediaPoint { get; private set; }
        public Vector3D CameraPosition { get; private set; }
        public MediaIntersectionPoint CameraMediaPoint { get; private set; }

        public TestData(TestInput data)
        {
            var rayObjects = CreateRayObjects(data);
            var intersectionFinder = PixelRadianceCreationHelper.CreateIntersectionFinder(rayObjects.AllIntersectableRayObjects, null);
            MediaIntersectionFinder mediaIntersectionFinder = data.CreateMediaIntersectionFinder ? CreateMediaIntersectionFinder(rayObjects.AllIntersectableRayObjects, data.HasGlobalMedia) : null;

            this.RayVisibleTester = new RayVisibleTester(intersectionFinder, mediaIntersectionFinder);

            Vector3D groundNormal = Vector3D.RotateVector(new Vector3D(0, 0, 1), 90 + 180, data.GroundYRotation, 0);
            this.CameraPosition = groundNormal;
            this.EyePoint = intersectionFinder.GetIntersectionPoint(new Ray(this.CameraPosition, -groundNormal), 0);

            var lightIntersectionFinder = PixelRadianceCreationHelper.CreateIntersectionFinder(rayObjects.LightSourceTriangles, null);
            Vector3D lightNormal = Vector3D.RotateVector(new Vector3D(0, 0, 1), 90, data.LightYRotation, 0);            
            this.LightPoint = lightIntersectionFinder.GetIntersectionPoint(new Ray(new Vector3D(0,10,0) + lightNormal, -lightNormal), 0);

            if (data.CreateMediaIntersectionFinder)
            {
                this.CameraMediaPoint = this.RayVisibleTester.CreateCameraMediaStartPoint(this.CameraPosition);
                this.EyeMediaPoint = MediaIntersectionPoint.CreateSurfacePoint(this.CameraMediaPoint, this.EyePoint);
                this.LightMediaPoint = MediaIntersectionPoint.CreateSurfacePoint(this.CameraMediaPoint, this.LightPoint);
            }
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

        class RayObjekts
        {
            public List<IRayObject> AllIntersectableRayObjects;
            public List<IRayObject> LightSourceTriangles;
        }

        private RayObjekts CreateRayObjects(TestInput data)
        {
            List<DrawingObject> drawingObjects = new List<DrawingObject>
            {
                CreateQuad(0, 90 + 180, data.GroundYRotation)
            };
            if (data.Blocking == TestInput.Block.Glas) drawingObjects.Add(CreateBlockingObject(1.5f));
            if (data.Blocking == TestInput.Block.Air) drawingObjects.Add(CreateBlockingObject(1.0f));
            if (data.Blocking == TestInput.Block.Solid) drawingObjects.Add(CreateBlockingObject(float.NaN));
            if (data.HasAtmosphereSpere) drawingObjects.Add(CreateAtmosphereSphere());

            DrawingObject lightSourceDrawingObject = CreateQuad(10, 90, data.LightYRotation);

            var intersectableRayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(drawingObjects);
            var lightSourceTriangles = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { lightSourceDrawingObject });

            if (data.IsLightIntersectable)
            {
                intersectableRayObjects.AddRange(lightSourceTriangles);
            }

            return new RayObjekts()
            {
                AllIntersectableRayObjects = intersectableRayObjects,
                LightSourceTriangles = lightSourceTriangles
            };
        }

        private DrawingObject CreateQuad(float yPos, float xRotation, float yRotation)
        {
            return new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSize, groundSize, 1), new ObjectPropertys() 
            { 
                Position = new Vector3D(0, yPos, 0), 
                Orientation = new Vector3D(xRotation, yRotation, 0), 
                TextureFile = "#FFFFFF", 
                NormalSource = new NormalFromParallax() { ParallaxMap = WorkingDirectory + "ExpectedValues\\ParallaxBumpmap.bmp", TexturHeightFactor = 1 },
                
            });
        }

        private DrawingObject CreateBlockingObject(float refractionIndex)
        {
            return new DrawingObject(TriangleObjectGenerator.CreateCube(10, 0.5f, 10), new ObjectPropertys() { Position = new Vector3D(0, 5, 0), Orientation = new Vector3D(0, 0, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, 
                MediaDescription = float.IsNaN(refractionIndex) ? null : new DescriptionForHomogeneousMedia()
                {
                    ScatteringCoeffizent = new Vector3D(1, 1, 1) * 0.2f,
                    AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 0.1f,
                    AnisotropyCoeffizient = 0.0f
                },
                RefractionIndex = refractionIndex
            });
        }

        private DrawingObject CreateAtmosphereSphere()
        {
            return new DrawingObject(TriangleObjectGenerator.CreateSphere(7, 10, 10), new ObjectPropertys()
            {
                Position = new Vector3D(0, 0, 0),
                Orientation = new Vector3D(0, 0, 0),
                TextureFile = "#FFFFFF",
                NormalInterpolation = InterpolationMode.Flat,
                MediaDescription = new DescriptionForHomogeneousMedia()
                {
                    ScatteringCoeffizent = new Vector3D(1, 1, 1) * 0.2f,
                    AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 0.1f,
                    AnisotropyCoeffizient = 0.0f
                },
                RefractionIndex = 1
            });
        }
    }
}
