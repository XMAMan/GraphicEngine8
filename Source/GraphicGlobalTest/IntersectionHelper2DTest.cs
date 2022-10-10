using GraphicGlobal;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphicGlobalTest
{
    [TestClass]
    public class IntersectionHelper2DTest
    {
        [TestMethod]
        public void IntersectionPointRayCircle_CalledForRayInCircle_PointReturned()
        {
            Vector2D point = IntersectionHelper2D.IntersectionPointRayCircle(new Vector2D(0.5f, 0), new Vector2D(0, 1), new Vector2D(0, 0), 1);
            CheckPoint(new Vector2D(0.5f, (float)Math.Sqrt(1 - 0.5 * 0.5)), point);
        }

        [TestMethod]
        public void IntersectionPointRayCircle_CalledForRayOutsideCircle_NullReturned()
        {
            Vector2D point = IntersectionHelper2D.IntersectionPointRayCircle(new Vector2D(1.5f, 0), new Vector2D(0, 1), new Vector2D(0, 0), 1);
            Assert.IsNull(point);
        }

        [TestMethod]
        public void IntersectionPointRayCircle_CalledForOutsideCircle_PointReturned()
        {
            Vector2D point = IntersectionHelper2D.IntersectionPointRayCircle(new Vector2D(0.5f, -10), new Vector2D(0, 1), new Vector2D(0, 0), 1);
            CheckPoint(new Vector2D(0.5f, -(float)Math.Sqrt(1 - 0.5 * 0.5)), point);
        }

        private void CheckPoint(Vector2D expected, Vector2D actual)
        {
            float maxError = 0.00001f;
            Assert.IsTrue((expected.X - actual.X) < maxError, "Expected=" + expected.X + "Actual=" + actual.X);
            Assert.IsTrue((expected.Y - actual.Y) < maxError, "Expected=" + expected.Y + "Actual=" + actual.Y);
        }
    }
}
