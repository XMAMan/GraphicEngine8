using FullPathGenerator;
using GraphicGlobal;
using SubpathGenerator;
using System;
using System.Collections.Generic;

namespace RaytracingMethods.McVcm
{
    //Erzeugt mit Hilfe von FullPathsamplern eine Liste von Pixel-Radiance-Schätzwerten 
    //Entspricht dem FullPathSampler aber mit folgenden Änderungen:
    //PT/DL = Wird normal über den eyePathPT-Subpfad erzeugt
    //VC = Wird für ein zufälligen Pixel erzeugt und erzeugt somit auch LT-Pfad
    //VM = Anstatt ein EyeSubpfad mit vielen Light-Subpfads zu verbinden wird hier ein LightSubpfad mit allen Pixel-EyeSubpfads gemerged
    //LT = Wird normal über den lightPath erzeugt
    //Bei der MIS-Formel kommt für die VC/VM/LT-Pfade noch ein Jacobi-Faktor hinzu, da Pfade im Primaryspace gesampelt werden und im PathSpace integriert werden sollen
    //Im Paper sieht man bei Bild 9 und Formel 10 am Besten, was diese Klasse hier tut
    class SplatListSampler
    {
        public IFullPathSamplingMethod[] SamplingMethods { get; private set; }

        public SplatListSampler(FullPathKonstruktorData data, FullPathSettings settings)
        {
            List<IFullPathSamplingMethod> sampler = new List<IFullPathSamplingMethod>();

            if (settings.UsePathTracing) sampler.Add(new PathTracing(data.LightSourceSampler, data.EyePathSamplingType));
            if (settings.UseDirectLighting) sampler.Add(new DirectLighting(data.LightSourceSampler, settings.MaximumDirectLightingEyeIndex, data.PointToPointConnector, data.EyePathSamplingType));
            if (settings.UseMultipleDirectLighting) sampler.Add(new MultipleDirectLighting(data.LightSourceSampler, settings.MaximumDirectLightingEyeIndex, data.PointToPointConnector, data.EyePathSamplingType));
            if (settings.UseVertexConnection) sampler.Add(new VertexConnection(data.MaxPathLength, data.PointToPointConnector, data.EyePathSamplingType));
            if (settings.UseVertexMerging) sampler.Add(new EyeMapVertexMerging(data.MaxPathLength, data.EyePathSamplingType));
            if (settings.UseLightTracing) sampler.Add(new LightTracing(data.RayCamera, data.PointToPointConnector, data.LightPathSamplingType, false));

            this.SamplingMethods = sampler.ToArray();
        }

        public SplatList SampleSplatList(SubPath eyePathPT, EyeSubPath eyePathVC, SubPath lightPath, FullPathFrameData frameData, IRandom rand, float visibleChainNormalization)
        {
            SplatList splats = new SplatList();

            bool eyePathIsEmpty = eyePathPT != null && eyePathPT.Points.Length == 1; //Primärstrahl fliegt ins Leere

            foreach (var sampleMethod in this.SamplingMethods)
            {
                if (eyePathIsEmpty && (sampleMethod is LightTracing) == false && (sampleMethod is EyeMapVertexMerging) == false) continue; //Primärstrahl fliegt ins Leere. Nur Lightracing und EyeMapVertexMerging kann hier noch was machen

                var eyePath = sampleMethod.Name == SamplingMethod.VertexConnection ? eyePathVC : eyePathPT; //VC nimmt ein zufälligen Pfad aus der EyeMap
                var paths = sampleMethod.SampleFullPaths(eyePath, lightPath, frameData, rand);
                foreach (var fullPath in paths)
                {
                    float misWeight = GetMisWeightWithBalanceWeighting(fullPath, frameData, visibleChainNormalization);

                    fullPath.MisWeight = misWeight;
                    fullPath.Radiance = fullPath.PathContribution * fullPath.MisWeight;

                    if (fullPath.SamplingMethod == SamplingMethod.VertexConnection) 
                        fullPath.PixelPosition = eyePathVC.PixelPosition; //VC erzeugt nun ein Pfad für ein zufälligen Pixel

                    if (fullPath.PixelPosition == null)
                    {
                        splats.AddPathtracing(fullPath);
                    }else
                    {
                        splats.AddLighttracing(fullPath);
                    }                    
                }
            }

            return splats;
        }

        //Siehe Paper Formel (10)
        private float VisibleChainJacobian(SamplingMethod method, float visibleChainNormalization)
        {
            if (method == SamplingMethod.PathTracing || method == SamplingMethod.DirectLighting)
                return 1;

            return 1f / visibleChainNormalization;
        }

        private float GetMisWeightWithBalanceWeighting(FullPath path, FullPathFrameData frameData, float visibleChainNormalization)
        {
            double sum = 0;

            foreach (var method in this.SamplingMethods)
            {
                sum += method.GetPathPdfAForAGivenPath(path, frameData) * VisibleChainJacobian(method.Name, visibleChainNormalization);
            }

            return GetMisWeightBalanceHeuristic(path.PathPdfA * VisibleChainJacobian(path.SamplingMethod, visibleChainNormalization), sum);
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
