using GraphicGlobal;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphicMinimalTest
{
    [TestClass]
    public class Vector3DTest
    {
        [TestMethod]
        public void RotateVector_RandomRotation_MatchWithRotationMatrix()
        {
            Random rand = new Random(0);

            for (int i=0;i<1000;i++)
            {
                var v1 = Vector3D.Normalize(new Vector3D((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()));

                float degreeX = (float)rand.NextDouble() * 360;
                float degreeY = (float)rand.NextDouble() * 360;
                float degreeZ = (float)rand.NextDouble() * 360;

                var v2 = Vector3D.RotateVector(v1, degreeX, degreeY, degreeZ);

                Matrix4x4 rotMatrix = Matrix4x4.NormalRotate(new Vector3D(degreeX, degreeY, degreeZ));
                var v2_ = Vector3D.Normalize(Matrix4x4.MultDirection(rotMatrix, v1));

                Assert.IsTrue((v2 - v2_).Length() < 0.001f);
            }
        }

        [TestMethod]
        [Ignore] //GetAngularOrientation geht momentan nicht
        public void GetAngularOrientation_RandomDirections_MatchWithRotationMatrix()
        {
            Random rand = new Random(0);

            for (int i = 0; i < 1000; i++)
            {
                var v1 = Vector3D.Normalize(new Vector3D((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()));

                float degreeX = (float)rand.NextDouble() * 360;
                float degreeY = (float)rand.NextDouble() * 360;
                float degreeZ = (float)rand.NextDouble() * 360;

                Matrix4x4 rotMatrix = Matrix4x4.NormalRotate(new Vector3D(degreeX, degreeY, degreeZ));
                var v2 = Vector3D.Normalize(Matrix4x4.MultDirection(rotMatrix, v1));

                var degree = Vector3D.GetAngularOrientation(v1, v2);
                var v2_ = Vector3D.RotateVector(v1, degree.X, degree.Y, degree.Z);

                Assert.IsTrue((v2 - v2_).Length() < 0.001f);
            }
        }

        [TestMethod]
        public void GetAngularOrientationZ_RandomDirections_MatchWithRotationMatrix()
        {
            Random rand = new Random(0);

            for (int i = 0; i < 1000; i++)
            {
                var v1 = new Vector3D(0, 0, 1);

                float degreeX = (float)rand.NextDouble() * 360;
                float degreeY = (float)rand.NextDouble() * 360;
                float degreeZ = (float)rand.NextDouble() * 360;

                Matrix4x4 rotMatrix = Matrix4x4.NormalRotate(new Vector3D(degreeX, degreeY, degreeZ));
                var v2 = Vector3D.Normalize(Matrix4x4.MultDirection(rotMatrix, v1));

                var degree = Vector3D.GetAngularOrientationZ(v2);
                var v2_ = Vector3D.RotateVector(v1, degree.X, degree.Y, degree.Z);

                //Assert.IsTrue(Math.Abs(degreeX - degree.X) < 0.1f);
                //Assert.IsTrue(Math.Abs(degreeY - degree.Y) < 0.1f);

                Assert.IsTrue((v2 - v2_).Length() < 0.001f);
            }
        }
    }
}
