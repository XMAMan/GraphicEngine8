using GraphicMinimal;
using ParticipatingMedia;
using PointSearch;
using RayTracerGlobal;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Photonusmap
{
    //Menge von MediaParticeln
    public class PointDataPointQueryMap
    {
        public int LightPathCount { get; private set; }
        public float SearchRadius { get; set; }

        private readonly FixedRadiusPointSearch pointSearch;

        public PointDataPointQueryMap(List<SubPath> ligthPahts, int sendedLightPathCount, Action<string, float> progressChanged)
        {
            this.LightPathCount = sendedLightPathCount;
            progressChanged("Erstelle Photonmapsuchstruktur", 0);
            this.pointSearch = new FixedRadiusPointSearch(ligthPahts.SelectMany(x => x.Points).Where(x => x.Index > 0 && x.LocationType == MediaPointLocationType.MediaParticle).Cast<IPoint>());
        }

        public IEnumerable<PathPoint> QuerryPhotons(Vector3D querryPosition, float searchRadius)
        {
            return this.pointSearch.FixedRadiusSearch(querryPosition, searchRadius).Cast<PathPoint>();
        }

        //1.0f / Kugel-Rauminhalt
        public float KernelFunction(float searchRadius)
        {
            return 1.0f / ((4.0f / 3) * searchRadius * searchRadius * searchRadius * (float)Math.PI);
        }
    }
}
