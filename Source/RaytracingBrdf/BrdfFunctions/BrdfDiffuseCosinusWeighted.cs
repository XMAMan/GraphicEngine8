using GraphicMinimal;
using IntersectionTests;
using RayTracerGlobal;
using System;

namespace RaytracingBrdf.BrdfFunctions
{
    //Sampling der Brdf für diffuse Flächen für Strahlen, welche von der Kamera starten
    //Erklärtext: Raytracingmitschriften unter 'Sampeln einer Cosinus-Gewichteten Diffuse-Brdf'
    public class BrdfDiffuseCosinusWeighted : IBrdf
    {
        private readonly IntersectionPoint point;
        public BrdfDiffuseCosinusWeighted(IntersectionPoint point)
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
            return Math.Max(MagicNumbers.MinAllowedPdfW, Math.Max(0, this.point.OrientedFlatNormal * lightGoingOutDirection) / (float)Math.PI);
        }

        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            Vector3D sampledDirection = SampleDirection(u1, u2, this.point.OrientedFlatNormal);
            return new BrdfSampleData()
            {
                SampledDirection = sampledDirection,
                BrdfWeightAfterSampling = this.point.Color * this.point.Albedo, //Entspricht f(i,o,n) * |o*n| / PdfW(o)  (Wird vom Brdf-Sampler verwendet)
                PdfW = PdfW(null, sampledDirection),
                PdfWReverse = PdfW(null, -lightGoingInDirection)
            };
        }

        //u1,u2 uniform random Number in Range of 0..1
        public static Vector3D SampleDirection(double u1, double u2, Vector3D normal)
        {
            //u1 = phi / (2 PI); u2 = cos² theta

            double phi = 2 * Math.PI * u1;
            float sinTheta = (float)Math.Sqrt(1 - u2);
            float cosTheta = (float)Math.Sqrt(u2);

            Vector3D w = normal,
                   u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
                   v = Vector3D.Cross(w, u);

            Vector3D d = Vector3D.Normalize(u * (float)Math.Cos(phi) * sinTheta + v * (float)Math.Sin(phi) * sinTheta + w * cosTheta);

            return d;
        }

        public static void GetU1AndU2ForDirection(Vector3D normal, Vector3D direction, out double u1, out double u2)
        {
            //Koordinatensystem wird mit der linken Hand aufgestpannt
            Vector3D w = normal, //Linker Daumen (Zeigt anch oben)
                   v = -Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)), //Linker Zeigefinter / Binormale / Zeigt von mir weg nach vorne
                   u = -Vector3D.Cross(w, v); //Mittelfinger / Tangente / (Zeigt nach rechts)

            Vector3D localDirection = new Vector3D(direction * u,
                       direction * v,
                       direction * w);

            double phi = 0;
            if (localDirection.Z < 0.99999f)
            {
                phi = Math.Atan2(localDirection.Y, localDirection.X);
            }
            double cosTheta = localDirection.Z;

            if (phi < 0) phi += 2 * Math.PI;

            u1 = phi / (2 * Math.PI);
            u2 = cosTheta * cosTheta;
        }

        public static float PDFw(Vector3D normal, Vector3D lightGoingOutDirection)
        {
            return Math.Max(MagicNumbers.MinAllowedPdfW, Math.Max(0, normal * lightGoingOutDirection) / (float)Math.PI);
        }
    }
}
