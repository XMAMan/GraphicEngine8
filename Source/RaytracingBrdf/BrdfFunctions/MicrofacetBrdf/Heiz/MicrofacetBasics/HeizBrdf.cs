using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.SlopSpace;
using System;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.MicrofacetBasics
{
    abstract class HeizBrdf
    {
        public Vector3D MacroNormal { get; private set; }
        protected float roughnessFactorX;
        protected float roughnessFactorY;
        protected ISlopeSpaceMicrofacet slopeSpaceMicrofacet;
        protected Frame frame;
        public IntersectionPoint HitPoint { get; private set; }

        public HeizBrdf(IntersectionPoint hitPoint, Vector3D macroNormal, float roughnessFactorX, float roughnessFactorY)
        {
            this.HitPoint = hitPoint;
            this.MacroNormal = macroNormal;
            this.roughnessFactorX = roughnessFactorX;
            this.roughnessFactorY = roughnessFactorY;
            this.frame = new Frame(macroNormal);
        }

        //Das ist die Funktion D(m)
        public abstract double NormalDistribution(Vector3D micronormal);

        //108 Sekunden bei Table-Size von 256
        //242 Sekunden bei Table-Size von 1024
        //Der alte Weg über SlopeSpace-Sampling von 2014 (Importance Sampling Microfacet-Based BSDFs with the Distribution of Visible Normals Supplemental Material 2_2 2014.pdf)
        //u1 und u2 müssen im Bereich von 0..1 liegen. Die Micronormale wird im WorldSpace zurück gegeben
        //Die Micronormale wird mit der Pdf(D_wi(m)) im Raumwinkelmaß zur Micronormale gesampelt Pdf_wm)
        public Vector3D SampleVisibleMicroNormal_OldWay(Vector3D directionToCamera, double u1, double u2)
        {
            Vector3D directionToCameraInLocalSpace = this.frame.ToLocal(directionToCamera);
            Vector3D localMicronormal = this.slopeSpaceMicrofacet.SampleMicronormalFromTableData(directionToCameraInLocalSpace, (float)u1, (float)u2);
            Vector3D micronormal = this.frame.ToWorld(localMicronormal);
            return micronormal;
        }

        //97 Sekunden
        //Sampling the GGX Distribution of Visible Normals Eric Heitz 2018.pdf
        //Leichters Sampling ohne SlopeSpace
        // Input U1, U2: uniform random numbers
        // Output Ne: normal sampled with PDF D_Ve(Ne) = G1(Ve) * max(0, dot(Ve, Ne)) * D(Ne) / Ve.z
        public Vector3D SampleVisibleMicroNormal(Vector3D directionToCamera, double u1, double u2)
        {
            Vector3D directionToCameraInLocalSpace = this.frame.ToLocal(directionToCamera);

            // Section 3.2: transforming the view direction to the hemisphere configuration
            Vector3D Vh = Vector3D.Normalize(new Vector3D(this.roughnessFactorX * directionToCameraInLocalSpace.X, this.roughnessFactorY * directionToCameraInLocalSpace.Y, directionToCameraInLocalSpace.Z));

            // Section 4.1: orthonormal basis
            Vector3D T1 = (Vh.Z < 0.9999f) ? Vector3D.Normalize(Vector3D.Cross(new Vector3D(0, 0, 1), Vh)) : new Vector3D(1, 0, 0);
            Vector3D T2 = Vector3D.Cross(Vh, T1);

            // Section 4.2: parameterization of the projected area
            double r = Math.Sqrt(u1);
            double phi = 2 * Math.PI * u2;
            double t1 = r * Math.Cos(phi);
            double t2 = r * Math.Sin(phi);
            double s = 0.5 * (1.0 + Vh.Z);
            t2 = (1.0 - s) * Math.Sqrt(1.0 - t1 * t1) + s * t2;

            // Section 4.3: reprojection onto hemisphere
            Vector3D Nh = (float)t1 * T1 + (float)t2 * T2 + (float)Math.Sqrt(Math.Max(0, 1.0 - t1 * t1 - t2 * t2)) * Vh;

            // Section 3.4: transforming the normal back to the ellipsoid configuration
            Vector3D Ne = Vector3D.Normalize(new Vector3D(this.roughnessFactorX * Nh.X, this.roughnessFactorY * Nh.Y, Math.Max(0, Nh.Z)));
            //return Ne;
            Vector3D micronormal = this.frame.ToWorld(Ne);
            return micronormal;
        }

        protected abstract double Lambda(Vector3D directionToLightOrCamera);

        //Masking-Function G1(m,v)
        public double G1(Vector3D micronormal, Vector3D directionToLightOrCamera)
        {
            return HeavisideFunction((directionToLightOrCamera * micronormal) / (directionToLightOrCamera * this.MacroNormal)) / (1 + Lambda(directionToLightOrCamera));
        }

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

        //Zeigt, wie viel Normalen aus Kamerarichtung sichtbar sind
        public double DistributionOfVisibleNormals(Vector3D directionToCamera, Vector3D micronormal)
        {
            return G1(micronormal, directionToCamera) * Math.Abs(directionToCamera * micronormal) * NormalDistribution(micronormal) / Math.Max(1e-6f, Math.Abs(directionToCamera * this.MacroNormal));
        }

        //Pdf with Respect to Solid Angle dP / dMicroNormal
        public double Pdf_wm(Vector3D lightGoingInDirection, Vector3D micronormal)
        {
            return DistributionOfVisibleNormals(-lightGoingInDirection, micronormal);
        }

        //Das entspricht f(i,o,n) * |o*n| / PdfW(o) 
        public double BrdfWeightAfterSampling(Vector3D i, Vector3D o, Vector3D microNormal)
        {
            //double brdfWeight = G2(microNormal, i, o) / G1(microNormal, i);
            double brdfWeight = G1(microNormal, o); //Da G2 einfach nur Faktor von G1(i)*G1(o) ist, kann ich direkt kürzen
            return brdfWeight;
        }
        protected float HeavisideFunction(double f)
        {
            return f > 0 ? 1 : 0;
        }
    }
}
