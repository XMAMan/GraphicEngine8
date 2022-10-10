using BitmapHelper;
using GraphicMinimal;
using IntersectionTests;
using RayTracerGlobal;
using System;

namespace RaytracingBrdf.BrdfFunctions
{
    //Summe aus Diffuse, Phong und Glas oder Mirror (Nachbau von der SmallUPBP-Bsdf damit ich ihre 4 Scenen rendern kann)
    class DiffusePhongGlasOrMirrorSum : IBrdf
    {
        private readonly IntersectionPoint point;
        private readonly float refractionIndexCurrentMedium;
        private readonly float refractionIndexNextMedium;

        private readonly float fresnel;
        private readonly float diffuseProb;
        private readonly float glossyProb;
        private readonly float reflectProb;
        private readonly float refractProb;

        public DiffusePhongGlasOrMirrorSum(IntersectionPoint point, Vector3D directionToPoint, float refractionIndexCurrentMedium, float refractionIndexNextMedium)
        {
            this.point = point;
            this.refractionIndexCurrentMedium = refractionIndexCurrentMedium;
            this.refractionIndexNextMedium = refractionIndexNextMedium;

            this.fresnel = (point.RefractionIndex < 0 || float.IsNaN(point.RefractionIndex)) ? 1 : (float)Vector3D.FresnelTerm(-directionToPoint, point.ShadedNormal, refractionIndexCurrentMedium, refractionIndexNextMedium);

            float albedoDiffuse = PixelHelper.ColorToGray(point.Color * point.Albedo);
            float albedoGlossy = PixelHelper.ColorToGray(point.Propertys.GlossyColor);
            float albedoReflect = this.fresnel * PixelHelper.ColorToGray(point.Propertys.MirrorColor);
            float albedoRefract = 1 - this.fresnel; //Die Farbe vom Refaktion-Ray ist IMMER (1,1,1)

            float totalAlbedo = albedoDiffuse + albedoGlossy + albedoReflect + albedoRefract;

            if (totalAlbedo < 1e-9f)
                this.ContinuationPdf = this.diffuseProb = this.glossyProb = this.reflectProb = this.refractProb = 0;
            else
            {
                this.diffuseProb = albedoDiffuse / totalAlbedo;
                this.glossyProb = albedoGlossy / totalAlbedo;
                this.reflectProb = albedoReflect / totalAlbedo;
                this.refractProb = albedoRefract / totalAlbedo;

                this.ContinuationPdf = (point.Color * point.Albedo + point.Propertys.GlossyColor + this.fresnel * point.Propertys.MirrorColor).Max() + (1 - this.fresnel);
                this.ContinuationPdf = Math.Min(1, Math.Max(0, this.ContinuationPdf));
            }

            this.IsSpecularBrdf = (this.diffuseProb == 0) && (this.glossyProb == 0);
            this.CanCreateRefractedRays = this.refractProb > 0;
            this.DiffuseFactor = this.diffuseProb;
        }

        public bool IsSpecularBrdf { get; private set; }
        public bool CanCreateRefractedRays { get; private set; }
        public float ContinuationPdf { get; private set; }
        public float DiffuseFactor { get; private set; }

        public Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            Vector3D sum = new Vector3D(0, 0, 0);
            if (this.diffuseProb > 0) sum += DiffuseBrdf();
            if (this.glossyProb > 0) sum += GlossyBrdf(lightGoingInDirection, lightGoingOutDirection);
            return sum;
        }

        private Vector3D DiffuseBrdf()
        {
            return this.point.Color * this.point.Albedo / (float)Math.PI;
        }

        //Quelle: SmallUPBP->Bsdf.hxx -> EvaluatePhong
        private Vector3D GlossyBrdf(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            Vector3D perfektReflection = Vector3D.GetReflectedDirection(lightGoingInDirection, this.point.ShadedNormal);

            float dot_R_wi = perfektReflection * lightGoingOutDirection;
            if (dot_R_wi <= 1e-6f) return new Vector3D(0, 0, 0);

            float rho = (this.point.GlossyPowExponent + 2) * 0.5f / (float)Math.PI;
            return this.point.Propertys.GlossyColor * rho * (float)Math.Pow(dot_R_wi, this.point.GlossyPowExponent);
        }

        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            float pdfWSum = 0;
            if (this.diffuseProb > 0) pdfWSum += DiffusePdfW(lightGoingOutDirection);
            if (this.glossyProb > 0) pdfWSum += GlossyPdfW(lightGoingInDirection, lightGoingOutDirection);

            return Math.Max(MagicNumbers.MinAllowedPdfW, pdfWSum);
        }

        //Diffuse-PdfW * Diffuse-Selection-PdfW
        private float DiffusePdfW(Vector3D lightGoingOutDirection)
        {
            return this.diffuseProb * Math.Max(0, this.point.OrientedFlatNormal * lightGoingOutDirection) / (float)Math.PI;
        }

        //Phong-PdfW * Phong-Selection-PdfW
        private float GlossyPdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            Vector3D perfektReflection = Vector3D.GetReflectedDirection(lightGoingInDirection, this.point.ShadedNormal);
            float cosThetaR = Math.Max(1e-6f, perfektReflection * lightGoingOutDirection);

            return this.glossyProb * (float)((this.point.GlossyPowExponent + 1) * Math.Pow(cosThetaR, this.point.GlossyPowExponent) * ((1 / Math.PI) * 0.5f));
        }

        //Ohne Wichtung
        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            if (u3 < this.diffuseProb)
            {
                Vector3D sampledDirection = BrdfDiffuseCosinusWeighted.SampleDirection(u1, u2, this.point.OrientedFlatNormal);

                if (this.glossyProb > 0)
                    return DiffuseAndBrdfSum(lightGoingInDirection, sampledDirection);

                return new BrdfSampleData()
                {
                    SampledDirection = sampledDirection,
                    BrdfWeightAfterSampling = this.point.Color * this.point.Albedo / this.diffuseProb, //Entspricht f(i,o,n) * |o*n| / PdfW(o)  (Wird vom Brdf-Sampler verwendet)
                    PdfW = DiffusePdfW(sampledDirection),
                    PdfWReverse = DiffusePdfW(-lightGoingInDirection)
                };
            }
            else if (u3 < this.diffuseProb + this.glossyProb)
            {
                Vector3D sampledDirection = BrdfGlossy.SampleDirection(u1, u2, lightGoingInDirection, this.point.ShadedNormal, this.point.GlossyPowExponent);

                if (this.diffuseProb > 0)
                    return DiffuseAndBrdfSum(lightGoingInDirection, sampledDirection);

                float pdfW = GlossyPdfW(lightGoingInDirection, sampledDirection);

                return new BrdfSampleData()
                {
                    SampledDirection = sampledDirection,
                    BrdfWeightAfterSampling = GlossyBrdf(lightGoingInDirection, sampledDirection) * Math.Abs(point.ShadedNormal * sampledDirection) / pdfW, //Entspricht f(i,o,n) * |o*n| / PdfW(o)  (Wird vom Brdf-Sampler verwendet)
                    PdfW = pdfW,
                    PdfWReverse = pdfW
                };
            }
            else if (u3 < this.diffuseProb + this.glossyProb + this.reflectProb)
            {
                return new BrdfSampleData()
                {
                    SampledDirection = Vector3D.GetReflectedDirection(lightGoingInDirection, this.point.ShadedNormal),
                    BrdfWeightAfterSampling = this.fresnel * this.point.Propertys.MirrorColor / this.reflectProb,
                    IsSpecularReflected = true,
                    PdfW = this.reflectProb,
                    PdfWReverse = this.reflectProb
                };
            }
            else if (this.refractProb > 0) //Prüfung wegen Float-Ungenaugkeit, wo die Summe nicht 1 ergibt
            {
                //Wenn ich ein Verbundmaterial aus Diffuse+Mirror habe, dann kann es aufgrund von float-Ungenauigkeiten passieren, dass
                //u3 > diffuseProb + reflectProb ist, obwohl Brechungsindex = NaN ist und Brechung garnicht möglich ist. Dann gebe ich null zurück.
                return new BrdfSampleData()
                {
                    SampledDirection = Vector3D.GetRefractedDirection(-lightGoingInDirection, this.point.ShadedNormal, refractionIndexCurrentMedium, refractionIndexNextMedium),
                    BrdfWeightAfterSampling = (1 - this.fresnel) * new Vector3D(1, 1, 1) / this.refractProb,
                    IsSpecularReflected = true,
                    RayWasRefracted = true,
                    PdfW = this.refractProb,
                    PdfWReverse = this.refractProb
                };
            }

            return null;
        }

        //Wenn ich ein Material habe, was sowohl diffuse als auch phong-Anteil hat, dann kann ich das sampeln etwas optimieren.
        //Ist es z.B. zu 50% diffuse und zu 50% phong, dann würde ich bei 1000 Samples normalerweise 500 Diffuse- und 500 Phongsamples 
        //nehmen. Wenn ich gleich die Summe bilde, habe ich 1000 summierte Samples. 
        private BrdfSampleData DiffuseAndBrdfSum(Vector3D lightGoingInDirection, Vector3D sampledDirection)
        {
            float diffusePdfW = DiffusePdfW(sampledDirection);
            float phongPdfW = GlossyPdfW(lightGoingInDirection, sampledDirection);

            float pdfW = diffusePdfW + phongPdfW;

            Vector3D brdfWeightAfterSampling = (DiffuseBrdf() + GlossyBrdf(lightGoingInDirection, sampledDirection)) * Math.Abs(point.ShadedNormal * sampledDirection) / pdfW;

            return new BrdfSampleData()
            {
                SampledDirection = sampledDirection,
                BrdfWeightAfterSampling = brdfWeightAfterSampling, //Entspricht f(i,o,n) * |o*n| / PdfW(o)  (Wird vom Brdf-Sampler verwendet)
                PdfW = pdfW,
                PdfWReverse = DiffusePdfW(-lightGoingInDirection) + phongPdfW
            };
        }
    }
}
