using GraphicMinimal;
using IntersectionTests;
using RayTracerGlobal;
using System;

namespace RaytracingBrdf.BrdfFunctions
{
    //Sampling für Glossy-Flächen
    //Quelle: http://www.cs.princeton.edu/courses/archive/fall03/cs526/papers/lafortune94.pdf
    class BrdfGlossy : IBrdf
    {
        private readonly IntersectionPoint point;
        public BrdfGlossy(IntersectionPoint point)
        {
            this.point = point;
            this.ContinuationPdf = point.Propertys.GlossyColor.Max() * this.point.SpecularAlbedo;
        }

        public bool IsSpecularBrdf { get { return false; } }
        public bool CanCreateRefractedRays { get { return false; } }
        public float ContinuationPdf { get; private set; }
        public float DiffuseFactor { get { return 0; } }

        //Das entspricht f(i,o,n)   (Wird vom Brdf-Abfrager verwendet)
        public Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            Vector3D perfektReflection = Vector3D.GetReflectedDirection(lightGoingInDirection, this.point.ShadedNormal);

            float dot_R_wi = perfektReflection * lightGoingOutDirection;
            if (dot_R_wi <= 1e-6f) return new Vector3D(0, 0, 0);

            float jacobi = 1 / Math.Max(1e-6f, Math.Abs(this.point.ShadedNormal * lightGoingOutDirection)); //Die cos^n-Gleichung wurde so aufgestellt, dass Alpha der Winkel zwischen o und (0,0,1) ist und nicht zwischen o und PerfektSpecular

            float rho = (this.point.GlossyPowExponent + 2) * 0.5f / (float)Math.PI;
            return this.point.Propertys.GlossyColor * rho * (float)Math.Pow(dot_R_wi, this.point.GlossyPowExponent) * jacobi * this.point.SpecularAlbedo; ; //Der Helmholz-Test schlägt fehl, wenn jacbi drin ist (oder zumindest Jacobi nur vom Outvektor abhängt) aber ohne den Faktor schlägt der Energieerhaltungssatz fehl
        }

        //Pdf With Respect to Solid Angle dP / do
        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            Vector3D perfektReflection = Vector3D.GetReflectedDirection(lightGoingInDirection, this.point.ShadedNormal);
            float cosThetaR = Math.Max(1e-6f, perfektReflection * lightGoingOutDirection);

            //float jacobi = 1 / Math.Max(1e-6f, Math.Abs(this.point.Normal * lightGoingOutDirection));

            return Math.Max(MagicNumbers.MinAllowedPdfW, (float)((this.point.GlossyPowExponent + 1) * Math.Pow(cosThetaR, this.point.GlossyPowExponent) * ((1 / Math.PI) * 0.5f)));// * jacobi;
        }

        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            Vector3D sampledDirection = SampleDirection(u1, u2, lightGoingInDirection, this.point.ShadedNormal, this.point.GlossyPowExponent);

            //float f = sampledDirection * this.point.ShadedNormal < 0 ? 0 : 1;

            return new BrdfSampleData()
            {
                SampledDirection = sampledDirection,
                //BrdfWeightAfterSampling = Brdf(lightGoingInDirection, d, true) * Math.Abs(point.Normal * d) / PdfW(lightGoingInDirection, d, true) * f,
                BrdfWeightAfterSampling = BrdfSamplingWeight(this.point, sampledDirection),// * f,
                PdfW = PdfW(lightGoingInDirection, sampledDirection),
                PdfWReverse = PdfW(-sampledDirection, -lightGoingInDirection)
            };
        }

        //u1,u2 uniform random Number in Range of 0..1
        public static Vector3D SampleDirection(double u1, double u2, Vector3D lightGoingInDirection, Vector3D normal, float power)
        {
            u1 *= 2 * Math.PI;
            Vector3D reflekt = Vector3D.GetReflectedDirection(lightGoingInDirection, normal);

            float term2 = (float)Math.Pow(u2, 1 / (power + 1));
            float term3 = (float)Math.Sqrt(1 - term2 * term2);
            Vector3D w = reflekt,
                   u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
                   v = Vector3D.Cross(w, u);

            Vector3D d = Vector3D.Normalize((u * (float)Math.Cos(u1) * term3 + v * (float)Math.Sin(u1) * term3 + w * term2));

            //if (d * normal < 0) //Wenn erzeugter Richtungsvektor durch Surface zeigt, dann Spiegel an normale-Ebene
            //{
            //    d = Vector3D.Normalize((-u * (float)Math.Cos(u1) * term3 - v * (float)Math.Sin(u1) * term3 + w * term2));
            //}

            return d;
        }

        //Entspricht f(i,o,n) * |o*n| / PdfW(o)  (Wird vom Brdf-Sampler verwendet)
        private static Vector3D BrdfSamplingWeight(IntersectionPoint hitPoint, Vector3D lightGoingOutDirection)
        {
            float jacobi = 1 / Math.Max(MagicNumbers.MinAllowedPdfW, Math.Abs(hitPoint.ShadedNormal * lightGoingOutDirection));
            return hitPoint.Propertys.GlossyColor * Math.Max(MagicNumbers.MinAllowedPdfW, Math.Min(1, Math.Abs(hitPoint.ShadedNormal * lightGoingOutDirection) * (hitPoint.GlossyPowExponent + 2) / (hitPoint.GlossyPowExponent + 1)) * jacobi) * hitPoint.SpecularAlbedo;
        }
    }
}
