using System;
using SubpathGenerator;
using System.Threading;
using RayCameraNamespace;

namespace Photonusmap
{
    public class ConstruktorDataForPhotonmapCreation
    {
        public SubpathSampler LightPathSampler;
        public int FrameId; //frameId = Das wie vielte Frame aus dem FrameColorEstimator ist das? Bei ein PixelEstimator kann hier eine 0 eingesetzt werden
        public int LightPathCount;
        public Action<string, float> ProgressChanged;
        public CancellationTokenSource StopTrigger;
        public int ThreadCount;
        public IRayCamera RayCamera; //Um den maximal erlaubten Volumetric-Photonn-Radius zu bestimmen
    }
}
