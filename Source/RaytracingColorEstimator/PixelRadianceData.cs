using FullPathGenerator;
using IntersectionTests;
using RayCameraNamespace;
using RaytracingBrdf;
using RaytracingLightSource;
using SubpathGenerator;

namespace RaytracingColorEstimator
{
    //Enthält all die Daten, die der PixelRadianceCalculator benötigt (Aufbereitete Input-Daten)
    public class PixelRadianceData
    {
        public RaytracingFrame3DData Frame3DData;
        public IntersectionFinder IntersectionFinder;
        public MediaIntersectionFinder MediaIntersectionFinder;
        public IRayCamera RayCamera;
        public LightSourceSampler LightSourceSampler;
        public SubpathSampler EyePathSampler;
        public SubpathSampler LightPathSampler;
        public FullPathSampler FullPathSampler;
        public IPhaseFunctionSampler PhaseFunction;
    }
}
