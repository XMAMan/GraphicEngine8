using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BitmapHelper;
using GraphicPanels;
using FullPathGenerator.AnalyseHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GraphicPanelsTest
{
    [TestClass]
    public class PixelConvergenceTests
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;
        private readonly float maxPixelErrorForStandardSzene = 10; //Maximal erlaubter Fehler für RingSphere/NoWindowRoom/Kornellbox

        [TestMethod]
        public void PixelConvergenzTest()
        {
            //File.WriteAllText(WorkingDirectory + "PixelKonvergenzTest.txt", PixelKonvergenzHelper.StartMasterTest());
        }



        [TestMethod]
        public void CheckPixelColor_PathTracer_RingSphere_YellowOnGround()
        {
            //Equal=88; Tent=107
            float color = GetPixelColor(TestScenes.AddTestscene1_RingSphere, Mode3D.PathTracer, 129, 228, 50000);
            Assert.IsTrue(Math.Abs(214 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_BidirectionalPathTracing_RingSphere_YellowOnGround()
        {
            float color = GetPixelColor(TestScenes.AddTestscene1_RingSphere, Mode3D.BidirectionalPathTracing, 129, 228, 100);
            Assert.IsTrue(Math.Abs(214 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_Photonmapping_RingSphere_YellowOnGround()
        {
            float color = GetPixelColor(TestScenes.AddTestscene1_RingSphere, Mode3D.Photonmapping, 129, 228, 100);
            Assert.IsTrue(Math.Abs(214 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_PathTracer_RingSphere_GreenRing()
        {
            //Equal=114; Tent=119
            float color = GetPixelColor(TestScenes.AddTestscene1_RingSphere, Mode3D.PathTracer, 254, 123, 60000);
            Assert.IsTrue(Math.Abs(157 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_BidirectionalPathTracing_RingSphere_GreenRing()
        {
            float color = GetPixelColor(TestScenes.AddTestscene1_RingSphere, Mode3D.BidirectionalPathTracing, 254, 123, 100);
            Assert.IsTrue(Math.Abs(157 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_Photonmapping_RingSphere_GreenRing()
        {
            float color = GetPixelColor(TestScenes.AddTestscene1_RingSphere, Mode3D.Photonmapping, 254, 123, 100);
            Assert.IsTrue(Math.Abs(157 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_PathTracer_RingSphere_YellowMarioReflection()
        {
            //Equal=6.5; Tent=6.74
            float color = GetPixelColor(TestScenes.AddTestscene1_RingSphere, Mode3D.PathTracer, 90, 310, 10000);
            Assert.IsTrue(Math.Abs(17 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_BidirectionalPathTracing_RingSphere_YellowMarioReflection()
        {
            float color = GetPixelColor(TestScenes.AddTestscene1_RingSphere, Mode3D.BidirectionalPathTracing, 90, 310, 1000);
            Assert.IsTrue(Math.Abs(17 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_Photonmapping_RingSphere_YellowMarioReflection()
        {
            float color = GetPixelColor(TestScenes.AddTestscene1_RingSphere, Mode3D.Photonmapping, 90, 310, 1000);
            Assert.IsTrue(Math.Abs(17 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_PathTracer_NoWindowRoom_GreenSphere()
        {
            //Equal=228; Tent=220
            float color = GetPixelColor(TestScenes.AddTestscene2_NoWindowRoom, Mode3D.PathTracer, 303, 175, 100000 * 1);
            Assert.IsTrue(Math.Abs(220 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        [Ignore]
        public void CheckPixelColor_BidirectionalPathTracing_NoWindowRoom_GreenSphere()
        {
            float color = GetPixelColor(TestScenes.AddTestscene2_NoWindowRoom, Mode3D.BidirectionalPathTracing, 303, 175, 8000);
            Assert.IsTrue(Math.Abs(220 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        [Ignore]
        public void CheckPixelColor_Photonmapping_NoWindowRoom_GreenSphere()
        {
            float color = GetPixelColor(TestScenes.AddTestscene2_NoWindowRoom, Mode3D.Photonmapping, 303, 175, 3000);
            Assert.IsTrue(Math.Abs(220 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_PathTracer_NoWindowRoom_Cupboard()
        {
            //Equal=135; Tent=135
            float color = GetPixelColor(TestScenes.AddTestscene2_NoWindowRoom, Mode3D.PathTracer, 178, 169, 5000 * 2);
            Assert.IsTrue(Math.Abs(135 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_BidirectionalPathTracing_NoWindowRoom_Cupboard()
        {
            float color = GetPixelColor(TestScenes.AddTestscene2_NoWindowRoom, Mode3D.BidirectionalPathTracing, 178, 169, 800);
            Assert.IsTrue(Math.Abs(135 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_Photonmapping_NoWindowRoom_Cupboard()
        {
            float color = GetPixelColor(TestScenes.AddTestscene2_NoWindowRoom, Mode3D.Photonmapping, 178, 169, 200);
            Assert.IsTrue(Math.Abs(135 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        [Ignore] //Für Godrays braucht man LightTracing oder eine Photonmap
        public void CheckPixelColor_MediaBidirectionalPathTracing_Mirrorbox_BeamRayAfterReflectionMultipeTimes()
        {
            //Equal=??; Tent=color = 55
            float color = GetPixelColor(TestScenes.AddTestscene5_MirrorCornellbox, Mode3D.MediaBidirectionalPathTracing, 169, 202, 50000);
            Assert.IsTrue(Math.Abs(55 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        [Ignore] //Für Godrays braucht man anscheinend LightTracing. Deswegen kommt ThinMediaMultipleScattering bei reflektierten Licht nicht klar
        public void CheckPixelColor_ThinMediaMultipleScattering_Mirrorbox_BeamRayAfterReflectionMultipeTimes()
        {
            //Equal=??; Tent=50
            float color = GetPixelColor(TestScenes.AddTestscene5_MirrorCornellbox, Mode3D.ThinMediaMultipleScattering, 169, 202, 5000);
            Assert.IsTrue(Math.Abs(50 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        [Ignore] //Für Godrays braucht man LightTracing oder eine Photonmap
        public void CheckPixelColor_MediaBidirectionalPathTracing_Mirrorbox_BeamRayWithoutRefelction()
        {
            //Equal=??; Tent=color = 254
            float color = GetPixelColor(TestScenes.AddTestscene5_MirrorCornellbox, Mode3D.MediaBidirectionalPathTracing, 209, 99, 5000);
            Assert.IsTrue(Math.Abs(254 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        [Ignore] //Laut mein Verständnis müsste das Verfahren trotz fehlenden LightTracing über das DirecdtLightingOnEdge eigentlich funktionieren ab es klappt leider nicht. Vielleicht ist im DirecdtLightingOnEdge noch ein Fehler 
        public void CheckPixelColor_ThinMediaMultipleScattering_Mirrorbox_BeamRayWithoutRefelction()
        {
            //Equal=??; Tent=257
            float color = GetPixelColor(TestScenes.AddTestscene5_MirrorCornellbox, Mode3D.ThinMediaMultipleScattering, 209, 99, 50000);
            Assert.IsTrue(Math.Abs(257 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_PathTracer_Cornellbox_GreenWall()
        {
            //Equal=167; Tent=167
            float color = GetPixelColor(TestScenes.AddTestscene5_Cornellbox, Mode3D.PathTracer, 343, 113, 50000);
            Assert.IsTrue(Math.Abs(167 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_BidirectionalPathTracing_Cornellbox_GreenWall()
        {
            float color = GetPixelColor(TestScenes.AddTestscene5_Cornellbox, Mode3D.BidirectionalPathTracing, 343, 113, 400);
            Assert.IsTrue(Math.Abs(167 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        [Ignore]
        public void CheckPixelColor_Photonmapping_Cornellbox_GreenWall()
        {
            float color = GetPixelColor(TestScenes.AddTestscene5_Cornellbox, Mode3D.Photonmapping, 343, 113, 200);
            Assert.IsTrue(Math.Abs(167 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_PathTracer_Cornellbox_BigCubeInFront()
        {
            //Equal=43; Tent=44
            float color = GetPixelColor(TestScenes.AddTestscene5_Cornellbox, Mode3D.PathTracer, 53, 177, 50000);
            Assert.IsTrue(Math.Abs(44 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_BidirectionalPathTracing_Cornellbox_BigCubeInFront()
        {
            float color = GetPixelColor(TestScenes.AddTestscene5_Cornellbox, Mode3D.BidirectionalPathTracing, 53, 177, 400);
            Assert.IsTrue(Math.Abs(44 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_Photonmapping_Cornellbox_BigCubeInFront()
        {
            float color = GetPixelColor(TestScenes.AddTestscene5_Cornellbox, Mode3D.Photonmapping, 53, 177, 2000);
            Assert.IsTrue(Math.Abs(44 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_PathTracer_Cornellbox_GlassSphereLightFlackOverRectangle()
        {
            //Equal=149; Tent=149
            float color = GetPixelColor(TestScenes.AddTestscene5_Cornellbox, Mode3D.PathTracer, 129, 209, 100000);
            Assert.IsTrue(Math.Abs(149 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        public void CheckPixelColor_BidirectionalPathTracing_Cornellbox_GlassSphereLightFlackOverRectangle()
        {
            float color = GetPixelColor(TestScenes.AddTestscene5_Cornellbox, Mode3D.BidirectionalPathTracing, 129, 209, 100000);
            Assert.IsTrue(Math.Abs(149 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        [TestMethod]
        [Ignore]
        public void CheckPixelColor_Photonmapping_Cornellbox_GlassSphereLightFlackOverRectangle()
        {
            float color = GetPixelColor(TestScenes.AddTestscene5_Cornellbox, Mode3D.Photonmapping, 129, 209, 100);
            Assert.IsTrue(Math.Abs(149 - color) < maxPixelErrorForStandardSzene, "color=" + color);
        }

        //Schwarze Würfelkanten-Problem
        [TestMethod]
        [Ignore] //Erstmal Ignor, da ich Wolken verändert habe und die Kante nun woanders liegt
        public void CheckPixelColor_ThinMediaTracer_CloudsAtDay_BlackEdge() //Misst die Farbdifferenz zwischen ein Pixel auf einer WOlken-Würfel-Kante und dem Himmel daneben
        {
            int sampleCount = 1000;
            Vector3D c1 = GetPixelColor(TestScenes.AddTestScene18_CloudsForTestImage, 1260, 984, new ImagePixelRange(new Point(98, 344), new Point(129, 371)), 19, 24 + 0, sampleCount, Mode3D.ThinMediaMultipleScattering).Sum() / sampleCount * 255; //Die Kante geht von Links nach Rechts auf der Y=24-Linie
            Vector3D c2 = GetPixelColor(TestScenes.AddTestScene18_CloudsForTestImage, 1260, 984, new ImagePixelRange(new Point(98, 344), new Point(129, 371)), 19, 24 + 1, sampleCount, Mode3D.ThinMediaMultipleScattering).Sum() / sampleCount * 255; //Unter der Kante liegend
            Vector3D c3 = GetPixelColor(TestScenes.AddTestScene18_CloudsForTestImage, 1260, 984, new ImagePixelRange(new Point(98, 344), new Point(129, 371)), 19 + 1, 24 + 1, sampleCount, Mode3D.ThinMediaMultipleScattering).Sum() / sampleCount * 255; //Schräg unter der Kante liegend
            float d1 = (c1 - c2).Length(); //So groß ist der Abstand zwischen Würfelkante und Himmel
            float d2 = (c2 - c3).Length(); //So groß soll der Abstand maximal sein

            float error = d1 / d2;
            Assert.IsTrue(d1/ d2  < 2, "Error="+error); //Die Farbdifferenz bei der Kante soll nicht mehr als doppelt so groß sein, wo die Differnz zwischen zwei Pixeln, welche direkt unter der Linie sind
        }

        [TestMethod]
        public void CheckPixelColor_Stilllife()
        {
            PixelData[] pixels = new PixelData[]
            {
                new PixelData(){Description = "Candle", Position = new Point(60, 170), Expected = new Vector3D(255, 204, 148), SampleCount = 70000}, //Actual: [0] = {new Vector3D(255f, 201f, 144f)}
                new PixelData(){Description = "Ground", Position = new Point(600, 210), Expected = new Vector3D(198, 196, 193), SampleCount = 40000},//Actual: [1] = {new Vector3D(199f, 197f, 193f)}
                new PixelData(){Description = "Juice Glass", Position = new Point(420, 100), Expected = new Vector3D(151, 157, 155), SampleCount = 10000},//Actual: [2] = {new Vector3D(150f, 157f, 154f)}
                new PixelData(){Description = "Green pot window reflection", Position = new Point(538, 134), Expected = new Vector3D(152, 180, 162), SampleCount = 20000}, //Actual: [3] = {new Vector3D(151f, 180f, 161f)}
            };

            Vector3D[] actuals = GetPixelColors(TestScenes.AddTestscene19_StillLife, 640, 280, Mode3D.MediaBidirectionalPathTracing, TonemappingMethod.GammaOnly, pixels);

            float[] errors = new float[pixels.Length];            
            for (int i=0;i< pixels.Length;i++)
            {
                float error = (pixels[i].Expected - actuals[i]).AbsMax();
                errors[i] = error;                
                Assert.IsTrue(error < 5, $"Error on Pixel ({pixels[i].Position.X},{pixels[i].Position.Y}) {pixels[i].Description} Error=" + error + " Actual=" + actuals[i].ToShortString());
            }
            float maxError = errors.Max();

            //Assert.IsTrue(false, "MaxError=" + maxError); //MaxError=0,03022611
        }

        [TestMethod]
        public void CheckPixelColor_Mirrorballs()
        {
            //Wegen der Glaslinse an den Lampen muss ich mit Lightracing arbeiten. Deswegen darf das Bild nur 80*80 groß sein, um die Pixel-Treffer-Wahrscheinlichkeit zu erhöhen.
            PixelData[] pixels = new PixelData[]
            {
                new PixelData(){Description = "Light", Position = new Point(24, 12), Expected = new Vector3D(83, 83, 81), SampleCount = 50000},
                new PixelData(){Description = "Pipe", Position = new Point(56, 34), Expected = new Vector3D(101, 98, 66), SampleCount = 50000},
            };

            Vector3D[] actuals = GetPixelColors(TestScenes.AddTestscene20_Mirrorballs, 80, 80, Mode3D.MediaFullBidirectionalPathTracing, TonemappingMethod.GammaOnly, pixels);

            float[] errors = new float[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                float error = (pixels[i].Expected - actuals[i]).AbsMax();
                errors[i] = error;
                Assert.IsTrue(error < 14, $"Error on Pixel ({pixels[i].Position.X},{pixels[i].Position.Y}) {pixels[i].Description} Error=" + error + " Actual=" + actuals[i].ToShortString());
            }
            float maxError = errors.Max();

            //Assert.IsTrue(false, "MaxError=" + maxError); //MaxError=0,08355415
        }

        class PixelData
        {
            public Point Position;
            public Vector3D Expected;
            public string Description;
            public int SampleCount = 1;
        }

        private Vector3D[] GetPixelColors(Action<GraphicPanel3D> addSceneMethod, int width, int height, Mode3D mode, TonemappingMethod tonemapping, PixelData[] pixels)
        {
            GraphicPanel3D panel = new GraphicPanel3D() { Width = width, Height = height };
            panel.GlobalSettings.PhotonCount = 40000;
            
            addSceneMethod(panel);
            panel.Mode = mode;
            panel.GlobalSettings.Tonemapping = tonemapping;
            panel.GlobalSettings.CameraSamplingMode = PixelSamplingMode.Equal;

            return pixels.Select(x => panel.GetColorFromSinglePixel(width, height, new ImagePixelRange(x.Position.X, x.Position.Y, 1,1), 0, 0, x.SampleCount)).ToArray();
            //return pixels.Select(x => panel.GetColorFromSinglePixel(width, height, null, x.Position.X, x.Position.Y, x.SampleCount)).ToArray();
        }

        private float GetPixelColor(Action<GraphicPanel3D> addSceneMethod, Mode3D mode, int pixX, int pixY, int sampleCount)
        {
            List<Vector3D> colorValuesRaw = GetPixelColor(addSceneMethod, 420, 328, null, pixX, pixY, sampleCount, mode);
            List<float> colorValues = colorValuesRaw.Select(x => VectorToColor(x)).ToList();
            return colorValues.Sum() / colorValues.Count;
        }
        private List<Vector3D> GetPixelColor(Action<GraphicPanel3D> addSceneMethod, int width, int height, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount,  Mode3D mode)
        {
            GraphicPanel3D panel = new GraphicPanel3D() { Width = width, Height = height };            
            addSceneMethod(panel);
            panel.GlobalSettings.PhotonCount = 40000;
            panel.Mode = mode;
            List<Vector3D> colorValuesRaw = panel.GetNPixelSamples(panel.Width, panel.Height, pixelRange, pixX, pixY, sampleCount);
            panel.Dispose();
            return colorValuesRaw;
        }

        private static float VectorToColor(Vector3D color)
        {
            return color * new Vector3D(0.2126f, 0.7152f, 0.0722f) * 255;
        }

        [TestMethod]
        public void PixelConvergenceImage()
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = 420, Height = 328 };
            graphic.GlobalSettings.PhotonCount = 100000;

            TestScenes.AddTestscene1_RingSphere(graphic);

            PixelConvergenceHelper.StartTest(graphic, new List<Mode3D>()
            {
                Mode3D.PathTracer,
                Mode3D.BidirectionalPathTracing,
                Mode3D.Photonmapping
            }).Save(WorkingDirectory + "PixelConvergenceTest.bmp");
        }       
    }
}
