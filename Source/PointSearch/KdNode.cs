using System.Collections.Generic;
using RayTracerGlobal;

namespace PointSearch
{
    interface IKdNode
    {
        bool IsEmptyLeaf();
        void ApproximateKNearestNeighborSearch(QueryAndResultDataForApproximateSearch data, float boxDistance);
        void PriorityKNearestNeighborSearch(QueryAndResultDataForPrioritySearch data, float boxDistance);
        void FixedRadiusSearchForKNearestNeighbors(QueryAndResultDataForFixedRadiusSearch data, float boxDistance);
        void FixedRadiusSearch(QueryAndResultDataForFixedRadiusSearch1 data);
    }

    class QueryAndResultDataForApproximateSearch
    {
        //Query-Data
        public IPoint QueryPoint;
        public float MaxTolerableSquaredError;

        //Result-Data
        public SortedArray<IPoint> KClosestPoints;
    }

    class QueryAndResultDataForPrioritySearch : QueryAndResultDataForApproximateSearch
    {
        public PriorityQueue<IKdNode> PriorityQueueForBoxes;
    }

    class QueryAndResultDataForFixedRadiusSearch : QueryAndResultDataForApproximateSearch
    {
        public float SquaredSearchRadius;
    }

    class QueryAndResultDataForFixedRadiusSearch1
    {
        //Query-Data
        public IPoint QueryPoint;
        public float SearchRadius;
        public float SquaredSearchRadius;

        //Result-Data
        public List<IPoint> ResultList = new List<IPoint>();
    }

    class KdSplitNode : IKdNode
    {
        private int cuttingDimension;
        private float cuttingValue;
        private float lowerBound, upperBound; //Grenze ist entlang der cuttingDimension
        private IKdNode leftChild, rightChild;

        public KdSplitNode(int cuttingDimension, float cuttingValue, float lowerBound, float upperBound, IKdNode leftChild, IKdNode rightChild)
        {
            this.cuttingDimension = cuttingDimension;
            this.cuttingValue = cuttingValue;
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
            this.leftChild = leftChild;
            this.rightChild = rightChild;
        }

        public bool IsEmptyLeaf()
        {
            return false;
        }

        public void ApproximateKNearestNeighborSearch(QueryAndResultDataForApproximateSearch data, float boxDistance)
        {
            float cutDiff = data.QueryPoint[this.cuttingDimension] - this.cuttingValue; // distance to cutting plane

            if (cutDiff < 0)    // left of cutting plane
            {
                this.leftChild.ApproximateKNearestNeighborSearch(data, boxDistance); // visit closer child first

                float boxDiff = this.lowerBound - data.QueryPoint[this.cuttingDimension]; //boxDiff wird nur benötigt, um die boxDistance(Distanz zwischen Query-Punkt und diesen Split-Knoten) 
                if (boxDiff < 0)    // within bounds - ignore                             //in eine boxDistance umzurechnen, wo die Distanz zwischen den Qeury-Punkt und den Rechten Child-Knoten angezeigt wird
                    boxDiff = 0;

                //Vor dieser Zeile zeigt boxDistance vom Query-Punkt aus auf diesen SplitKnoten. Nach dieser Zeile zeigt es auf den Rechten Child-Knoten
                //boxDistance ist Summe aus lauter a*a + b*b + c*c + d*d ...Kompnonenten. 
                //Wenn ich nun die boxDistance nicht mehr auf diesen Splitknoten sondern auf sein rechtes Kind zeigen lassen will, dann ziehe ich zuerst 
                //die boxDiff-Komponenten ab (Wurde über Linke Kante vom diesen Split-Knoten berechnet) und sage nun, das cutDiff die neue linke Kante ist.
                boxDistance += cutDiff * cutDiff - boxDiff * boxDiff;

                // visit further child if close enough
                if (boxDistance * data.MaxTolerableSquaredError < data.KClosestPoints.GetMaximumKey())
                    this.rightChild.ApproximateKNearestNeighborSearch(data, boxDistance);
            }
            else  // right of cutting plane
            {
                this.rightChild.ApproximateKNearestNeighborSearch(data, boxDistance);    // visit closer child first

                float boxDiff = data.QueryPoint[this.cuttingDimension] - this.upperBound;
                if (boxDiff < 0)    // within bounds - ignore
                    boxDiff = 0;

                // distance to further box
                boxDistance += cutDiff * cutDiff - boxDiff * boxDiff;

                // visit further child if close enough
                if (boxDistance * data.MaxTolerableSquaredError < data.KClosestPoints.GetMaximumKey())
                    this.leftChild.ApproximateKNearestNeighborSearch(data, boxDistance);
            }
        }

        public void PriorityKNearestNeighborSearch(QueryAndResultDataForPrioritySearch data, float boxDistance)
        {
            float cutDiff = data.QueryPoint[this.cuttingDimension] - this.cuttingValue; // distance to cutting plane

            if (cutDiff < 0)    // left of cutting plane
            {
                float boxDiff = this.lowerBound - data.QueryPoint[this.cuttingDimension];
                if (boxDiff < 0)    // within bounds - ignore
                    boxDiff = 0;

                // distance to further box
                float newDistance = boxDistance + cutDiff * cutDiff - boxDiff * boxDiff;

                if (this.rightChild.IsEmptyLeaf() == false) // enqueue if not trivial
                    data.PriorityQueueForBoxes.Insert(newDistance, this.rightChild);

                this.leftChild.PriorityKNearestNeighborSearch(data, boxDistance);    // continue with closer child
            }
            else  // right of cutting plane
            {
                float boxDiff = data.QueryPoint[this.cuttingDimension] - this.upperBound;
                if (boxDiff < 0)    // within bounds - ignore
                    boxDiff = 0;

                // distance to further box
                float newDistance = boxDistance + cutDiff * cutDiff - boxDiff * boxDiff;

                if (this.leftChild.IsEmptyLeaf() == false) // enqueue if not trivial
                    data.PriorityQueueForBoxes.Insert(newDistance, this.leftChild);

                this.rightChild.PriorityKNearestNeighborSearch(data, boxDistance);    // continue with closer child
            }
        }

        public void FixedRadiusSearchForKNearestNeighbors(QueryAndResultDataForFixedRadiusSearch data, float boxDistance)
        {
            float cutDiff = data.QueryPoint[this.cuttingDimension] - this.cuttingValue; // distance to cutting plane

            if (cutDiff < 0)    // left of cutting plane
            {
                this.leftChild.FixedRadiusSearchForKNearestNeighbors(data, boxDistance); // visit closer child first

                float boxDiff = this.lowerBound - data.QueryPoint[this.cuttingDimension];
                if (boxDiff < 0)    // within bounds - ignore
                    boxDiff = 0;

                // distance to further box
                boxDistance += cutDiff * cutDiff - boxDiff * boxDiff;

                // visit further child if close enough
                if (boxDistance * data.MaxTolerableSquaredError < data.KClosestPoints.GetMaximumKey())
                    this.rightChild.FixedRadiusSearchForKNearestNeighbors(data, boxDistance);
            }
            else  // right of cutting plane
            {
                this.rightChild.FixedRadiusSearchForKNearestNeighbors(data, boxDistance);    // visit closer child first

                float boxDiff = data.QueryPoint[this.cuttingDimension] - this.upperBound;
                if (boxDiff < 0)    // within bounds - ignore
                    boxDiff = 0;

                // distance to further box
                boxDistance += cutDiff * cutDiff - boxDiff * boxDiff;

                // visit further child if close enough
                if (boxDistance * data.MaxTolerableSquaredError < data.KClosestPoints.GetMaximumKey())
                    this.leftChild.FixedRadiusSearchForKNearestNeighbors(data, boxDistance);
            }
        }

        public void FixedRadiusSearch(QueryAndResultDataForFixedRadiusSearch1 data)
        {
            float cutDiff = data.QueryPoint[this.cuttingDimension] - this.cuttingValue; // distance to cutting plane

            if (cutDiff < 0)    // left of cutting plane
            {
                if (cutDiff < -data.SearchRadius)
                    this.leftChild.FixedRadiusSearch(data); // visit closer child first
                else
                {
                    this.leftChild.FixedRadiusSearch(data);
                    this.rightChild.FixedRadiusSearch(data);
                }
            }
            else  // right of cutting plane
            {
                if (cutDiff > data.SearchRadius)
                    this.rightChild.FixedRadiusSearch(data);    // visit closer child first
                else
                {
                    this.leftChild.FixedRadiusSearch(data);
                    this.rightChild.FixedRadiusSearch(data);
                }
            }
        }
    }

    class KdLeafNode : IKdNode
    {
        private IPoint[] points;
        private int dimension;  //Jede Punkt aus der 'points'-List hat die Dimension 'dimension' (Wenn 'points' also ein Liste von 2D-Punkten ist, dann steht hier eine 2)

        public KdLeafNode(IPoint[] points, int dimension)
        {
            this.points = points;
            this.dimension = dimension;
        }

        public bool IsEmptyLeaf()
        {
            return this.points.Length == 0;
        }

        public void ApproximateKNearestNeighborSearch(QueryAndResultDataForApproximateSearch data, float boxDistance)
        {
            float minDist = data.KClosestPoints.GetMaximumKey();    // k-th smallest distance so far

            for (int i = 0; i < this.points.Length; i++)            // check points in bucket
            {
                IPoint pp = this.points[i];
                IPoint qq = data.QueryPoint;
                float distanceToDataPoint = 0;
                int d;

                for (d = 0; d < this.dimension; d++)
                {
                    float t = qq[d] - pp[d];
                    distanceToDataPoint += t * t;
                    if (distanceToDataPoint > minDist) break;       // exceeds dist to k-th smallest?
                }

                if (d >= this.dimension)                                 // among the k best?
                {
                    data.KClosestPoints.Insert(distanceToDataPoint, this.points[i]);
                    minDist = data.KClosestPoints.GetMaximumKey();
                }
            }
        }

        public void PriorityKNearestNeighborSearch(QueryAndResultDataForPrioritySearch data, float boxDistance)
        {
            ApproximateKNearestNeighborSearch(data, boxDistance);
        }

        public void FixedRadiusSearchForKNearestNeighbors(QueryAndResultDataForFixedRadiusSearch data, float boxDistance)
        {
            for (int i = 0; i < this.points.Length; i++)            // check points in bucket
            {
                IPoint pp = this.points[i];
                IPoint qq = data.QueryPoint;
                float distanceToDataPoint = 0;
                int d;

                for (d = 0; d < this.dimension; d++)
                {
                    float t = qq[d] - pp[d];
                    distanceToDataPoint += t * t;
                    if (distanceToDataPoint > data.SquaredSearchRadius) break;       // exceeds dist to k-th smallest?
                }

                if (d >= this.dimension)                                 // among the k best?
                {
                    data.KClosestPoints.Insert(distanceToDataPoint, this.points[i]);
                }
            }
        }

        public void FixedRadiusSearch(QueryAndResultDataForFixedRadiusSearch1 data)
        {
            for (int i = 0; i < this.points.Length; i++)            // check points in bucket
            {
                IPoint pp = this.points[i];
                IPoint qq = data.QueryPoint;
                float distanceToDataPoint = 0;
                int d;

                for (d = 0; d < this.dimension; d++)
                {
                    float t = qq[d] - pp[d];
                    distanceToDataPoint += t * t;
                    if (distanceToDataPoint > data.SquaredSearchRadius) break;       // exceeds dist to k-th smallest?
                }

                if (d >= this.dimension)                                 // among the k best?
                {
                    data.ResultList.Add(this.points[i]);
                }
            }
        }
    }
}
