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
        
        private bool withMedia;

        public MultiplexedMetropolisLightTransport(bool withMedia)
        {
            this.withMedia = withMedia;
        }

        private ImageBuffer image;

        public void BuildUp(RaytracingFrame3DData data)
        {            
            this.singleFullPathSampler = new SingleFullPathSampler(data, this.withMedia);

            //MLTIntegrator mLTIntegrator = new MLTIntegrator(this.singleFullPathSampler, 100000, 1000, 100, 0.01f, 0.3f);
            MLTIntegrator mLTIntegrator = new MLTIntegrator(this.singleFullPathSampler, 100000, 1000, data.GlobalObjektPropertys.SamplingCount, 0.01f, 0.3f);
            var bootstrap = mLTIntegrator.CreateBootstrapSamples();
            this.image = mLTIntegrator.RunMarkovChains(bootstrap);

            Vector3D pixelRadiance = image[0,0]; //expected = 712.3121337890625 
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            var r = this.singleFullPathSampler.PixelRange;
            return new FullPathSampleResult() { RadianceFromRequestetPixel = this.image[x - r.XStart, y - r.YStart] };

            FullPathSampleResult result = new FullPathSampleResult();
            result.RadianceFromRequestetPixel = new Vector3D(0, 0, 0);

            //1. Gehe über alle Fullpfadlängen
            for (int fullPathLength = 2; fullPathLength <= this.singleFullPathSampler.MaxFullPathLength; fullPathLength++)
            {
                //2. Wähle für jede Fullpfadlänge eine zufällige Samplingstrategie aus
                var strategy = this.singleFullPathSampler.SampleFullpathStrategy(fullPathLength, rand);
                if (strategy == null) continue;

                //3. Erzeuge Eyepfad mit genau der Länge, wie es der Fullpathsampler braucht
                SubPath eyePath = this.singleFullPathSampler.SampleEyeSubPath(strategy.NeededEyePathLength, rand, out Point pix);
                if (eyePath == null) continue;

                //4. Erzeuge Lightpfad mit genau der Länge, wie es der Fullpathsampler braucht
                SubPath lightPath = this.singleFullPathSampler.SampleLightSubPath(strategy.NeededLightPathLength, rand);
                if (lightPath == null) continue;                

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
