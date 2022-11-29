using GraphicGlobal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaytracingMethods.MMLT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaytracingMethodsTest
{
    [TestClass]
    public class MLTSamplerTest
    {
        //Erst wird ein Eyepath mit 3 Zahle, ein Lightpath mit 2 und Fullpathsampler mit einer Random-Zahl erzeugt (Initial-Large-Step)
        //Dann wächst der Eyepath um eine Zahl im nächsten Small-Step. Erwartung: Der Lightpath und FullpathSampler bleibt unberührt
        [TestMethod]
        public void GrowingEyePath_DoesNotModifieTheLightSubpath()
        {
            RandMock randMock = new RandMock(new List<double>()
            {
                //Erster Fullpath mit LargeStep
                0.1, //Eypath[0]
                0.11, //Eypath[1]
                0.12, //Eypath[2]
                0.20, //Lightpath[0]
                0.21, //Lightpath[1]
                0.30, //Full[0]              

                //Zweiter Fullpath mit SmallStep
                1,  //Starte ein SmallStep
                0.5 ,0.5, 0.5, //Eyepath wird nicht pertubiert (SmallStep-Sigma-Zahlen)
                0.13, //Eyepath (Eine Zahl kommt hinzu)
                0.5, //Die neue Eyepath-Zahl wird nicht pertubiert
                0.5 ,0.5, //Lightpath (Unverändert)
                0.5, //Fullpath-Connect (Unverändert)
            });

            MLTSampler sut = new MLTSampler(randMock, 0.01f, 0.3f, 3);

            //Erster Fullpath mit LargeStep
            sut.StartStream(0); //Starte Eye-Subpath
            for (int i=0;i<3;i++) sut.NextDouble(); //Eye-Subpath braucht 3 Zahlen

            sut.StartStream(1); //Starte Light-Subpath
            for (int i = 0; i < 2; i++) sut.NextDouble(); //Light-Subpath braucht 2 Zahlen

            sut.StartStream(2); //Starte den Fullpathsampler
            sut.NextDouble(); //Der Fullpathsampler braucht eine Zahl um ein einzelnen Fullpath zu erstellen

            var X = sut.GetX();
            Assert.IsTrue(sut.IsLargeStep);

            //Eyepath-Check
            Assert.AreEqual(0.10, X[0].Value);
            Assert.AreEqual(0.11, X[3].Value);
            Assert.AreEqual(0.12, X[6].Value);

            //Lightpath-Check
            Assert.AreEqual(0.20, X[1].Value);
            Assert.AreEqual(0.21, X[4].Value);

            //Fullpath-Check
            Assert.AreEqual(0.30, X[2].Value);




            //Zweiter Fullpath mit SmallStep; EyePath bekommt eine Zahl hinzu
            sut.Accept();
            sut.StartIteration();

            sut.StartStream(0); //Starte Eye-Subpath
            for (int i = 0; i < 4; i++) sut.NextDouble(); //Eye-Subpath braucht jetzt 4 Zahlen (Einer hinzu)

            sut.StartStream(1); //Starte Light-Subpath
            for (int i = 0; i < 2; i++) sut.NextDouble(); //Light-Subpath braucht 2 Zahlen (Unverändert)

            sut.StartStream(2); //Starte den Fullpathsampler
            sut.NextDouble(); //Der Fullpathsampler braucht eine Zahl um ein einzelnen Fullpath zu erstellen (Unverändert)

            X = sut.GetX();
            Assert.IsFalse(sut.IsLargeStep);

            //Eyepath-Check
            Assert.AreEqual(0.10, X[0].Value);
            Assert.AreEqual(0.11, X[3].Value);
            Assert.AreEqual(0.12, X[6].Value);
            Assert.AreEqual(0.13, X[9].Value); //Diese Zahl kam beim SmallStep hinzu. Um diese Zahl zu erzeugen musste ein Reset erfolgen

            //Lightpath-Check
            Assert.AreEqual(0.20, X[1].Value);
            Assert.AreEqual(0.21, X[4].Value);

            //Fullpath-Check
            Assert.AreEqual(0.30, X[2].Value);
        }

        //Erzeuge initialen Large-Step-Fullpfad und Rejecte ihn dann. Erwartungshaltung: Beim nächsten erzeugten Fullpath muss zwangsweise ein LargeStep
        //erfolgen, da man ja auf ein Rejecteten Pfad kein SmallStep machen darf
        [TestMethod]
        public void RejectFirstPath_SecondPathIsALargeStep()
        {
            RandMock randMock = new RandMock(new List<double>()
            {
                //Erstere Initial-Iteration mit LargeStep die Rejected wird
                0.1, 
                0.1,                           

                //Zweite Iteration mit LargeStep
                //1,  //Es erfolgt kein Sampling, ob die nächste Iteration Small/Large wird da hier zwangsweise ein LargeStep erfolgen muss
                0.2,
                0.2,
                0.2,
            });

            MLTSampler sut = new MLTSampler(randMock, 0.01f, 0.3f, 1);

            // Erstere Iteration mit LargeStep
            sut.StartStream(0); 
            for (int i = 0; i < 2; i++) sut.NextDouble(); //Nimm 2 Zahlen

            var X = sut.GetX();
            Assert.IsTrue(sut.IsLargeStep);
            Assert.AreEqual(0.1, X[0].Value);
            Assert.AreEqual(0.1, X[1].Value);

            //Zweite Iteration mit LargeStep
            sut.Reject();
            sut.StartIteration();

            sut.StartStream(0);
            for (int i = 0; i < 3; i++) sut.NextDouble(); //Nimm 3 Zahlen

            X = sut.GetX();
            Assert.IsTrue(sut.IsLargeStep);
            Assert.AreEqual(0.2, X[0].Value);
            Assert.AreEqual(0.2, X[1].Value);
            Assert.AreEqual(0.2, X[2].Value);
        }

        //-Wenn ein SmallStep erfolgen soll, dann stelle sicher, dass die Pertubation auf einer gültigen Small- oder Large-Step-Zahl erfolgt und
        //  Reset(Simulierter LargeStep) nur dann erfolgt, wenn nötig

        //0 L 12
        //1 S 123     -> Für die 3 muss ein Reset auf Iteration 0 erfolgen; die 2 nutzt ohne Reset Iteration 0
        //2 S 1
        //3 S 1234    -> Die 2 wird ohne Reset auf Iteration 1 aufbauend erzeugt; Für die 4 muss ein Reset auf Iteration 0 erfolgen
        [TestMethod]
        public void SmallStepIteration1_CheckThatResetIsUsed()
        {
            RandMock randMock = new RandMock(new List<double>()
            {
                //Erstere Iteration mit LargeStep
                0.1,
                0.2,                           

                //Zweite Iteration mit SmallStep
                1,  //Starte ein SmallStep
                0.5, //Keine Pertubation für Zahl 1
                0.5, //Keine Pertubation für Zahl 2
                0.3, 0.5, //Reset mit 0.3 und dann keine Pertubation für Zahl 3

                //Dritte Iteration mit SmallStep
                1,  //Starte ein SmallStep
                0.5, //Keine Pertubation für Zahl 1

                //Vierte Iteration mit SmallStep
                1,  //Starte ein SmallStep
                0.5, //Keine Pertubation für Zahl 1
                0.5, //Keine Pertubation für Zahl 2
                0.5, //Keine Pertubation für Zahl 3
                0.4, 0.5, //Reset mit 0.4 und dann keine Pertubation für Zahl 4
            });

            MLTSampler sut = new MLTSampler(randMock, 0.01f, 0.3f, 1);

            // Erstere Iteration mit LargeStep
            sut.StartStream(0);
            for (int i = 0; i < 2; i++) sut.NextDouble(); //Nimm 2 Zahlen

            var X = sut.GetX();
            Assert.IsTrue(sut.IsLargeStep);
            Assert.AreEqual(0.1, X[0].Value);
            Assert.AreEqual(0.2, X[1].Value);

            //Zweite Iteration mit SmallStep
            sut.Accept();
            sut.StartIteration();

            sut.StartStream(0);
            for (int i = 0; i < 3; i++) sut.NextDouble(); //Nimm 3 Zahlen

            X = sut.GetX();
            Assert.IsFalse(sut.IsLargeStep);
            Assert.AreEqual(0.1, X[0].Value);
            Assert.AreEqual(0.2, X[1].Value);
            Assert.AreEqual(0.3, X[2].Value);

            //Dritte Iteration mit SmallStep
            sut.Accept();
            sut.StartIteration();

            sut.StartStream(0);
            sut.NextDouble(); //Nimm eine Zahl

            X = sut.GetX();
            Assert.IsFalse(sut.IsLargeStep);
            Assert.AreEqual(0.1, X[0].Value);

            //Vierte Iteration mit SmallStep
            sut.Accept();
            sut.StartIteration();

            sut.StartStream(0);
            for (int i = 0; i < 4; i++) sut.NextDouble(); //Nimm 4 Zahlen

            X = sut.GetX();
            Assert.IsFalse(sut.IsLargeStep);
            Assert.AreEqual(0.1, X[0].Value);
            Assert.AreEqual(0.2, X[1].Value);
            Assert.AreEqual(0.3, X[2].Value);
            Assert.AreEqual(0.4, X[3].Value);
        }

        //-MTLSampler nutzen um mit einzelnen Stream/Zahl im Stream den 1D-Funktionstest nachzustellen
    }
}
