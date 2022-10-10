using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public class NonLinearFunctionScannerTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        //Erzeuge ein Bild, wo mehr rote Striche sind, wo die Funktion höher ist und wo ich für ein beliebigen X-Wert (Gelb) die beiden Nachbarstützpunkte zurück bekomme
        [TestMethod]
        public void GetScanPosition_CalledWithValueInsideRange_TwoNeighborPointsReturned()
        {
            double minX = -1.363;
            double maxX = +1.791;
            double maxY = 3;
            NonLinearFunctionScanner sut = new NonLinearFunctionScanner(UnnormalisizedPdf, minX, maxX, 20);

            Assert.AreEqual(minX, sut.XValues.First());
            Assert.AreEqual(maxX, sut.XValues.Last());

            Bitmap image = new Bitmap(400, 300);

            Graphics grx = Graphics.FromImage(image);
            grx.Clear(Color.White);

            //Plot Function
            for (int i=0;i<image.Width - 1;i++)
            {
                double y1 = image.Height - 1 - UnnormalisizedPdf(i / (double)image.Width * (maxX - minX) + minX) / maxY * image.Height;
                double y2 = image.Height - 1 - UnnormalisizedPdf((i+1) / (double)image.Width * (maxX - minX) + minX) / maxY * image.Height;
                grx.DrawLine(Pens.Black, i, (int)y1, i + 1, (int)y2);
                //image.SetPixel((int)i, (int)y1, Color.Black);
            }
            
            //Draw ScanPoints
            for (int i=0;i< sut.XValues.Length;i++)
            {
                double x = (sut.XValues[i] - minX) / (maxX - minX) * image.Width;
                double y = image.Height - 1 - UnnormalisizedPdf(sut.XValues[i]) / maxY * image.Height;
                grx.DrawLine(Pens.Red, (int)x, image.Height - 1, (int)x, (int)y);
            }

            //Get Scan Position
            {
                double xScan = 0.2;
                                
                var scan = sut.GetScanPosition(xScan);
                double x1 = (sut.XValues[scan.Index] - minX) / (maxX - minX) * image.Width;
                double y1 = image.Height - 1 - UnnormalisizedPdf(sut.XValues[scan.Index]) / maxY * image.Height;
                double x2 = (sut.XValues[scan.Index + 1] - minX) / (maxX - minX) * image.Width;
                double y2 = image.Height - 1 - UnnormalisizedPdf(sut.XValues[scan.Index + 1]) / maxY * image.Height;

                grx.DrawLine(Pens.Green, (int)x1, (int)y1, (int)x2, (int)y2);

                double xScan1 = (((1-scan.F) * sut.XValues[scan.Index] + scan.F * sut.XValues[scan.Index + 1]) - minX) / (maxX - minX) * image.Width;
                double yScan1 = image.Height - 1 - UnnormalisizedPdf(xScan) / 2 / maxY * image.Height;

                double xScan2 = (xScan - minX) / (maxX - minX) * image.Width;
                double yScan2 = image.Height - 1 - UnnormalisizedPdf(xScan) / maxY * image.Height;

                grx.DrawLine(Pens.Yellow, (int)xScan1, image.Height - 1, (int)xScan1, (int)yScan1); //Return-X-Wert
                grx.DrawLine(Pens.AliceBlue, (int)xScan2, (int)yScan1, (int)xScan2, (int)yScan2);   //Vorgabe-X-Wert
            }            
            
            grx.Dispose();

            image.Save(WorkingDirectory + "NonLinearFunctionScanner.bmp");
        }

        //von x=-1.363 bis x=+1.791	y=0 bis 3
        private double UnnormalisizedPdf(double x)
        {
            return -(x * x * x * x) + 0.5 * (x * x * x) + 2 * (x * x) + 1;
        }
    }
}
