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

        private SingleFullPathSampler singleFullPathSampler;
        private ImagePixelRange pixelRange;
        private bool withMedia;

        public MultiplexedMetropolisLightTransport(bool withMedia)
        {
            this.withMedia = withMedia;
        }

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.pixelRange = data.PixelRange;
            this.singleFullPathSampler = new SingleFullPathSampler(data, this.withMedia);
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            FullPathSampleResult result = new FullPathSampleResult();
            result.RadianceFromRequestetPixel = new Vector3D(0, 0, 0);

            //1. Gehe über alle Fullpfadlängen
            for (int fullPathLength = 2; fullPathLength <= this.singleFullPathSampler.MaxFullPathLength; fullPathLength++)
            {
                //2. Wähle für jede Fullpfadlänge eine zufällige Samplingstrategie aus
                var strategy = this.singleFullPathSampler.SampleFullpathStrategy(fullPathLength, rand);
                if (strategy == null) continue;

                //3. Erzeuge Eyepfad mit genau der Länge, wie es der Fullpathsampler braucht
                SubPath eyePath;
                Point pix = new Point(-1, -1);
                if (strategy.NeededEyePathLength > 0)
                {
                    //Wähle zufälligen Pixel aus
                    pix.X = this.pixelRange.XStart + rand.Next(this.pixelRange.Width);
                    pix.Y = this.pixelRange.YStart + rand.Next(this.pixelRange.Height);
                    eyePath = this.singleFullPathSampler.SampleEyeSubPath(strategy.NeededEyePathLength, pix.X, pix.Y, rand);
                    if (eyePath.Points.Length != strategy.NeededEyePathLength) continue;
                }else
                {
                    eyePath = new SubPath(new PathPoint[0], 0);
                }

                //4. Erzeuge Lightpfad mit genau der Länge, wie es der Fullpathsampler braucht
                SubPath lightPath;
                if (strategy.NeededLightPathLength > 0)
                {
                    lightPath = this.singleFullPathSampler.SampleLightSubPath(strategy.NeededLightPathLength, rand);
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

    
}
