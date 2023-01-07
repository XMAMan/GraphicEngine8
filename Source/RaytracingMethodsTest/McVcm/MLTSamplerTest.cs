using GraphicGlobal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaytracingMethods.McVcm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaytracingMethodsTest.McVcm
{
    [TestClass]
    public class MLTSamplerTest
    {
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
                0.5, //Keine Pertubation für Zahl 1
                0.5, //Keine Pertubation für Zahl 2
                0.3, 0.5, //Reset mit 0.3 und dann keine Pertubation für Zahl 3

                //Dritte Iteration mit SmallStep
                0.5, //Keine Pertubation für Zahl 1

                //Vierte Iteration mit SmallStep
                0.5, //Keine Pertubation für Zahl 1
                0.5, 0.5, //Keine Pertubation für Zahl 2
                0.5, 0.5, //Keine Pertubation für Zahl 3
                0.4, 0.5, 0.5, 0.5, //Reset mit 0.4 und dann keine Pertubation für Zahl 4
            });

            MLTSampler sut = new MLTSampler(randMock, false);

            // Erstere Iteration mit LargeStep
            sut.StartIteration(true);
            for (int i = 0; i < 2; i++) sut.NextDouble(); //Nimm 2 Zahlen

            var X = sut.GetX();
            Assert.IsTrue(sut.IsLargeStep);
            Assert.AreEqual(0.1, X[0].Value);
            Assert.AreEqual(0.2, X[1].Value);

            //Zweite Iteration mit SmallStep
            sut.Accept();
            sut.StartIteration(false);

            for (int i = 0; i < 3; i++) sut.NextDouble(); //Nimm 3 Zahlen

            X = sut.GetX();
            Assert.IsFalse(sut.IsLargeStep);
            Assert.AreEqual(0.1 - (1 / 64f), X[0].Value);
            Assert.AreEqual(0.2 - (1 / 64f), X[1].Value);
            Assert.AreEqual(0.3 - (1 / 64f), X[2].Value);

            //Dritte Iteration mit SmallStep
            sut.Accept();
            sut.StartIteration(false);

            sut.NextDouble(); //Nimm eine Zahl

            X = sut.GetX();
            Assert.IsFalse(sut.IsLargeStep);
            Assert.AreEqual(0.1 - (1 / 64f) * 2, X[0].Value);

            //Vierte Iteration mit SmallStep
            sut.Accept();
            sut.StartIteration(false);

            for (int i = 0; i < 4; i++) sut.NextDouble(); //Nimm 4 Zahlen

            X = sut.GetX();
            Assert.IsFalse(sut.IsLargeStep);
            Assert.AreEqual(0.1 - (1 / 64f) * 3, X[0].Value);
            Assert.AreEqual(0.2 - (1 / 64f) * 3, X[1].Value);
            Assert.AreEqual(0.3 - (1 / 64f) * 3, X[2].Value);
            Assert.AreEqual(0.4 - (1 / 64f) * 3, X[3].Value);
        }
    }
}
