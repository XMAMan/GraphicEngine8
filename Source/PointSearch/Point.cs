using System.Linq;
using RayTracerGlobal;

namespace PointSearch
{
    class Point : IPoint
    {
        private float[] data;

        public float this[int dimension]
        {
            get
            {
                return this.data[dimension];
            }
            set
            {
                this.data[dimension] = value;
            }
        }

        public int Length
        {
            get
            {
                return this.data.Length;
            }
        }

        public Point GetCopy()
        {
            return new Point(this.data.Clone() as float[]);
        }

        public Point(int dimension)
        {
            this.data = new float[dimension];
        }

        public Point(float[] data)
        {
            this.data = data;
        }

        public Point(float x)
        {
            this.data = new float[] { x };
        }

        public Point(float x, float y)
        {
            this.data = new float[] { x, y };
        }

        public Point(float x, float y, float z)
        {
            this.data = new float[] { x, y, z };
        }

        public override string ToString()
        {
            return "[" + string.Join(", ", this.data.Select(x => (int)x)) + "]";
        }
    }
}
