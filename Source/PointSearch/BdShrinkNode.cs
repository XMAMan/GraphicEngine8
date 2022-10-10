namespace PointSearch
{
    class BdShrinkNode : IKdNode
    {
        private AxisAlignedHalfspace[] boundingHalfspaces;
        private IKdNode inChild, outChild;

        public BdShrinkNode(AxisAlignedHalfspace[] boundingHalfspaces, IKdNode inChild, IKdNode outChild)
        {
            this.boundingHalfspaces = boundingHalfspaces;
            this.inChild = inChild;
            this.outChild = outChild;
        }

        public bool IsEmptyLeaf()
        {
            return false;
        }

        public void ApproximateKNearestNeighborSearch(QueryAndResultDataForApproximateSearch data, float boxDistance)
        {
            float distanceToInnerBox = 0;
            foreach (var innerBoxSide in this.boundingHalfspaces)
            {
                if (innerBoxSide.IsPointOutside(data.QueryPoint))
                {
                    distanceToInnerBox += innerBoxSide.GetSquaredDistanceFromPoint(data.QueryPoint);
                }
            }

            if (distanceToInnerBox <= boxDistance) // if inner box is closer
            {
                this.inChild.ApproximateKNearestNeighborSearch(data, distanceToInnerBox);   // search inner child first
                this.outChild.ApproximateKNearestNeighborSearch(data, boxDistance);         // ...then outer child
            }
            else // if outer box is closer
            {
                this.outChild.ApproximateKNearestNeighborSearch(data, boxDistance);         // search outer child first
                this.inChild.ApproximateKNearestNeighborSearch(data, distanceToInnerBox);   // ...then outer child
            }
        }

        public void PriorityKNearestNeighborSearch(QueryAndResultDataForPrioritySearch data, float boxDistance)
        {
            float distanceToInnerBox = 0;
            foreach (var boxSide in this.boundingHalfspaces)
            {
                if (boxSide.IsPointOutside(data.QueryPoint))
                {
                    distanceToInnerBox += boxSide.GetSquaredDistanceFromPoint(data.QueryPoint);
                }
            }

            if (distanceToInnerBox <= boxDistance) // if inner box is closer
            {
                if (this.outChild.IsEmptyLeaf() == false) // enqueue outer if not trivial
                    data.PriorityQueueForBoxes.Insert(boxDistance, this.outChild);

                this.inChild.PriorityKNearestNeighborSearch(data, distanceToInnerBox);   // continue with inner child

            }
            else // if outer box is closer
            {
                if (this.inChild.IsEmptyLeaf() == false) // enqueue inner if not trivial
                    data.PriorityQueueForBoxes.Insert(distanceToInnerBox, this.inChild);

                this.outChild.PriorityKNearestNeighborSearch(data, boxDistance);        // continue with outer child
            }
        }

        public void FixedRadiusSearchForKNearestNeighbors(QueryAndResultDataForFixedRadiusSearch data, float boxDistance)
        {
            float distanceToInnerBox = 0;
            foreach (var box in this.boundingHalfspaces)
            {
                if (box.IsPointOutside(data.QueryPoint))
                {
                    distanceToInnerBox += box.GetSquaredDistanceFromPoint(data.QueryPoint);
                }
            }

            if (distanceToInnerBox <= boxDistance) // if inner box is closer
            {
                this.inChild.FixedRadiusSearchForKNearestNeighbors(data, distanceToInnerBox);   // search inner child first
                this.outChild.FixedRadiusSearchForKNearestNeighbors(data, boxDistance);         // ...then outer child
            }
            else // if outer box is closer
            {
                this.outChild.FixedRadiusSearchForKNearestNeighbors(data, boxDistance);         // search outer child first
                this.inChild.FixedRadiusSearchForKNearestNeighbors(data, distanceToInnerBox);   // ...then outer child
            }
        }

        public void FixedRadiusSearch(QueryAndResultDataForFixedRadiusSearch1 data)
        {
            this.inChild.FixedRadiusSearch(data);   // search inner child first
            this.outChild.FixedRadiusSearch(data);         // ...then outer child
        }
    }
}
