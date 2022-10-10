using GraphicMinimal;

namespace RaytracingBrdf.BrdfFunctions
{
    //Sehr gute Zusammenfassung sämplicher Light- und Brdf-Sampling-Routinen
    //http://people.cs.kuleuven.be/~philip.dutre/GI/TotalCompendium.pdf

    public interface IBrdf
    {
        bool IsSpecularBrdf { get; } //Specular bedeutet absolut keine Streung. Die Brdf enthält eine Dirac-Delta-Funktion und wirft eine Exception. (Glas oder Spiegel)
        bool CanCreateRefractedRays { get; } //Kann diese Brdf den Strahl brechen? (Nur bei Glas true)
        float ContinuationPdf { get; } //Wahrscheinlichkeit, dass Strahl nach Reflektion/Brechung weiter fliegt
        float DiffuseFactor { get; } //Wie viel Prozent des Lichtes wird komplett diffuse gestreuten?
        Vector3D Evaluate(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection);
        float PdfW(Vector3D lightGoingInDirection, Vector3D lightGoingOutDirection); //Summe aus DiffusePdf+PhongPdf+MicrofacetPdf (Ohne Specular-Pdf)
        BrdfSampleData SampleDirection(Vector3D lightGoingInDirection, double u1, double u2, double u3);
    }

    public class BrdfSampleData
    {
        public Vector3D SampledDirection;
        public Vector3D BrdfWeightAfterSampling;
        public bool RayWasRefracted = false;
        public bool IsSpecularReflected = false; //Damit das Photonmapping Speculare Pfade (S*) erkennt

        //Achtung: Wenn ich eine Brdf habe, wo mit u3 ein Material zufällig ausgewählt wird, dann lautet die zugehörige Pdf SelectionPdf * MaterialPdf
        //Die PdfW-Funktion ist für die BrdfAbfrage, wo man die Oderknüpfung von Diffuse/Phong/Microfacet wissen will
        //Aus dem Grund MUSS ich die PdfW/PdfWReverse beim Sampeln hier zurück geben lassen, da nur der Sampler weiß,
        //ob der Strahl denn Diffuse/Specular/Phong-Mäßig reflektiert/gebrochen wurde. 
        public float PdfW = float.NaN;
        public float PdfWReverse = float.NaN;
    }
}
