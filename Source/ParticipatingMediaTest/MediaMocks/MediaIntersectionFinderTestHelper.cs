using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia;
using ParticipatingMedia.Media;
using RayObjects;
using RayObjects.RayObjects;
using RaytracingColorEstimator;
using System.Collections.Generic;
using System.Linq;
using TriangleObjectGeneration;

namespace ParticipatingMediaTest.MediaMocks
{
    public class IntersectionFinderData
    {
        public MediaIntersectionFinder MediaIntersectionFinder; //Alle Media+NoMedia-Objekte außer Environmentlights
        public IntersectionFinder NoMediaIntersectionFinder;//Alle NoMedia-Objekte außer Environmentlights
        public List<IRayObject> RayObjects; //Media, NoMedia, Environmentlights
    }

    public class IntersectableTestObject
    {
        public enum ObjectType { Cube, Sphere}

        public ObjectType Type = IntersectableTestObject.ObjectType.Cube;
        public float ScatteringCoeffizient = 0; //Wenn 0, dann hat es kein Media
        public float XPosition = float.NaN;
        public float Rotation = 0;
        public float Radius = float.NaN;
        public float RefractionIndex = float.NaN;
        public ILightSourceDescription LightsourceData = null;
        public BrdfModel Material = BrdfModel.Diffus;
    }

    public static class MediaIntersectionFinderTestHelper
    {
        public static IntersectionFinderData CreateIntersectionDataWithMedia(List<IntersectableTestObject> objList, float scatterCoeffizientFromGlobalMedia, ParticipatingMediaMockData distanceSamplingMockData = null)
        {
            return CreateIntersectionData(objList, scatterCoeffizientFromGlobalMedia, true, distanceSamplingMockData);
        }

        public static IntersectionFinderData CreateIntersectionDataNoMedia(List<IntersectableTestObject> objList)
        {
            return CreateIntersectionData(objList, 0, false, null);
        }

        private static IntersectionFinderData CreateIntersectionData(List<IntersectableTestObject> objList, float scatterCoeffizientFromGlobalMedia, bool withMedia, ParticipatingMediaMockData distanceSamplingMockData = null)
        {
            List<DrawingObject> drawingObjects = new List<DrawingObject>();
            foreach (var obj in objList)
            {
                TriangleObject triangleData;
                if (obj.Type == IntersectableTestObject.ObjectType.Cube)
                    triangleData = TriangleObjectGenerator.CreateCube(obj.Radius, obj.Radius, obj.Radius);
                else
                    triangleData = TriangleObjectGenerator.CreateSphere(obj.Radius, 20, 20);

                IParticipatingMediaDescription medi = null;
                if (float.IsNaN(obj.RefractionIndex) == false)
                {
                    if (obj.ScatteringCoeffizient != 0)
                    {
                        medi = new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * obj.ScatteringCoeffizient };
                    }
                    else
                    {
                        if (withMedia) medi = new DescriptionForVacuumMedia();
                    }
                }
                

                DrawingObject drawingObject = new DrawingObject(triangleData, new ObjectPropertys()
                {
                    TextureFile = "#FFFFFF",
                    Position = new Vector3D(obj.XPosition, 0, 0),
                    Orientation = new Vector3D(0,0, obj.Rotation),
                    RefractionIndex = obj.RefractionIndex,
                    MediaDescription = medi,
                    RaytracingLightSource = (IRaytracingLightSource)obj.LightsourceData,
                    BrdfModel = obj.Material,
                    NormalInterpolation = InterpolationMode.Flat
                });

                drawingObjects.Add(drawingObject);
            }

            if (distanceSamplingMockData != null)
            {
                var mediaFactory = new ParticipatingMediaMockFactory(distanceSamplingMockData);
                var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys(), mediaFactory).CreateRayObjects(drawingObjects);
                var globalMedia = new ParticipatingMediaBuilder(mediaFactory).CreateGlobalMedia(scatterCoeffizientFromGlobalMedia == 0 ? null : new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * scatterCoeffizientFromGlobalMedia }); //Luft
                return CreateMediaIntersectionFinder(rayObjects, globalMedia);
            }else
            {
                var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(drawingObjects);
                var globalMedia = new ParticipatingMediaBuilder().CreateGlobalMedia(scatterCoeffizientFromGlobalMedia == 0 ? null : new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * scatterCoeffizientFromGlobalMedia }); //Luft
                return CreateMediaIntersectionFinder(rayObjects, globalMedia);
            }
        }

        //spheres.X = X-Koordinate der Kugel (y und z sind immer 0)
        //spheres.Y = Radius der Kugel
        //spheres.Z = Scattering-Koeffizient vom Media innerhalb der Kugel (Wenn 0, dann hat es kein Media)
        public static IntersectionFinderData CreateSphereRowOnXAxis(List<Vector3D> spheres, float scatterCoeffizientFromGlobalMedia, ParticipatingMediaMockData data)
        {
            var mediaFactory = new ParticipatingMediaMockFactory(data);
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys(), mediaFactory).CreateRayObjects(spheres.Select(x => new DrawingObject(TriangleObjectGenerator.CreateSphere(x.Y, 20, 20), new ObjectPropertys() { TextureFile = "#FFFFFF", Position = new Vector3D(x.X, 0, 0), RefractionIndex = 1, MediaDescription = x.Z == 0 ? null : new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * x.Z } })).ToList());
            var globalMedia = new ParticipatingMediaBuilder(mediaFactory).CreateGlobalMedia(scatterCoeffizientFromGlobalMedia == 0 ? null : new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * scatterCoeffizientFromGlobalMedia }); //Luft
            var intersectionFinder = CreateMediaIntersectionFinder(rayObjects, globalMedia);
            return intersectionFinder;
        }

        //cubes.X = X-Koordinate des Würfels (y und z sind immer 0)
        //cubes.Y = Radius des Würfels
        //cubes.Z = Scattering-Koeffizient vom Media innerhalb des Würfels (Wenn 0, dann hat er kein Media)
        public static IntersectionFinderData CreateCubeRowOnXAxis(List<Vector3D> cubes, float scatterCoeffizientFromGlobalMedia, ParticipatingMediaMockData data)
        {
            var mediaFactory = new ParticipatingMediaMockFactory(data);
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys(), mediaFactory).CreateRayObjects(cubes.Select(x => new DrawingObject(TriangleObjectGenerator.CreateCube(x.Y, x.Y, x.Y), new ObjectPropertys() { TextureFile = "#FFFFFF", Position = new Vector3D(x.X, 0, 0), MediaDescription = x.Z == 0 ? null : new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * x.Z } })).ToList());
            var globalMedia = new ParticipatingMediaBuilder(mediaFactory).CreateGlobalMedia(scatterCoeffizientFromGlobalMedia == 0 ? null : new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * scatterCoeffizientFromGlobalMedia }); //Luft
            var intersectionFinder = CreateMediaIntersectionFinder(rayObjects, globalMedia);
            return intersectionFinder;
        }

        //spheres.X = X-Koordinate der Kugel (y und z sind immer 0)
        //spheres.Y = Radius der Kugel
        //spheres.Z = Scattering-Koeffizient vom Media innerhalb der Kugel (Wenn 0, dann hat es kein Media)
        public static IntersectionFinderData CreateSphereRowOnXAxis(List<Vector3D> spheres, float scatterCoeffizientFromGlobalMedia)
        {
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(spheres.Select(x => new DrawingObject(TriangleObjectGenerator.CreateSphere(x.Y, 20, 20), new ObjectPropertys() { TextureFile = "#FFFFFF", Position = new Vector3D(x.X, 0, 0), RefractionIndex = 1, MediaDescription = x.Z == 0 ? null : new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * x.Z } })).ToList());
            var globalMedia = new ParticipatingMediaBuilder().CreateGlobalMedia(scatterCoeffizientFromGlobalMedia == 0 ? null : new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * scatterCoeffizientFromGlobalMedia }); //Luft
            var intersectionFinder = CreateMediaIntersectionFinder(rayObjects, globalMedia);
            return intersectionFinder;
        }

        //cubes.X = X-Koordinate des Würfels (y und z sind immer 0)
        //cubes.Y = Radius des Würfels
        //cubes.Z = Scattering-Koeffizient vom Media innerhalb des Würfels (Wenn 0, dann hat er kein Media)
        public static IntersectionFinderData CreateCubeRowOnXAxis(List<Vector3D> cubes, float scatterCoeffizientFromGlobalMedia)
        {
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(cubes.Select(x => new DrawingObject(TriangleObjectGenerator.CreateCube(x.Y, x.Y, x.Y), new ObjectPropertys() { TextureFile = "#FFFFFF", Position = new Vector3D(x.X, 0, 0), RefractionIndex = 1, MediaDescription = x.Z == 0 ? null : new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * x.Z } })).ToList());
            var globalMedia = new ParticipatingMediaBuilder().CreateGlobalMedia(scatterCoeffizientFromGlobalMedia == 0 ? null : new DescriptionForHomogeneousMedia() { ScatteringCoeffizent = new Vector3D(1, 1, 1) * scatterCoeffizientFromGlobalMedia }); //Luft
            var intersectionFinder = CreateMediaIntersectionFinder(rayObjects, globalMedia);
            return intersectionFinder;
        }

        private static IntersectionFinderData CreateMediaIntersectionFinder(List<IRayObject> rayObjects, IParticipatingMedia globalParticipatingMediaFromScene)
        {
            var geometryObjects = PixelRadianceCreationHelper.GetObjectsWhichShouldBeUsedForIntersection(rayObjects);
            var mediaObjects = geometryObjects.Where(x => x.RayHeigh.Propertys.MediaDescription != null).Cast<IIntersecableObject>().ToList();
            var noMediaObjects = geometryObjects.Where(x => x.RayHeigh.Propertys.MediaDescription == null).Cast<IIntersecableObject>().ToList();
            var mediaIntersectionFinder = new MediaIntersectionFinder(noMediaObjects, mediaObjects, globalParticipatingMediaFromScene, null);
            return new IntersectionFinderData()
            {
                MediaIntersectionFinder = mediaIntersectionFinder,
                NoMediaIntersectionFinder = new IntersectionFinder(noMediaObjects, null),
                RayObjects = rayObjects
            };
        }
    }
}
