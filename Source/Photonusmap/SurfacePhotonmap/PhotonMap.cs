using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PointSearch;
using GraphicMinimal;
using SubpathGenerator;
using RayTracerGlobal;

namespace Photonusmap
{
    //Speichert eine Menge von 3D-Punkten (SurfacePointPhotonmap)
    public class PhotonMap : ISurfacePhotonmap
    {
        public int LightPathCount { get; private set; }
        public float SearchRadius { get; set; }
        public int MinPhotonCountForRadianceEstimation { get { return 0; } }

        public int MinEyeIndex { get; private set; } //Für diese Eye-Indizes ist die Phtonmap gedacht
        public int MaxEyeIndex { get; private set; }
        public bool ContainsOnlySpecularLightPahts { get { return false; } }

        private readonly FixedRadiusPointSearch surfacePointSearch;

        //photonCount so viel LightPaths sollten erstellt werden
        //Bei einer Caustic-Map ist lightPaths.Count < photonCount
        public PhotonMap(List<SubPath> ligthPahts, int sendedLightPathCount, Action<string, float> progressChanged, int minEyeIndex, int maxEyeIndex)
        {
            this.LightPathCount = sendedLightPathCount;
            progressChanged("Erstelle Photonmapsuchstruktur", 0);
            this.surfacePointSearch = new FixedRadiusPointSearch(ligthPahts.SelectMany(x => x.Points).Where(x => x.Index > 0 && x.IsDiffusePoint).Cast<IPoint>().ToList());
            this.MinEyeIndex = minEyeIndex;
            this.MaxEyeIndex = maxEyeIndex;
        }

        public IEnumerable<PathPoint> QuerrySurfacePhotons(Vector3D querryPosition, float searchRadius)
        {
            return this.surfacePointSearch.FixedRadiusSearch(querryPosition, searchRadius).Cast<PathPoint>();
        }

        //Kernelfunktion für die globale Photonmap
        public float KernelFunction(float distanceSquareToCenter, float searchRadiusSquare)
        {
            return 1.0f / (searchRadiusSquare * (float)Math.PI);
        }

        
    }

    
}
