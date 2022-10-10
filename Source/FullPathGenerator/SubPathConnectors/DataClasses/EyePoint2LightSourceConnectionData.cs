using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia.DistanceSampling;
using RaytracingBrdf.SampleAndRequest;

namespace FullPathGenerator
{
    public class EyePoint2LightSourceConnectionData
    {
        public float GeometryTerm;
        public Vector3D AttenuationTerm;
        public BrdfEvaluateResult EyeBrdf;
        public IntersectionPoint LightPoint;
        public MediaIntersectionPoint MediaLightPoint = null;
        public Vector3D DirectionToLightPoint;
        public MediaLine LineFromEyePointToLightSource = null;
        public DistancePdf PdfLForEyepointToLightSource = null; //Distancesampling-Pdf, um vom Eye- zum LightPoint zu gehen
        public bool LighIsInfinityAway;
    }
}
