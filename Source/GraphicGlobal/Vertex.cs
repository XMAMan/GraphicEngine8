using GraphicMinimal;

namespace GraphicGlobal
{
    public class Vertex : IParseableString
    {
        public Vector3D Position;
        public Vector3D Normal;
        public Vector3D Tangent;
        public float TexcoordU;
        public float TexcoordV;

        public Vector2D TextcoordVector
        {
            get
            {
                return new Vector2D(this.TexcoordU, this.TexcoordV);
            }
        }

        public Vertex(Vector3D position, Vector3D normal = null, Vector3D tangent = null, float textCoordU = 0, float textCoordV = 0)
        {
            this.Position = position;
            this.Normal = normal;
            this.Tangent = tangent;
            this.TexcoordU = textCoordU;
            this.TexcoordV = textCoordV;
        }

        public string ToCtorString()
        {
            return $"new Vertex({Position.ToCtorString()},{Normal.ToCtorString()},{ParseableStringExtension.ToCtorString(Tangent)},{TexcoordU.ToFloatString()},{TexcoordV.ToFloatString()})";
        }

        //Kopierkonstruktor
        public Vertex(Vertex sourceObj)
        {
            this.Position = new Vector3D(sourceObj.Position);
            this.Normal = sourceObj.Normal != null ? new Vector3D(sourceObj.Normal) : null;
            this.Tangent = sourceObj.Tangent != null ? new Vector3D(sourceObj.Tangent) : sourceObj.Tangent;
            this.TexcoordU = sourceObj.TexcoordU;
            this.TexcoordV = sourceObj.TexcoordV;
        }

        public Vertex(float x, float y, float z, float textCoordU, float textCoordV)
            : this(new Vector3D(x, y, z), null, null, textCoordU, textCoordV)
        {
        }

        public Vertex(float x, float y, float z)
            : this(new Vector3D(x, y, z), null, null, 0, 0)
        {
        }

        public static Vertex Interpolate(Vertex v1, Vertex v2, float f)
        {
            float f1 = (1 - f);
            Vertex V = new Vertex(0, 0, 0)
            {
                Position = v1.Position * f1 + v2.Position * f,
                Normal = v1.Normal * f1 + v2.Normal * f,
                //V.Tangent = v1.Tangent * f1 + v2.Tangent * f; //Tangente wird nicht interpoliert sondern direkt vom Dreieck/Quad genommen
                TexcoordU = v1.TexcoordU * f1 + v2.TexcoordU * f,
                TexcoordV = v1.TexcoordV * f1 + v2.TexcoordV * f
            };

            return V;
        }
    }
}
