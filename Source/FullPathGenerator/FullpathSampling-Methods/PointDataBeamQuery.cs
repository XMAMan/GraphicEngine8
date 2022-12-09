using System;
using System.Collections.Generic;
using SubpathGenerator;
using Photonusmap;
using GraphicMinimal;
using RaytracingBrdf;
using ParticipatingMedia;
using ParticipatingMedia.DistanceSampling;
using RayTracerGlobal;
using GraphicGlobal;

namespace FullPathGenerator
{
    //Der EyeSubpath wird ohne Distanzsampling oder mit LongRay und Distanzesampling erzeugt; Der LightSubPath mit Distancesampling (Short Ray oder LongRay)
    //Folgende PathSpace deckt dieses Sampleverfahren ab: C P L, C {D} P {D/P} L    {} = 0..N mal
    //Der FullPath-Connection-Punkt ist also ein P, wo vorher ein C oder D steht. Bei C P P L ist also lediglich das erste P der ConnectionPunkt.
    //Bei C P D P L sind beide Ps mögliche ConnectionPunkte.

    //https://www.cs.dartmouth.edu/~wjarosz/publications/jarosz08beam.pdf
    //The Beam Radiance Estimate for Volumetric Photon Mapping - Wojciech Jarosz     Matthias Zwicker    Henrik Wann Jensen      2008
    public class PointDataBeamQuery : IFullPathSamplingMethod
    {
        private readonly bool noDistanceSampling;
        private readonly int maxPathLength;
        private readonly IPhaseFunctionSampler phaseFunction;

        public PointDataBeamQuery(PathSamplingType usedEyeSubPathType, int maxPathLength, IPhaseFunctionSampler phaseFunction)
        {
            this.noDistanceSampling = usedEyeSubPathType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling;
            this.maxPathLength = maxPathLength;
            this.phaseFunction = phaseFunction;
        }

        public SamplingMethod Name => SamplingMethod.PointDataBeamQuery;
        public int SampleCountForGivenPath(FullPath path)
        {
            int pCounter = 0; //ParticleCounter

            //Zähle alle Particle
            for (int i = 1; i < path.PathLength; i++)
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
                paths.AddRange(TryToCreatePaths(eyePath.Points[i], frameData.PhotonMaps.PointDataBeamQueryMap));
            }
            return paths;
        }

        private List<FullPath> TryToCreatePaths(PathPoint eyePoint, PointDataBeamQueryMap photonmap)
        {
            List<FullPath> paths = new List<FullPath>();

            if (eyePoint.LineToNextPoint != null && eyePoint.LineToNextPoint.HasScattering())
            {
                var photons = photonmap.QuerryMediaPhotons(eyePoint.LineToNextPoint, eyePoint.AssociatedPath.PathCreationTime);

                foreach (var photon in photons)
                {
                    if (photon.LightPoint.Index + eyePoint.Index + 2 <= this.maxPathLength && photon.DistanceToRayStart > MagicNumbers.MinAllowedPathPointDistance)
                    {
                        //Jeder FullPath bekommt sein eigenen EyePoint, dessen LineToNextPoint bis zum Photon reicht
                        var subLine = eyePoint.LineToNextPoint.CreateShortMediaSubLine(photon.DistanceToRayStart);
                        if (subLine.EndPoint.HasScattering())
                        {
                            var path = CreatePath(new PathPoint(eyePoint) { LineToNextPoint = subLine }, photon, photonmap);
                            paths.Add(path);
                        }
                        
                    }                        
                }
            }

            return paths;
        }

        private FullPath CreatePath(PathPoint eyePoint, PhotonIntersectionPoint photon, PointDataBeamQueryMap photonmap)
        {
            SubPath eyePath = eyePoint.AssociatedPath;
            PathPoint lightPoint = photon.LightPoint;
            SubPath lightPath = lightPoint.AssociatedPath;

            Vector3D pathweightOnPointT = Vector3D.Mult(eyePoint.PathWeight, eyePoint.BrdfSampleEventOnThisPoint.Brdf);
            pathweightOnPointT = Vector3D.Mult(pathweightOnPointT, eyePoint.LineToNextPoint.AttenuationWithoutPdf()); //LineToNextPoint wurde bis zum Photon gestutzt 

            var linePdf = this.noDistanceSampling ? new DistancePdf() { PdfL = 1, ReversePdfL = 1 } : eyePoint.LineToNextPoint.GetPdfLIfDistanceSamplingWouldBeUsed();
            PathPoint pointOnT = PathPoint.CreateMediaParticlePoint(eyePoint.LineToNextPoint.EndPoint, pathweightOnPointT); //Punkt auf der Eye-Medialine in der Nähe vom Photon
            pointOnT.PdfA = eyePoint.PdfA * PdfHelper.PdfWToPdfAOrV(eyePoint.BrdfSampleEventOnThisPoint.PdfW, eyePoint, pointOnT) * linePdf.PdfL;
            pointOnT.AssociatedPath = eyePoint.AssociatedPath;
            pointOnT.Predecessor = eyePoint;

            var pointTBsdf = this.phaseFunction.EvaluateBsdf(pointOnT.DirectionToThisPoint, lightPoint.MediaPoint, -lightPoint.DirectionToThisPoint);


            //float kernel2D = 1.0f / photon.SquareDistanceToRayline * VolumetricPhotonmap.SilvermanTwoDimensinalBiweightKernel((float)Math.Sqrt(photon.SquareDistanceToRayline) / photon.PhotonRadius);
            //float kernel2D = 1.0f / (photon.SquareDistanceToRayline * (float)Math.PI);
            float kernel2D = 1.0f / (photon.PhotonRadius * photon.PhotonRadius * (float)Math.PI);

            float acceptancePdf = 1.0f / kernel2D;

            Vector3D pathContribution = Vector3D.Mult(Vector3D.Mult(photon.LightPoint.PathWeight, pointTBsdf.Brdf) * kernel2D, pointOnT.PathWeight) / photonmap.LightPathCount;
            double pathPdfA = pointOnT.PdfA * lightPoint.PdfA * acceptancePdf * photonmap.LightPathCount;
            
            var points = new FullPathPoint[eyePoint.Index + lightPoint.Index + 2];
            double lightPdfA = lightPoint.PdfA;

            //pointOnT ConnectionPunkt (Hat keine LineToNexts)
            points[eyePoint.Index + 1] = new FullPathPoint(pointOnT, null, null, pointTBsdf.PdfW, pointTBsdf.PdfWReverse, BrdfCreator.MergingPoint)
            {
                EyePdfA = pointOnT.PdfA,
                LightPdfA = lightPdfA,
            };
            lightPdfA = lightPdfA * PdfHelper.PdfWToPdfAOrV(pointTBsdf.PdfWReverse, pointOnT, eyePoint) * linePdf.ReversePdfL;

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

            //LightPoint-Predecessor
            //eyePdfA *= PdfHelper.PdfWToPdfAOrV(pointTBsdf.PdfW, pointOnT.Position, lightPoint.Predecessor) * (this.noDistanceSampling ? 1 : lightPoint.Predecessor.PdfLFromNextPointToThis); //Wenn ich das so mache, fällt der MultisamplerTest mit Pathtracing und DirectLighting um
            eyePdfA = eyePdfA * PdfHelper.PdfWToPdfAOrV(pointTBsdf.PdfW, pointOnT, lightPoint.Predecessor) * lightPoint.Predecessor.PdfLFromNextPointToThis;
            points[eyePoint.Index + 2] = new FullPathPoint(lightPoint.Predecessor, null, lightPoint.Predecessor.LineToNextPoint, lightPoint.Predecessor.BrdfSampleEventOnThisPoint.PdfWReverse, lightPoint.Predecessor.BrdfSampleEventOnThisPoint.PdfW, BrdfCreator.BrdfSampling)
            {
                EyePdfA = eyePdfA,
                LightPdfA = lightPoint.Predecessor.PdfA,
                PdfWContainsNumericErrors = true
            };

            //Light-Subpath            
            for (int i= eyePoint.Index + 3, j= lightPoint.Index -2; i< points.Length;i++,j--)
            {
                //eyePdfA *= lightPath.Points[j].PdfAReverse * (this.noDistanceSampling ? 1 : lightPath.Points[j].PdfLFromNextPointToThis);
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

            float radius = frameData.PhotonMaps.PointDataBeamQueryMap.SearchRadius;
            float kernel2D = 1.0f / (radius * radius * (float)Math.PI);
            float acceptancePdf = 1.0f / kernel2D;

            //Alle Particle
            for (int i = 1; i < path.Points.Length - 1; i++)
            {
                if (path.Points[i].LocationType == MediaPointLocationType.MediaParticle)
                {
                    sum += path.Points[i].EyePdfA * path.Points[i].LightPdfA * acceptancePdf * frameData.PhotonMaps.PointDataBeamQueryMap.LightPathCount;
                }
            }

            return sum;
        }
    }
}
