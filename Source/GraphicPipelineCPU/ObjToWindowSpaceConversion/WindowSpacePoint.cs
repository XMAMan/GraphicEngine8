using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper.Helper3D;
using System.Drawing;

namespace GraphicPipelineCPU.ObjToWindowSpaceConversion
{
    class WindowSpacePoint : IPoint2D
    {
        public WindowSpacePoint(Vector3D windowPos, Interpolationvariables v)
        {
            this.WindowPos = windowPos;
            this.XY = new Point((int)(windowPos.X), (int)(windowPos.Y));
            this.V = v;
        }

        public WindowSpacePoint GetCopy()
        {
            var copy = new WindowSpacePoint(new Vector3D(this.WindowPos.X, this.WindowPos.Y, this.Z), this.V.GetCopy());
            return copy;
        }

        public override string ToString()
        {
            return string.Format("[X={0}, Y={1}, Z={2}]", XY.X, XY.Y, Z);
        }

        public Vector3D WindowPos { get; private set; } //XY = Fensterkoordinaten als Float
        public Interpolationvariables V { get; private set; }
        public Point XY { get; private set; }      // Fensterkoordinaten als Integer
        public float Z { get => WindowPos.Z; }     // Tiefenwert: Liegt zwischen 0 und 1


        //f geht von 0 bis 1
        public static WindowSpacePoint InterpolateLinear(WindowSpacePoint p1, WindowSpacePoint p2, float f)
        {
            var variables = Interpolationvariables.InterpolateLinear(p1.V, p2.V, f);

            Vector3D winPos = p1.WindowPos * (1 - f) + p2.WindowPos * f;

            return new WindowSpacePoint(winPos, variables);
        }

        public static WindowSpacePoint InterpolateByzentric(WindowSpacePoint p0, WindowSpacePoint p1, WindowSpacePoint p2, float w0, float w1, float w2)
        {
            var variables = Interpolationvariables.InterpolateByzentric(p0.V, p1.V, p2.V, w0, w1, w2);

            Vector3D winPos = w0 * p0.WindowPos + w1 * p1.WindowPos + w2 * p2.WindowPos;

            return new WindowSpacePoint(winPos, variables);
        }

        #region IPoint2D Member

        float IPoint2D.X
        {
            get { return this.WindowPos.X; }
        }

        float IPoint2D.Y
        {
            get { return this.WindowPos.Y; }
        }

        #endregion
    }
}
