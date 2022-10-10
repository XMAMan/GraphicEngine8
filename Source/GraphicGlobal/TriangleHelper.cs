using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;

namespace GraphicGlobal
{
    public static class TriangleHelper
    {
        public static BoundingBox GetBoundingBox(this IEnumerable<Triangle> triangles)
        {
            return new BoundingBox(triangles.Select(x => GetBoundingBox(x)));
        }

        public static BoundingBox GetBoundingBox(this Triangle triangle)
        {
            var V = triangle.V;

            Vector3D min = new Vector3D(Math.Min(Math.Min(V[0].Position.X, V[1].Position.X), V[2].Position.X),
                                  Math.Min(Math.Min(V[0].Position.Y, V[1].Position.Y), V[2].Position.Y),
                                  Math.Min(Math.Min(V[0].Position.Z, V[1].Position.Z), V[2].Position.Z));

            Vector3D max = new Vector3D(Math.Max(Math.Max(V[0].Position.X, V[1].Position.X), V[2].Position.X),
                                  Math.Max(Math.Max(V[0].Position.Y, V[1].Position.Y), V[2].Position.Y),
                                  Math.Max(Math.Max(V[0].Position.Z, V[1].Position.Z), V[2].Position.Z));

            return new BoundingBox(min, max);
        }

        public static Plane GetTrianglePlane(this Triangle triangle)
        {
            return new Plane(triangle.Normal, triangle.V[0].Position);
        }

        public static void TransformTriangleListToVertexIndexList(Triangle[] triangles, out List<Vertex> vertexList, out List<uint> indexList)
        {
            vertexList = new List<Vertex>();
            indexList = new List<uint>();

            foreach (Triangle triangle in triangles)
                foreach (Vertex vertex in triangle.V)
                {
                    //Der Mario (3D-Bitmap) hat fehlerhafte UV-Koordinaten, wenn ich gemeinsame Vertice nutze
                    /*int index = vertexList.FindIndex(x => x.Position == vertex.Position);
                    if (index >= 0)
                    {
                        indexList.Add((uint)index);
                    }
                    else*/
                    {
                        vertexList.Add(vertex);
                        indexList.Add((uint)(vertexList.Count - 1));

                    }
                }
        }

        public static List<Triangle> TransformTrianglesWithMatrix(Matrix4x4 modelMatrix4x4, IEnumerable<Triangle> triangles)
        {
            Matrix4x4 normalMatrix = Matrix4x4.Transpose(Matrix4x4.Invert(modelMatrix4x4));

            List<Triangle> newTriangles = new List<Triangle>();

            foreach (var T in triangles)
            {
                Vector3D p1 = Matrix4x4.MultPosition(modelMatrix4x4, T.V[0].Position);
                Vector3D p2 = Matrix4x4.MultPosition(modelMatrix4x4, T.V[1].Position);
                Vector3D p3 = Matrix4x4.MultPosition(modelMatrix4x4, T.V[2].Position);
                Vector3D flatNormal = Vector3D.Cross(p2 - p1, p3 - p1);
                if (flatNormal.Length() > 0)
                {
                    newTriangles.Add(new Triangle(
                        new Vertex(p1, Vector3D.Normalize(Matrix4x4.MultDirection(normalMatrix, T.V[0].Normal)), null, T.V[0].TexcoordU, T.V[0].TexcoordV),
                        new Vertex(p2, Vector3D.Normalize(Matrix4x4.MultDirection(normalMatrix, T.V[1].Normal)), null, T.V[1].TexcoordU, T.V[1].TexcoordV),
                        new Vertex(p3, Vector3D.Normalize(Matrix4x4.MultDirection(normalMatrix, T.V[2].Normal)), null, T.V[2].TexcoordU, T.V[2].TexcoordV)));
                }
            }

            return newTriangles;
        }

        public static List<Triangle2DIPoint> TransformPolygonToTriangleList(List<IPoint2D> points)
        {
            return Triangulator.TransformPolygonToTriangleList(points);                 
        }
        
        //Quelle: http://wiki.unity3d.com/index.php?title=Triangulator
        //Beschreibung: Split a 2D polygon into triangles. The algorithm supports concave polygons, but not polygons with holes, or multiple polygons at once.
        class Triangulator
        {
            private readonly List<IPoint2D> m_points = new List<IPoint2D>();

            public static List<Triangle2DIPoint> TransformPolygonToTriangleList(List<IPoint2D> points)
            {
                if (points.Count == 3)
                {
                    return new List<Triangle2DIPoint>() { new Triangle2DIPoint(points[0], points[1], points[2]) };
                }

                if (points.Count == 4)
                {
                    return new List<Triangle2DIPoint>() { new Triangle2DIPoint(points[0], points[1], points[2]), new Triangle2DIPoint(points[2], points[3], points[0]) };
                }

                points = RemoveDupplicatedPoints(points);

                List<Triangle2DIPoint> ret = new List<Triangle2DIPoint>();
                Triangulator tr = new Triangulator(points);
                int[] indices = tr.Triangulate();
                for (int i = 0; i < indices.Length; i += 3)
                {
                    ret.Add(new Triangle2DIPoint(points[indices[i]], points[indices[i + 1]], points[indices[i + 2]]));
                }
                return ret;
            }

            private static List<IPoint2D> RemoveDupplicatedPoints(List<IPoint2D> points)
            {
                List<IPoint2D> newList = new List<IPoint2D>() { points[0] };
                for (int i = 1; i < points.Count; i++)
                {
                    var p1 = newList.Last();
                    var p2 = points[i];
                    if (i == points.Count - 1)
                    {
                        var p3 = newList.First();
                        if (IsEqual(p1, p2) == false && IsEqual(p2, p3) == false) newList.Add(p2);
                    }
                    else
                    {
                        if (IsEqual(p1, p2) == false) newList.Add(p2);
                    }
                }

                return newList;
            }

            private static bool IsEqual(IPoint2D p1, IPoint2D p2)
            {
                return p1.X == p2.X && p1.Y == p2.Y;
            }

            public Triangulator(List<IPoint2D> points)
            {
                m_points = points;
            }

            public int[] Triangulate()
            {
                List<int> indices = new List<int>();

                int n = m_points.Count;
                if (n < 3)
                    return indices.ToArray();

                int[] V = new int[n];
                if (Area() > 0)
                {
                    for (int v = 0; v < n; v++)
                        V[v] = v;
                }
                else
                {
                    for (int v = 0; v < n; v++)
                        V[v] = (n - 1) - v;
                }

                int nv = n;
                int count = 2 * nv;
                for (int m = 0, v = nv - 1; nv > 2; )
                {
                    if ((count--) <= 0)
                        return indices.ToArray();

                    int u = v;
                    if (nv <= u)
                        u = 0;
                    v = u + 1;
                    if (nv <= v)
                        v = 0;
                    int w = v + 1;
                    if (nv <= w)
                        w = 0;

                    if (Snip(u, v, w, nv, V))
                    {
                        int a, b, c, s, t;
                        a = V[u];
                        b = V[v];
                        c = V[w];
                        indices.Add(a);
                        indices.Add(b);
                        indices.Add(c);
                        m++;
                        for (s = v, t = v + 1; t < nv; s++, t++)
                            V[s] = V[t];
                        nv--;
                        count = 2 * nv;
                    }
                }

                indices.Reverse();
                return indices.ToArray();
            }

            private float Area()
            {
                int n = m_points.Count;
                float A = 0.0f;
                for (int p = n - 1, q = 0; q < n; p = q++)
                {
                    IPoint2D pval = m_points[p];
                    IPoint2D qval = m_points[q];
                    A += pval.X * qval.Y - qval.X * pval.Y;
                }
                return (A * 0.5f);
            }

            private bool Snip(int u, int v, int w, int n, int[] V)
            {
                //float Epsilon = 100.0f;
                float Epsilon = 0.0001f;

                int p;
                IPoint2D A = m_points[V[u]];
                IPoint2D B = m_points[V[v]];
                IPoint2D C = m_points[V[w]];
                if (Epsilon > (((B.X - A.X) * (C.Y - A.Y)) - ((B.Y - A.Y) * (C.X - A.X))))
                    return false;
                for (p = 0; p < n; p++)
                {
                    if ((p == u) || (p == v) || (p == w))
                        continue;
                    IPoint2D P = m_points[V[p]];
                    if (InsideTriangle(A, B, C, P))
                        return false;
                }
                return true;
            }

            private bool InsideTriangle(IPoint2D A, IPoint2D B, IPoint2D C, IPoint2D P)
            {
                float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
                float cCROSSap, bCROSScp, aCROSSbp;

                ax = C.X - B.X; ay = C.Y - B.Y;
                bx = A.X - C.X; by = A.Y - C.Y;
                cx = B.X - A.X; cy = B.Y - A.Y;
                apx = P.X - A.X; apy = P.Y - A.Y;
                bpx = P.X - B.X; bpy = P.Y - B.Y;
                cpx = P.X - C.X; cpy = P.Y - C.Y;

                aCROSSbp = ax * bpy - ay * bpx;
                cCROSSap = cx * apy - cy * apx;
                bCROSScp = bx * cpy - by * cpx;

                return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
            }
        }
    }
}
