using GraphicMinimal;
using IntersectionTests;
using RayTracerGlobal;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.MicrofacetBasics;
using System;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz
{
    //Quelle: Importance Sampling Microfacet-Based BSDFs using the Distribution of Visible Normals 2014 Eric Heiz

    //Ein Microsurface-Glas welches die Beckmann- oder GGX-Verteilung für die Micronormalen nutzt
    class HeizGlas : IBrdf
    {
        private readonly IHeizBrdf brdf;
        private readonly IHeizBrdf reverseBrdf;
        private readonly float refractionIndexCurrentMedium;
        private readonly float refractionIndexNextMedium;

        public HeizGlas(IntersectionPoint hitPoint, Vector3D directionToPoint, float refractionIndexCurrentMedium, float refractionIndexNextMedium)
        {
            Vector2D roughness = hitPoint.Propertys.NormalSource.As<NormalFromMicrofacet>().MicrofacetRoughness;
            float roughnessFactorX = roughness.X;
            float roughnessFactorY = roughness.Y;

            this.refractionIndexCurrentMedium = refractionIndexCurrentMedium;
            this.refractionIndexNextMedium = refractionIndexNextMedium;
            if (hitPoint.BumpmapColor != null) roughnessFactorX = roughnessFactorY = (1 - hitPoint.BumpmapColor.X) / 10;

            //Die Anisotropische Normaldistribution-Funktion verträgt kein Rauheitsfaktor der 0 ist, da durch diesen dividiert wird
            roughnessFactorX = Math.Max(0.001f, roughnessFactorX);
            roughnessFactorY = Math.Max(0.001f, roughnessFactorY);

            Vector3D normal = hitPoint.OrientedFlatNormal;
            normal = (directionToPoint * normal > 0) ? -normal : normal;
            this.brdf = new HeizGGX(hitPoint, normal, roughnessFactorX, roughnessFactorY);
            this.reverseBrdf = new HeizGGX(hitPoint, -normal, roughnessFactorX, roughnessFactorY);
            //this.brdf = new HeizBeckmann(hitPoint, (directionToPoint * normal > 0) ? -normal : normal, roughnessFactorX, roughnessFactorY);

            float fresnel = (float)Vector3D.FresnelTerm(-directionToPoint, normal, refractionIndexCurrentMedium, refractionIndexNextMedium);
            this.ContinuationPdf = (fresnel * hitPoint.Color.Max() + (1 - fresnel)) * hitPoint.SpecularAlbedo;
        }

        public bool IsSpecularBrdf { get { return false; } }
        public bool CanCreateRefractedRays { get { return true; } }
        public float ContinuationPdf { get; private set; }
        public float DiffuseFactor { get { return 0; } }

        //Das entspricht f(i,o,n)   (Wird vom Brdf-Abfrager verwendet)
        public Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            Vector3D i = -lightGoingInDirection;
            Vector3D o = lightGoingOutDirection;
            //float ni = lightGoingInRayIsOutside ? 1 : this.brdf.HitPoint.Brechungsindex; //Brechungsindex von wo der Strahl herkommt
            //float no = lightGoingInRayIsOutside ? this.brdf.HitPoint.Brechungsindex : 1; //Brechungsindex von der anderen Seite
            float ni = refractionIndexCurrentMedium; //Brechungsindex von wo der Strahl herkommt
            float no = refractionIndexNextMedium;    //Brechungsindex von der anderen Seite

            float inDot = i * this.brdf.MacroNormal;
            float outDot = o * this.brdf.MacroNormal;

            bool isReflected = !((inDot < 0.0) ^ (outDot < 0.0));

            float brdfWeight = isReflected ? ReflectionBrdfWeight(i, o, ni, no) : RefractionBrdfWeigt(i, o, ni, no);
            if (brdfWeight > 2) brdfWeight = 2;
            return this.brdf.HitPoint.Color * brdfWeight * this.brdf.HitPoint.SpecularAlbedo;// * 10;
        }

        private float ReflectionBrdfWeight(Vector3D i, Vector3D o, float ni, float no)
        {
            Vector3D microNormal = Vector3D.Normalize(i + o);
            double fresnel = Vector3D.FresnelTerm(i, microNormal, ni, no);
            double brdfWeight = fresnel * this.brdf.G2(microNormal, i, o) * this.brdf.NormalDistribution(microNormal) / Math.Max(1e-6f, (4 * Math.Abs(i * this.brdf.MacroNormal) * Math.Abs(o * this.brdf.MacroNormal)));
            if (double.IsNaN(brdfWeight)) return 0;
            return (float)brdfWeight;
        }

        private float RefractionBrdfWeigt(Vector3D i, Vector3D o, float ni, float no)
        {
            Vector3D microNormal = Vector3D.Normalize(-(ni * i + no * o));
            if (microNormal * this.brdf.MacroNormal < 0) microNormal = -microNormal;
            double fresnel = Vector3D.FresnelTerm(i, microNormal, ni, no);
            float d = ni * (i * microNormal) + no * (o * microNormal);
            double brdfWeight = Math.Abs(i * microNormal) * Math.Abs(o * microNormal) / (Math.Max(1e-6f, Math.Abs(i * this.brdf.MacroNormal) * Math.Abs(o * this.brdf.MacroNormal))) * no * no * (1 - fresnel) * this.brdf.G2(microNormal, i, o) * this.brdf.NormalDistribution(microNormal) / (d * d);
            if (double.IsNaN(brdfWeight)) return 0;
            return (float)brdfWeight;
        }

        //Pdf With Respect to Solid Angle dP / do
        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            Vector3D i = -lightGoingInDirection;
            Vector3D o = lightGoingOutDirection;
            //float ni = lightGoingInRayIsOutside ? 1 : this.brdf.HitPoint.Brechungsindex; //Brechungsindex von wo der Strahl herkommt
            //float no = lightGoingInRayIsOutside ? this.brdf.HitPoint.Brechungsindex : 1; //Brechungsindex von der anderen Seite
            float ni = refractionIndexCurrentMedium; //Brechungsindex von wo der Strahl herkommt
            float no = refractionIndexNextMedium;    //Brechungsindex von der anderen Seite

            float inDot = i * this.brdf.MacroNormal;
            float outDot = o * this.brdf.MacroNormal;

            bool isReflected = !((inDot < 0.0) ^ (outDot < 0.0));

            float pdfW = isReflected ? ReflectionPdfW(i, o, ni, no) : RefractionPdfW(this.brdf, i, o, ni, no);
            return PdfWCheck(pdfW);
        }

        private float PdfWCheck(float pdfW)
        {
            if (float.IsNaN(pdfW)) throw new Exception("Ärger");
            if (pdfW > 5) pdfW = 5;
            return Math.Max(MagicNumbers.MinAllowedPdfW, pdfW);
        }

        private float ReflectionPdfW(Vector3D i, Vector3D o, float ni, float no)
        {
            Vector3D microNormal = Vector3D.Normalize(i + o);
            double fresnel = Vector3D.FresnelTerm(i, microNormal, ni, no);
            return (float)(fresnel * this.brdf.Pdf_wm(-i, microNormal) * this.brdf.JacobianDeterminantForReflection(o, microNormal));
        }

        private float RefractionPdfW(IHeizBrdf bf, Vector3D i, Vector3D o, float ni, float no)
        {
            if (ni == no) return 1; //Wenn es kein Mediumwechsel gibt, fliegt Strahl gerade aus und MicroNormal entspricht MacroNormal
            Vector3D microNormal = Vector3D.Normalize(-(ni * i + no * o));
            if (microNormal * bf.MacroNormal < 0) microNormal = -microNormal;
            double fresnel = Vector3D.FresnelTerm(i, microNormal, ni, no);
            return (float)((1 - fresnel) * bf.Pdf_wm(-i, microNormal) * bf.JacobianDeterminantForRefraction(i, o, microNormal, ni, no));
        }

        //u1, u2 und u3 müssen im Bereich von 0..1 liegen
        //lightGoingInRayIsOutside = Liegt der eingehende Lichtstrahl, welcher aus lightGoingInDirection kommt, in der Luft?
        //brdfWithCosThetaOutAndPdfW entspricht f(i,o,n) * |o*n| / PdfW(o) 
        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            BrdfSampleData result = new BrdfSampleData();
            Vector3D i = -lightGoingInDirection;
            Vector3D microNormal = this.brdf.SampleVisibleMicroNormal(-lightGoingInDirection, u1, u2);
            //float ni = lightGoingInRayIsOutside ? 1 : this.brdf.HitPoint.Brechungsindex; //Brechungsindex wo der Strahl herkommt
            //float no = lightGoingInRayIsOutside ? this.brdf.HitPoint.Brechungsindex : 1; //Brechungsindex von der anderen Seite
            float ni = refractionIndexCurrentMedium; //Brechungsindex von wo der Strahl herkommt
            float no = refractionIndexNextMedium;    //Brechungsindex von der anderen Seite
            double fresnel = Vector3D.FresnelTerm(i, microNormal, ni, no);

            Vector3D o;
            float pdfW;
            float pdfWReverse;
            if (u3 <= fresnel)
            {
                result.RayWasRefracted = false;
                o = Vector3D.GetReflectedDirection(lightGoingInDirection, microNormal);

                //Die Pfad-Selection-Pdf ist in der Funktion schon drin
                pdfW = PdfWCheck(ReflectionPdfW(i, o, ni, no));
                pdfWReverse = PdfWCheck(ReflectionPdfW(o, i, ni, no));
            }
            else
            {
                result.RayWasRefracted = true;
                o = Vector3D.GetRefractedDirection(i, microNormal, ni, no);

                pdfW = PdfWCheck(RefractionPdfW(this.brdf, i, o, ni, no));
                pdfWReverse = PdfWCheck(RefractionPdfW(this.reverseBrdf, o, i, no, ni));
            }

            if (this.brdf.HitPoint.Propertys.GlasIsSingleLayer) result.RayWasRefracted = false; //In eine unendlich dünne Glasscheibe kann kein Strahl eindringen

            double brdfWeight = this.brdf.BrdfWeightAfterSampling(i, o, microNormal);
            result.SampledDirection = o;
            if (double.IsNaN(brdfWeight))
            {
                throw new Exception("brdfWeight is NaN lightGoingInDirection=" + lightGoingInDirection + " u1=" + u1 + " u2=" + u2 + " u3=" + u3 + " refractionIndexCurrentMedium=" + refractionIndexCurrentMedium + " MacroNormal=" + this.brdf.MacroNormal);
            }
            result.BrdfWeightAfterSampling = this.brdf.HitPoint.Color * (float)brdfWeight * this.brdf.HitPoint.SpecularAlbedo;
            result.PdfW = pdfW;
            result.PdfWReverse = pdfWReverse;
            return result;
        }
    }
}
