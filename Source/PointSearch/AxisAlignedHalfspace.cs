using RayTracerGlobal;

namespace PointSearch
{
    //----------------------------------------------------------------------
    //	Orthogonal (axis aligned) halfspace
    //	An orthogonal halfspace is represented by an integer cutting
    //	dimension cd, coordinate cutting value, cv, and side, sd, which is
    //	either +1 or -1. Our convention is that point q lies in the (closed)
    //	halfspace if (q[cd] - cv)*sd >= 0.
    //----------------------------------------------------------------------
    class AxisAlignedHalfspace
    {
        public enum Side { Right = +1, Left = -1 };

        private int cuttingDimension;
        private float cuttingValue;
        private Side side;          //An welcher Seite vom CuttingValue aus betrachtet liegt der Halfspace?

        public AxisAlignedHalfspace(int cuttingDimension, float cuttingValue, Side side)
        {
            this.cuttingDimension = cuttingDimension;
            this.cuttingValue = cuttingValue;
            this.side = side;
        }

        public bool IsPointInside(IPoint point)
        {
            return (point[this.cuttingDimension] - this.cuttingValue) * (int)this.side >= 0;
        }

        public bool IsPointOutside(IPoint point)
        {
            return (point[this.cuttingDimension] - this.cuttingValue) * (int)this.side < 0;
        }

        public float GetSquaredDistanceFromPoint(IPoint point)
        {
            float t = point[this.cuttingDimension] - this.cuttingValue;
            return t * t;
        }

        public void SetLowerBound(int dimension, IPoint setPoint)
        {
            this.cuttingDimension = dimension;
            this.cuttingValue = setPoint[dimension];
            this.side = Side.Right;
        }

        public void SetUpperBound(int dimension, IPoint setPoint)
        {
            this.cuttingDimension = dimension;
            this.cuttingValue = setPoint[dimension];
            this.side = Side.Left;
        }

        public void ProjectPointOntoHalfspace(Point point)
        {
            if (IsPointOutside(point)) point[this.cuttingDimension] = this.cuttingValue;
        }
    }
}
