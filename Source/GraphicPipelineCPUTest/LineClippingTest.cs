using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper;
using GraphicPipelineCPU.ObjToWindowSpaceConversion;
using GraphicPipelineCPU.Rasterizer;
using GraphicPipelineCPU.Shader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;

namespace GraphicPipelineCPUTest
{
    [TestClass]
    public class LineClippingTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        private int bufferWidth = 100;
        private int bufferHeight = 100;

        enum ProjectionMode { Ortho, Perspective}

        private LineTestData[] lineTestData = new LineTestData[]
            {
                new LineTestData() //0
                {
                    Input = new Line() { P1 = new Vector3D(0, 0, -1), P2 = new Vector3D(100, 100, -1) }, //Kein Clipping
                    Expected = new Line() { P1 = new Vector3D(0, 0, 0.5f), P2 = new Vector3D(100, 100, 0.5f) }
                },
                new LineTestData() //1
                {
                    Input = new Line() { P1 = new Vector3D(-50, 50, -1), P2 = new Vector3D(50, 50, -1) }, //Clippe an -X
                    Expected = new Line() { P1 = new Vector3D(0, 50, 0.5f), P2 = new Vector3D(50, 50, 0.5f) }
                },
                new LineTestData() //2
                {
                    Input = new Line() { P1 = new Vector3D(150, 50, -1), P2 = new Vector3D(50, 50, -1) }, //Clippe an +X
                    Expected = new Line() { P1 = new Vector3D(100, 50, 0.5f), P2 = new Vector3D(50, 50, 0.5f) }
                },
                new LineTestData() //3
                {
                    Input = new Line() { P1 = new Vector3D(50, -50, -1), P2 = new Vector3D(50, 50, -1) }, //Clippe an -Y
                    Expected = new Line() { P1 = new Vector3D(50, 0, 0.5f), P2 = new Vector3D(50, 50, 0.5f) }
                },
                new LineTestData() //4
                {
                    Input = new Line() { P1 = new Vector3D(50, 150, -1), P2 = new Vector3D(50, 50, -1) }, //Clippe an +Y
                    Expected = new Line() { P1 = new Vector3D(50, 100, 0.5f), P2 = new Vector3D(50, 50, 0.5f) }
                },
                new LineTestData() //5
                {
                    Input = new Line() { P1 = new Vector3D(50, 50, -1 + 50), P2 = new Vector3D(50, 50, -1) }, //Clippe an -Z
                    Expected = new Line() { P1 = new Vector3D(50, 50, 0), P2 = new Vector3D(50, 50, 0.5f) }
                },
                new LineTestData() //6
                {
                    Input = new Line() { P1 = new Vector3D(50, 50, -1 - 50), P2 = new Vector3D(50, 50, -1) }, //Clippe an +Z
                    Expected = new Line() { P1 = new Vector3D(50, 50, 1), P2 = new Vector3D(50, 50, 0.5f) }
                },
                new LineTestData() //7
                {
                    Input = new Line() { P1 = new Vector3D(0 - 50, 10 + 50, -1), P2 = new Vector3D(10 + 50, 0 - 50, -1) }, //Clippe an -X und +Y
                    Expected = new Line() { P2 = new Vector3D(0, 10, 0.5f), P1 = new Vector3D(10, 0, 0.5f) }
                },
                new LineTestData() //8
                {
                    Input = new Line() { P1 = new Vector3D(100 + 50, 10 + 50, -1), P2 = new Vector3D(90 - 50, 0 - 50, -1) }, //Clippe an +X und +Y
                    Expected = new Line() { P2 = new Vector3D(100, 10, 0.5f), P1 = new Vector3D(90, 0, 0.5f) }
                },
                new LineTestData() //9
                {
                    Input = new Line() { P1 = new Vector3D(0 - 50, 90 - 50, -1), P2 = new Vector3D(10 + 50, 100 + 50, -1) }, //Clippe an -X und -Y
                    Expected = new Line() { P2 = new Vector3D(0, 90, 0.5f), P1 = new Vector3D(10, 100, 0.5f) }
                },
                new LineTestData() //10
                {
                    Input = new Line() { P1 = new Vector3D(90 - 50, 100 + 50, -1), P2 = new Vector3D(100 + 50, 90 - 50, -1) }, //Clippe an +X und -Y
                    Expected = new Line() { P1 = new Vector3D(90, 100, 0.5f), P2 = new Vector3D(100, 90, 0.5f) }
                },
                new LineTestData() //11
                {
                    Input = new Line() { P1 = new Vector3D(-50, 50, -1), P2 = new Vector3D(150, 50, -1) }, //Clippe an -X +X
                    Expected = new Line() { P1 = new Vector3D(0, 50, 0.5f), P2 = new Vector3D(100, 50, 0.5f) }
                },
                new LineTestData() //12
                {
                    Input = new Line() { P1 = new Vector3D(-50, -50, -1), P2 = new Vector3D(150, -50, -1) }, //Horizontallinie liegt Y-Mäßig zu weit oben
                    Expected = null
                },
                new LineTestData() //13
                {
                    Input = new Line() { P1 = new Vector3D(-50, 50, +1), P2 = new Vector3D(150, 50, +1) }, //Horizontallinie liegt hinter der Kamera
                    Expected = null
                },
            };

        class LineTestData
        {
            public Line Input;
            public Line Expected;
        }

        class Line
        {
            public Vector3D P1;
            public Vector3D P2;

            public bool IsEqual(Line line)
            {
                return IsEqual(P1.X, line.P1.X) && IsEqual(P1.Y, line.P1.Y) && IsEqual(P1.Z, line.P1.Z) &&
                    IsEqual(P2.X, line.P2.X) && IsEqual(P2.Y, line.P2.Y) && IsEqual(P2.Z, line.P2.Z);
            }

            private bool IsEqual(float f1, float f2)
            {
                return Math.Abs(f1 - f2) < 0.001f;
            }
        }

        [TestMethod]
        public void DrawLines()
        {
            Bitmap buffer = new Bitmap(bufferWidth + 1, bufferHeight + 1);

            for (int i = 0; i < this.lineTestData.Length; i++)
            {
                var dataRow = this.lineTestData[i];
                var line = ClipLine(dataRow.Input, ProjectionMode.Perspective);
                if (line == null) continue;

                LineRasterizer.DrawLine(new Point((int)line.P1.X, (int)line.P1.Y), new Point((int)line.P2.X, (int)line.P2.Y), (pix, f) =>
                {
                    buffer.SetPixel(pix.X, pix.Y, Color.Black);
                });
            }
                

            buffer.Save(WorkingDirectory + "LineClipping.bmp");
        }

        [TestMethod]
        public void LineClippingTestOrtho()
        {
            LineClippingTestForOneMode(ProjectionMode.Ortho);
        }

        [TestMethod]
        public void LineClippingTestPerspective()
        {
            LineClippingTestForOneMode(ProjectionMode.Perspective);
        }

        private void LineClippingTestForOneMode(ProjectionMode projectionMode)
        {
            for (int i = 0; i < this.lineTestData.Length; i++)
            {
                var dataRow = this.lineTestData[i];
                if (projectionMode == ProjectionMode.Perspective)
                {
                    if (dataRow.Expected != null)
                    {
                        if (dataRow.Expected.P1.Z == 0.5f) dataRow.Expected.P1.Z = 0.75f;
                        if (dataRow.Expected.P2.Z == 0.5f) dataRow.Expected.P2.Z = 0.75f;
                    }
                    
                }
                var actual = ClipLine(dataRow.Input, projectionMode);

                Assert.IsTrue(dataRow.Expected != null ? dataRow.Expected.IsEqual(actual) : actual == null, "Error on line " + i);
            }
        }

        private Line ClipLine(Line line, ProjectionMode projectionMode)
        {
            Matrix4x4 projectionMatrix = null;

            if (projectionMode == ProjectionMode.Ortho)
            {
                //Ortho
                //Bei Ortho gehen die XY-Koordinaten von 0..100; z genau in der Mitte zwischen zNear und zFar ergibt 0.5
                float zNear = 0, zFar = 2;
                projectionMatrix = Matrix4x4.ProjectionMatrixOrtho(0, this.bufferWidth, this.bufferHeight, 0, zNear, zFar);
            }else
            {
                //Bei Perspective gehen die XY-Koordinaten von -50 .. +50; z genau in der Mitte zwischen zNear und zFar ergibt 0.75
                line.P1 -= new Vector3D(50, 50, 0);
                line.P2 -= new Vector3D(50, 50, 0);
                line.P1.Y *= -1; //Das Bild ist bei Perspective-Mode y-Mäßig geflippt. Woher kommt das?
                line.P2.Y *= -1; //Es muss an der Perspective-Matrix liegen

                //Perspective
                float z = 1; //In diesen z-Abstand sind alle Linien/Dreiecke definiert (Alle liegen bei z=-1)
                float foV = (float)(Math.Atan((this.bufferHeight / 2) / z) / Math.PI * 180) * 2;
                projectionMatrix = Matrix4x4.ProjectionMatrixPerspective(foV, 1, z - 0.5f, z + 0.5f);
            }
            

            var viewPort = new ViewPort(0, 0, this.bufferWidth, this.bufferHeight);

            var clippedLine = ObjectSpaceToWindowSpaceConverter.TransformLineFromObjectToWindowSpace(line.P1, line.P2,
                new ShaderDataForLines()
                {
                    WorldViewProj = projectionMatrix
                },
               VertexShader.VertexShaderForLines,
               viewPort);

            if (clippedLine == null) return null; //Linie liegt komplett außerhalb vom Sichtbereich

            return new Line() { P1 = clippedLine.P1.WindowPos, P2 = clippedLine.P2.WindowPos };
        }
    }
}
