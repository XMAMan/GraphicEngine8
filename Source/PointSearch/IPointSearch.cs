using System.Collections.Generic;
using RayTracerGlobal;

namespace PointSearch
{
    interface IPointSearch
    {
        IPoint[] ApproximateKNearestNeighborSearch(IPoint queryPoint,
                                                          int k,                // number of near neighbors to return
                                                          float eps = 0);        // the error bound

        IPoint[] PriorityKNearestNeighborSearch(IPoint queryPoint,
                                                          int k,                // number of near neighbors to return
                                                          float eps = 0);        // the error bound

        IPoint[] FixedRadiusSearchForKNearestNeighbors(IPoint queryPoint,
                                                          float searchRadius,   // Search Radius bound
                                                          int k,                // number of near neighbors to return
                                                          float eps = 0);        // the error bound

        List<IPoint> FixedRadiusSearch(IPoint queryPoint, float searchRadius);
    }
}
