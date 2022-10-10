using GraphicGlobal;
using IntersectionTests;
using RayCameraNamespace;
using System.Collections.Generic;
using System.Linq;

namespace Radiosity._02_Patchcreation
{
    class AverageDistanceBetweenSceneAndCamera
    {
        private readonly IntersectionFinder intersectionFinder;
        private readonly IRayCamera camera;

        public AverageDistanceBetweenSceneAndCamera(IntersectionFinder intersectionFinder, IRayCamera camera)
        {
            this.intersectionFinder = intersectionFinder;
            this.camera = camera;
        }

        public float GetAverageDistanceToSceneCenter()
        {
            return (this.intersectionFinder.GetBoundingBoxFromSzene().Center - this.camera.Position).Length();
        }

        public float GetAverageDistanceWithPrimaryRays(int sampleCount = 10)
        {
            IRandom rand = new Rand(0);

            List<float> distanceValues = new List<float>();

            for (int i = 0; i < sampleCount; i++)
            {
                var ray = this.camera.CreateRandomPrimaryRay(rand);

                var obj = this.intersectionFinder.GetIntersectionPoint(ray, (float)rand.NextDouble());

                if (obj != null)
                {
                    float distance = (ray.Start - obj.Position).Length();

                    distanceValues.Add(distance);
                }
            }

            if (distanceValues.Any() == false) return 1;

            return distanceValues.Average();
        }
    }
}
