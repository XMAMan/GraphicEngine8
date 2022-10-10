using GraphicMinimal;
using IntersectionTests;
using RayTracerGlobal;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Walter.MicrofacetBasics;
using System;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Walter
{
    //Ein Microsurface-Spiegel welcher über die Beckmann- oder GGX-Microsurface-Normalenverteilung benutzt werden kann
    class WalterMirror : IBrdf
    {
        private readonly IWalterBrdf brdf;

        public bool IsSpecularBrdf { get { return false; } }
        public bool CanCreateRefractedRays { get { return false; } }
        public float ContinuationPdf { get; private set; }
        public float DiffuseFactor { get { return 0; } }

        public WalterMirror(IntersectionPoint hitPoint, Vector3D directionToPoint, float roughnessFactor = 0.1f)
        {
            //this.brdf = new WalterBeckmann(hitPoint, (directionToPoint * hitPoint.Normal > 0) ? -hitPoint.Normal : hitPoint.Normal, roughnessFactor);
            this.brdf = new WalterGGX(hitPoint, (directionToPoint * hitPoint.OrientedFlatNormal > 0) ? -hitPoint.OrientedFlatNormal : hitPoint.OrientedFlatNormal, roughnessFactor);
            this.ContinuationPdf = hitPoint.Color.Max() * hitPoint.SpecularAlbedo;
        }

        //Das entspricht f(i,o,n)   (Wird vom Brdf-Abfrager verwendet)
        public Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            Vector3D i = -lightGoingInDirection;
            Vector3D o = lightGoingOutDirection;
            Vector3D microNormal = Vector3D.Normalize(i + o);
            double brdfWeight = this.brdf.G2(microNormal, i, o) * (float)this.brdf.NormalDistribution(microNormal) / Math.Max(1e-6f, (4 * Math.Abs(i * this.brdf.MacroNormal) * Math.Abs(o * this.brdf.MacroNormal)));
            return this.brdf.HitPoint.Color * (float)brdfWeight;
        }

        //Pdf With Respect to Solid Angle dP / do
        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            if (lightGoingOutDirection * this.brdf.MacroNormal < 0) return 0;
            Vector3D microNormal = Vector3D.Normalize((-lightGoingInDirection) + lightGoingOutDirection);
            return Math.Max(MagicNumbers.MinAllowedPdfW, (float)(this.brdf.Pdf_wm(microNormal) * this.brdf.JacobianDeterminantForReflection(lightGoingOutDirection, microNormal)));
        }

        //u1 und u2 müssen im Bereich von 0..1 liegen
        //brdfWithCosThetaOutAndPdfW entspricht f(i,o,n) * |o*n| / PdfW(o) 
        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            Vector3D i = -lightGoingInDirection;
            Vector3D microNormal = this.brdf.SampleMicroNormal(u1, u2);
            Vector3D o = Vector3D.GetReflectedDirection(lightGoingInDirection, microNormal);

            double brdfWeight = this.brdf.BrdfWeightAfterSampling(i, o, microNormal);
            if (brdfWeight == 0) return null;
            float f = o * this.brdf.MacroNormal > 0 ? 1 : 0;

            return new BrdfSampleData()
            {
                BrdfWeightAfterSampling = this.brdf.HitPoint.Color * (float)brdfWeight * f * this.brdf.HitPoint.SpecularAlbedo,
                SampledDirection = o,
                PdfW = PdfW(lightGoingInDirection, o),
                PdfWReverse = PdfW(-o, -lightGoingInDirection)
            };
        }
    }
}
