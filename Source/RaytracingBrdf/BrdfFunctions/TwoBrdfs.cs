using GraphicMinimal;

namespace RaytracingBrdf.BrdfFunctions
{
    //Kombinatino von zwei beliebigen Brdfs
    class TwoBrdfs : IBrdf
    {
        private readonly IBrdf brdf1;
        private readonly IBrdf brdf2;
        private readonly float mix;

        //mix = 0..1; 0 = Nur brdf2; 1 = nur brdf1
        public TwoBrdfs(IBrdf brdf1, IBrdf brdf2, float mix)
        {
            this.brdf1 = brdf1;
            this.brdf2 = brdf2;
            this.mix = mix;
            this.DiffuseFactor = mix * brdf1.DiffuseFactor + (1 - mix) * brdf2.DiffuseFactor;
            this.IsSpecularBrdf = brdf1.IsSpecularBrdf && brdf2.IsSpecularBrdf;
            this.CanCreateRefractedRays = brdf1.CanCreateRefractedRays || brdf2.CanCreateRefractedRays;
            this.ContinuationPdf = mix * brdf1.ContinuationPdf + (1 - mix) * brdf2.ContinuationPdf;
        }

        public bool IsSpecularBrdf { get; private set; }
        public bool CanCreateRefractedRays { get; private set; }
        public float ContinuationPdf { get; private set; }
        public float DiffuseFactor { get; private set; }

        public Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            Vector3D sum = new Vector3D(0, 0, 0);

            if (this.brdf1.IsSpecularBrdf == false)
            {
                sum += this.mix * brdf1.Evaluate(lightGoingInDirection, lightGoingOutDirection);
            }
            if (this.brdf2.IsSpecularBrdf == false)
            {
                sum += (1 - this.mix) * brdf2.Evaluate(lightGoingInDirection, lightGoingOutDirection);
            }

            return sum;

        }

        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            float sum = 0;

            if (this.brdf1.IsSpecularBrdf == false)
            {
                sum += this.mix * brdf1.PdfW(lightGoingInDirection, lightGoingOutDirection);
            }
            if (this.brdf2.IsSpecularBrdf == false)
            {
                sum += (1 - this.mix) * brdf2.PdfW(lightGoingInDirection, lightGoingOutDirection);
            }

            return sum;
        }

        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            float selectionPdf;
            BrdfSampleData sample;
            if (u3 < this.mix)
            {
                sample = this.brdf1.SampleDirection(lightGoingInDirection, u1, u2, u3 / this.mix);
                selectionPdf = this.mix;
            }
            else
            {
                sample = this.brdf2.SampleDirection(lightGoingInDirection, u1, u2, (u3 - this.mix) / (1 - this.mix));
                selectionPdf = 1 - this.mix;
            }

            sample.PdfW *= selectionPdf;
            sample.PdfWReverse *= selectionPdf;
            return sample;
        }
    }
}
