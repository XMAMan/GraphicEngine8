using GraphicMinimal;
using IntersectionTests;
using System;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.MicrofacetBasics
{
    //Eine Microsurface-Brdf, bei der die Micronoramlen mit der GGX-Verteilung verteilt sind. Die Masking-Funktion G1 hängt von der Verteilungsfunktion D ab
    class HeizGGX : HeizBrdf, IHeizBrdf
    {
        public HeizGGX(IntersectionPoint hitPoint, Vector3D macroNormal, float roughnessFactorX, float roughnessFactorY)
            : base(hitPoint, macroNormal, roughnessFactorX, roughnessFactorY)
        {
            //this.slopeSpaceMicrofacet = new SlopeSpaceSmithMicrofacet(new GgxSlopeDistribution(), roughnessFactorX, roughnessFactorY);
            //this.slopeSpaceMicrofacet = SlopeSpaceFactory.Build(roughnessFactorX, roughnessFactorY); //Brauche ich nicht mehr seit der neuen VisibleNormal-Sampling-Methode von Heitz 2018
        }

        //Das ist die Funktion D(m) (Anisotrophisch)
        public override double NormalDistribution(Vector3D micronormal)
        {
            //Anisotrophic (Heiz)
            double cosThetaMicroNormal = Math.Min(1, micronormal * this.MacroNormal);
            float x = HeavisideFunction(cosThetaMicroNormal);
            if (x == 0) return 0;
            double thetaMicroNormal = Math.Acos(cosThetaMicroNormal);
            double roughness2 = this.roughnessFactorX * this.roughnessFactorY;
            double cosThetaMicroNormal2 = cosThetaMicroNormal * cosThetaMicroNormal;
            double tanThetaMicroNormal = Math.Tan(thetaMicroNormal);

            double phi = Math.Atan2(micronormal.Y, micronormal.X);
            double cosPhi = Math.Cos(phi);
            double sinPhi = Math.Sin(phi);
            double phiSum = cosPhi * cosPhi / (this.roughnessFactorX * this.roughnessFactorX) + sinPhi * sinPhi / (this.roughnessFactorY * this.roughnessFactorY);

            double d2 = 1 + tanThetaMicroNormal * tanThetaMicroNormal * phiSum;
            double denominator = Math.PI * roughness2 * cosThetaMicroNormal2 * cosThetaMicroNormal2 * d2 * d2;
            return x / Math.Max(1e-9f, denominator);

            //Isotrophic (Walter)
            //double cosThetaMicroNormal = Math.Min(1, micronormal * this.MacroNormal);
            //float x = HeavisideFunction(cosThetaMicroNormal);
            //if (x == 0) return 0;
            //double thetaMicroNormal = Math.Acos(cosThetaMicroNormal);
            //double roughness2 = this.roughnessFactorX * this.roughnessFactorX;
            //double cosThetaMicroNormal2 = cosThetaMicroNormal * cosThetaMicroNormal;
            //double tanThetaMicroNormal = Math.Tan(thetaMicroNormal);
            //double d2 = roughness2 + tanThetaMicroNormal * tanThetaMicroNormal;
            //double denominator = Math.PI * cosThetaMicroNormal2 * cosThetaMicroNormal2 * d2 * d2;
            //return roughness2 * x / Math.Max(1e-9f, denominator); //Die denominator-Zahl hat ein Rieseneinfluß auf das Aussehen. Wenn sie gegen 0 geht, dann kommen sehr große Pdf/Brdf-Werte raus

            //Isotrophic (Heiz)
            //double cosThetaMicroNormal = Math.Min(1, micronormal * this.MacroNormal);
            //float x = HeavisideFunction(cosThetaMicroNormal);
            //if (x == 0) return 0;
            //double thetaMicroNormal = Math.Acos(cosThetaMicroNormal);
            //double roughness2 = this.roughnessFactorX * this.roughnessFactorX;
            //double cosThetaMicroNormal2 = cosThetaMicroNormal * cosThetaMicroNormal;
            //double tanThetaMicroNormal = Math.Tan(thetaMicroNormal);             
            //double d2 = 1 + tanThetaMicroNormal * tanThetaMicroNormal / roughness2;
            //double denominator = Math.PI * roughness2 * cosThetaMicroNormal2 * cosThetaMicroNormal2 * d2 * d2;
            //return x / Math.Max(1e-9f, denominator); //Die denominator-Zahl hat ein Rieseneinfluß auf das Aussehen. Wenn sie gegen 0 geht, dann kommen sehr große Pdf/Brdf-Werte raus

        }

        //Helperfunction for the Masking-Function G1(m)
        protected override double Lambda(Vector3D directionToLightOrCamera)
        {
            //Anisotrophic
            //double cosTheta = Math.Max(-1, Math.Min(1, directionToLightOrCamera * this.MacroNormal));
            //double theta = Math.Acos(cosTheta);
            //double phi = Math.Atan2(directionToLightOrCamera.Y, directionToLightOrCamera.X);
            //double tanTheta = Math.Tan(theta);
            //double cosPhi = Math.Cos(phi);
            //double sinPhi = Math.Sin(phi);
            //double a0 = Math.Sqrt(cosPhi * cosPhi * this.roughnessFactorX * this.roughnessFactorX + sinPhi * sinPhi * this.roughnessFactorY * this.roughnessFactorY);
            //double a = 1 / (a0 * tanTheta);
            //double lambda = (-1 + Math.Sqrt(1 + 1 / (a * a))) / 2;
            //return lambda;

            //Isotrophic
            double cosTheta = Math.Max(-1, Math.Min(1, directionToLightOrCamera * this.MacroNormal));
            double theta = Math.Acos(cosTheta);
            double tanTheta = Math.Tan(theta);
            double a0 = this.roughnessFactorX;
            double a = 1 / (a0 * tanTheta);
            double lambda = (-1 + Math.Sqrt(1 + 1 / (a * a))) / 2;
            return lambda;
        }
    }
}
