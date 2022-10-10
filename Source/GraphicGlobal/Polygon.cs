using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GraphicMinimal;

namespace GraphicGlobal
{
    public class Polygon
    {
        public List<IPoint2D> Points { get; private set; }

        public Polygon(List<Vertex2D> points)
        {
            this.Points = points.Cast<IPoint2D>().ToList();
        }

        public Polygon(List<Vector2D> points)
        {
            this.Points = points.Cast<IPoint2D>().ToList();
        }

        public List<Triangle2D> TransformToTriangleList()
        {
            if (this.Points.First() is Vertex2D)
            {
                List<Triangle2D> triangleList = new List<Triangle2D>();
                List<Triangle2DIPoint> triangles = TriangleHelper.TransformPolygonToTriangleList(Points.Cast<IPoint2D>().ToList());

                foreach (Triangle2DIPoint triangle in triangles)
                {
                    triangleList.Add(
                        new Triangle2D(
                            new Vertex2D(triangle.P1.X, triangle.P1.Y, (triangle.P1 as Vertex2D).TexcoordU, (triangle.P1 as Vertex2D).TexcoordV),
                            new Vertex2D(triangle.P2.X, triangle.P2.Y, (triangle.P2 as Vertex2D).TexcoordU, (triangle.P2 as Vertex2D).TexcoordV),
                            new Vertex2D(triangle.P3.X, triangle.P3.Y, (triangle.P3 as Vertex2D).TexcoordU, (triangle.P3 as Vertex2D).TexcoordV)
                            )
                        );
                }

                return triangleList;
            }

            float minX = Points.Min(x => x.X);
            float minY = Points.Min(x => x.Y);
            float maxX = Points.Max(x => x.X);
            float maxY = Points.Max(x => x.Y);

            return TransformToTriangleList(new RectangleF(minX, minY, maxX - minX, maxY - minY));
        }

        public List<Triangle2D> TransformToTriangleList(RectangleF texturRec)
        {
            List<Triangle2D> triangleList = new List<Triangle2D>();
            List<Triangle2DIPoint> triangles = TriangleHelper.TransformPolygonToTriangleList(Points.Cast<IPoint2D>().ToList());

            foreach (Triangle2DIPoint triangle in triangles)
            {
                triangleList.Add(
                    new Triangle2D(
                        new Vertex2D(triangle.P1.X, triangle.P1.Y, (float)((triangle.P1.X - texturRec.Left) / texturRec.Width), (float)((triangle.P1.Y - texturRec.Top) / texturRec.Height)),
                        new Vertex2D(triangle.P2.X, triangle.P2.Y, (float)((triangle.P2.X - texturRec.Left) / texturRec.Width), (float)((triangle.P2.Y - texturRec.Top) / texturRec.Height)),
                        new Vertex2D(triangle.P3.X, triangle.P3.Y, (float)((triangle.P3.X - texturRec.Left) / texturRec.Width), (float)((triangle.P3.Y - texturRec.Top) / texturRec.Height))
                    ));
            }

            return triangleList;
        }

        public float GetSurfaceArea()
        {
            return TransformToTriangleList().Sum(x => x.GetSurfaceArea());
        }

        public bool IsPointInsidePolygon(Vector2D point)
        {
            return TransformToTriangleList().Any(x => x.IsPointInsideTriangle(point));
        }
    }
}
