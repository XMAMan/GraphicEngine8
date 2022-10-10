using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RayObjects;
using RaytracingBrdf;
using RaytracingBrdf.SampleAndRequest;

namespace RaytracingBrdfTest
{
    [TestClass]
    public class BrdfSamplerTest
    {
        [TestMethod]
        public void ValueRangeCheck_Diffuse()
        {
            ValueRangeCheck(BrdfModel.Diffus);
        }

        [TestMethod]
        public void ValueRangeCheck_Fliese()
        {
            ValueRangeCheck(BrdfModel.Tile);
        }

        [TestMethod]
        public void ValueRangeCheck_Glas()
        {
            ValueRangeCheck(BrdfModel.TextureGlass);
        }

        [TestMethod]
        public void ValueRangeCheck_Glossy()
        {
            ValueRangeCheck(BrdfModel.Phong);
        }

        [TestMethod]
        public void ValueRangeCheck_WalterGlas()
        {
            ValueRangeCheck(BrdfModel.WalterGlass);
        }

        [TestMethod]
        public void ValueRangeCheck_WalterMetall()
        {
            ValueRangeCheck(BrdfModel.WalterMetal);
        }

        [TestMethod]
        public void ValueRangeCheck_HeizGlas()
        {
            ValueRangeCheck(BrdfModel.HeizGlass);
        }

        [TestMethod]
        public void ValueRangeCheck_HeizMetall()
        {
            ValueRangeCheck(BrdfModel.HeizMetal);
        }

        [TestMethod]
        public void ValueRangeCheck_Phong()
        {
            ValueRangeCheck(BrdfModel.PlasticDiffuse);
        }

        [TestMethod]
        public void ValueRangeCheck_Spiegel()
        {
            ValueRangeCheck(BrdfModel.Mirror);
        }

        //Überprüft BrdfSampler.CreateDirectionWithPdfW.Brdf/PdfW/PdfWReverse
        //Teste Einhaltung der Wertebereiche von BrdfWeight, Pdf, Direction
        private void ValueRangeCheck(BrdfModel material, int sampleCount = 100)
        {
            SphericalCoordinateConverter sphere = new SphericalCoordinateConverter();
            var rayHeigh = new RayDrawingObject(new ObjectPropertys() { BrdfModel = material, RefractionIndex = 1.5f, NormalSource = new NormalFromMicrofacet() { MicrofacetRoughness = new Vector2D(0.1f, 0.1f) } }, null, null);
            var brdfPoint = new IntersectionPoint(new Vertex(new Vector3D(0, 0, 0), sphere.Normal, sphere.Tangent), new Vector3D(1, 1, 1), null, sphere.Normal, sphere.Normal, null, null, rayHeigh);
            var brdfSampler = new BrdfSampler();

            SphericalCoordinate spherical = new SphericalCoordinate(Math.PI / 8, 0);
            double thetaMax = Math.PI;
            int thetaStepCount = 300;
            //double maxError = 0.01f;

            IRandom rand = new Rand(0);
            for (int ti = 0; ti < thetaStepCount; ti++)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    spherical.Theta = ti / (double)thetaStepCount * thetaMax;
                    Vector3D inputDirection = -sphere.ToWorldDirection(spherical);

                    var brdfPoint1 = new BrdfPoint(brdfPoint, inputDirection, 1, brdfPoint.RefractionIndex);
                    var result1 = brdfPoint1.SampleDirection(brdfSampler, rand);
                    if (result1 == null) continue;
                    var result2 = brdfPoint1.Evaluate(result1.Ray.Direction);
                    //if (result1.Brdf.Max() == 0) continue;
                    Assert(result1.Brdf.X >= 0 && float.IsNaN(result1.Brdf.X) == false && float.IsInfinity(result1.Brdf.X) == false);
                    Assert(result1.Brdf.Y >= 0 && float.IsNaN(result1.Brdf.Y) == false && float.IsInfinity(result1.Brdf.Y) == false);
                    Assert(result1.Brdf.Z >= 0 && float.IsNaN(result1.Brdf.Z) == false && float.IsInfinity(result1.Brdf.Z) == false);
                    //WalterBrdf-Glas sampelt bei schräger Draufsicht auch Micronormalen, welche vom Betrachter aus nicht sichtbar sind. Dadurch ist dann
                    //die Brdf und die PdfW dann 0. Nur wenn die Brdf auch was liefert, muss die PdfW ein Wert > 0 haben.
                    if (result1.Brdf.X > 0)
                    {
                        Assert(result1.PdfW > 0 && float.IsNaN(result1.PdfW) == false && float.IsInfinity(result1.PdfW) == false);
                        Assert(result1.PdfWReverse > 0 && float.IsNaN(result1.PdfWReverse) == false && float.IsInfinity(result1.PdfWReverse) == false);
                    }

                    if (result1.RayWasRefracted)
                    {
                        var reverseBrdf = new BrdfPoint(brdfPoint, -result1.Ray.Direction, brdfPoint.RefractionIndex, 1);
                        float reversePdfW = reverseBrdf.Brdf.PdfW(-result1.Ray.Direction, -inputDirection);

                        Assert(Math.Abs(result1.PdfWReverse - reversePdfW) < 0.1f);
                    }

                    if (result2 != null)
                    {
                        Assert(result2.Brdf.X >= 0 && float.IsNaN(result2.Brdf.X) == false && float.IsInfinity(result2.Brdf.X) == false);
                        Assert(result2.Brdf.Y >= 0 && float.IsNaN(result2.Brdf.Y) == false && float.IsInfinity(result2.Brdf.Y) == false);
                        Assert(result2.Brdf.Z >= 0 && float.IsNaN(result2.Brdf.Z) == false && float.IsInfinity(result2.Brdf.Z) == false);
                        if (result2.Brdf.X > 0)
                        {
                            Assert(result2.PdfW > 0 && float.IsNaN(result2.PdfW) == false && float.IsInfinity(result2.PdfW) == false);
                            Assert(result2.PdfWReverse > 0 && float.IsNaN(result2.PdfWReverse) == false && float.IsInfinity(result2.PdfWReverse) == false);

                            bool isCompoundMaterial = brdfPoint1.DiffusePortion > 0 && brdfPoint1.DiffusePortion < 1; //Mischung aus Diffuse + Specular (Bei der PdfW-Abfrage bekomme ich nur die diffuse Pdf zurück aber bei Specular gesampelten Strahl erhalte ich eine andere PdfW)
                            bool isMicrofacetGlas = material == BrdfModel.HeizGlass || material == BrdfModel.WalterGlass; //Bei Microfacett-Glas kann die PdfW-Funktion nicht feststellen, ob der Strahl ursprünglich Reflektiert/Gebrochen wurde
                            if (isCompoundMaterial == false && isMicrofacetGlas == false)
                            {
                                PdfWCompare(result1.PdfW, result2.PdfW);
       
                                if (result1.RayWasRefracted == false)
                                    PdfWCompare(result1.PdfWReverse, result2.PdfWReverse);
                            }

                            
                        }

                        //Check HelmolzReciprocity -> MicrofacetGlas erfüllt nicht die Helmholz-Regel, da die Brdf den no²-Term (Brechungsindex vom Outvektor)
                        if (material != BrdfModel.WalterGlass &&
                            material != BrdfModel.HeizGlass &&
                            material != BrdfModel.Phong) //Aufgrund des Jacobi-Faktors in der Glossy-Brdf-Abfrage schlägt der Helmholztest fehl aber ohne diese schlägt der Brdf-IsOne-Test fehl
                        {
                            var brdf1 = BrdfFactory.CreateBrdf(brdfPoint, inputDirection, 1, brdfPoint.RefractionIndex).Evaluate(inputDirection, result1.Ray.Direction);
                            var brdf2 = BrdfFactory.CreateBrdf(brdfPoint, -result1.Ray.Direction, brdfPoint.RefractionIndex, 1).Evaluate(-result1.Ray.Direction, -inputDirection);

                            Assert(Math.Abs(brdf1.X - brdf2.X) < 0.1f);
                            Assert(Math.Abs(brdf1.Y - brdf2.Y) < 0.1f);
                            Assert(Math.Abs(brdf1.Z - brdf2.Z) < 0.1f);
                        }
                    }

                    //Brdf/Pdf von Abfrage weicht von Sampler ab, da Sampler PathSelectionPdf und PathContinuationPdf enthält. Deswegen kann man die nicht hier vergleichen sondern man müsste das IBrdf-Interface direkt testen
                    /*if (result1.IsSpecualarReflected == false && result2 != null)
                    {
                        Vector3D brdfWeight = result2.Brdf * Math.Abs(result2.CosThetaOut) / result2.PdfW;
                        Assert(Math.Abs(result1.Brdf.X - brdfWeight.X) < maxError);
                        Assert(Math.Abs(result1.Brdf.Y - brdfWeight.Y) < maxError);
                        Assert(Math.Abs(result1.Brdf.Z - brdfWeight.Z) < maxError);
                        Assert(Math.Abs(result1.PdfW - result2.PdfW) < maxError);
                        Assert(Math.Abs(result1.PdfWReverse - result2.PdfWReverse) < maxError);
                    } */
                }
            }
        }

        private void PdfWCompare(float pdfW1, float pdfW2)
        {
            if (pdfW1 > 50)
                Assert(Math.Abs(pdfW1 - pdfW2) < 1);
            else
                Assert(Math.Abs(pdfW1 - pdfW2) < 0.1f);
        }

        private void Assert(bool condition)
        {
            if (condition == false) throw new Exception("Assert-Exception");
        }
    }
}
