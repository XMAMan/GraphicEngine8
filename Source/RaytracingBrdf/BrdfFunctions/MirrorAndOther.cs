using GraphicMinimal;
using IntersectionTests;

namespace RaytracingBrdf.BrdfFunctions
{
    //Anstatt ein Spiegel mit einer diffusen Fläche konstant zu mischen will ich diesmal ein Spiegel mit einer diffusen Fläche mischen, wo der Mischfaktor == dem Fresnelterm entspricht
    class MirrorAndOther : IBrdf
    {
        private IntersectionPoint point;
        private IBrdf otherBrdf;
        private float otherFactor;

        public MirrorAndOther(IntersectionPoint point, Vector3D directionToPoint, IBrdf otherBrdf)
        {
            this.point = point;
            this.otherBrdf = otherBrdf;

            float fresnel = (float)Vector3D.FresnelTerm(-directionToPoint, point.OrientedFlatNormal, 1, point.RefractionIndex); //So viel Prozent wird bei diesen Einstrahlwinkel reflektiert
            //this.otherFactor = 1 - fresnel; //Der Rest wird gebrochen/diffuse gestrecut

            this.otherFactor = (1 - fresnel) * point.Color.Max(); //Wichte den Reflectionterm mit dem Mirrorfaktor. Der Rest wird diffuse gestreut
            //this.otherFactor = (1 - fresnel) / (fresnel * point.Color.Max() + (1 - fresnel)); //Wichte den Reflectionterm mit dem Mirrorfaktor. Der Rest wird diffuse gestreut

            this.IsSpecularBrdf = otherBrdf.IsSpecularBrdf;
            this.CanCreateRefractedRays = otherBrdf.CanCreateRefractedRays;
            this.ContinuationPdf = this.otherBrdf.ContinuationPdf * this.otherFactor + this.point.Propertys.MirrorColor.Max() * (1 - this.otherFactor);
        }

        public bool IsSpecularBrdf { get; private set; }
        public bool CanCreateRefractedRays { get; private set; }
        public float ContinuationPdf { get; private set; }
        public float DiffuseFactor { get { return this.otherFactor; } }

        public Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            return this.otherBrdf.Evaluate(lightGoingInDirection, lightGoingOutDirection) * this.otherFactor;

        }

        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            return this.otherBrdf.PdfW(lightGoingInDirection, lightGoingOutDirection) * this.otherFactor;
        }

        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            if (u3 < this.otherFactor)
            {
                var sample = this.otherBrdf.SampleDirection(lightGoingInDirection, u1, u2, u3 / this.otherFactor);
                //sample.BrdfWeightAfterSampling /= this.otherFactor; //Kürzt sich gegenseitig weg
                sample.PdfW *= this.otherFactor;
                sample.PdfWReverse *= this.otherFactor;
                return sample;
            }
            else
            {
                float selectionPdf = 1 - this.otherFactor;
                return new BrdfSampleData()
                {
                    SampledDirection = Vector3D.GetReflectedDirection(lightGoingInDirection, this.point.ShadedNormal),
                    BrdfWeightAfterSampling = this.point.Propertys.MirrorColor, //Entspricht f(i,o,n) * |o*n| / PdfW(o)  (Wird vom Brdf-Sampler verwendet)
                    PdfW = selectionPdf,
                    PdfWReverse = selectionPdf
                };
            }
        }
    }
}
