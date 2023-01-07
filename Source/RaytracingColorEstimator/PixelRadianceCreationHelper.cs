using System.Collections.Generic;
using System.Linq;
using RayObjects;
using RaytracingLightSource;
using IntersectionTests;
using SubpathGenerator;
using FullPathGenerator;
using RayCameraNamespace;
using RaytracingBrdf;
using System;
using GraphicMinimal;
using ParticipatingMedia.Media;
using RayObjects.RayObjects;
using RaytracingBrdf.SampleAndRequest;

namespace RaytracingColorEstimator
{
    //Hilft beim Erstellen von ein PixelRadianceCalculator-Objekt
    public class PixelRadianceCreationHelper
    {
        //Gibt all die Objekte zurück, welche vom IntersectionTest (Standard oder Media) getroffen werden sollen
        public static List<IRayObject> GetObjectsWhichShouldBeUsedForIntersection(List<IRayObject> rayObjects)
        {
            return rayObjects.Where(x => 
            x.RayHeigh.Propertys.RaytracingLightSource == null || 
            ((x.RayHeigh.Propertys.RaytracingLightSource is FarAwayDirectionLightDescription) == false &&
             (x.RayHeigh.Propertys.RaytracingLightSource is EnvironmentLightDescription) == false)
            ).ToList();
        }

        //Rayobjekte werden in KD-Baum / BIH einsortiert
        public static IntersectionFinder CreateIntersectionFinder(List<IRayObject> rayObjects, Action<string, float> progressChangeHandler)
        {
            var geometryObjects = GetObjectsWhichShouldBeUsedForIntersection(rayObjects);
            var noMediaObjects = geometryObjects.Where(x => x.RayHeigh.Propertys.MediaDescription == null).Cast<IIntersecableObject>().ToList();
            return new IntersectionFinder(noMediaObjects, progressChangeHandler);
        }

        //Das Ding kommt direkt aus der Hölle
        public static MediaIntersectionFinder CreateMediaIntersectionFinder(List<IRayObject> rayObjects, Action<string, float> progressChangeHandler, IParticipatingMedia globalParticipatingMediaFromScene)
        {
            var geometryObjects = GetObjectsWhichShouldBeUsedForIntersection(rayObjects);
            var mediaObjects = geometryObjects.Where(x => x.RayHeigh.Propertys.MediaDescription != null).Cast<IIntersecableObject>().ToList();
            var noMediaObjects = geometryObjects.Where(x => x.RayHeigh.Propertys.MediaDescription == null).Cast<IIntersecableObject>().ToList();
            return new MediaIntersectionFinder(noMediaObjects, mediaObjects, globalParticipatingMediaFromScene, progressChangeHandler);
        }

        //Objekt, was alle Lichtquellen enthält
        public static LightSourceSampler CreateLightSourceSampler(List<IRayObject> rayObjects, IntersectionFinder intersectionFinder, MediaIntersectionFinder mediaIntersectionFinder, IRayCamera rayCamera, RaytracingFrame3DData data)
        {
            var lightCreationData = new ConstruktorDataForLightSourceSampler()
            {
                LightDrawingObjects = rayObjects.Where(x => x.RayHeigh.Propertys.RaytracingLightSource != null).ToList(),
                IntersectionFinder = intersectionFinder,
                MediaIntersectionFinder = mediaIntersectionFinder,
                RayCamera = rayCamera,
                LightPickStepSize = data.GlobalObjektPropertys.LightPickStepSize,
                ProgressChangedHandler = data.ProgressChanged,
                StopTriggerForColorEstimatorCreation = data.StopTrigger
            };

            return new LightSourceSampler(lightCreationData);
        }

        public static PixelRadianceData CreatePixelRadianceData(RaytracingFrame3DData data, SubPathSettings subPathSettings, FullPathSettings fullPathSettings)
        {
            var rayObjects = new RayObjectCreationHelper(data.GlobalObjektPropertys).CreateRayObjects(data.DrawingObjects);
            IntersectionFinder intersectionFinder = CreateIntersectionFinder(rayObjects, data.ProgressChanged);

            MediaIntersectionFinder mediaIntersectionFinder = null;            
            if (subPathSettings.EyePathType == PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling || subPathSettings.LightPathType == PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling ||
                subPathSettings.EyePathType == PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling || subPathSettings.LightPathType == PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling ||
                subPathSettings.EyePathType == PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling || subPathSettings.LightPathType == PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling ||
                subPathSettings.EyePathType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling || subPathSettings.LightPathType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling)
            {
                var globalMedia = new ParticipatingMediaBuilder().CreateGlobalMedia(data.GlobalObjektPropertys.GlobalParticipatingMedia);
                mediaIntersectionFinder = CreateMediaIntersectionFinder(rayObjects, data.ProgressChanged, globalMedia);

                data.ProgressChanged("Fertig mit Erstellung des MediaIntersectionFinder", 100);
            }
            
            var rayCamera = RayCameraFactory.CreateCamera(new CameraConstructorData() { Camera = data.GlobalObjektPropertys.Camera, ScreenWidth = data.ScreenWidth, ScreenHeight = data.ScreenHeight, PixelRange = data.PixelRange, DistanceDephtOfFieldPlane = data.GlobalObjektPropertys.DistanceDephtOfFieldPlane, WidthDephtOfField = data.GlobalObjektPropertys.WidthDephtOfField, DepthOfFieldIsEnabled = data.GlobalObjektPropertys.DepthOfFieldIsEnabled, UseCosAtCamera = data.GlobalObjektPropertys.UseCosAtCamera, SamplingMode = data.GlobalObjektPropertys.CameraSamplingMode });
            var lightSourceSampler = CreateLightSourceSampler(rayObjects, intersectionFinder, mediaIntersectionFinder, rayCamera, data);

            data.ProgressChanged("Fertig mit Erstellung des LightSourceSamplers", 100);

            IPhaseFunctionSampler phaseFunction = new PhaseFunction();

            var eyePathSampler = new SubpathSampler(new SubpathSamplerConstruktorData()
            {
                RayCamera = rayCamera,
                LightSourceSampler = lightSourceSampler,
                IntersectionFinder = intersectionFinder,
                MediaIntersectionFinder = mediaIntersectionFinder,
                PathSamplingType = subPathSettings.EyePathType,
                MaxPathLength = subPathSettings.MaxEyePathLength != -1 ? subPathSettings.MaxEyePathLength : data.GlobalObjektPropertys.RecursionDepth,
                BrdfSampler = new BrdfSampler(),
                PhaseFunction = phaseFunction
            });

            data.ProgressChanged("Fertig mit Erstellung des EyePathSampler", 100);            

            var lightPathSampler = new SubpathSampler(new SubpathSamplerConstruktorData()
            {
                RayCamera = rayCamera,
                LightSourceSampler = lightSourceSampler,
                IntersectionFinder = intersectionFinder,
                MediaIntersectionFinder = mediaIntersectionFinder,
                PathSamplingType = subPathSettings.LightPathType,
                MaxPathLength = data.GlobalObjektPropertys.RecursionDepth - 1,
                BrdfSampler = new BrdfSampler(),
                PhaseFunction = phaseFunction
            });

            data.ProgressChanged("Fertig mit Erstellung des LightPathSamplers", 100);

            FullPathSampler fullPathSampler = null;
            if (fullPathSettings != null)
            {
                var fullPathData = new FullPathKonstruktorData()
                {
                    EyePathSamplingType = eyePathSampler.PathSamplingType,
                    LightPathSamplingType = lightPathSampler.PathSamplingType,
                    PointToPointConnector = new PointToPointConnector(new RayVisibleTester(intersectionFinder, mediaIntersectionFinder), rayCamera, eyePathSampler.PathSamplingType, phaseFunction),
                    RayCamera = rayCamera,
                    LightSourceSampler = lightSourceSampler,
                    MaxPathLength = data.GlobalObjektPropertys.RecursionDepth,
                };

                fullPathSampler = new FullPathSampler(fullPathData, fullPathSettings);

                data.ProgressChanged("Fertig mit Erstellung des FullPathSamplers", 100);
            }

            return new PixelRadianceData()
            {
                Frame3DData = data,
                IntersectionFinder = intersectionFinder,
                MediaIntersectionFinder = mediaIntersectionFinder,
                RayCamera = rayCamera,
                LightSourceSampler = lightSourceSampler,
                EyePathSampler = eyePathSampler,
                LightPathSampler = lightPathSampler,
                FullPathSampler = fullPathSampler,
                PhaseFunction = phaseFunction
            };
        }

        public static PixelRadianceCalculator CreatePixelRadianceCalculator(RaytracingFrame3DData data, SubPathSettings subPathSettings, FullPathSettings fullPathSettings)
        {
            PixelRadianceData radianceData = CreatePixelRadianceData(data, subPathSettings, fullPathSettings);

            var pixelRadianceCalculator = new PixelRadianceCalculator(radianceData);

            data.ProgressChanged("Fertig mit Erstellung des PixelRadianceCalculators", 100);

            return pixelRadianceCalculator;
        }
    }
}
