using GraphicMinimal;
using IntersectionTests;
using RayTracerGlobal;
using System;

namespace RaytracingBrdf.BrdfFunctions
{
    //Spiegel mit Rost. Nur wo die Textur komlett weiß ist, wird gespiegelt
    class MirrorWithRustBrdf : IBrdf
    {
        private IntersectionPoint point;
        public MirrorWithRustBrdf(IntersectionPoint point)
        {
            this.point = point;
            this.IsSpecularBrdf = point.Color == new Vector3D(1, 1, 1);
            this.DiffuseFactor = this.IsSpecularBrdf ? 0 : 1;
            this.ContinuationPdf = this.IsSpecularBrdf ? this.point.Propertys.MirrorColor.Max() * this.point.SpecularAlbedo : this.point.Color.Max() * this.point.Albedo;
        }

        public bool IsSpecularBrdf { get; private set; }
        public bool CanCreateRefractedRays { get { return false; } }
        public float ContinuationPdf { get; private set; }
        public float DiffuseFactor { get; private set; }

        //Das entspricht f(i,o,n)   (Wird vom Brdf-Abfrager verwendet)
        public Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            if (this.IsSpecularBrdf)
                throw new Exception("Diese Funktion darf in der Brdf-Abfrage nicht benutzt werden, da sie eine Dirac-Delta-Funktion enthält. Die Formel wäre hitPoint.Color*Delta(wo)/|n*wo|");
            else
                return this.point.Color / (float)Math.PI * this.point.Albedo;
        }

        //Pdf With Respect to Solid Angle dP / do
        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            if (this.IsSpecularBrdf)
                return 1;
            else
                return Math.Max(MagicNumbers.MinAllowedPdfW, Math.Max(0, this.point.OrientedFlatNormal * lightGoingOutDirection) / (float)Math.PI);
        }

        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            Vector3D sampledDirection = null;

            if (this.IsSpecularBrdf)
                sampledDirection = Vector3D.GetReflectedDirection(lightGoingInDirection, this.point.ShadedNormal);
            else
                sampledDirection = BrdfDiffuseCosinusWeighted.SampleDirection(u1, u2, this.point.OrientedFlatNormal);

            return new BrdfSampleData()
            {
                SampledDirection = sampledDirection,
                BrdfWeightAfterSampling = this.IsSpecularBrdf ? this.point.Propertys.MirrorColor * this.point.SpecularAlbedo : this.point.Color * this.point.Albedo,
                IsSpecularReflected = this.IsSpecularBrdf,
                PdfW = PdfW(lightGoingInDirection, sampledDirection),
                PdfWReverse = PdfW(-sampledDirection, -lightGoingInDirection)
            };
        }
    }
}
