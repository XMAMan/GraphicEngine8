using GraphicMinimal;
using GraphicGlobal;

namespace IntersectionTests.Ray_3D_Object.IntersectableObjects
{
    public class SimpleIntersectionPoint : IIntersectionPointSimple
    {
        public IIntersecableObject IntersectedObject { get; private set; }
        public Vector3D Position { get; private set; }
        public float DistanceToRayStart { get; private set; }
        public ParallaxPoint ParallaxPoint { get; set; }         //Hier steht nur was drin, wenn Parallaxmapping verwendet wird
        public Vector3D RayDirection { get; private set; }

        public SimpleIntersectionPoint(IIntersecableObject intersectedObject, Vector3D position, float distanceToRayStart, ParallaxPoint parallaxPoint, Vector3D rayDirection)
        {
            this.IntersectedObject = intersectedObject;
            this.Position = position;
            this.DistanceToRayStart = distanceToRayStart;
            this.ParallaxPoint = parallaxPoint;
            this.RayDirection = rayDirection;
        }
    }
}
