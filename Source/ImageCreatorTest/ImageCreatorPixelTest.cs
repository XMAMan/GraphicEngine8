using GraphicMinimal;
using ImageCreator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaytracingColorEstimator;
using System;
using System.Drawing;
using System.Threading;
using UnitTestHelper;

namespace ImageCreatorTest
{
    [TestClass]
    public class ImageCreatorPixelTest
    {
        private readonly string DataDirectory = UnitTestHelper.FilePaths.DataDirectory;
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void GetImage_FullScreen()
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap expected = new Bitmap(testImagePath);

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, expected);

            ImageCreatorPixel sut = new ImageCreatorPixel(new PixelEstimatorMock(), frameData);
            var rawImage = sut.GetImage(frameData.PixelRange);
            var result = Tonemapping.GetImage(rawImage, TonemappingMethod.None);

            result.Save(WorkingDirectory + "ImageCreatorPixel_All.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
            Assert.AreEqual(1, sut.Progress);
        }

        [TestMethod]
        public void GetImage_PixelRange()
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap all = new Bitmap(testImagePath);

            ImagePixelRange pixelRange = new ImagePixelRange(new Point(244, 205), new Point(281, 239));
            Bitmap expected = all.GetPixelRangeFromBitmap(pixelRange);

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, all);
            frameData.PixelRange = pixelRange;

            ImageCreatorPixel sut = new ImageCreatorPixel(new PixelEstimatorMock(), frameData);
            var rawImage = sut.GetImage(frameData.PixelRange);
            var result = Tonemapping.GetImage(rawImage, TonemappingMethod.None);

            result.Save(WorkingDirectory + "ImageCreatorPixel_Range.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
            Assert.AreEqual(1, sut.Progress);
        }

        [TestMethod]
        public void GetImage_StopAtHalf()
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap expected = new Bitmap(testImagePath);

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, expected);
            frameData.GlobalObjektPropertys.ThreadCount = 1;

            ImageCreatorPixel sut1 = new ImageCreatorPixel(new PixelEstimatorMock(frameData.StopTrigger, expected.Width * expected.Height / 2), frameData);
            var rawImageHalf = sut1.GetImage(frameData.PixelRange);
            var resultHalf = Tonemapping.GetImage(rawImageHalf, TonemappingMethod.None);

            Assert.IsTrue(Math.Abs(0.5f - sut1.Progress) < 0.01f);

            frameData.StopTrigger = new CancellationTokenSource();
            ImageCreatorPixel sut2 = new ImageCreatorPixel(new PixelEstimatorMock(), frameData);
            var rawImageResumed = sut2.GetImageFromInitialData(sut1.GetProgressImage(), frameData.PixelRange);

            
            var resultResumed = Tonemapping.GetImage(rawImageResumed, TonemappingMethod.None);

            resultHalf.Save(WorkingDirectory + "ImageCreatorPixel_Half.bmp");
            resultResumed.Save(WorkingDirectory + "ImageCreatorPixel_Resumed.bmp");

            int blackColorCounter = TestHelper.GetBlackPixelCount(resultHalf);
            Assert.IsTrue(Math.Abs(expected.Width * expected.Height / 2- blackColorCounter) < 5000, "Differenze:" + Math.Abs(expected.Width * expected.Height / 2 - blackColorCounter));

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, resultResumed));
        }

        [TestMethod]
        public void GetImage_ThrowsException()
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap expected = new Bitmap(testImagePath);

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, expected);

            Exception catchedException = null;
            ImageCreatorPixel sut = new ImageCreatorPixel(new PixelEstimatorMock(new Exception("Fehler")), frameData);
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
