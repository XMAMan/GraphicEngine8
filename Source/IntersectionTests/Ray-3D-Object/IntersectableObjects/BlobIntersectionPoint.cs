using GraphicMinimal;
using GraphicGlobal;

namespace IntersectionTests.Ray_3D_Object.IntersectableObjects
{
    public class BlobIntersectionPoint : SimpleIntersectionPoint, IIntersectionPointSimple
    {
        public Vertex Point { get; private set; }
        public BlobIntersectionPoint(IIntersecableObject blob, Vector3D position, float distanceToRayStart, Vertex vertexPoint, Vector3D rayDirection)
            : base(blob, position, distanceToRayStart, null, rayDirection)
        {
            this.Point = vertexPoint;
        }
    }
}
