using BitmapHelper;
using GraphicMinimal;
using GraphicPanels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTestHelper;

namespace GraphicPanelsTest
{
    //Testet all die Hilfsfunktionen die es so extra noch gibt (Bitmap erstellen; Fehler untersuchen) 
    [TestClass]
    public class HelperFunctionsTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;
        private readonly string DataDirectory = UnitTestHelper.FilePaths.DataDirectory;
        private readonly string StilllifeDirectory = UnitTestHelper.FilePaths.StilllifeDirectory;

        //Beispiel wie man die TransformColorToMaxAlpha-Funktion verwendet um den Hintergrund transparent zu machen
        [TestMethod]
        public void TransformColorToMaxAlpha()
        {
            GraphicPanel2D.TransformColorToMaxAlpha(new Bitmap(DataDirectory + "Fire2.jpg"), Color.Black, 0.5f).Save(WorkingDirectory + "FireWithTransparentBackground.png", System.Drawing.Imaging.ImageFormat.Png);

            Bitmap actual = new Bitmap(WorkingDirectory + "FireWithTransparentBackground.png");
            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\FireWithTransparentBackground_Expected.png");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, actual));
        }

        //Beispiel wie man ein aufgehelltes Bild erzeugt
        [TestMethod]
        public void ScaleColor()
        {
            BitmapHelp.ScaleColor(BitmapHelp.AddToColor(new Bitmap(DataDirectory + "Pilz.png"), new Vector3D(1, 1, 1) * 0.0f), new Vector3D(1, 1, 1) * 1.5f).Save(WorkingDirectory + "PilzBright.png");

            Bitmap actual = new Bitmap(WorkingDirectory + "PilzBright.png");
            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\PilzBright_Expected.png");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, actual));
        }

        [TestMethod]
        public void GetMaterialOverviewFromObjFile()
        {
            string objMaterials = GraphicPanel3D.GetMaterialOverviewFromObjFile(DataDirectory + "20_Mirrorballs.obj");
            File.WriteAllText(WorkingDirectory + "MirrorballsMaterials.txt", objMaterials);
            string expected = File.ReadAllText(WorkingDirectory + "ExpectedValues\\MirrorballsMaterials_Expected.txt");
            Assert.AreEqual(expected, objMaterials);
        }

        //Beispiel wie man ein Differenzbild erzeugt
        [TestMethod]
        public void GetDifferenceImage()
        {
            DifferenceImageCreator.GetDifferenceImage(new Bitmap(StilllifeDirectory + "Stilllife 50000(Reference).bmp"), new Bitmap(StilllifeDirectory + "Stilllife 50000.bmp"), true).Image.Save(WorkingDirectory + "Stilllife_DifferenceUPBP.png", ImageFormat.Png);
            DifferenceImageCreator.GetDifferenceImage(new Bitmap(StilllifeDirectory + "Stilllife 50000(Reference-BPT).bmp"), new Bitmap(StilllifeDirectory + "Stilllife 50000 BPT(Wrong).bmp"), true).Image.Save(WorkingDirectory + "Stilllife_DifferenceBPT(Wrong).png", ImageFormat.Png);

            Bitmap actual1 = new Bitmap(WorkingDirectory + "Stilllife_DifferenceUPBP.png");
            Bitmap expected1 = new Bitmap(WorkingDirectory + "ExpectedValues\\Stilllife_DifferenceUPBP_Expected.png");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected1, actual1));

            Bitmap actual2 = new Bitmap(WorkingDirectory + "Stilllife_DifferenceBPT(Wrong).png");
            Bitmap expected2 = new Bitmap(WorkingDirectory + "ExpectedValues\\Stilllife_DifferenceBPT(Wrong)_Expected.png");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected2, actual2));
        }

        //So kann man die Summe über ein Pixelbereich bilden
        [TestMethod]
        public void GetPixelRangeColor()
        {
            string col1 = BitmapHelp.GetPixelRangeColor(new Bitmap(StilllifeDirectory + "Stilllife 50000(Reference-BPT).bmp"), new ImagePixelRange(new Point(17 * 4, 35 * 4), new Point(17 * 4 + 11, 35 * 4 + 10))).ToShortString();
            string col2 = BitmapHelp.GetPixelRangeColor(new Bitmap(StilllifeDirectory + "Stilllife 50000 BPT(Wrong).bmp"), new ImagePixelRange(new Point(17 * 4, 35 * 4), new Point(17 * 4 + 11, 35 * 4 + 10))).ToShortString();
            string col3 = BitmapHelp.GetPixelRangeColor(new Bitmap(StilllifeDirectory + "Stilllife 50000 BPT.bmp"), new ImagePixelRange(new Point(17 * 4, 35 * 4), new Point(17 * 4 + 11, 35 * 4 + 10))).ToShortString();

            Assert.AreEqual("[104,827522;75,9058838;55,0509911]", col1); //Referenz
            Assert.AreEqual("[98,6079102;72,7294083;53,415699]", col2);  //BPT-Wrong
            Assert.AreEqual("[104,592224;76,9058685;55,6745186]", col3); //BPT
        }

        //Ermittle die Farbe von ein einzelnen Pixel
        [TestMethod]
        public void GetColorFromSinglePixel()
        {
            GraphicPanel3D panel = new GraphicPanel3D() { Width = 420, Height = 328 };
            TestScenes.AddTestszene5_Cornellbox(panel);
            panel.Mode = Mode3D.BidirectionalPathTracing;            
            string color = panel.GetColorFromSinglePixel(420, 328, null, 53, 177, 10000).ToShortString();
            panel.Dispose();

            Assert.AreEqual("[96;29;26]", color); //BPT
        }

        //So kann man schauen, welches Verfahren schneller gegen den richtigen Wert konvergiert
        //Beispiele:
        //PixelConvergenceHelper.GetPixelConvergenzImage(this.graphicPanel, Mode3D.MediaBidirectionalPathTracing, 50, 50, 1000).Save("PixelKonvergenzTest.bmp"); //Schaue für ein einzelnes Verfahren wie schnell es konvergiert
        //PixelConvergenceHelper.GetPixelConvergenzImage(this.graphicPanel, 640, 280, Modus3D.MediaBidirectionalPathTracing, null, 60, 170, 100000).Save("Kerze_g_0_8.bmp");

        //Hiermit habe ich untersucht, ob das Bild schneller konvergiert, wenn ich per ContinationPdf das Pfadgewicht bei 1 halte
        //Ergebnis: Es bringt keine Verbesserung, wenn ich die Absorbation mit dem BrdfWeightAfterSampling mache. Die Diffuse+Mirror-Farbe reicht aus(Erst Absorbation, dann Richtungssampling)
        //PixelConvergenceHelper.WritePixelDataToFile(this.graphicPanel, "Absorbation nach BrdfSampling", 50, 50, 1000, "File1.dat"); //Ich kommentiere Absorbation im BrdfSampler aus
        //PixelConvergenceHelper.WritePixelDataToFile(this.graphicPanel, "Absorbation vor BrdfSampling", 50, 50, 1000, "File2.dat"); //Ich kommentiere Absorbation im SubPathSampler aus
        //PixelConvergenceHelper.GetKonvergenzimageFromFiles(420, 328, new string[] { "File1.dat" , "File2.dat" }).Save("PixelKonvergenzTest.bmp");
        [TestMethod]
        public void GetPixelConvergenzImage()
        {
            GraphicPanel3D panel = new GraphicPanel3D() { Width = 420, Height = 328 };
            TestScenes.AddTestszene5_Cornellbox(panel);

            //Pixel [53, 177] = BigCubeInFront
            PixelConvergenceHelper.WritePixelDataToFile(panel, "PathTracer", Mode3D.PathTracer, 53, 177, 100000, WorkingDirectory + "ConvergenceData_PT.txt"); 
            PixelConvergenceHelper.WritePixelDataToFile(panel, "BidirectionalPathTracing", Mode3D.BidirectionalPathTracing, 53, 177, 100000, WorkingDirectory + "ConvergenceData_BPT.txt");

            panel.Dispose();

            Bitmap actual = PixelConvergenceHelper.GetConvergenceImageFromFiles(420, 328, new string[] { WorkingDirectory + "ConvergenceData_PT.txt", WorkingDirectory + "ConvergenceData_BPT.txt" });
            actual.Save(WorkingDirectory + "Convergence_PT_vs_BPT.png", ImageFormat.Png);

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\Convergence_PT_vs_BPT_Expected.png");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, actual));
        }

        //Hinweis: Wenn du Fireflys mit der GetFullPathsFromSinglePixel-Funktion finden willst, dann erstelle zuerst im Kästchen-Modus (Mit PixelRange) ein Bild
        //Beim ImageCreatorPixel muss in der GetColorFromOnePixel das hier stehen: parm.Rand = new Rand((y - this.data.PixelRange.YStart) * this.data.PixelRange.Width + (x - this.data.PixelRange.XStart));
        //Damit wird dann GetFullPathsFromSinglePixel den gleichen Rand-Startwert nehmen
        [TestMethod]
        public void GetFullPathsFromSinglePixel()
        {
            GraphicPanel3D panel = new GraphicPanel3D() { Width = 420, Height = 328 };
            TestScenes.AddTestszene5_Cornellbox(panel);
            panel.Mode = Mode3D.BidirectionalPathTracing;
            string paths = panel.GetFullPathsFromSinglePixel(420, 328, null, 53, 177, 1);
            panel.Dispose();

            Assert.AreEqual("DirectLighting\tC D L\t0,2877586\t0,2889537\t0,9958642\tAreaLight\t3649809,15610635\r\nPixelColor with Gamma and Clampling=new Vector3D(0.56768024f, 0.321628004f, 0.321628004f)\tRGB=[144;82;82]\r\nRadiance-Sum(1)=new Vector3D(0.287758589f, 0.0824476853f, 0.0824476853f)\r\nDirectLighting-Sum(1)=new Vector3D(0.287758589f, 0.0824476853f, 0.0824476853f)\r\n", paths);
        }

        //So sehe ich für jeden Pathspace die Radiance
        [TestMethod]
        public void GetPathContributionsForSinglePixel()
        {
            GraphicPanel3D panel = new GraphicPanel3D() { Width = 420, Height = 328 };
            TestScenes.AddTestszene5_Cornellbox(panel);
            panel.Mode = Mode3D.BidirectionalPathTracing;
            string pathSpace = panel.GetPathContributionsForSinglePixel(420, 328, null, 53, 177, 1000);
            panel.Dispose();

            File.WriteAllText(WorkingDirectory + "PathSpace.txt", pathSpace);

            string pathSpacesum = GraphicPanel3D.GetSumOverallPathSpaces(WorkingDirectory + "PathSpace.txt");

            Assert.AreEqual("PixelColor=[164;96;91]\r\nSamples=1000\r\nPercentPerFullPathSampler\r\n\tDirectLighting=88% MinPdfA=8643,72610689691 MaxPdfA=3,9221416357948E+17\r\n\tVertexConnection=11% MinPdfA=10333,7902375661 MaxPdfA=1,93583127484964E+16\r\n\tPathTracing=0% MinPdfA=19761,0836569457 MaxPdfA=87917162498942,4\r\n\r\nC D L=[0,293949008;0,0842212886;0,0842212886]\r\nC D D L=[0,0218760502;0,0138365785;0,00626786193]\r\nC D D D L=[0,0216617119;0,00691924291;0,00477352086]\r\nC D S D L=[4,82501164E-05;4,82501164E-05;1,38244732E-05]\r\nC D D D D L=[0,0043059513;0,00223297998;0,000906560104]\r\nC D S S D L=[0,024622947;0,00705488818;0,00705488818]\r\nC D S D D L=[0,000515359279;5,31037258E-05;4,23068341E-05]\r\nC D D D S L=[7,61388874E-05;6,2503882E-06;6,2503882E-06]\r\nC D D D D D L=[0,00246201083;0,00112535805;0,000461485644]\r\nC D S S D D L=[0,000985086663;0,000289422751;0,000261209963]\r\nC D S S S D L=[0,00099947711;8,2048995E-05;8,2048995E-05]\r\nC D D S S D L=[0,000115668277;9,49543028E-06;9,49543028E-06]\r\nC D D D D D D L=[0,000776352827;0,000339412916;8,31282159E-05]\r\nC D S S D D D L=[0,00175228412;0,00054041011;0,000454074354]\r\nC D D D S S D L=[9,45420688E-05;1,18486223E-05;7,76113939E-06]\r\nC D D S S D D L=[1,65584188E-05;4,74426633E-06;4,74426633E-06]\r\nC D D D S D D L=[0,000148501305;3,64973348E-05;1,39123113E-05]\r\nC D S S D S D L=[0,000449505809;0,000128790998;0,000128790998]\r\nC D S D D D D L=[6,75693664E-05;1,58928049E-06;1,58928049E-06]\r\nC D D D D D D D L=[0,000524666102;0,000215966531;6,34987155E-05]\r\nC D S S D S S D L=[0,00366328005;0,000300725718;0,000300725718]\r\nC D S S D D D D L=[0,000332390831;0,000116335723;9,26895227E-05]\r\nC D D S S S D D L=[3,93416121E-05;1,12720372E-05;1,12720372E-05]\r\nC D D S S D S S L=[0,000554435595;0,000158855139;0,000158855139]\r\nC D D S S S D S L=[3,3891436E-05;3,3891436E-05;9,71046939E-06]\r\nC D D D S D D D L=[3,13762903E-05;8,98983762E-06;2,57573993E-06]\r\nC D S S S S S D L=[0,000616072677;0,000176515212;0,000176515212]\r\nC D D D D D D D D L=[0,000389566936;5,15110078E-05;2,50291359E-05]\r\nC D S S D D D D D L=[0,000158659022;8,13251099E-05;3,39138278E-05]\r\nC D S S D S S D D L=[0,000500546012;6,06664726E-05;4,10907778E-05]\r\nC D D S S D S S D L=[0,000245301315;3,04298374E-05;2,0137255E-05]\r\nC D D D S D D D D L=[1,17686923E-05;3,37192887E-06;9,66114612E-07]\r\nC D D D S S S D D L=[2,80159693E-05;2,29988473E-06;6,58955798E-07]\r\nC D S S D S S S D L=[0,000123407721;3,53583928E-05;3,53583928E-05]\r\nC D D S D D D D D L=[0,000160695417;1,08293989E-06;1,08293989E-06]", pathSpace);
            Assert.AreEqual("Sum=[0,382336408;0,118230797;0,105768807]\r\nPixelColor with Gamma and Clampling=[0,645955443;0,378890663;0,360185474]\tRGB=[164;96;91]\r\n", pathSpacesum);
        }

        //So vergleiche ich die PathSpace-Radiance-Werte von verschiedenen Verfahren
        [TestMethod]
        public void CompareManyPathSpaceFiles()
        {
            GraphicPanel3D panel = new GraphicPanel3D() { Width = 420, Height = 328 };
            TestScenes.AddTestszene5_Cornellbox(panel);
            panel.Mode = Mode3D.PathTracer;
            string pathSpacePT = panel.GetPathContributionsForSinglePixel(420, 328, null, 53, 177, 10000);

            panel.Mode = Mode3D.BidirectionalPathTracing;
            string pathSpaceBPT = panel.GetPathContributionsForSinglePixel(420, 328, null, 53, 177, 10000);
            panel.Dispose();

            File.WriteAllText(WorkingDirectory + "PathSpace_PT.txt", pathSpacePT);
            File.WriteAllText(WorkingDirectory + "PathSpace_BPT.txt", pathSpaceBPT);

            string allSpaces = GraphicPanel3D.CompareManyPathSpaceFiles(new[] { WorkingDirectory + "PathSpace_PT.txt", WorkingDirectory + "PathSpace_BPT.txt" }, true);
            string twoSpaces = GraphicPanel3D.CompareTwoPathSpaceFiles(WorkingDirectory + "PathSpace_PT.txt", WorkingDirectory + "PathSpace_BPT.txt");

            string allSpaces3Lines = string.Join("\n", allSpaces.Split(new[] { System.Environment.NewLine }, StringSplitOptions.None).Take(3));
            string twoSpaces3Lines = string.Join("\n", twoSpaces.Split(new[] { System.Environment.NewLine }, StringSplitOptions.None).Take(3));

            Assert.AreEqual("Space\tSpace0\tSpace1\tFactor1\nC D L\t0,2435042\t0,2954318\t1,213252\nC D D D L\t0,03380758\t0,02021929\t0,5980698", allSpaces3Lines);
            Assert.AreEqual("C D L=[0,243504167;0,0697680563;0,0697680563] <-> [0,295431823;0,0846461132;0,0846461132] Factor=[1,21325159;1,21325028;1,21325028]\nC D D D L=[0,0338075757;0,00968602952;0,0064345873] <-> [0,0202192888;0,00643555215;0,0043069073] Factor=[0,598069787;0,664415896;0,669336975]\nC D S S D L=[0,0139150154;0,0039868867;0,0039868867] <-> [0,0222625025;0,00637858175;0,00637858175] Factor=[1,59989059;1,59989035;1,59989035]", twoSpaces3Lines);
        }

        //Es wird der Helligkeitswert von lauter zufällig ausgewählten Pixeln genommen und dann der Median daraus ermittelt um somit
        //ein Helligkeitsfaktor zu bekommen, so dass das Bild nicht all zu hell/dunkel ist (Kann man gut nehmen um Lichtquellen einzustellen)
        [TestMethod]
        public void GetBrightnessFactor()
        {
            GraphicPanel3D panel = new GraphicPanel3D() { Width = 420, Height = 328 };
            TestScenes.AddTestszene5_Cornellbox(panel);
            panel.Mode = Mode3D.BidirectionalPathTracing;
            panel.GlobalSettings.BrightnessFactor = panel.GetBrightnessFactor(panel.Width, panel.Height);
            panel.Dispose();

            Assert.AreEqual(2.293298f, panel.GlobalSettings.BrightnessFactor);
        }
    }
}
