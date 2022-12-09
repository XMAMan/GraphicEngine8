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
            TestScenes.AddTestscene5_Cornellbox(panel);
            panel.Mode = Mode3D.BidirectionalPathTracing;            
            string color = panel.GetColorFromSinglePixel(420, 328, null, 53, 177, 10000).ToShortString();
            panel.Dispose();

            Assert.AreEqual("[97;29;26]", color); //BPT
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
            TestScenes.AddTestscene5_Cornellbox(panel);

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
            TestScenes.AddTestscene5_Cornellbox(panel);
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
            TestScenes.AddTestscene5_Cornellbox(panel);
            panel.Mode = Mode3D.BidirectionalPathTracing;
            string pathSpace = panel.GetPathContributionsForSinglePixel(420, 328, null, 53, 177, 1000);
            panel.Dispose();

            File.WriteAllText(WorkingDirectory + "PathSpace.txt", pathSpace);

            string pathSpacesum = GraphicPanel3D.GetSumOverallPathSpaces(WorkingDirectory + "PathSpace.txt");

            Assert.AreEqual("PixelColor=[164;96;92]\r\nSamples=1000\r\nPercentPerFullPathSampler\r\n\tDirectLighting=87% MinPdfA=2933,14120697176 MaxPdfA=3,90733906908106E+17\r\n\tVertexConnection=12% MinPdfA=1120,92216560977 MaxPdfA=9,01882480365062E+16\r\n\tPathTracing=0% MinPdfA=10996,991794927 MaxPdfA=5022118,372503\r\n\r\nC D L=[0,295631826;0,0847034305;0,0847034305]\r\nC D D L=[0,0249442719;0,0145701999;0,00714695733]\r\nC D D D L=[0,0200315192;0,00688394159;0,00443151919]\r\nC D S D L=[0,000243744798;2,00094782E-05;2,00094782E-05]\r\nC D D D D L=[0,00467047794;0,00225919508;0,000931643299]\r\nC D S S D L=[0,0204512421;0,00585962506;0,00585962506]\r\nC D D S D L=[3,43383726E-05;9,83852351E-06;2,81890289E-06]\r\nC D S D D L=[0,00104097219;0,000298256113;0,000298256113]\r\nC D D D S L=[0,000411819958;3,9662511E-05;3,38070895E-05]\r\nC D D D D D L=[0,00266963802;0,000907016103;0,000361990416]\r\nC D D D S D L=[0,00010278132;8,43751604E-06;8,43751604E-06]\r\nC D S S D D L=[0,000930863782;0,000191299259;0,000161227756]\r\nC D D S S S L=[0,000269941054;7,73426582E-05;7,73426582E-05]\r\nC D S D S D L=[0,000204350814;4,80647759E-06;4,80647759E-06]\r\nC D D D D S L=[1,04511118E-05;2,99442013E-06;8,57951818E-07]\r\nC D D D D D D L=[0,00110806571;0,000402351201;0,000140852091]\r\nC D S S D D D L=[0,00150748435;0,000383631093;0,000361057726]\r\nC D D D S S S L=[7,33508059E-05;2,10162434E-05;2,10162434E-05]\r\nC D D S S S D L=[6,51894079E-05;1,86778652E-05;1,86778652E-05]\r\nC D D D S S D L=[0,000163797769;1,34464708E-05;1,34464708E-05]\r\nC D D D D S D L=[1,97267545E-05;1,97267545E-05;5,652048E-06]\r\nC D S S S D D L=[0,000815070583;0,000245584408;0,000233531464]\r\nC D D D D S S L=[5,27794436E-05;4,33276591E-06;4,33276591E-06]\r\nC D S S D S S D L=[0,00184539205;0,000151491768;0,000151491768]\r\nC D D S S D S S L=[0,00197927631;0,000567096169;0,000567096169]\r\nC D D D D D D D L=[0,000200189534;0,000131818088;3,49441543E-05]\r\nC D S S D D D D L=[0,00033805269;7,42390912E-05;5,97112157E-05]\r\nC D S S S D D D L=[0,000151742919;3,48036992E-05;1,7433671E-05]\r\nC D S S D S S D D L=[0,000732676766;0,000175973619;0,000160445794]\r\nC D D D D D D D D L=[0,000130431101;2,78093867E-05;9,09374376E-06]\r\nC D D D S S S D D L=[2,15596192E-05;1,76987055E-06;5,07097695E-07]\r\nC D S S D D S S S L=[0,0014133486;0,000404948194;0,000404948194]\r\nC D D D S S D D D L=[0,000133646376;1,09712864E-05;1,09712864E-05]\r\nC D S S D D D D D L=[4,13391317E-05;1,16338006E-05;5,22876053E-06]\r\nC D S D S D S S D L=[0,000449795829;1,05795234E-05;1,05795234E-05]\r\nC D S S S D D D D L=[4,08840897E-05;1,17139825E-05;3,35625327E-06]", pathSpace);
            Assert.AreEqual("Sum=[0,382932037;0,118559688;0,106277116]\r\nPixelColor with Gamma and Clampling=[0,646412671;0,379369408;0,360971242]\tRGB=[164;96;92]\r\n", pathSpacesum);
        }

        //So vergleiche ich die PathSpace-Radiance-Werte von verschiedenen Verfahren
        [TestMethod]
        public void CompareManyPathSpaceFiles()
        {
            GraphicPanel3D panel = new GraphicPanel3D() { Width = 420, Height = 328 };
            TestScenes.AddTestscene5_Cornellbox(panel);
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

            Assert.AreEqual("Space\tSpace0\tSpace1\tFactor1\nC D L\t0,222641\t0,2952552\t1,326149\nC D D D L\t0,02485633\t0,01988139\t0,7998522", allSpaces3Lines);
            Assert.AreEqual("C D L=[0,222641036;0,0637904108;0,0637904108] <-> [0,295255184;0,0845956132;0,0845956132] Factor=[1,32614899;1,32614934;1,32614934]\nC D D D L=[0,0248563327;0,00569922198;0,00387000735] <-> [0,0198813919;0,00632043835;0,00420032023] Factor=[0,799852192;1,10900021;1,08535194]\nC D S S D L=[0,0139150303;0,00398689136;0,00398689136] <-> [0,0234292913;0,0067128907;0,0067128907] Factor=[1,6837399;1,68374062;1,68374062]", twoSpaces3Lines);
        }

        //Es wird der Helligkeitswert von lauter zufällig ausgewählten Pixeln genommen und dann der Median daraus ermittelt um somit
        //ein Helligkeitsfaktor zu bekommen, so dass das Bild nicht all zu hell/dunkel ist (Kann man gut nehmen um Lichtquellen einzustellen)
        [TestMethod]
        public void GetBrightnessFactor()
        {
            GraphicPanel3D panel = new GraphicPanel3D() { Width = 420, Height = 328 };
            TestScenes.AddTestscene5_Cornellbox(panel);
            panel.Mode = Mode3D.BidirectionalPathTracing;
            panel.GlobalSettings.BrightnessFactor = panel.GetBrightnessFactor(panel.Width, panel.Height);
            panel.Dispose();

            Assert.AreEqual(2.023748f, panel.GlobalSettings.BrightnessFactor);
        }
    }
}
