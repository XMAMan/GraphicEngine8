using GraphicMinimal;

namespace GraphicGlobal
{
    public class Triangle2DIPoint
    {
        public IPoint2D P1 { get; private set; }
        public IPoint2D P2 { get; private set; }
        public IPoint2D P3 { get; private set; }

        public Triangle2DIPoint(IPoint2D p1, IPoint2D p2, IPoint2D p3)
        {
            this.P1 = p1;
            this.P2 = p2;
            this.P3 = p3;
        }
    }
}
