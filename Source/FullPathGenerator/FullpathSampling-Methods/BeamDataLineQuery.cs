using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests.BeamLine;
using ParticipatingMedia;
using ParticipatingMedia.DistanceSampling;
using Photonusmap;
using RayTracerGlobal;
using RaytracingBrdf;
using SubpathGenerator;
using System.Collections.Generic;

namespace FullPathGenerator
{
    //A Comprehensive Theory of Volumetric Radiance Estimation using Photon Points and Beams [2011] (Beam Query x Beam Data 1D Blur)
    //https://pdfs.semanticscholar.org/344e/c434457b5888057114dc816da77196661fea.pdf
    public class BeamDataLineQuery : IFullPathSamplingMethod
    {
        private readonly bool noDistanceSampling;
        private readonly int maxPathLength;
        private readonly IPhaseFunctionSampler phaseFunction;

        public BeamDataLineQuery(PathSamplingType usedEyeSubPathType, int maxPathLength, IPhaseFunctionSampler phaseFunction)
        {
            this.noDistanceSampling = usedEyeSubPathType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling;
            this.maxPathLength = maxPathLength;
            this.phaseFunction = phaseFunction;
        }

        public SamplingMethod Name => SamplingMethod.BeamDataLineQuery;

        public int SampleCountForGivenPath(FullPath path)
        {
            int pCounter = 0; //ParticleCounter

            //Zähle alle Particle
            for (int i = 1; i < path.PathLength - 1; i++)
            {
                if (path.Points[i].LocationType == MediaPointLocationType.MediaParticle)
                    pCounter++;
            }

            return pCounter;
        }

        public List<FullPath> SampleFullPaths(SubPath eyePath, SubPath lightPath, FullPathFrameData frameData, IRandom rand)
        {
            List<FullPath> paths = new List<FullPath>();
            if (eyePath == null) return paths;

            for (int i = 0; i < eyePath.Points.Length; i++)
            {
                PathPoint eyePoint = eyePath.Points[i];
                if (eyePoint.LineToNextPoint != null)
                {
                    if (eyePoint.LineToNextPoint.HasScattering() == false) continue;
                    var intersectionPoints = frameData.PhotonMaps.BeamDataLineQueryMap.QuerryBeamRays(eyePoint.LineToNextPoint);

                    foreach (var intersectionData in intersectionPoints)
                    {
                        PathPoint lightPoint = (intersectionData.IntersectedBeam as BeamRay).LightPoint;
                        if (lightPoint.Index + eyePoint.Index + 3 <= this.maxPathLength)
                        {                         
                            var mediaFromEyeIntersectionPoint = eyePoint.LineToNextPoint.GetMedia(intersectionData.LineIntersectionPosition);
                            var mediaFromLightIntersectionPoint = lightPoint.LineToNextPoint.GetMedia(intersectionData.BeamIntersectionPosition);

                            if (mediaFromEyeIntersectionPoint.HasScatteringSomeWhereInMedium() &&
                                mediaFromLightIntersectionPoint.HasScatteringSomeWhereInMedium() &&
                                mediaFromEyeIntersectionPoint.HasScatteringOnPoint(eyePoint.LineToNextPoint.GetPositionFromDistance(intersectionData.LineIntersectionPosition)) &&
                                mediaFromLightIntersectionPoint.HasScatteringOnPoint(lightPoint.LineToNextPoint.GetPositionFromDistance(intersectionData.BeamIntersectionPosition)) &&
                                intersectionData.LineIntersectionPosition > MagicNumbers.MinAllowedPathPointDistance &&
                                intersectionData.BeamIntersectionPosition > MagicNumbers.MinAllowedPathPointDistance)
                            {
                                var eyeSubLine = eyePoint.LineToNextPoint.CreateShortMediaSubLine(intersectionData.LineIntersectionPosition);
                                var lightSubLine = lightPoint.LineToNextPoint.CreateShortMediaSubLine(intersectionData.BeamIntersectionPosition);


                                var path = CreatePath(new PathPoint(eyePoint) { LineToNextPoint = eyeSubLine }, new PathPoint(lightPoint) { LineToNextPoint = lightSubLine }, intersectionData, frameData.PhotonMaps.BeamDataLineQueryMap);
                                paths.Add(path);
                            }                            
                        }
                    }
                }
            }
            
            return paths;
        }

        //Auf den Endpunkten von den LineToNext-Linien vom eyePoint und vom lightPoint treffen die Linien zusammen
        //Bei ein Fullpath mit 3 Punkten liegt eyePoint auf der Kamera und lightPoint auf der Lichtquelle; eyePoint.LineToNext.EndPoint ist dann der Merging-Point(pointOnT)
        private FullPath CreatePath(PathPoint eyePoint, PathPoint lightPoint, LineBeamIntersectionPoint intersectionData, BeamDataLineQueryMap photonmap)
        {
            //Eye-Merging-Point
            SubPath eyePath = eyePoint.AssociatedPath;

            Vector3D pathweightOnEyeMergingPoint = Vector3D.Mult(eyePoint.PathWeight, eyePoint.BrdfSampleEventOnThisPoint.Brdf);
            pathweightOnEyeMergingPoint = Vector3D.Mult(pathweightOnEyeMergingPoint, eyePoint.LineToNextPoint.AttenuationWithoutPdf()); //LineToNextPoint wurde bis zum Photon gestutzt 

            var eyeLinePdf = this.noDistanceSampling ? new DistancePdf() { PdfL = 1, ReversePdfL = 1 } : eyePoint.LineToNextPoint.GetPdfLIfDistanceSamplingWouldBeUsed();
            PathPoint pointOnT = PathPoint.CreateMediaParticlePoint(eyePoint.LineToNextPoint.EndPoint, pathweightOnEyeMergingPoint); //Punkt auf der Eye-Medialine in der Nähe vom Photon
            pointOnT.PdfA = eyePoint.PdfA * PdfHelper.PdfWToPdfAOrV(eyePoint.BrdfSampleEventOnThisPoint.PdfW, eyePoint, pointOnT) * eyeLinePdf.PdfL;
            pointOnT.AssociatedPath = eyePoint.AssociatedPath;
            pointOnT.Predecessor = eyePoint;

            //Light-Merging-Point
            SubPath lightPath = lightPoint.AssociatedPath;

            Vector3D pathweightOnLightMergingPoint = Vector3D.Mult(lightPoint.PathWeight, lightPoint.BrdfSampleEventOnThisPoint.Brdf);
            pathweightOnLightMergingPoint = Vector3D.Mult(pathweightOnLightMergingPoint, lightPoint.LineToNextPoint.AttenuationWithoutPdf()); //LineToNextPoint wurde bis zum Photon gestutzt 

            var lightLinePdf = this.noDistanceSampling ? new DistancePdf() { PdfL = 1, ReversePdfL = 1 } : lightPoint.LineToNextPoint.GetPdfLIfDistanceSamplingWouldBeUsed();
            double lightPointPdfA = lightPoint.PdfA * PdfHelper.PdfWtoV(lightPoint.BrdfSampleEventOnThisPoint.PdfW, lightPoint.Position, lightPoint.LineToNextPoint.EndPoint.Position) * lightLinePdf.PdfL;


            //Merging-Bsdf
            var pointTBsdf = this.phaseFunction.EvaluateBsdf(pointOnT.DirectionToThisPoint, pointOnT.MediaPoint, -lightPoint.LineToNextPoint.Ray.Direction);

            float kernel1D = 1.0f / (photonmap.SearchRadius * 2);
            float acceptancePdf = 1.0f / kernel1D;

            kernel1D = (1 - intersectionData.Distance * intersectionData.Distance / (photonmap.SearchRadius * photonmap.SearchRadius)) * 3 / (4 * photonmap.SearchRadius); //Epanechnikov Kernel

            Vector3D pathContribution = Vector3D.Mult(Vector3D.Mult(pathweightOnLightMergingPoint, pointTBsdf.Brdf) * kernel1D, pathweightOnEyeMergingPoint) / photonmap.LightPathCount / intersectionData.SinTheta;
            double pathPdfA = pointOnT.PdfA * lightPointPdfA * acceptancePdf * photonmap.LightPathCount * intersectionData.SinTheta;

            var points = new FullPathPoint[eyePoint.Index + lightPoint.Index + 3];

            double lightPdfA = lightPointPdfA;

            //Eye-Merging-Point ConnectionPunkt (Hat keine LineToNexts)
            points[eyePoint.Index + 1] = new FullPathPoint(pointOnT, null, null, pointTBsdf.PdfW, pointTBsdf.PdfWReverse, BrdfCreator.MergingPoint)
            {
                EyePdfA = pointOnT.PdfA,
                LightPdfA = lightPdfA
            };
            lightPdfA = lightPdfA * PdfHelper.PdfWToPdfAOrV(pointTBsdf.PdfWReverse, pointOnT, eyePoint) * eyeLinePdf.ReversePdfL;

            //eyePoint
            points[eyePoint.Index] = new FullPathPoint(eyePoint, eyePoint.LineToNextPoint, null, eyePoint.BrdfSampleEventOnThisPoint.PdfW, eyePoint.BrdfSampleEventOnThisPoint.PdfWReverse, BrdfCreator.BrdfSampling)
            {
                EyePdfA = eyePoint.PdfA,
                LightPdfA = lightPdfA,
                PdfWContainsNumericErrors = true
            };

            //EyeSubpath
            for (int i = eyePoint.Index - 1; i >= 0; i--)
            {
                lightPdfA = lightPdfA * eyePath.Points[i].PdfAReverse * eyePath.Points[i].PdfLFromNextPointToThis;
                points[i] = new FullPathPoint(eyePath.Points[i], eyePath.Points[i].LineToNextPoint, null, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfW, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfWReverse, BrdfCreator.BrdfSampling)
                {
                    EyePdfA = eyePath.Points[i].PdfA,
                    LightPdfA = lightPdfA
                };
            }

            double eyePdfA = pointOnT.PdfA;

            //LightPoint
            eyePdfA = eyePdfA * PdfHelper.PdfWToPdfAOrV(pointTBsdf.PdfW, pointOnT, lightPoint) * lightLinePdf.ReversePdfL;
            points[eyePoint.Index + 2] = new FullPathPoint(lightPoint, null, lightPoint.LineToNextPoint, lightPoint.BrdfSampleEventOnThisPoint.PdfWReverse, lightPoint.BrdfSampleEventOnThisPoint.PdfW, BrdfCreator.BrdfSampling)
            {
                EyePdfA = eyePdfA,
                LightPdfA = lightPoint.PdfA,
                PdfWContainsNumericErrors = true
            };

            //Lightpoint-Precedor
            if (lightPoint.Index - 1 >= 0)
            {
                eyePdfA = eyePdfA * PdfHelper.PdfWToPdfAOrV(lightPoint.BrdfSampleEventOnThisPoint.PdfWReverse, lightPoint, lightPoint.Predecessor) * lightPoint.Predecessor.PdfLFromNextPointToThis;
                points[eyePoint.Index + 3] = new FullPathPoint(lightPoint.Predecessor, null, lightPoint.Predecessor.LineToNextPoint, lightPoint.Predecessor.BrdfSampleEventOnThisPoint.PdfWReverse, lightPoint.Predecessor.BrdfSampleEventOnThisPoint.PdfW, BrdfCreator.BrdfSampling)
                {
                    EyePdfA = eyePdfA,
                    LightPdfA = lightPoint.Predecessor.PdfA
                };
            }

            //Light-Subpath            
            for (int i = eyePoint.Index + 4, j = lightPoint.Index - 2; i < points.Length; i++, j--)
            {
                eyePdfA = eyePdfA * lightPath.Points[j].PdfAReverse * lightPath.Points[j].PdfLFromNextPointToThis;
                points[i] = new FullPathPoint(lightPath.Points[j], null, lightPath.Points[j].LineToNextPoint, lightPath.Points[j].BrdfSampleEventOnThisPoint.PdfWReverse, lightPath.Points[j].BrdfSampleEventOnThisPoint.PdfW, BrdfCreator.BrdfSampling)
                {
                    EyePdfA = eyePdfA,
                    LightPdfA = lightPath.Points[j].PdfA,
                };
            }

            return new FullPath(pathContribution, pathPdfA, points, this);
        }

        public double GetPathPdfAForAGivenPath(FullPath path, FullPathFrameData frameData)
        {
            double sum = 0;

            float radius = frameData.PhotonMaps.BeamDataLineQueryMap.SearchRadius;
            float kernel1D = 1.0f / (radius * 2);
            float acceptancePdf = 1.0f / kernel1D;

            //Alle Particle
            for (int i = 1; i < path.Points.Length - 1; i++)
            {
                if (path.Points[i].LocationType == MediaPointLocationType.MediaParticle)
                {
                    Vector3D eyeToMergingPoint = path.Points[i - 1].EyeLineToNext != null ? path.Points[i - 1].EyeLineToNext.Ray.Direction : Vector3D.Normalize(path.Points[i].Position - path.Points[i - 1].Position);
                    Vector3D lightToMergingPoint = path.Points[i + 1].LightLineToNext != null ? path.Points[i + 1].LightLineToNext.Ray.Direction : Vector3D.Normalize(path.Points[i].Position - path.Points[i + 1].Position);
                    float sinTheta = Vector3D.Cross(eyeToMergingPoint, lightToMergingPoint).Length();
                    sum += path.Points[i].EyePdfA * path.Points[i].LightPdfA * acceptancePdf * frameData.PhotonMaps.BeamDataLineQueryMap.LightPathCount * sinTheta;
                }
            }

            return sum;
        }
    }
}
