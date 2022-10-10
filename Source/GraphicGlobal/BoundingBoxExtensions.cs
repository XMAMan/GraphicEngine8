using GraphicMinimal;

namespace GraphicGlobal
{
    //Eine BoundingBox hat nicht zwangsweise was mit Strahlen zu tun. Deswegen benutze ich die Extension im GraphicGlobal-Namespace
    public static class BoundingBoxExtensions
    {
        //Diese Funktion hier ist fehlerhaft. Bitte nicht verwenden!
        //Quelle: http://www.flipcode.com/archives/Raytracing_Topics_Techniques-Part_7_Kd-Trees_and_More_Speed.shtml    -> Raytracer.cpp
        public static bool ClipRayWithBoundingBox_(this BoundingBox box, Ray ray, out float tnear, out float tfar)
        {
            tnear = 0;
            tfar = float.MaxValue;

            Vector3D p1 = box.Min;
            Vector3D p2 = box.Max;
            Vector3D D = ray.Direction, O = ray.Start;
            for (int i = 0; i < 3; i++) if (D[i] < 0)
                {
                    if (O[i] < p1[i]) return false;
                }
                else if (O[i] > p2[i]) return false;

            //Hier in der Clip-Funktion ist ein Fehler, welcher durch KDSahBaumTest.GetIntersectionPoint_CalledForSpezialRay2_ReturnsIntersectionPoint aufgedeckt wird
            // clip ray segment to box
            for (int i = 0; i < 3; i++)
            {
                float pos = O[i] + tfar * D[i];
                if (D[i] < 0)
                {
                    // clip end point
                    if (pos < p1[i]) tfar = tnear + (tfar - tnear) * ((O[i] - p1[i]) / (O[i] - pos));
                    // clip start point
                    if (O[i] > p2[i]) tnear += (tfar - tnear) * ((O[i] - p2[i]) / (tfar * D[i]));
                }
                else
                {
                    // clip end point
                    if (pos > p2[i]) tfar = tnear + (tfar - tnear) * ((p2[i] - O[i]) / (pos - O[i]));
                    // clip start point
                    if (O[i] < p1[i]) tnear += (tfar - tnear) * ((p1[i] - O[i]) / (tfar * D[i]));
                }
                if (tnear > tfar) return false;
            }

            return true;
        }


        //Quelle: http://www.scratchapixel.com/code.php?id=10&origin=/lessons/3d-basic-rendering/ray-tracing-rendering-simple-shapes&src=1
        //Gibt false zurück, wenn kein Schnittpunkt. Ansonsten gibt es die Ein- und Austrittspunkt
        public static bool ClipRayWithBoundingBox(this BoundingBox box, Ray ray, out float tmin, out float tmax)
        {
            Vector3D[] bounds = new Vector3D[] { box.Min, box.Max };
            float[] invdir = new float[] { 1.0f / ray.Direction.X, 1.0f / ray.Direction.Y, 1.0f / ray.Direction.Z };
            int[] sign = new int[]
            {
                invdir[0] < 0 ? 1 : 0,
                invdir[1] < 0 ? 1 : 0,
                invdir[2] < 0 ? 1 : 0,
            };

            float tymin, tymax, tzmin, tzmax;

            tmin = (bounds[sign[0]].X - ray.Start.X) * invdir[0];
            tmax = (bounds[1 - sign[0]].X - ray.Start.X) * invdir[0];
            tymin = (bounds[sign[1]].Y - ray.Start.Y) * invdir[1];
            tymax = (bounds[1 - sign[1]].Y - ray.Start.Y) * invdir[1];

            if ((tmin > tymax) || (tymin > tmax))
                return false;

            if (tymin > tmin || float.IsNaN(tmin))
                tmin = tymin;
            if (tymax < tmax || float.IsNaN(tmax))
                tmax = tymax;

            tzmin = (bounds[sign[2]].Z - ray.Start.Z) * invdir[2];
            tzmax = (bounds[1 - sign[2]].Z - ray.Start.Z) * invdir[2];

            if ((tmin > tzmax) || (tzmin > tmax))
                return false;

            if (tzmin > tmin || float.IsNaN(tmin))
                tmin = tzmin;
            if (tzmax < tmax || float.IsNaN(tmax))
                tmax = tzmax;

            return true;
        }
        //Erklärung zum Tema 'Schnittpunkttest zwischen Strahl und AABB'
        //http://www0.cs.ucl.ac.uk/staff/j.kautz/teaching/3080/Slides/16_FastRaytrace.pdf
        //Eine AABB besteht aus lauter parallelen Ebenen
        //Ray ist definiert über q(t)=q0 + t*d
        //-Berechne tNear für jede Ebene
        //-Finde maximales tNear
        //-Berechne tFar für jede Ebene
        //-Finde minimales tFar
        //-Wenn max-TNear größer als Min-tFar, dann wird die Box nicht getroffen
    }
}
