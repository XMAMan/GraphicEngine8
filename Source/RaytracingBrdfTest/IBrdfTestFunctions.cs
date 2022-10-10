using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BitmapHelper;
using PdfHistogram;
using RayObjects;
using GraphicGlobal.MathHelper;
using UnitTestHelper;
using RaytracingBrdf.BrdfFunctions;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Walter;

namespace RaytracingBrdfTest
{
    enum BrdfBasisFunction { Diffuse, Mirror, Glas, Phong, Glanzpunkt, WalterGlas, WalterMirror, HeizGlas, HeizMirror }
    static class IBrdfTestFunctions
    {
        private static IBrdf CreateBrdf(SphericalCoordinateConverter sphere, BrdfBasisFunction basisFunction, Vector3D directionToPoint)
        {
            BrdfModel material = BrdfModel.Diffus;
            switch (basisFunction)
            {
                case BrdfBasisFunction.Diffuse:
                    material = BrdfModel.Diffus;
                    break;
                case BrdfBasisFunction.Mirror:
                    material = BrdfModel.Mirror;
                    break;
                case BrdfBasisFunction.Glas:
                    material = BrdfModel.TextureGlass;
                    break;
                case BrdfBasisFunction.Phong:
                    material = BrdfModel.Phong;
                    break;
                case BrdfBasisFunction.Glanzpunkt:
                    material = BrdfModel.PlasticDiffuse;
                    break;
                case BrdfBasisFunction.WalterGlas:
                    material = BrdfModel.WalterGlass;
                    break;
                case BrdfBasisFunction.WalterMirror:
                    material = BrdfModel.WalterMetal;
                    break;
                case BrdfBasisFunction.HeizGlas:
                    material = BrdfModel.HeizGlass;
                    break;
                case BrdfBasisFunction.HeizMirror:
                    material = BrdfModel.HeizMetal;
                    break;
            }

            var rayHeigh = new RayDrawingObject(new ObjectPropertys() { BrdfModel = material, RefractionIndex = 1.5f, SpecularHighlightPowExponent = 20, NormalSource = new NormalFromMicrofacet() { MicrofacetRoughness = new Vector2D(0.1f, 0.1f) }, Albedo = 1 }, null, null);
            var brdfPoint = new IntersectionPoint(new Vertex(new Vector3D(0, 0, 0), sphere.Normal, sphere.Tangent), new Vector3D(1, 1, 1), null, sphere.Normal, sphere.Normal, null, null, rayHeigh);

            IBrdf brdf = null;
            switch (basisFunction)
            {
                case BrdfBasisFunction.Diffuse:
                    brdf = new BrdfDiffuseCosinusWeighted(brdfPoint); break;
                case BrdfBasisFunction.Mirror:
                    brdf = new BrdfMirror(brdfPoint, true); break;
                case BrdfBasisFunction.Glas:
                    brdf = new BrdfGlas(brdfPoint, directionToPoint, 1, rayHeigh.Propertys.RefractionIndex, false); break;
                case BrdfBasisFunction.Phong:
                    brdf = new BrdfGlossy(brdfPoint); break;
                case BrdfBasisFunction.Glanzpunkt:
                    brdf = new BrdfSpecularHighlight(brdfPoint, true); break;
                case BrdfBasisFunction.WalterGlas:
                    brdf = new WalterGlas(brdfPoint, directionToPoint, 1, rayHeigh.Propertys.RefractionIndex, 0.2f); break;
                case BrdfBasisFunction.WalterMirror:
                    brdf = new WalterMirror(brdfPoint, directionToPoint); break;
                case BrdfBasisFunction.HeizGlas:
                    brdf = new HeizGlas(brdfPoint, directionToPoint, 1, rayHeigh.Propertys.RefractionIndex); break;
                case BrdfBasisFunction.HeizMirror:
                    brdf = new HeizMirror(brdfPoint, directionToPoint); break;
            }
            return brdf;
                
            throw new Exception("Unknown enumvalue " + basisFunction.ToString());
        }

        public static TestResult SampleDirection_Brdf_RadianceIsOne(BrdfBasisFunction material, int thetaStepCount = 31, int sampleCount = 1000) //10000
        {
            SphericalCoordinateConverter sphere = new SphericalCoordinateConverter();

            SphericalCoordinate spherical = new SphericalCoordinate(Math.PI / 8, 0);
            double thetaMax = Math.PI;
            double maxError = 0.2;

            IRandom rand = new Rand(0);
            List<double> radianceValues = new List<double>();

            for (int ti = 0; ti < thetaStepCount; ti++)
            {
                spherical.Theta = ti / (double)thetaStepCount * thetaMax;
                Vector3D directionToPoint = -sphere.ToWorldDirection(spherical);

                IBrdf brdfPoint = CreateBrdf(sphere, material, directionToPoint);

                double radiance = 0;
                for (int i = 0; i < sampleCount; i++)
                {
                    var result = brdfPoint.SampleDirection(directionToPoint, rand.NextDouble(), rand.NextDouble(), rand.NextDouble());
                    if (result != null)
                        radiance += result.BrdfWeightAfterSampling.X;
                }
                radiance /= sampleCount;

                radianceValues.Add(radiance);
            }

            double minRadiance = radianceValues.Min();
            double maxRadiance = radianceValues.Max();


            Bitmap img = new Bitmap(thetaStepCount, 100);
            double minRadiance1 = Math.Min(0, minRadiance - 1);
            double maxRadiance1 = maxRadiance + 1;
            double range = maxRadiance1 - minRadiance1;

            double error = 0;
            double radianceSetPoint = 1;

            for (int x = 0; x < thetaStepCount; x++)
            {
                spherical.Theta = x / (double)thetaStepCount * thetaMax;
                Vector3D directionToPoint = -sphere.ToWorldDirection(spherical);
                //if (material == BrdfBasisFunction.Glossy && directionToPoint.Z > 0) radianceSetPoint = 0;

                img.SetPixel(x, MathExtensions.Clamp(img.Height - 1 - (int)((radianceSetPoint - minRadiance1) / range * img.Height), 0, img.Height), Color.Blue); //Sollwert
                img.SetPixel(x, MathExtensions.Clamp(img.Height - 1 - (int)((radianceSetPoint - maxError - minRadiance1) / range * img.Height), 0, img.Height), Color.Green); //Grenze
                img.SetPixel(x, MathExtensions.Clamp(img.Height - 1 - (int)((radianceSetPoint + maxError - minRadiance1) / range * img.Height), 0, img.Height), Color.Green); //Grenze
                img.SetPixel(x, MathExtensions.Clamp(img.Height - 1 - (int)((radianceValues[x] - minRadiance1) / range * img.Height), 0, img.Height), Color.Red); //Istwert

                if (Math.Abs(directionToPoint * sphere.Normal) > 0.001f)
                {
                    if (material == BrdfBasisFunction.Glanzpunkt)
                    {
                        double error1 = radianceValues[x] - radianceSetPoint;
                        if (error1 > error) error = error1;
                    }
                    else
                    {
                        double error1 = Math.Abs(radianceValues[x] - radianceSetPoint);
                        if (error1 > error) error = error1;
                    }
                }                
            }

            return new TestResult() { Image = img, TestWasOk = error < maxError, Error = error };
        }


        public static TestResult GetResult_Brdf_RadianceIsOne(BrdfBasisFunction material, int[] sampleCounts, int thetaStepCount = 31)
        {
            if (material == BrdfBasisFunction.Mirror ||
                material == BrdfBasisFunction.Glas) return new TestResult() { TestWasOk = true }; //Diese Brdf-Werte sind per BrdfAbfrage nicht abfragbar

            SphericalCoordinateConverter sphere = new SphericalCoordinateConverter();

            SphericalCoordinate spherical = new SphericalCoordinate(Math.PI / 8, 0);
            double thetaMax = Math.PI;
            double maxError = 0.1;

            List<double> radianceValues = new List<double>();

            for (int ti = 0; ti < thetaStepCount; ti++)
            {
                //if (ti > 12) break;
                spherical.Theta = ti / (double)thetaStepCount * thetaMax;
                Vector3D directionToPoint = -sphere.ToWorldDirection(spherical);

                IBrdf brdfPoint = CreateBrdf(sphere, material, directionToPoint);

                List<float> ra = new List<float>();
                //Versuche das Integral notfalls mehrmals mit verschiedenen SampleCounts zu lösen, um Integrations-Rechenfehler auszuschließen
                double radiance = 0;
                //List<float> values = null;
                for (int i = 0; i < sampleCounts.Length; i++)
                {
                    
                    //if (ti == 9 && i == sampleCounts.Length - 1)
                    //{
                    //    values = new List<float>();
                    //}
                    radiance = SphereIntegrator.IntegrateWithMonteCarlo((wo, phiP, thetaP) =>
                    {
                        float inDot = (-directionToPoint) * sphere.Normal;
                        float outDot = wo * sphere.Normal;
                        bool inAndOutOnDifferentSides = (inDot < 0.0) ^ (outDot < 0.0);
                        if (material == BrdfBasisFunction.Diffuse && inAndOutOnDifferentSides) return 0;
                        //if (material != BrdfBasisFunction.WalterGlas && inAndOutOnDifferentSides) return 0;

                        var result = brdfPoint.Evaluate(directionToPoint, wo);
                        if (result == null) return 0;
                        //if (values != null && values.Count == 17383)
                        //{
                        //    string ha = "";
                        //}
                        //if (values != null) values.Add(result.X);
                        return result.X * Math.Abs(sphere.Normal * wo);
                    }, 0, 360, 0, 180, sampleCounts[i]);
                    //if (Math.Abs(radiance - 1) < maxError) break;
                    if (radiance + maxError < 1) break;
                }
                //if (ti == 9)
                //{
                //    string hu = "";
                //}
                radianceValues.Add(radiance);
            }

            double minRadiance = radianceValues.Min();
            double maxRadiance = radianceValues.Max();

            Bitmap img = new Bitmap(thetaStepCount, 100);
            double minRadiance1 = Math.Min(0, minRadiance - 1);
            double maxRadiance1 = maxRadiance + 1;
            double range = maxRadiance1 - minRadiance1;

            double error = 0;
            double radianceSetPoint = 1;

            for (int x = 0; x < thetaStepCount; x++)
            {
                spherical.Theta = x / (double)thetaStepCount * thetaMax;
                Vector3D directionToPoint = -sphere.ToWorldDirection(spherical);
                //double radianceSetPoint = directionToPoint.Z < 0 ? 1 : 0;
                //if (material == BrdfBasisFunction.WalterGlas) radianceSetPoint = 1;

                img.SetPixel(x, MathExtensions.Clamp(img.Height - 1 - (int)((radianceSetPoint - minRadiance1) / range * img.Height), 0, img.Height), Color.Blue); //Sollwert
                img.SetPixel(x, MathExtensions.Clamp(img.Height - 1 - (int)((radianceSetPoint - maxError - minRadiance1) / range * img.Height), 0, img.Height), Color.Green); //Grenze
                img.SetPixel(x, MathExtensions.Clamp(img.Height - 1 - (int)((radianceSetPoint + maxError - minRadiance1) / range * img.Height), 0, img.Height), Color.Green); //Grenze
                img.SetPixel(x, MathExtensions.Clamp(img.Height - 1 - (int)((radianceValues[x] - minRadiance1) / range * img.Height), 0, img.Height), Color.Red); //Istwert

                if (Math.Abs(directionToPoint * sphere.Normal) > 0.001f)
                {
                    if (material == BrdfBasisFunction.Glanzpunkt ||
                        material == BrdfBasisFunction.WalterGlas ||
                        material == BrdfBasisFunction.HeizGlas)
                    {
                        double error1 = radianceValues[x] - radianceSetPoint;
                        if (error1 > error) error = error1;
                    }
                    else
                    {
                        double error1 = Math.Abs(radianceValues[x] - radianceSetPoint);
                        if (error1 > error) error = error1;
                    }
                }                   
            }

            return new TestResult() { Image = img, TestWasOk = error < maxError, Error = error };
        }

        public static TestResult GetResult_Pdf_IntegralIsOne(BrdfBasisFunction material, int[] sampleCounts, int thetaStepCount = 31)
        {
            if (material == BrdfBasisFunction.Mirror ||
                material == BrdfBasisFunction.Glas) return new TestResult() { TestWasOk = true }; //Diese Pdf-Werte sind per BrdfAbfrage nicht abfragbar

            SphericalCoordinateConverter sphere = new SphericalCoordinateConverter();

            SphericalCoordinate spherical = new SphericalCoordinate(Math.PI / 8, 0);
            double thetaMax = Math.PI;
            double maxError = 0.2;
            List<double> pdfValues = new List<double>();

            for (int ti = 0; ti < thetaStepCount; ti++)
            {
                spherical.Theta = ti / (double)thetaStepCount * thetaMax;
                Vector3D directionToPoint = -sphere.ToWorldDirection(spherical);

                IBrdf brdfPoint = CreateBrdf(sphere, material, directionToPoint);

                //Versuche das Integral notfalls mehrmals mit verschiedenen SampleCounts zu lösen, um Integrations-Rechenfehler auszuschließen
                double pdfIntegral = 0;
                //List<float> values = null;
                for (int i = 0; i < sampleCounts.Length; i++)
                {
                    //if (ti == 1 && i == 0)
                    //{
                    //    values = new List<float>();
                    //}

                    pdfIntegral = SphereIntegrator.IntegrateWithMonteCarlo((wo, phiP, thetaP) =>
                    {
                        /*float inDot = (-directionToPoint) * sphere.Normal;
                        float outDot = wo * sphere.Normal;
                        bool inAndOutOnDifferentSides = (inDot < 0.0) ^ (outDot < 0.0);
                        if (material != BrdfBasisFunction.WalterGlas && inAndOutOnDifferentSides) return 0;*/


                        //float pdfW = brdfPoint.PdfW(directionToPoint, wo, true);
                        //if (values != null && values.Count == 35)
                        //{
                        //    string ha = "";
                        //}
                        //if (values != null) values.Add(pdfW);

                        return brdfPoint.PdfW(directionToPoint, wo);
                    }, 0, 360, 0, 180, sampleCounts[i]);
                    if (Math.Abs(pdfIntegral - 1) < maxError) break;
                    //if (pdfIntegral < 1 + maxError) break;
                }
                //if (pdfIntegral > 8)
                //{
                //    string u = "";
                //}
                pdfValues.Add(pdfIntegral);
            }

            double minPdf = pdfValues.Min();
            double maxPdf = pdfValues.Max();

            Bitmap img = new Bitmap(thetaStepCount, 100);
            double minPdf1 = Math.Min(0, minPdf - 1);
            double maxPdf1 = maxPdf + 1;
            double range = maxPdf1 - minPdf1;

            double pdfSetPoint = 1;

            double error = 0;

            for (int x = 0; x < thetaStepCount; x++)
            {
                spherical.Theta = x / (double)thetaStepCount * thetaMax;
                Vector3D directionToPoint = -sphere.ToWorldDirection(spherical);
                //double pdfSetPoint = directionToPoint.Z < 0 ? 1 : 0;
                //if (material == BrdfBasisFunction.WalterGlas) pdfSetPoint = 1;
                //if (material == BrdfBasisFunction.Diffuse && directionToPoint.Z > 0) pdfSetPoint = 0;

                img.SetPixel(x, MathExtensions.Clamp(img.Height - 1 - (int)((pdfSetPoint - minPdf1) / range * img.Height), 0, img.Height), Color.Blue); //Sollwert
                img.SetPixel(x, MathExtensions.Clamp(img.Height - 1 - (int)((pdfSetPoint - maxError - minPdf1) / range * img.Height), 0, img.Height), Color.Green); //Grenze
                img.SetPixel(x, MathExtensions.Clamp(img.Height - 1 - (int)((pdfSetPoint + maxError - minPdf1) / range * img.Height), 0, img.Height), Color.Green); //Grenze
                img.SetPixel(x, MathExtensions.Clamp(img.Height - 1 - (int)((pdfValues[x] - minPdf1) / range * img.Height), 0, img.Height), Color.Red); //Istwert

                //if (Math.Abs(directionToPoint * sphere.Normale) > 0.001f)
                {
                    double error1 = Math.Abs(pdfSetPoint - pdfValues[x]);
                    if (error1 > error) error = error1;
                }                    
            }

            //return new TestResult() { Image = img, TestWasOk = maxPdf < 1 + maxError };
            return new TestResult() { Image = img, TestWasOk = error < maxError, Error = error };
        }


        public static TestResult CompareSamplePdfWithPdfReturnValue(BrdfBasisFunction material, int sampleCount = 1000000)
        {
            double maxError = 0.5f;

            if (material == BrdfBasisFunction.Mirror ||
                material == BrdfBasisFunction.Glas) return new TestResult() { TestWasOk = true };  //Speculare Materialien sampeln keine Richtung

            SphericalCoordinateConverter sphere = new SphericalCoordinateConverter();

            DirectionChunkTable<BrdfHistogramEntry> histogram = new DirectionChunkTable<BrdfHistogramEntry>(256, 256, sphere);

            Vector3D directionToPoint = -sphere.ToWorldDirection(new SphericalCoordinate(0, 45 / 180.0 * Math.PI));

            double dPhi = 2 * Math.PI / histogram.Data.GetLength(0);
            double dTheta = Math.PI / histogram.Data.GetLength(1);

            IBrdf brdfPoint = CreateBrdf(sphere, material, directionToPoint);

            Random rand = new Random(0);
            for (int i = 0; i < sampleCount; i++)
            {
                double d1 = rand.NextDouble(), d2 = rand.NextDouble(), d3 = rand.NextDouble();
                var result = brdfPoint.SampleDirection(directionToPoint, d1, d2, d3);
                if (result == null || result.BrdfWeightAfterSampling.X == 0) continue;
                var e = histogram[result.SampledDirection];
                float cosTheta = result.SampledDirection * sphere.Normal;
                if (result.RayWasRefracted == false && cosTheta < 0) throw new Exception("Bei einer Reflektion darf der Outvektor die Fläche nicht durchstoßen");
                e.Count++;
                e.PdfWSum += brdfPoint.PdfW(directionToPoint, result.SampledDirection);

                double differentialSolidAngle = histogram.GetDifferentialSolidAngle(result.SampledDirection);
                if (differentialSolidAngle != 0)
                    e.PdfHistogram += 1.0 / sampleCount / differentialSolidAngle;
            }

            List<HistogramResultValue> valueList = new List<HistogramResultValue>();
            HistogramResultValue[,] valueGrid = new HistogramResultValue[histogram.Data.GetLength(0), histogram.Data.GetLength(1)];
            double[] errorsForEachPhi = new double[histogram.Data.GetLength(0)];

            for (int phi = 0; phi < histogram.Data.GetLength(0); phi++)
            {
                double error = double.MinValue;
                for (int theta = 1; theta < histogram.Data.GetLength(1); theta++)
                {
                    var h = histogram.Data[phi, theta];
                    //var brdfResult = BrdfAbfrage.GetResult(directionToPoint, brdfPoint, sphere.ToWorldDirection(new SphericalCoordinate(phi / (double)histogram.Data.GetLength(0) * (2 * Math.PI), theta / (double)histogram.Data.GetLength(1) * (1 * Math.PI))));
                    Vector3D outDirection = sphere.ToWorldDirection(new SphericalCoordinate(phi / (double)histogram.Data.GetLength(0) * (2 * Math.PI), theta / (double)histogram.Data.GetLength(1) * (1 * Math.PI)));
                    float pdfW = brdfPoint.PdfW(directionToPoint, outDirection);
                    double pdfBrdfAbfrage = pdfW;
                    double pdfFromHistogram = h.PdfHistogram;
                    double pdfFromBrdfSampler = h.PdfWSum / Math.Max(1, h.Count);

                    var pdfValues = new HistogramResultValue()
                    {
                        PdfBrdfAbfrage = pdfBrdfAbfrage,
                        PdfFromBrdfSampler = pdfFromBrdfSampler,
                        PdfFromHistogram = pdfFromHistogram
                    };

                    valueList.Add(pdfValues);
                    valueGrid[phi, theta] = pdfValues;

                    double diff = MathExtensions.Range(pdfBrdfAbfrage, pdfFromBrdfSampler, pdfFromHistogram);
                    if (diff > error) error = diff;
                }
                errorsForEachPhi[phi] = error;
            }

            double max = MathExtensions.Max(valueList.Max(x => x.PdfBrdfAbfrage), valueList.Max(x => x.PdfFromBrdfSampler), valueList.Max(x => x.PdfFromHistogram));
            if (max > 5) max = 5;

            Bitmap img = new Bitmap(histogram.Data.GetLength(1), 100);
            double maxDiff = double.MinValue;

            
            //for (int phi = 0; phi < histogram.Data.GetLength(0); phi++)
            for (int theta = 1; theta < histogram.Data.GetLength(1); theta++)
            {
                int phi = 0;
                //int phi = errorsForEachPhi.ToList().IndexOf(errorsForEachPhi.Max());
                //double diff = MathExtensions.Range(valueGrid[phi, theta].PdfBrdfAbfrage, valueGrid[phi, theta].PdfFromHistogram, valueGrid[phi, theta].PdfFromBrdfSampler);
                double diff = MathExtensions.Range(Math.Min(valueGrid[phi, theta].PdfFromHistogram, 5), Math.Min(valueGrid[phi, theta].PdfFromBrdfSampler, 5));
                if (diff > maxDiff) maxDiff = diff;

                img.SetPixel(theta, MathExtensions.Clamp(img.Height - 1 - (int)(valueGrid[phi, theta].PdfBrdfAbfrage / max * (img.Height - 1)), 0, img.Height), Color.Gray);     //Sollwert 1
                img.SetPixel(theta, MathExtensions.Clamp(img.Height - 1 - (int)(valueGrid[phi, theta].PdfFromBrdfSampler / max * (img.Height - 1)), 0, img.Height), Color.Blue);  //Sollwert 2
                img.SetPixel(theta, MathExtensions.Clamp(img.Height - 1 - (int)(valueGrid[phi, theta].PdfFromHistogram / max * (img.Height - 1)), 0, img.Height), Color.Red);    //Istwert 2

                img.SetPixel(theta, MathExtensions.Clamp(img.Height - 1 - (int)((valueGrid[phi, theta].PdfFromBrdfSampler - maxError) / max * (img.Height - 1)), 0, img.Height), Color.Green); //Error-Grenze
                img.SetPixel(theta, MathExtensions.Clamp(img.Height - 1 - (int)((valueGrid[phi, theta].PdfFromBrdfSampler + maxError) / max * (img.Height - 1)), 0, img.Height), Color.Green); //Error-Grenze
            }

            return new TestResult() { Image = img, TestWasOk = maxDiff < maxError, Error = maxDiff };
        }

        class BrdfHistogramEntry
        {
            public double PdfHistogram = 0;  //Pdf laut BrdfSampler ermittelt aus Ray-Direction
            public int Count = 0;
            public double PdfWSum = 0;//Pdf laut BrdfSampler ermittelt aus PdfW
        }
        class HistogramResultValue
        {
            public double PdfBrdfAbfrage;
            public double PdfFromBrdfSampler;
            public double PdfFromHistogram;
        }


        public static Bitmap GetBrdfSlope(BrdfBasisFunction material, int size)
        {
            if (material == BrdfBasisFunction.Mirror ||
                material == BrdfBasisFunction.Glas) return null;

                int thetaIStepCount = 15;
            int thetaOStepCount = 300;

            SphericalCoordinateConverter sphere = new SphericalCoordinateConverter();

            SphericalCoordinate spherical = new SphericalCoordinate(Math.PI / 2, 0);
            double thetaIMax = Math.PI;


            List<Bitmap> bitmaps = new List<Bitmap>();

            List<BrdfSlope> slopes = new List<BrdfSlope>();
            for (int ti = 0; ti < thetaIStepCount; ti++)
            {
                spherical.Theta = ti / (double)thetaIStepCount * thetaIMax;
                Vector3D wi = sphere.ToWorldDirection(spherical);

                IBrdf brdfPoint = CreateBrdf(sphere, material, wi);

                List<Vector3D> brdfSlope = new List<Vector3D>();
                for (int to = 0; to < thetaOStepCount; to++)
                {
                    float thetaO = to / (float)thetaOStepCount * (float)(2 * Math.PI);
                    Vector3D wo = new Vector3D((float)Math.Cos(thetaO), 0, (float)Math.Sin(thetaO));
 
                    Vector3D brdfVec = brdfPoint.Evaluate(wi, wo);

                    float brdf = brdfVec != null ? brdfVec.X : 0;
                    brdfSlope.Add(wo * brdf);
                }
                slopes.Add(new BrdfSlope(brdfSlope, wi));
            }

            float maxSize = slopes.Max(x => x.GetSize());
            if (maxSize == 0) return new Bitmap(1, 1);
            maxSize = 0.01f;
            slopes = slopes.Select(x => x.GetScaledSlope(1.0f / maxSize)).ToList(); 
            //slopes = slopes.Select(x => x.GetScaledSlope(1.0f / x.GetSize())).ToList();

            return BitmapHelp.TransformBitmapListToRow(slopes.Select(x => GetBrdfSlopeForInputDirection(x, size)).ToList());
        }

        class BrdfSlope
        {
            public List<Vector3D> Values { get; private set; }
            public Vector3D InputDirection { get; private set; }
            public BrdfSlope(List<Vector3D> values, Vector3D inputDirection)
            {
                this.Values = values;
                this.InputDirection = inputDirection;
            }

            public float GetSize()
            {
                return Math.Max(this.Values.Max(x => x.X) - this.Values.Min(x => x.X), this.Values.Max(x => x.Z) - this.Values.Min(x => x.Z));
            }

            public BrdfSlope GetScaledSlope(float scaleFactor)
            {
                return new BrdfSlope(this.Values.Select(x => x *= scaleFactor).ToList(), this.InputDirection);
            }
        }

        private static Bitmap GetBrdfSlopeForInputDirection(BrdfSlope brdfSlope, int size)
        {
            Bitmap img = new Bitmap(size, size);
            Graphics grx = Graphics.FromImage(img);

            Vector3D center = new Vector3D(size / 2, 0, size / 2);
            float radius = size / 2;

            grx.DrawLine(Pens.Green, ToPoint(center), ToPoint(center - brdfSlope.InputDirection * radius));
            for (int i = 0; i < brdfSlope.Values.Count - 1; i++)
            {
                grx.DrawLine(Pens.Black, ToPoint(center + brdfSlope.Values[i] * radius), ToPoint(center + brdfSlope.Values[i + 1] * radius));
            }
            grx.DrawLine(Pens.Black, ToPoint(center + brdfSlope.Values[0] * radius), ToPoint(center + brdfSlope.Values.Last() * radius));

            grx.Dispose();
            return img;
        }

        private static PointF ToPoint(Vector3D v)
        {
            return new PointF(v.X, v.Z);
        }
    }
}
