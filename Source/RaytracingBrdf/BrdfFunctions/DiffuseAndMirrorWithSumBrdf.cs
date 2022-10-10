using GraphicMinimal;
using IntersectionTests;
using System;

namespace RaytracingBrdf.BrdfFunctions
{
    //Summe aus Diffuse-Brdf (Wird mit FlieseDiffuseFactor gewichtet) und Mirror-Brdf (MirrorColor)
    class DiffuseAndMirrorWithSumBrdf : IBrdf
    {
        private readonly IntersectionPoint point;
        private readonly IBrdf diffuseBrdf;
        private readonly float diffusePortion;

        public DiffuseAndMirrorWithSumBrdf(IntersectionPoint point)
        {
            this.point = point;
            this.diffuseBrdf = new BrdfDiffuseCosinusWeighted(point);
            this.diffusePortion = (this.point.Color.Max() * this.point.Albedo) / ((this.point.Color.Max() * this.point.Albedo) + this.point.Propertys.MirrorColor.Max());

            //this.ContinuationPdf = this.point.Color.Max() * this.diffusePortion * this.point.Albedo + this.point.Propertys.MirrorColor.Max() * (1 - this.diffusePortion) * this.point.SpecularAlbedo;
            this.ContinuationPdf = (this.point.Color.Max() * this.point.Albedo) + this.point.Propertys.MirrorColor.Max(); //Nach dieser Formel arbeitet SmallUPBP
            this.ContinuationPdf = Math.Min(1, this.ContinuationPdf);

            //Hinweis woran man sieht wie man die ContinuationPdf bestimmen sollte.
            //Das oberste Ziel beim Brdf-Sampling ist, dass das Pfadgewicht bei 1 bleibt
            //Das Pfadgewicht ergibt sich ...
            // bei Diffuse-Pfaden aus: DiffuseColor / (diffusePortion * ContinuationPdf)
            // bei Specular-Pfaden aus: MirrorColor / ((1-diffusePortion) * ContinuationPdf)

            //Bei Diffuse-Pfaden kann man das Pfadgewicht bei 1 halten indem ich "ContinuationPdf = this.point.Color.Max() + this.point.Propertys.MirrorColor.Max()" schreibe
            // DiffuseAfterSampling = DiffuseColor / (DiffuseColor.Max / (DiffuseColor.Max + MirrorColor.Max) * (DiffuseColor.Max + MirrorColor.Max))
            // DiffuseAfterSampling = DiffuseColor / DiffuseColor.Max

            //Bei Specular-Pfaden geht das auch:
            // SpecularAfterSampling = MirrorColor / ( (1 - DiffuseColor.Max / (DiffuseColor.Max + MirrorColor.Max)) * (DiffuseColor.Max + MirrorColor.Max) )
            // SpecularAfterSampling = MirrorColor / ( (DiffuseColor.Max + MirrorColor.Max) - DiffuseColor.Max )
            // SpecularAfterSampling = MirrorColor / MirrorColor.Max

            //Aus diesen Grund ist es wichtig, dass die ContinuationPdf über die SmallUPBP-Formel bestimmt wird

            this.IsSpecularBrdf = this.diffusePortion == 0;
        }

        public bool IsSpecularBrdf { get; private set; }
        public bool CanCreateRefractedRays { get { return false; } }
        public float ContinuationPdf { get; private set; }
        public float DiffuseFactor { get { return this.diffusePortion; } }

        public Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            return this.diffuseBrdf.Evaluate(lightGoingInDirection, lightGoingOutDirection); //Ohne Wichtung
        }

        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            return this.diffuseBrdf.PdfW(lightGoingInDirection, lightGoingOutDirection) * this.diffusePortion;
        }

        //Ohne Wichtung
        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            BrdfSampleData result;
            if (u3 < this.diffusePortion)
            {
                result = this.diffuseBrdf.SampleDirection(lightGoingInDirection, u1, u2, u3);
                result.BrdfWeightAfterSampling /= this.diffusePortion; //Da sich Selection-Pdf nur im Nenner und nicht im Zähler (als Brdf-Faktor) befindet, kürzt es sich nicht gegenseitig weg
                result.PdfW *= this.diffusePortion;
                result.PdfWReverse *= this.diffusePortion;
            }
            else
            {
                Vector3D lightGoingOutDirection = Vector3D.GetReflectedDirection(lightGoingInDirection, this.point.ShadedNormal);
                float selectionPdf = 1 - this.diffusePortion;
                result = new BrdfSampleData()
                {
                    SampledDirection = lightGoingOutDirection,
                    IsSpecularReflected = true,
                    BrdfWeightAfterSampling = this.point.Propertys.MirrorColor / selectionPdf,
                    PdfW = selectionPdf,
                    PdfWReverse = selectionPdf
                };
            }

            return result;
        }
    }
}
