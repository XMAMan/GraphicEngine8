using System;
using System.Collections.Generic;
using System.Linq;
using RaytracingColorEstimator;
using RayObjects;
using IntersectionTests;
using GraphicMinimal;
using GraphicGlobal;
using RayCameraNamespace;
using RayObjects.RayObjects;

namespace Radiosity._02_Patchcreation
{
    //Erzeugt aus ein RaytracingFrame3DData-Objekt eine Liste von IPatch-Objekten
    class PatchCreator
    {
        public static List<IPatch> CreatePatchList(RaytracingFrame3DData data, float maxAreaPerPatch, bool generateQuads, int sampleCountForShadowTest)
        {
            //1. Erzeuge erst große planare RayObjekte(Dreiecke und Vierecke aus der Scenenbeschreibung)
            var rayObjects = new RayObjectCreationHelper(data.GlobalObjektPropertys).CreatePlanarObjects(data.DrawingObjects, generateQuads);

            //Daten für Schritt 2 und 3
            var rayCamera = RayCameraFactory.CreateCamera(new CameraConstructorData() { Camera = data.GlobalObjektPropertys.Camera, ScreenWidth = data.ScreenWidth, ScreenHeight = data.ScreenHeight, PixelRange = data.PixelRange, DistanceDephtOfFieldPlane = data.GlobalObjektPropertys.DistanceDephtOfFieldPlane, WidthDephtOfField = data.GlobalObjektPropertys.WidthDephtOfField, DepthOfFieldIsEnabled = data.GlobalObjektPropertys.DepthOfFieldIsEnabled, SamplingMode = PixelSamplingMode.None });
            var intersectionFinder = new IntersectionFinder(rayObjects.Cast<IIntersecableObject>().ToList(), data.ProgressChanged);
            var lightSourceSampler = PixelRadianceCreationHelper.CreateLightSourceSampler(rayObjects, intersectionFinder, null, rayCamera, data);
            
            //2. Lege fest wie groß ein Dreieck/Viereck maximal sein soll
            float averageDistance = new AverageDistanceBetweenSceneAndCamera(intersectionFinder, rayCamera).GetAverageDistanceWithPrimaryRays();
            float maxPatchSize = maxAreaPerPatch * (averageDistance * averageDistance);

            //3. Unterteile die Dreiecke/Vierecke so lange, bis sie klein genug sind
            var halfShadowChecker = new IsInHalfShadowChecker(lightSourceSampler, intersectionFinder, sampleCountForShadowTest);
            IRandom rand = new Rand(0);
            var flatObjects = new RayObjectCreationHelper(data.GlobalObjektPropertys).DividePlanarObjects(rayObjects.Cast<IDivideable>().ToList(), (source, flat) =>
            {
                if (flat.SurfaceArea > maxPatchSize) return false; //Unterteile weiter wenn das Patch noch zu groß ist
                if ((flat as IFlatObject).RayHeigh.Propertys.RaytracingLightSource != null) return true; //Breche mit Unterteilen ab, wenn es eine Lichtquelle ist

                bool isInHalfShadow = halfShadowChecker.IsInHalfShadow(source as IFlatObject, flat as IFlatObject, rand);
                return flat.SurfaceArea < maxPatchSize / 4 || isInHalfShadow == false; //Abbruch wenn Patch nicht im Halbschatten liegt oder es ein viertel so groß ist wie maxPatchSize
            }, data.ProgressChanged).Cast<IFlatObject>().ToList();

            //4. Wandle die unterteilten Dreiecke und Vierecke in Radiosity-Daten um
            var patches = flatObjects.Select(x => CreatePatch(x)).ToList();

            return patches;
        }

        private static IPatch CreatePatch(IFlatObject flatObject)
        {
            if (flatObject is RayTriangle) return new RadiosityTriangle(flatObject as RayTriangle);
            if (flatObject is RayQuad) return new RadiosityQuad(flatObject as RayQuad);
            throw new Exception("Can not convert " + flatObject.GetType() + " to IPatch");
        }
    }
}
