using System;
using GraphicMinimal;

namespace GraphicGlobal
{
    public class Triangle2D
    {
        public Vertex2D P1 { get; private set; }
        public Vertex2D P2 { get; private set; }
        public Vertex2D P3 { get; private set; }

        public Triangle2D(Vertex2D p1, Vertex2D p2, Vertex2D p3)
        {
            this.P1 = p1;
            this.P2 = p2;
            this.P3 = p3;
        }

        public float GetSurfaceArea()
        {
            Vector3D v1 = new Vector3D(this.P3.X - this.P2.X, this.P3.Y - this.P2.Y, 0);
            Vector3D v2 = new Vector3D(this.P2.X - this.P1.X, this.P2.Y - this.P1.Y, 0);
            float pa2 = v1.X * v2.Y - v2.X * v1.Y;
            return (float)Math.Sqrt(pa2 * pa2) * 0.5f;
        }

        public bool IsPointInsideTriangle(Vector2D point)
        {
            //Es wird angenommen, dass die 3 Eckpunkte gegen den Uhrzeigersinn angegeben wurden

            //Bilde Kreuzprodukt zwischen Kantenvektor und Kante-Start-zu-Point-Vektor und schaue, dass Punkt bei allen 3 Kanten auf der Innenseite liegt
            bool b1 = (P2.X - P1.X) * (point.Y - P1.Y) - (point.X - P1.X) * (P2.Y - P1.Y) > 0;
            bool b2 = (P3.X - P2.X) * (point.Y - P2.Y) - (point.X - P2.X) * (P3.Y - P2.Y) > 0;
            bool b3 = (P1.X - P3.X) * (point.Y - P3.Y) - (point.X - P3.X) * (P1.Y - P3.Y) > 0;
            return b1 && b2 && b3;
        }
    }

    
}
