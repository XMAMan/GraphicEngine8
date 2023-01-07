using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;
using Photonusmap;
using RaytracingBrdf.SampleAndRequest;
using RaytracingColorEstimator;
using SubpathGenerator;
using System.Collections.Generic;

namespace RaytracingMethods.McVcm
{
    //Macht das gleiche wie VertexMerging nur dass anstatt einer Light- eine EyePhotonmap verwendet wird und
    //beim Sampeln eine Liste von Fullpaths erzeugt wird, wo die PixelPosition-Property gesetzt ist (Wie beim LightTracing)
    class EyeMapVertexMerging : VertexMerging, IFullPathSamplingMethod
    {
        public EyeMapVertexMerging(int maxPathLength, PathSamplingType usedSubPathSamplingType)
            :base(maxPathLength, usedSubPathSamplingType)
        {
        }

        public override List<FullPath> SampleFullPaths(SubPath eyePath, SubPath lightPath, FullPathFrameData frameData, IRandom rand)
        {
            return base.SampleFullPaths(lightPath, null, frameData, rand);
        }

        protected override FullPath CreatePath(PathPoint eyePoint, PathPoint lightPoint, BrdfEvaluateResult eyeBrdf, ISurfacePhotonmap photonmap)
        {
            var fullPath = base.CreatePath(lightPoint, eyePoint, eyeBrdf, photonmap);

            //Setze die PixelPosition-Property
            EyeSubPath eyePath = lightPoint.AssociatedPath as EyeSubPath;
            fullPath.PixelPosition = eyePath.PixelPosition;

            return fullPath;
        }
        
    }
}
