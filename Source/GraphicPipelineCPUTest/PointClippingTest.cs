using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper;
using GraphicPipelineCPU.ObjToWindowSpaceConversion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GraphicPipelineCPUTest
{
    [TestClass]
    public class PointClippingTest
    {
        private int bufferWidth = 100;
        private int bufferHeight = 100;

        enum ProjectionMode { Ortho, Perspective }

        private PointTestData[] pointTestData = new PointTestData[]
        {
            new PointTestData(){ Input = new Point(50, 50, -1), Expected = new Point(50,50, 0.5f) },        //0 Kein Clipping Mitte
            new PointTestData(){ Input = new Point(0, 0, -1), Expected = new Point(0,0, 0.5f) },            //1 Kein Clipping Links Oben
            new PointTestData(){ Input = new Point(100, 0, -1), Expected = new Point(100,0, 0.5f) },        //2 Kein Clipping Rechts Oben
            new PointTestData(){ Input = new Point(0, 100, -1), Expected = new Point(0,100, 0.5f) },        //3 Kein Clipping Links unten
            new PointTestData(){ Input = new Point(100, 100, -1), Expected = new Point(100,100, 0.5f) },    //4 Kein Clipping Rechts unten
            new PointTestData(){ Input = new Point(-1, 50, -1), Expected = null },                          //5 Clippe an -X
            new PointTestData(){ Input = new Point(101, 50, -1), Expected = null },                         //6 Clippe an +X
            new PointTestData(){ Input = new Point(50, -1, -1), Expected = null },                          //7 Clippe an -Y
            new PointTestData(){ Input = new Point(50, 101, -1), Expected = null },                         //8 Clippe an +Y
            new PointTestData(){ Input = new Point(50, 50, +1), Expected = null },                          //9 Clippe an -Z
            new PointTestData(){ Input = new Point(50, 50, -3), Expected = null },                          //10 Clippe an +Z
        };

        [TestMethod]
        public void PointClippingTestOrtho()
        {
            PointClippingTestForOneMode(ProjectionMode.Ortho);
        }

        [TestMethod]
        public void PointClippingTestPerspective()
        {
            PointClippingTestForOneMode(ProjectionMode.Perspective);
        }

        private void PointClippingTestForOneMode(ProjectionMode projectionMode)
        {
            for (int i = 0; i < this.pointTestData.Length; i++)
            {
                var dataRow = this.pointTestData[i];
                if (projectionMode == ProjectionMode.Perspective)
                {
                    if (dataRow.Expected != null)
                    {
                        if (dataRow.Expected.Z == 0.5f) dataRow.Expected.Z = 0.75f;
                    }

                }
                var actual = ClipPoint(dataRow.Input, projectionMode);

                Assert.IsTrue(dataRow.Expected != null ? dataRow.Expected.IsEqual(actual) : actual == null, "Error on line " + i);
            }
        }

        private Point ClipPoint(Point point, ProjectionMode projectionMode)
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
                point = new Point(point.X - 50, point.Y - 50, point.Z);

                point.Y *= -1; //Das Bild ist bei Perspective-Mode y-Mäßig geflippt. Woher kommt das? Es muss an der Perspective-Matrix liegen

                //Perspective
                float z = 1; //In diesen z-Abstand sind alle Linien/Dreiecke definiert (Alle liegen bei z=-1)
                float foV = (float)(Math.Atan((this.bufferHeight / 2) / z) / Math.PI * 180) * 2;
                projectionMatrix = Matrix4x4.ProjectionMatrixPerspective(foV, 1, z - 0.5f, z + 0.5f);
            }


            var viewPort = new ViewPort(0, 0, this.bufferWidth, this.bufferHeight);

            var clippedPoint = ObjectSpaceToWindowSpaceConverter.TransformObjectSpacePositionToWindowCoordinates(
                new Vector3D(point.X, point.Y, point.Z), Matrix4x4.Ident(), projectionMatrix, viewPort, out bool pointIsInScreen);
                

            if (pointIsInScreen == false) return null; //Punkt liegt komplett außerhalb vom Sichtbereich

            return new Point(clippedPoint.X, clippedPoint.Y, clippedPoint.Z);
        }

        class PointTestData
        {
            public Point Input;
            public Point Expected;
        }

        class Point
        {
            public float X;
            public float Y;
            public float Z;

            public Point(float x, float y, float z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
            }

            public bool IsEqual(Point point)
            {
                return IsEqual(this.X, point.X) && IsEqual(this.Y, point.Y) && IsEqual(this.Z, point.Z);
            }

            private bool IsEqual(float f1, float f2)
            {
                return Math.Abs(f1 - f2) < 0.001f;
            }
        }
    }
}
