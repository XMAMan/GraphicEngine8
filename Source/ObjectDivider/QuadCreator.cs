using System;
using System.Collections.Generic;
using System.Linq;
using GraphicGlobal;
using GraphicMinimal;

namespace ObjectDivider
{
    //Input: Liste von Dreiecken
    //Verarbeitung: Die Dreieckspärchen, die ein rechtwinkliges Viereck bilden, werden durch ein Viereck ersetzt.
    //Output: Liste von Dreiecken und Vierecken. 
    public static class QuadCreator
    {
        public static List<IDivideable> GetQuadList(IEnumerable<IDivideable> divideables)
        {
            return GetRayObjekteWithQuads(divideables.Cast<Triangle>().ToList());
        }

        //Alle Dreiecke, die sich zu ein Viereck vereinen lassen, werden vereint
        private static List<IDivideable> GetRayObjekteWithQuads(List<Triangle> triangles)
        {
            List<IDivideable> newList = new List<IDivideable>();

            //Schritt 1: Fische zuerst alle Dreiecke raus, welche sich zu Vierecken verbinden lassen
            for (int i = triangles.Count - 1; i >= 0; i--)
            {
                if (i >= triangles.Count) i = triangles.Count - 1;
                for (int j = triangles.Count - 1; j >= 0; j--)
                {
                    if (j >= triangles.Count) j = triangles.Count - 1;
                    if (i != j)
                    {
                        if (IsQuad(triangles[i], triangles[j]))
                        {
                            var quad = BuildQuad(triangles[i], triangles[j]);
                            if (quad != null)
                            {
                                newList.Add(quad);
                                var löschi1 = triangles[i];
                                var löschi2 = triangles[j];
                                triangles.Remove(löschi1);
                                triangles.Remove(löschi2);
                                break;
                            }
                        }
                    }
                }
            }

            //Schritt 2: Die restlichen Dreiecke ließen sich nicht zuordnen. Nimm sie so.
            newList.AddRange(triangles);

            return newList;
        }

        private static float maxDistance = 0.000001f;

        //Gibt true zurück, wenn beide Dreiecke 2 Gemeinsame Punkte haben und die Normale bei beiden gleich ist
        private static bool IsQuad(Triangle t1, Triangle t2)
        {
            if (t1.Normal * t2.Normal < 0.99f) return false;

            int schnittPunktCount = 0;
            if ((t1.V[0].Position - t2.V[0].Position).SquareLength() < maxDistance) schnittPunktCount++;
            if ((t1.V[0].Position - t2.V[1].Position).SquareLength() < maxDistance) schnittPunktCount++;
            if ((t1.V[0].Position - t2.V[2].Position).SquareLength() < maxDistance) schnittPunktCount++;

            if ((t1.V[1].Position - t2.V[0].Position).SquareLength() < maxDistance) schnittPunktCount++;
            if ((t1.V[1].Position - t2.V[1].Position).SquareLength() < maxDistance) schnittPunktCount++;
            if ((t1.V[1].Position - t2.V[2].Position).SquareLength() < maxDistance) schnittPunktCount++;

            if ((t1.V[2].Position - t2.V[0].Position).SquareLength() < maxDistance) schnittPunktCount++;
            if ((t1.V[2].Position - t2.V[1].Position).SquareLength() < maxDistance) schnittPunktCount++;
            if ((t1.V[2].Position - t2.V[2].Position).SquareLength() < maxDistance) schnittPunktCount++;

            return schnittPunktCount == 2;
        }

        private static Quad BuildQuad(Triangle t1, Triangle t2)
        {
            List<int> nichtDoppelt1 = new List<int>() { 0, 1, 2 };
            List<int> nichtDoppelt2 = new List<int>() { 0, 1, 2 };
            if ((t1.V[0].Position - t2.V[0].Position).SquareLength() < maxDistance) { nichtDoppelt1 = nichtDoppelt1.Where(x => x != 0).ToList(); nichtDoppelt2 = nichtDoppelt2.Where(x => x != 0).ToList(); }
            if ((t1.V[0].Position - t2.V[1].Position).SquareLength() < maxDistance) { nichtDoppelt1 = nichtDoppelt1.Where(x => x != 0).ToList(); nichtDoppelt2 = nichtDoppelt2.Where(x => x != 1).ToList(); }
            if ((t1.V[0].Position - t2.V[2].Position).SquareLength() < maxDistance) { nichtDoppelt1 = nichtDoppelt1.Where(x => x != 0).ToList(); nichtDoppelt2 = nichtDoppelt2.Where(x => x != 2).ToList(); }

            if ((t1.V[1].Position - t2.V[0].Position).SquareLength() < maxDistance) { nichtDoppelt1 = nichtDoppelt1.Where(x => x != 1).ToList(); nichtDoppelt2 = nichtDoppelt2.Where(x => x != 0).ToList(); }
            if ((t1.V[1].Position - t2.V[1].Position).SquareLength() < maxDistance) { nichtDoppelt1 = nichtDoppelt1.Where(x => x != 1).ToList(); nichtDoppelt2 = nichtDoppelt2.Where(x => x != 1).ToList(); }
            if ((t1.V[1].Position - t2.V[2].Position).SquareLength() < maxDistance) { nichtDoppelt1 = nichtDoppelt1.Where(x => x != 1).ToList(); nichtDoppelt2 = nichtDoppelt2.Where(x => x != 2).ToList(); }

            if ((t1.V[2].Position - t2.V[0].Position).SquareLength() < maxDistance) { nichtDoppelt1 = nichtDoppelt1.Where(x => x != 2).ToList(); nichtDoppelt2 = nichtDoppelt2.Where(x => x != 0).ToList(); }
            if ((t1.V[2].Position - t2.V[1].Position).SquareLength() < maxDistance) { nichtDoppelt1 = nichtDoppelt1.Where(x => x != 2).ToList(); nichtDoppelt2 = nichtDoppelt2.Where(x => x != 1).ToList(); }
            if ((t1.V[2].Position - t2.V[2].Position).SquareLength() < maxDistance) { nichtDoppelt1 = nichtDoppelt1.Where(x => x != 2).ToList(); nichtDoppelt2 = nichtDoppelt2.Where(x => x != 2).ToList(); }

            Vertex v1 = t1.V[nichtDoppelt1[0]];
            Vertex v2 = t1.V[(nichtDoppelt1[0] + 1) % 3];
            Vertex v3 = t2.V[nichtDoppelt2[0]];
            Vertex v4 = t2.V[(nichtDoppelt2[0] + 1) % 3];

            //Prüfe, ob die Gegenüberliegenden Seiten gleichlang sind
            float l1 = (v2.Position - v1.Position).Length();
            float l2 = (v4.Position - v3.Position).Length();
            float l3 = (v3.Position - v2.Position).Length();
            float l4 = (v1.Position - v4.Position).Length();
            if (Math.Abs(l1 - l2) > 0.00001f || Math.Abs(l3 - l4) > 0.00001f) return null; //Gegenüberliegende Kanten sind unterschiedlich lang

            Vector3D e1 = Vector3D.Normalize(v2.Position - v1.Position);
            Vector3D e2 = Vector3D.Normalize(v4.Position - v1.Position);
            if (Math.Abs(e1 * e2) > 0.001f) return null; //Viereck ist windschief

            Plane ebene1 = new Plane(v1.Position, v2.Position, v3.Position);
            Plane ebene2 = new Plane(v3.Position, v4.Position, v1.Position);

            if (Math.Abs(ebene1.Normal * ebene2.Normal - 1) > 0.001f) throw new Exception("Viereck hat Knick zwischen den zwei Dreiecken");

            return new Quad(v1, v2, v3, v4);
        }
    }
}
