using Microsoft.VisualStudio.TestTools.UnitTesting;
using GraphicMinimal;


namespace GraphicMinimalTest
{
    [TestClass]
    public class Matrix3x3Test
    {
        [TestMethod]
        public void Definition_Ident()
        {
            string func = "Ident()";

            Matrix3x3 m1 = Matrix3x3.Ident();
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix3x3 m2 = new Matrix3x3();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Translate()
        {
            string func = "Translate(3,4)";

            Matrix3x3 m1 = Matrix3x3.Translate(3, 4);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix3x3 m2 = new Matrix3x3();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Scale()
        {
            string func = "Scale(0.5,0.6)";

            Matrix3x3 m1 = Matrix3x3.Scale(0.5f, 0.6f);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix3x3 m2 = new Matrix3x3();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Rotate()
        {
            string func = "Rotate(90)";

            Matrix3x3 m1 = Matrix3x3.Rotate(90);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix3x3 m2 = new Matrix3x3();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Transpose()
        {
            string func = "Transpose(Translate(3,4))";

            Matrix3x3 m1 = Matrix3x3.Transpose(Matrix3x3.Translate(3, 4));
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix3x3 m2 = new Matrix3x3();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Invert()
        {
            string func = "Invert(Translate(3,4))";

            Matrix3x3 m1 = Matrix3x3.Invert(Matrix3x3.Translate(3, 4));
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix3x3 m2 = new Matrix3x3();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Mult()
        {
            string func = "Mult(Translate(3,4), Scale(5,6))";

            Matrix3x3 m1 = Matrix3x3.Translate(3, 4) * Matrix3x3.Scale(5, 6);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix3x3 m2 = new Matrix3x3();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_SpriteMatrix4()
        {
            string func = "SpriteMatrix(5, 6, 1, 2)";

            Matrix3x3 m1 = Matrix3x3.SpriteMatrix(5, 6, 1, 2);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix3x3 m2 = new Matrix3x3();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_SpriteMatrix3()
        {
            string func = "SpriteMatrix(5, 6, 1)";

            Matrix3x3 m1 = Matrix3x3.SpriteMatrix(5, 6, 1);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix3x3 m2 = new Matrix3x3();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void MultDirection_Rotate90()
        {
            Matrix3x3 m1 = Matrix3x3.Rotate(90);
            var v1 = new Vector2D(1, 0); //Zeige nach rechts
            var v2 = new Vector2D(0, 1); //Zeige nach oben

            var r1 = Matrix3x3.MultDirection(m1, v1); //Zeige nach oben
            var r2 = Matrix3x3.MultDirection(m1, v2); //Zeige nach links
            string s1 = (int)r1.X + ";" + (int)r1.Y;
            string s2 = (int)r2.X + ";" + (int)r2.Y;

            Assert.AreEqual("0;1", s1);
            Assert.AreEqual("-1;0", s2);      
        }

        [TestMethod]
        public void MultPosition_TranslateRight()
        {
            Matrix3x3 m1 = Matrix3x3.Translate(1, 0);
            var v1 = new Vector2D(0, 0);
            var v2 = new Vector2D(1, 1);

            var r1 = Matrix3x3.MultPosition(m1, v1);
            var r2 = Matrix3x3.MultPosition(m1, v2);

            string s1 = (int)r1.X + ";" + (int)r1.Y;
            string s2 = (int)r2.X + ";" + (int)r2.Y;

            Assert.AreEqual("1;0", s1);
            Assert.AreEqual("2;1", s2);
        }


        [TestMethod]
        public void GetSizeFactorFromMatrix_SizeFactor3()
        {
            float sizeFactor = Matrix3x3.GetSizeFactorFromMatrix(Matrix3x3.Scale(3, 3));
            Assert.AreEqual(3, sizeFactor);
        }

        [TestMethod]
        public void GetAngleInDegreeFromMatrix_Angle45()
        {
            float angle = Matrix3x3.GetAngleInDegreeFromMatrix(Matrix3x3.Rotate(45));
            Assert.AreEqual(45, (int)(angle + 0.5f));
        }

        [TestMethod]
        public void GetTranslationVectorFromMatrix_Translate11_12()
        {
            var translate = Matrix3x3.GetTranslationVectorFromMatrix(Matrix3x3.Translate(11, 12));
            Assert.AreEqual(11, translate.X);
            Assert.AreEqual(12, translate.Y);
        }
    }
}
