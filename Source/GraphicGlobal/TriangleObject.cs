using System.Linq;
using System.Text;
using GraphicMinimal;

namespace GraphicGlobal
{
    public class TriangleObject
    {
        public Triangle[] Triangles { get; private set; }
        public string Name { get; private set; } //Entspricht entweder den Aufrufparametern aus dem TriangleObjectGenerator oder den Name aus der Obj-Datei
        public string MaterialName { get; set; } = null; //Material-Name aus der WaveFront-Datei, welche auf Bereich aus mtl-Datei verweist

        //Wird intern berechnet
        public Vector3D CenterPoint { get; private set; }
        public float Radius { get; private set; }

        public TriangleObject(Triangle[] triangles, string name)
        {
            this.Triangles = triangles;
            this.Name = name;

            var box = this.Triangles.GetBoundingBox();
            this.CenterPoint = box.Center;
            this.Radius = box.RadiusOutTheBox;
        }

        public TriangleObject(TriangleObject copy)
        {
            this.Triangles = new Triangle[copy.Triangles.Length];
            for (int i = 0; i < this.Triangles.Length; i++) this.Triangles[i] = new Triangle(copy.Triangles[i]);
            this.Name = copy.Name;
            this.MaterialName = copy.MaterialName;
            this.CenterPoint = new Vector3D(copy.CenterPoint);
            this.Radius = copy.Radius;
        }

        public void MoveTrianglePoints(Vector3D translate)
        {
            for (int i = 0; i < this.Triangles.Length; i++)
                for (int j = 0; j < 3; j++)
                    Triangles[i].V[j].Position += translate;
        }

        public string Print()
        {
            StringBuilder str = new StringBuilder(this.Name + "\n");
            foreach (var t in this.Triangles)
            {
                foreach (var v in t.V)
                {
                    str.Append(v.Position + " " + v.Normal + "#");
                }
                str.Append("\n");
            }
            return str.ToString();
        }
    }
}
