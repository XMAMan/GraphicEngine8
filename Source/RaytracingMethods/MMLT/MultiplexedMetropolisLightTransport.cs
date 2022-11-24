using RaytracingColorEstimator;
using FullPathGenerator;
using GraphicGlobal;
using SubpathGenerator;
using FullPathGenerator.FullpathSampling_Methods;
using RaytracingBrdf.SampleAndRequest;
using RaytracingBrdf;
using System.Linq;
using GraphicMinimal;
using System.Drawing;

namespace RaytracingMethods.MMLT
{
    //https://cs.uwaterloo.ca/~thachisu/mmlt.pdf
    //https://github.com/mmp/pbrt-v3/blob/master/src/integrators/mlt.cpp
    public class MultiplexedMetropolisLightTransport : IPixelEstimator
    {
        public bool CreatesLigthPaths { get; } = true;

        private SinglePathLengthData[] pathLengthData; //Der Index steht sowohl für die die Länge, die der Subpfadsampler erzeugt als auch für die Fullpfadlänge
        private ImagePixelRange pixelRange;
        private bool withMedia;

        public MultiplexedMetropolisLightTransport(bool withMedia)
        {
            this.withMedia = withMedia;
        }

        public void BuildUp(RaytracingFrame3DData data)
        {
            PathSamplingType pathSamplingType = this.withMedia ? PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling : PathSamplingType.NoMedia;

            this.pixelRange = data.PixelRange;
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
            for (int depth=2; depth <= data.GlobalObjektPropertys.RecursionDepth; depth++) //depth steht sowohl für die Länge des Subpfads als auch des Fullpfads
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

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            FullPathSampleResult result = new FullPathSampleResult();
            result.RadianceFromRequestetPixel = new Vector3D(0, 0, 0);

            //1. Gehe über alle Fullpfadlängen
            for (int fullPathLength = 2; fullPathLength < this.pathLengthData.Length; fullPathLength++)
            {
                //2. Wähle für jede Fullpfadlänge eine zufällige Samplingstrategie aus
                var pathLength = this.pathLengthData[fullPathLength];
                if (pathLength.Strategies.Length == 0) continue;
                int strategyIndex = rand.Next(pathLength.Strategies.Length);
                var strategy = pathLength.Strategies[strategyIndex];

                //3. Erzeuge Eyepfad mit genau der Länge, wie es der Fullpathsampler braucht
                SubPath eyePath;
                Point pix = new Point(-1, -1);
                if (strategy.NeededEyePathLength > 0)
                {
                    //Wähle zufälligen Pixel aus
                    pix.X = this.pixelRange.XStart + rand.Next(this.pixelRange.Width);
                    pix.Y = this.pixelRange.YStart + rand.Next(this.pixelRange.Height);
                    eyePath = this.pathLengthData[strategy.NeededEyePathLength].EyePathSampler.SamplePathFromCamera(pix.X, pix.Y, rand);
                    if (eyePath.Points.Length != strategy.NeededEyePathLength) continue;
                }else
                {
                    eyePath = new SubPath(new PathPoint[0], 0);
                }

                //4. Erzeuge Lightpfad mit genau der Länge, wie es der Fullpathsampler braucht
                SubPath lightPath;
                if (strategy.NeededLightPathLength > 0)
                {
                    lightPath = this.pathLengthData[strategy.NeededLightPathLength].LightPathSampler.SamplePathFromLighsource(rand);
                    if (lightPath.Points.Length != strategy.NeededLightPathLength) continue;
                }
                else
                {
                    lightPath = new SubPath(new PathPoint[0], 0);
                }

                //5. Erzeuge Fullpfad laut ausgewählter Strategie
                var fullPath = strategy.Sampler.SampleFullPathFromSingleStrategy(eyePath, lightPath, fullPathLength, strategy.StrategyIndex, rand);

                //6. Füge Fullpath in Liste ein (Summe über alle Fullpaths wird im ImageCreatorFrame gemacht)
                if (fullPath != null)
                {
                    //Dividiere durch die Strategie-SelectionPdf indem mit der Strategieanzahl multipliziert wird
                    fullPath.MisWeight = 1;// pathLength.Strategies.Length; //Wenn ich den PathSpace mit GetPathContributionsForSinglePixel prüfe, dann rechntet der SinglePixelAnalyser mit der PathContribution und dem MIS-Gewicht die Radiance selber aus
                    fullPath.Radiance = fullPath.PathContribution;// * pathLength.Strategies.Length;
                    

                    //Wenn der Fullpathsampler kein Lighttracing ist, dann nimm die oben zufällig erzeugte Pixelposition
                    if (fullPath.PixelPosition == null) 
                        fullPath.PixelPosition = new Vector2D(pix.X + 0.5f, pix.Y + 0.5f);

                    result.LighttracingPaths.Add(fullPath);
                }
            }

            return result;
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
            :base(data)
        {
            this.Sampler = sampler;
        }
    }
}
