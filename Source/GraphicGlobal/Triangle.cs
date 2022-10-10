using System;
using System.Linq;
using GraphicMinimal;

namespace GraphicGlobal
{
    public class Triangle : IDivideable, IParseableString
    {
        public Vertex[] V { get; private set; }		    // Die 3 Eckpunkte des Dreiecks
        public Vector3D Normal { get; private set; }
        public Vector3D Tangent { get; private set; }        

        public Triangle(Vertex p1, Vertex p2, Vertex p3)
        {
            //TBN-Daten berechnen 
            TBNData data = CalculateTangentAndBinormal(p1, p2, p3);
            this.Normal = data.Normal;
            this.Tangent = data.Tangent;
                        
            this.V = new Vertex[] { p1, p2, p3 };

            //Ein Dreieck hat nur eine Tangente! Diese darf nicht über die Eckpunkte interpoliert werden
            this.V[0].Tangent = this.Tangent;
            this.V[1].Tangent = this.Tangent;
            this.V[2].Tangent = this.Tangent;

            Vector3D pa2 = Vector3D.Cross(this.V[2].Position - this.V[1].Position, this.V[1].Position - this.V[0].Position);
            this.SurfaceArea = (float)Math.Sqrt(pa2 * pa2) * 0.5f;            
        }

        

        public Triangle(Vector3D p1, Vector3D p2, Vector3D p3)
            : this(new Vertex(p1, new Vector3D(0, 0, 0), new Vector3D(1, 0, 0)), new Vertex(p2, new Vector3D(0, 0, 0), new Vector3D(1, 0, 0)), new Vertex(p3, new Vector3D(0, 0, 0), new Vector3D(1, 0, 0)))
        {            
        }

        //Kopierkonstruktor
        public Triangle(Triangle sourceObj)
        {
            this.V = new Vertex[] { new Vertex(sourceObj.V[0]), new Vertex(sourceObj.V[1]), new Vertex(sourceObj.V[2]) };
            this.Normal = sourceObj.Normal;
            this.Tangent = sourceObj.Tangent;
            this.SurfaceArea = sourceObj.SurfaceArea;
        }

        public string ToCtorString()
        {
            return $"new Triangle({V[0].ToCtorString()},{V[1].ToCtorString()},{V[2].ToCtorString()})";
        }

        public override string ToString()
        {
            if (this.V != null)
                return string.Join(" ", V.Select(x => x.Position.ToString()));
            return base.ToString();
        }

        //http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-13-normal-mapping/
        public static TBNData CalculateTangentAndBinormal(Vertex p1, Vertex p2, Vertex p3)
        {
            TBNData data = new TBNData
            {
                Normal = Vector3D.Normalize(Vector3D.Cross(p2.Position - p1.Position, p3.Position - p1.Position))
            };

            // Edges of the triangle : postion delta
            Vector3D deltaPos1 = p2.Position - p1.Position;
            Vector3D deltaPos2 = p3.Position - p1.Position;

            // UV delta
            Vector2D deltaUV1 = p2.TextcoordVector - p1.TextcoordVector;
            Vector2D deltaUV2 = p3.TextcoordVector - p1.TextcoordVector;

            //We can now use our formula to compute the tangent and the bitangent :
            float fDenominator = deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X;
            if (Math.Abs(fDenominator) > 0.001f)
            {
                float r = 1.0f / fDenominator;
                data.Tangent = Vector3D.Normalize((deltaPos1 * deltaUV2.Y - deltaPos2 * deltaUV1.Y) * r);
                data.Binominal = Vector3D.Normalize(Vector3D.Cross(data.Tangent, data.Normal));
            }
            else
            {
                data.Tangent = Vector3D.Normalize(deltaPos1);
                data.Binominal = Vector3D.Normalize(Vector3D.Cross(data.Tangent, data.Normal));
            }

            if (Math.Abs(data.Normal.Length() - 1) > 0.1f) throw new Exception("Normal must have length 1");
            if (Math.Abs(data.Tangent.Length() - 1) > 0.1f) throw new Exception("Tangent must have length 1");
            if (Math.Abs((data.Normal * data.Tangent)) > 0.1f) throw new Exception("Normal must be perpendicular to the tangent");

            return data;
        }

        public class TBNData
        {
            public Vector3D Normal = null;
            public Vector3D Tangent = null;
            public Vector3D Binominal = null;
        }

        #region IDivideable
        public float SurfaceArea { get; private set; }
        public virtual IDivideable[] Divide()
        {
            Vector3D l1 = this.V[1].Position - this.V[0].Position;
            Vector3D l2 = this.V[2].Position - this.V[1].Position;
            Vector3D l3 = this.V[0].Position - this.V[2].Position;
            float ll1 = l1.Length(), ll2 = l2.Length(), ll3 = l3.Length();
            IDivideable t2;
            IDivideable t1;
            if (ll1 > ll2 && ll1 > ll3)
            {
                Vertex m = Vertex.Interpolate(this.V[0], this.V[1], 0.5f);
                t1 = new Triangle(this.V[0], m, this.V[2]);
                t2 = new Triangle(m, this.V[1], this.V[2]);
            }
            else
                if (ll2 > ll1 && ll2 > ll3)
            {
                Vertex m = Vertex.Interpolate(this.V[1], this.V[2], 0.5f);
                t1 = new Triangle(this.V[2], this.V[0], m);
                t2 = new Triangle(this.V[0], this.V[1], m);
            }
            else
            {
                Vertex m = Vertex.Interpolate(this.V[2], this.V[0], 0.5f);
                t1 = new Triangle(m, this.V[0], this.V[1]);
                t2 = new Triangle(this.V[2], m, this.V[1]);
            }

            return new IDivideable[] { t1, t2 };
        }
        #endregion
    }
}
