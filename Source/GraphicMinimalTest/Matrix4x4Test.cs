using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GraphicMinimalTest
{
    [TestClass]
    public class Matrix4x4Test
    {
        [TestMethod]
        public void Definition_Ident()
        {
            string func = "Ident()";

            Matrix4x4 m1 = Matrix4x4.Ident();
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Translate()
        {
            string func = "Translate(3,4,-5)";

            Matrix4x4 m1 = Matrix4x4.Translate(3, 4, -5);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Scale()
        {
            string func = "Scale(0.5,0.6,0.7)";

            Matrix4x4 m1 = Matrix4x4.Scale(0.5f, 0.6f, 0.7f);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Rotate()
        {
            string func = "Rotate(90,1,0,0)";

            Matrix4x4 m1 = Matrix4x4.Rotate(90, 1, 0, 0);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Reflect()
        {
            string func = "Reflect(90,1,0,0)";

            Matrix4x4 m1 = Matrix4x4.Reflect(90, 1, 0, 0);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_LookAt()
        { 
            string func = "LookAt(0,0,0,0,0,-1,0,1,0)";

            //Diese Kamera erzeugt eine Einheitsmatrix bei LookAt
            var camera = new Camera(new Vector3D(0, 0, 0), new Vector3D(0, 0, -1), 45.0f);

            Matrix4x4 m1 = Matrix4x4.LookAt(camera.Position, camera.Forward, camera.Up);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());

            Assert.AreEqual(m1.Print(), Matrix4x4.Ident().Print());
        }

        [TestMethod]
        public void Definition_InverseLookAt()
        {
            string func = "InverseLookAt(0,1,2,3,4,5,6,7,8)";

            Matrix4x4 m1 = Matrix4x4.InverseLookAt(new Vector3D(0, 1, 2), new Vector3D(3, 4, 5), new Vector3D(6, 7, 8));
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Transpose()
        {
            string func = "Transpose(Translate(3,4,5))";

            Matrix4x4 m1 = Matrix4x4.Transpose(Matrix4x4.Translate(3, 4, 5));
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Invert()
        {
            string func = "Invert(Translate(3,4,5))";

            Matrix4x4 m1 = Matrix4x4.Invert(Matrix4x4.Translate(3, 4, 5));
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Mult()
        {
            string func = "Mult(Translate(3,4,5), Scale(5,6,7))";

            Matrix4x4 m1 = Matrix4x4.Translate(3, 4, 5) * Matrix4x4.Scale(5, 6, 7);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_Model()
        {
            string func = "Model(0,1,2,3,4,5,9)";

            Matrix4x4 m1 = Matrix4x4.Model(new Vector3D(0, 1, 2), new Vector3D(3, 4, 5), 9);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_NormalRotate()
        {
            string func = "NormalRotate(0,1,2)";

            Matrix4x4 m1 = Matrix4x4.NormalRotate(new Vector3D(0, 1, 2));
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_InverseModel()
        {
            string func = "InverseModel(0,1,2,3,4,5,9)";

            Matrix4x4 m1 = Matrix4x4.InverseModel(new Vector3D(0, 1, 2), new Vector3D(3, 4, 5), 9);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_InverseNormalRotate()
        {
            string func = "InverseNormalRotate(0,1,2)";

            Matrix4x4 m1 = Matrix4x4.InverseNormalRotate(new Vector3D(0, 1, 2));
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_ProjectionMatrixOrtho()
        {
            string func = "ProjectionMatrixOrtho(0,1,2,3,4,5)";

            Matrix4x4 m1 = Matrix4x4.ProjectionMatrixOrtho(0, 1, 2, 3, 4, 5);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_ProjectionMatrixPerspective()
        {
            string func = "ProjectionMatrixPerspective(0,1,2,3)";

            Matrix4x4 m1 = Matrix4x4.ProjectionMatrixPerspective(0, 1, 2, 3);
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_BilboardMatrixFromCameraMatrix()
        {
            string func = "BilboardMatrixFromCameraMatrix(0,1,2,3,4,5,6,LookAt(7,8,9,10,11,12,13,14,15))";

            Matrix4x4 m1 = Matrix4x4.BilboardMatrixFromCameraMatrix(new Vector3D(0, 1, 2), new Vector3D(3, 4, 5), 6, Matrix4x4.LookAt(new Vector3D(7, 8, 9), new Vector3D(10, 11, 12), new Vector3D(13, 14, 15)));
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void Definition_TBNMatrix()
        {
            string func = "TBNMatrix(0,1,2,3,4,5)";

            Matrix4x4 m1 = Matrix4x4.TBNMatrix(new Vector3D(0, 1, 2), new Vector3D(3, 4, 5));
            Assert.AreEqual(func, m1.Definition); //Prüfe Getter von Definition

            Matrix4x4 m2 = new Matrix4x4();
            m2.Definition = func;                 //Prüfe Setter von Definition

            Assert.AreEqual(m1.Print(), m2.Print());
        }

        [TestMethod]
        public void MultDirection_Rotate90()
        {
            Matrix4x4 m1 = Matrix4x4.Rotate(90, 0, 0, 1);
            var v1 = new Vector3D(1, 0, 0); //Zeige nach rechts
            var v2 = new Vector3D(0, 1, 0); //Zeige nach oben

            var r1 = Matrix4x4.MultDirection(m1, v1); //Zeige nach oben
            var r2 = Matrix4x4.MultDirection(m1, v2); //Zeige nach links

            string s1 = (int)r1.X + ";" + (int)r1.Y + ";" + (int)r1.Z;
            string s2 = (int)r2.X + ";" + (int)r2.Y + ";" + (int)r2.Z;

            Assert.AreEqual("0;1;0", s1);
            Assert.AreEqual("-1;0;0", s2);
        }

        [TestMethod]
        public void MultPosition_TranslateRight()
        {
            Matrix4x4 m1 = Matrix4x4.Translate(1, 0, 0);
            var v1 = new Vector3D(0, 0, 0);
            var v2 = new Vector3D(1, 1, 0);

            var r1 = Matrix4x4.MultPosition(m1, v1);
            var r2 = Matrix4x4.MultPosition(m1, v2);

            string s1 = (int)r1.X + ";" + (int)r1.Y + ";" + (int)r1.Z;
            string s2 = (int)r2.X + ";" + (int)r2.Y + ";" + (int)r2.Z;

            Assert.AreEqual("1;0;0", s1);
            Assert.AreEqual("2;1;0", s2);
        }

        [TestMethod]
        public void GetSizeFactorFromMatrix_SizeFactor3()
        {
            float sizeFactor = Matrix4x4.GetSizeFactorFromMatrix(Matrix4x4.Scale(3, 3, 3));
            Assert.AreEqual(3, sizeFactor);
        }

        [TestMethod]
        public void GetAngleInDegreeFromMatrix_Angle45()
        {
            float angle = Matrix4x4.GetAngleInDegreeFromMatrix(Matrix4x4.Rotate(45, 0,0,1));
            Assert.AreEqual(45, (int)(angle + 0.5f));
        }

        [TestMethod]
        public void GetTranslationVectorFromMatrix_Translate11_12_0()
        {
            var translate = Matrix4x4.GetTranslationVectorFromMatrix(Matrix4x4.Translate(11, 12, 0));
            Assert.AreEqual(11, translate.X);
            Assert.AreEqual(12, translate.Y);
        }
    }
}
