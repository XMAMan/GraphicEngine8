using System;
using System.Collections.Generic;
using GraphicMinimal;
using SubpathGenerator;
using GraphicGlobal;

namespace FullPathGenerator
{
    //Erzeugt pro Sample-Schritt eine Menge von Fullpaths und gibt deren Radiance-Summe zurück
    public class FullPathSampler
    {
        public IFullPathSamplingMethod[] SamplingMethods { get; private set; }

        private readonly bool withoutMis;

        public FullPathSampler(FullPathKonstruktorData data, FullPathSettings settings)
        {
            settings.MaximumDirectLightingEyeIndex = Math.Min(data.MaxPathLength - 2, settings.MaximumDirectLightingEyeIndex);
            this.withoutMis = settings.WithoutMis;

            int maxMediaPathLength = settings.DoSingleScattering ? 3 : data.MaxPathLength;

            List<IFullPathSamplingMethod> sampler = new List<IFullPathSamplingMethod>();
            if (settings.UsePathTracing) sampler.Add(new PathTracing(data.LightSourceSampler, data.EyePathSamplingType));
            if (settings.UseSpecularPathTracing) sampler.Add(new SpecularPathtracing(data.LightSourceSampler, data.EyePathSamplingType));
            if (settings.UseDirectLighting) sampler.Add(new DirectLighting(data.LightSourceSampler, settings.MaximumDirectLightingEyeIndex, data.PointToPointConnector, data.EyePathSamplingType));
            if (settings.UseMultipleDirectLighting) sampler.Add(new MultipleDirectLighting(data.LightSourceSampler, settings.MaximumDirectLightingEyeIndex, data.PointToPointConnector, data.EyePathSamplingType));
            if (settings.UseDirectLightingOnEdge) sampler.Add(new DirectLightingOnEdge(data.LightSourceSampler, data.PointToPointConnector, data.EyePathSamplingType, maxMediaPathLength, settings.UseSegmentSamplingForDirectLightingOnEdge));
            if (settings.UseLightTracingOnEdge) sampler.Add(new LightTracingOnEdge(data.RayCamera, data.PointToPointConnector, data.LightPathSamplingType));
            if (settings.UseVertexConnection) sampler.Add(new VertexConnection(data.MaxPathLength, data.PointToPointConnector, data.EyePathSamplingType));
            if (settings.UseVertexMerging) sampler.Add(new VertexMerging(data.MaxPathLength, data.EyePathSamplingType));
            if (settings.UseLightTracing) sampler.Add(new LightTracing(data.RayCamera, data.PointToPointConnector, data.LightPathSamplingType));
            if (settings.UsePointDataPointQuery) sampler.Add(new PointDataPointQuery(data.MaxPathLength));
            if (settings.UsePointDataBeamQuery) sampler.Add(new PointDataBeamQuery(data.EyePathSamplingType, data.MaxPathLength));
            if (settings.UseBeamDataLineQuery) sampler.Add(new BeamDataLineQuery(data.EyePathSamplingType, maxMediaPathLength));

            this.SamplingMethods = sampler.ToArray();
        }

        public FullPathSampleResult SampleFullPaths(SubPath eyePath, SubPath lightPath, FullPathFrameData frameData, IRandom rand)
        {
            FullPathSampleResult result = new FullPathSampleResult();
            bool eyePathIsEmpty = eyePath != null && eyePath.Points.Length == 1; //Primärstrahl fliegt ins Leere

            result.RadianceFromRequestetPixel = new Vector3D(0, 0, 0);

            foreach (var sampleMethod in this.SamplingMethods)
            {
                if ((sampleMethod is LightTracing) == false && eyePathIsEmpty) continue; //Primärstrahl fliegt ins Leere. Nur Lightracing kann hier noch was machen

                var paths = sampleMethod.SampleFullPaths(eyePath, lightPath, frameData, rand);
                foreach (var fullPath in paths)
                {
                    float misWeight = this.withoutMis ? GetMisWeightWithEqalWeighting(fullPath) : GetMisWeightWithBalanceWeighting(fullPath, frameData);

                    fullPath.MisWeight = misWeight;
                    fullPath.Radiance = fullPath.PathContribution * fullPath.MisWeight;

                    if (fullPath.PixelPosition == null)
                    {
                        result.RadianceFromRequestetPixel += fullPath.Radiance; 

                        result.MainPaths.Add(fullPath); 
                    }
                    else
                    {
                        //LightTracing / LightTracing with Media
                        result.LighttracingPaths.Add(fullPath); 
                    }
                }

                //Wenn ich bei progressiven Photonmapping/VertexMerging den Suchradius mit der Anzahl der aufgesammelten Photonen reduzieren wöllte. (Hat sich nicht bewährt)
                //if (sampleMethod is VertexMerging || sampleMethod is PointDataPointQuery) result.CollectedVertexMergingPhotonCount += paths.Count;
            }

            result.MainPixelHitsBackground = eyePathIsEmpty;

            return result;
        }

        private float GetMisWeightWithBalanceWeighting(FullPath path, FullPathFrameData frameData)
        {
            double sum = 0;

            foreach (var method in this.SamplingMethods)
            {
                sum += method.GetPathPdfAForAGivenPath(path, frameData);
            }

            return GetMisWeightBalanceHeuristic(path.PathPdfA, sum);
        }

        private float GetMisWeightWithEqalWeighting(FullPath path)
        {
            int sum = 0;

            foreach (var method in this.SamplingMethods)
            {
                sum += method.SampleCountForGivenPath(path);                
            }

            return 1.0f / sum;
        }

        private float GetMisWeightBalanceHeuristic(double pdf, double pdfSum)
        {
            if (double.IsInfinity(pdfSum)) return 0; //Wenn ganz viele große Zahlen addiert werden, und die Summe dann irgendwann double.MaxValue überrschreitet, dann ist das Ergebnis double.Infinity
            double d = pdf / pdfSum;
            float f = (float)d;
            if (float.IsInfinity(f) || float.IsNaN(f) || (f <= 0 && d <= 0) || f > 1.00001f) throw new Exception("Mis-Weight out of Range '" + f + "'  " + d);         

            return f;
        }
    }
}
