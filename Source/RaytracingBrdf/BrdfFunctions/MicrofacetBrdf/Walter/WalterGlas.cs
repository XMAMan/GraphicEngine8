using GraphicMinimal;
using IntersectionTests;
using RayTracerGlobal;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Walter.MicrofacetBasics;
using System;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Walter
{
    //Quelle: Microfacet Models for Refraction through Rough Surfaces Walter 2007

    //2.2.2019: Hinweise: Wenn der RoughnessFactor kleiner als 0.1 ist, dann klappt der GGX-Normaldistribution-Test schon nicht mehr
    //Der Pdf-Normalisierungstest geht deswegen auch nur, wenn der Roughness-Faktor zwischen 0.1 und 1 liegt
    //Beim Glas liefert die Brdf- und Pdf-Abfrage zu geringe Werte. Ich verstehe noch nicht 100% warum

    //Ein Microsurface-Glas welches die Beckmann- oder GGX-Verteilung für die Micronormalen nutzt
    class WalterGlas : IBrdf
    {
        private readonly IWalterBrdf brdf;
        private readonly IWalterBrdf reverseBrdf;
        private readonly float refractionIndexCurrentMedium;
        private readonly float refractionIndexNextMedium;

        public WalterGlas(IntersectionPoint hitPoint, Vector3D directionToPoint, float refractionIndexCurrentMedium, float refractionIndexNextMedium, float roughnessFactor = 0.01f)
        {
            this.refractionIndexCurrentMedium = refractionIndexCurrentMedium;
            this.refractionIndexNextMedium = refractionIndexNextMedium;

            if (hitPoint.BumpmapColor != null) roughnessFactor = (1 - hitPoint.BumpmapColor.X) / 10;

            roughnessFactor = Math.Max(0.001f, roughnessFactor); //Es wird durch diese Zahl geteilt. Vermeide Division durch 0

            Vector3D normal = (directionToPoint * hitPoint.OrientedFlatNormal > 0) ? -hitPoint.OrientedFlatNormal : hitPoint.OrientedFlatNormal;

            //this.brdf = new WalterBeckmann(hitPoint, (directionToPoint * hitPoint.Normal > 0) ? -hitPoint.Normal : hitPoint.Normal, roughnessFactor);
            this.brdf = new WalterGGX(hitPoint, normal, roughnessFactor);
            this.reverseBrdf = new WalterGGX(hitPoint, -normal, roughnessFactor);

            float fresnel = (float)Vector3D.FresnelTerm(-directionToPoint, hitPoint.OrientedFlatNormal, refractionIndexCurrentMedium, refractionIndexNextMedium);
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
            //float brdfWeight = ReflectionBrdfWeight(i, o, ni, no) + RefractionBrdfWeigt(i, o, ni, no);    
            if (brdfWeight > 2) brdfWeight = 2;
            return this.brdf.HitPoint.Color * brdfWeight * this.brdf.HitPoint.SpecularAlbedo;// * 10;
        }

        private float ReflectionBrdfWeight(Vector3D i, Vector3D o, float ni, float no)
        {
            Vector3D microNormal = Vector3D.Normalize(i + o);
            double fresnel = Vector3D.FresnelTerm(i, microNormal, ni, no);
            double brdfWeight = fresnel * this.brdf.G2(microNormal, i, o) * (float)this.brdf.NormalDistribution(microNormal) / Math.Max(1e-6f, (4 * Math.Abs(i * this.brdf.MacroNormal) * Math.Abs(o * this.brdf.MacroNormal)));
            return (float)brdfWeight;
        }

        private float RefractionBrdfWeigt(Vector3D i, Vector3D o, float ni, float no)
        {
            Vector3D microNormal = /*Math.Sign(i * this.brdf.MacroNormal) **/ Vector3D.Normalize(-(ni * i + no * o));
            if (microNormal * this.brdf.MacroNormal < 0) microNormal = -microNormal;
            double fresnel = Vector3D.FresnelTerm(i, microNormal, ni, no);
            float d = ni * (i * microNormal) + no * (o * microNormal);
            //double brdfWeight = Math.Abs(i * microNormal) * Math.Abs(o * microNormal) / (Math.Max(1e-6f, Math.Abs(i * this.brdf.MacroNormal) * Math.Abs(o * this.brdf.MacroNormal))) * no * no * (1 - fresnel) * this.brdf.G2(microNormal, i, o) * this.brdf.NormalDistribution(microNormal) / (d * d);
            double brdfWeight = Math.Abs(i * microNormal) * Math.Abs(o * microNormal) / (Math.Max(1e-6f, Math.Abs(i * this.brdf.MacroNormal) * Math.Abs(o * this.brdf.MacroNormal))) * no * no * (1 - fresnel) * this.brdf.G1(microNormal, i) * this.brdf.G1(-microNormal, o) * this.brdf.NormalDistribution(microNormal) / (d * d);
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

            //return ReflectionPdfW(i, o, ni, no) + RefractionPdfW(i, o, ni, no);
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
            return (float)(fresnel * this.brdf.Pdf_wm(microNormal) * this.brdf.JacobianDeterminantForReflection(o, microNormal));
        }

        private float RefractionPdfW(IWalterBrdf bf, Vector3D i, Vector3D o, float ni, float no)
        {
            Vector3D microNormal = Vector3D.Normalize(-(ni * i + no * o));
            if (microNormal * bf.MacroNormal < 0) microNormal = -microNormal;
            double fresnel = Vector3D.FresnelTerm(i, microNormal, ni, no);
            return (float)((1 - fresnel) * bf.Pdf_wm(microNormal) * bf.JacobianDeterminantForRefraction(i, o, microNormal, ni, no));
        }

        //u1, u2 und u3 müssen im Bereich von 0..1 liegen
        //lightGoingInRayIsOutside = Liegt der eingehende Lichtstrahl, welcher aus lightGoingInDirection kommt, in der Luft?
        //brdfWithCosThetaOutAndPdfW entspricht f(i,o,n) * |o*n| / PdfW(o) 
        public BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            BrdfSampleData result = new BrdfSampleData();
            Vector3D i = -lightGoingInDirection;
            Vector3D microNormal = this.brdf.SampleMicroNormal(u1, u2);
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
            result.BrdfWeightAfterSampling = this.brdf.HitPoint.Color * (float)brdfWeight * this.brdf.HitPoint.SpecularAlbedo;
            result.PdfW = pdfW;
            result.PdfWReverse = pdfWReverse;
            return result;
        }
    }
}
