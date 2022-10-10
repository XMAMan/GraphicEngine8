using GraphicMinimal;
using IntersectionTests;
using System;

namespace RaytracingBrdf.BrdfFunctions
{
    //Sampling für Spiegel-Flächen
    class BrdfMirror : IBrdf
    {
        private readonly IntersectionPoint point;
        private readonly bool useTextur;
        public BrdfMirror(IntersectionPoint point, bool useTextur)
        {
            this.point = point;
            this.useTextur = useTextur;
            this.ContinuationPdf = useTextur ? point.Color.Max() : point.Propertys.MirrorColor.Max() * this.point.SpecularAlbedo;
        }

        public bool IsSpecularBrdf { get { return true; } }
        public bool CanCreateRefractedRays { get { return false; } }
        public float ContinuationPdf { get; private set; }
        public float DiffuseFactor { get { return 0; } }

        //Das entspricht f(i,o,n)   (Wird vom Brdf-Abfrager verwendet)
        public Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            throw new Exception("Diese Funktion darf in der Brdf-Abfrage nicht benutzt werden, da sie eine Dirac-Delta-Funktion enthält. Die Formel wäre hitPoint.Color*Delta(wo)/|n*wo|");
        }

        //Pdf With Respect to Solid Angle dP / do
        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            return 1;
        }

        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            return new BrdfSampleData()
            {
                //SampledDirection = Vector3D.GetReflectedDirection(lightGoingInDirection, this.point.OrientedFlatNormal),
                SampledDirection = Vector3D.GetReflectedDirection(lightGoingInDirection, this.point.ShadedNormal),
                BrdfWeightAfterSampling = this.useTextur ? this.point.Color * this.point.SpecularAlbedo : this.point.Propertys.MirrorColor * this.point.SpecularAlbedo,
                //BrdfWeightAfterSampling = this.point.Color, //Entspricht f(i,o,n) * |o*n| / PdfW(o)  (Wird vom Brdf-Sampler verwendet)
                //BrdfWeightAfterSampling = this.point.Propertys.MirrorColor,
                IsSpecularReflected = true,
                PdfW = 1,
                PdfWReverse = 1
            };
        }
    }
}
