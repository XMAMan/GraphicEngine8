using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using GraphicGlobal;

namespace IntersectionTests
{
    public static class IntersectionHelper
    {
        public static BoundingBox GetBoundingBoxFromSzene(IntersectionFinder intersectionFinder, MediaIntersectionFinder mediaIntersectionFinder)
        {
            if (mediaIntersectionFinder != null) return mediaIntersectionFinder.GetBoundingBoxFromScene();
            return intersectionFinder.GetBoundingBoxFromSzene();
        }

        public static BoundingBox GetBoundingBoxFromIVolumeObjektCollection(IEnumerable<IIntersecableObject> volumeObjects)
        {
            Vector3D min = new Vector3D(volumeObjects.Min(x => x.MinPoint.X),
                                    volumeObjects.Min(x => x.MinPoint.Y),
                                    volumeObjects.Min(x => x.MinPoint.Z));

            Vector3D max = new Vector3D(volumeObjects.Max(x => x.MaxPoint.X),
                                    volumeObjects.Max(x => x.MaxPoint.Y),
                                    volumeObjects.Max(x => x.MaxPoint.Z));

            return new BoundingBox(min, max);
        }

        //liefert true, wenn Strahl die Kugel schneidet, sonst false
        public static bool IsAnyIntersectionPointBetweenRayAndSphere(Ray ray, Vector3D sphereCenter, float sphereRadiusSquared)
        {
            Vector3D V = ray.Start - sphereCenter;
            float a, b, c;
            a = ray.Direction * ray.Direction;
            b = 2 * (V * ray.Direction);
            c = V * V - sphereRadiusSquared;

            return (b * b - 4 * a * c) >= 0;
        }

        //Quelle für Formel, um Schnittpunkt(e) zwischen Strahl und Kugel auszurechnen: http://www.matheboard.de/archive/452217/thread.html
        //liefert true, wenn Strahl die Kugel schneidet, sonst false
        public static bool IsAnyIntersectionPointBettweenLineAndSphere(Vector3D P1, Vector3D P2, Vector3D sphereCenter, float sphereRadius)
        {
            Vector3D b = P2 - P1;
            float bQuad = b.SquareLength();
            float squareBelow = sphereRadius * sphereRadius * bQuad - Vector3D.Cross(P1 - sphereCenter, b).SquareLength();
            if (squareBelow < 0) return false;
            float tFirst = (sphereCenter - P1) * b;
            float tLast = (float)Math.Sqrt(squareBelow);
            float t1 = (tFirst + tLast) / bQuad;
            float t2 = (tFirst - tLast) / bQuad;
            if (t1 >= 0 && t1 <= 1 || t2 >= 0 && t2 <= 1) return true;

            return false; // Kein Schnittpunkt
        }


        public static Vector3D GetIntersectionPointBetweenRayAndSphere(Ray ray, Vector3D center, float radius)
        {
            float distance = GetIntersectionPointDistanceBetweenRayAndSphere(ray, center, radius);
            if (float.IsNaN(distance)) return null;
            return ray.Start + ray.Direction * distance;
        }

        public static List<float> GetAllIntersectionPointDistancesBetweenRayAndSphere(Ray ray, Vector3D sphereCenter, float radius)
        {
            double radiusSquared = radius * radius;
            double Vx = ray.Start.X - sphereCenter.X;
            double Vy = ray.Start.Y - sphereCenter.Y;
            double Vz = ray.Start.Z - sphereCenter.Z;
            double a, b, c;
            a = ray.Direction.X * ray.Direction.X + ray.Direction.Y * ray.Direction.Y + ray.Direction.Z * ray.Direction.Z;
            b = 2 * (Vx * ray.Direction.X + Vy * ray.Direction.Y + Vz * ray.Direction.Z);
            c = (Vx * Vx + Vy * Vy + Vz * Vz) - radiusSquared;

            int roots = IntersectionHelper.CalcQuadricRoots(a, b, c, out double t1, out double t2);

            List<float> distanceList = new List<float>();
            if (roots > 0)
            {
                if (t1 > 0) distanceList.Add((float)t1);
                if (t2 > 0) distanceList.Add((float)t2);
            }

            return distanceList;
        }

        //Intern wird hier mit double-Genauigkeit gearbeitet
        public static float GetIntersectionPointDistanceBetweenRayAndSphere(Ray ray, Vector3D sphereCenter, float radius)
        {
            double radiusSquared = radius * radius;
            double Vx = ray.Start.X - sphereCenter.X;
            double Vy = ray.Start.Y - sphereCenter.Y;
            double Vz = ray.Start.Z - sphereCenter.Z;
            double a, b, c;
            a = ray.Direction.X * ray.Direction.X + ray.Direction.Y * ray.Direction.Y + ray.Direction.Z * ray.Direction.Z;
            b = 2 * (Vx * ray.Direction.X + Vy * ray.Direction.Y + Vz * ray.Direction.Z);
            c = (Vx * Vx + Vy * Vy + Vz * Vz)- radiusSquared;

            int roots = IntersectionHelper.CalcQuadricRoots(a, b, c, out double t1, out double t2);

            if (roots > 0 && (t1 > 0.01f || t2 > 0.0001f))// kleinsterpositiver pos. Wert aus t1, t2
            {
                if (t1 < 0.0001f) t1 = float.MaxValue;
                if (t2 < 0.0001f) t2 = float.MaxValue;
                float distance = t1 < t2 ? (float)t1 : (float)t2;

                return distance;
            }

            return float.NaN;
        }

        private static int CalcQuadricRoots(double a, double b, double c, out double x1, out double x2)
        {
            double determinant = b * b - 4 * a * c;
            if (determinant < 0)
            {
                x1 = 0.0;
                x2 = 0.0;
                return 0;
            }
            determinant = Math.Sqrt(determinant);
            double psign = 1;
            if (b < 0) psign = -1;
            double q = -0.5f * (b + psign * determinant);
            x1 = q / a;
            x2 = c / q;
            // Sort by value
            if (x1 > x2)
            {
                q = x2; x2 = x1; x1 = q;
            }
            return x1 == x2 ? 1 : 2;
        }

        //Den Schnittpunkt erhalte ich durch p1 + direcdtion1 * t1      oder p2 + direction2 * t2
        public static void GetDistanceToIntersectionPoint(Vector2D p1, Vector2D direction1, Vector2D p2, Vector2D direction2, out float t1, out float t2)
        {
            Vector2D V = direction1;
            Vector2D L = direction2;
            Vector2D C = p2 - p1;

            t2 = (V.Y * C.X / V.X - C.Y) / (L.Y - L.X * V.Y / V.X);
            if (float.IsNaN(t2) || float.IsInfinity(t2))
            {
                t2 = (C.Y * V.X / V.Y - C.X) / (L.X + L.Y * V.X / V.Y);
            }

            t1 = (C.X * L.Y / L.X - C.Y) / (V.X * L.Y / L.X - V.Y);
            if (float.IsNaN(t1) || float.IsInfinity(t1))
            {
                t1 = (C.Y * L.X / L.Y - C.X) / (V.Y * L.X / L.Y - V.X);
            }
        }
    }
}
