using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using GraphicGlobal;

namespace TriangleObjectGeneration
{
    //Hilfsklasse, welche für den Bau eines TriangleObject-Objektes genutzt wird
    class TriangleList
    {
        public string Name = null;
        public List<Triangle> Triangles = new List<Triangle>();

        public TriangleList() { }

        //Kopierkonstruktor
        public TriangleList(TriangleList sourceObj)
        {
            this.Name = sourceObj.Name;
            for (int i = 0; i < sourceObj.Triangles.Count; i++)
                Triangles.Add(new Triangle(sourceObj.Triangles[i]));
        }

        public TriangleObject GetTriangleObject()
        {
            if (this.Triangles.Any() == false) return null;
            return new TriangleObject(this.Triangles.ToArray(), this.Name);
        }

        //Dreieck muss im Uhrzeigersinn angegeben werden
        public bool AddTriangle(Vertex p1, Vertex p2, Vertex p3)
        {
            if (p1.Position == p2.Position || p1.Position == p3.Position || p2.Position == p3.Position) return false; //Alle 3 Eckpunkte müssen sich unterscheiden

            //Kontrolle, ob Dreieck schon existiert
            foreach (Triangle E in this.Triangles)
            {
                if ((p1.Position == E.V[0].Position || p1.Position == E.V[1].Position || p1.Position == E.V[2].Position) &&
                    (p2.Position == E.V[0].Position || p2.Position == E.V[1].Position || p2.Position == E.V[2].Position) &&
                    (p3.Position == E.V[0].Position || p3.Position == E.V[1].Position || p3.Position == E.V[2].Position)) return true;
            }

            Vector3D N = Vector3D.Cross(p2.Position - p1.Position, p3.Position - p1.Position);
            if (N.Length() < 0.00000001f) return false;
            N = Vector3D.Normalize(N);
            if (Math.Abs(N.Length() - 1) > 0.1f) return false;

            this.Triangles.Add(new Triangle(new Vertex(p1), new Vertex(p2), new Vertex(p3)));
            return true;
        }

        //Viereck muss gegen den Uhrzeigersinn angegeben werden
        public bool AddQuad(Vertex p1, Vertex p2, Vertex p3, Vertex p4)
        {
            if (!AddTriangle(p1, p2, p3)) return false;
            if (!AddTriangle(p3, p4, p1)) return false;
            return true;
        }

        //P1 und P2 sind die Eckpunkte. P1 liegt links unten vorne, P2 rechts hinten oben
        public bool AddCube(Vector3D p1, Vector3D p2)
        {
            if (!AddQuad(new Vertex(p1.X, p1.Y, p1.Z, 0, 0), new Vertex(p2.X, p1.Y, p1.Z, 1, 0), new Vertex(p2.X, p2.Y, p1.Z, 1, 1), new Vertex(p1.X, p2.Y, p1.Z, 0, 1))) return false; //Vorne
            if (!AddQuad(new Vertex(p2.X, p1.Y, p1.Z, 0, 0), new Vertex(p2.X, p1.Y, p2.Z, 1, 0), new Vertex(p2.X, p2.Y, p2.Z, 1, 1), new Vertex(p2.X, p2.Y, p1.Z, 0, 1))) return false; //Rechts
            if (!AddQuad(new Vertex(p1.X, p1.Y, p2.Z, 0, 0), new Vertex(p1.X, p1.Y, p1.Z, 1, 0), new Vertex(p1.X, p2.Y, p1.Z, 1, 1), new Vertex(p1.X, p2.Y, p2.Z, 0, 1))) return false; //Links
            if (!AddQuad(new Vertex(p1.X, p2.Y, p1.Z, 0, 0), new Vertex(p2.X, p2.Y, p1.Z, 1, 0), new Vertex(p2.X, p2.Y, p2.Z, 1, 1), new Vertex(p1.X, p2.Y, p2.Z, 0, 1))) return false; //Oben
            if (!AddQuad(new Vertex(p1.X, p1.Y, p2.Z, 0, 0), new Vertex(p2.X, p1.Y, p2.Z, 1, 0), new Vertex(p2.X, p1.Y, p1.Z, 1, 1), new Vertex(p1.X, p1.Y, p1.Z, 0, 1))) return false; //Unten
            if (!AddQuad(new Vertex(p2.X, p1.Y, p2.Z, 0, 0), new Vertex(p1.X, p1.Y, p2.Z, 1, 0), new Vertex(p1.X, p2.Y, p2.Z, 1, 1), new Vertex(p2.X, p2.Y, p2.Z, 0, 1))) return false; //Hinten
            return true;
        }

        public bool AddCube(Vertex v0, Vertex v1, Vertex v2, Vertex v3, Vertex v4, Vertex v5, Vertex v6, Vertex v7)
        {
            if (!AddQuad(v0, v1, v2, v3)) return false;//Oben
            if (!AddQuad(v4, v5, v1, v0)) return false;//Vorne
            if (!AddQuad(v5, v6, v2, v1)) return false;//Rechts
            if (!AddQuad(v7, v4, v0, v3)) return false;//Links
            if (!AddQuad(v6, v7, v3, v2)) return false;//Hinten
            if (!AddQuad(v7, v6, v5, v4)) return false;//Unten
            return true;
        }

        public bool AddRoundedCube(Vector3D centerPoint, float size, float roundFactor)
        {
            size = size / roundFactor;

            Vector3D SX1 = centerPoint + new Vector3D(-size * roundFactor, +size, +size);//X-Stab
            Vector3D SX2 = centerPoint + new Vector3D(+size * roundFactor, +size, +size);
            Vector3D SX3 = centerPoint + new Vector3D(+size * roundFactor, +size, -size);
            Vector3D SX4 = centerPoint + new Vector3D(-size * roundFactor, +size, -size);
            Vector3D SX5 = centerPoint + new Vector3D(-size * roundFactor, -size, +size);
            Vector3D SX6 = centerPoint + new Vector3D(+size * roundFactor, -size, +size);
            Vector3D SX7 = centerPoint + new Vector3D(+size * roundFactor, -size, -size);
            Vector3D SX8 = centerPoint + new Vector3D(-size * roundFactor, -size, -size);

            Vector3D SY1 = centerPoint + new Vector3D(-size, +size * roundFactor, +size);//Y-Stab
            Vector3D SY2 = centerPoint + new Vector3D(+size, +size * roundFactor, +size);
            Vector3D SY3 = centerPoint + new Vector3D(+size, +size * roundFactor, -size);
            Vector3D SY4 = centerPoint + new Vector3D(-size, +size * roundFactor, -size);
            Vector3D SY5 = centerPoint + new Vector3D(-size, -size * roundFactor, +size);
            Vector3D SY6 = centerPoint + new Vector3D(+size, -size * roundFactor, +size);
            Vector3D SY7 = centerPoint + new Vector3D(+size, -size * roundFactor, -size);
            Vector3D SY8 = centerPoint + new Vector3D(-size, -size * roundFactor, -size);

            Vector3D SZ1 = centerPoint + new Vector3D(-size, +size, +size * roundFactor);//Z-Stab
            Vector3D SZ2 = centerPoint + new Vector3D(+size, +size, +size * roundFactor);
            Vector3D SZ3 = centerPoint + new Vector3D(+size, +size, -size * roundFactor);
            Vector3D SZ4 = centerPoint + new Vector3D(-size, +size, -size * roundFactor);
            Vector3D SZ5 = centerPoint + new Vector3D(-size, -size, +size * roundFactor);
            Vector3D SZ6 = centerPoint + new Vector3D(+size, -size, +size * roundFactor);
            Vector3D SZ7 = centerPoint + new Vector3D(+size, -size, -size * roundFactor);
            Vector3D SZ8 = centerPoint + new Vector3D(-size, -size, -size * roundFactor);

            float f = (roundFactor - 1) / 2 + 1;
            Vector3D P1 = centerPoint + new Vector3D(-size * f, +size * f, +size * f); //Mitte
            Vector3D P2 = centerPoint + new Vector3D(+size * f, +size * f, +size * f);
            Vector3D P3 = centerPoint + new Vector3D(+size * f, +size * f, -size * f);
            Vector3D P4 = centerPoint + new Vector3D(-size * f, +size * f, -size * f);
            Vector3D P5 = centerPoint + new Vector3D(-size * f, -size * f, +size * f);
            Vector3D P6 = centerPoint + new Vector3D(+size * f, -size * f, +size * f);
            Vector3D P7 = centerPoint + new Vector3D(+size * f, -size * f, -size * f);
            Vector3D P8 = centerPoint + new Vector3D(-size * f, -size * f, -size * f);

            AddQuad(                              //Oben
                new Vertex(SY1.X, SY1.Y, SY1.Z, 0, 0),
                new Vertex(SY2.X, SY2.Y, SY2.Z, 1, 0),
                new Vertex(SY3.X, SY3.Y, SY3.Z, 1, 1),
                new Vertex(SY4.X, SY4.Y, SY4.Z, 0, 1));
            AddQuad(                              //Oben,Vorne
                new Vertex(P1.X, P1.Y, P1.Z, 0, 0),
                new Vertex(P2.X, P2.Y, P2.Z, 0, 0),
                new Vertex(SY2.X, SY2.Y, SY2.Z, 0, 0),
                new Vertex(SY1.X, SY1.Y, SY1.Z, 0, 0));
            AddQuad(                              //Oben,Rechts
                new Vertex(P2.X, P2.Y, P2.Z, 0, 0),
                new Vertex(P3.X, P3.Y, P3.Z, 0, 0),
                new Vertex(SY3.X, SY3.Y, SY3.Z, 0, 0),
                new Vertex(SY2.X, SY2.Y, SY2.Z, 0, 0));
            AddQuad(                              //Oben,Links
                new Vertex(P4.X, P4.Y, P4.Z, 0, 0),
                new Vertex(P1.X, P1.Y, P1.Z, 0, 0),
                new Vertex(SY1.X, SY1.Y, SY1.Z, 0, 0),
                new Vertex(SY4.X, SY4.Y, SY4.Z, 0, 0));
            AddQuad(                              //Oben,Hinten
                new Vertex(P3.X, P3.Y, P3.Z, 0, 0),
                new Vertex(P4.X, P4.Y, P4.Z, 0, 0),
                new Vertex(SY4.X, SY4.Y, SY4.Z, 0, 0),
                new Vertex(SY3.X, SY3.Y, SY3.Z, 0, 0));

            AddQuad(                              //Vorne
               new Vertex(SZ5.X, SZ5.Y, SZ5.Z, 0, 0),
               new Vertex(SZ6.X, SZ6.Y, SZ6.Z, 1, 0),
               new Vertex(SZ2.X, SZ2.Y, SZ2.Z, 1, 1),
               new Vertex(SZ1.X, SZ1.Y, SZ1.Z, 0, 1));
            AddQuad(                              //Vorne,Oben
                new Vertex(SZ1.X, SZ1.Y, SZ1.Z, 0, 0),
                new Vertex(SZ2.X, SZ2.Y, SZ2.Z, 0, 0),
                new Vertex(P2.X, P2.Y, P2.Z, 0, 0),
                new Vertex(P1.X, P1.Y, P1.Z, 0, 0));
            AddQuad(                              //Vorne,Rechts
                new Vertex(SZ2.X, SZ2.Y, SZ2.Z, 0, 0),
                new Vertex(SZ6.X, SZ6.Y, SZ6.Z, 0, 0),
                new Vertex(P6.X, P6.Y, P6.Z, 0, 0),
                new Vertex(P2.X, P2.Y, P2.Z, 0, 0));
            AddQuad(                              //Vorne,Links
                new Vertex(P1.X, P1.Y, P1.Z, 0, 0),
                new Vertex(P5.X, P5.Y, P5.Z, 0, 0),
                new Vertex(SZ5.X, SZ5.Y, SZ5.Z, 0, 0),
                new Vertex(SZ1.X, SZ1.Y, SZ1.Z, 0, 0));
            AddQuad(                              //Vorne,Unten
                new Vertex(P5.X, P5.Y, P5.Z, 0, 0),
                new Vertex(P6.X, P6.Y, P6.Z, 0, 0),
                new Vertex(SZ6.X, SZ6.Y, SZ6.Z, 0, 0),
                new Vertex(SZ5.X, SZ5.Y, SZ5.Z, 0, 0));

            AddQuad(                              //Rechts
               new Vertex(SX6.X, SX6.Y, SX6.Z, 0, 0),
               new Vertex(SX7.X, SX7.Y, SX7.Z, 1, 0),
               new Vertex(SX3.X, SX3.Y, SX3.Z, 1, 1),
               new Vertex(SX2.X, SX2.Y, SX2.Z, 0, 1));
            AddQuad(                              //Rechts,Oben
                new Vertex(SX2.X, SX2.Y, SX2.Z, 0, 0),
                new Vertex(SX3.X, SX3.Y, SX3.Z, 0, 0),
                new Vertex(P3.X, P3.Y, P3.Z, 0, 0),
                new Vertex(P2.X, P2.Y, P2.Z, 0, 0));
            AddQuad(                              //Rechts,Vorne
                new Vertex(P2.X, P2.Y, P2.Z, 0, 0),
                new Vertex(P6.X, P6.Y, P6.Z, 0, 0),
                new Vertex(SX6.X, SX6.Y, SX6.Z, 0, 0),
                new Vertex(SX2.X, SX2.Y, SX2.Z, 0, 0));
            AddQuad(                              //Rechts,Hinten
                new Vertex(SX3.X, SX3.Y, SX3.Z, 0, 0),
                new Vertex(SX7.X, SX7.Y, SX7.Z, 0, 0),
                new Vertex(P7.X, P7.Y, P7.Z, 0, 0),
                new Vertex(P3.X, P3.Y, P3.Z, 0, 0));
            AddQuad(                              //Rechts,Unten
                new Vertex(P6.X, P6.Y, P6.Z, 0, 0),
                new Vertex(P7.X, P7.Y, P7.Z, 0, 0),
                new Vertex(SX7.X, SX7.Y, SX7.Z, 0, 0),
                new Vertex(SX6.X, SX6.Y, SX6.Z, 0, 0));

            AddQuad(                              //Links
               new Vertex(SX8.X, SX8.Y, SX8.Z, 0, 0),
               new Vertex(SX5.X, SX5.Y, SX5.Z, 1, 0),
               new Vertex(SX1.X, SX1.Y, SX1.Z, 1, 1),
               new Vertex(SX4.X, SX4.Y, SX4.Z, 0, 1));
            AddQuad(                              //Links,Oben
                new Vertex(SX4.X, SX4.Y, SX4.Z, 0, 0),
                new Vertex(SX1.X, SX1.Y, SX1.Z, 0, 0),
                new Vertex(P1.X, P1.Y, P1.Z, 0, 0),
                new Vertex(P4.X, P4.Y, P4.Z, 0, 0));
            AddQuad(                              //Links,Vorne
                new Vertex(SX1.X, SX1.Y, SX1.Z, 0, 0),
                new Vertex(SX5.X, SX5.Y, SX5.Z, 0, 0),
                new Vertex(P5.X, P5.Y, P5.Z, 0, 0),
                new Vertex(P1.X, P1.Y, P1.Z, 0, 0));
            AddQuad(                              //Links,Hinten
                new Vertex(SX8.X, SX8.Y, SX8.Z, 0, 0),
                new Vertex(SX4.X, SX4.Y, SX4.Z, 0, 0),
                new Vertex(P4.X, P4.Y, P4.Z, 0, 0),
                new Vertex(P8.X, P8.Y, P8.Z, 0, 0));
            AddQuad(                              //Links,Unten
                new Vertex(P8.X, P8.Y, P8.Z, 0, 0),
                new Vertex(P5.X, P5.Y, P5.Z, 0, 0),
                new Vertex(SX5.X, SX5.Y, SX5.Z, 0, 0),
                new Vertex(SX8.X, SX8.Y, SX8.Z, 0, 0));

            AddQuad(                              //Hinten
               new Vertex(SZ7.X, SZ7.Y, SZ7.Z, 0, 0),
               new Vertex(SZ8.X, SZ8.Y, SZ8.Z, 1, 0),
               new Vertex(SZ4.X, SZ4.Y, SZ4.Z, 1, 1),
               new Vertex(SZ3.X, SZ3.Y, SZ3.Z, 0, 1));
            AddQuad(                              //Hinten,Oben
                new Vertex(SZ3.X, SZ3.Y, SZ3.Z, 0, 0),
                new Vertex(SZ4.X, SZ4.Y, SZ4.Z, 0, 0),
                new Vertex(P4.X, P4.Y, P4.Z, 0, 0),
                new Vertex(P3.X, P3.Y, P3.Z, 0, 0));
            AddQuad(                              //Hinten,Rechts
                new Vertex(SZ7.X, SZ7.Y, SZ7.Z, 0, 0),
                new Vertex(SZ3.X, SZ3.Y, SZ3.Z, 0, 0),
                new Vertex(P3.X, P3.Y, P3.Z, 0, 0),
                new Vertex(P7.X, P7.Y, P7.Z, 0, 0));
            AddQuad(                              //Hinten,Links
                new Vertex(P8.X, P8.Y, P8.Z, 0, 0),
                new Vertex(P4.X, P4.Y, P4.Z, 0, 0),
                new Vertex(SZ4.X, SZ4.Y, SZ4.Z, 0, 0),
                new Vertex(SZ8.X, SZ8.Y, SZ8.Z, 0, 0));
            AddQuad(                              //Hinten,Unten
                new Vertex(P8.X, P8.Y, P8.Z, 0, 0),
                new Vertex(P7.X, P7.Y, P7.Z, 0, 0),
                new Vertex(SZ7.X, SZ7.Y, SZ7.Z, 0, 0),
                new Vertex(SZ8.X, SZ8.Y, SZ8.Z, 0, 0));

            AddQuad(                              //Unten
                new Vertex(SY8.X, SY8.Y, SY8.Z, 0, 0),
                new Vertex(SY7.X, SY7.Y, SY7.Z, 1, 0),
                new Vertex(SY6.X, SY6.Y, SY6.Z, 1, 1),
                new Vertex(SY5.X, SY5.Y, SY5.Z, 0, 1));
            AddQuad(                              //Unten,Vorne
                new Vertex(P6.X, P6.Y, P6.Z, 0, 0),
                new Vertex(P5.X, P5.Y, P5.Z, 0, 0),
                new Vertex(SY5.X, SY5.Y, SY5.Z, 0, 0),
                new Vertex(SY6.X, SY6.Y, SY6.Z, 0, 0));
            AddQuad(                              //Unten,Rechts
                new Vertex(P7.X, P7.Y, P7.Z, 0, 0),
                new Vertex(P6.X, P6.Y, P6.Z, 0, 0),
                new Vertex(SY6.X, SY6.Y, SY6.Z, 0, 0),
                new Vertex(SY7.X, SY7.Y, SY7.Z, 0, 0));
            AddQuad(                              //Unten,Links
                new Vertex(P5.X, P5.Y, P5.Z, 0, 0),
                new Vertex(P8.X, P8.Y, P8.Z, 0, 0),
                new Vertex(SY8.X, SY8.Y, SY8.Z, 0, 0),
                new Vertex(SY5.X, SY5.Y, SY5.Z, 0, 0));
            AddQuad(                              //Unten,Hinten
                new Vertex(P8.X, P8.Y, P8.Z, 0, 0),
                new Vertex(P7.X, P7.Y, P7.Z, 0, 0),
                new Vertex(SY7.X, SY7.Y, SY7.Z, 0, 0),
                new Vertex(SY8.X, SY8.Y, SY8.Z, 0, 0));

            return true;
        }
        
        public void TransformToCoordinateOrigin()
        {
            int i, j;
            float MaxX, MaxY, MaxZ, MinX, MinY, MinZ, XD, YD, ZD;

            if (this.Triangles.Count > 0)
            {
                MaxX = MinX = this.Triangles[0].V[0].Position.X;
                MaxY = MinY = this.Triangles[0].V[0].Position.Y;
                MaxZ = MinZ = this.Triangles[0].V[0].Position.Z;

                for (i = 0; i < this.Triangles.Count; i++)
                    for (j = 0; j < 3; j++)
                    {
                        if (this.Triangles[i].V[j].Position.X > MaxX) MaxX = this.Triangles[i].V[j].Position.X;
                        if (this.Triangles[i].V[j].Position.X < MinX) MinX = this.Triangles[i].V[j].Position.X;
                        if (this.Triangles[i].V[j].Position.Y > MaxY) MaxY = this.Triangles[i].V[j].Position.Y;
                        if (this.Triangles[i].V[j].Position.Y < MinY) MinY = this.Triangles[i].V[j].Position.Y;
                        if (this.Triangles[i].V[j].Position.Z > MaxZ) MaxZ = this.Triangles[i].V[j].Position.Z;
                        if (this.Triangles[i].V[j].Position.Z < MinZ) MinZ = this.Triangles[i].V[j].Position.Z;
                    }
                XD = (MaxX - MinX) / 2 + MinX;
                YD = (MaxY - MinY) / 2 + MinY;
                ZD = (MaxZ - MinZ) / 2 + MinZ;

                for (i = 0; i < this.Triangles.Count; i++) for (j = 0; j < 3; j++) this.Triangles[i].V[j].Position -= new Vector3D(XD, YD, ZD);
            }
        }

        public void SetNormals()
        {
            List<Triangle> newTriangles = new List<Triangle>();
            foreach (var oldTriangle in this.Triangles)
            {
                newTriangles.Add(new Triangle(oldTriangle));
            }

            int i, j, k;
            Vector3D N = null;

            for (i = 0; i < this.Triangles.Count; i++)
            {
                for (j = 0; j < 3; j++)
                {
                    N = this.Triangles[i].Normal;

                    for (k = 0; k < this.Triangles.Count; k++)
                        if (i != k &&
                            (this.Triangles[k].V[0].Position == this.Triangles[i].V[j].Position ||
                            this.Triangles[k].V[1].Position == this.Triangles[i].V[j].Position ||
                            this.Triangles[k].V[2].Position == this.Triangles[i].V[j].Position))
                        {
                            N += this.Triangles[k].Normal;
                        }

                    if (N.Length() > 0.0001f)
                        newTriangles[i].V[j].Normal = Vector3D.Normalize(N);
                    else
                        newTriangles[i].V[j].Normal = this.Triangles[i].Normal;
                }
            }

            this.Triangles = newTriangles;
        }

        public void Move(Vector3D translate)
        {
            for (int i = 0; i < Triangles.Count; i++)
                for (int j = 0; j < 3; j++)
                    Triangles[i].V[j].Position += translate;
        }
    }
}
