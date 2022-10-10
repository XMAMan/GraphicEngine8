using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IntersectionTestsTest
{
    [TestClass]
    public class BoundingIntervallHierarchyTest
    {
        private void Linear_BIH_Compare(List<IIntersecableObject> list, Ray ray, IIntersecableObject excludedObject = null)
        {
            //KDSahTree sut = new KDSahTree(list, (s, f) => { });
            BoundingIntervallHierarchy sut = new BoundingIntervallHierarchy(list, (s, f) => { });
            LinearSearchIntersector lin = new LinearSearchIntersector(list);

            var linPoint = lin.GetIntersectionPoint(ray, excludedObject, float.MaxValue, 0);
            var sutPoint = sut.GetIntersectionPoint(ray, excludedObject, float.MaxValue, 0);

            if (linPoint == null && sutPoint == null) return;

            //var kdItem = string.Join(", ", sut.VisitEachLeafeItem().Where(x => x.Obj == linPoint.IntersectedObject).Select(x => x.Location));

            Assert.IsNotNull(linPoint);
            Assert.IsNotNull(sutPoint, "BIH zeigt Schnittpunkt nicht.");
            Assert.AreEqual(linPoint.DistanceToRayStart, sutPoint.DistanceToRayStart);
        }

        [TestMethod]
        public void GetIntersectionPoint_CheckPixelColor_BidirectionalPathTracing_Cornellbox_GlassSphereLightFlackOverRectangle3()
        {
            Linear_BIH_Compare(IntersectableObjectsData.cornellBox, new Ray(new Vector3D(0.234931305f, 0.330000043f, -0.303239435f), new Vector3D(-0.573871791f, 0f, -0.818945229f)), IntersectableObjectsData.cornellBox[22]);
        }
    }
}
