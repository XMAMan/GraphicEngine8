using GraphicGlobal;
using System.Collections.Generic;
using System.Linq;

namespace GraphicPipelineCPU.PipelineHelper
{
    class TriangleArrayCollection
    {
        private Dictionary<int, Triangle[]> triangleArrays = new Dictionary<int, Triangle[]>(); //TriangleArray-ID | Daten

        public Triangle[] this[int id]
        {
            get
            {
                return this.triangleArrays[id];
            }
        }

        public int AddTriangleArray(Triangle[] data)
        {
            int triangleArrayID = 1;

            if (this.triangleArrays.Count > 0)
            {
                triangleArrayID = this.triangleArrays.Keys.Max() + 1;
            }

            this.triangleArrays.Add(triangleArrayID, data);

            return triangleArrayID;
        }

        public void TryToRemoveTriangleArray(int triangleArrayId)
        {
            if (this.triangleArrays.ContainsKey(triangleArrayId))
                this.triangleArrays.Remove(triangleArrayId);
        }
    }
}
