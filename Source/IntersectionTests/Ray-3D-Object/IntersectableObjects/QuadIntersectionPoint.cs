using GraphicGlobal;
using GraphicMinimal;

namespace IntersectionTests.Ray_3D_Object.IntersectableObjects
{
    public class QuadIntersectionPoint : SimpleIntersectionPoint
    {
        public float F1 { get; private set; } //Edge 1
        public float F2 { get; private set; } //Edge 2

        public QuadIntersectionPoint(IIntersecableObject quad, Vector3D position, float distanceToRayStart, ParallaxPoint vertexPoint, Vector3D rayDirection, float f1, float f2)
            : base(quad, position, distanceToRayStart, vertexPoint, rayDirection)
        {
            this.F1 = f1;
            this.F2 = f2;
        }
    }
}
