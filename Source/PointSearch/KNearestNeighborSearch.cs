using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using RayTracerGlobal;

namespace PointSearch
{
    public class KNearestNeighborSearch
    {
        private IPointSearch pointSearch;

        public KNearestNeighborSearch(IEnumerable<IPoint> points)
        {
            this.pointSearch = new BdTree(points.ToArray(), 3);
        }

        public IPoint[] SearchKNearestNeighbors(Vector3D position, int k)
        {
            return this.pointSearch.ApproximateKNearestNeighborSearch(new Point(position.X, position.Y, position.Z), k);
        }
    }

    public class FixedRadiusPointSearch
    {
        private IPointSearch pointSearch;

        public FixedRadiusPointSearch(IEnumerable<IPoint> points)
        {
            this.pointSearch = new KdTree(points.ToArray(), 3);
        }

        public List<IPoint> FixedRadiusSearch(Vector3D position, float searchRadius)
        {
            return this.pointSearch.FixedRadiusSearch(new Point(position.X, position.Y, position.Z), searchRadius);
        }
    }

}
