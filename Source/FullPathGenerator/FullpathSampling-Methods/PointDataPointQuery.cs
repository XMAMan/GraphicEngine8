using System.Collections.Generic;
using System.Linq;
using SubpathGenerator;
using Photonusmap;
using RaytracingBrdf;
using GraphicMinimal;
using GraphicGlobal;
using RaytracingBrdf.SampleAndRequest;

namespace FullPathGenerator
{
    public class PointDataPointQuery : IFullPathSamplingMethod
    {
        private readonly int maxPathLength;

        public PointDataPointQuery(int maxPathLength)
        {
            this.maxPathLength = maxPathLength;
        }
        public SamplingMethod Name => SamplingMethod.PointDataPointQuery;
        public int SampleCountForGivenPath(FullPath path)
        {
            return path.Points.Count(x => x.LocationType == ParticipatingMedia.MediaPointLocationType.MediaParticle);
        }
        public List<FullPath> SampleFullPaths(SubPath eyePath, SubPath lightPath, FullPathFrameData frameData, IRandom rand)
        {
            List<FullPath> paths = new List<FullPath>();

            PointDataPointQueryMap photonmap = frameData.PhotonMaps.PointDataPointQueryMap;
            float kernelFunction = photonmap.KernelFunction(photonmap.SearchRadius);
            
            if (float.IsInfinity(kernelFunction)) return paths;
            if (eyePath == null) return paths;

            for (int i = 1; i < eyePath.Points.Length; i++)
            {
                paths.AddRange(TryToCreatePaths(eyePath.Points[i], photonmap));
            }
            return paths;
        }

        protected List<FullPath> TryToCreatePaths(PathPoint eyePoint, PointDataPointQueryMap photonmap)
        {
            List<FullPath> paths = new List<FullPath>();

            if (eyePoint.IsLocatedOnLightSource == false && eyePoint.LocationType == ParticipatingMedia.MediaPointLocationType.MediaParticle)
            {
                var photons = photonmap.QuerryPhotons(eyePoint.Position, photonmap.SearchRadius).ToList();

                foreach (var lightPoint in photons)
                {
                    if (lightPoint.Index + eyePoint.Index < this.maxPathLength)
                    {
                        var eyeBrdf = PhaseFunction.EvaluateBsdf(eyePoint.DirectionToThisPoint, eyePoint.MediaPoint, -lightPoint.DirectionToThisPoint); //Point2Point-Merging

                        if (eyeBrdf != null)
                        {
                            var path = CreatePath(eyePoint, lightPoint, eyeBrdf, photonmap);
                            paths.Add(path);
                        }
                    }
                }
            }

            return paths;
        }

        private FullPath CreatePath(PathPoint eyePoint, PathPoint lightPoint, BrdfEvaluateResult eyeBrdf, PointDataPointQueryMap photonmap)
        {
            SubPath eyePath = eyePoint.AssociatedPath;
            SubPath lightPath = lightPoint.AssociatedPath;

            float kernelFunction = photonmap.KernelFunction(photonmap.SearchRadius);

            float acceptancePdf = 1.0f / kernelFunction;

            Vector3D pathContribution = Vector3D.Mult(Vector3D.Mult(eyePoint.PathWeight, lightPoint.PathWeight), eyeBrdf.Brdf) * kernelFunction / photonmap.LightPathCount;
            double pathPdfA = eyePoint.PdfA * lightPoint.PdfA * acceptancePdf * photonmap.LightPathCount;

            var points = new FullPathPoint[eyePoint.Index + lightPoint.Index + 1];
            for (int i = 0; i < eyePoint.Index; i++) points[i] = new FullPathPoint(eyePath.Points[i], eyePath.Points[i].LineToNextPoint, null, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfW, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfWReverse, BrdfCreator.BrdfSampling) { EyePdfA = eyePath.Points[i].PdfA };

            points[eyePoint.Index - 1].LightPdfA = lightPoint.PdfA * PdfHelper.PdfWToPdfAOrV(eyeBrdf.PdfWReverse, eyePoint, eyePoint.Predecessor) * eyePoint.Predecessor.PdfLFromNextPointToThis;
            points[eyePoint.Index] = new FullPathPoint(eyePath.Points[eyePoint.Index], null, null, eyeBrdf.PdfW, eyeBrdf.PdfWReverse, BrdfCreator.MergingPoint) { EyePdfA = eyePoint.PdfA, LightPdfA = lightPoint.PdfA };
            points[eyePoint.Index + 1] = new FullPathPoint(lightPoint.Predecessor, null, lightPoint.Predecessor.LineToNextPoint, lightPoint.Predecessor.BrdfSampleEventOnThisPoint.PdfWReverse, lightPoint.Predecessor.BrdfSampleEventOnThisPoint.PdfW, BrdfCreator.BrdfSampling) { LightPdfA = lightPoint.Predecessor.PdfA, PdfWContainsNumericErrors = true };
            points[eyePoint.Index + 1].EyePdfA = eyePoint.PdfA * PdfHelper.PdfWToPdfAOrV(eyeBrdf.PdfW, eyePoint, lightPoint.Predecessor) * lightPoint.Predecessor.PdfLFromNextPointToThis;

            double lightPdfA = points[eyePoint.Index - 1].LightPdfA;
            for (int j = eyePoint.Index - 2; j >= 0; j--)
            {
                lightPdfA = lightPdfA * eyePath.Points[j].PdfAReverse * eyePath.Points[j].PdfLFromNextPointToThis;
                points[j].LightPdfA = lightPdfA;
            }

            double eyePdfA = points[eyePoint.Index + 1].EyePdfA;
            for (int i = eyePoint.Index + 2, j = lightPoint.Index - 2; i < points.Length; i++, j--)
            {
                eyePdfA = eyePdfA * lightPath.Points[j].PdfAReverse * lightPath.Points[j].PdfLFromNextPointToThis;
                points[i] = new FullPathPoint(lightPath.Points[j], null, lightPath.Points[j].LineToNextPoint, lightPath.Points[j].BrdfSampleEventOnThisPoint.PdfWReverse, lightPath.Points[j].BrdfSampleEventOnThisPoint.PdfW, BrdfCreator.BrdfSampling)
                {
                    EyePdfA = eyePdfA,
                    LightPdfA = lightPath.Points[j].PdfA
                };
            }
            //points[eyePoint.Index + 1].LightLineToNext = null;

            return new FullPath(pathContribution, pathPdfA, points, this);
        }

        public double GetPathPdfAForAGivenPath(FullPath path, FullPathFrameData frameData)
        {
            PointDataPointQueryMap photonmap = frameData.PhotonMaps.PointDataPointQueryMap;
            float acceptancePdf = 1.0f / photonmap.KernelFunction(photonmap.SearchRadius);

            double sum = 0;

            for (int i = 1; i < path.Points.Length - 1; i++)
            {
                if (path.Points[i].LocationType == ParticipatingMedia.MediaPointLocationType.MediaParticle)
                {
                    sum += path.Points[i].EyePdfA * path.Points[i].LightPdfA * acceptancePdf * photonmap.LightPathCount;
                }
            }
            return sum;
        }
    }
}
