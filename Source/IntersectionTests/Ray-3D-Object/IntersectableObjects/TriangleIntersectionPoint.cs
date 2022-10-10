using GraphicMinimal;
using GraphicGlobal;

namespace IntersectionTests.Ray_3D_Object.IntersectableObjects
{
    //Datenklasse, welche nur die Daten enthält, welche bei der KD-Baum-Abfrage erstellt werden
    public class TriangleIntersectionPoint : SimpleIntersectionPoint, IIntersectionPointSimple
    {
        public float Alpha { get; private set; }
        public float Beta { get; private set; }
        public float Gamma { get; private set; }

        public TriangleIntersectionPoint(IIntersecableObject triangle, Vector3D position, float distanceToRayStart, ParallaxPoint parallaxPoint, Vector3D rayDirection, float alpha, float beta, float gamma)
            :base(triangle, position, distanceToRayStart, parallaxPoint, rayDirection)
        {
            this.Alpha = alpha;
            this.Beta = beta;
            this.Gamma = gamma;
        }
    }
}
