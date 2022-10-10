namespace GraphicMinimal
{
    public class Vertex2D : IPoint2D
    {
        public Vector2D Position { get; private set; }
        public Vector2D Textcoord { get; private set; }

        public int Xi
        {
            get
            {
                return (int)(Position.X + 0.5f);
            }
        }

        public int Yi
        {
            get
            {
                return (int)(Position.Y + 0.5f);
            }
        }

        public Vertex2D(float x, float y, float u, float v)
        {
            this.Position = new Vector2D(x, y);
            this.Textcoord = new Vector2D(u, v);
        }

        public Vertex2D(Vector2D pos, Vector2D textcoord)
        {
            this.Position = pos;
            this.Textcoord = textcoord;
        }

        public float X
        {
            get { return this.Position.X; }
        }

        public float Y
        {
            get { return this.Position.Y; }
        }

        public float TexcoordU
        {
            get { return this.Textcoord.X; }
        }

        public float TexcoordV
        {
            get { return this.Textcoord.Y; }
        }
    }
}
