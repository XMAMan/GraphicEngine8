using BitmapHelper;
using GraphicMinimal;
using IntersectionTests;
using RayTracerGlobal;
using System;

namespace RaytracingBrdf.BrdfFunctions
{
    //Nur der helle Fleck
    class BrdfSpecularHighlight : IBrdf
    {
        private readonly IntersectionPoint point;
        private readonly bool useSamplingBrdf;
        public BrdfSpecularHighlight(IntersectionPoint point, bool useSamplingBrdf)
        {
            this.point = point;
            this.useSamplingBrdf = useSamplingBrdf;
            this.ContinuationPdf = point.Propertys.MirrorColor.Max() * this.point.SpecularAlbedo;
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

            float rho = (this.point.SpecularHighlightPowExponent + 2) * 0.5f / (float)Math.PI;
            float f = Math.Min(this.point.SpecularHighlightCutoff1, rho * (float)Math.Pow(dot_R_wi, this.point.SpecularHighlightPowExponent)) / this.point.SpecularHighlightCutoff2;

            //return new Vector3D(1, 1, 1) * f;

            Vector3D specularColor = this.point.Propertys.MirrorColor;
            return new Vector3D(PixelHelper.Lerp(specularColor.X, 1, f),
                              PixelHelper.Lerp(specularColor.Y, 1, f),
                              PixelHelper.Lerp(specularColor.Z, 1, f)) * f * this.point.SpecularHighlightCutoff1 * this.point.SpecularAlbedo;
        }

        //Pdf With Respect to Solid Angle dP / do
        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            //return 1; //Glanzpunkt ist sonst heller als der Diffuseanteil, wenn ich durch die Glossy-Pdf teile
            //return 0.3f / (float)Math.PI; //Dieser Wert müsste es laut BrdfTester sein aber der Pathtracer zeigt dann eine zu helle blaue Kugel

            Vector3D perfektReflection = Vector3D.GetReflectedDirection(lightGoingInDirection, this.point.ShadedNormal);
            float cosThetaR = Math.Max(1e-6f, perfektReflection * lightGoingOutDirection);
            return Math.Max(MagicNumbers.MinAllowedPdfW, (float)((this.point.SpecularHighlightPowExponent + 1) * Math.Pow(cosThetaR, this.point.SpecularHighlightPowExponent) * ((1 / Math.PI) * 0.5f)));

        }

        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            Vector3D sampledDirection = BrdfGlossy.SampleDirection(u1, u2, lightGoingInDirection, this.point.ShadedNormal, this.point.SpecularHighlightPowExponent);
            if (sampledDirection * this.point.ShadedNormal < 0) return null; //point.Normale kann aus Normalmap kommen. Bei diesen Test soll aber sichergestellt werden, dass reflektierter Vektor nichts ins Objekt reinfliegt. Das ist nur durch FlatNormale sicher gestellt
            return new BrdfSampleData()
            {
                SampledDirection = sampledDirection,
                BrdfWeightAfterSampling = this.useSamplingBrdf ? BrdfSamplingWeight(this.point, lightGoingInDirection, sampledDirection) * this.point.SpecularAlbedo : new Vector3D(0, 0, 0),
                PdfW = PdfW(lightGoingInDirection, sampledDirection),
                PdfWReverse = PdfW(-sampledDirection, -lightGoingInDirection)
            };
        }

        //Entspricht f(i,o,n) * |o*n| / PdfW(o)  (Wird vom Brdf-Sampler verwendet)
        private Vector3D BrdfSamplingWeight(IntersectionPoint hitPoint, Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            return Evaluate(lightGoingInDirection, lightGoingOutDirection) * Math.Abs(hitPoint.ShadedNormal * lightGoingOutDirection) / PdfW(lightGoingInDirection, lightGoingOutDirection);
        }
    }
}
