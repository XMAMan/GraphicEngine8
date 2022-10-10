using GraphicMinimal;
using IntersectionTests;
using RayTracerGlobal;
using System;

namespace RaytracingBrdf.BrdfFunctions
{
    //Brdf für diffuse Flächen für Strahlen, welche von der Lichtquelle starten
    //Wenn ich die Photonmap bei Szene 1 mir anzeige, dann sieht sie mit Unifomren Sampling genau so aus wie wenn ich Cosinus-Gewichtetes Sampling verwende. D.h. das hier scheint zu stimmen
    //Siehe Erklärtext aus mein Raytracermitschrifen unter 'Uniformes Sampeln eines Richtungsvektors innerhalb einer Halbkugel'
    public class BrdfDiffuseUniformWeighted : IBrdf
    {
        private readonly IntersectionPoint point;
        public BrdfDiffuseUniformWeighted(IntersectionPoint point)
        {
            this.point = point;
            this.ContinuationPdf = point.Color.Max() * this.point.Albedo;
        }

        public bool IsSpecularBrdf { get { return false; } }
        public bool CanCreateRefractedRays { get { return false; } }
        public float ContinuationPdf { get; private set; }
        public float DiffuseFactor { get { return 1; } }

        //Das entspricht f(i,o,n)   (Wird vom Brdf-Abfrager verwendet)
        public Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            return this.point.Color / (float)Math.PI * this.point.Albedo;
        }

        //Pdf With Respect to Solid Angle dP / do
        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            return 1.0f / (2 * (float)Math.PI);
        }

        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            Vector3D sampledDirection = SampleDirection(u1, u2, this.point.OrientedFlatNormal);
            float pdfW = PdfW(null, null);
            return new BrdfSampleData()
            {
                SampledDirection = sampledDirection,
                BrdfWeightAfterSampling = BrdfSamplingWeight(this.point, sampledDirection),
                PdfW = pdfW,
                PdfWReverse = pdfW
            };
        }

        //u1,u2 uniform random Number in Range of 0..1
        public static Vector3D SampleDirection(double u1, double u2, Vector3D normal)
        {
            //u1 = phi / (2 PI); u2 = cos(theta)

            u1 *= 2 * Math.PI;
            float sinTheta = (float)Math.Sqrt(1 - u2 * u2);

            Vector3D w = normal,
                   u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
                   v = Vector3D.Cross(w, u);

            Vector3D d = Vector3D.Normalize((u * (float)Math.Cos(u1) * sinTheta + v * (float)Math.Sin(u1) * sinTheta + w * (float)u2));

            return d;
        }

        //Entspricht f(i,o,n) * |o*n| / PdfW(o)  (Wird vom Brdf-Sampler verwendet)
        private static Vector3D BrdfSamplingWeight(IntersectionPoint hitPoint, Vector3D lightGoingOutDirection)
        {
            return hitPoint.Color * Math.Max(MagicNumbers.MinAllowedPdfW, Math.Abs(hitPoint.ShadedNormal * lightGoingOutDirection) * 2) * hitPoint.Albedo;
        }
    }
}
