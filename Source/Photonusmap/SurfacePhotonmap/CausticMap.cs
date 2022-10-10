using System;
using System.Collections.Generic;
using System.Linq;
using PointSearch;
using SubpathGenerator;
using RayTracerGlobal;
using GraphicMinimal;

namespace Photonusmap
{
    public class CausticMap : ISurfacePhotonmap
    {
        public int LightPathCount { get; private set; }
        public float SearchRadius { get; set; }
        public int MinPhotonCountForRadianceEstimation { get { return 5; } }

        public int MinEyeIndex { get; private set; } //Für diese Eye-Indizes ist die Phtonmap gedacht
        public int MaxEyeIndex { get; private set; }

        public bool ContainsOnlySpecularLightPahts { get { return true; } }

        private readonly FixedRadiusPointSearch surfacePointSearch;

        //lightPathCount so viel Sub-LightPaths wurden ausgesendet
        //Bei einer Caustic-Map ist lightPaths.Count < photonCount
        public CausticMap(List<SubPath> ligthPahts, int sendedLightPathCount, Action<string, float> progressChanged, int minEyeIndex, int maxEyeIndex)
        {
            this.LightPathCount = sendedLightPathCount;
            var causticPaths = ligthPahts.Where(x => x.Points.Any(y => y.Predecessor != null && y.Predecessor.BrdfSampleEventOnThisPoint.IsSpecualarReflected)).ToList();
            var causticPhotons = causticPaths.SelectMany(x => x.Points).Where(x => x.Index > 0 && x.IsDiffusePoint && x.Predecessor.BrdfSampleEventOnThisPoint.IsSpecualarReflected).Cast<IPoint>();
            progressChanged("Erstelle CausticmapSuchstruktur", 0);
            this.surfacePointSearch = new FixedRadiusPointSearch(causticPhotons);
            this.MinEyeIndex = minEyeIndex;
            this.MaxEyeIndex = maxEyeIndex;
        }

        public CausticMap(List<PathPoint> photons, int photonCount, Action<string, float> progressChanged, int minEyeIndex, int maxEyeIndex)
        {
            this.LightPathCount = photonCount;
            var causticPhotons = photons.Cast<IPoint>();
            progressChanged("Erstelle CausticmapSuchstruktur", 0);
            this.surfacePointSearch = new FixedRadiusPointSearch(causticPhotons);
            this.MinEyeIndex = minEyeIndex;
            this.MaxEyeIndex = maxEyeIndex;
        }

        //GaussianFilter
        public float KernelFunction(float distanceSquareToCenter, float searchRadiusSquare)
        {
            float alpha = 0.918f, beta = 1.953f;
            return alpha * (1 - ((1 - (float)Math.Exp(-beta * distanceSquareToCenter / 2 / (searchRadiusSquare))) / (1 - (float)Math.Exp(-beta))));
        }

        public IEnumerable<PathPoint> QuerrySurfacePhotons(Vector3D querryPosition, float searchRadius)
        {
            return this.surfacePointSearch.FixedRadiusSearch(querryPosition, searchRadius).Cast<PathPoint>();
        }
    }
}
