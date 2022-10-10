using GraphicMinimal;
using IntersectionTests;
using System;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Walter.MicrofacetBasics
{
    public abstract class WalterBrdf
    {
        public Vector3D MacroNormal { get; private set; }
        protected float roughnessFactor;
        public IntersectionPoint HitPoint { get; private set; }

        public WalterBrdf(IntersectionPoint hitPoint, Vector3D macroNormal, float roughnessFactor)
        {
            this.HitPoint = hitPoint;
            this.MacroNormal = macroNormal;
            this.roughnessFactor = roughnessFactor;
        }

        //Das ist die Funktion D(m)
        public abstract double NormalDistribution(Vector3D micronormal);

        //u1 und u2 müssen im Bereich von 0..1 liegen. Die Micronormale wird im WorldSpace zurück gegeben
        //Die Micronormale wird mit der Pdf(D(m)|m*n|) im Raumwinkelmaß zur Micronormale gesampelt Pdf_wm)
        public abstract Vector3D SampleMicroNormal(double u1, double u2);

        //Masking-Function G1(m,v)
        public abstract double G1(Vector3D micronormal, Vector3D directionToLightOrCamera);

        //Shadowing-Masking-Function G2(m,i,o)
        public double G2(Vector3D micronormal, Vector3D directionToLight, Vector3D directionToCamera)
        {
            return G1(micronormal, directionToLight) * G1(micronormal, directionToCamera);
        }

        //Faktor, um eine Pdf_wm in eine Pdf_wo umzurechnen
        public float JacobianDeterminantForReflection(Vector3D outDirection, Vector3D micronormal)
        {
            return 1.0f / (4 * Math.Abs(outDirection * micronormal));
        }

        //Faktor, um eine Pdf_wm in eine Pdf_wo umzurechnen
        public float JacobianDeterminantForRefraction(Vector3D inDirection, Vector3D outDirection, Vector3D micronormal, float ni, float no)
        {
            float d = ni * (inDirection * micronormal) + no * (outDirection * micronormal);
            return no * no * Math.Abs(outDirection * micronormal) / (d * d);
        }

        //Pdf with Respect to Solid Angle dP / dMicroNormal
        public double Pdf_wm(Vector3D micronormal)
        {
            return NormalDistribution(micronormal) * Math.Abs(micronormal * this.MacroNormal);
        }

        //Das entspricht f(i,o,n) * |o*n| / PdfW(o) 
        public double BrdfWeightAfterSampling(Vector3D i, Vector3D o, Vector3D microNormal)
        {
            double brdfWeight = Math.Abs(i * microNormal) * G2(microNormal, i, o) / (Math.Max(1e-6f, Math.Abs(i * this.MacroNormal) * Math.Abs(microNormal * this.MacroNormal)));
            return brdfWeight;
        }

        protected float HeavisideFunction(double f)
        {
            return f > 0 ? 1 : 0;
        }
    }
}
