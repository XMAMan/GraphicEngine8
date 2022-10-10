using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitmapHelper;
using RayObjects;
using RaytracingBrdf.BrdfFunctions;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz;

//https://en.wikipedia.org/wiki/Bidirectional_reflectance_distribution_function

namespace RaytracingBrdfTest
{
    [TestClass]
    public class IBrdfTest
    {
        private readonly int[] sampleCounts = new int[] { 1000, 10000, 100000 }; //5 Minuten
        //private readonly int[] sampleCounts = new int[] { 100000 }; 
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]        
        public void CreateBrdfImage()
        {
            //Result(IBrdfTestFunctions.GetResult_Brdf_RadianceIsOne(BrdfBasisFunction.WalterGlas, sampleCounts), "Get-Brdf").Save(WorkingDirectory + "BrdfSpezialTest.bmp");
            //Result(IBrdfTestFunctions.SampleDirection_Brdf_RadianceIsOne(BrdfBasisFunction.Glossy), "SampleDirection.Brdf").Save(WorkingDirectory + "BrdfSpezialTest.bmp");
            //Result(IBrdfTestFunctions.GetResult_Pdf_IntegralIsOne(BrdfBasisFunction.WalterMirror, sampleCounts), "Get-Pdf").Save(WorkingDirectory + "BrdfSpezialTest.bmp");
            //Result(IBrdfTestFunctions.CompareSamplePdfWithPdfReturnValue(BrdfBasisFunction.WalterGlas, 1000000), "Histogram-Pdf").Save(WorkingDirectory + "BrdfSpezialTest.bmp");
            int i = 0; if (i==0) return;

            List <Bitmap> rows = new List<Bitmap>();
            foreach (BrdfBasisFunction material in (BrdfBasisFunction[])Enum.GetValues(typeof(BrdfBasisFunction)))
            {
                //if (material != BrdfBasisFunction.HeizGlas) continue;
                List<Bitmap> images = new List<Bitmap>
                {
                    BitmapHelp.WriteToBitmap(new Bitmap(100, 30), material.ToString(), Color.Black),

                    //Energieerhaltungstest
                    Result(IBrdfTestFunctions.GetResult_Brdf_RadianceIsOne(material, sampleCounts, 300), "Get-Brdf"),
                    Result(IBrdfTestFunctions.SampleDirection_Brdf_RadianceIsOne(material, 300), "SampleDirection.Brdf"),

                    //Pdf-Normalisierungstest
                    Result(IBrdfTestFunctions.GetResult_Pdf_IntegralIsOne(material, sampleCounts, 300), "Get-Pdf"),

                    //Pdf-Histogram-Test
                    Result(IBrdfTestFunctions.CompareSamplePdfWithPdfReturnValue(material), "Histogram-Pdf")
                };

                rows.Add(BitmapHelp.TransformBitmapListToRow(images));
            }

            BitmapHelp.TransformBitmapListToCollum(rows).Save(WorkingDirectory + "BrdfTest.bmp");
        }

        [TestMethod]
        public void CreateBrdfSlopeImage()
        {
            List<Bitmap> rows = new List<Bitmap>();
            foreach (BrdfBasisFunction material in (BrdfBasisFunction[])Enum.GetValues(typeof(BrdfBasisFunction)))
            {
                Bitmap image = IBrdfTestFunctions.GetBrdfSlope(material, 300);
                if (image != null)
                    rows.Add(BitmapHelp.WriteToBitmap(image, material.ToString(), Color.Blue));
            }
            BitmapHelp.TransformBitmapListToCollum(rows).Save(WorkingDirectory + "BrdfSlopes.bmp");
        }

        private static Bitmap Result(TestResult result, string text)
        {
            if (result.Image == null) result.Image = new Bitmap(100, 30);
            return BitmapHelp.WriteToBitmap(result.Image, text + " " + result.Error, result.TestWasOk ? Color.Green : Color.Red);
        }

        //Prüfe Energieerhaltungssatz bei SampleDirection
        [TestMethod]
        public void SampleDirection_CalledMultipeTimes_SumIsOne_Diffuse()
        {
            Assert.IsTrue(IBrdfTestFunctions.SampleDirection_Brdf_RadianceIsOne(BrdfBasisFunction.Diffuse).TestWasOk);
        }

        [TestMethod]
        public void SampleDirection_CalledMultipeTimes_SumIsOne_Glanzpunkt()
        {
            Assert.IsTrue(IBrdfTestFunctions.SampleDirection_Brdf_RadianceIsOne(BrdfBasisFunction.Glanzpunkt).TestWasOk);
        }

        [TestMethod]
        public void SampleDirection_CalledMultipeTimes_SumIsOne_Glas()
        {
            Assert.IsTrue(IBrdfTestFunctions.SampleDirection_Brdf_RadianceIsOne(BrdfBasisFunction.Glas).TestWasOk);
        }

        [TestMethod]
        public void SampleDirection_CalledMultipeTimes_SumIsOne_Glossy()
        {
            Assert.IsTrue(IBrdfTestFunctions.SampleDirection_Brdf_RadianceIsOne(BrdfBasisFunction.Phong).TestWasOk);
        }

        [TestMethod]
        public void SampleDirection_CalledMultipeTimes_SumIsOne_Mirror()
        {
            Assert.IsTrue(IBrdfTestFunctions.SampleDirection_Brdf_RadianceIsOne(BrdfBasisFunction.Mirror).TestWasOk);
        }

        [TestMethod]
        public void SampleDirection_CalledMultipeTimes_SumIsOne_WalterGlas()
        {
            Assert.IsTrue(IBrdfTestFunctions.SampleDirection_Brdf_RadianceIsOne(BrdfBasisFunction.WalterGlas).TestWasOk);
        }

        [TestMethod]
        public void SampleDirection_CalledMultipeTimes_SumIsOne_WalterMirror()
        {
            Assert.IsTrue(IBrdfTestFunctions.SampleDirection_Brdf_RadianceIsOne(BrdfBasisFunction.WalterMirror).TestWasOk);
        }

        [TestMethod]
        public void SampleDirection_CalledMultipeTimes_SumIsOne_HeizGlas()
        {
            Assert.IsTrue(IBrdfTestFunctions.SampleDirection_Brdf_RadianceIsOne(BrdfBasisFunction.HeizGlas).TestWasOk);
        }

        [TestMethod]
        public void SampleDirection_CalledWithSpecialParameters1_ThrowsNoException_HeizGlas()
        {
            SphericalCoordinateConverter sphere = new SphericalCoordinateConverter(new Frame(new Vector3D(0,-1,0)));
            var rayHeigh = new RayDrawingObject(new ObjectPropertys() { BrdfModel =  BrdfModel.HeizGlass, NormalSource = new NormalFromMicrofacet(), RefractionIndex = 1.5f }, null, null);
            var brdfPoint = new IntersectionPoint(new Vertex(new Vector3D(0, 0, 0), sphere.Normal, sphere.Tangent), new Vector3D(1, 1, 1), null, sphere.Normal, sphere.Normal, null, null, rayHeigh);

            Vector3D lightGoingInDirection = new Vector3D(-0.006531299f, 0.9999654f, 0.005153736f);
            double u1 = 0.0527893766075323;
            double u2 = 0.848681543883254;
            double u3 = 0.575728383183353;

            //bool lightGoingInRayIsOutside = false;

            var brdf = new HeizGlas(brdfPoint, lightGoingInDirection, 1.5f, 1);
            brdf.SampleDirection(lightGoingInDirection, u1, u2, u3);        
        }

        [TestMethod]
        public void SampleDirection_CalledWithSpecialParameters2_ThrowsNoException_HeizGlas()
        {
            SphericalCoordinateConverter sphere = new SphericalCoordinateConverter(new Frame(new Vector3D(0, -1, 0)));
            var rayHeigh = new RayDrawingObject(new ObjectPropertys() { BrdfModel = BrdfModel.HeizGlass, RefractionIndex = 1.5f, NormalSource = new NormalFromMicrofacet() { MicrofacetRoughness = new Vector2D(0.03f, 0.03f) } }, null, null);
            var brdfPoint = new IntersectionPoint(new Vertex(new Vector3D(0, 0, 0), sphere.Normal, sphere.Tangent), new Vector3D(1, 1, 1), null, sphere.Normal, sphere.Normal, null, null, rayHeigh);

            Vector3D lightGoingInDirection = new Vector3D(0.01406738f, 0.999542f, 0.0267962f);
            double u1 = 0.912311193026747;
            double u2 = 0.963524011878075;
            double u3 = 0.475663766486414;

            //bool lightGoingInRayIsOutside = false;

            var brdf = new HeizGlas(brdfPoint, lightGoingInDirection, 1.5f, 1);
            brdf.SampleDirection(lightGoingInDirection, u1, u2, u3);
        }

        [TestMethod]
        [Ignore] //Lichtstrahl kommt ja schon von Rückseite
        public void SampleDirection_DiffuseMaterial_SampledDirectionLaysInFrontOfSurface()
        {
            SphericalCoordinateConverter sphere = new SphericalCoordinateConverter(new Frame(new Vector3D(0.956590652f, 0.0039525074f, -0.291408211f)));
            var rayHeigh = new RayDrawingObject(new ObjectPropertys() { BrdfModel = BrdfModel.Diffus }, null, null);
            var brdfPoint = new IntersectionPoint(new Vertex(new Vector3D(0, 0, 0), sphere.Normal, sphere.Tangent), new Vector3D(1, 1, 1), null, sphere.Normal, sphere.Normal, null, null, rayHeigh);

            Vector3D lightGoingInDirection = new Vector3D(-0.289335459f, 0.0721146837f, -0.95450747f);
            double u1 = 0.68876869822329312;
            double u2 = 0.64567714400853826;
            double u3 = double.NaN;

            //bool lightGoingInRayIsOutside = false;

            var brdf = new BrdfDiffuseCosinusWeighted(brdfPoint);
            var d = brdf.SampleDirection(lightGoingInDirection, u1, u2, u3);

            float inDot = (-lightGoingInDirection) * brdfPoint.OrientedFlatNormal;
            float outDot = d.SampledDirection * brdfPoint.OrientedFlatNormal;
            bool inAndOutOnDifferentSides = (inDot < 0.0) ^ (outDot < 0.0);
            if (inAndOutOnDifferentSides) throw new Exception("Wie kann das sein?");
        }

        [TestMethod]
        public void SampleDirection_CalledMultipeTimes_SumIsOne_HeizMirror()
        {
            Assert.IsTrue(IBrdfTestFunctions.SampleDirection_Brdf_RadianceIsOne(BrdfBasisFunction.HeizMirror).TestWasOk);
        }

        //Prüfe Energieerhaltungssatz bei GetBrdf
        [TestMethod]
        public void Brdf_CalledMultipeTimes_SumIsOne_Diffuse()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Brdf_RadianceIsOne(BrdfBasisFunction.Diffuse, sampleCounts).TestWasOk);
        }
        [TestMethod]
        public void Brdf_CalledMultipeTimes_SumIsOne_Glanzpunkt()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Brdf_RadianceIsOne(BrdfBasisFunction.Glanzpunkt, sampleCounts).TestWasOk);
        }
        [TestMethod]
        public void Brdf_CalledMultipeTimes_SumIsOne_Glas()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Brdf_RadianceIsOne(BrdfBasisFunction.Glas, sampleCounts).TestWasOk);
        }
        [TestMethod]
        [Ignore]
        public void Brdf_CalledMultipeTimes_SumIsOne_Glossy()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Brdf_RadianceIsOne(BrdfBasisFunction.Phong, sampleCounts).TestWasOk);
        }
        [TestMethod]
        public void Brdf_CalledMultipeTimes_SumIsOne_Mirror()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Brdf_RadianceIsOne(BrdfBasisFunction.Mirror, sampleCounts).TestWasOk);
        }
        [TestMethod]
        public void Brdf_CalledMultipeTimes_SumIsOne_WalterGlas()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Brdf_RadianceIsOne(BrdfBasisFunction.WalterGlas, sampleCounts).TestWasOk);
        }
        [TestMethod]
        [Ignore]
        public void Brdf_CalledMultipeTimes_SumIsOne_WalterMirror()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Brdf_RadianceIsOne(BrdfBasisFunction.WalterMirror, sampleCounts).TestWasOk);
        }
        [TestMethod]
        public void Brdf_CalledMultipeTimes_SumIsOne_HeizGlas()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Brdf_RadianceIsOne(BrdfBasisFunction.HeizGlas, sampleCounts).TestWasOk);
        }
        [TestMethod]
        [Ignore]
        public void Brdf_CalledMultipeTimes_SumIsOne_HeizMirror()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Brdf_RadianceIsOne(BrdfBasisFunction.HeizMirror, sampleCounts).TestWasOk);
        }


        //Prüfe Normalisierung bei PdfW
        [TestMethod]
        public void PdfW_CalledMultipeTimes_SumIsOne_Diffuse()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Pdf_IntegralIsOne(BrdfBasisFunction.Diffuse, sampleCounts).TestWasOk);
        }
        [TestMethod]
        public void PdfW_CalledMultipeTimes_SumIsOne_Glanzpunkt()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Pdf_IntegralIsOne(BrdfBasisFunction.Glanzpunkt, sampleCounts).TestWasOk);
        }
        [TestMethod]
        public void PdfW_CalledMultipeTimes_SumIsOne_Glas()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Pdf_IntegralIsOne(BrdfBasisFunction.Glas, sampleCounts).TestWasOk);
        }
        [TestMethod]
        public void PdfW_CalledMultipeTimes_SumIsOne_Glossy()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Pdf_IntegralIsOne(BrdfBasisFunction.Phong, sampleCounts).TestWasOk);
        }
        [TestMethod]
        public void PdfW_CalledMultipeTimes_SumIsOne_Mirror()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Pdf_IntegralIsOne(BrdfBasisFunction.Mirror, sampleCounts).TestWasOk);
        }
        [TestMethod]
        [Ignore]
        public void PdfW_CalledMultipeTimes_SumIsOne_WalterGlas()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Pdf_IntegralIsOne(BrdfBasisFunction.WalterGlas, sampleCounts).TestWasOk);
        }
        [TestMethod]
        [Ignore]
        public void PdfW_CalledMultipeTimes_SumIsOne_WalterMirror()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Pdf_IntegralIsOne(BrdfBasisFunction.WalterMirror, sampleCounts).TestWasOk);
        }
        [TestMethod]
        [Ignore]
        public void PdfW_CalledMultipeTimes_SumIsOne_HeizGlas()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Pdf_IntegralIsOne(BrdfBasisFunction.HeizGlas, sampleCounts).TestWasOk);
        }
        [TestMethod]
        public void PdfW_CalledMultipeTimes_SumIsOne_HeizMirror()
        {
            Assert.IsTrue(IBrdfTestFunctions.GetResult_Pdf_IntegralIsOne(BrdfBasisFunction.HeizMirror, sampleCounts).TestWasOk);
        }

        //Pdf-Histogram-Test

        [TestMethod]
        public void SampleDirectionAndPdfW_CalledMultipeTimes_PdfWEqualsHistrogramValue_Diffuse()
        {
            Assert.IsTrue(IBrdfTestFunctions.CompareSamplePdfWithPdfReturnValue(BrdfBasisFunction.Diffuse).TestWasOk);
        }
        [TestMethod]
        public void SampleDirectionAndPdfW_CalledMultipeTimes_PdfWEqualsHistrogramValue_Glanzpunkt()
        {
            Assert.IsTrue(IBrdfTestFunctions.CompareSamplePdfWithPdfReturnValue(BrdfBasisFunction.Glanzpunkt).TestWasOk);
        }
        [TestMethod]
        public void SampleDirectionAndPdfW_CalledMultipeTimes_PdfWEqualsHistrogramValue_Glas()
        {
            Assert.IsTrue(IBrdfTestFunctions.CompareSamplePdfWithPdfReturnValue(BrdfBasisFunction.Glas).TestWasOk);
        }
        [TestMethod]
        public void SampleDirectionAndPdfW_CalledMultipeTimes_PdfWEqualsHistrogramValue_Glossy()
        {
            Assert.IsTrue(IBrdfTestFunctions.CompareSamplePdfWithPdfReturnValue(BrdfBasisFunction.Phong).TestWasOk);
        }
        [TestMethod]
        public void SampleDirectionAndPdfW_CalledMultipeTimes_PdfWEqualsHistrogramValue_Mirror()
        {
            Assert.IsTrue(IBrdfTestFunctions.CompareSamplePdfWithPdfReturnValue(BrdfBasisFunction.Mirror).TestWasOk);
        }
        [TestMethod]
        [Ignore]
        public void SampleDirectionAndPdfW_CalledMultipeTimes_PdfWEqualsHistrogramValue_WalterGlas()
        {
            Assert.IsTrue(IBrdfTestFunctions.CompareSamplePdfWithPdfReturnValue(BrdfBasisFunction.WalterGlas).TestWasOk);
        }
        [TestMethod]
        public void SampleDirectionAndPdfW_CalledMultipeTimes_PdfWEqualsHistrogramValue_WalterMirror()
        {
            Assert.IsTrue(IBrdfTestFunctions.CompareSamplePdfWithPdfReturnValue(BrdfBasisFunction.WalterMirror).TestWasOk);
        }
        [TestMethod]
        [Ignore]
        public void SampleDirectionAndPdfW_CalledMultipeTimes_PdfWEqualsHistrogramValue_HeizGlas()
        {
            Assert.IsTrue(IBrdfTestFunctions.CompareSamplePdfWithPdfReturnValue(BrdfBasisFunction.HeizGlas).TestWasOk);
        }
        [TestMethod]
        public void SampleDirectionAndPdfW_CalledMultipeTimes_PdfWEqualsHistrogramValue_HeizMirror()
        {
            Assert.IsTrue(IBrdfTestFunctions.CompareSamplePdfWithPdfReturnValue(BrdfBasisFunction.HeizMirror).TestWasOk);
        }

    }
}
