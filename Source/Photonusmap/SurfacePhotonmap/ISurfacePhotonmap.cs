using System.Collections.Generic;
using SubpathGenerator;
using GraphicMinimal;

namespace Photonusmap
{
    public interface IPhotonmap
    {
        float SearchRadius { get; }
        int LightPathCount { get; } //So viele Sub-LightPaths wurden ausgesendet um die Photonmap zu erstellen
        int MinPhotonCountForRadianceEstimation { get; }
        int MinEyeIndex { get; } //Für diese Eye-Indizes ist die Phtonmap gedacht
        int MaxEyeIndex { get; }
    }

    public interface ISurfacePhotonmap : IPhotonmap
    {
        IEnumerable<PathPoint> QuerrySurfacePhotons(Vector3D querryPosition, float searchRadius);
        float KernelFunction(float distanceSquareToCenter, float searchRadiusSquare);
        bool ContainsOnlySpecularLightPahts { get; }
    }
}
