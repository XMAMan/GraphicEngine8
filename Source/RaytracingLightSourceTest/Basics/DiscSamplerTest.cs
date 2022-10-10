using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphicGlobal;
using GraphicGlobal.MathHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaytracingLightSource.Basics;

namespace RaytracingLightSourceTest.Basics
{
    [TestClass]
    public class DiscSamplerTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void SamplePointOnDisc_CalledMultipeTimes_PointPatternLooksEqualSampling()
        {
            int sampleCount = 1000;
            float radius = 3;
            Vector3D center = new Vector3D(5, 6, 7);
            DiscSampler sut = new DiscSampler(center, new Vector3D(0, -1, 0), radius);
            Random rand = new Random(0);

            int imageSize = 100;
            Bitmap image = new Bitmap(imageSize * 2, imageSize);

            for (int i=0;i<sampleCount;i++)
            {
                Vector3D point3D = sut.SamplePointOnDisc(rand.NextDouble(), rand.NextDouble()) - center;
                Vector2D point2D1 = (new Vector2D(point3D.X, point3D.Z) / (2 * radius) + new Vector2D(0.5f, 0.5f)) * imageSize;
                SetPixel(image, point2D1);
            }

            int sampleCountExtra = (int)(sampleCount * 1.0 / (Math.PI / 4)); //Sorge dafür, das beim Rechteck-Equal-Sampling im Kreis stastisch genau so viele Punkte sind, wie beim Kreis-Direkt-Sampling

            for (int i = 0; i < sampleCountExtra; i++)
            {
                Vector2D point2D2 = TryToCreatePointInCircle(rand.NextDouble(), rand.NextDouble());
                if (point2D2 != null)
                {
                    point2D2 = (point2D2 + new Vector2D(0.5f, 0.5f)) * imageSize + new Vector2D(imageSize, 0);
                    SetPixel(image, point2D2);
                }
            }

            image.Save(WorkingDirectory + "DiscSamplerTest.bmp");
        }

        private void SetPixel(Bitmap image, Vector2D point)
        {
            int x = MathExtensions.Clamp((int)(point.X), 0, image.Width - 1);
            int y = MathExtensions.Clamp((int)(point.Y), 0, image.Height - 1);
            image.SetPixel(x, y, Color.Blue);
        }

        private Vector2D TryToCreatePointInCircle(double u1, double u2)
        {
            u1 -= 0.5;
            u2 -= 0.5;
            if (u1 * u1 + u2 * u2 > 0.25) return null;
            return new Vector2D((float)u1, (float)u2);
        }
    }
}
