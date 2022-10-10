using GraphicMinimal;
using IntersectionTests;
using RayTracerGlobal;
using System;

namespace RaytracingBrdf.BrdfFunctions
{
    //https://graphics.stanford.edu/courses/cs148-10-summer/docs/2006--degreve--reflection_refraction.pdf
    class BrdfGlas : IBrdf
    {
        protected IntersectionPoint point;
        protected Vector3D normal; //Wird von außen reingegeben
        protected float refractionIndexCurrentMedium;
        protected float refractionIndexNextMedium;
        protected bool useTexture;
        protected float fresnelTerm;
        protected float reflectionProb; //Wie viel Prozent des Lichtes wird reflektiert? Der Rest wird gebrochen


        public bool IsSpecularBrdf { get { return true; } }
        public bool CanCreateRefractedRays { get { return true; } }
        public float ContinuationPdf { get; protected set; }
        public float DiffuseFactor { get { return 0; } }

        //useTexture = true => Glas ohne Media (Die Absorbation von einzelnen Lichtwellen übernimmt der Glasrand)
        //useTexture = false => Glas mit Media. Dringt der Strahl ins Glas ein, übernehmen die Absorbationteilchen vom Medium die Farbgestaltung; Wird der 
        //                      Strahl reflektiert, übernimmt die MirrorColor die Farbgestaltung
        public BrdfGlas(IntersectionPoint point, Vector3D directionToPoint, float refractionIndexCurrentMedium, float refractionIndexNextMedium, bool useTexture)
        {
            this.point = point;
            this.normal = point.ShadedNormal;
            this.refractionIndexCurrentMedium = refractionIndexCurrentMedium;
            this.refractionIndexNextMedium = refractionIndexNextMedium;
            this.useTexture = useTexture;

            //Es erfolgt hier keine Überprüfung, dass -directionToPoint und this.normal auf der gleichen Seite liegt. FresnelTerm setzt das aber vorraus.  ShadedNormal wird vom IntersectionFinder immer passend gedreht
            this.fresnelTerm = (float)Vector3D.FresnelTerm(-directionToPoint, this.normal, refractionIndexCurrentMedium, refractionIndexNextMedium); //So viel Prozent wird bei diesen Einstrahlwinkel reflektiert

            //Siehe SmappUPBP->Bsdf.hxx-Zeile 710 für ContinationPdf-Formel
            this.ContinuationPdf = this.useTexture ? this.point.Color.Max() * this.point.SpecularAlbedo : (this.point.Propertys.MirrorColor.Max() * fresnelTerm + (1 - fresnelTerm) * 1) * this.point.SpecularAlbedo;
            //this.ContinuationPdf = this.useTexture ?  this.point.Color.Max() * this.point.SpecularAlbedo : this.point.Propertys.MirrorColor.Max() * this.point.SpecularAlbedo; //Meine Formel, wenn ich reflectionProb = fresnelTerm verwende dann muss die ContinuationPdf so aussehen, damit ich bei Reflektion 1 und bei Refraction 1/MirrorColor.Max erhalte

            this.reflectionProb = (this.point.Propertys.MirrorColor.Max() * this.fresnelTerm) / (this.point.Propertys.MirrorColor.Max() * this.fresnelTerm + (1 - this.fresnelTerm)); //So macht es SmallUPBP
            //this.reflectionProb = fresnelTerm; //So hatte ich es die ganze Zeit

            //Ich prüfe, ob das Pfadgewicht nach den Sampeln (und nach der Division mit der ContinuationPdf) 1 ergibt:
            //Fall 1: Reflection
            // BrdfAfterSampling = MirrorColor * Fresnel / (reflectionProb * ContinuationPdf)
            // BrdfAfterSampling = MirrorColor * Fresnel / (MirrorColor.Max * Fresnel / (MirrorColor.Max * Fresnel + (1-Fresnel)) * (MirrorColor.Max * Fresnel + (1-Fresnel)) )
            // BrdfAfterSampling = MirrorColor / MirrorColor.Max

            //Fall 2: Refraction
            // BrdfAfterSampling = (1,1,1) * (1-Fresnel) / ( (1-reflectionProb) * ContinuationPdf)
            // BrdfAfterSampling = (1,1,1) * (1-Fresnel) / ( (1 - MirrorColor.Max * Fresnel / (MirrorColor.Max * Fresnel + (1-Fresnel))) * (MirrorColor.Max * Fresnel + (1-Fresnel)) )
            // BrdfAfterSampling = (1,1,1) * (1-Fresnel) / ( ((MirrorColor.Max * Fresnel + (1-Fresnel)) - MirrorColor.Max * Fresnel ) )
            // BrdfAfterSampling = (1,1,1) * (1-Fresnel) / (1-Fresnel)
            // BrdfAfterSampling = (1,1,1)
        }

        //Das entspricht f(i,o,n)   (Wird vom Brdf-Abfrager verwendet)
        public Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            throw new Exception("Diese Funktion darf in der Brdf-Abfrage nicht benutzt werden, da sie eine Dirac-Delta-Funktion enthält. Die Formel wäre hitPoint.Color*Delta(wo)/|n*wo|");
        }

        //Pdf With Respect to Solid Angle dP / do
        public float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection)
        {
            Vector3D normal = this.normal;
            if (lightGoingInDirection * normal > 0) normal = -normal;

            Vector3D i = -lightGoingInDirection;
            Vector3D o = lightGoingOutDirection;
            float ni = this.refractionIndexCurrentMedium; //Brechungsindex von wo der Strahl herkommt
            float no = this.refractionIndexNextMedium;    //Brechungsindex von der anderen Seite

            float inDot = i * normal;
            float outDot = o * normal;

            bool isReflected = !((inDot < 0.0) ^ (outDot < 0.0));

            float fresnel = (float)Vector3D.FresnelTerm(i, normal, ni, no);

            return Math.Max(MagicNumbers.MinAllowedPdfW, isReflected ? fresnel : (1 - fresnel));
        }

        public virtual BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3)
        {
            bool rayWasRefracted = false;
            Vector3D direction;
            float pdfW;
            float pdfWReverse;

            if (u3 <= this.reflectionProb)
            {
                direction = Vector3D.GetReflectedDirection(lightGoingInDirection, this.normal);
                pdfW = this.reflectionProb;
                pdfWReverse = this.reflectionProb;
            }
            else
            {
                direction = Vector3D.GetRefractedDirection(-lightGoingInDirection, this.normal, refractionIndexCurrentMedium, refractionIndexNextMedium);
                rayWasRefracted = true;
                pdfW = 1 - this.reflectionProb; //1 - Fresnel = So viel wird gebrochen

                //Wenn ich den Fresnel-Term von der anderen Seite berechne, weiß ich, wie viel Prozent aus der anderen Richtung gehend reflektiert wird
                //Ich muss dann noch 1 - Fresnel_von_anderer_Seite rechnen, um zu sehen, wie viel von der anderen Seite aus kommend gebrochen wird
                //pdfWReverse = 1 - (float)Vector3D.FresnelTerm(direction, -this.normal, this.refractionIndexNextMedium, this.refractionIndexCurrentMedium);

                //Im Debugger sehe ich, dass (1 - (float)Vector3D.FresnelTerm(direction, -this.normal, this.refractionIndexNextMedium, this.refractionIndexCurrentMedium)) == pdfW
                //Außerdem gilt bei SmallUPBP: UPBP.hxx Zeile 2040: float bsdfRevPdfW = bsdfDirPdfW; If we sampled specular event, then the reverse probability cannot be evaluated, but we know it is exactly the same as forward probability, so just set it.
                pdfWReverse = pdfW;
            }
            if (this.point.Propertys.GlasIsSingleLayer) rayWasRefracted = false; //In eine unendlich dünne Glasscheibe kann kein Strahl eindringen

            return new BrdfSampleData()
            {
                SampledDirection = direction,
                BrdfWeightAfterSampling = this.useTexture ? this.point.Color * this.point.SpecularAlbedo : (rayWasRefracted ? (new Vector3D(1, 1, 1) * (1 - this.fresnelTerm) / (1 - this.reflectionProb)) : (this.point.Propertys.MirrorColor * this.fresnelTerm / this.reflectionProb)) * this.point.SpecularAlbedo, //Formel wo ich ReflektionProp = FresnelTerm * MirrorColor verwende (Und Kürzung des Fresnelterms im Zähler und Nenner nicht möglich ist) Hier erfolgt noch keine Division mit der ContinuationPdf. Deswegen ist hier noch keine Kürzung möglich
                //BrdfWeightAfterSampling = this.useTexture ? this.point.Color * this.point.SpecularAlbedo : (rayWasRefracted ? new Vector3D(1, 1, 1) : this.point.Propertys.MirrorColor) * this.point.SpecularAlbedo, //Bei mir galt fresnelTerm==reflectionProb wodurch sich dass dann wegkürzte
                RayWasRefracted = rayWasRefracted,
                IsSpecularReflected = true,
                PdfW = Math.Max(MagicNumbers.MinAllowedPdfW, pdfW),
                PdfWReverse = Math.Max(MagicNumbers.MinAllowedPdfW, pdfWReverse)
            };
        }
    }
}
