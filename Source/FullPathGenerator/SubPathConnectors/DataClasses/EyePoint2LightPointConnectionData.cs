using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia.DistanceSampling;
using RaytracingBrdf.SampleAndRequest;

namespace FullPathGenerator
{
    public class EyePoint2LightPointConnectionData
    {
        public float GeometryTerm;
        public Vector3D AttenuationTerm;
        public BrdfEvaluateResult EyeBrdf;
        public BrdfEvaluateResult LightBrdf;
        public MediaLine LineFromEyeToLightPoint = null;
        public DistancePdf PdfLForEyeToLightPoint = null;//Distancesampling-Pdf, um vom Eye- zum LightPoint zu gehen
    }
}
