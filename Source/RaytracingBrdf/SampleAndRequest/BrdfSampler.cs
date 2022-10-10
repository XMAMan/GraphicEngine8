using System;
using GraphicMinimal;
using GraphicGlobal;
using RayTracerGlobal;
using RaytracingBrdf.BrdfFunctions;

namespace RaytracingBrdf.SampleAndRequest
{
    //Überlegung zur ContinuationPdf: Ich könnte theoetisch jeden Wert zwischen 0 und 1 als ContinationPdf nehmen nur wäre es besser
    //ein möglichst hohen Wert nahe 1 zu nehmen, da eine kleine ContinationPdf eine hohe Varianz bedeutet, was für mehr Bildrauschen/Fireflys sorgt.
    //Wenn ich ein Fresnel-Term habe, der nahe 1 oder 0 liegt, dann führt das vermutlich auch zu hoher Varianz/Fireflys. 
    //Anderseits führt eine hohe ContinationPdf zu ewig langen BBB-Pfaden. 
    //SmallUPBP macht das so: Bei Surface/MediaBorder-Punkten wird die ContinuationPdf über 
    //(DiffuseRefelctance + PhongReflectance + Fresnel*MirrorColor + (1 - Fresnel)) berechnet
    //Siehe Bsdf.hxx-Zeile 710. Bei meiner Glas-Brdf verwende ich auch diese Formel
    //UPBP.hxx Zeile 2022: Erst erfolgt Zufällige Auswahl von Diffuse/Phong/SpecularReflect/SpecularRefract
    //UPBP.hxx Zeile 2043: Dann erfolg hier Russian Roulette (Absorbation)
    //Bei Partikeln nimmt er die Formel Math.Max(0.8, scatterCoef.Max() / (scatterCoef.Max() + absorbationCoef.Max())) -> Siehe Scene.hxx Zeile 1219
    //Sollte das Partikel ein ScatterCoeffizient von 0 haben, erfolgt kein Sampling/Abfrage -> Siehe Bsdf.hxx Zeile 393
    //D.h. SmallUPBP nimmt bei SurfacePunkten den Albedo-Wert und bei Partikeln ein möglichst hohen Continuationwert
    //Nehme ich für Partikelsampling fix 0.8, ist die Kerze zu dunkel und nehme ich fix die os/(os+oa)-Formel, ist der grüne Topf verpixelt.

    //Überlegung zum Thema PdfW == PdfWReverse?
    //Der Begriff symmetrische Brdf bezieht sich auf die Lichtmenge von Ein- und ausgehenden Licht
    //Er sagt nichts über die PdfW aus. Diese hängt vom LightInputDirection und der Normale ab und 
    //muss somit IMMER extra berechnet werden. Selbst bei der diffusen Brdf kann ich nicht davon
    //ausgehen, das PdfW == PdfWReverse gilt da die PdfW vom Lichtinput-Winkel abhängt. Nachtrag: Bei specular-Pdf gilt schon PdfW==PdfWReverse
    public class BrdfSampler : IBrdfSampler
    {
        public BrdfSampleEvent CreateDirection(BrdfPoint brdfPoint, IRandom rand)
        {
            float continuationPdf = Math.Min(1, Math.Max(MagicNumbers.MinSurfaceContinuationPdf, brdfPoint.ContinuationPdf));
            //float continuationPdf = Math.Min(1, Math.Max(0, brdfPoint.ContinuationPdf)); //Nach dieser Formal arbeitet SmallUPBP

            if (rand.NextDouble() >= continuationPdf)      // Absorbation; wenn hier > steht kann eine continuationPdf zur 0-Division führen; bei >= kann eine Spekular-Reflektion zur Absorbation führen (weniger schlimm)
                return null;            

            var d = brdfPoint.Brdf.SampleDirection(brdfPoint.DirectionToThisPoint, rand.NextDouble(), rand.NextDouble(), rand.NextDouble());
            if (d == null) return null;

            var sampleEvent = TransformBrdfSampleDataToBrdfSampleEvent(brdfPoint, d);

            sampleEvent.Brdf /= continuationPdf;
            sampleEvent.PdfW *= continuationPdf;
            sampleEvent.PdfWReverse *= continuationPdf;

            if (sampleEvent.PdfW < MagicNumbers.MinAllowedPdfW) return null;

            return sampleEvent;
        }

        private static BrdfSampleEvent TransformBrdfSampleDataToBrdfSampleEvent(BrdfPoint brdfPoint, BrdfSampleData d)
        {
            return new BrdfSampleEvent()
            {
                Ray = new Ray(brdfPoint.SurfacePoint.Position, d.SampledDirection),
                Brdf = d.BrdfWeightAfterSampling,
                PdfW = d.PdfW,
                PdfWReverse = d.PdfWReverse,
                IsSpecualarReflected = d.IsSpecularReflected,
                ExcludedObject = brdfPoint.SurfacePoint.IntersectedObject,
                RayWasRefracted = d.RayWasRefracted,
            };
        }
    }
}
