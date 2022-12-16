using UnitTestHelper;
using GraphicGlobal;
using GraphicMinimal;
using ImageCreator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaytracingColorEstimator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageCreatorTest
{
    [TestClass]
    public class MasterImageCreatorTest
    {
        private readonly string DataDirectory = UnitTestHelper.FilePaths.DataDirectory;
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        //Untersucht, um wie viel die ImageCreatorWithSave langsamer gegenüber ImageCreatorPixel/ImageCreatorFrame ist
        //Ergebnis: Der ImageCreatorWithSave ist nicht merklich langsamer und bringt kein overhead mit)
        [TestMethod]
        [Ignore] //Dient nur zur Ermittlung der Rechenzeiten um sie vergleichen zu können
        public void SaveCompareTime()
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
 
            Size screenSize = new Size(1920 / 5, 1017 / 5);

            RaytracingFrame3DData frameData = new RaytracingFrame3DData()
            {
                GlobalObjektPropertys = new GlobalObjectPropertys()
                {
                    ThreadCount = 3,
                    SamplingCount = 10,
                    BackgroundImage = testImagePath,
                },
                PixelRange = new ImagePixelRange(0, 0, screenSize.Width, screenSize.Height),
                ScreenWidth = screenSize.Width,
                ScreenHeight = screenSize.Height,
                StopTrigger = new CancellationTokenSource()
            };

           
            ImageCreatorFrame sut1 = new ImageCreatorFrame(new PixelEstimatorMock(), frameData);
            
            sut1.GetImage(frameData.PixelRange);    //rawImage1 = 15444 ms
            DateTime start1 = DateTime.Now;
            sut1.GetProgressImage();                //16,018 ms
            double time1 = (DateTime.Now - start1).TotalMilliseconds;

            ImageCreatorPixel sut2 = new ImageCreatorPixel(new PixelEstimatorMock(), frameData);
            
            sut2.GetImage(frameData.PixelRange);    //rawImage2 = 11664 ms
            DateTime start2 = DateTime.Now;
            sut2.GetProgressImage();                //0,9996 ms
            double time2 = (DateTime.Now - start2).TotalMilliseconds;


            string saveFolder = WorkingDirectory + "ImageCreatorSaveFolder";
            foreach (FileInfo file in new DirectoryInfo(saveFolder).GetFiles())
            {
                file.Delete();
            }

            saveFolder = "";
            ImageCreatorWithSave sut3 = new ImageCreatorWithSave(new ImageCreatorPixel(new PixelEstimatorMock(), frameData), saveFolder, screenSize, RaytracerAutoSaveMode.Disabled, frameData.StopTrigger);
            
            sut3.GetImage(frameData.PixelRange);    //rawImage3 = 11550 ms
            DateTime start3 = DateTime.Now;
            sut3.GetProgressImage();                //5,0065 ms
            double time3 = (DateTime.Now - start3).TotalMilliseconds;

            throw new Exception($"Time1={time1}, Time2={time2}, Time3={time3}");
        }

        [TestMethod]
        public void GetImage_FullScreen_Frame()
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap expected = new Bitmap(testImagePath);

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, expected);

            MasterImageCreator sut = MasterImageCreator.CreateInFrameMode(new PixelEstimatorMock() as IFrameEstimator, frameData);
            var rawImage = sut.GetImage(frameData.PixelRange);
            var result = Tonemapping.GetImage(rawImage, TonemappingMethod.None);

            result.Save(WorkingDirectory + "MasterImageCreatorFrame_All.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
            Assert.AreEqual(1, sut.Progress);
        }

        [TestMethod]
        public void GetImage_FullScreen_Pixel()
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap expected = new Bitmap(testImagePath);

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, expected);

            MasterImageCreator sut = MasterImageCreator.CreateInPixelMode(new PixelEstimatorMock() as IPixelEstimator, frameData);
            var rawImage = sut.GetImage(frameData.PixelRange);
            var result = Tonemapping.GetImage(rawImage, TonemappingMethod.None);

            result.Save(WorkingDirectory + "MasterImageCreatorPixel_All.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
            Assert.AreEqual(1, sut.Progress);
        }

        [TestMethod]
        public void GetImage_PixelRange_Frame()
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap all = new Bitmap(testImagePath);

            ImagePixelRange pixelRange = new ImagePixelRange(new Point(244, 205), new Point(281, 239));
            Bitmap expected = all.GetPixelRangeFromBitmap(pixelRange);

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, all);
            frameData.PixelRange = pixelRange;

            MasterImageCreator sut = MasterImageCreator.CreateInFrameMode(new PixelEstimatorMock() as IFrameEstimator, frameData);
            var rawImage = sut.GetImage(frameData.PixelRange);
            var result = Tonemapping.GetImage(rawImage, TonemappingMethod.None);

            result.Save(WorkingDirectory + "MasterImageCreatorFrame_Range.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
            Assert.AreEqual(1, sut.Progress);
        }

        [TestMethod]
        public void GetImage_PixelRange_Pixel()
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap all = new Bitmap(testImagePath);

            ImagePixelRange pixelRange = new ImagePixelRange(new Point(244, 205), new Point(281, 239));
            Bitmap expected = all.GetPixelRangeFromBitmap(pixelRange);

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, all);
            frameData.PixelRange = pixelRange;

            MasterImageCreator sut = MasterImageCreator.CreateInPixelMode(new PixelEstimatorMock() as IPixelEstimator, frameData);
            var rawImage = sut.GetImage(frameData.PixelRange);
            var result = Tonemapping.GetImage(rawImage, TonemappingMethod.None);

            result.Save(WorkingDirectory + "MasterImageCreatorFrame_Pixel.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
            Assert.AreEqual(1, sut.Progress);
        }

        [TestMethod]
        public void GetImage_StopAtHalf_Frame()
        {
            CreateImageInTwoSteps(useFrameMode:true);
        }

        [TestMethod]
        public void GetImage_StopAtHalf_Pixel()
        {
            CreateImageInTwoSteps(useFrameMode: false);
        }

        //Erzeugt ein Bild indem es erst bis zur Hälft rendert und dabei immer den Zwischenstand abspeichert
        //Dann wird dieser Zwischenstand geladen und der Rest erzeugt
        private void CreateImageInTwoSteps(bool useFrameMode)
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap expected = new Bitmap(testImagePath);

            string saveFolder = WorkingDirectory + "ImageCreatorSaveFolder";

            foreach (FileInfo file in new DirectoryInfo(saveFolder).GetFiles())
            {
                if (file.Name != ".gitkeep")
                    file.Delete();
            }

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, expected);
            frameData.GlobalObjektPropertys.SaveFolder = saveFolder;
            frameData.GlobalObjektPropertys.AutoSaveMode = RaytracerAutoSaveMode.SaveAreas;

            MasterImageCreator sut1 = useFrameMode ? MasterImageCreator.CreateInFrameMode(new PixelEstimatorMock(), frameData) : MasterImageCreator.CreateInPixelMode(new PixelEstimatorMock(), frameData);
            
            //Stoppe in der Hälfte vom Bild
            List<float> progressList = new List<float>();
            float lastProgressFromStep1 = -1;
            var task1 = Task.Run(() =>
            {
                float progress = -1;
                while (true)
                {
                    Thread.Sleep(10);
                    progress = sut1.Progress;
                    sut1.SaveToFolder();
                    if (sut1.State == RaytracerWorkingState.InWork) progressList.Add(progress);
                    if (progress > 0.6f) break;
                }
                lastProgressFromStep1 = progress;
                sut1.StopRaytracing();

            });
            var rawImageHalf = sut1.GetImage(frameData.PixelRange); //Erzeuges nur ein halbes Bild
            task1.Wait();

            Assert.IsTrue(new DirectoryInfo(saveFolder).GetFiles().Count(x => x.Name.Contains("InWork")) <= 1);
            //return;
            //Erzeuge die andere Hälfte
            frameData.StopTrigger = new CancellationTokenSource();
            MasterImageCreator sut2 = useFrameMode ? MasterImageCreator.CreateInFrameMode(new PixelEstimatorMock(), frameData) : MasterImageCreator.CreateInPixelMode(new PixelEstimatorMock(), frameData);

            bool isFinish = false;
            float firstProgressFromStep2 = -1;
            var task2 = Task.Run(() =>
            {
                while (isFinish == false)
                {
                    Thread.Sleep(10);
                    float progress = sut2.Progress;
                    if (sut2.State == RaytracerWorkingState.InWork)
                    {
                        if (firstProgressFromStep2 == -1) firstProgressFromStep2 = progress;
                        progressList.Add(progress);
                    }
                }
            });

            var rawImage = sut2.GetImage(frameData.PixelRange);
            isFinish = true;

            var result = Tonemapping.GetImage(rawImage, TonemappingMethod.None);

            Tonemapping.GetImage(rawImageHalf, TonemappingMethod.None).Save(WorkingDirectory + "MasterImageCreatorWithSave_StopAtHalf1.bmp");
            result.Save(WorkingDirectory + "MasterImageCreatorWithSave_StopAtHalf2.bmp");

            //Erwartung: Alles ist das Mariobild außer der gelöschte Teil. Der ist vom neuen Bild.
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
            Assert.IsTrue(sut1.Progress > 0.1f && sut1.Progress < 0.999f, $"sut1.Progress={sut1.Progress}");
            Assert.AreEqual(1, sut2.Progress);
        }


        [TestMethod]
        public void GetImage_ThrowsException_Frame()
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap expected = new Bitmap(testImagePath);

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, expected);
            frameData.GlobalObjektPropertys.SamplingCount = 2;

            Exception catchedException = null;
            MasterImageCreator sut = MasterImageCreator.CreateInFrameMode(new PixelEstimatorMock(new Exception("Fehler")), frameData);
            try
            {
                var rawImage = sut.GetImage(frameData.PixelRange);
                var result = Tonemapping.GetImage(rawImage, TonemappingMethod.None);
            }
            catch (Exception ex)
            {
                catchedException = ex;
            }
            Assert.IsNotNull(catchedException);
            Assert.IsTrue(catchedException.Message.Contains("Fehler"));

            string debugString = RaytracingDebuggingData.ExtractDebuggingString(catchedException.Message);

            try
            {
                sut.GetColorFromSinglePixelForDebuggingPurpose(RaytracingDebuggingData.CreateFromXmlString(debugString.Replace("\\", "")));
            }
            catch (Exception ex)
            {
                catchedException = ex;
            }

            Assert.IsNotNull(catchedException);
            Assert.IsTrue(catchedException.Message.Contains("Fehler"));
        }

        [TestMethod]
        public void GetImage_ThrowsException_Pixel()
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap expected = new Bitmap(testImagePath);

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, expected);

            Exception catchedException = null;
            MasterImageCreator sut = MasterImageCreator.CreateInPixelMode(new PixelEstimatorMock(new Exception("Fehler")), frameData);
            try
            {
                var rawImage = sut.GetImage(frameData.PixelRange);
                var result = Tonemapping.GetImage(rawImage, TonemappingMethod.None);
            }
            catch (Exception ex)
            {
                catchedException = ex;
            }
            Assert.IsNotNull(catchedException);
            Assert.IsTrue(catchedException.Message.Contains("Fehler"));

            string debugString = RaytracingDebuggingData.ExtractDebuggingString(catchedException.Message);

            try
            {
                sut.GetColorFromSinglePixelForDebuggingPurpose(RaytracingDebuggingData.CreateFromXmlString(debugString.Replace("\\", "")));
            }
            catch (Exception ex)
            {
                catchedException = ex;
            }

            Assert.IsNotNull(catchedException);
            Assert.IsTrue(catchedException.Message.Contains("Fehler"));
        }
    }
}
