using GraphicMinimal;
using GraphicPipelineCPU.ObjToWindowSpaceConversion;

namespace GraphicPipelineCPU.Rasterizer
{
    class WindowSpaceTriangle
    {
        public WindowSpacePoint W0 { get; private set; }
        public WindowSpacePoint W1 { get; private set; }
        public WindowSpacePoint W2 { get; private set; }

        public bool IsCounterClockwise { get; private set; } //Fürs Backfaceculling

        private float area; //Für die GetByzentricCoordinate-Methode
        public WindowSpaceTriangle(WindowSpacePoint w0, WindowSpacePoint w1, WindowSpacePoint w2)
        {
            this.W0 = w0;
            this.W1 = w1;
            this.W2 = w2;
            this.IsCounterClockwise = GetIsCounterClockwise();
 
            this.area = EdgeFunction(this.W0.WindowPos.XY, this.W1.WindowPos.XY, this.W2.WindowPos.XY);
        }

        public override string ToString()
        {
            return W0.WindowPos.ToString() + " " + W1.WindowPos.ToString() + " " + W2.WindowPos.ToString();
        }

        //https://math.stackexchange.com/questions/1324179/how-to-tell-if-3-connected-points-are-connected-clockwise-or-counter-clockwise
        private bool GetIsCounterClockwise()
        {
            float A = W1.WindowPos.X * W0.WindowPos.Y + W2.WindowPos.X * W1.WindowPos.Y + W0.WindowPos.X * W2.WindowPos.Y;
            float B = W0.WindowPos.X * W1.WindowPos.Y + W1.WindowPos.X * W2.WindowPos.Y + W2.WindowPos.X * W0.WindowPos.Y;
            return A > B;
        }

        public Vector3D GetByzentricCoordinate(Vector2D p)
        {
            float w0 = EdgeFunction(this.W1.WindowPos.XY, this.W2.WindowPos.XY, p);
            float w1 = EdgeFunction(this.W2.WindowPos.XY, this.W0.WindowPos.XY, p);
            float w2 = EdgeFunction(this.W0.WindowPos.XY, this.W1.WindowPos.XY, p);

            w0 /= this.area;
            w1 /= this.area;
            w2 /= this.area;

            return new Vector3D(w0, w1, w2);
        }

        private static float EdgeFunction(Vector2D a, Vector2D b, Vector2D c)
        {
            return (c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X);
        }
    }
}
