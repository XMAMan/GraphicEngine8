using System;
using System.Collections.Generic;
using System.Linq;
using SubpathGenerator;
using Photonusmap;
using GraphicMinimal;
using ParticipatingMedia;
using GraphicGlobal;
using RaytracingBrdf.SampleAndRequest;

namespace FullPathGenerator
{
    //Verbindet zwei Punkte mit KernelFunktion auf Surface
    public class VertexMerging : IFullPathSamplingMethod
    {
        private readonly int maxPathLength;
        private readonly bool checkThatEachPointIsASurfacePoint;

        public VertexMerging(int maxPathLength, PathSamplingType usedSubPathSamplingType)
        {
            this.checkThatEachPointIsASurfacePoint = usedSubPathSamplingType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling;
            this.maxPathLength = maxPathLength;
        }
        public SamplingMethod Name => SamplingMethod.VertexMerging;
        public int SampleCountForGivenPath(FullPath path)
        {
            int pCounter = 0; // Diffuse-Punkte-Counter

            //Zähle alle Diffuse-Surfaces
            for (int i = 1; i < path.PathLength - 1; i++)
            {
                if (path.Points[i].LocationType == MediaPointLocationType.Surface && path.Points[i].IsDiffusePoint)
                    pCounter++;
            }

            return pCounter;
        }
        public virtual List<FullPath> SampleFullPaths(SubPath eyePath, SubPath lightPath, FullPathFrameData frameData, IRandom rand)
        {
            List<FullPath> paths = new List<FullPath>();
            if (eyePath == null) return paths;
            for (int i = 1; i < eyePath.Points.Length; i++)
            {
                ISurfacePhotonmap photonmap;
                if (i == 1) photonmap = frameData.PhotonMaps.CausticSurfacemap; else photonmap = frameData.PhotonMaps.GlobalSurfacePhotonmap;
                if (photonmap == null) photonmap = frameData.PhotonMaps.GlobalSurfacePhotonmap;

                float radiusSqrt = photonmap.SearchRadius * photonmap.SearchRadius;
                float kernelFunction = 1.0f / (radiusSqrt * (float)Math.PI);
                if (float.IsInfinity(kernelFunction)) return paths;

                paths.AddRange(TryToCreatePaths(eyePath.Points[i], photonmap));
            }
            return paths;
        }

        protected List<FullPath> TryToCreatePaths(PathPoint eyePoint, ISurfacePhotonmap photonmap)
        {
            List<FullPath> paths = new List<FullPath>();

            if (eyePoint.IsLocatedOnLightSource == false && eyePoint.IsDiffusePoint && eyePoint.LocationType == MediaPointLocationType.Surface && eyePoint.Index >= photonmap.MinEyeIndex && eyePoint.Index <= photonmap.MaxEyeIndex)
            {
                var photons = photonmap.QuerrySurfacePhotons(eyePoint.Position, photonmap.SearchRadius).Where(p => p.LocationType == MediaPointLocationType.Surface && p.Normal * eyePoint.Normal > 0 /*&& eyePoint.SurfacePoint.FlatNormal * p.SurfacePoint.FlatNormal > 0.9f && eyePoint.SurfacePoint.IntersectedRayHeigh == p.SurfacePoint.IntersectedRayHeigh*/);
                int resultCount = photons.Count();

                if (resultCount > photonmap.MinPhotonCountForRadianceEstimation)
                {
                    foreach (var lightPoint in photons)
                    {
                        if (lightPoint.Index + eyePoint.Index < this.maxPathLength)
                        {
                            var eyeBrdf = eyePoint.BrdfPoint.Evaluate(-lightPoint.DirectionToThisPoint);//VertexMerging

                            if (eyeBrdf != null)
                            {
                                var path = CreatePath(eyePoint, lightPoint, eyeBrdf, photonmap);
                                paths.Add(path);
                            }
                        }
                    }
                }
            }

            return paths;
        }

        protected virtual FullPath CreatePath(PathPoint eyePoint, PathPoint lightPoint, BrdfEvaluateResult eyeBrdf, ISurfacePhotonmap photonmap)
        {
            SubPath eyePath = eyePoint.AssociatedPath;
            SubPath lightPath = lightPoint.AssociatedPath;

            float radiusSqrt = photonmap.SearchRadius * photonmap.SearchRadius;
            //float kernelFunction = photonmap.KernelFunction((eyePoint.Position - lightPoint.Position).QuadratBetrag(), radiusSqrt);
            float kernelFunction = 1.0f / (radiusSqrt * (float)Math.PI);

            float acceptancePdf = radiusSqrt * (float)Math.PI;

            Vector3D pathContribution = Vector3D.Mult(Vector3D.Mult(eyePoint.PathWeight, lightPoint.PathWeight), eyeBrdf.Brdf) * kernelFunction / photonmap.LightPathCount;
            double pathPdfA = eyePoint.PdfA * lightPoint.PdfA * acceptancePdf * photonmap.LightPathCount;

            var points = new FullPathPoint[eyePoint.Index + lightPoint.Index + 1];
            for (int i = 0; i < eyePoint.Index; i++) points[i] = new FullPathPoint(eyePath.Points[i], eyePath.Points[i].LineToNextPoint, null, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfW, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfWReverse, BrdfCreator.BrdfSampling) { EyePdfA = eyePath.Points[i].PdfA };

            points[eyePoint.Index - 1].LightPdfA = lightPoint.PdfA * PdfHelper.PdfWToPdfAOrV(eyeBrdf.PdfWReverse, eyePoint, eyePoint.Predecessor) * eyePoint.Predecessor.PdfLFromNextPointToThis;
            points[eyePoint.Index] = new FullPathPoint(eyePath.Points[eyePoint.Index], null, null, eyeBrdf.PdfW, eyeBrdf.PdfWReverse, BrdfCreator.MergingPoint) { EyePdfA = eyePoint.PdfA, LightPdfA = lightPoint.PdfA };
            points[eyePoint.Index + 1] = new FullPathPoint(lightPoint.Predecessor, null, lightPoint.Predecessor.LineToNextPoint, lightPoint.Predecessor.BrdfSampleEventOnThisPoint.PdfWReverse, lightPoint.Predecessor.BrdfSampleEventOnThisPoint.PdfW, BrdfCreator.BrdfSampling) { LightPdfA = lightPoint.Predecessor.PdfA, PdfWContainsNumericErrors = true };
            points[eyePoint.Index + 1].EyePdfA = eyePoint.PdfA * PdfHelper.PdfWToPdfAOrV(eyeBrdf.PdfW, eyePoint, lightPoint.Predecessor) * lightPoint.Predecessor.PdfLFromNextPointToThis;

            double lightPdfA = points[eyePoint.Index - 1].LightPdfA;
            for (int j=eyePoint.Index - 2;j>=0;j--)
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
            points[eyePoint.Index + 1].LightLineToNext = null;

            return new FullPath(pathContribution, pathPdfA, points, this);
        }

        public double GetPathPdfAForAGivenPath(FullPath path, FullPathFrameData frameData)
        {
            if (this.checkThatEachPointIsASurfacePoint && path.IsSurfacePathOnly() == false) return 0;

            double sum = 0;

            for (int i = 1; i < path.Points.Length - 1; i++)
            {
                ISurfacePhotonmap photonmap;
                if (i == 1) photonmap = frameData.PhotonMaps.CausticSurfacemap; else photonmap = frameData.PhotonMaps.GlobalSurfacePhotonmap;
                if (photonmap == null) photonmap = frameData.PhotonMaps.GlobalSurfacePhotonmap;
                float radiusSqrt = photonmap.SearchRadius * photonmap.SearchRadius; //Achtung: Komm ja nicht auf die Idee das radiusSqrt wegkürzen zu wollen. Es kommt eine andere AcceptancePdf raus, wenn man das Radius*Radius direkt unten reinschreibt als wenn man es so macht
                float acceptancePdf = radiusSqrt * (float)Math.PI;

                //Wird auf false gesetzt, wenn das hier eine CausticMap ist und aber kein Specular-Path
                bool causticSpecularCondition = true;
                if (photonmap.ContainsOnlySpecularLightPahts)
                {
                    causticSpecularCondition = i <= GetSpecularIndex(path);
                }

                if (path.Points[i].IsDiffusePoint && path.Points[i].LocationType == MediaPointLocationType.Surface && i >= photonmap.MinEyeIndex && i <= photonmap.MaxEyeIndex && causticSpecularCondition)
                {
                    sum += path.Points[i].EyePdfA * path.Points[i].LightPdfA * acceptancePdf * photonmap.LightPathCount;
                }
            }
            return sum;
        }

        //Gibt den Index zurück, ab dem der Pfad Specular ist
        private int GetSpecularIndex(FullPath path)
        {
            for (int i = path.Points.Length - 1; i > 0; i--)
            {
                if (path.Points[i].Point.BrdfPoint.DiffusePortion < 1) return i - 1;
            }

            return -1;
        }
    }
}
