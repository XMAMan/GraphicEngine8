using RayTracerGlobal;

namespace PointSearch
{
    class AxisAlignedBox
    {
        public Point LowerBound { get; private set; }
        public Point UpperBound { get; private set; }

        public int Dimension
        {
            get
            {
                return this.LowerBound.Length;
            }
        }

        public AxisAlignedBox GetCopy()
        {
            return new AxisAlignedBox(this.LowerBound.GetCopy(), this.UpperBound.GetCopy());
        }

        private AxisAlignedBox(Point lowerBound, Point upperBound)
        {
            this.LowerBound = lowerBound;
            this.UpperBound = upperBound;
        }

        // find smallest enclosing rectangle
        public AxisAlignedBox(IPoint[] points, int dimensionOfSpace)
        {
            Point lowerBound = new Point(dimensionOfSpace);
            Point upperBound = new Point(dimensionOfSpace);

            for (int d = 0; d < dimensionOfSpace; d++)
            {
                float min = float.MaxValue;
                float max = float.MinValue;
                for (int i = 0; i < points.Length; i++)
                {
                    float f = points[i][d];
                    if (f < min) min = f;
                    if (f > max) max = f;
                }
                lowerBound[d] = min;
                upperBound[d] = max;
            }

            this.LowerBound = lowerBound;
            this.UpperBound = upperBound;
        }

        public float GetEdgeLength(int dimension)
        {
            return this.UpperBound[dimension] - this.LowerBound[dimension];
        }

        public int GetAxisIndizeFromLongestEdge()
        {
            int maxDimension = -1;
            float maxEdgeLength = -1;
            for (int d = 0; d < this.UpperBound.Length; d++)
            {
                float length = GetEdgeLength(d);
                if (length > maxEdgeLength)
                {
                    maxEdgeLength = length;
                    maxDimension = d;
                }
            }

            return maxDimension;
        }

        public float GetLongestEdgeLength()
        {
            return GetEdgeLength(GetAxisIndizeFromLongestEdge());
        }

        public bool IsPointInside(IPoint point)
        {
            for (int i = 0; i < this.Dimension; i++)
            {
                if (point[i] < this.LowerBound[i] || point[i] > this.UpperBound[i]) return false;
            }

            return true;
        }
    }
}
