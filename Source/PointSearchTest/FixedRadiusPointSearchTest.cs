using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PointSearch;
using RayTracerGlobal;

namespace PointSearchTest
{
    [TestClass]
    public class FixedRadiusPointSearchTest
    {
        private int maxError = 5;

        [TestMethod]
        public void FixedRadiusSearch_SearchUnitSphere_SphereVolumeIsReturned()
        {
            int pointCount = 500;
            float searchRadius = 0.5f;

            Random rand = new Random(0);

            List<Point3D> points = new List<Point3D>();
            Point3D searchCenterPoint = new Point3D(0.5f, 0.5f, 0.5f);

            for (int i = 0; i < pointCount; i++) points.Add(new Point3D((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()));

            var sut = new FixedRadiusPointSearch(points);
            List<IPoint> result = sut.FixedRadiusSearch(searchCenterPoint, searchRadius);

            int expectedCount1 = points.Where(x => (x - searchCenterPoint).Length() < searchRadius).Count();

            float sphereVolume = (float)(4.0 / 3.0 * Math.PI * searchRadius * searchRadius * searchRadius);
            int expectedCount = (int)(pointCount * sphereVolume);

            Assert.AreEqual(expectedCount1, result.Count);
            Assert.IsTrue(Math.Abs(result.Count - expectedCount) < maxError);
        }
    }
}
