using GraphicMinimal;
using IntersectionTests;
using RayTracerGlobal;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.MicrofacetBasics;
using System;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz
{
    //Ein Microsurface-Spiegel welcher über die Beckmann- oder GGX-Microsurface-Normalenverteilung benutzt werden kann
    class HeizMirror : IBrdf
    {
        private readonly IHeizBrdf brdf;

        public HeizMirror(IntersectionPoint hitPoint, Vector3D directionToPoint)
        {
            Vector2D roughness = hitPoint.Propertys.NormalSource.As<NormalFromMicrofacet>().MicrofacetRoughness;
            float roughnessFactorX = roughness.X;
            float roughnessFactorY = roughness.Y;

            if (hitPoint.BumpmapColor != null)
            {
                roughnessFactorX = hitPoint.BumpmapColor.X / 10;
                roughnessFactorY = hitPoint.BumpmapColor.Y / 10;
                //roughnessFactorX = roughnessFactorY = (1 - hitPoint.BumpmapColor.X) / 10;
            }

            Vector3D normal = hitPoint.ShadedNormal;

            //this.brdf = new HeizBeckmann(hitPoint, (directionToPoint * hitPoint.Normal > 0) ? -hitPoint.Normal : hitPoint.Normal, roughnessFactorX, roughnessFactorY);
            this.brdf = new HeizGGX(hitPoint, (directionToPoint * normal > 0) ? -normal : normal, roughnessFactorX, roughnessFactorY);
            this.ContinuationPdf = hitPoint.Color.Max() * hitPoint.SpecularAlbedo;
        }

        public bool IsSpecularBrdf { get { return false; } }
        public bool CanCreateRefractedRays { get { return false; } }
        public float ContinuationPdf { get; private set; }
        public float DiffuseFactor { get { return 0; } }

        //Das entspricht f(i,o,n)   (Wird vom Brdf-Abfrager verwendet)
        public Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            Vector3D i = -lightGoingInDirection;
            Vector3D o = lightGoingOutDirection;
            Vector3D microNormal = Vector3D.Normalize(i + o);
            double brdfWeight = this.brdf.G2(microNormal, i, o) * this.brdf.NormalDistribution(microNormal) / Math.Max(1e-6f, (4 * Math.Abs(i * this.brdf.MacroNormal) * Math.Abs(o * this.brdf.MacroNormal)));
            if (brdfWeight > 2) brdfWeight = 2;
            return this.brdf.HitPoint.Color * (float)brdfWeight * this.brdf.HitPoint.SpecularAlbedo;
        }

        //Pdf With Respect to Solid Angle dP / do
        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            Vector3D microNormal = Vector3D.Normalize((-lightGoingInDirection) + lightGoingOutDirection);
            float pdfW = Math.Max(MagicNumbers.MinAllowedPdfW, (float)(this.brdf.Pdf_wm(lightGoingInDirection, microNormal) * this.brdf.JacobianDeterminantForReflection(lightGoingOutDirection, microNormal)));
            //if (pdfW > 5) pdfW = 5; //Der PdfW_CalledMultipeTimes_SumIsOne_HeizMirror-Test wird rot, wenn ich diese Zeile hier drin habe
            return pdfW;
        }

        //u1 und u2 müssen im Bereich von 0..1 liegen
        //brdfWithCosThetaOutAndPdfW entspricht f(i,o,n) * |o*n| / PdfW(o) 
        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            Vector3D microNormal = this.brdf.SampleVisibleMicroNormal(-lightGoingInDirection, u1, u2);
            Vector3D i = -lightGoingInDirection;
            Vector3D o = Vector3D.GetReflectedDirection(lightGoingInDirection, microNormal);

            double brdfWeight = this.brdf.BrdfWeightAfterSampling(i, o, microNormal);

            if (brdfWeight == 0) return null;
            //float inDot = (-lightGoingInDirection) * this.brdf.MacroNormal;
            //float outDot = o * this.brdf.MacroNormal;
            //bool inAndOutOnDifferentSides = (inDot < 0.0) ^ (outDot < 0.0);

            return new BrdfSampleData()
            {
                BrdfWeightAfterSampling = this.brdf.HitPoint.Color * (float)brdfWeight * this.brdf.HitPoint.SpecularAlbedo,
                //BrdfWeightAfterSampling = this.brdf.HitPoint.Propertys.MirrorColor * (float)brdfWeight,
                SampledDirection = o,
                PdfW = PdfW(lightGoingInDirection, o),
                PdfWReverse = PdfW(-o, -lightGoingInDirection)
            };
        }
    }
}
