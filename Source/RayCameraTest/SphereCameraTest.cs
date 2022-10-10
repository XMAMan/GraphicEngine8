using GraphicGlobal;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfHistogram;
using RayCameraNamespace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayCameraTestNamespace
{
    [TestClass]
    public class SphereCameraTest
    {
        [TestMethod]
        public void GetPixelPositionFromEyePoint_CalledMultipeTimes_MatchWithGivenXY()
        {
            int screenSize = 5;
            var sut = new SphereCamera(new CameraConstructorData() { Camera = new Camera(360), ScreenWidth = screenSize, ScreenHeight = screenSize, PixelRange = new ImagePixelRange(0, 0, screenSize, screenSize) });

            int sampleCount = 1000;
            IRandom rand = new Rand(0);

            for (int i=0;i< sampleCount;i++)
            {
                int x = rand.Next(screenSize);
                int y = rand.Next(screenSize);
                var ray = sut.CreatePrimaryRay(x, y, rand);

                var pix = sut.GetPixelPositionFromEyePoint(ray.Direction);

                Assert.AreEqual(x, (int)pix.X);
                Assert.AreEqual(y, (int)pix.Y);
            }

            
        }
    }
}
