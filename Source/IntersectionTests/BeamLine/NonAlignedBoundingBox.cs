using GraphicMinimal;
using System.Collections.Generic;

namespace IntersectionTests.BeamLine
{
    //Quadar, welcher nicht axial ausgerichet ist
    public class NonAlignedBoundingBox
    {
        public Vector3D Pos { get; private set; } //Eine Ecke vom Quader
        public Vector3D V1 { get; private set; }  //Nicht normierter Richtungsvektor (Kante 1)
        public Vector3D V2 { get; private set; }  //Nicht normierter Richtungsvektor (Kante 2)
        public Vector3D V3 { get; private set; }  //Nicht normierter Richtungsvektor (Kante 3)

        public NonAlignedBoundingBox(Vector3D pos, Vector3D v1, Vector3D v2, Vector3D v3)
        {
            this.Pos = pos;
            this.V1 = v1;
            this.V2 = v2;
            this.V3 = v3;
        }

        public NonAlignedBoundingBox(BoundingBox alignedBox)
        {
            this.Pos = alignedBox.Min;
            this.V1 = new Vector3D(alignedBox.Max.X - alignedBox.Min.X, 0, 0);
            this.V2 = new Vector3D(0, alignedBox.Max.Y - alignedBox.Min.Y, 0);
            this.V3 = new Vector3D(0, 0, alignedBox.Max.Z - alignedBox.Min.Z);
        }

        public BoundingBox GetAxialAlignedBoundingBox()
        {
            List<Vector3D> points = new List<Vector3D>();

            points.Add(this.Pos);
            points.Add(this.Pos + this.V1);
            points.Add(this.Pos + this.V1 + this.V2);
            points.Add(this.Pos + this.V2);
            points.Add(this.Pos + this.V3);
            points.Add(this.Pos + this.V1 + this.V3);
            points.Add(this.Pos + this.V1 + this.V2 + this.V3);
            points.Add(this.Pos + this.V2 + this.V3);

            Vector3D min = new Vector3D(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3D max = new Vector3D(float.MinValue, float.MinValue, float.MinValue);
            foreach (var p in points)
            {
                if (p.X < min.X) min.X = p.X;
                if (p.Y < min.Y) min.Y = p.Y;
                if (p.Z < min.Z) min.Z = p.Z;

                if (p.X > max.X) max.X = p.X;
                if (p.Y > max.Y) max.Y = p.Y;
                if (p.Z > max.Z) max.Z = p.Z;
            }
            return new BoundingBox(min, max);
        }
    }
}
