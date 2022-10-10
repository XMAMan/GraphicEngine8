using GraphicMinimal;
using GraphicGlobal;

namespace IntersectionTests.Ray_3D_Object.IntersectableObjects
{
    public class SphereIntersectionPoint : SimpleIntersectionPoint, IIntersectionPointSimple
    {
        public Vector3D Normal { get; private set; }

        public SphereIntersectionPoint(IIntersecableObject sphere, Vector3D position, float distanceToRayStart, ParallaxPoint parallaxPoint, Vector3D rayDirection, Vector3D normal)
            : base(sphere, position, distanceToRayStart, parallaxPoint, rayDirection)
        {
            this.Normal = normal;
        }
    }
}
