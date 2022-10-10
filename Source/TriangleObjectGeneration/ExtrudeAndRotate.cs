using System;
using System.Collections.Generic;
using GraphicMinimal;
using GraphicGlobal;

namespace TriangleObjectGeneration
{
    static class ExtrudeAndRotate
    {
        public static TriangleList CreateExtrusionObject(List<Vector3D> path, List<Vector2D> shape, bool addCap)
        {
            TriangleList newObj = new TriangleList();
            int i, j;
            float[] wy = new float[2], w = new float[2];
            float minX = 0, minY = 0, maxX = 0, maxY = 0, rotX, rotY, pathLength = 0, shapeLength = 0, pathIntermediateLength = 0, shapeIntermediateLength = 0;
            Vector3D[] N = new Vector3D[] { new Vector3D(0, 0, 0), new Vector3D(0, 0, 0) };
            Vector3D V = new Vector3D(0, 0, 0);
            Vertex[] P = new Vertex[] { new Vertex(0, 0, 0), new Vertex(0, 0, 0), new Vertex(0, 0, 0), new Vertex(0, 0, 0) };

            if (path.Count <= 1 || shape.Count < 1) return null;//Pfad muss mindestens 2 Eckpunkte haben 

            minX = maxX = shape[0].X;
            minY = maxY = shape[0].Y;
            for (i = 0; i < shape.Count; i++)
            {
                if (shape[i].X < minX) minX = shape[i].X;
                if (shape[i].Y < minY) minY = shape[i].Y;
                if (shape[i].X > maxX) maxX = shape[i].X;
                if (shape[i].Y > maxY) maxY = shape[i].Y;
            }
            rotX = minX + (maxX - minX) / 2;
            rotY = minY + (maxY - minY) / 2;
            for (i = 0; i < shape.Count; i++) //Die Grundform zum Mittelpunkt verschieben
            {
                shape[i].X -= rotX;
                shape[i].Y -= rotY;
            }

            for (i = 0; i < path.Count - 1; i++) pathLength += (path[i + 1] - path[i]).Length();
            for (i = 0; i < shape.Count - 1; i++) shapeLength += (shape[i + 1] - shape[i]).Length();

            N[0] = Vector3D.Normalize(path[1] - path[0]);
            for (i = 0; i < path.Count - 1; i++)
            {
                if (path[i] == path[i + 1]) return null;//es dürfen keine 2 hintereinander liegende Eckpunkte gleich sein
                if (i + 2 < path.Count) N[1] = Vector3D.Normalize(Vector3D.Normalize(path[i + 2] - path[i + 1]) + N[0]); else N[1] = Vector3D.Normalize(path[i + 1] - path[i]);

                for (j = 0; j < 2; j++)
                {
                    if (N[j].X == 0 && N[j].Z == 0)
                    {
                        if (N[j].Y > 0) w[j] = 90; else w[j] = -90;
                        wy[j] = 0;
                    }
                    else
                    {
                        if (N[j].Y > 0) w[j] = Vector3D.AngleDegree(N[j], new Vector3D(N[j].X, 0, N[j].Z)); else w[j] = -Vector3D.AngleDegree(N[j], new Vector3D(N[j].X, 0, N[j].Z));
                        if (N[j].X > 0) wy[j] = Vector3D.AngleDegree(new Vector3D(0, 0, 1), new Vector3D(N[j].X, 0, N[j].Z)); else wy[j] = -Vector3D.AngleDegree(new Vector3D(0, 0, 1), new Vector3D(N[j].X, 0, N[j].Z));
                    }
                }

                shapeIntermediateLength = 0;
                for (j = 0; j < shape.Count; j++)
                {
                    V.Z = -shape[j].Y * (float)(Math.Sin(w[0] * Math.PI / 180));									//P[0]
                    V.Y = +shape[j].Y * (float)(Math.Cos(w[0] * Math.PI / 180));
                    V.X = shape[j].X;
                    P[0].Position.Z = V.Z * (float)(Math.Cos(wy[0] * Math.PI / 180)) - V.X * (float)(Math.Sin(wy[0] * Math.PI / 180));
                    P[0].Position.X = V.Z * (float)(Math.Sin(wy[0] * Math.PI / 180)) + V.X * (float)(Math.Cos(wy[0] * Math.PI / 180));
                    P[0].Position.Y = V.Y;
                    P[0].Position += path[i];

                    V.Z = -shape[j].Y * (float)(Math.Sin(w[1] * Math.PI / 180));									//P[1]
                    V.Y = +shape[j].Y * (float)(Math.Cos(w[1] * Math.PI / 180));
                    V.X = shape[j].X;
                    P[1].Position.Z = V.Z * (float)(Math.Cos(wy[1] * Math.PI / 180)) - V.X * (float)(Math.Sin(wy[1] * Math.PI / 180));
                    P[1].Position.X = V.Z * (float)(Math.Sin(wy[1] * Math.PI / 180)) + V.X * (float)(Math.Cos(wy[1] * Math.PI / 180));
                    P[1].Position.Y = V.Y;
                    P[1].Position += path[i + 1];

                    V.Z = -shape[(j + 1) % shape.Count].Y * (float)(Math.Sin(w[1] * Math.PI / 180));					//P[2]
                    V.Y = +shape[(j + 1) % shape.Count].Y * (float)(Math.Cos(w[1] * Math.PI / 180));
                    V.X = shape[(j + 1) % shape.Count].X;
                    P[2].Position.Z = V.Z * (float)(Math.Cos(wy[1] * Math.PI / 180)) - V.X * (float)(Math.Sin(wy[1] * Math.PI / 180));
                    P[2].Position.X = V.Z * (float)(Math.Sin(wy[1] * Math.PI / 180)) + V.X * (float)(Math.Cos(wy[1] * Math.PI / 180));
                    P[2].Position.Y = V.Y;
                    P[2].Position += path[i + 1];

                    V.Z = -shape[(j + 1) % shape.Count].Y * (float)(Math.Sin(w[0] * Math.PI / 180));					//P[3]
                    V.Y = +shape[(j + 1) % shape.Count].Y * (float)(Math.Cos(w[0] * Math.PI / 180));
                    V.X = shape[(j + 1) % shape.Count].X;
                    P[3].Position.Z = V.Z * (float)(Math.Cos(wy[0] * Math.PI / 180)) - V.X * (float)(Math.Sin(wy[0] * Math.PI / 180));
                    P[3].Position.X = V.Z * (float)(Math.Sin(wy[0] * Math.PI / 180)) + V.X * (float)(Math.Cos(wy[0] * Math.PI / 180));
                    P[3].Position.Y = V.Y;
                    P[3].Position += path[i];

                    P[0].TexcoordU = shapeIntermediateLength / shapeLength;					//Texturcoordinaten berechnen
                    P[0].TexcoordV = pathIntermediateLength / pathLength;
                    P[1].TexcoordU = shapeIntermediateLength / shapeLength;
                    P[1].TexcoordV = (pathIntermediateLength + (path[i + 1] - path[i]).Length()) / pathLength;
                    P[2].TexcoordU = (shapeIntermediateLength + (shape[(j + 1) % shape.Count] - shape[j]).Length()) / shapeLength;
                    P[2].TexcoordV = (pathIntermediateLength + (path[i + 1] - path[i]).Length()) / pathLength;
                    P[3].TexcoordU = (shapeIntermediateLength + (shape[(j + 1) % shape.Count] - shape[j]).Length()) / shapeLength;
                    P[3].TexcoordV = pathIntermediateLength / pathLength;

                    shapeIntermediateLength += (shape[(j + 1) % shape.Count] - shape[j]).Length();

                    if (!newObj.AddTriangle(P[2], P[1], P[0])) return null;
                    if (!newObj.AddTriangle(P[0], P[3], P[2])) return null;

                    if (addCap && (i == 0 || i == path.Count - 2))
                    {
                        if (i == 0) newObj.AddTriangle(new Vertex(path[i].X, path[i].Y, path[i].Z, 0, 0), P[0], P[3]);
                        if (i == path.Count - 2) newObj.AddTriangle(new Vertex(path[i].X, path[i].Y, path[i].Z, 0, 0), P[1], P[2]);
                    }

                    P = new Vertex[] { new Vertex(0, 0, 0), new Vertex(0, 0, 0), new Vertex(0, 0, 0), new Vertex(0, 0, 0) };
                }
                pathIntermediateLength += (path[i + 1] - path[i]).Length();
                N[0] = N[1];
            }

            newObj.TransformToCoordinateOrigin();
            newObj.SetNormals();												//Normalen berechnen

            return newObj;
        }

        public static TriangleList CreateRotationObject(List<Vector2D> shape, Vector3D P1, Vector3D P2, float startDegree, float endDegree, int degreeSteps)
        {
            TriangleList newObj = new TriangleList();
            int i;
            float wy, w, f, fd, shapeLength = 0, shapeIntermediateLength = 0; ;
            Vector3D N = new Vector3D(0, 0, 0), V = new Vector3D(0, 0, 0);
            Vertex[] P = new Vertex[] { new Vertex(0, 0, 0), new Vertex(0, 0, 0), new Vertex(0, 0, 0), new Vertex(0, 0, 0), new Vertex(0, 0, 0) };//0...P1 gedreht, 1-4...spannen Viereck vom Drehkörper auf

            if (startDegree > endDegree || shape.Count < 2) return null;

            for (i = 0; i < shape.Count - 1; i++) shapeLength += (shape[i + 1] - shape[i]).Length();

            N = Vector3D.Normalize(P2 - P1);//Objekt wird mit N zur Z-Axse gedreht, dann rotiert, dann zurückgedreht
            if (N.X == 0 && N.Z == 0)
            {
                if (N.Y > 0) w = 90; else w = -90;
                wy = 0;
            }
            else
            {
                if (N.Y > 0) w = Vector3D.AngleDegree(N, new Vector3D(N.X, 0, N.Z)); else w = -Vector3D.AngleDegree(N, new Vector3D(N.X, 0, N.Z));
                if (N.X > 0) wy = Vector3D.AngleDegree(new Vector3D(0, 0, 1), new Vector3D(N.X, 0, N.Z)); else wy = -Vector3D.AngleDegree(new Vector3D(0, 0, 1), new Vector3D(N.X, 0, N.Z));
            }

            fd = (endDegree - startDegree) / degreeSteps;
            for (f = startDegree; f < endDegree; f += fd)
            {
                V.Z = P1.Z * (float)Math.Cos(-wy * Math.PI / 180) - P1.X * (float)Math.Sin(-wy * Math.PI / 180);
                V.X = P1.Z * (float)Math.Sin(-wy * Math.PI / 180) + P1.X * (float)Math.Cos(-wy * Math.PI / 180);
                V.Y = P1.Y;
                P[0].Position.Z = V.Z * (float)Math.Cos(-w * Math.PI / 180) - V.Y * (float)Math.Sin(-w * Math.PI / 180);
                P[0].Position.Y = V.Z * (float)Math.Sin(-w * Math.PI / 180) + V.Y * (float)Math.Cos(-w * Math.PI / 180);
                P[0].Position.X = V.X;
                shapeIntermediateLength = 0;
                for (i = 0; i < shape.Count - 1; i++)
                {
                    V.Z = -shape[i].X * (float)Math.Sin(-wy * Math.PI / 180); //P[1] (shape[i], f)
                    V.X = +shape[i].X * (float)Math.Cos(-wy * Math.PI / 180);
                    V.Y = shape[i].Y;
                    P[1].Position.Z = V.Z * (float)Math.Cos(-w * Math.PI / 180) - V.Y * (float)Math.Sin(-w * Math.PI / 180);
                    P[1].Position.Y = V.Z * (float)Math.Sin(-w * Math.PI / 180) + V.Y * (float)Math.Cos(-w * Math.PI / 180);
                    P[1].Position.X = V.X;
                    V.X = P[0].Position.X - P[1].Position.X;
                    V.Y = P[0].Position.Y - P[1].Position.Y;
                    P[1].Position.X = P[0].Position.X + V.X * (float)Math.Cos(f * Math.PI / 180) - V.Y * (float)Math.Sin(f * Math.PI / 180);
                    P[1].Position.Y = P[0].Position.Y + V.X * (float)Math.Sin(f * Math.PI / 180) + V.Y * (float)Math.Cos(f * Math.PI / 180);
                    V.Z = P[1].Position.Z * (float)Math.Cos(w * Math.PI / 180) - P[1].Position.Y * (float)Math.Sin(w * Math.PI / 180);
                    V.Y = P[1].Position.Z * (float)Math.Sin(w * Math.PI / 180) + P[1].Position.Y * (float)Math.Cos(w * Math.PI / 180);
                    V.X = P[1].Position.X;
                    P[1].Position.Z = V.Z * (float)Math.Cos(wy * Math.PI / 180) - V.X * (float)Math.Sin(wy * Math.PI / 180);
                    P[1].Position.X = V.Z * (float)Math.Sin(wy * Math.PI / 180) + V.X * (float)Math.Cos(wy * Math.PI / 180);
                    P[1].Position.Y = V.Y;

                    V.Z = -shape[i].X * (float)Math.Sin(-wy * Math.PI / 180); //P[2] (shape[i], f+fd)
                    V.X = +shape[i].X * (float)Math.Cos(-wy * Math.PI / 180);
                    V.Y = shape[i].Y;
                    P[2].Position.Z = V.Z * (float)Math.Cos(-w * Math.PI / 180) - V.Y * (float)Math.Sin(-w * Math.PI / 180);
                    P[2].Position.Y = V.Z * (float)Math.Sin(-w * Math.PI / 180) + V.Y * (float)Math.Cos(-w * Math.PI / 180);
                    P[2].Position.X = V.X;
                    V.X = P[0].Position.X - P[2].Position.X;
                    V.Y = P[0].Position.Y - P[2].Position.Y;
                    P[2].Position.X = P[0].Position.X + V.X * (float)Math.Cos((f + fd) * Math.PI / 180) - V.Y * (float)Math.Sin((f + fd) * Math.PI / 180);
                    P[2].Position.Y = P[0].Position.Y + V.X * (float)Math.Sin((f + fd) * Math.PI / 180) + V.Y * (float)Math.Cos((f + fd) * Math.PI / 180);
                    V.Z = P[2].Position.Z * (float)Math.Cos(w * Math.PI / 180) - P[2].Position.Y * (float)Math.Sin(w * Math.PI / 180);
                    V.Y = P[2].Position.Z * (float)Math.Sin(w * Math.PI / 180) + P[2].Position.Y * (float)Math.Cos(w * Math.PI / 180);
                    V.X = P[2].Position.X;
                    P[2].Position.Z = V.Z * (float)Math.Cos(wy * Math.PI / 180) - V.X * (float)Math.Sin(wy * Math.PI / 180);
                    P[2].Position.X = V.Z * (float)Math.Sin(wy * Math.PI / 180) + V.X * (float)Math.Cos(wy * Math.PI / 180);
                    P[2].Position.Y = V.Y;

                    V.Z = -shape[(i + 1) % shape.Count].X * (float)Math.Sin(-wy * Math.PI / 180); //P[3] (shape[i+1], f+fd)
                    V.X = +shape[(i + 1) % shape.Count].X * (float)Math.Cos(-wy * Math.PI / 180);
                    V.Y = shape[(i + 1) % shape.Count].Y;
                    P[3].Position.Z = V.Z * (float)Math.Cos(-w * Math.PI / 180) - V.Y * (float)Math.Sin(-w * Math.PI / 180);
                    P[3].Position.Y = V.Z * (float)Math.Sin(-w * Math.PI / 180) + V.Y * (float)Math.Cos(-w * Math.PI / 180);
                    P[3].Position.X = V.X;
                    V.X = P[0].Position.X - P[3].Position.X;
                    V.Y = P[0].Position.Y - P[3].Position.Y;
                    P[3].Position.X = P[0].Position.X + V.X * (float)Math.Cos((f + fd) * Math.PI / 180) - V.Y * (float)Math.Sin((f + fd) * Math.PI / 180);
                    P[3].Position.Y = P[0].Position.Y + V.X * (float)Math.Sin((f + fd) * Math.PI / 180) + V.Y * (float)Math.Cos((f + fd) * Math.PI / 180);
                    V.Z = P[3].Position.Z * (float)Math.Cos(w * Math.PI / 180) - P[3].Position.Y * (float)Math.Sin(w * Math.PI / 180);
                    V.Y = P[3].Position.Z * (float)Math.Sin(w * Math.PI / 180) + P[3].Position.Y * (float)Math.Cos(w * Math.PI / 180);
                    V.X = P[3].Position.X;
                    P[3].Position.Z = V.Z * (float)Math.Cos(wy * Math.PI / 180) - V.X * (float)Math.Sin(wy * Math.PI / 180);
                    P[3].Position.X = V.Z * (float)Math.Sin(wy * Math.PI / 180) + V.X * (float)Math.Cos(wy * Math.PI / 180);
                    P[3].Position.Y = V.Y;

                    V.Z = -shape[(i + 1) % shape.Count].X * (float)Math.Sin(-wy * Math.PI / 180); //P[4] (shape[i+1], f)
                    V.X = +shape[(i + 1) % shape.Count].X * (float)Math.Cos(-wy * Math.PI / 180);
                    V.Y = shape[(i + 1) % shape.Count].Y;
                    P[4].Position.Z = V.Z * (float)Math.Cos(-w * Math.PI / 180) - V.Y * (float)Math.Sin(-w * Math.PI / 180);
                    P[4].Position.Y = V.Z * (float)Math.Sin(-w * Math.PI / 180) + V.Y * (float)Math.Cos(-w * Math.PI / 180);
                    P[4].Position.X = V.X;
                    V.X = P[0].Position.X - P[4].Position.X;
                    V.Y = P[0].Position.Y - P[4].Position.Y;
                    P[4].Position.X = P[0].Position.X + V.X * (float)Math.Cos(f * Math.PI / 180) - V.Y * (float)Math.Sin(f * Math.PI / 180);
                    P[4].Position.Y = P[0].Position.Y + V.X * (float)Math.Sin(f * Math.PI / 180) + V.Y * (float)Math.Cos(f * Math.PI / 180);
                    V.Z = P[4].Position.Z * (float)Math.Cos(w * Math.PI / 180) - P[4].Position.Y * (float)Math.Sin(w * Math.PI / 180);
                    V.Y = P[4].Position.Z * (float)Math.Sin(w * Math.PI / 180) + P[4].Position.Y * (float)Math.Cos(w * Math.PI / 180);
                    V.X = P[4].Position.X;
                    P[4].Position.Z = V.Z * (float)Math.Cos(wy * Math.PI / 180) - V.X * (float)Math.Sin(wy * Math.PI / 180);
                    P[4].Position.X = V.Z * (float)Math.Sin(wy * Math.PI / 180) + V.X * (float)Math.Cos(wy * Math.PI / 180);
                    P[4].Position.Y = V.Y;

                    //P[1] (shape[i], f)
                    //P[2] (shape[i], f+fd)
                    //P[3] (shape[i+1], f+fd)
                    //P[4] (shape[i+1], f)

                    P[1].TexcoordV = shapeIntermediateLength / shapeLength;					//Texturcoordinaten berechnen
                    P[1].TexcoordU = 1 - (f - startDegree) * 1.0f / (endDegree - startDegree);

                    P[2].TexcoordV = shapeIntermediateLength / shapeLength;
                    P[2].TexcoordU = 1 - (f + fd - startDegree) * 1.0f / (endDegree - startDegree);

                    P[3].TexcoordV = (shapeIntermediateLength + (shape[i + 1] - shape[i]).Length()) / shapeLength;
                    P[3].TexcoordU = 1 - (f + fd - startDegree) * 1.0f / (endDegree - startDegree);

                    P[4].TexcoordV = (shapeIntermediateLength + (shape[i + 1] - shape[i]).Length()) / shapeLength;
                    P[4].TexcoordU = 1 - (f - startDegree) * 1.0f / (endDegree - startDegree);


                    shapeIntermediateLength += (shape[i + 1] - shape[i]).Length();

                    newObj.AddTriangle(P[1], P[2], P[3]);
                    newObj.AddTriangle(P[3], P[4], P[1]);
                    P = new Vertex[] { new Vertex(0, 0, 0), new Vertex(0, 0, 0), new Vertex(0, 0, 0), new Vertex(0, 0, 0), new Vertex(0, 0, 0) };
                }
            }

            //Texturkoordinaten auf bereich zwischen 0 und 1 normieren
            for (i = 0; i < newObj.Triangles.Count; i++)
                for (int j = 0; j < newObj.Triangles[i].V.Length; j++)
                {
                    if (newObj.Triangles[i].V[j].TexcoordU > 1)
                        newObj.Triangles[i].V[j].TexcoordU = newObj.Triangles[i].V[j].TexcoordU % 1;
                    if (newObj.Triangles[i].V[j].TexcoordU < 0)
                        newObj.Triangles[i].V[j].TexcoordU = 1 - ((-newObj.Triangles[i].V[j].TexcoordU) % 1);

                    if (newObj.Triangles[i].V[j].TexcoordV > 1)
                    {
                        newObj.Triangles[i].V[j].TexcoordV = newObj.Triangles[i].V[j].TexcoordV % 1;
                    }
                    if (newObj.Triangles[i].V[j].TexcoordV < 0)
                        newObj.Triangles[i].V[j].TexcoordV = 1 - ((-newObj.Triangles[i].V[j].TexcoordV) % 1);
                }

            newObj.TransformToCoordinateOrigin();
            newObj.SetNormals();

            return newObj;
        }

        //Verbindet object1 mit object2, verschiebt aber object2 um transObject2
        public static TriangleList MergeObjects(TriangleList object1, TriangleList object2, Vector3D transObject2)
        {
            TriangleList newObject = new TriangleList();
            for (int i = 0; i < object1.Triangles.Count; i++) newObject.AddTriangle(new Vertex(object1.Triangles[i].V[0]), new Vertex(object1.Triangles[i].V[1]), new Vertex(object1.Triangles[i].V[2]));
            for (int i = 0; i < object2.Triangles.Count; i++)
            {
                Vertex V1 = new Vertex(object2.Triangles[i].V[0]);
                V1.Position = V1.Position + transObject2;
                Vertex V2 = new Vertex(object2.Triangles[i].V[1]);
                V2.Position = V2.Position + transObject2;
                Vertex V3 = new Vertex(object2.Triangles[i].V[2]);
                V3.Position = V3.Position + transObject2;
                newObject.AddTriangle(V1, V2, V3);
            }

            newObject.SetNormals();

            return newObject;
        }
    }
}
