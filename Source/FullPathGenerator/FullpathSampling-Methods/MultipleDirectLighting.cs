using System;
using System.Collections.Generic;
using System.Linq;
using RaytracingLightSource;
using SubpathGenerator;
using GraphicGlobal;

namespace FullPathGenerator
{
    //Multiple Direct Lighting -> Der Grund, warum DirectLighting und MultipleDirectLighting die gleichen Bilder erzeugen, obwohl doch DirectLighting nur
    //                            ein Sample erzeugt und Multiple mehrere ist, weil DirectLighting mit der LightPickProb noch gewichtet wird, was dazu führt
    //                            das dessen PathContributation mit der Lichtquellenanzahl multipliziert wird.
    //Erklärung 1: Rekursive Denkweise. Ich stehe bei ein EyePoint und integriere über alle Lichtquellen:
    //Prinzipelle Erklärung zum DirectLighting: Man teilt die PathContribution durch die: LightPickProb, PdfA und PdfW. Das bedeutet laut Monte Carlo,
    //dass implizit eine Summe über alle Lichtquellen/Summe über alle dA-LightPoints auf jeder Lichtquelle/Summe über alle Richtungen; gebildet wird.
    //Ob ich die Summe nun wirklich mithilfe einer For-Schleife mache oder nur mit den Monte-Carlo-Trick, das bleibt das gleiche (Abgsehen vom Bildrauschen)
    //Erklärung 2: Path Integral-Denkweise:
    //Warum ist mein MultilDirectLighting der ni-Term 1? -> Da der Sample-Raum in disjunkte Mengen unterteilt wird(Jede Lichtquelle ist ein Teilbereich aus dem Sampelraum),
    //und man die Summe über Disjunkte Mengen bildet, muss ni 1 sein. ni ist immer nur dann größer 1, wenn ich im selben Sampleraum mehr als ein Sample nehme. Bsp: Zwei Samples 
    //pro Lichtquelle. Oder zwei Samples über alle Lichtquellen.
    public class MultipleDirectLighting : DirectLighting, IFullPathSamplingMethod
    {
        public MultipleDirectLighting(LightSourceSampler lightSourceSampler, int maximumDirectLightingEyeIndex, PointToPointConnector pointToPointConnector, PathSamplingType usedEyeSubPathType)
            : base(lightSourceSampler, maximumDirectLightingEyeIndex, pointToPointConnector, usedEyeSubPathType)
        {
        }

        public override List<FullPath> SampleFullPaths(SubPath eyePath, SubPath lightPath, FullPathFrameData frameData, IRandom rand)
        {
            if (eyePath == null) return new List<FullPath>();
            List<FullPath> paths = new List<FullPath>();
            for (int i=1;i<eyePath.Points.Length;i++)
            {
                paths.AddRange(TryToCreatePaths(eyePath.Points[i], rand));
            }
            return paths;
        }

        private List<FullPath> TryToCreatePaths(PathPoint eyePoint, IRandom rand)
        {
            List<FullPath> paths = new List<FullPath>();

            if (eyePoint.IsLocatedOnLightSource == false && eyePoint.IsDiffusePoint && eyePoint.Index <= this.maximumDirectLightingEyeIndex)
            {
                var toLightDirections = this.lightSourceSampler.GetRandomPointOnLightList(eyePoint.Position, rand); //Muliple-Ligthsource-Sampling
                foreach (var toLightDirection in toLightDirections)
                {
                    var connectData = this.pointToPointConnector.TryToConnectToLightSource(eyePoint, toLightDirection);
                    if (connectData != null)
                    {
                        double directLightingPdfA = this.lightSourceSampler.GetMultipleDirectLightingPdfA(eyePoint.Position, connectData.LightPoint, eyePoint.AssociatedPath.PathCreationTime);
                        if (directLightingPdfA != 0)
                        {
                            var path = CreatePath(eyePoint, connectData, directLightingPdfA);
                            paths.Add(path);
                        }
                    }
                }
            }

            return paths;
        }

        public override double GetPathPdfAForAGivenPath(FullPath path, FullPathFrameData frameData)
        {
            if (this.checkThatEachPointIsASurfacePoint && path.IsSurfacePathOnly() == false) return 0;
            int directLightingEyePathIndex = path.Points.Length - 2;

            if (directLightingEyePathIndex <= this.maximumDirectLightingEyeIndex && path.Points[directLightingEyePathIndex].IsDiffusePoint)
            {
                double multipleDirectLightingPdfA = this.lightSourceSampler.GetMultipleDirectLightingPdfA(path.Points[path.Points.Length - 2].Position, path.Points.Last().Point.SurfacePoint, path.Points.First().Point.AssociatedPath.PathCreationTime);
                return path.Points[directLightingEyePathIndex].EyePdfA * multipleDirectLightingPdfA;
            }
            return 0;
        }
    }
}
