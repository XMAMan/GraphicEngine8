using RaytracingColorEstimator;
using FullPathGenerator;
using GraphicGlobal;
using System.Linq;
using GraphicMinimal;
using RaytracingRandom;

namespace RaytracingMethods.MMLT
{
    //Mittels Markov-Ketten wird für jede Pfadlänge ein Histogram(Jeder Pixel bekommt Wichtung laut Helligkeit) erstellt,
    //was über die AvgLuminance-Multiplikation ein fertiges Bild ergibt.  
    //https://cs.uwaterloo.ca/~thachisu/mmlt.pdf    
    //https://github.com/mmp/pbrt-v3/blob/master/src/integrators/mlt.cpp -> Von hier habe ich die Idee für den MLTSampler. Achtung diese Lösung enthält einige Fehler und darf nicht ohne weiteres übernommen werden
    //https://cs.uwaterloo.ca/~thachisu/smallmmlt.cpp -> Von hier stammt die Idee mit dem "mis * inverseStrategySelectionPmf"-Term und der modifizierten Accept-Gleichung 
    public class MultiplexedMetropolisLightTransport : IFrameEstimator
    {
        public bool CreatesLigthPaths { get; } = true;

        //Daten für alle Threads vom Konstruktor
        private bool withMedia;

        //Daten für alle Threads aus dem BuildUp
        private MLTFullPathSampler fullPathSampler;
        private MarkovChainCreator chainCreator;        

        //Daten pro Thread
        private MarkovChain[] chains; //Für jede Pfadlänge eine eigene Kette. In den Array sind die Pfadlängen [2,3,4,..]
        private PdfWithTableSampler chainSelector; //Jeder Thread hat leicht unterschiedliche Ketten-Normalisierungswerte und braucht somit sein eigenen ChainSelector

        public MultiplexedMetropolisLightTransport(bool withMedia)
        {
            this.withMedia = withMedia;            
        }

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.fullPathSampler = new MLTFullPathSampler(new SinglePathSampler(data, withMedia));
            this.chainCreator = new MarkovChainCreator(this.fullPathSampler, 100000, 0.01f, 0.3f);
        }

        public MultiplexedMetropolisLightTransport() { }
        private MultiplexedMetropolisLightTransport(MultiplexedMetropolisLightTransport copy)
        {
            this.withMedia = copy.withMedia;
            this.fullPathSampler = copy.fullPathSampler;
            this.chainCreator = copy.chainCreator;
        }
        public IFrameEstimator CreateCopy()
        {
            return new MultiplexedMetropolisLightTransport(this);
        }


        public void DoFramePrepareStep(int frameIterationNumber, IRandom rand)
        {
            //Erzeuge pro Thread ein MarkovChain-Array
            if (this.chains == null)
            {
                //Es gibt pro Pfadlänge genau eine Markovkette
                this.chains = this.chainCreator.CreateChainForEachPathLength(rand);
                this.chainSelector = PdfWithTableSampler.CreateFromUnnormalisizedFunctionArray(this.chains.Select(x => (double)x.ImagePlaneLuminance).ToArray());
            }         
        }

        //Diese Funktion wird pro Pixel gerufen. Pro Pixel erzeuge ich für eine zufällig ausgewählte Kette (Steht für eine Pfadlänge)
        //ein Markovchain-Mutationsschritt, was ein zufälligen Fullpath der durch ein zufälliges Pixel geht, erzeugt
        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            FullPathSampleResult result = new FullPathSampleResult();
            result.RadianceFromRequestetPixel = new Vector3D(0, 0, 0);

            //Wähle zufällig eine Kette (Fullpfad) aus
            int chainIndex = this.chainSelector.SampleDiscrete(rand.NextDouble());
            float chainSelectionPdf = (float)this.chainSelector.PdfValue(chainIndex);
            var chain = this.chains[chainIndex];

            //Durch das mutieren des Pfades entsteht ein neuer Pfad
            var chainResult = chain.RunIteration(this.fullPathSampler);

            //zuletzt accepted Pfad
            if (chainResult.CurrentPath != null)
            {
                SetRadiance(chainResult.CurrentPath, chainResult.CurrentWeight, chainSelectionPdf);
                result.LighttracingPaths.Add(chainResult.CurrentPath);
            }

            //durch Mutation entstanden aber noch nicht akzeptiert
            if (chainResult.ProposedPath != null)
            {
                SetRadiance(chainResult.ProposedPath, chainResult.ProposedWeight, chainSelectionPdf);
                result.LighttracingPaths.Add(chainResult.ProposedPath);
            }

            return result;
        }

        private void SetRadiance(FullPath path, float acceptWeight, float chainSelectionPdf)
        {
            path.MisWeight *= acceptWeight * this.chains[path.PathLength - 2].ImagePlaneLuminance / chainSelectionPdf / this.fullPathSampler.PixelCount;
            path.Radiance = path.PathContribution * path.MisWeight;
        }
    }
}
