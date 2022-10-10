using GraphicMinimal;

namespace GraphicGlobal
{
    public class Plane //Ebene, wie man sie ausm Mathematikunterricht kennt
    {
        //Ebenengleichung: Für alle Punkte P(x,y,z) der Ebene gilt: A*x + B*y + C*z + D = 0
        public float A { get; private set; }
        public float B { get; private set; }
        public float C { get; private set; }
        public float D { get; private set; }


        public Vector3D Normal //Normalvektor der Ebene
        {
            get { return new Vector3D(A, B, C); }
        }

        public Plane(float a, float b, float c, float d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public Plane(Vector3D p1, Vector3D p2, Vector3D p3)
        {
            Vector3D N = Vector3D.Normalize(Vector3D.Cross(p2 - p1, p3 - p1));
            this.A = N.X;
            this.B = N.Y;
            this.C = N.Z;
            this.D = -N * p1;
        }

        public Plane(Vector3D normal, Vector3D pointOnPlane)//normal steht auf P
        {
            this.A = normal.X;
            this.B = normal.Y;
            this.C = normal.Z;
            this.D = -normal * pointOnPlane;
        }

        public Vector3D GetIntersectionPointWithRay(Ray ray)
        {
            float mr_proper = this.A * ray.Direction.X + this.B * ray.Direction.Y + this.C * ray.Direction.Z;
            if (mr_proper == 0) return null;
            float f = this.A * ray.Start.X + this.B * ray.Start.Y + this.C * ray.Start.Z + this.D;
            float distance = (-f) / mr_proper;
            if (distance < 0) return null;
            return ray.Start + ray.Direction * distance;
        }

        public Vector3D GetIntersectionPointWithRay(Ray ray, out float distance)
        {
            float mr_proper = this.A * ray.Direction.X + this.B * ray.Direction.Y + this.C * ray.Direction.Z;
            if (mr_proper == 0) { distance = float.NaN; return null; }
            float f = this.A * ray.Start.X + this.B * ray.Start.Y + this.C * ray.Start.Z + this.D;
            distance = (-f) / mr_proper;
            if (distance < 0) return null;
            return ray.Start + ray.Direction * distance;
        }

        public bool IsPointAbovePlane(Vector3D point)
        {
            return A * point.X +
                   B * point.Y +
                   C * point.Z +
                   D > 0;
        }

        //Der point wird senkrecht zur Ebene prozetziert
        public float GetOrthogonalDistanceFromPointToPlane(Vector3D point)
        {
            return (this.Normal * point + this.D) / this.Normal.SquareLength();
        }
    }
}
