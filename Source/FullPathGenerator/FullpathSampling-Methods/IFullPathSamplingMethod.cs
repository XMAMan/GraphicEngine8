using System.Collections.Generic;
using GraphicGlobal;
using SubpathGenerator;

namespace FullPathGenerator
{
    //Erzeugt potentiell für jede Pfadlänge mehrere Fullpaths
    public interface IFullPathSamplingMethod
    {
        SamplingMethod Name { get; }
        int SampleCountForGivenPath(FullPath path); //So viele Möglichkeiten gibt es, das dieses Verfahren genau diesen Pfad hier erzeugt
        List<FullPath> SampleFullPaths(SubPath eyePath, SubPath lightPath, FullPathFrameData frameData, IRandom rand);
        double GetPathPdfAForAGivenPath(FullPath path, FullPathFrameData frameData);
    }

    //Für den ImageFullPathAnalyser will ich wissen, welcher FullPath von welchen Verfahren erzeugt wurde
    public enum SamplingMethod
    {
        DirectLighting,
        DirectLightingOnEdge,
        LightTracing,
        LightTracingOnEdge,
        MultipleDirectLighting,
        PathTracing,
        SpecularPathtracing,
        VertexConnection,
        VertexConnectionWithImportanceSampling,
        VertexMerging,
        PointDataPointQuery,    //Volumetrisches Photonmapping
        PointDataBeamQuery,     //Beam Radiance Estimate
        BeamDataLineQuery       //Beam-Beam
    }
}
