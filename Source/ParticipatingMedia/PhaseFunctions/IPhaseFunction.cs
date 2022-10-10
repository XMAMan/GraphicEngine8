using GraphicGlobal;
using GraphicMinimal;

namespace ParticipatingMedia.PhaseFunctions
{
    public interface IPhaseFunction
    {
        PhaseSampleResult SampleDirection(Vector3D mediaPoint, Vector3D directionToPoint, IRandom rand);
        PhaseFunctionResult GetBrdf(Vector3D directionToMediaPoint, Vector3D mediaPoint, Vector3D outDirection);
    }

    public class PhaseSampleResult
    {
        public float PdfW;
        public float PdfWReverse;
        public Ray Ray;
        public float BrdfDividedByPdfW;
    }

    public class PhaseFunctionResult
    {
        public float Brdf;
        public float PdfW;          //Pdf With Respect to Solid Angle dP / do
        public float PdfWReverse;
    }
}
