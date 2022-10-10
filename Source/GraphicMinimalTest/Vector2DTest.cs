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
    public class Vector2DTest
    {
        [TestMethod]
        public void Angle_TwoDiffernt90_SameAngle()
        {
            float angle1 = Vector2D.Angle(new Vector2D(1, 0), new Vector2D(0, 1));
            float angle2 = Vector2D.Angle(new Vector2D(1, 0), new Vector2D(0, -1));

            Assert.AreEqual(90, angle1);
            Assert.AreEqual(90, angle2);
        }

        [TestMethod]
        public void Angle360_TwoDiffernt90_DifferentAngle()
        {
            float angle1 = Vector2D.Angle360(new Vector2D(1, 0), new Vector2D(0, 1));
            float angle2 = Vector2D.Angle360(new Vector2D(1, 0), new Vector2D(0, -1));

            Assert.AreEqual(90, angle1);
            Assert.AreEqual(270, angle2);
        }

        [TestMethod]
        public void GetV2FromAngle360_RandomDirection_WorksTogetherWithAngle360()
        {
            Random rand = new Random(0);

            for (int i=0;i<1000;i++)
            {
                var v1 = new Vector2D((float)rand.NextDouble(), (float)rand.NextDouble()).Normalize();
                var v2 = new Vector2D((float)rand.NextDouble(), (float)rand.NextDouble()).Normalize();

                float angle360 = Vector2D.Angle360(v1, v2);
                var v2_ = Vector2D.GetV2FromAngle360(v1, angle360);

                Assert.IsTrue((v2 - v2_).Length() < 0.0001f);
            }
        }

        [TestMethod]
        public void GetSpinAngle_AroundTheCircle_0To360()
        {
            string str1 = "", str2 = "";

            var v1 = new Vector2D(1, 0) * 10;
            for (int angle = 0; angle < 360; angle++)
            {
                var v2 = Vector2D.GetV2FromAngle360(v1, angle);
                float angle360 = Vector2D.Angle360(v1, v2);
                float spinAngle = Vector2D.GetSpinAngle(v1, v2);

                var v2_ = Vector2D.RotatePointAroundPivotPoint(new Vector2D(0, 0), v1, spinAngle);
                Assert.IsTrue((v2_ - v2).Length() < 0.0001f);

                Assert.IsTrue(Math.Abs(angle360 - angle) < 0.0001f);
    
                str1 += angle360 + "\t";
                str2 += spinAngle + "\t";
            }
        }

        [TestMethod]
        public void RotatePointAroundPivotPoint_RandomPoint_WorksTogetherWithGetSpinAngle()
        {
            Random rand = new Random(0);

            for (int i = 0; i < 1000; i++)
            {
                var pivotpoint = new Vector2D((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f) * 10;
                var p1 = new Vector2D((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f) * 10;
                var p2 = new Vector2D((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f) * 10;

                float spingAngle = Vector2D.GetSpinAngle(p1 - pivotpoint, p2 - pivotpoint);
                float angle360 = Vector2D.Angle360(p1 - pivotpoint, p2 - pivotpoint);
                var p2a = Vector2D.RotatePointAroundPivotPoint(pivotpoint, p1, spingAngle);
                var p2b = Vector2D.RotatePointAroundPivotPoint(pivotpoint, p1, angle360);
                
                Assert.IsTrue(Vector2D.Angle(p2 - pivotpoint, p2a - pivotpoint) < 0.1f);
                Assert.IsTrue(Vector2D.Angle(p2 - pivotpoint, p2b - pivotpoint) < 0.1f);
                Assert.IsTrue((p2a - p2b).Length() < 0.001f);
            }
        }

        [TestMethod]
        public void GetAngle360ForSorting_ComparisonToAngle360_NotEqual()
        {
            //float t1 = Vector2D.GetAngle360ForSorting(new Vector2D(1, 0), new Vector2D(1, 0));                  //0   Grad
            //float t2 = Vector2D.GetAngle360ForSorting(new Vector2D(1, 0), new Vector2D(1, 1).Normalize());      //45  Grad
            //float t3 = Vector2D.GetAngle360ForSorting(new Vector2D(1, 0), new Vector2D(0, 1));                  //90  Grad
            //float t4 = Vector2D.GetAngle360ForSorting(new Vector2D(1, 0), new Vector2D(-1, 1).Normalize());     //135 Grad
            //float t5 = Vector2D.GetAngle360ForSorting(new Vector2D(1, 0), new Vector2D(-1, 0));                 //180 Grad
            //float t6 = Vector2D.GetAngle360ForSorting(new Vector2D(1, 0), new Vector2D(-1, -1).Normalize());    //225 Grad
            //float t7 = Vector2D.GetAngle360ForSorting(new Vector2D(1, 0), new Vector2D(0, -1));                 //270 Grad
            //float t8 = Vector2D.GetAngle360ForSorting(new Vector2D(1, 0), new Vector2D(1, -1).Normalize());     //315 Grad

            string str1 = "", str2 = "";
            var v1 = new Vector2D(1, 0);
            for (int angle=0;angle<360;angle++)
            {
                var v2 = Vector2D.GetV2FromAngle360(v1, angle);
                float angle360 = Vector2D.Angle360(v1, v2);
                float theta = Vector2D.GetAngle360ForSorting(new Vector2D(0,0), v2);
                str1 += angle360 + "\t";
                str2 += theta + "\t";
            }            
        }
    }
}
