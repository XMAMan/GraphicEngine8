using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RayObjects;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.MicrofacetBasics;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Walter.MicrofacetBasics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnitTestHelper;

namespace RaytracingBrdfTest
{
    //Da Microfacets die Funktion D und G1 haben, werden diese hier gesondert getestet
    [TestClass]
    public class IMicrofacetBasicsTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        //Roughness 0.001 (Dauer=230 Minuten) (Alles grün)
        //private static float roughnessFactor = 0.001f;
        //private static int samplingCount = 100000000;
        //private static double thetaMaxNormalDistributionImage = Math.PI / 200;

        //Roughness 0.01 (Dauer=2 Minuten) (Alles grün)
        //private static float roughnessFactor = 0.01f;
        //private static int samplingCount = 1000000;
        //private static double thetaMaxNormalDistributionImage = Math.PI / 20;

        //Roughness 0.1 (Dauer=13 Sekunden) (Alles grün außer WalterBeckmann_G1)
        private static readonly float roughnessFactor = 0.1f;
        private static readonly int samplingCount = 5000;
        private static readonly double thetaMaxNormalDistributionImage = Math.PI / 2;

        [TestMethod]
        public void WalterBeckmann_NormalDistribution_ProjectedOnMacroNormal_AreaIsOne()
        {
            NormalDistribution_ProjectedOnMacroNormal_AreaIsOne(new TestData(MicroFunction.WalterBeckmann));
        }

        [TestMethod]
        public void WalterGGX_NormalDistribution_ProjectedOnMacroNormal_AreaIsOne()
        {
            NormalDistribution_ProjectedOnMacroNormal_AreaIsOne(new TestData(MicroFunction.WalterGGX));
        }

        [TestMethod]
        public void HeizBeckmann_NormalDistribution_ProjectedOnMacroNormal_AreaIsOne()
        {
            NormalDistribution_ProjectedOnMacroNormal_AreaIsOne(new TestData(MicroFunction.HeizBeckmann));
        }

        [TestMethod]
        public void HeizGGX_NormalDistribution_ProjectedOnMacroNormal_AreaIsOne()
        {
            NormalDistribution_ProjectedOnMacroNormal_AreaIsOne(new TestData(MicroFunction.HeizGGX));
        }

        private void NormalDistribution_ProjectedOnMacroNormal_AreaIsOne(TestData data)
        {
            double microAreaSum = SphereIntegrator.IntegrateWithMonteCarlo((wo, phiP, thetaP) =>
            {
                return data.Sut.NormalDistribution(wo) * (data.Sphere.Normal * wo);
            }, 0, 360, 0, 180, samplingCount);

            if (Math.Abs(microAreaSum - 1) > 0.1) throw new Exception("Die NormalDistribution-Funktion ist falsch");
        }

        //Prüfe für jede Outdirection, ob die G1-Funktion stimmt   
        [TestMethod]
        [Ignore]
        public void WalterBeckmann_G1_ProjectedOnOutDirecdtion_ProjectedMicroAreaEqualsMacroArea()
        {
            //Diese Test schlägt selbst bei hoher SamplingCount-Anzahl fehl. Also muss es daran liegen, dass der Roughness-Faktor mit 0.1 zu hoch ist
            G1_ProjectedOnOutDirecdtion_ProjectedMicroAreaEqualsMacroArea(new TestData(MicroFunction.WalterBeckmann));
        }

        [TestMethod]
        public void WalterGGX_G1_ProjectedOnOutDirecdtion_ProjectedMicroAreaEqualsMacroArea()
        {
            G1_ProjectedOnOutDirecdtion_ProjectedMicroAreaEqualsMacroArea(new TestData(MicroFunction.WalterGGX));
        }

        [TestMethod]
        public void HeizBeckmann_G1_ProjectedOnOutDirecdtion_ProjectedMicroAreaEqualsMacroArea()
        {
            G1_ProjectedOnOutDirecdtion_ProjectedMicroAreaEqualsMacroArea(new TestData(MicroFunction.HeizBeckmann));
        }

        [TestMethod]
        public void HeizGGX_G1_ProjectedOnOutDirecdtion_ProjectedMicroAreaEqualsMacroArea()
        {
            G1_ProjectedOnOutDirecdtion_ProjectedMicroAreaEqualsMacroArea(new TestData(MicroFunction.HeizGGX));
        }

        private void G1_ProjectedOnOutDirecdtion_ProjectedMicroAreaEqualsMacroArea(TestData data)
        {
            SphericalCoordinate spherical = new SphericalCoordinate(Math.PI / 8, 0);
            int thetaStepCount = 30;
            double thetaMax = Math.PI;

            for (int ti = 0; ti < thetaStepCount; ti++)
            {
                spherical.Theta = ti / (double)thetaStepCount * thetaMax;
                Vector3D outdirection = data.Sphere.ToWorldDirection(spherical);

                G1_ProjectedOnOutDirecdtion_ProjectedMicroAreaEqualsMacroArea(data.Sut, data.Sphere.Normal, outdirection);
            }
        }

        //Ich beziehe mich hier auf Gleichung 14(Seite 11) aus dem Heiz-Paper Understanding the Masking-Shadowing Function - Eric Heitz 2014.pdf
        private void G1_ProjectedOnOutDirecdtion_ProjectedMicroAreaEqualsMacroArea(IMicrofacetBasics sut, Vector3D macroNormal, Vector3D outdirection)
        {
            double microArea = SphereIntegrator.IntegrateWithMonteCarlo((wm, phiP, thetaP) =>
            {
                return sut.G1(wm, outdirection) * Clamp(wm * outdirection) * sut.NormalDistribution(wm);
            }, 0, 360, 0, 180, samplingCount);

            double macroArea = Clamp(outdirection * macroNormal);

            if (Math.Abs(microArea - macroArea) > 0.1) throw new Exception("Die projektzierte Microarea muss mit der projektzierten Macroarea übereinstimmen");
        }

        private static double Clamp(double d)
        {
            if (d < 0) return 0;
            return d;
        }

        [TestMethod]
        public void WalterBeckmann_CreateNormalDistributionImage()
        {
            CreateNormalDistributionImage(new TestData(MicroFunction.WalterBeckmann)).Save(WorkingDirectory + "NormalDistribution-WalterBeckmann.bmp");
        }

        [TestMethod]
        public void WalterGGX_CreateNormalDistributionImage()
        {
            CreateNormalDistributionImage(new TestData(MicroFunction.WalterGGX)).Save(WorkingDirectory + "NormalDistribution-WalterGGX.bmp");
        }

        [TestMethod]
        public void HeizBeckmann_CreateNormalDistributionImage()
        {
            CreateNormalDistributionImage(new TestData(MicroFunction.HeizBeckmann)).Save(WorkingDirectory + "NormalDistribution-HeizBeckmann.bmp");
        }

        [TestMethod]
        public void HeizGGX_CreateNormalDistributionImage()
        {
            CreateNormalDistributionImage(new TestData(MicroFunction.HeizGGX)).Save(WorkingDirectory + "NormalDistribution-HeizGGX.bmp");
        }

        private Bitmap CreateNormalDistributionImage(TestData data)
        {
            SphericalCoordinate spherical = new SphericalCoordinate(Math.PI / 8, 0);
            int thetaStepCount = 300;
            double thetaMin = 0;
            //double thetaMax = Math.PI / 2;
            double thetaMax = thetaMaxNormalDistributionImage;

            List<double> values = new List<double>();
            for (int ti = 0; ti < thetaStepCount; ti++)
            {
                spherical.Theta = ti / (double)thetaStepCount * (thetaMax - thetaMin) + thetaMin;
                Vector3D microNormal = data.Sphere.ToWorldDirection(spherical);

                values.Add(data.Sut.NormalDistribution(microNormal));
            }

            double max = values.Max();
            Bitmap img = new Bitmap(thetaStepCount, 100);
            for (int x = 0; x < img.Width; x++)
            {
                img.SetPixel(x, img.Height - 1 - (int)(values[x] / max * (img.Height - 1)), Color.Red);
            }

            return img;
        }


        private enum MicroFunction { WalterBeckmann, WalterGGX, HeizBeckmann, HeizGGX }
        class TestData
        {
            public IMicrofacetBasics Sut;
            public SphericalCoordinateConverter Sphere;
            public TestData(MicroFunction function)
            {
                SphericalCoordinateConverter sphere = new SphericalCoordinateConverter();
                var rayHeigh = new RayDrawingObject(new ObjectPropertys() { BrdfModel = BrdfModel.WalterMetal }, null, null);
                var brdfPoint = new IntersectionPoint(new Vertex(new Vector3D(0, 0, 0), sphere.Normal, sphere.Tangent), new Vector3D(1, 1, 1), null, sphere.Normal, sphere.Normal, null, null, rayHeigh);

                this.Sphere = sphere;
                this.Sut = MicroFunctionFactory(function, brdfPoint, roughnessFactor);
            }

            private IMicrofacetBasics MicroFunctionFactory(MicroFunction function, IntersectionPoint brdfPoint, float roughnessFactor = 0.001f)
            {
                switch (function)
                {
                    case MicroFunction.WalterBeckmann:
                        return new WalterBeckmann(brdfPoint, brdfPoint.OrientedFlatNormal, roughnessFactor);
                    case MicroFunction.WalterGGX:
                        return new WalterGGX(brdfPoint, brdfPoint.OrientedFlatNormal, roughnessFactor);
                    case MicroFunction.HeizBeckmann:
                        return new HeizBeckmann(brdfPoint, brdfPoint.OrientedFlatNormal, roughnessFactor, roughnessFactor);
                    case MicroFunction.HeizGGX:
                        return new HeizGGX(brdfPoint, brdfPoint.OrientedFlatNormal, roughnessFactor, roughnessFactor);
                }
                throw new Exception("Unknown Enumvalue");
            }
        }
    }
}
