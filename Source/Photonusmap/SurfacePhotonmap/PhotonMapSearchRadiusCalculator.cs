using System.Collections.Generic;
using System.Linq;
using GraphicGlobal;
using IntersectionTests;
using PointSearch;
using RayCameraNamespace;
using RayTracerGlobal;
using SubpathGenerator;

namespace Photonusmap
{
    public static class PhotonMapSearchRadiusCalculator
    {
        public static float GetSearchRadiusForPhotonmapWithPhotonDensity(List<SubPath> lightPaths, IntersectionFinder intersectionFinder, IRayCamera rayCamera, int minPhotonSearchCount = 15)
        {
            int primaryRayCount = 50;

            var photons = lightPaths.SelectMany(x => x.Points).Cast<IPoint>().ToList();
            if (photons.Count < minPhotonSearchCount) minPhotonSearchCount = photons.Count;

            KNearestNeighborSearch photonSearch = new KNearestNeighborSearch(photons);
            IRandom rand = new Rand(0);
            List<float> distanceList = new List<float>();

            for (int i = 0; i < primaryRayCount; i++)
            {
                var ray = rayCamera.CreateRandomPrimaryRay(rand);
                var point = intersectionFinder.GetIntersectionPoint(ray, 0);
                if (point != null)
                {
                    float distance = ((photonSearch.SearchKNearestNeighbors(point.Position, minPhotonSearchCount).Last() as PathPoint).Position - point.Position).Length();
                    distanceList.Add(distance);
                }
            }

            distanceList.Sort();
            return distanceList[distanceList.Count / 2];
        }

        public static float GetSearchRadiusForPhotonmapWithPixelFootprint(IntersectionFinder intersectionFinder, IRayCamera rayCamera)
        {
            int primaryRayCount = 50;
            IRandom rand = new Rand(0);

            List<float> pixelSizes = new List<float>();
            for (int i = 0; i < primaryRayCount; i++)
            {
                var ray = rayCamera.CreateRandomPrimaryRay(rand);
                var point = intersectionFinder.GetIntersectionPoint(ray, 0);
                if (point != null)
                {
                    pixelSizes.Add(rayCamera.GetPixelFootprintSize(point.Position).X);
                }
            }

            pixelSizes.Sort();
            return pixelSizes[pixelSizes.Count / 2];
        }
    }
}
