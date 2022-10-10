using System.Linq;
using SubpathGenerator;
using RaytracingLightSource;

namespace FullPathGenerator
{
    //Erzeugt nur C S* L-Pfade; Wird beim Photonmapping benötigt damit die Lichtquelle nicht ein schwarzes Objekt ist
    class SpecularPathtracing : PathTracing
    {
        public SpecularPathtracing(LightSourceSampler lightSourceSampler, PathSamplingType usedEyeSubPathType)
            : base(lightSourceSampler, usedEyeSubPathType)
        {
        }
        public new SamplingMethod Name => SamplingMethod.SpecularPathtracing;

        public override int SampleCountForGivenPath(FullPath path)
        {
            if (path.Points.Last().IsLocatedOnLightSourceWhichCanBeHitViaPathtracing == false) return 0;
            if (this.checkThatEachPointIsASurfacePoint && path.IsSurfacePathOnly() == false) return 0;
            if (IsSpecularPath(path) == false) return 0;

            return 1;
        }

        protected override FullPath TryToCreatePath(PathPoint eyePoint)
        {
            if (eyePoint.IsLocatedOnLightSourceWhichCanBeHitViaPathtracing == false) return null;
            if (IsSpecularSubPath(eyePoint.AssociatedPath.Points) == false) return null;

            var path = CreatePath(eyePoint);
            path.Sampler = this;
            return path;
        }

        public override double GetPathPdfAForAGivenPath(FullPath path, FullPathFrameData frameData)
        {
            var lightPoint = path.Points.Last();
            if (lightPoint.IsLocatedOnLightSourceWhichCanBeHitViaPathtracing == false) return 0;
            if (this.checkThatEachPointIsASurfacePoint && path.IsSurfacePathOnly() == false) return 0;

            if (IsSpecularPath(path) == false) return 0;

            return lightPoint.EyePdfA;
        }

        private bool IsSpecularPath(FullPath path)
        {
            //if (path.Points.Length <= 3) return false; //Das war ein Test, weil ich das Stilllife-OriginalRefenzbild so nachmachen wollte (SpecularPathTracing braucht direktes Licht, da sonst die Lampen bei Photonmapping schwarz sind)
            for (int i = 1; i < path.Points.Length - 1; i++)
            {
                if (path.Points[i].IsSpecularSurfacePoint == false) return false;
            }
            return true;
        }

        private bool IsSpecularSubPath(PathPoint[] points)
        {
            //if (points.Length <= 3) return false;  //Das war ein Test, weil ich das Stilllife-OriginalRefenzbild so nachmachen wollte
            for (int i = 1; i < points.Length - 1; i++)
            {
                if (points[i].IsSpecularSurfacePoint == false) return false;
            }
            return true;
        }
    }
}
