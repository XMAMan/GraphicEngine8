using System.Collections.Generic;
using System.Linq;
using SubpathGenerator;
using RaytracingLightSource;
using GraphicMinimal;
using GraphicGlobal;
using FullPathGenerator.FullpathSampling_Methods;

namespace FullPathGenerator
{
    public class PathTracing : IFullPathSamplingMethod, ISingleFullPathSampler
    {
        private readonly LightSourceSampler lightSourceSampler;
        protected bool checkThatEachPointIsASurfacePoint;

        public PathTracing(LightSourceSampler lightSourceSampler, PathSamplingType usedEyeSubPathType)
        {
            this.checkThatEachPointIsASurfacePoint = usedEyeSubPathType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling;
            this.lightSourceSampler = lightSourceSampler;
        }

        public SamplingMethod Name => SamplingMethod.PathTracing;
        public virtual int SampleCountForGivenPath(FullPath path)
        {
            if (path.Points.Last().IsLocatedOnLightSourceWhichCanBeHitViaPathtracing == false) return 0;
            if (this.checkThatEachPointIsASurfacePoint && path.IsSurfacePathOnly() == false) return 0;

            return 1;
        }
        public List<FullPath> SampleFullPaths(SubPath eyePath, SubPath lightPath, FullPathFrameData frameData, IRandom rand)
        {
            List<FullPath> paths = new List<FullPath>();
            if (eyePath == null) return paths;
            var path = TryToCreatePath(eyePath.Points.Last());
            if (path != null) paths.Add(path);
            return paths;
        }

        protected virtual FullPath TryToCreatePath(PathPoint eyePoint)
        {
            if (eyePoint.IsLocatedOnLightSourceWhichCanBeHitViaPathtracing == false) return null;

            return CreatePath(eyePoint);
        }

        protected FullPath CreatePath(PathPoint eyePoint)
        {
            Vector3D pathContribution = Vector3D.Mult(eyePoint.PathWeight, eyePoint.SurfacePoint.Color) * lightSourceSampler.GetEmissionForEyePathHitLightSourceDirectly(eyePoint.SurfacePoint, eyePoint.Predecessor.Position, eyePoint.DirectionToThisPoint);
            double pathPdfA = eyePoint.PdfA;
            
            var eyePath = eyePoint.AssociatedPath;

            FullPathPoint[] points = new FullPathPoint[eyePath.Points.Length];
            double lightPdfA = lightSourceSampler.PdfAFromRandomPointOnLightSourceSampling(eyePoint.SurfacePoint);
            float pdfWFromLightDirectionSampling = this.lightSourceSampler.PdfWFromLightDirectionSampling(eyePoint.SurfacePoint, -eyePoint.DirectionToThisPoint);
            points[points.Length - 1] = new FullPathPoint(eyePoint, null, null, float.NaN, pdfWFromLightDirectionSampling, BrdfCreator.BrdfSampling) { EyePdfA = eyePoint.PdfA, LightPdfA = lightPdfA };

            lightPdfA *= PdfHelper.PdfWToPdfAOrV(pdfWFromLightDirectionSampling, eyePoint, eyePoint.Predecessor) * eyePoint.Predecessor.PdfLFromNextPointToThis;

            points[points.Length - 2] = new FullPathPoint(eyePoint.Predecessor, eyePoint.Predecessor.LineToNextPoint, null, eyePoint.Predecessor.BrdfSampleEventOnThisPoint.PdfW, eyePoint.Predecessor.BrdfSampleEventOnThisPoint.PdfWReverse, BrdfCreator.BrdfSampling) { EyePdfA = eyePoint.Predecessor.PdfA, LightPdfA = lightPdfA };
            for (int i = points.Length - 3; i >= 0; i--)
            {
                lightPdfA = lightPdfA * eyePath.Points[i].PdfAReverse * eyePath.Points[i].PdfLFromNextPointToThis;
                points[i] = new FullPathPoint(eyePath.Points[i], eyePath.Points[i].LineToNextPoint, null, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfW, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfWReverse, BrdfCreator.BrdfSampling) { EyePdfA = eyePath.Points[i].PdfA, LightPdfA = lightPdfA };
            }

            return new FullPath(pathContribution, pathPdfA, points, this);
        }

        public virtual double GetPathPdfAForAGivenPath(FullPath path, FullPathFrameData frameData)
        {
            var lightPoint = path.Points.Last();

            if (lightPoint.IsLocatedOnLightSourceWhichCanBeHitViaPathtracing == false) return 0;
            if (this.checkThatEachPointIsASurfacePoint && path.IsSurfacePathOnly() == false) return 0;

            return lightPoint.EyePdfA;
        }

        #region ISingleFullPathSampler
        public FullPathSamplingStrategy[] GetAvailableStrategiesForFullPathLength(int fullPathLength)
        {
            if (fullPathLength <= 1) return new FullPathSamplingStrategy[0];

            return new FullPathSamplingStrategy[]
            {
                new FullPathSamplingStrategy()
                {
                    NeededEyePathLength = fullPathLength,
                    NeededLightPathLength = 0,
                    StrategyIndex = 0
                }
            };
        }
        public FullPath SampleFullPathFromSingleStrategy(SubPath eyePath, SubPath lightPath, int fullPathLength, int strategyIndex, IRandom rand)
        {
            return TryToCreatePath(eyePath.Points[fullPathLength - 1]);
        }
        #endregion 
    }
}
