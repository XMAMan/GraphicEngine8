using System;
using System.Collections.Generic;
using System.Linq;
using SubpathGenerator;
using IntersectionTests;
using ParticipatingMedia;

namespace Photonusmap
{
    //Speichert Menge von 3D-Punkten, die in der Luft liegen. Mit einer Beam-Query kann man die Photonen abfragen
    public class PointDataBeamQueryMap : IPhotonmap
    {
        private readonly IRayObjectIntersection mediaPoints;

        public int LightPathCount { get; private set; }
        public float SearchRadius { get; set; }
        public int MinPhotonCountForRadianceEstimation { get { return 5; } }

        public int MinEyeIndex { get { return 1; } } //Für diese Eye-Indizes ist die Phtonmap gedacht
        public int MaxEyeIndex { get { return int.MaxValue; } }

        public PointDataBeamQueryMap(List<SubPath> lightPaths, int photonCount, Action<string, float> progressChanged, float searchRadius)
        {
            var photons = lightPaths.SelectMany(x => x.Points).Where(x => x.Index > 0 && x.LocationType == MediaPointLocationType.MediaParticle).Select(x => new VolumetricPhoton(x) { Radius = searchRadius }).ToList();
            this.SearchRadius = searchRadius;
            //this.SearchRadius = VolumetricPhotonmapRadiusCalculator.CalculateAndSetPhotonRadiusForEachPhoton(photons.Cast<ISphere>().ToList(), progressChanged, 5, rayCamera);
            this.mediaPoints = new BoundingIntervallHierarchy(photons.Cast<IIntersecableObject>().ToList(), progressChanged);
            this.LightPathCount = photonCount;
        }

        public IEnumerable<PhotonIntersectionPoint> QuerryMediaPhotons(IQueryLine querryLine, float pathCreationTime)
        {
            return this.mediaPoints.GetAllIntersectionPoints(querryLine.Ray, querryLine.LongRayLength, pathCreationTime).Cast<PhotonIntersectionPoint>();
        }

        public static float SilvermanTwoDimensinalBiweightKernel(float x)
        {
            float f = (1 - x * x);
            return 3 / (float)Math.PI * f * f;
        }
    }
}
