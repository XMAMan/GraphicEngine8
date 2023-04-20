using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using System.IO;
using UnitTestHelper;

namespace ToolsTest
{
    [TestClass]
    public class CreateScenesBatFilesTest
    {
        //1  Sample pro Bild (Größe / 10): 4,6 Minuten
        //1  Sample pro Bild (Größe / 5):  8,4 Minuten
        //10 Sample pro Bild (Größe / 10): 13,3 Minuten (Sieht von der Qualität nicht sonderlich besser aus)
        private static readonly string BatchToolDirectory = UnitTestHelper.FilePaths.BatchToolDirectory;
        private static readonly string ScenesDirectory = UnitTestHelper.FilePaths.ScenesDirectory;
        private static readonly string ExpectedImages = UnitTestHelper.FilePaths.ExpectedImages;
        private static readonly string OutputFolder = UnitTestHelper.FilePaths.OutputFolder;


        [ClassInitialize]  // Executes once for the test class. (Optional) 
        public static void CreateDeployPackage(TestContext context)
        {
            //1. Wenn schon vorhanden lösche vorher den ExeFolder
            if (Directory.Exists(BatchToolDirectory + "\\ExeFolder"))
            {
                Directory.Delete(BatchToolDirectory + "\\ExeFolder", true);
            }

            //2. Erstelle den ExeFolder neu
            string cmdOutput = CmdHelper.RunCommand(BatchToolDirectory, "CreateDeployPackage.bat NoZip");

            //3. Gehe durch all Bat-Dateien und skaliere die Bildgröße und Samplezahl runter
            foreach (var file in Directory.EnumerateFiles(ScenesDirectory, "*.bat"))
            {
                File.WriteAllText(file, File.ReadAllText(file)
                    .Replace("-closeWindowAfterRendering false", "")    //Fenster soll am Ende automatisch geschlossen werden
                    .Replace("-saveFolder AutoSave", "")                //Entferne AutoSave
                    );  

                if (new FileInfo(file).Name.StartsWith("01_"))
                {
                    
                    continue; //Die 01-Scene ist schnell genug und muss nicht kleiner gemacht werden
                }

                string content = File.ReadAllText(file);

                string newContent = content;
                newContent = newContent.ReplaceInteger(@"-width\s+(\d+)", (x) => x / 10); //-width 420  -> -width 42
                newContent = newContent.ReplaceInteger(@"-height\s+(\d+)", (x) => x / 10);//-height 328 -> -height 32
                newContent = newContent.ReplaceInteger(@"-pixelRange\s+\[(\d+);(\d+);(\d+);(\d+)\]", (x) => x / 10); //-pixelRange [34;144;101;250] -> -pixelRange [3;14;10;25]
                newContent = newContent.ReplaceInteger(@"-sampleCount\s+(\d+)", (x) => 1);//-sampleCount 1000 -> -sampleCount 1
                newContent = newContent.ReplaceFloat(@"-radiosityMaxAreaPerPatch\s+(\d+,\d+)", (x) => 0.1f);//-radiosityMaxAreaPerPatch 0,003 -> -radiosityMaxAreaPerPatch 0,1
                newContent = newContent.Replace("-saveFolder AutoSave", ""); //Entferne AutoSave

                File.WriteAllText(file, newContent);
            }
        }

        [ClassCleanup] // Runs once after all tests in this class are executed. (Optional)
        public static void TestFixtureTearDown()
        {
            if (Directory.Exists(BatchToolDirectory + "\\ExeFolder"))
            {
                Directory.Delete(BatchToolDirectory + "\\ExeFolder", true);
            }
        }

        private void RunTestWithExactCompare(string file)
        {
            CmdHelper.RunCommand(ScenesDirectory, $"{file}.bat");

            if (File.Exists(OutputFolder + $"{file}.jpg")) File.Delete(OutputFolder + $"{file}.jpg");
            File.Move(ScenesDirectory + $"{file}.jpg", OutputFolder + $"{file}.jpg");

            Bitmap expected = new Bitmap(ExpectedImages + $"{file}.jpg");
            Bitmap actual = new Bitmap(OutputFolder + $"{file}.jpg");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, actual));
        }

        private void RunTestWithApproximatelyCompare(string file, int maxError = 100)
        {
            CmdHelper.RunCommand(ScenesDirectory, $"{file}.bat");

            if (File.Exists(OutputFolder + $"{file}.jpg")) File.Delete(OutputFolder + $"{file}.jpg");
            File.Move(ScenesDirectory + $"{file}.jpg", OutputFolder + $"{file}.jpg");

            Bitmap expected = new Bitmap(ExpectedImages + $"{file}.jpg");
            Bitmap actual = new Bitmap(OutputFolder + $"{file}.jpg");

            var diff = DifferenceImageCreator.GetDifferenceImage(expected, actual);
            //diff.Image.Save(OutputFolder + "Error.bmp");
            //File.AppendAllText(OutputFolder + "Diffs.txt", file + "\n" +diff.GetErros() +"\n"); //Damit kann ich sehen, welche Errorwerte maximal so kommen
            Assert.IsTrue(diff.GetMaxError() < maxError, diff.GetMaxErrorWithName() + $" (Max Allowed Error={maxError})");
        }

        [TestMethod]
        public void Batfile_01_RingSphere_CPU()
        {
            RunTestWithExactCompare("01_RingSphere_CPU");
        }

        [TestMethod]
        public void Batfile_01_RingSphere_DirectX()
        {
            RunTestWithExactCompare("01_RingSphere_DirectX");
        }

        [TestMethod]
        public void Batfile_01_RingSphere_Raytracer()
        {
            RunTestWithExactCompare("01_RingSphere_Raytracer");
        }

        [TestMethod]
        public void Batfile_01_RingSphere_SubArea_CPU()
        {
            RunTestWithExactCompare("01_RingSphere_SubArea_CPU");
        }

        [TestMethod]
        public void Batfile_01_RingSphere_SubArea_Raytracer()
        {
            RunTestWithExactCompare("01_RingSphere_SubArea_Raytracer");
        }

        [TestMethod]
        public void Batfile_02_NoWindowRoom_RadiosityNoInterpolation()
        {
            RunTestWithApproximatelyCompare("02_NoWindowRoom_RadiosityNoInterpolation");
        }

        [TestMethod]
        public void Batfile_05_WaterCornellbox()
        {
            RunTestWithApproximatelyCompare("05_WaterCornellbox");
        }

        [TestMethod]
        public void Batfile_05_MirrorCornellbox()
        {
            RunTestWithApproximatelyCompare("05_MirrorCornellbox");
        }

        [TestMethod]
        public void Batfile_06_ChinaRoom()
        {
            RunTestWithApproximatelyCompare("06_ChinaRoom");
        }

        [TestMethod]
        public void Batfile_07_Chessboard()
        {
            RunTestWithApproximatelyCompare("07_Chessboard");
        }

        [TestMethod]
        public void Batfile_08_WindowRoom()
        {
            RunTestWithExactCompare("08_WindowRoom");
        }

        [TestMethod]
        public void Batfile_10_MirrorGlassCaustic()
        {
            RunTestWithApproximatelyCompare("10_MirrorGlassCaustic");
        }

        [TestMethod]
        public void Batfile_11_PillarsOffice()
        {
            RunTestWithApproximatelyCompare("11_PillarsOffice");
        }

        [TestMethod]
        public void Batfile_12_Snowman()
        {
            RunTestWithApproximatelyCompare("12_Snowman");
        }

        [TestMethod]
        public void Batfile_15_MicrofacetSphere()
        {
            RunTestWithApproximatelyCompare("15_MicrofacetSphere");
        }

        [TestMethod]
        public void Batfile_16_Graphic6Memories()
        {
            RunTestWithApproximatelyCompare("16_Graphic6Memories");
        }

        [TestMethod]
        public void Batfile_18_Clouds()
        {
            RunTestWithApproximatelyCompare("18_Clouds");
        }

        [TestMethod]
        public void Batfile_19_Stilllife()
        {
            RunTestWithApproximatelyCompare("19_Stilllife");
        }

        [TestMethod]
        public void Batfile_20_Mirrorballs()
        {
            RunTestWithApproximatelyCompare("20_Mirrorballs");
        }

        [TestMethod]
        public void Batfile_24_EnvironmentMaterialTest()
        {
            RunTestWithApproximatelyCompare("24_EnvironmentMaterialTest");
        }

        [TestMethod]
        public void Batfile_27_MirrorsEdge()
        {
            RunTestWithApproximatelyCompare("27_MirrorsEdge");
        }

        [TestMethod]
        public void Batfile_32_LivingRoom()
        {
            RunTestWithApproximatelyCompare("32_LivingRoom");
        }
    }
}
