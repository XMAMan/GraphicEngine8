using System;
using System.Collections.Generic;
using GraphicGlobal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TriangleObjectGeneration;

namespace TriangleObjectGenerationTest
{
    [TestClass]
    public class TriangleObjectGeneratorTest
    {
        private readonly string DataDirectory = UnitTestHelper.FilePaths.DataDirectory;
        private readonly float maxError = 0.001f;

        [TestMethod]
        public void LoadWaveFrontFile_SplitFile_NormalsHasLengthOne()
        {
            var triangleObjects = TriangleObjectGenerator.LoadWaveFrontFile(DataDirectory + "02_NoWindowRoom.obj", true, false);
            TestTriangleList(triangleObjects);
        }

        [TestMethod]
        public void CreateCornellBox_Called_NormalsHasLengthOne()
        {
            var triangleObjects = TriangleObjectGenerator.CreateCornellBox();
            TestTriangleList(triangleObjects);
        }

        private void TestTriangleList(List<TriangleObject> triangleObjects)
        {
            Assert.IsTrue(triangleObjects.Count > 1);

            foreach (var obj in triangleObjects)
            {
                TestTriangleObject(obj);
            }
        }

        private void TestTriangleObject(TriangleObject obj)
        {
            Assert.IsFalse(string.IsNullOrEmpty(obj.Name));
            Assert.IsTrue(obj.Radius > 0);
            Assert.IsNotNull(obj.CenterPoint);
            Assert.IsTrue(obj.Triangles.Length > 0);
            foreach(var triangle in obj.Triangles)
            {
                TestTriangle(triangle);
            }
        }

        private void TestTriangle(Triangle triangle)
        {            
            Assert.IsTrue(Math.Abs(triangle.Normal.Length() - 1) < maxError);
            Assert.IsTrue(Math.Abs(triangle.Tangent.Length() - 1) < maxError);
            Assert.IsTrue(Math.Abs(triangle.Normal * triangle.Tangent) < maxError, "Normal must be perpendicular to the tangent");
            var box = triangle.GetBoundingBox();
            Assert.IsTrue((box.Max - box.Min).Length() > maxError);

            Assert.IsTrue(triangle.V.Length == 3);
            for (int i=0;i<3;i++)
            {
                TestTriangleVertex(triangle.V[i]);
            }
        }

        private void TestTriangleVertex(Vertex vertex)
        {
            Assert.IsNotNull(vertex.Normal);
            //Assert.IsNull(vertex.Tangent, "Ein Dreieck hat nur eine Tangente"); //Der Pixelshader braucht die Tangente im Vertex
            Assert.IsTrue(Math.Abs(vertex.Normal.Length() - 1) < maxError);
        }
    }
}
