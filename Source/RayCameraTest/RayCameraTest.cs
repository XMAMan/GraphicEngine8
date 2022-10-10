using System;
using GraphicMinimal;
using GraphicGlobal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RayCameraNamespace;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using BitmapHelper;
using PdfHistogram;

namespace RayCameraTestNamespace
{
    [TestClass]
    public class RayCameraTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;


        private float maxError = 0.001f;
        private int maxIntError = 20;

        [TestMethod]
        public void Constructor_Called_PropertysAreSet()
        {
            int pixel = 100;
            var sut = new RayCamera(new CameraConstructorData() { Camera = new Camera(new Vector3D(0, 0, 0), new Vector3D(0, 0, 1), 45), ScreenWidth = pixel, ScreenHeight = pixel, PixelRange = new ImagePixelRange(0, 0, pixel, pixel), SamplingMode = PixelSamplingMode.Equal });
            Assert.AreEqual(pixel * pixel, sut.PixelCountFromScreen);
        }

        [TestMethod]
        public void CreatePrimaryRay_CreateForCenterPixel_DirectionShowsToForwardDirection()
        {
            int pixel = 99;
            IRandom rand = new Rand(0);
            Vector3D cameraPosition = new Vector3D(1, 2, 3);
            Vector3D forward = Vector3D.Normalize(new Vector3D(0, 0, -1));
            var sut = new RayCamera(new CameraConstructorData() { Camera = new Camera(cameraPosition, forward, 45), ScreenWidth = pixel, ScreenHeight = pixel, PixelRange = new ImagePixelRange(0, 0, pixel, pixel), SamplingMode = PixelSamplingMode.None });
            Ray centerRay = sut.CreatePrimaryRay(pixel / 2, pixel / 2, rand);
            Assert.IsTrue((centerRay.Start - cameraPosition).Length() < maxError);
            Assert.IsTrue(Math.Abs(centerRay.Direction * forward - 1) < maxError);

            Ray leftRay = sut.CreatePrimaryRay(pixel / 2- 10, pixel / 2, rand);
            Assert.IsTrue(leftRay.Direction * forward > 0 && leftRay.Direction.X < 0 && Math.Abs(leftRay.Direction.Y) < maxError && leftRay.Direction.Z < 0 );

            Ray upRay = sut.CreatePrimaryRay(pixel / 2, pixel / 2 - 10, rand);
            Assert.IsTrue(upRay.Direction * forward > 0 && upRay.Direction.Y > 0 && Math.Abs(upRay.Direction.X) < maxError && upRay.Direction.Z < 0);
        }

        [TestMethod]
        public void CreatePrimaryRayWithPixi_CreateForCenterPixel_DirectionShowsToForwardDirection()
        {
            int pixel = 99;
            Vector3D cameraPosition = new Vector3D(1, 2, 3);
            Vector3D forward = Vector3D.Normalize(new Vector3D(0, 0, -1));
            var sut = new RayCamera(new CameraConstructorData() { Camera = new Camera(cameraPosition, forward, 45), ScreenWidth = pixel, ScreenHeight = pixel, PixelRange = new ImagePixelRange(0, 0, pixel, pixel), SamplingMode = PixelSamplingMode.None });
            Ray centerRay = sut.CreatePrimaryRayWithPixi(pixel / 2, pixel / 2, new Vector2D(0,0));
            Assert.IsTrue((centerRay.Start - cameraPosition).Length() < maxError);
            Assert.IsTrue(Math.Abs(centerRay.Direction * forward - 1) < maxError);
        }

        [TestMethod]
        public void IsPointInVieldOfFiew_CalledMultipeTimes_PointCountIsPyramidVolume()
        {
            float qx=1.5f,qy=1,qz = 1;
            float aspectRatio = qx / qy;

            int pixel = 100;
            Vector3D cameraPosition = new Vector3D(0, 0, 0);
            Vector3D forward = Vector3D.Normalize(new Vector3D(0, 0, -1));
            float foV = (float)(Math.Atan(qy / 2 / qz) / (2 * Math.PI) * 360) * 2;
            var sut = new RayCamera(new CameraConstructorData() { Camera = new Camera(cameraPosition, forward, foV), ScreenWidth = (int)(pixel * aspectRatio), ScreenHeight = pixel, PixelRange = new ImagePixelRange(0, 0, (int)(pixel * aspectRatio), pixel), SamplingMode = PixelSamplingMode.None });

            IRandom rand = new Rand(0);
            int pointCount = 1000;
            int counter = 0;

            bool b1 = sut.IsPointInVieldOfFiew(new Vector3D(0, qy / 2, qz));//Liegt genau auf der Grenze oben
            bool b2 = sut.IsPointInVieldOfFiew(new Vector3D(qx / 2, 0, qz));//Liegt genau auf der Grenze rechts
            bool b3 = sut.IsPointInVieldOfFiew(new Vector3D(qx / 2, qy / 2, qz));//Rechte Ecke oben

            //Obere Grenze Test
            Assert.IsFalse(sut.IsPointInVieldOfFiew(new Vector3D(0, qy / 2 + maxError, -1)));
            Assert.IsTrue(sut.IsPointInVieldOfFiew(new Vector3D(0, qy / 2 - maxError, -1)));

            //Rechte Grenze Test
            Assert.IsFalse(sut.IsPointInVieldOfFiew(new Vector3D(qx / 2 + maxError, 0, -1))); 
            Assert.IsTrue(sut.IsPointInVieldOfFiew(new Vector3D(qx / 2 - maxError, 0, -1)));

            for (int i=0;i<pointCount;i++)
            {
                Vector3D point = new Vector3D((float)(-0.5  + rand.NextDouble()) * qx, (float)(-0.5 + rand.NextDouble()) * qy, -(float)rand.NextDouble() * qz);
                if (sut.IsPointInVieldOfFiew(point)) counter++;
            }


            float frustumVolume = qx * qy * qz / 3; //Formel für Pyramidenvolumen
            float quaderVolume = qx * qy * qz;

            //Pyramide ist 1/3 so groß wie der Quader, der sie umschließt. ALso müssen 1/3 alle im Quader erzeugten Punkte in der Pyramide(ViewFrustum) liegen
            int expectedCount = (int)(pointCount /3);

            Assert.IsTrue(Math.Abs(counter - expectedCount) <= maxIntError);
        }

        [TestMethod]
        public void GetPixelPositionFromEyePoint_EdgePixels_MinMaxImageSize()
        {
            float qx = 1.5f, qy = 1, qz = 1;
            float aspectRatio = qx / qy;

            int pixel = 100;
            Vector3D cameraPosition = new Vector3D(0, 0, 0);
            Vector3D forward = Vector3D.Normalize(new Vector3D(0, 0, -1));
            float foV = (float)(Math.Atan(qy / 2 / qz) / (2 * Math.PI) * 360) * 2;
            var sut = new RayCamera(new CameraConstructorData() { Camera = new Camera(cameraPosition, forward, foV), ScreenWidth = (int)(pixel * aspectRatio), ScreenHeight = pixel, PixelRange = new ImagePixelRange(0, 0, (int)(pixel * aspectRatio), pixel), SamplingMode = PixelSamplingMode.None });

            //Center
            var ce = sut.GetPixelPositionFromEyePoint(new Vector3D(0, 0, -qz));
            Assert.IsTrue((int)ce.X == (int)(pixel * aspectRatio) / 2 && (int)ce.Y == pixel / 2);

            //Links Oben
            var lo = sut.GetPixelPositionFromEyePoint(new Vector3D(-qx / 2 + maxError, qy / 2, -qz));
            Assert.IsTrue((int)lo.X == 0 && (int)lo.Y == 0);

            //Rechts Oben
            var ro = sut.GetPixelPositionFromEyePoint(new Vector3D(qx / 2 - maxError, qy / 2, -qz));
            Assert.IsTrue((int)ro.X == (int)(pixel * aspectRatio) - 1 && (int)ro.Y == 0);

            //Links Unten
            var lu = sut.GetPixelPositionFromEyePoint(new Vector3D(-qx / 2 + maxError, -qy / 2 + maxError, -qz));
            Assert.IsTrue((int)lu.X == 0 && (int)lu.Y == pixel - 1);

            //Rechts unten
            var ru = sut.GetPixelPositionFromEyePoint(new Vector3D(qx / 2 - maxError, -qy / 2 + maxError, -qz));
            Assert.IsTrue((int)ru.X == (int)(pixel * aspectRatio) - 1 && (int)ru.Y == pixel - 1);
        }

        [TestMethod]
        public void GetPixelPositionFromEyePoint_MinSquare_MinMaxImageSize()
        {
            //In der XY-Ebene liegt ein Viereck mit der Kantenlänge 1
            //Es geht von -0.5 bis +0.5 sowohl bei X als auch Y
            //Die Kamera schaut von der Z-Achse im Abstand 1 drauf. Der Öffnunswinkel ist so, dass er genau das ganze Viereck sieht
            int pixelSize = 1;
            float imagePlaneSize = 1;
            float imagePlaneDistance = 1.0f;
            float foV = (float)(Math.Atan(imagePlaneSize / 2 / imagePlaneDistance) / (2 * Math.PI) * 360) * 2;
            var sut = new RayCamera(new CameraConstructorData() { Camera = new Camera(new Vector3D(0, 0, imagePlaneDistance), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), foV), ScreenWidth = pixelSize, ScreenHeight = pixelSize, PixelRange = new ImagePixelRange(0, 0, pixelSize, pixelSize), SamplingMode = PixelSamplingMode.Equal });

            var pix1 = sut.GetPixelPositionFromEyePoint(new Vector3D(+0.5f, +0.5f, 0));
            var pix2 = sut.GetPixelPositionFromEyePoint(new Vector3D(+0.5f, -0.5f, 0));
            var pix3 = sut.GetPixelPositionFromEyePoint(new Vector3D(-0.5f, +0.5f, 0));
            var pix4 = sut.GetPixelPositionFromEyePoint(new Vector3D(-0.5f, -0.5f, 0));

            Assert.IsTrue(Math.Abs(pix1.X - 1) < maxError);
            Assert.IsTrue(Math.Abs(pix1.Y - 0) < maxError);

            Assert.IsTrue(Math.Abs(pix2.X - 1) < maxError);
            Assert.IsTrue(Math.Abs(pix2.Y - 1) < maxError);

            Assert.IsTrue(Math.Abs(pix3.X - 0) < maxError);
            Assert.IsTrue(Math.Abs(pix3.Y - 0) < maxError);

            Assert.IsTrue(Math.Abs(pix4.X - 0) < maxError);
            Assert.IsTrue(Math.Abs(pix4.Y - 1) < maxError);
        }

        [TestMethod]
        public void GetPixelFootprintSize_EdgePoint_EqualsOkValue()
        {
            float qx = 1.5f, qy = 1, qz = 1;
            float aspectRatio = qx / qy;

            int pixel = 100;
            Vector3D cameraPosition = new Vector3D(0, 0, 0);
            Vector3D forward = Vector3D.Normalize(new Vector3D(0, 0, -1));
            float foV = (float)(Math.Atan(qy / 2 / qz) / (2 * Math.PI) * 360) * 2;
            var sut = new RayCamera(new CameraConstructorData() { Camera = new Camera(cameraPosition, forward, foV), ScreenWidth = (int)(pixel * aspectRatio), ScreenHeight = pixel, PixelRange = new ImagePixelRange(0, 0, (int)(pixel * aspectRatio), pixel), SamplingMode = PixelSamplingMode.None });

            //Center
            var ce = sut.GetPixelFootprintSize(new Vector3D(0, 0, -qz));
            Assert.IsTrue(ce.X == 0.01f && ce.Y == 0.01f);

            //Links Oben
            var lo = sut.GetPixelFootprintSize(new Vector3D(-qx / 2, qy / 2, -qz));
            Assert.IsTrue(Math.Abs(lo.X - 0.007427813f) < 0.000001f && Math.Abs(lo.Y - 0.007427813f) < 0.000001f);

            //Rechts Oben
            var ro = sut.GetPixelFootprintSize(new Vector3D(qx / 2, qy / 2, -qz));
            Assert.IsTrue(Math.Abs(ro.X - 0.007427813f) < 0.000001f && Math.Abs(ro.Y - 0.007427813f) < 0.000001f);
        }

        [TestMethod]
        public void GetPixelPdfW_EqualSampling_MatchWithHistogramPdf()
        {
            float maxError = 0.6f;
            int sampleCount = 1000000;
            float error = GetPixelpdfWError(sampleCount, PixelSamplingMode.Equal);
            Assert.IsTrue(error < maxError);
        }

        [TestMethod]
        public void GetPixelPdfW_TentSampling_MatchWithHistogramPdf()
        {
            float maxError = 0.12f;
            int sampleCount = 1000000;
            float error = GetPixelpdfWError(sampleCount, PixelSamplingMode.Tent);
            Assert.IsTrue(error < maxError, "Error=" + error);
        }

        [TestMethod]
        public void CreatePrimaryRay_TentSampling_DirectionLengthIsOne()
        {
            IRandom rand = new Rand("AAEAAAD/////AQAAAAAAAAAEAQAAAA1TeXN0ZW0uUmFuZG9tAwAAAAVpbmV4dAZpbmV4dHAJU2VlZEFycmF5AAAHCAgIKAAAAAYAAAAJAgAAAA8CAAAAOAAAAAgAAAAALdIvElz3eWmHGJ1QuFUyCn6IoG5tzy0KBky2cZ/51Hu/r/w2/HvCVG0I53ycek02NbymU/dd7wXVxb4+OkHTHMQtRzmaWEFx5NOmErW1rS40E+8EsgLeUVJWrWcD6qhRld3XaURCAyClOCchFyXVbeEs/2C7Q34GPEkPHg7/L3DooMAkjYRfGuuL3nDPaKN/dTlfXAJBxiYOWXdTjoufTDP60APIbrw+BNE4Q7FU6gV3JM5V3pq8WSzg1ne/B2dMWL9uVDqO6kDj/c0Dsu7TSVKpkyet4+M5Jhdkcws=");
            RayCamera sut = new RayCamera(new CameraConstructorData() { Camera = new Camera(new Vector3D(0f, 6360100f, 0f), new Vector3D(0.672672808f, 0.739940107f, 0f), 100), ScreenWidth = 400, ScreenHeight = 400, PixelRange = new ImagePixelRange(0, 0, 400, 400), DistanceDephtOfFieldPlane = 100, WidthDephtOfField = 2, SamplingMode = PixelSamplingMode.Tent });
            var ray = sut.CreatePrimaryRay(200, 384, rand);
            Assert.AreEqual(1, ray.Direction.Length());
        }

        [TestMethod]
        public void CreatePrimaryRay_TentSampling_PdfWFromCreatedRayIsNotNull1()
        {
            //Der notAllowedDirection-Richtungsvektor liegt im Pixel-Space bei fx = 1; fy = -0.0612022877 (Das passiert, wenn rand.NextDouble()==1 für die Pixel-X-Berechnung liefert)
            //Wenn nun die GetPixelPdfW die PixelSpace-Koordinaten ausrechnet, dann kommt aufeinmal fx = 1.00000381; fy = -0.0611953735 raus
            //Durch diese Abweichung würde die Abfrage hier 0 als PdfW-Wert returnen -> if (Math.Abs(fx) > 1 || Math.Abs(fy) > 1) return 0;
            //Durch pdfW==0 kommt bei der Pfad-PdfA dann auch 0 raus, was bei der MIS-Überprüfung MIS-Gewicht == 0 ergibt, was nicht erlaubt ist
            Vector3D notAllowedDirection = new Vector3D(-0.319146216f, -0.357097924f, -0.877853513f);
            IRandom rand = new Rand(0);
            RayCamera sut = new RayCamera(new CameraConstructorData() { Camera = new Camera(new Vector3D(-1.60623193f, 2.89738941f, 6.26268578f), new Vector3D(-0.120355569f, -0.116316527f, -0.985893011f), 80), ScreenWidth = 420, ScreenHeight = 328, PixelRange = new ImagePixelRange(0, 0, 420, 328), DistanceDephtOfFieldPlane = 100, WidthDephtOfField = 2, SamplingMode = PixelSamplingMode.Tent });
            for (int i = 0; i < 331984133 * 2; i++) rand.NextDouble();

            var ray = sut.CreatePrimaryRay(165, 215, rand);
            //if (notAllowedDirection == ray.Direction) throw new Exception("Gefunden");

            float pdfW = sut.GetPixelPdfW(165, 215, ray.Direction);
            Assert.IsTrue(pdfW > 0);
        }

        [TestMethod]
        public void CreatePrimaryRay_TentSampling_PdfWFromCreatedRayIsNotNull2()
        {
            Vector3D notAllowedDirection = new Vector3D(0.030893553f, -0.00104927761f, -0.99952209f);
            IRandom rand = new Rand(0);
            RayCamera sut = new RayCamera(new CameraConstructorData() { Camera = new Camera(new Vector3D(0.277999997f, 0.275000006f, 0.788999975f), new Vector3D(0f, 0f, -1f), 38), ScreenWidth = 420, ScreenHeight = 328, PixelRange = new ImagePixelRange(0, 0, 420, 328), DistanceDephtOfFieldPlane = 100, WidthDephtOfField = 2, SamplingMode = PixelSamplingMode.Tent });

            //for (int i = 0; true; i++)
            //{
            //    var ray = sut.CreatePrimaryRay(225, 163, rand);
            //    if (notAllowedDirection == ray.Direction) throw new Exception("Gefunden " + i);
            //}

            for (int i = 0; i < 140205872 * 2; i++) rand.NextDouble();

            var ray = sut.CreatePrimaryRay(225, 163, rand);
            //if (notAllowedDirection == ray.Direction) throw new Exception("Gefunden");

            float pdfW = sut.GetPixelPdfW(225, 163, ray.Direction);
            Assert.IsTrue(pdfW > 0);
        }

        private float GetPixelpdfWError(int sampleCount, PixelSamplingMode samplingMode)
        {
            int histogramSize = 100;          
            int screenWidth = 3;
            int screenHeight = 3;
            float imagePlaneSize = 1, imagePlaneDistance = 1; //imagePlaneSize = Breite/Höhe von der Bildebene

            Vector3D cameraPosition = new Vector3D(0, 0, 0);
            Vector3D forward = Vector3D.Normalize(new Vector3D(0, 0, 1));
            float foV = (float)(Math.Atan(imagePlaneSize / 2 / imagePlaneDistance) / (2 * Math.PI) * 360) * 2;
            var sut = new RayCamera(new CameraConstructorData() { Camera = new Camera(cameraPosition, forward, foV), ScreenWidth = screenWidth, ScreenHeight = screenHeight, PixelRange = new ImagePixelRange(0, 0, screenWidth, screenHeight), SamplingMode = samplingMode });

            AxialQuadChunkTable<PixelCounter> histogram = new AxialQuadChunkTable<PixelCounter>(new Vector3D(0, 0, imagePlaneDistance), 0, 1, imagePlaneSize, histogramSize);
            Plane imagePlane = new Plane(new Vector3D(0, 0, -1), new Vector3D(0, 0, imagePlaneDistance));

            IRandom rand = new Rand(0);

            //1. Histogram erstellen
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int i = 0; i < sampleCount; i++)
            {
                Ray ray = sut.CreatePrimaryRay(1, 1, rand);
                Vector3D p = imagePlane.GetIntersectionPointWithRay(ray);
                var field = histogram[p];

                if (field.X < minX) minX = field.X;
                if (field.X > maxX) maxX = field.X;
                if (field.Y < minY) minY = field.Y;
                if (field.Y > maxY) maxY = field.Y;

                float pdfA = sut.GetPixelPdfW(1, 1, ray.Direction) * (-ray.Direction * imagePlane.Normal) / p.SquareLength();
                field.Data.PdfAs.Add(pdfA);
            }

            //2. PdfA aus Histogram+Samplefunktion berechnen
            foreach (var field in histogram.EntryCollection())
            {
                if (field.PdfAs.Any())
                {
                    field.PdfAFromCamera = field.PdfAs.Average();
                    field.PdfAFromHistogram = (float)(field.PdfAs.Count / (double)sampleCount / histogram.DifferentialArea);
                }
            }

            //3. Auswertung/Vergleich
            var fields = histogram.EntryCollection().Where(x => x.PdfAs.Any());

            float minPdfAFromCamera = fields.Min(x => x.PdfAFromCamera);
            float maxPdfAFromCamera = fields.Max(x => x.PdfAFromCamera);
            float minPdfAFromHistogram = fields.Min(x => x.PdfAFromHistogram);
            float maxPdfAFromHistogram = fields.Max(x => x.PdfAFromHistogram);
            float minPdf = Math.Min(minPdfAFromCamera, minPdfAFromHistogram);
            float maxPdf = Math.Max(maxPdfAFromCamera, maxPdfAFromHistogram);

            Bitmap image = new Bitmap(histogramSize * 2, histogramSize);
            foreach (var field in histogram.EntryCollectionWithIndizes())
            {
                if (field.Data.PdfAs.Any())
                {
                    float f1 = PixelHelper.GetZeroToOne(minPdf, maxPdf, field.Data.PdfAFromCamera);
                    Color color1 = PixelHelper.ConvertFloatToColor(f1);
                    image.SetPixel(field.X, field.Y, color1);

                    float f2 = PixelHelper.GetZeroToOne(minPdf, maxPdf, field.Data.PdfAFromHistogram);
                    Color color2 = PixelHelper.ConvertFloatToColor(f2);
                    image.SetPixel(field.X + histogramSize, field.Y, color2);
                }
            }
            image.Save(WorkingDirectory + "cameraPdfW"+samplingMode+".bmp"); //Die beiden Kästchen müssen gleich aussehen

            float error = histogram.EntryCollection().Where(x => x.PdfAs.Any()).Select(x => Math.Abs(x.PdfAFromCamera - x.PdfAFromHistogram)).Average();
            string resultText = string.Join(System.Environment.NewLine, histogram.EntryCollection().Where(x => x.PdfAs.Any()).Select(x => x.PdfAFromCamera + "\t" + x.PdfAFromHistogram));
            return error;
        }

        class PixelCounter
        {
            public List<float> PdfAs = new List<float>(); //Das sind die PdfW-Werte von der Kamera, welche in eine PdfA umgerechnet wurden
            public float PdfAFromCamera;
            public float PdfAFromHistogram;
        }
    }
}
