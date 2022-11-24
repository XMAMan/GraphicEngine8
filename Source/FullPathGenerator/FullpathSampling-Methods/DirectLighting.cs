using System.Collections.Generic;
using System.Linq;
using RaytracingLightSource;
using SubpathGenerator;
using GraphicMinimal;
using GraphicGlobal;
using FullPathGenerator.FullpathSampling_Methods;

namespace FullPathGenerator
{
    public class DirectLighting : IFullPathSamplingMethod, ISingleFullPathSampler
    {
        protected LightSourceSampler lightSourceSampler;
        protected int maximumDirectLightingEyeIndex;
        protected PointToPointConnector pointToPointConnector;
        protected bool checkThatEachPointIsASurfacePoint;

        public DirectLighting(LightSourceSampler lightSourceSampler, int maximumDirectLightingEyeIndex, PointToPointConnector pointToPointConnector, PathSamplingType usedEyeSubPathType)
        {
            this.checkThatEachPointIsASurfacePoint = usedEyeSubPathType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling;
            this.lightSourceSampler = lightSourceSampler;
            this.maximumDirectLightingEyeIndex = maximumDirectLightingEyeIndex;
            this.pointToPointConnector = pointToPointConnector;
        }

        public SamplingMethod Name => SamplingMethod.DirectLighting;

        private readonly int minEyeIndex = 1;

        public virtual int SampleCountForGivenPath(FullPath path)
        {
            if (path.PathLength > 2 && path.PathLength <= (this.maximumDirectLightingEyeIndex + 2) && path.Points[path.Points.Length - 2].IsDiffusePoint) return 1;
            return 0;
        }

        public virtual List<FullPath> SampleFullPaths(SubPath eyePath, SubPath lightPath, FullPathFrameData frameData, IRandom rand)
        {
            List<FullPath> paths = new List<FullPath>();
            if (eyePath == null) return paths;
            for (int i = minEyeIndex; i < eyePath.Points.Length; i++)
            {
                var path = TryToCreatePath(eyePath.Points[i], rand);
                if (path != null) paths.Add(path);
            }
            return paths;
        }

        private FullPath TryToCreatePath(PathPoint eyePoint, IRandom rand)
        {
            if (eyePoint.IsLocatedOnLightSource == false && eyePoint.IsDiffusePoint && eyePoint.Index >= minEyeIndex && eyePoint.Index <= this.maximumDirectLightingEyeIndex)
            {
                var toLightDirection = this.lightSourceSampler.GetRandomPointOnLight(eyePoint.Position, rand); //Ligthsource-Sampling
                if (toLightDirection == null) return null;
                var connectData = this.pointToPointConnector.TryToConnectToLightSource(eyePoint, toLightDirection);
                if (connectData == null) return null;

                //Der LightPoint, welcher durch den Visible-Test erzeugt wurde liegt nicht exakt dort, wo der gesampelte LightPoint liegt. Deswegen kommt es zu PdfA-Abweichungen/MIS-OutOfRange-Exception. Hiermit vermeide ich diesen Fehler.
                double directLightingPdfA = this.lightSourceSampler.GetDirectLightingPdfA(eyePoint.Position, connectData.LightPoint, eyePoint.AssociatedPath.PathCreationTime);
                if (directLightingPdfA == 0) return null;

                var path = CreatePath(eyePoint, connectData, directLightingPdfA);
                return path;
            }

            return null;
        }

        protected FullPath CreatePath(PathPoint eyePoint, EyePoint2LightSourceConnectionData connectData, double directLightingPdfA)
        {
            var eyePath = eyePoint.AssociatedPath;
            var lightPoint = connectData.LightPoint;

            double pathPdfA = eyePoint.PdfA * directLightingPdfA;

            Vector3D pathContribution = Vector3D.Mult(Vector3D.Mult(eyePoint.PathWeight, connectData.EyeBrdf.Brdf), lightPoint.Color) * connectData.GeometryTerm * this.lightSourceSampler.GetEmissionForEyePathHitLightSourceDirectly(lightPoint, eyePoint.Position, connectData.DirectionToLightPoint) / (float)directLightingPdfA;// toLightDirection.PdfA;
            pathContribution = Vector3D.Mult(pathContribution, connectData.AttenuationTerm);

            var points = new FullPathPoint[eyePoint.Index + 2];
            double lightPdfA = this.lightSourceSampler.PdfAFromRandomPointOnLightSourceSampling(lightPoint);
            float pdfWFromLightDirectionSampling = this.lightSourceSampler.PdfWFromLightDirectionSampling(lightPoint, -connectData.DirectionToLightPoint);

            var lightPathPoint = PathPoint.CreateLightsourcePointWithSurroundingMedia(lightPoint, null, connectData.MediaLightPoint, connectData.LighIsInfinityAway);
            points[points.Length - 1] = new FullPathPoint(lightPathPoint, null, null, float.NaN, pdfWFromLightDirectionSampling, BrdfCreator.BrdfSampling) { EyePdfA = eyePoint.PdfA * PdfHelper.PdfWToPdfAOrV(connectData.EyeBrdf.PdfW, eyePoint, lightPathPoint) * connectData.PdfLForEyepointToLightSource.PdfL, LightPdfA = lightPdfA };
            points[points.Length - 1].Point.AssociatedPath = eyePath;
            
            lightPdfA = lightPdfA * PdfHelper.PdfWToPdfAOrV(pdfWFromLightDirectionSampling, lightPathPoint, eyePoint) * connectData.PdfLForEyepointToLightSource.ReversePdfL;
            points[points.Length - 2] = new FullPathPoint(eyePoint, connectData.LineFromEyePointToLightSource, null, connectData.EyeBrdf.PdfW, connectData.EyeBrdf.PdfWReverse, BrdfCreator.BrdfEvaluation) { EyePdfA = eyePoint.PdfA, LightPdfA = lightPdfA };

            float eyePredecessorPdfA = PdfHelper.PdfWToPdfAOrV(connectData.EyeBrdf.PdfWReverse, eyePoint, eyePoint.Predecessor);
            lightPdfA = lightPdfA * eyePredecessorPdfA * eyePoint.Predecessor.PdfLFromNextPointToThis;
            points[points.Length - 3] = new FullPathPoint(eyePoint.Predecessor, eyePoint.Predecessor.LineToNextPoint, null, eyePoint.Predecessor.BrdfSampleEventOnThisPoint.PdfW, eyePoint.Predecessor.BrdfSampleEventOnThisPoint.PdfWReverse, BrdfCreator.BrdfSampling) { EyePdfA = eyePoint.Predecessor.PdfA, LightPdfA = lightPdfA };

            for (int i = points.Length - 4; i >= 0; i--)
            {
                lightPdfA = lightPdfA * eyePath.Points[i].PdfAReverse * eyePath.Points[i].PdfLFromNextPointToThis;
                points[i] = new FullPathPoint(eyePath.Points[i], eyePath.Points[i].LineToNextPoint, null, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfW, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfWReverse, BrdfCreator.BrdfSampling) 
                { 
                    EyePdfA = eyePath.Points[i].PdfA, 
                    LightPdfA = lightPdfA, 
                };
            }

            return new FullPath(pathContribution, pathPdfA, points, this);
        }

        public virtual double GetPathPdfAForAGivenPath(FullPath path, FullPathFrameData frameData)
        {
            if (this.checkThatEachPointIsASurfacePoint && path.IsSurfacePathOnly() == false) return 0;
            int directLightingEyePathIndex = path.Points.Length - 2;

            if (directLightingEyePathIndex >= minEyeIndex && directLightingEyePathIndex <= this.maximumDirectLightingEyeIndex && path.Points[directLightingEyePathIndex].IsDiffusePoint)
            {
                double directLightingPdfA = this.lightSourceSampler.GetDirectLightingPdfA(path.Points[path.Points.Length - 2].Position, path.Points.Last().Point.SurfacePoint, path.Points.First().Point.AssociatedPath.PathCreationTime);
                return path.Points[directLightingEyePathIndex].EyePdfA * directLightingPdfA;
            }
            return 0;
        }

        #region ISingleFullPathSampler
        public FullPathSamplingStrategy[] GetAvailableStrategiesForFullPathLength(int fullPathLength)
        {
            if (fullPathLength <= 2) return new FullPathSamplingStrategy[0];

            return new FullPathSamplingStrategy[] 
            {
                new FullPathSamplingStrategy()
                {
                    NeededEyePathLength = fullPathLength - 1,
                    NeededLightPathLength = 0,
                    StrategyIndex = 0
                }
            };
        }
        public FullPath SampleFullPathFromSingleStrategy(SubPath eyePath, SubPath lightPath, int fullPathLength, int strategyIndex, IRandom rand)
        {
            return TryToCreatePath(eyePath.Points[fullPathLength - 2], rand);
        }
        #endregion 
    }
}
