using Microsoft.VisualStudio.TestTools.UnitTesting;
using ObjectDivider;
using TriangleObjectGeneration;

namespace ObjectDividerTest
{
    [TestClass]
    public class QuadCreatorTest
    {
        [TestMethod]
        public void GetQuadList_CalledWithTwoTriangles_OneQuadIsReturned()
        {
            float size = 0.5f;
            var quads = QuadCreator.GetQuadList(TriangleObjectGenerator.CreateSquareXY(size, size, 1).Triangles);
            Assert.AreEqual(1, quads.Count);
            Assert.AreEqual(1, quads[0].SurfaceArea);
        }
    }
}
