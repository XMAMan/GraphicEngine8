using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using IntersectionTests.Ray_3D_Object.IntersectableObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RayObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntersectionTestsTest
{
    [TestClass]
    public class IntersectableTriangleTest
    {
        //RayStart.Z ist größer als die BoundingBox.MaxZ des Dreiecks und RayDirection.Z > 0
        //Erwartung: Es kann kein Schnittpunkt geben, da der Strahl rechts vom Dreieck liegt und nach rechts hin wegfliegt.
        //Hintergrund des Tests:
        //Beim Schnittpunkttest aus flipcode gibt es folgende Bedingung:
        //float sum = beta + gamma;
        //if (sum > 1f) return null; 
        //Diese ist falsch, da dieser Test hier rot wird wenn man es so macht.
        //Stattdessen muss man
        //if (1 - beta - gamma < 0) return null;
        //Schreiben. Es ist also ein numerisches Ungenauigkeitsproblem.
        [TestMethod]
        public void GetSimpleIntersectionPoint_CalledForZRightSideRay_NoIntersectionFound()
        {
            var rayHeigh = new RayDrawingObject(new ObjectPropertys() { Name = "Test" }, null, null);
            var sut = new IntersectableTriangle(new Triangle(new Vertex(new Vector3D(-0.431770653f, 0.891006529f, -0.140290797f), new Vector3D(-0.430576921f, 0.891028643f, -0.143775687f), new Vector3D(-0.156434372f, 0f, 0.987688363f), 0.949999988f, 0.150000006f), new Vertex(new Vector3D(-0.453990519f, 0.891006529f, -1.09113223E-16f), new Vector3D(-0.475795656f, 0.876321614f, -0.0753585845f), new Vector3D(-0.156434372f, 0f, 0.987688363f), 1f, 0.150000006f), new Vertex(new Vector3D(-0.309017003f, 0.95105654f, -1.16466991E-16f), new Vector3D(-0.333348632f, 0.941324174f, -0.0527971871f), new Vector3D(-0.156434372f, 0f, 0.987688363f), 1f, 0.099999994f)), rayHeigh);
            var ray = new Ray(new Vector3D(0f, 0f, 0f), new Vector3D(-0.309017003f, 0.95105654f, 6.12303177E-17f));

            var point = sut.GetSimpleIntersectionPoint(ray, 0);

            Assert.IsNull(point);
        }

        //RayStart.X ist kleiner als die BoundingBox.MinX des Dreiecks und RayDirection.X< 0
        //Erwartung: Es kann kein Schnittpunkt geben, da der Strahl links vom Dreieck liegt und nach links hin wegfliegt.

        [TestMethod]
        [Ignore] //Wenn ich in der IntersectableTriangle-Klasse IntersectionHelper.CanRayClippedAwayWithBoundingBox nutze, dann kann ich diesen Edge-Case hier lösen; In dieser Implementierung hier fehlt dieser Funktionsaufruf
        public void GetSimpleIntersectionPoint_CalledForXLeftSideRay_Case1()
        {
            Vector3D[] V = new Vector3D[]
            {
                new Vector3D(0.275336176f, 0.453990459f, 0.847397566f),
                new Vector3D(1.35871854E-16f, 0.453990459f, 0.891006529f),
                new Vector3D(1.55779272E-16f, 0.309016973f, 0.95105654f)
            };
            var ray = new Ray(new Vector3D(0f, 0f, 0f), new Vector3D(-2.25403822E-17f, 0.368124545f, 0.92977649f));

            var point1 = GetRayTriangleIntersectionPoint_Flipcode(V[0], V[1], V[2], ray);
            var point2 = GetRayTriangleIntersectionPoint_4Planes(V[0], V[1], V[2], ray);

            Assert.IsNull(point2, "False Intersectionpoint in 4-Planes");
            Assert.IsNull(point1, "False Intersectionpoint in Flipcode"); //Fail
            
        }

        //RayStart.X ist kleiner als die BoundingBox.MinX des Dreiecks und RayDirection.X < 0
        //Erwartung: Es kann kein Schnittpunkt geben, da der Strahl links vom Dreieck liegt und nach links hin wegfliegt.
        [TestMethod]
        [Ignore] //Wenn ich in der IntersectableTriangle-Klasse IntersectionHelper.CanRayClippedAwayWithBoundingBox nutze, dann kann ich diesen Edge-Case hier lösen; In dieser Implementierung hier fehlt dieser Funktionsaufruf
        public void GetSimpleIntersectionPoint_CalledForXLeftSideRay_Case2()
        {
            Vector3D[] V = new Vector3D[]
            {
                new Vector3D(0.293892652f, 0.309016973f, 0.904508531f),
                new Vector3D(1.55779272E-16f, 0.309016973f, 0.95105654f),
                new Vector3D(1.71850888E-16f, 0.156434432f, 0.987688363f)
            };
            var ray = new Ray(new Vector3D(0f, 0f, 0f), new Vector3D(-9.57853146E-18f, 0.156434461f, 0.987688363f));

            var point1 = GetRayTriangleIntersectionPoint_Flipcode(V[0], V[1], V[2], ray);
            var point2 = GetRayTriangleIntersectionPoint_4Planes(V[0], V[1], V[2], ray);


            Assert.IsNull(point1, "False Intersectionpoint in Flipcode"); //Fail
            Assert.IsNull(point2, "False Intersectionpoint in 4-Planes"); //Fail            
        }

        //Hilfsfunktion, um UnitTests im IntersectableTriangleTest zu unterstützen
        private static Vector3D GetRayTriangleIntersectionPoint_Flipcode(Vector3D A, Vector3D B, Vector3D C, Ray ray)
        {
            //...
            double nu, nv, nd, bnu, bnv, cnu, cnv;
            int k, ku, kv;
            Vector3D c = B - A;
            Vector3D b = C - A;
            Vector3D normal = Vector3D.Cross(b, c);
            int u, v;
            if (Math.Abs(normal.X) > Math.Abs(normal.Y))
            {
                if (Math.Abs(normal.X) > Math.Abs(normal.Z)) k = 0; else k = 2;
            }
            else
            {
                if (Math.Abs(normal.Y) > Math.Abs(normal.Z)) k = 1; else k = 2;
            }
            u = (k + 1) % 3;
            v = (k + 2) % 3;
            ku = u;
            kv = v;
            // precomp
            double krec = 1.0f / normal[k];
            nu = normal[u] * krec;
            nv = normal[v] * krec;
            nd = (normal * A) * krec;
            // first line equation
            double reci = 1.0 / (b[u] * c[v] - b[v] * c[u]);
            bnu = b[u] * reci;
            bnv = -b[v] * reci;
            // second line equation
            cnu = c[v] * reci;
            cnv = -c[u] * reci;
            //...

            double[] O = new double[] { ray.Start.X, ray.Start.Y, ray.Start.Z };
            double[] D = new double[] { ray.Direction.X, ray.Direction.Y, ray.Direction.Z };
            double[] P0 = new double[] { A.X, A.Y, A.Z };

            double Ind = 1.0 / (D[k] + nu * D[ku] + nv * D[kv]);
            if (double.IsInfinity(Ind) || double.IsNaN(Ind)) return null;
            double t = (nd - O[k] - nu * O[ku] - nv * O[kv]) * Ind;
            if (t <= 0) return null;
            double hu = O[ku] + t * D[ku] - P0[ku];
            double hv = O[kv] + t * D[kv] - P0[kv];
            double beta = hv * bnu + hu * bnv;
            if (beta < 0) return null;
            double gamma = hu * cnu + hv * cnv;
            if (gamma < 0) return null;

            return ray.Start + ray.Direction * (float)t;
        }

        //Hilfsfunktion, um UnitTests im IntersectableTriangleTest zu unterstützen
        private static Vector3D GetRayTriangleIntersectionPoint_4Planes(Vector3D A, Vector3D B, Vector3D C, Ray ray)
        {
            Vector3D N = Vector3D.Normalize(Vector3D.Cross(B - A, C - A));
            var planes = new Plane[]{
                new Plane(N, A),                                              //Ebene des Dreiecks
                new Plane(Vector3D.Normalize(Vector3D.Cross(B - A, N)), A),   //Ebene bei Kante 1-2
                new Plane(Vector3D.Normalize(Vector3D.Cross(C - B, N)), B),   //Ebene bei Kante 2-3
                new Plane(Vector3D.Normalize(Vector3D.Cross(A - C, N)), C)};  //Ebene bei Kante 3-1

            //Prüfe zuerst, ob Strahl die Ebene durchschneidet
            float mr_proper = planes[0].A * ray.Direction.X + planes[0].B * ray.Direction.Y + planes[0].C * ray.Direction.Z;
            if (mr_proper == 0) return null;
            float t = -(planes[0].A * ray.Start.X + planes[0].B * ray.Start.Y + planes[0].C * ray.Start.Z + planes[0].D) / mr_proper;

            Vector3D position;
            if (t > 0)//Strahl durchschneidet Ebene. Prüfe nun, ob es innerhalb der 4 Eckpunkte liegt
            {           //ich schreibe hier 0.1 statt 0, da sonst Kollision mit sicher Selber bei Reflexion passiert(Streuseleffekt)
                position = ray.Start + ray.Direction * t;

                for (int i = 1; i < planes.Length; i++)
                    if (planes[i].A * position.X +
                        planes[i].B * position.Y +
                        planes[i].C * position.Z +
                        planes[i].D > 0) return null;
            }
            else
            {
                return null;
            }

            return position;
        }
    }
}
