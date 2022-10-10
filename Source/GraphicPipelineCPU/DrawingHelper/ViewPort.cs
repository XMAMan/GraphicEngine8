using GraphicMinimal;
using System.Drawing;

namespace GraphicPipelineCPU.DrawingHelper
{
    class ViewPort
    {
        private Rectangle rectangle;
        public ViewPort(int x, int y, int width, int height)
        {
            this.rectangle = new Rectangle(x, y, width, height);
        }

        public int Left { get => this.rectangle.Left;  }
        public int Right { get => this.rectangle.Right; }
        public int Top { get => this.rectangle.Top; }
        public int Bottom { get => this.rectangle.Bottom; }

        //ViewPort-Transformation bedeutet man hat eine 2D-Koordinate in ein 0..1-Bereich oder -1..+1-Bereich und
        //ich lege nun für diese Koordinate fest wo auf dem Bildschirm sie erscheinen soll

        //v=0..1
        public Vector2D TransformIntoViewPort(Vector2D v)
        {
            float x = this.rectangle.X + v.X * this.rectangle.Width;
            float y = this.rectangle.Y + v.Y * this.rectangle.Height;
            return new Vector2D(x, y);
        }

        //s.Width/Height = 0..1
        public Size TransformIntoViewPort(SizeF s)
        {
            return new Size((int)(s.Width * this.rectangle.Width), (int)(s.Height * this.rectangle.Height));
        }

        //http://www.songho.ca/opengl/gl_transform.html
        //Umrechnen einer Normalized Device Coordinate in eine WindowCoordinate
        //v.x/y/z = -1 .. +1
        public Vector3D TransformIntoViewPort(Vector3D v)
        {
            float wideHalf = this.rectangle.Width / 2f;                 //umrechnen in Fensterkoordinaten
            float highHalf = this.rectangle.Height / 2f;

            return new Vector3D(wideHalf * v.X + (this.rectangle.X + wideHalf),
                              highHalf * (0 - v.Y) + (this.rectangle.Y + highHalf),
                              (v.Z + 1) / 2);
        }
    }
}
