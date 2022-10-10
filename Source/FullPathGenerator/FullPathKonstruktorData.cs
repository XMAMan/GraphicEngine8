using SubpathGenerator;
using RaytracingLightSource;
using RayCameraNamespace;

namespace FullPathGenerator
{
    public class FullPathKonstruktorData
    {
        public PathSamplingType EyePathSamplingType;
        public PathSamplingType LightPathSamplingType;
        public PointToPointConnector PointToPointConnector; //Zum Verbinden von zwei PathPoints
        public IRayCamera RayCamera; //Fürs Lighttracing
        public LightSourceSampler LightSourceSampler; //Fürs DirectLighting
        public int MaxPathLength;//Damit beim Konstruieren eines Fullfpaths über VertexConnection/VertexMerging die maximal erlaubte Länge nicht überschritten wird
    }
}
