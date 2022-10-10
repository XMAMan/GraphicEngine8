using BitmapHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfHistogram;
using RaytracingRandom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaytracingRandomTest
{
    [TestClass]
    public class Distribution2DTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void SamplerMatchWithHistogram()
        {
            double[,] image = new double[,]
            {
                {0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0 },
                {0, 3, 0, 0, 0, 3, 0 },
                {0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 5, 0, 0, 0 },
                {0, 0, 0, 5, 0, 0, 0 },
                {1, 0, 0, 0, 0, 0, 1 },
                {1, 1, 1, 1, 1, 1, 1 },
            };

            Distribution2D sut = new Distribution2D(image);

            RectangleHistogram histogram = new RectangleHistogram(image.GetLength(0) * 2, image.GetLength(1) * 2, new Size(image.GetLength(0), image.GetLength(1)));

            Random rand = new Random(0);
            int sampleCount = 10000;
            for (int i=0;i<sampleCount;i++)
            {
                var sample = sut.SampleXYIndex(rand.NextDouble(), rand.NextDouble());
                double pdfAFromFunction = sut.Pdf(sample.X, sample.Y);
                Assert.IsTrue(Math.Abs(sample.PdfA - pdfAFromFunction) < 0.0001);
                histogram.AddSample(new Vector2D((float)sample.X + 0.5f, (float)sample.Y + 0.5f), (float)sample.PdfA);
            }

            histogram.GetResultImage().Save(WorkingDirectory + "Distribution2D.bmp");
        }

    }
}
