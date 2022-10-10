using GraphicMinimal;
using System;

namespace GraphicGlobal
{
    public class Quad : IDivideable, IParseableString
    {
        public Vertex v1, v2, v3, v4;
        public Vector3D edgePos1, edgePos2;

        public Vector3D Normal { get; private set; }
        public Vector3D Tangent { get; private set; }

        public Quad(Vertex v1, Vertex v2, Vertex v3, Vertex v4)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
            this.v4 = v4;

            this.edgePos1 = v2.Position - v1.Position;
            this.edgePos2 = v4.Position - v1.Position;
            Vector3D pa2 = Vector3D.Cross(edgePos1, edgePos2);
            this.SurfaceArea = (float)Math.Sqrt(pa2 * pa2);

            var data = Triangle.CalculateTangentAndBinormal(v1, v2, v3);
            this.Normal = data.Normal;
            this.Tangent = data.Tangent;
        }

        public string ToCtorString()
        {
            return $"new Quad({v1.ToCtorString()},{v2.ToCtorString()},{v3.ToCtorString()},{v4.ToCtorString()})";
        }

        //Kopierkonstruktor
        public Quad(Quad copy)
        {
            this.v1 = new Vertex(copy.v1);
            this.v2 = new Vertex(copy.v2);
            this.v3 = new Vertex(copy.v3);
            this.v4 = new Vertex(copy.v4);
            this.edgePos1 = new Vector3D(copy.edgePos1);
            this.edgePos2 = new Vector3D(copy.edgePos2);
            this.Normal = new Vector3D(copy.Normal);
            this.Tangent = new Vector3D(copy.Tangent);
            this.SurfaceArea = copy.SurfaceArea;
        }

        public override string ToString()
        {
            return "[" + this.v1.Position.ToString() + "|" + this.v2.Position.ToString() + "|" + this.v3.Position.ToString() + "|" + this.v4.Position.ToString() + "]";
        }

        #region IDivideable
        public float SurfaceArea { get; private set; }

        public virtual IDivideable[] Divide()
        {
            double l1 = this.edgePos1.Length(), l2 = this.edgePos2.Length();
            if (l1 > l2)
            {

                Vertex p1 = Vertex.Interpolate(v1, v2, 0.5f), p2 = Vertex.Interpolate(v3, v4, 0.5f);
                return new IDivideable[]
                {
                    new Quad(v1, p1, p2, v4),
                    new Quad(p1, v2, v3, p2),
                };
            }
            else
            {
                Vertex p1 = Vertex.Interpolate(v4, v1, 0.5f), p2 = Vertex.Interpolate(v2, v3, 0.5f);
                return new IDivideable[]
                {
                    new Quad(v1, v2, p2, p1),
                    new Quad(p1, p2, v3, v4),
                };
            }
        }
        #endregion
    }
}
