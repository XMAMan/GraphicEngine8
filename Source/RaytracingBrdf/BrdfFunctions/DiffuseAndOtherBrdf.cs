using GraphicMinimal;
using IntersectionTests;

namespace RaytracingBrdf.BrdfFunctions
{
    //Eine Brdf, welche sich aus einer diffusen, und einer beliebig anderen Brdf zusammen setzt. Das Mischverhältnis zwischen den beiden wird über FlieseDiffuseFactor bestimmt) 
    class DiffuseAndOtherBrdf : IBrdf
    {
        private readonly IBrdf diffuseBrdf;
        public IBrdf OtherBrdf { get; private set; }
        public bool CanCreateRefractedRays { get; private set; }
        public DiffuseAndOtherBrdf(IntersectionPoint point, IBrdf otherBrdf, float diffuseFactor)
        {
            this.OtherBrdf = otherBrdf;
            this.diffuseBrdf = new BrdfDiffuseCosinusWeighted(point);
            this.DiffuseFactor = diffuseFactor;
            this.CanCreateRefractedRays = otherBrdf.CanCreateRefractedRays;

            this.ContinuationPdf = diffuseFactor * this.diffuseBrdf.ContinuationPdf + (1 - diffuseFactor) * otherBrdf.ContinuationPdf;
        }

        public bool IsSpecularBrdf { get { return false; } }
        public float ContinuationPdf { get; private set; }
        public float DiffuseFactor { get; private set; }

        public Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            if (this.OtherBrdf.IsSpecularBrdf)
            {
                return this.diffuseBrdf.Evaluate(lightGoingInDirection, lightGoingOutDirection) * this.DiffuseFactor; //Mit DiffusePortion-Wichtung
            }
            else
            {
                return this.diffuseBrdf.Evaluate(lightGoingInDirection, lightGoingOutDirection) * this.DiffuseFactor + this.OtherBrdf.Evaluate(lightGoingInDirection, lightGoingOutDirection) * (1 - this.DiffuseFactor);
            }

        }

        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            if (this.OtherBrdf.IsSpecularBrdf)
            {
                return this.diffuseBrdf.PdfW(lightGoingInDirection, lightGoingOutDirection) * this.DiffuseFactor;
            }
            else
            {
                return this.diffuseBrdf.PdfW(lightGoingInDirection, lightGoingOutDirection) * this.DiffuseFactor + this.OtherBrdf.PdfW(lightGoingInDirection, lightGoingOutDirection) * (1 - this.DiffuseFactor);
            }
        }

        //Mit DiffusePortion-Wichtung. Selection-Pdf-Faktor kürzt sich mit Selection-Pdf weg. Somit muss BrdfWeightAfterSampling nicht durch die SelectionPdf dividiert werden
        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            float selectionPdf;
            BrdfSampleData sample;

            if (u3 < this.DiffuseFactor)
            {
                sample = this.diffuseBrdf.SampleDirection(lightGoingInDirection, u1, u2, u3 / this.DiffuseFactor);
                selectionPdf = this.DiffuseFactor;
            }
            else
            {
                sample = this.OtherBrdf.SampleDirection(lightGoingInDirection, u1, u2, (u3 - this.DiffuseFactor) / (1 - this.DiffuseFactor));
                if (sample == null) return null;
                selectionPdf = 1 - this.DiffuseFactor;
            }

            sample.PdfW *= selectionPdf;
            sample.PdfWReverse *= selectionPdf;
            return sample;
        }
    }
}
