using GraphicMinimal;
using GraphicGlobal;

namespace Rasterizer
{
    class TriangleWithNeighborReferences : Triangle
    {
        public bool Visible;
        public TriangleWithNeighborReferences[] Neighbors = new TriangleWithNeighborReferences[] { null, null, null };

        private Plane trianglePlane;

        public TriangleWithNeighborReferences(Triangle triangle)
            :base(triangle)
        {
            this.trianglePlane = triangle.GetTrianglePlane();
        }

        public bool IsPointAboveTrianglePlane(Vector3D point)
        {
            return this.trianglePlane.IsPointAbovePlane(point);
        }
    }
}
