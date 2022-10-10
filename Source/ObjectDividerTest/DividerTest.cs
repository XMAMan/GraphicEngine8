using System.Collections.Generic;
using System.Linq;
using GraphicGlobal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ObjectDivider;
using TriangleObjectGeneration;

namespace ObjectDividerTest
{
    [TestClass]
    public class DividerTest
    {
        [TestMethod]
        public void Subdivide_CalledForAQuad_4QuadsReturned()
        {
            //Quad quad = new Quad(new Vertex(0, 0, 0), new Vertex(1, 0, 0), new Vertex(1, 1, 0), new Vertex(0, 1, 0));
            Quad quad = QuadCreator.GetQuadList(TriangleObjectGenerator.CreateSquareXY(0.5f, 0.5f, 1).Triangles)[0] as Quad;
            var result = Divider.Subdivide(new List<IDivideable>() { quad }, (source, obj) => 
            {
                return obj.SurfaceArea < 1;
            });

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(quad.SurfaceArea, result.Sum(x => x.SurfaceArea));
        }
    }
}
