using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;

namespace RaytracingBrdf.SampleAndRequest
{
    //Ein Strahl wurde zufällig reflektiert oder gebrochen
    public class BrdfSampleEvent
    {
        public float PdfW = 1;           // Wahrscheinlichkeit, dass genau die Richtung 'Direction' genommen wird        -> PdfW
        public float PdfWReverse = 1;    // Wahrscheinlichkeit, dass ich von der SampledDirection kommend in Richtung BrdfDirectionInput gehe
        public Ray Ray;                  // Neue Richtung
        public IIntersecableObject ExcludedObject = null;
        public Vector3D Brdf;              // So viel Licht fliegt weiter. => Entspricht BrdfsFunctions.Brdf / (PathSelectionPdf * ContinuationPdf * PdfAFromNextPoint)
        public bool IsSpecualarReflected = false;
        public bool RayWasRefracted = false;

        public BrdfSampleEvent() { }

        public BrdfSampleEvent(BrdfSampleEvent copy)
        {
            this.PdfW = copy.PdfW;
            this.PdfWReverse = copy.PdfWReverse;
            this.Ray = copy.Ray;
            this.ExcludedObject = copy.ExcludedObject;
            this.Brdf = copy.Brdf;
            this.IsSpecualarReflected = copy.IsSpecualarReflected;
            this.RayWasRefracted = copy.RayWasRefracted;
        }
    }
}
