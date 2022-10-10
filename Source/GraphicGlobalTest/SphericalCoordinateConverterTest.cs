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
    public class SphericalCoordinateConverterTest
    {
        [TestMethod]
        public void ToWorldDirection_WithRandomPhiTheta_MatchWithToSphereCoordinate()
        {
            Random rand = new Random(0);

            for (int i = 0; i < 1000; i++)
            {
                var v1 = Vector3D.Normalize(new Vector3D((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()));

                double phi = rand.NextDouble() * 2 * Math.PI;
                double theta = rand.NextDouble() * Math.PI;


                SphericalCoordinateConverter con = new SphericalCoordinateConverter(new Frame(v1));
                var v2 = con.ToWorldDirection(new SphericalCoordinate(phi, theta));

                var sp = con.ToSphereCoordinate(v2);

                Assert.IsTrue(Math.Abs(phi - sp.Phi) < 0.001f);
                Assert.IsTrue(Math.Abs(theta - sp.Theta) < 0.001f);
            }
        }

        [TestMethod]
        public void ToWorldDirection_WithRandomDirection_MatchWithToSphereCoordinate()
        {
            Random rand = new Random(0);

            for (int i = 0; i < 1000; i++)
            {
                var v1 = Vector3D.Normalize(new Vector3D((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()));
                var v2 = Vector3D.Normalize(new Vector3D((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()));


                SphericalCoordinateConverter con = new SphericalCoordinateConverter(new Frame(v1));
                var sp = con.ToSphereCoordinate(v2);
                var v2_ = con.ToWorldDirection(sp);

                Assert.IsTrue((v2 - v2_).Length() < 0.001f);
            }
        }
    }
}
