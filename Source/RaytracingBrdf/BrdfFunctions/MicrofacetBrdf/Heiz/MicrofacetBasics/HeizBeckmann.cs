using GraphicGlobal.MathHelper;
using GraphicMinimal;
using IntersectionTests;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.SlopSpace;
using System;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.MicrofacetBasics
{
    //Eine Microsurface-Brdf, bei der die Micronoramlen mit der Beckmann-Verteilung verteilt sind. Die Masking-Funktion G1 hängt von der Verteilungsfunktion D ab
    class HeizBeckmann : HeizBrdf, IHeizBrdf
    {
        public HeizBeckmann(IntersectionPoint hitPoint, Vector3D macroNormal, float roughnessFactorX, float roughnessFactorY)
            : base(hitPoint, macroNormal, roughnessFactorX, roughnessFactorY)
        {
            //this.slopeSpaceMicrofacet = new SlopeSpaceSmithMicrofacet(new BeckmannSlopeDistribution(), roughnessFactorX, roughnessFactorY);
            this.slopeSpaceMicrofacet = SlopeSpaceFactory.Build(roughnessFactorX, roughnessFactorY);
        }

        //Das ist die Funktion D(m) (Anisotrophisch)
        public override double NormalDistribution(Vector3D micronormal)
        {
            double cosThetaMicroNormal = Math.Min(1, micronormal * this.MacroNormal);
            double thetaMicroNormal = Math.Acos(cosThetaMicroNormal);
            double roughness2 = this.roughnessFactorX * this.roughnessFactorY;
            float x = HeavisideFunction(cosThetaMicroNormal);
            double cosThetaMicroNormal2 = cosThetaMicroNormal * cosThetaMicroNormal;
            double tanThetaMicroNormal = Math.Tan(thetaMicroNormal);
            double phi = Math.Atan2(micronormal.Y, micronormal.X);
            double cosPhi = Math.Cos(phi);
            double sinPhi = Math.Sin(phi);
            double phiSum = cosPhi * cosPhi / (this.roughnessFactorX * this.roughnessFactorX) + sinPhi * sinPhi / (this.roughnessFactorY * this.roughnessFactorY);

            double denominator = Math.PI * roughness2 * cosThetaMicroNormal2 * cosThetaMicroNormal2;
            double exp = Math.Exp(-tanThetaMicroNormal * tanThetaMicroNormal * phiSum);
            return x / denominator * exp;
        }

        //Helperfunction for the Masking-Function G1(m)
        protected override double Lambda(Vector3D directionToLightOrCamera)
        {
            double cosTheta = Math.Min(1, directionToLightOrCamera * this.MacroNormal);
            if (cosTheta < 0) cosTheta = -cosTheta;
            double theta = Math.Acos(cosTheta);
            double phi = Math.Atan2(directionToLightOrCamera.Y, directionToLightOrCamera.X);

            double tanTheta = Math.Tan(theta);
            double cosPhi = Math.Cos(phi);
            double sinPhi = Math.Sin(phi);

            double a0 = Math.Sqrt(cosPhi * cosPhi * this.roughnessFactorX * this.roughnessFactorX + sinPhi * sinPhi * this.roughnessFactorY * this.roughnessFactorY);
            double a = 1 / (a0 * tanTheta);

            return (MathExtensions.Erf(a) - 1) / 2 + 1 / (2 * a * Math.Sqrt(Math.PI)) * Math.Exp(-a * a);
        }
    }
}
