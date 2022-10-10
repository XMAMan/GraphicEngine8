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
    }
}
