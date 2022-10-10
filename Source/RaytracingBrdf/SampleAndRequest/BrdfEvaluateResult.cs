using GraphicMinimal;

namespace RaytracingBrdf.SampleAndRequest
{
    public class BrdfEvaluateResult
    {
        public Vector3D Brdf;
        public float PdfW;
        public float PdfWReverse;
        public float CosThetaOut;//Normale * DirectionOut
    }
}
