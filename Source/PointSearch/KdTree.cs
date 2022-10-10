using System;
using System.Collections.Generic;
using RayTracerGlobal;

namespace PointSearch
{
    public class KdTree : IPointSearch
    {
        public enum SplitRule
        {
            Standard = 0,   // the optimized kd-splitting rule
            Midpoint = 1,   // midpoint split
            Fair = 2,   // fair split
            SlidingMidpoint = 3, // sliding midpoint splitting method
            SlidingFair = 4,// sliding fair split method
            Suggest = 5     // the authors' suggestion for best
        };

        internal IPoint[] points;
        internal IKdNode rootNode;
        internal AxisAlignedBox boundingBox;

        protected KdTree() { }

        //dimension: Aus wie viel Komponenten(x,y,z,..) bestehen die Punkte jeweils
        public KdTree(IPoint[] points, int dimension, SplitRule splitRule = SplitRule.Suggest, int maxPointCountPerLeafNode = 10)
        {
            this.points = points;
            this.boundingBox = new AxisAlignedBox(points, dimension);

            KdSplitFunctions.SplittFunction splitFunctionHandler = GetSplitHandler(splitRule);

            this.rootNode = BuildKdTreeRecursive(points, this.boundingBox, maxPointCountPerLeafNode, splitFunctionHandler, 15);
        }

        internal static KdSplitFunctions.SplittFunction GetSplitHandler(SplitRule splitRule)
        {
            KdSplitFunctions.SplittFunction splitFunctionHandler = null;
            switch (splitRule)
            {
                case SplitRule.Standard:
                    splitFunctionHandler = KdSplitFunctions.MedianSplitFromLongestEdge;
                    break;
                case SplitRule.Midpoint:
                    splitFunctionHandler = KdSplitFunctions.MidpointSplitFromLongestEdge;
                    break;
                case SplitRule.Fair:
                    splitFunctionHandler = KdSplitFunctions.FairSplit;
                    break;
                case SplitRule.Suggest:
                case SplitRule.SlidingMidpoint:
                    splitFunctionHandler = KdSplitFunctions.SlidingMidpointSplitFromLongestEdge;
                    break;
                case SplitRule.SlidingFair:
                    splitFunctionHandler = KdSplitFunctions.SlidingFairSplit;
                    break;
                default:
                    throw new Exception("Splitfunction not available: " + splitRule.ToString());
            }
            return splitFunctionHandler;
        }

        private static IKdNode BuildKdTreeRecursive(IPoint[] points, AxisAlignedBox boundingBox, int maxPointCountPerLeafNode, KdSplitFunctions.SplittFunction splitFunctionHandler, int rekursionDeep)
        {
            if (points.Length <= maxPointCountPerLeafNode || rekursionDeep < 0)   // n small, make a leaf node
            {
                return new KdLeafNode(points, boundingBox.Dimension);
            }
            else
            {
                var splitResult = splitFunctionHandler(points, boundingBox);

                // save bounds for cutting dimension
                float lv = boundingBox.LowerBound[splitResult.CuttingDimension];
                float hv = boundingBox.UpperBound[splitResult.CuttingDimension];

                boundingBox.UpperBound[splitResult.CuttingDimension] = splitResult.CuttingValue;    // modify bounds for left subtree
                IKdNode leftNode = BuildKdTreeRecursive(splitResult.PoingsOnLeftSide, boundingBox, maxPointCountPerLeafNode, splitFunctionHandler, rekursionDeep - 1);
                boundingBox.UpperBound[splitResult.CuttingDimension] = hv;  // restore bounds

                boundingBox.LowerBound[splitResult.CuttingDimension] = splitResult.CuttingValue;
                IKdNode rightNode = BuildKdTreeRecursive(splitResult.PoingsOnRightSide, boundingBox, maxPointCountPerLeafNode, splitFunctionHandler, rekursionDeep - 1);
                boundingBox.LowerBound[splitResult.CuttingDimension] = lv;  // restore bounds

                return new KdSplitNode(splitResult.CuttingDimension, splitResult.CuttingValue, lv, hv, leftNode, rightNode);
            }
        }

        //----------------------------------------------------------------------
        //	Approximate nearest neighbor searching by kd-tree search
        //		The kd-tree is searched for an approximate nearest neighbor.
        //		The point is returned through one of the arguments, and the
        //		distance returned is the squared distance to this point.
        //
        //		The method used for searching the kd-tree is an approximate
        //		adaptation of the search algorithm described by Friedman,
        //		Bentley, and Finkel, ``An algorithm for finding best matches
        //		in logarithmic expected time,'' ACM Transactions on Mathematical
        //		Software, 3(3):209-226, 1977).
        //
        //		The algorithm operates recursively.  When first encountering a
        //		node of the kd-tree we first visit the child which is closest to
        //		the query point.  On return, we decide whether we want to visit
        //		the other child.  If the box containing the other child exceeds
        //		1/(1+eps) times the current best distance, then we skip it (since
        //		any point found in this child cannot be closer to the query point
        //		by more than this factor.)  Otherwise, we visit it recursively.
        //		The distance between a box and the query point is computed exactly
        //		(not approximated as is often done in kd-tree), using incremental
        //		distance updates, as described by Arya and Mount in ``Algorithms
        //		for fast vector quantization,'' Proc.  of DCC '93: Data Compression
        //		Conference, eds. J. A. Storer and M. Cohn, IEEE Press, 1993,
        //		381-390.
        //
        //		The main entry points is annkSearch() which sets things up and
        //		then call the recursive routine ann_search().  This is a recursive
        //		routine which performs the processing for one node in the kd-tree.
        //		There are two versions of this virtual procedure, one for splitting
        //		nodes and one for leaves.  When a splitting node is visited, we
        //		determine which child to visit first (the closer one), and visit
        //		the other child on return.  When a leaf is visited, we compute
        //		the distances to the points in the buckets, and update information
        //		on the closest points.
        //
        //		Some trickery is used to incrementally update the distance from
        //		a kd-tree rectangle to the query point.  This comes about from
        //		the fact that which each successive split, only one component
        //		(along the dimension that is split) of the squared distance to
        //		the child rectangle is different from the squared distance to
        //		the parent rectangle.
        //----------------------------------------------------------------------
        public IPoint[] ApproximateKNearestNeighborSearch(IPoint queryPoint,
                                                          int k,                // number of near neighbors to return
                                                          float eps = 0)        // the error bound
        {
            if (k > this.points.Length) throw new Exception("Requesting more near neighbors than data points");

            QueryAndResultDataForApproximateSearch data = new QueryAndResultDataForApproximateSearch()
            {
                QueryPoint = queryPoint,
                MaxTolerableSquaredError = (1 + eps) * (1 + eps),
                KClosestPoints = new SortedArray<IPoint>(k),
            };

            this.rootNode.ApproximateKNearestNeighborSearch(data, GetDistanceFromPointToBox(queryPoint, this.boundingBox));

            return data.KClosestPoints.GetAllValues().ToArray();
        }

        //https://www.cs.umd.edu/~mount/Papers/DCC.pdf  -> Hier wird Kd-Priority-Searching beschrieben
        //----------------------------------------------------------------------
        //	Approximate nearest neighbor searching by priority search.
        //		The kd-tree is searched for an approximate nearest neighbor.
        //		The point is returned through one of the arguments, and the
        //		distance returned is the SQUARED distance to this point.
        //
        //		The method used for searching the kd-tree is called priority
        //		search.  (It is described in Arya and Mount, ``Algorithms for
        //		fast vector quantization,'' Proc. of DCC '93: Data Compression
        //		Conference}, eds. J. A. Storer and M. Cohn, IEEE Press, 1993,
        //		381--390.)
        //
        //		The cell of the kd-tree containing the query point is located,
        //		and cells are visited in increasing order of distance from the
        //		query point.  This is done by placing each subtree which has
        //		NOT been visited in a priority queue, according to the closest
        //		distance of the corresponding enclosing rectangle from the
        //		query point.  The search stops when the distance to the nearest
        //		remaining rectangle exceeds the distance to the nearest point
        //		seen by a factor of more than 1/(1+eps). (Implying that any
        //		point found subsequently in the search cannot be closer by more
        //		than this factor.)
        //
        //		The main entry point is annkPriSearch() which sets things up and
        //		then call the recursive routine ann_pri_search().  This is a
        //		recursive routine which performs the processing for one node in
        //		the kd-tree.  There are two versions of this virtual procedure,
        //		one for splitting nodes and one for leaves. When a splitting node
        //		is visited, we determine which child to continue the search on
        //		(the closer one), and insert the other child into the priority
        //		queue.  When a leaf is visited, we compute the distances to the
        //		points in the buckets, and update information on the closest
        //		points.
        //
        //		Some trickery is used to incrementally update the distance from
        //		a kd-tree rectangle to the query point.  This comes about from
        //		the fact that which each successive split, only one component
        //		(along the dimension that is split) of the squared distance to
        //		the child rectangle is different from the squared distance to
        //		the parent rectangle.
        //----------------------------------------------------------------------
        public IPoint[] PriorityKNearestNeighborSearch(IPoint queryPoint,
                                                          int k,                // number of near neighbors to return
                                                          float eps = 0)        // the error bound
        {
            if (k > this.points.Length) throw new Exception("Requesting more near neighbors than data points");

            QueryAndResultDataForPrioritySearch data = new QueryAndResultDataForPrioritySearch()
            {
                QueryPoint = queryPoint,
                MaxTolerableSquaredError = (1 + eps) * (1 + eps),
                KClosestPoints = new SortedArray<IPoint>(k),
                PriorityQueueForBoxes = new PriorityQueue<IKdNode>(this.points.Length)
            };

            data.PriorityQueueForBoxes.Insert(GetDistanceFromPointToBox(queryPoint, this.boundingBox), this.rootNode); // insert root in priority queue

            while (data.PriorityQueueForBoxes.IsNonEmpty())
            {
                var closestBoxFromQueue = data.PriorityQueueForBoxes.ExtractMinimum();

                if (closestBoxFromQueue.Key * data.MaxTolerableSquaredError >= data.KClosestPoints.GetMaximumKey())
                    break;

                closestBoxFromQueue.Value.PriorityKNearestNeighborSearch(data, closestBoxFromQueue.Key);
            }

            return data.KClosestPoints.GetAllValues().ToArray();
        }

        //----------------------------------------------------------------------
        //	Approximate fixed-radius k nearest neighbor search
        //		The squared radius is provided, and this procedure finds the
        //		k nearest neighbors within the radius, and returns the total
        //		number of points lying within the radius.
        //
        //		The method used for searching the kd-tree is a variation of the
        //		nearest neighbor search used in kd_search.cpp, except that the
        //		radius of the search ball is known.  We refer the reader to that
        //		file for the explanation of the recursive search procedure.
        //----------------------------------------------------------------------
        public IPoint[] FixedRadiusSearchForKNearestNeighbors(IPoint queryPoint,
                                                          float searchRadius,   // Search Radius bound
                                                          int k,                // number of near neighbors to return
                                                          float eps = 0)        // the error bound
        {
            if (k > this.points.Length) throw new Exception("Requesting more near neighbors than data points");

            QueryAndResultDataForFixedRadiusSearch data = new QueryAndResultDataForFixedRadiusSearch()
            {
                QueryPoint = queryPoint,
                MaxTolerableSquaredError = (1 + eps) * (1 + eps),
                KClosestPoints = new SortedArray<IPoint>(k),
                SquaredSearchRadius = searchRadius * searchRadius
            };

            this.rootNode.FixedRadiusSearchForKNearestNeighbors(data, GetDistanceFromPointToBox(queryPoint, this.boundingBox));

            return data.KClosestPoints.GetAllValues().ToArray();
        }

        public List<IPoint> FixedRadiusSearch(IPoint queryPoint, float searchRadius)   // Search Radius bound                                              
        {
            QueryAndResultDataForFixedRadiusSearch1 data = new QueryAndResultDataForFixedRadiusSearch1()
            {
                QueryPoint = queryPoint,
                SearchRadius = searchRadius,
                SquaredSearchRadius = searchRadius * searchRadius
            };

            this.rootNode.FixedRadiusSearch(data);

            return data.ResultList;
        }



        //----------------------------------------------------------------------
        //	annBoxDistance - utility routine which computes distance from point to
        //		box (Note: most distances to boxes are computed using incremental
        //		distance updates, not this function.)
        //----------------------------------------------------------------------
        private float GetDistanceFromPointToBox(IPoint point, AxisAlignedBox box)
        {
            float distanceSum = 0;

            for (int d = 0; d < box.Dimension; d++)
            {
                if (point[d] < box.LowerBound[d])   // point is left of box
                {
                    float t = box.LowerBound[d] - point[d];
                    distanceSum += t * t;
                }
                else if (point[d] > box.UpperBound[d]) // point is right of box
                {
                    float t = point[d] - box.UpperBound[d];
                    distanceSum += t * t;
                }
            }

            return distanceSum;
        }
    }
}
