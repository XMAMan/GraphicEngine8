using FullPathGenerator;
using FullPathGenerator.FullpathSampling_Methods;
using GraphicGlobal;
using GraphicMinimal;
using RaytracingBrdf;
using RaytracingBrdf.SampleAndRequest;
using RaytracingColorEstimator;
using SubpathGenerator;
using System;
using System.Drawing;
using System.Linq;

namespace RaytracingMethods.MMLT
{
    //Erstellt ein Subpfad mit einer genau festgelegten Länge
    class SinglePathSampler
    {
        private SinglePathLengthData[] pathLengthData; //Der Index steht sowohl für die die Länge, die der Subpfadsampler erzeugt als auch für die Fullpfadlänge
        private ISingleFullPathSampler[] fullPathSampler;

        public readonly ImagePixelRange PixelRange; //Innerhalb von diesen PixelRange werden Eye-Subpaths erstellt
        public int MaxFullPathLength => this.pathLengthData.Length - 1; //Maximal diese Eye/Light-Subpfadlänge kann erstellt werden

        public SinglePathSampler(SinglePathSampler copy)
        {
            this.pathLengthData = copy.pathLengthData;
            this.PixelRange = copy.PixelRange;
        }

        public SinglePathSampler(RaytracingFrame3DData data, bool withMedia)
        {
            this.PixelRange = data.PixelRange;

            PathSamplingType pathSamplingType = withMedia ? PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling : PathSamplingType.NoMedia;

            PixelRadianceData radianceData = PixelRadianceCreationHelper.CreatePixelRadianceData(data, new SubPathSettings()
            {
                EyePathType = pathSamplingType,
                LightPathType = pathSamplingType,
            }, null);

            var pointToPointConnector = new PointToPointConnector(new RayVisibleTester(radianceData.IntersectionFinder, radianceData.MediaIntersectionFinder), radianceData.RayCamera, pathSamplingType);
            this.fullPathSampler = new ISingleFullPathSampler[]
            {
                new PathTracing(radianceData.LightSourceSampler, pathSamplingType),
                new DirectLighting(radianceData.LightSourceSampler, int.MaxValue, pointToPointConnector, pathSamplingType),
                new VertexConnection(data.GlobalObjektPropertys.RecursionDepth, pointToPointConnector, pathSamplingType),
                new LightTracing(radianceData.RayCamera, pointToPointConnector, pathSamplingType, true)
            };

            this.pathLengthData = new SinglePathLengthData[data.GlobalObjektPropertys.RecursionDepth + 1];
            for (int depth = 2; depth <= data.GlobalObjektPropertys.RecursionDepth; depth++) //depth steht sowohl für die Länge des Subpfads als auch des Fullpfads
            {
                //Erzeuge Subpfadsampler, welcher mit einer Maxpfadlänge von 'depth' arbeitet
                var subPathSampler = new SubpathSampler(new SubpathSamplerConstruktorData()
                {
                    RayCamera = radianceData.RayCamera,
                    LightSourceSampler = radianceData.LightSourceSampler,
                    IntersectionFinder = radianceData.IntersectionFinder,
                    MediaIntersectionFinder = radianceData.MediaIntersectionFinder,
                    PathSamplingType = pathSamplingType,
                    MaxPathLength = depth,
                    BrdfSampler = new BrdfSampler(),
                    PhaseFunction = new PhaseFunction(),
                    CreateAbsorbationEvent = false,
                });

                //Gehe durch alle Fullpathsampler und frage jeden wie viele Samplingstrategien er hat um ein
                //Fullpfad der Länge 'depth' zu erzeugen. Für jede dieser Strategien gibt es ein Eintrag in
                //der Strategies-Property
                this.pathLengthData[depth] = new SinglePathLengthData()
                {
                    EyePathSampler = subPathSampler,
                    LightPathSampler = subPathSampler,
                    Strategies = fullPathSampler.SelectMany(x => x.GetAvailableStrategiesForFullPathLength(depth).Select(strategy => new FullPathStrategy(strategy, x)).ToArray()).ToArray()
                };
            }
        }

        public float GetMisWeight(FullPath path)
        {
            double sum = 0;

            foreach (var method in this.fullPathSampler)
            {
                sum += method.GetPathPdfAForAGivenPath(path, null);
            }

            return GetMisWeightBalanceHeuristic(path.PathPdfA, sum);
        }

        private float GetMisWeightBalanceHeuristic(double pdf, double pdfSum)
        {
            if (double.IsInfinity(pdfSum)) return 0; //Wenn ganz viele große Zahlen addiert werden, und die Summe dann irgendwann double.MaxValue überrschreitet, dann ist das Ergebnis double.Infinity
            double d = pdf / pdfSum;
            float f = (float)d;
            if (float.IsInfinity(f) || float.IsNaN(f) || (f <= 0 && d <= 0) || f > 1.00001f) throw new Exception("Mis-Weight out of Range '" + f + "'  " + d);

            return f;
        }

        public FullPathStrategy SampleFullpathStrategy(int fullPathLength, IRandom rand)
        {
            var pathLength = this.pathLengthData[fullPathLength];
            if (pathLength.Strategies.Length == 0) return null;
            int strategyIndex = rand.Next(pathLength.Strategies.Length);
            var strategy = pathLength.Strategies[strategyIndex];
            return strategy;
        }

        public int GetStrategyCountForFullPathLength(int fullPathLength)
        {
            return this.pathLengthData[fullPathLength].Strategies.Length;
        }

        //Erzeugt ein EyeSubpfad für ein zufällig ausgewählten Pixel mit genau der gewünschten Länge
        public SubPath SampleEyeSubPath(int pathLength, IRandom rand, out Point pix)
        {
            pix = new Point(-1, -1);
            if (pathLength > 0)
            {
                //Wähle zufälligen Pixel aus
                var eyePath = this.pathLengthData[pathLength].EyePathSampler.SamplePathFromCamera(this.PixelRange, rand, out pix);
                if (eyePath.Points.Length != pathLength) return null;
                return eyePath;
            }
            else
            {
                return new SubPath(new PathPoint[0], 0);
            }            
        }

        //Erzeugt ein LightSubpfad mit genau der gewünschten Länge
        public SubPath SampleLightSubPath(int pathLength, IRandom rand)
        {
            if (pathLength > 0)
            {
                var lightPath = this.pathLengthData[pathLength].LightPathSampler.SamplePathFromLighsource(rand);
                if (lightPath.Points.Length != pathLength) return null;
                return lightPath;
            }
            else
            {
                return new SubPath(new PathPoint[0], 0);
            }
        }
    }

    class SinglePathLengthData
    {
        public SubpathSampler EyePathSampler;
        public SubpathSampler LightPathSampler;
        public FullPathStrategy[] Strategies;
    }

    class FullPathStrategy : FullPathSamplingStrategy
    {
        public ISingleFullPathSampler Sampler { get; private set; }

        public FullPathStrategy(FullPathSamplingStrategy data, ISingleFullPathSampler sampler)
            : base(data)
        {
            this.Sampler = sampler;
        }
    }
}
