using FullPathGenerator;
using FullPathGenerator.FullpathSampling_Methods;
using GraphicGlobal;
using RaytracingBrdf;
using RaytracingBrdf.SampleAndRequest;
using RaytracingColorEstimator;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaytracingMethods.MMLT
{
    class SingleFullPathSampler
    {
        private SinglePathLengthData[] pathLengthData; //Der Index steht sowohl für die die Länge, die der Subpfadsampler erzeugt als auch für die Fullpfadlänge

        public int MaxFullPathLength => this.pathLengthData.Length - 1;

        public SingleFullPathSampler(RaytracingFrame3DData data, bool withMedia)
        {
            PathSamplingType pathSamplingType = withMedia ? PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling : PathSamplingType.NoMedia;

            PixelRadianceData radianceData = PixelRadianceCreationHelper.CreatePixelRadianceData(data, new SubPathSettings()
            {
                EyePathType = pathSamplingType,
                LightPathType = pathSamplingType
            }, null);

            var pointToPointConnector = new PointToPointConnector(new RayVisibleTester(radianceData.IntersectionFinder, radianceData.MediaIntersectionFinder), radianceData.RayCamera, pathSamplingType);
            var fullPathSampler = new ISingleFullPathSampler[]
            {
                new PathTracing(radianceData.LightSourceSampler, pathSamplingType),
                new DirectLighting(radianceData.LightSourceSampler, int.MaxValue, pointToPointConnector, pathSamplingType),
                new VertexConnection(data.GlobalObjektPropertys.RecursionDepth, pointToPointConnector, pathSamplingType),
                new LightTracing(radianceData.RayCamera, pointToPointConnector, pathSamplingType)
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
                    CreateAbsorbationEvent = false
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

        public FullPathStrategy SampleFullpathStrategy(int fullPathLength, IRandom rand)
        {
            var pathLength = this.pathLengthData[fullPathLength];
            if (pathLength.Strategies.Length == 0) return null;
            int strategyIndex = rand.Next(pathLength.Strategies.Length);
            var strategy = pathLength.Strategies[strategyIndex];
            return strategy;
        }

        public SubPath SampleEyeSubPath(int pathLength, int pixX, int pixY, IRandom rand)
        {
            return this.pathLengthData[pathLength].EyePathSampler.SamplePathFromCamera(pixX, pixY, rand);
        }

        public SubPath SampleLightSubPath(int pathLength, IRandom rand)
        {
            return this.pathLengthData[pathLength].LightPathSampler.SamplePathFromLighsource(rand);
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
