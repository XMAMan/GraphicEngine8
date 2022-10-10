using GraphicMinimal;
using IntersectionTests;
using System;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Walter.MicrofacetBasics
{
    //Eine Microsurface-Brdf, bei der die Micronoramlen mit der Beckmann-Verteilung verteilt sind. Die Masking-Funktion G1 hängt von der Verteilungsfunktion D ab
    public class WalterBeckmann : WalterBrdf, IWalterBrdf
    {
        public WalterBeckmann(IntersectionPoint hitPoint, Vector3D macroNormal, float roughnessFactor)
            : base(hitPoint, macroNormal, roughnessFactor)
        {
        }

        //Das ist die Funktion D(m) (Isotropisch)
        public override double NormalDistribution(Vector3D micronormal)
        {
            double cosThetaMicroNormal = Math.Min(1, micronormal * this.MacroNormal);
            float x = HeavisideFunction(cosThetaMicroNormal);
            if (x == 0) return 0;
            double thetaMicroNormal = Math.Acos(cosThetaMicroNormal);
            double roughness2 = this.roughnessFactor * this.roughnessFactor;
            double cosThetaMicroNormal2 = cosThetaMicroNormal * cosThetaMicroNormal;
            double tanThetaMicroNormal = Math.Tan(thetaMicroNormal);

            double denominator = Math.PI * roughness2 * cosThetaMicroNormal2 * cosThetaMicroNormal2;
            double exp = Math.Exp(-tanThetaMicroNormal * tanThetaMicroNormal / roughness2);
            return x / Math.Max(1e-9f, denominator) * exp;
        }

        //u1 und u2 müssen im Bereich von 0..1 liegen. Die Micronormale wird im WorldSpace zurück gegeben
        //Die Micronormale wird mit der Pdf(D(m)|m*n|) im Raumwinkelmaß zur Micronormale gesampelt Pdf_wm)
        public override Vector3D SampleMicroNormal(double u1, double u2)
        {
            double tanTheta = Math.Sqrt(-this.roughnessFactor * this.roughnessFactor * Math.Log(1 - u1));
            double theta = Math.Atan(tanTheta);
            double phi = 2 * Math.PI * u2;
            double h = Math.Cos(theta);
            double r = tanTheta * h;

            Vector3D tangent = Vector3D.Normalize(Vector3D.Cross((Math.Abs(this.MacroNormal.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), this.MacroNormal));
            Vector3D binormal = Vector3D.Cross(this.MacroNormal, tangent);

            Vector3D microNormal = Vector3D.Normalize(tangent * (float)(Math.Cos(phi) * r) + binormal * (float)(Math.Sin(phi) * r) + this.MacroNormal * (float)h);
            return microNormal;
        }

        //Masking-Function G1(m)
        public override double G1(Vector3D micronormal, Vector3D directionToLightOrCamera)
        {
            float x = HeavisideFunction((directionToLightOrCamera * micronormal) / (directionToLightOrCamera * this.MacroNormal));
            if (x == 0) return 0;

            float cosThetaV = this.MacroNormal * directionToLightOrCamera;
            if (cosThetaV < 0) cosThetaV = this.MacroNormal * (-directionToLightOrCamera);
            double a = 1.0 / (this.roughnessFactor * Math.Tan(Math.Acos(cosThetaV)));

            double s = 1;
            if (a < 1.6f)
            {
                s = 3.535f * a + 2.181f * a * a / (1 + 2.276f * a + 2.577f * a * a);
            }

            return (x * s);
        }
    }
}
