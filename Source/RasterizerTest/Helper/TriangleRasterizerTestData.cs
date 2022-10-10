using GraphicGlobal;
using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RasterizerTest.Helper
{
    class TriangleRasterizerTestData
    {
        //Quelle: https://docs.microsoft.com/en-us/windows/win32/direct3d11/d3d10-graphics-programming-guide-rasterizer-stage-rules
        //Dort das erste Bild mit den Dreiecken 
        public static Triangle[] GetTrianglesFromDirectXWebPage()
        {
            return Transform2DTrianglesInto3D(new Triangle[]
            {
                new Triangle(new Vector3D(1,1,0), new Vector3D(2,4,0), new Vector3D(6,2, 0)),
                //new Triangle(new Vector3D(4.5f,0.5f,0), new Vector3D(4.5f,0.5f,0), new Vector3D(4.5f,0.5f, 0)),
                new Triangle(new Vector3D(5.25f,1.25f,0), new Vector3D(6.25f,0.25f,0), new Vector3D(6.25f,1.25f, 0)),
                new Triangle(new Vector3D(7.5f, 0.5f ,0), new Vector3D(7.5f,1.5f,0), new Vector3D(6.5f,1.5f, 0)),
                new Triangle(new Vector3D(7.75f,2.5f,0), new Vector3D(11.75f,2.5f,0), new Vector3D(9.75f,0.75f, 0)),
                new Triangle(new Vector3D(7.75f,2.5f,0), new Vector3D(11.75f,2.5f,0), new Vector3D(9.5f,5.25f, 0)),
                new Triangle(new Vector3D(13.5f,1.5f,0), new Vector3D(15,0,0), new Vector3D(14.5f,2.5f, 0)),
                new Triangle(new Vector3D(13.5f,1.5f,0), new Vector3D(14.5f,4.5f,0), new Vector3D(14.5f,2.5f, 0)),
                new Triangle(new Vector3D(1,6,0), new Vector3D(5,6,0), new Vector3D(7,4, 0)),
                new Triangle(new Vector3D(8,7,0), new Vector3D(5,6,0), new Vector3D(7,4, 0)),
                new Triangle(new Vector3D(8,7,0), new Vector3D(9.5f,5.5f,0), new Vector3D(7,4, 0)),
                new Triangle(new Vector3D(11.5f,4.5f,0), new Vector3D(11.5f,6.5f,0), new Vector3D(12.5f,5.5f, 0)),
                new Triangle(new Vector3D(15.5f,7.5f,0), new Vector3D(13.5f,7.5f,0), new Vector3D(15.5f,5.5f, 0)),
                new Triangle(new Vector3D(13.5f,5.5f,0), new Vector3D(13.5f,7.5f,0), new Vector3D(15.5f,5.5f, 0)),
                new Triangle(new Vector3D(9.5f,7.5f,0), new Vector3D(10.5f,7.5f,0), new Vector3D(9.5f,9.5f, 0)),
            });
        }

        //Liste von vielen kleinen Dreiecken
        public static Triangle[] GetSmallTriangles(int width, int height)
        {
            List<Triangle> list = new List<Triangle>();

            int border = 1; //So viele Pixel lasse ich zum Rand Platz
            int w = width - border * 2;
            int h = height - border * 2;
            float b = 0.5f + border;

            int y = 0;

            foreach (var triangle in GetTriangleTypes())
            {
                foreach (var t in GetTranslations())
                {
                    var p0 = triangle.V[0].Position;
                    var p1 = triangle.V[1].Position;
                    var p2 = triangle.V[2].Position;

                    list.Add(new Triangle(
                        new Vertex(p0.X * w + t.X + b, p0.Y * h + t.Y + b + y, 0, p0.X, p0.Y),
                        new Vertex(p1.X * w + t.X + b, p1.Y * h + t.Y + b + y, 0, p1.X, p1.Y),
                        new Vertex(p2.X * w + t.X + b, p2.Y * h + t.Y + b + y, 0, p2.X, p2.Y)
                        ));

                    y += height;
                }
            }

            return Transform2DTrianglesInto3D(list.ToArray());
        }



        //All mögliche Verschiebungspositionen von der Pixelmitte aus
        private static Vector2D[] GetTranslations()
        {
            return new Vector2D[]
            {
                new Vector2D(0,0),
                new Vector2D(-0.25f, -0.25f),
                new Vector2D(0, -0.25f),
                new Vector2D(+0.25f, -0.25f),
                new Vector2D(-0.25f, 0),
                new Vector2D(+0.25f, 0),
                new Vector2D(-0.25f, +0.25f),
                new Vector2D(0, +0.25f),
                new Vector2D(+0.25f, +0.25f),
            };
        }

        //Erzeugt alle möglichen Dreiecke im 0..1-Bereich, welche ich testen will
        private static Triangle[] GetTriangleTypes()
        {
            return new Triangle[]
            {
                new Triangle(new Vector3D(0,0,0), new Vector3D(0,1,0), new Vector3D(1,1,0)),    //  |\
                new Triangle(new Vector3D(0,1,0), new Vector3D(0.5f,0,0), new Vector3D(1,1,0)), //  /\
                new Triangle(new Vector3D(0,1,0), new Vector3D(1,0,0), new Vector3D(1,1,0)),    //  /|
                new Triangle(new Vector3D(0,1,0), new Vector3D(0,0,0), new Vector3D(1,0,0)),    //  |/
                new Triangle(new Vector3D(0,0,0), new Vector3D(0.5f,1,0), new Vector3D(1,0,0)), //  \/
                new Triangle(new Vector3D(0,0,0), new Vector3D(1,1,0), new Vector3D(1,0,0)),    //  \|
            };
        }

        private static Triangle[] Transform2DTrianglesInto3D(Triangle[] triangles)
        {
            return triangles.Select(x => new Triangle(
                new Vertex(x.V[0].Position.X, x.V[0].Position.Y, -1, x.V[0].Position.X, x.V[0].Position.Y) { Normal = new Vector3D(0, 0, 1) },
                new Vertex(x.V[1].Position.X, x.V[1].Position.Y, -1, x.V[1].Position.X, x.V[1].Position.Y) { Normal = new Vector3D(0, 0, 1) },
                new Vertex(x.V[2].Position.X, x.V[2].Position.Y, -1, x.V[2].Position.X, x.V[2].Position.Y) { Normal = new Vector3D(0, 0, 1) }
                )).ToArray();
        }
    }
}
