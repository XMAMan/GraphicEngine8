using UnitTestHelper;
using GraphicMinimal;
using ImageCreator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaytracingColorEstimator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageCreatorTest
{
    //Erzeugt ein Bild (Alles oder PixelRange daraus) indem erst bis zur Hälfte gerendert wird und dann das halbe Bild geladen wird
    //um den Rest zu erzeugen. Wärend der Erstellung von der ersten Hälfe wird ständig zwischengespeichert
    [TestClass]
    public class ImageCreatorWithSaveTest
    {
        private readonly string DataDirectory = UnitTestHelper.FilePaths.DataDirectory;
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void GetImage_FullScreen_Frame()
        {
            //Erwartungshaltung hier bei ImageCreatorWithSave_StopAtHalf1.bmp: Die hälfte der SaveKästchen ist fertig
            CreateImageInTwoSteps((frameData) => new ImageCreatorFrame(new PixelEstimatorMock(), frameData));
        }

        [TestMethod]
        public void GetImage_FullScreen_Pixel()
        {
            //Erwartungshaltung hier bei ImageCreatorWithSave_StopAtHalf1.bmp: Die hälfte der SaveKästchen ist fertig. Eins von den SaveKästchen ist zur Hälfte fertig
            CreateImageInTwoSteps((frameData) => new ImageCreatorPixel(new PixelEstimatorMock(), frameData));
            //Thread.Sleep(1000); //Damit verhindere ich im nachfolgenden Test System.UnauthorizedAccessException: Der Zugriff auf den Pfad "0_0_100_100_Finish.dat" wurde verweigert.
        }

        [TestMethod]
        public void GetImage_PixelRange_Frame()
        {
            CreateImageInTwoSteps((frameData) => new ImageCreatorFrame(new PixelEstimatorMock(2), frameData), new ImagePixelRange(new Point(244, 205), new Point(281, 239)));
            //Thread.Sleep(1000);
        }

        [TestMethod]
        public void GetImage_PixelRange_Pixel()
        {
            //Erwartungshaltung hier bei ImageCreatorWithSave_StopAtHalf1.bmp: Manche ThreadPixel-Kästchen sind schwarz andere nicht
            CreateImageInTwoSteps((frameData) => new ImageCreatorPixel(new PixelEstimatorMock(2), frameData), new ImagePixelRange(new Point(244, 205), new Point(281, 239)));
        }
        //Erzeugt ein Bild indem es erst bis zur Hälft rendert und dabei immer den Zwischenstand abspeichert
        //Dann wird dieser Zwischenstand geladen und der Rest erzeugt
        private void CreateImageInTwoSteps(Func<RaytracingFrame3DData, IImageCreator> imageCreatorFactory, ImagePixelRange pixelRange = null)
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap fullImage = new Bitmap(testImagePath);

            string saveFolder = WorkingDirectory + "ImageCreatorSaveFolder";

            foreach (FileInfo file in new DirectoryInfo(saveFolder).GetFiles())
            {
                if (file.Name != ".gitkeep") file.Delete();
            }

            if (pixelRange == null) pixelRange = new ImagePixelRange(0, 0, fullImage.Width, fullImage.Height);

            Bitmap expected = new Bitmap(pixelRange.Width, pixelRange.Height);
            for (int x = 0; x < pixelRange.Width; x++)
                for (int y = 0; y < pixelRange.Height; y++)
                {
                    expected.SetPixel(x, y, fullImage.GetPixel(pixelRange.XStart + x, pixelRange.YStart + y));
                }

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, fullImage);
            frameData.PixelRange = pixelRange;

            ImageCreatorWithSave sut1 = new ImageCreatorWithSave(imageCreatorFactory(frameData), saveFolder, new Size(100, 100), RaytracerAutoSaveMode.SaveAreas, frameData.StopTrigger);

            //Stoppe in der Hälfte vom Bild
            List<float> progressList = new List<float>();
            var task = Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    float progress = sut1.Progress;
                    sut1.SaveToFolder();
                    progressList.Add(progress);
                    if (progress > 0.5f) break;
                }
                frameData.StopTrigger.Cancel();

            });
            var rawImageHalf = sut1.GetImage(frameData.PixelRange); //Erzeuges nur ein halbes Bild
            task.Wait();

            Assert.IsTrue(new DirectoryInfo(saveFolder).GetFiles().Count(x => x.Name.Contains("InWork")) <= 1);

            //Erzeuge die andere Hälfte
            frameData.StopTrigger = new CancellationTokenSource();
            ImageCreatorWithSave sut2 = new ImageCreatorWithSave(imageCreatorFactory(frameData), saveFolder, new Size(100, 100), RaytracerAutoSaveMode.SaveAreas, frameData.StopTrigger);
            var rawImage = sut2.GetImage(frameData.PixelRange);

            Assert.IsTrue(new DirectoryInfo(saveFolder).GetFiles().Count(x => x.Name.Contains("InWork")) == 0);

            var result = Tonemapping.GetImage(rawImage, TonemappingMethod.None);

            Tonemapping.GetImage(rawImageHalf, TonemappingMethod.None).Save(WorkingDirectory + "ImageCreatorWithSave_StopAtHalf1.bmp");
            result.Save(WorkingDirectory + "ImageCreatorWithSave_StopAtHalf2.bmp");

            //Erwartung: Alles ist das Mariobild außer der gelöschte Teil. Der ist vom neuen Bild.
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
            Assert.IsTrue(sut1.Progress > 0.1f && sut1.Progress < 0.9f);
            Assert.AreEqual(1, sut2.Progress);

            Thread.Sleep(1000);
        }
    }
}
