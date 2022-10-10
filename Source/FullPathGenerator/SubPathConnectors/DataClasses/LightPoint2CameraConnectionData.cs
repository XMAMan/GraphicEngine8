using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia.DistanceSampling;
using RaytracingBrdf.SampleAndRequest;
using SubpathGenerator;

namespace FullPathGenerator
{
    public class LightPoint2CameraConnectionData
    {
        public float GeometryTerm;
        public Vector3D AttenuationTerm;
        public BrdfEvaluateResult LightBrdf;
        public Vector3D CameraToLightPointDirection;
        public Vector2D PixelPosition;
        public MediaLine LineFromCameraToLightPoint = null;
        public DistancePdf PdfLForCameraToLightPoint = null; //Distancesampling-Pdf, um vom Kamera- zum LightPoint zu gehen
        public PathPoint CameraPoint;
    }
}
