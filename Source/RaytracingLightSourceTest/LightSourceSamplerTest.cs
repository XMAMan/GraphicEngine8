using System;
using System.IO;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaytracingLightSourceTest.Fullpathsampling;

namespace RaytracingLightSourceTest
{
    enum SamplingMethod { LightsourceSampling, BrdfSampling, Photonmapping, Lighttracing }
    


    [TestClass]
    public class LightSourceSamplerTest
    {
        private readonly float maxError = 0.1f;

        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;




        [TestMethod]
        public void CreateLightSourceImage()
        {
            //new LocalIlluminationComparer().CreateImageForAllLightsourceTypes(100).Save(WorkingDirectory + "LightSourceSamplerTest.bmp");
            //new LocalIlluminationComparer().CreateImageForSingleLightsourceType(LightsourceType.ImportanceSurface, 100).Save(WorkingDirectory + "SingleLightSourceType.bmp");
            //new LocalIlluminationComparer().CreateImage(new TestSzene(LightsourceType.ImportanceSphere, 100), 10, SamplingMethod.BrdfSampling).Save(WorkingDirectory + "SingleMethod.bmp");

            Vector3D color1 = new LocalIlluminationComparer().GetSinglePixel(new TestSzene(LightsourceType.SphereWithSpot, 1), 10000, SamplingMethod.LightsourceSampling, 0, 0);
            Vector3D color2 = new LocalIlluminationComparer().GetSinglePixel(new TestSzene(LightsourceType.SphereWithSpot, 1), 10000, SamplingMethod.BrdfSampling, 0, 0);
            Vector3D color3 = new LocalIlluminationComparer().GetSinglePixel(new TestSzene(LightsourceType.SphereWithSpot, 1), 10000, SamplingMethod.Lighttracing, 0, 0);
            File.WriteAllText(WorkingDirectory + "PixelTest.txt", "LightsourceSampling=" + color1 + "\r\nBrdfSampling=" + color2 + "\r\nLighttracing=" + color3);
        }

        [TestMethod]
        public void CompareBrdfWithLightsourceSampling_Surface()
        {
            Assert.IsTrue(GetPixelError(LightsourceType.Surface));
        }

        [TestMethod]
        public void CompareBrdfWithLightsourceSampling_Sphere()
        {
            Assert.IsTrue(GetPixelError(LightsourceType.Sphere));
        }

        [TestMethod]
        [Ignore]
        public void CompareBrdfWithLightsourceSampling_SphereWithSpot()
        {
            Assert.IsTrue(GetPixelError(LightsourceType.SphereWithSpot));
        }

        [TestMethod]
        public void CompareBrdfWithLightsourceSampling_SurfaceWithSpot()
        {
            Assert.IsTrue(GetPixelError(LightsourceType.SurfaceWithSpot));
        }

        [TestMethod]
        public void CompareBrdfWithLightsourceSampling_ImportanceSurface()
        {
            Assert.IsTrue(GetPixelError(LightsourceType.ImportanceSurface));
        }

        private bool GetPixelError(LightsourceType lightsourceType)
        {
            var data = new TestSzene(lightsourceType, 100);
            int x = 50, y = 50;
            Vector3D color1 = new LocalIlluminationComparer().GetSinglePixel(data, 100000, SamplingMethod.LightsourceSampling, x, y);
            Vector3D color2 = new LocalIlluminationComparer().GetSinglePixel(data, 100000, SamplingMethod.BrdfSampling, x, y);
            Vector3D color3 = new LocalIlluminationComparer().GetSinglePixel(data, 1000000, SamplingMethod.Lighttracing, x, y);
            return Math.Abs(color1.X - color2.X) < maxError && Math.Abs(color2.X - color3.X) < maxError * 10;
        }
    }
    
}
