using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using ParticipatingMedia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParticipatingMediaTest
{
    [TestClass]
    public class SkyIntegratorTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        private float maxError = 84;

        public const float EarthRadius = 6360000;
        public const float AtmosphereRadius = 6420000;
        public const float ScaleHeigh = 7994;

        [TestMethod]
        public void GetIntegralFromLine_CalledForParallelToSunDirection_IntegralMatchWithMonteIntegral()
        {
            var data = new TestData();

            //Integral der Partikeldichte von Punkt 100m über der Erde bis Atmospährenrand
            Vector3D p1 = new Vector3D(0, (float)(EarthRadius + 100), 0);
            Vector3D p2 = new Vector3D(0, (float)AtmosphereRadius, 0);

            double actualValue = data.SkyIntegrator.GetIntegralFromLine(p1, p2);
            //double expectedValue = data.ExpIntegralWithMonte.GetIntegralFromLineWithMonte(p1, p2);
            double expectedValue = 7890.3151295869475; //Berechnet mit MonteCarlo mit 100000000 Samples

            Assert.IsTrue(Math.Abs(actualValue - expectedValue) < 10, "Error=" + Math.Abs(actualValue - expectedValue));
        }

        [TestMethod]
        public void GetIntegralFromLine_CalledForOrtogonalToSunDirection_IntegralMatchWithMonteIntegral()
        {
            var data = new TestData();

            //Erzeuge Horizontale Linie 100 Meter über die Erde
            Vector2D p = IntersectionHelper2D.IntersectionPointRayCircle(new Vector2D(-AtmosphereRadius * 3, EarthRadius + 100), new Vector2D(1, 0), new Vector2D(0, 0), AtmosphereRadius);
            Vector3D p1 = new Vector3D(p.X, p.Y, 0);
            Vector3D p2 = new Vector3D(-p.X, p.Y, 0);

            double actualValue = data.SkyIntegrator.GetIntegralFromLine(p1, p2);
            //double expectedValue = data.ExpIntegralWithMonte.GetIntegralFromLineWithMonte(p1, p2);
            double expectedValue = 558411.02088211768; //Berechnet mit MonteCarlo mit 100000000 Samples
            //double expectedValue = 558403.867358797;     //Berechnet mit MonteCarlo mit 1000000000 Samples

            Assert.IsTrue(Math.Abs(actualValue - expectedValue) < maxError, "Error=" + Math.Abs(actualValue - expectedValue));
        }

        #region TestDataCreation
        [TestMethod]
        [Ignore]
        public void CreateExpectedValuesForLineIntegralTest()
        {
            var data = new TestData();

            StringBuilder str = new StringBuilder();

            IRandom rand = new Rand(0);
            for (int i = 0; i < 1000; i++)
            {
                Vector3D p1 = GetRandomPointOnSphere(data.AirDescription.EarthCenter, AtmosphereRadius, rand.NextDouble(), rand.NextDouble());
                Vector3D toP2 = GetRandomPointOnSphere(new Vector3D(0, 0, 0), 1, rand.NextDouble(), rand.NextDouble());

                Vector3D p2 = IntersectionHelper.GetIntersectionPointBetweenRayAndSphere(new Ray(p1, toP2), new Vector3D(0, 0, 0), AtmosphereRadius);
                if (p2 == null) continue;

                Vector3D earth = IntersectionHelper.GetIntersectionPointBetweenRayAndSphere(new Ray(p1, toP2), new Vector3D(0, 0, 0), EarthRadius);
                if (earth != null) p2 = earth;

                if ((p2 - p1).Length() < 1) continue;

                double expectedValue = data.ExpIntegralWithMonte.GetIntegralFromLineWithMonte(p1, p2);

                str.AppendLine(p1.ToShortString() + ":" + p2.ToShortString() + ":" + expectedValue);
            }

            File.WriteAllText(WorkingDirectory + "SkyIntegratorTestData.txt", str.ToString());
        }

        private Vector3D GetRandomPointOnSphere(Vector3D lightSourceCenter, float lightSourceRadius, double u1, double u2)
        {
            float theata = 2 * (float)(Math.PI * u1);
            float phi = (float)(Math.Acos(1 - 2 * u2));
            return lightSourceCenter + new Vector3D((float)(Math.Sin(phi) * Math.Cos(theata)), (float)(Math.Sin(phi) * Math.Sin(theata)), (float)(Math.Cos(phi))) * lightSourceRadius;
        }

        class TestDataRow
        {
            public Vector3D P1;
            public Vector3D P2;
            public float ExpectedValue;
        }

        private List<TestDataRow> GetRowsFromFile(string file)
        {
            List<TestDataRow> rows = new List<TestDataRow>();
            var lines = File.ReadLines(file);
            foreach (var line in lines)
            {
                var fields = line.Split(':');
                rows.Add(new TestDataRow()
                {
                    P1 = Vector3D.Parse(fields[0]),
                    P2 = Vector3D.Parse(fields[1]),
                    ExpectedValue = Convert.ToSingle(fields[2])
                });
            }

            return rows;
        }
        #endregion

        [TestMethod]
        public void GetIntegralFromLine_CalledForRandomLines_IntegralMatchWithMonteIntegral()
        {
            var testRows = GetRowsFromFile(WorkingDirectory + "ExpectedValues\\SkyIntegratorTestData.txt");

            IRandom rand = new Rand(0);
            List<double> errorList = new List<double>();
            var data = new TestData();

            foreach (var row in testRows)
            {
                double expectedValue = row.ExpectedValue;
                double actualValue = data.SkyIntegrator.GetIntegralFromLine(row.P1, row.P2);

                errorList.Add(Math.Abs(actualValue - expectedValue));
            }

            errorList = errorList.OrderByDescending(x => x).ToList();

            Assert.IsTrue(errorList[0] < maxError, "Error=" + errorList[0]);
        }

        class TestData
        {
            public LayerOfAirDescription AirDescription { get; private set; }
            public SkyIntegrator SkyIntegrator { get; private set; }
            public ExpIntegralWithMonte ExpIntegralWithMonte { get; private set; }

            public TestData()
            {
                LayerOfAirDescription layerOfAir = new LayerOfAirDescription()
                {
                    EarthCenter = new Vector3D(0,0,0),
                    EarthRadius = SkyIntegratorTest.EarthRadius,
                    AtmosphereRadius = SkyIntegratorTest.AtmosphereRadius,
                    ScaleHeigh = SkyIntegratorTest.ScaleHeigh,
                };
                //Maxerror bei diesen Zahlen: 84; Testzeit 2 Sekunden
                int thetaStepCount = 8192; //Die Genauigkeit vom SkyIntegrator hängt hauptsächlich von dieser Zahl ab
                int tStepCount = 64;       //Diese Zahl wird als zweites Wichtig, wenn es um die Genauigkeit geht
                int hStepCount = 4;
                

                long monteStepCount = 100000000 * 10; //Das hat 444 Minuten gedauert, um die 1000 Samples zu erzeugen
                this.AirDescription = layerOfAir;
                this.SkyIntegrator = new SkyIntegrator(layerOfAir, hStepCount, thetaStepCount, tStepCount);
                this.ExpIntegralWithMonte = new ExpIntegralWithMonte(new Vector3D(0,0,0), SkyIntegratorTest.EarthRadius, SkyIntegratorTest.AtmosphereRadius, SkyIntegratorTest.ScaleHeigh, monteStepCount);
            }
        }
    }

    class ExpIntegralWithMonte
    {
        private Vector3D center;
        private double hMinValue;
        private double hMaxValue;
        private double scaleHeigh;
        private long monteStepCount;
        

        public ExpIntegralWithMonte(Vector3D center, double hMinValue, double hMaxValue, double scaleHeigh, long monteStepCount)
        {
            this.center = center;
            this.hMinValue = hMinValue;
            this.hMaxValue = hMaxValue;
            this.scaleHeigh = scaleHeigh;
            this.monteStepCount = monteStepCount;
        }
        public double GetIntegralFromLineWithMonte(Vector3D p1, Vector3D p2)
        {
            Vector3D direction = Vector3D.Normalize(p2 - p1);
            float distance = (p2 - p1).Length();
            Random rand = new Random(0);
            double sum = 0;
            double pdfForSamplingT = 1.0 / distance;
            for (long i =0;i< this.monteStepCount;i++)
            {
                double t = rand.NextDouble() * distance;
                Vector3D p = p1 + direction * (float)t;
                double h = Math.Max(0, (p - this.center).Length() - this.hMinValue);
                //if (h < 0) throw new Exception("Die Höhe darf nicht kleiner 0 sein");
                if (h > this.hMaxValue) throw new Exception("Die Höhe darf nicht großer als hMaxValue sein");
                double exp = Math.Exp(-h / this.scaleHeigh);
                sum += exp / pdfForSamplingT / this.monteStepCount;
            }
            return sum;
        }
    }
}
