using GraphicMinimal;
using ImageCreator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaytracingColorEstimator;
using System;
using System.Drawing;
using System.Threading;

namespace ImageCreatorTest
{
    [TestClass]
    public class ImageCreatorFrameTest
    {
        private readonly string DataDirectory = UnitTestHelper.FilePaths.DataDirectory;
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void GetImage_FullScreen()
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap expected = new Bitmap(testImagePath);

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, expected);

            ImageCreatorFrame sut = new ImageCreatorFrame(new PixelEstimatorMock(), frameData);
            var rawImage = sut.GetImage(frameData.PixelRange);
            var result = Tonemapping.GetImage(rawImage, TonemappingMethod.None);

            result.Save(WorkingDirectory + "ImageCreatorFrame_All.bmp");

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

            ImageCreatorFrame sut = new ImageCreatorFrame(new PixelEstimatorMock(), frameData);
            var rawImage = sut.GetImage(frameData.PixelRange);
            var result = Tonemapping.GetImage(rawImage, TonemappingMethod.None);

            result.Save(WorkingDirectory + "ImageCreatorFrame_Range.bmp");

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

            ImageCreatorFrame sut1 = new ImageCreatorFrame(new PixelEstimatorMock(frameData.StopTrigger, expected.Width * expected.Height / 2), frameData);
            sut1.GetImage(frameData.PixelRange);
            //Assert.IsNull(rawImageHalf); //ALle Pixel vom rawImageHalf sind schwarz

            Assert.IsTrue(Math.Abs(0.5f - sut1.Progress) < 0.01f);

            frameData.StopTrigger = new CancellationTokenSource();
            ImageCreatorFrame sut2 = new ImageCreatorFrame(new PixelEstimatorMock(), frameData);
            var rawImageResumed = sut2.GetImageFromInitialData(sut1.GetImageBufferSum(), frameData.PixelRange);


            var resultResumed = Tonemapping.GetImage(rawImageResumed, TonemappingMethod.None);

            resultResumed.Save(WorkingDirectory + "ImageCreatorFrame_Resumed.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, resultResumed));
        }

        [TestMethod]
        public void GetImage_ThrowsException()
        {
            string testImagePath = DataDirectory + "nes_super_mario_bros.png";
            Bitmap expected = new Bitmap(testImagePath);

            RaytracingFrame3DData frameData = TestHelper.GetFrameData(testImagePath, expected);
            frameData.GlobalObjektPropertys.SamplingCount = 2;

            Exception catchedException = null;
            ImageCreatorFrame sut = new ImageCreatorFrame(new PixelEstimatorMock(new Exception("Fehler")), frameData);
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
