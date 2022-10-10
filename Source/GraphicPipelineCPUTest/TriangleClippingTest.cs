using GraphicGlobal;
using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper;
using GraphicPipelineCPU.ObjToWindowSpaceConversion;
using GraphicPipelineCPU.Rasterizer;
using GraphicPipelineCPU.Shader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Linq;

namespace GraphicPipelineCPUTest
{
    [TestClass]
    public class TriangleClippingTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        private int bufferWidth = 100;
        private int bufferHeight = 100;

        enum ProjectionMode { Ortho, Perspective }

        private TriangleTestData[] triangleTestData = new TriangleTestData[]
        {
            new TriangleTestData() //0 Kein Clipping
            {
                Input = new Triangle(new Vector3D(48, 50, -1), new Vector3D(52, 46, -1), new Vector3D(52, 54, -1)),
                Expected = new Vector3D[]{ new Vector3D(48, 50, 0.5f), new Vector3D(52,46, 0.5f), new Vector3D(52, 54, 0.5f) }
            },
            new TriangleTestData() //1 Riesen-Dreieck
            {
                Input = new Triangle(new Vector3D(48- 1000, 50, -1), new Vector3D(52 + 1000, 46 - 1000, -1), new Vector3D(52 + 1000, 54 + 1000, -1)),
                Expected = new Vector3D[]{ new Vector3D(0, 0, 0.5f), new Vector3D(100,0, 0.5f), new Vector3D(100, 100, 0.5f), new Vector3D(0, 100, 0.5f) }
            },
            new TriangleTestData() //2 Clippe an -X
            {
                Input = new Triangle(new Vector3D(-2, 50, -1), new Vector3D(2, 50 - 4, -1), new Vector3D(2, 50+ 4, -1)),
                Expected = new Vector3D[]{new Vector3D(0, 48, 0.5f), new Vector3D(2, 50-4, 0.5f), new Vector3D(2, 50 + 4, 0.5f), new Vector3D(0, 52, 0.5f) }
            },
            new TriangleTestData() //3 Clippe an +X
            {
                Input = new Triangle(new Vector3D(102, 50, -1), new Vector3D(98, 50 - 4, -1), new Vector3D(98, 50+ 4, -1)),
                Expected = new Vector3D[]{new Vector3D(100, 48, 0.5f), new Vector3D(98, 50-4, 0.5f), new Vector3D(98, 50 + 4, 0.5f), new Vector3D(100, 52, 0.5f) }
            },
            new TriangleTestData() //4 Clippe an -Y
            {
                Input = new Triangle(new Vector3D(50, -2, -1), new Vector3D(46, 2, -1), new Vector3D(54, 2, -1)),
                Expected = new Vector3D[]{ new Vector3D(46, 2, 0.5f), new Vector3D(54, 2, 0.5f), new Vector3D(52, 0, 0.5f), new Vector3D(48, 0, 0.5f), }
            },
            new TriangleTestData() //5 Clippe an +Y
            {
                Input = new Triangle(new Vector3D(50, 102, -1), new Vector3D(46, 98, -1), new Vector3D(54, 98, -1)),
                Expected = new Vector3D[]{ new Vector3D(46, 98, 0.5f), new Vector3D(54, 98, 0.5f), new Vector3D(52, 100, 0.5f), new Vector3D(48, 100, 0.5f), }
            },
            new TriangleTestData() //6 Clippe an -Z
            {
                Input = new Triangle(new Vector3D(50, 50, 1), new Vector3D(52, 50, -1), new Vector3D(48, 50, -1)),
                Expected = new Vector3D[]{ new Vector3D(52, 50, 0.5f), new Vector3D(48, 50, 0.5f), new Vector3D(49, 50, 0), new Vector3D(51, 50, 0) },
                ExpectedPerspective = new Vector3D[]{ new Vector3D(52, 50, 0.5f), new Vector3D(48, 50, 0.5f), new Vector3D(47, 50, 0), new Vector3D(53, 50, 0) }
            },
            new TriangleTestData() //7 Clippe an +Z
            {
                Input = new Triangle(new Vector3D(50, 50, -3), new Vector3D(52, 50, -1), new Vector3D(48, 50, -1)),
                Expected = new Vector3D[]{ new Vector3D(52, 50, 0.5f), new Vector3D(48, 50, 0.5f), new Vector3D(49, 50, 1), new Vector3D(51, 50, 1) },
                ExpectedPerspective = new Vector3D[]{ new Vector3D(52, 50, 0.5f), new Vector3D(48, 50, 0.5f), new Vector3D(49, 50, 1), new Vector3D(51, 50, 1) }
            },
            new TriangleTestData() //8 Clippe an -X und -Y
            {
                Input = new Triangle(new Vector3D(1, 1, -1), new Vector3D(1, -5, -1), new Vector3D(-5, 1, -1)),
                Expected = new Vector3D[]{new Vector3D(1, 1, 0.5f), new Vector3D(1, 0, 0.5f), new Vector3D(0, 0, 0.5f), new Vector3D(0, 1, 0.5f) }
            },
            new TriangleTestData() //9 Clippe an -X und -Y
            {
                Input = new Triangle(new Vector3D(99, 1, -1), new Vector3D(99, -5, -1), new Vector3D(104, 1, -1)),
                Expected = new Vector3D[]{new Vector3D(99, 1, 0.5f), new Vector3D(99, 0, 0.5f), new Vector3D(100, 0, 0.5f), new Vector3D(100, 1, 0.5f) }
            },
        };

        [TestMethod]
        public void DrawTriangles()
        {
            Bitmap buffer = new Bitmap(bufferWidth, bufferHeight);

            for (int i = 0; i < this.triangleTestData.Length; i++)
            {
                var dataRow = this.triangleTestData[i];
                var triangles = ClipTriangle(dataRow.Input, ProjectionMode.Perspective);
                if (triangles.Any() == false) continue;

                foreach (var t in triangles)
                {
                    TriangleRasterizer.DrawWindowSpaceTriangle(t.V[0].Position.XY, t.V[1].Position.XY, t.V[2].Position.XY, null, (pix) =>
                    {
                        buffer.SetPixel(pix.X, pix.Y, Color.Black);
                    });
                }
            }


            buffer.Save(WorkingDirectory + "TriangleClipping.bmp");
        }

        [TestMethod]
        public void TriangleClippingTestOrtho()
        {
            TriangleClippingTestForOneMode(ProjectionMode.Ortho);
        }

        [TestMethod]
        public void TriangleClippingTestPerspective()
        {
            TriangleClippingTestForOneMode(ProjectionMode.Perspective);
        }

        private void TriangleClippingTestForOneMode(ProjectionMode projectionMode)
        {
            for (int i = 0; i < this.triangleTestData.Length; i++)
            {
                var dataRow = this.triangleTestData[i];

                var expected = dataRow.Expected;
                if (projectionMode == ProjectionMode.Perspective && dataRow.ExpectedPerspective != null) expected = dataRow.ExpectedPerspective;

                if (projectionMode == ProjectionMode.Perspective)
                {
                    if (expected != null)
                    {
                        foreach (var p in expected)
                        {
                            if (p.Z == 0.5f) p.Z = 0.75f;
                            if (p.Z == 0.5f) p.Z = 0.75f;
                            if (p.Z == 0.5f) p.Z = 0.75f;
                        }                        
                    }

                }
                var actual = ClipTriangle(dataRow.Input, projectionMode);

                
                Assert.IsTrue(expected != null ? expected.IsEqual(actual) : actual == null, "Error on line " + i);
            }
        }

        private Triangle[] ClipTriangle(Triangle triangle, ProjectionMode projectionMode)
        {
            Matrix4x4 projectionMatrix = null;

            if (projectionMode == ProjectionMode.Ortho)
            {
                //Ortho
                //Bei Ortho gehen die XY-Koordinaten von 0..100; z genau in der Mitte zwischen zNear und zFar ergibt 0.5
                float zNear = 0, zFar = 2;
                projectionMatrix = Matrix4x4.ProjectionMatrixOrtho(0, this.bufferWidth, this.bufferHeight, 0, zNear, zFar);
            }
            else
            {
                //Bei Perspective gehen die XY-Koordinaten von -50 .. +50; z genau in der Mitte zwischen zNear und zFar ergibt 0.75
                triangle.V[0].Position -= new Vector3D(50, 50, 0);
                triangle.V[1].Position -= new Vector3D(50, 50, 0);
                triangle.V[2].Position -= new Vector3D(50, 50, 0);
                triangle.V[0].Position.Y *= -1; //Das Bild ist bei Perspective-Mode y-Mäßig geflippt. Woher kommt das?
                triangle.V[1].Position.Y *= -1; //Es muss an der Perspective-Matrix liegen
                triangle.V[2].Position.Y *= -1;

                //Perspective
                float z = 1; //In diesen z-Abstand sind alle Linien/Dreiecke definiert (Alle liegen bei z=-1)
                float foV = (float)(Math.Atan((this.bufferHeight / 2) / z) / Math.PI * 180) * 2;
                projectionMatrix = Matrix4x4.ProjectionMatrixPerspective(foV, 1, z - 0.5f, z + 0.5f);
            }


            var viewPort = new ViewPort(0, 0, this.bufferWidth, this.bufferHeight);

            var clippedTriangles = ObjectSpaceToWindowSpaceConverter.TransformTriangleFromObjectToWindowSpace(triangle,
                new ShaderDataForLines()
                {
                    WorldViewProj = projectionMatrix
                },
               VertexShader.VertexShaderForLines,
               GeometryShader.DoNothing,
               viewPort);

            return clippedTriangles.Select(x => new Triangle(x.W0.WindowPos, x.W1.WindowPos, x.W2.WindowPos)).ToArray();
        }

        class TriangleTestData
        {
            public Triangle Input;      //Ein Dreieck
            public Vector3D[] Expected;   //Durchs Clipping entstehen mehrere Dreiecke. Dessen Distincte Eckpunkte(Viereck oder Dreieck) ist dann hier
            public Vector3D[] ExpectedPerspective;
        }
    }

    static class TriangleExtension
    {
        public static bool IsEqual(this Vector3D[] points, Triangle[] triangles)
        {
            if (triangles == null && points == null) return true;

            foreach (var t in triangles)
            {
                foreach (var p in t.V.Select(x => x.Position))
                {
                    if (points.Any(x => TriangleExtension.IsEqual(x, p)) == false) return false;
                }
            }

            return true;
        }

        public static bool IsEqual(this Triangle[] t1, Triangle[] t2)
        {
            if (t1 == null && t2 == null) return true;
            if (t1.Length != t2.Length) return false;

            for (int i = 0; i < t1.Length; i++)
                if (t1[i].IsEqual(t2[i]) == false) return false;

            return true;
        }

        public static bool IsEqual(this Triangle t1, Triangle t2)
        {
            return IsEqual(t1.V[0].Position, t2.V[0].Position) &&
                   IsEqual(t1.V[1].Position, t2.V[1].Position) &&
                   IsEqual(t1.V[2].Position, t2.V[2].Position);
        }

        private static bool IsEqual(Vector3D p1, Vector3D p2)
        {
            return IsEqual(p1.X, p2.X) && IsEqual(p1.Y, p2.Y) && IsEqual(p1.Z, p2.Z);
        }

        private static bool IsEqual(float f1, float f2)
        {
            return Math.Abs(f1 - f2) < 0.001f;
        }
    }
}
