using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using RaytracingBrdf.SampleAndRequest;

namespace SubpathGenerator.SubPathSampler
{
    interface ISubPathPointsSampler
    {
        PathPoint[] SamplePointsFromCamera(Vector3D cameraForward, BrdfSampleEvent sampleEvent, Vector3D pathWeight, float pathCreationTime, IRandom rand);
        PathPoint[] SamplePointsFromLightSource(IntersectionPoint lightPoint, Vector3D pathWeightFromPointOnLight, float positionPdfA, BrdfSampleEvent sampleEvent, Vector3D pathWeight, float pathCreationTime, bool lightSourceIsInfinityAway, IRandom rand);
    }
}
