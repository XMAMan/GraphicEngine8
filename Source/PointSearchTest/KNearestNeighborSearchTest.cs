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
    public class KNearestNeighborSearchTest
    {
        [TestMethod]
        public void SearchKNearestNeighbors_Search3Nearest_3NearestReturned()
        {
            int pointCount = 100;
            int pointSearchCount = 3;

            Random rand = new Random(0);

            List<Point3D> points = new List<Point3D>();
            Point3D searchCenterPoint = new Point3D((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());           

            for (int i = 0; i < pointCount; i++) points.Add(new Point3D((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()));

            var sut = new KNearestNeighborSearch(points);
            IPoint[] result = sut.SearchKNearestNeighbors(searchCenterPoint, pointSearchCount);

            Assert.AreEqual(pointSearchCount, result.Length);
            var orderedList = points.OrderBy(x => (searchCenterPoint - x).SquareLength()).ToList();
            for (int i=0;i<pointSearchCount;i++)
            {
                Assert.AreEqual(orderedList[i], result[i]);
            }
        }
    }
}
