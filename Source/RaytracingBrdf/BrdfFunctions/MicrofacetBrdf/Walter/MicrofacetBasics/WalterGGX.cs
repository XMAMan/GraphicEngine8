﻿using GraphicMinimal;
using IntersectionTests;
using System;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Walter.MicrofacetBasics
{
    //Eine Microsurface-Brdf, bei der die Micronoramlen mit der GGX-Verteilung verteilt sind. Die Masking-Funktion G1 hängt von der Verteilungsfunktion D ab
    public class WalterGGX : WalterBrdf, IWalterBrdf
    {
        public WalterGGX(IntersectionPoint hitPoint, Vector3D macroNormal, float roughnessFactor)
            : base(hitPoint, macroNormal, roughnessFactor)
        {
        }

        //Das ist die Funktion D(m) (Isotropisch)
        public override double NormalDistribution(Vector3D micronormal)
        {
            //Isotrophic (Walter)
            double cosThetaMicroNormal = Math.Min(1, micronormal * this.MacroNormal);
            float x = HeavisideFunction(cosThetaMicroNormal);
            if (x == 0) return 0;
            double thetaMicroNormal = Math.Acos(cosThetaMicroNormal);
            double roughness2 = this.roughnessFactor * this.roughnessFactor;
            double cosThetaMicroNormal2 = cosThetaMicroNormal * cosThetaMicroNormal;
            double tanThetaMicroNormal = Math.Tan(thetaMicroNormal);
            double d2 = roughness2 + tanThetaMicroNormal * tanThetaMicroNormal;
            double denominator = Math.PI * cosThetaMicroNormal2 * cosThetaMicroNormal2 * d2 * d2;
            return roughness2 * x / Math.Max(1e-9f, denominator); //Die denominator-Zahl hat ein Rieseneinfluß auf das Aussehen. Wenn sie gegen 0 geht, dann kommen sehr große Pdf/Brdf-Werte raus

            //Isotrophic (Heiz)
            //double cosThetaMicroNormal = Math.Min(1, micronormal * this.MacroNormal);
            //float x = HeavisideFunction(cosThetaMicroNormal);
            //if (x == 0) return 0;
            //double thetaMicroNormal = Math.Acos(cosThetaMicroNormal);
            //double roughness2 = this.roughnessFactor * this.roughnessFactor;
            //double cosThetaMicroNormal2 = cosThetaMicroNormal * cosThetaMicroNormal;
            //double tanThetaMicroNormal = Math.Tan(thetaMicroNormal);
            //double d2 = 1 + tanThetaMicroNormal * tanThetaMicroNormal / roughness2;
            //double denominator = Math.PI * roughness2 * cosThetaMicroNormal2 * cosThetaMicroNormal2 * d2 * d2;
            //return x / Math.Max(1e-9f, denominator); //Die denominator-Zahl hat ein Rieseneinfluß auf das Aussehen. Wenn sie gegen 0 geht, dann kommen sehr große Pdf/Brdf-Werte raus
        }

        //u1 und u2 müssen im Bereich von 0..1 liegen. Die Micronormale wird im WorldSpace zurück gegeben
        //Die Micronormale wird mit der Pdf(D(m)|m*n|) im Raumwinkelmaß zur Micronormale gesampelt Pdf_wm)
        public override Vector3D SampleMicroNormal(double u1, double u2)
        {
            double tanTheta = this.roughnessFactor * Math.Sqrt(u1) / Math.Sqrt(1 - u1);
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

            //return (float)(x / (1 + Lambda(Math.Acos(cosThetaV))));

            if (cosThetaV < 0) cosThetaV = this.MacroNormal * (-directionToLightOrCamera);
            double tanThetaV = Math.Tan(Math.Acos(cosThetaV));
            if (double.IsNaN(tanThetaV)) return 1;
            return (x * 2 / (1 + Math.Sqrt(1 + this.roughnessFactor * this.roughnessFactor * tanThetaV * tanThetaV)));
        }

        //private double Lambda(double theta)
        //{
        //    double a = 1.0 / (this.roughnessFactor * Math.Tan(theta));
        //    return (-1 + Math.Sqrt(1 + 1 / (a * a))) / 2;
        //}
    }
}
