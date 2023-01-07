using RaytracingColorEstimator;
using FullPathGenerator;
using GraphicGlobal;
using System.Linq;
using GraphicMinimal;
using RaytracingRandom;
using System.Collections.Generic;

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
            this.chainCreator = new MarkovChainCreator(data.ProgressChanged, this.fullPathSampler, data.GlobalObjektPropertys.MetropolisBootstrapCount, 0.01f, 0.3f);
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

            //Möglichkeit 1: Wähle zufällig eine Kette (Fullpfad) aus
            int chainIndex = this.chainSelector.SampleDiscrete(rand.NextDouble());
            double chainSelectionPdf = this.chainSelector.PdfValue(chainIndex);
            var chain = this.chains[chainIndex];
            result.LighttracingPaths.AddRange(SampleChain(chain, chainSelectionPdf));            

            //Möglichkeit 2: Für jede Fullpfadlänge wird die zugehörige Kette genommen und ein Pfad erzeugt
            //for (int chainIndex=0; chainIndex < this.chains.Length;chainIndex++)
            //{
            //    var chain = this.chains[chainIndex];
            //    if (chain.ImagePlaneLuminance != 0)
            //    {
            //        double chainSelectionPdf = 1;
            //        result.LighttracingPaths.AddRange(SampleChain(this.chains[chainIndex], chainSelectionPdf));
            //    }                
            //}

            return result;
        }

        private List<FullPath> SampleChain(MarkovChain chain, double chainSelectionPdf)
        {
            List<FullPath> returnList = new List<FullPath>();

            //Durch das mutieren des Pfades entsteht ein neuer Pfad
            var chainResult = chain.RunIteration(this.fullPathSampler);

            //zuletzt accepted Pfad
            if (chainResult.CurrentPath != null)
            {
                SetRadiance(chainResult.CurrentPath, chainResult.CurrentWeight, chainSelectionPdf);
                returnList.Add(chainResult.CurrentPath);
            }

            //durch Mutation entstanden aber noch nicht akzeptiert
            if (chainResult.ProposedPath != null)
            {
                SetRadiance(chainResult.ProposedPath, chainResult.ProposedWeight, chainSelectionPdf);
                returnList.Add(chainResult.ProposedPath);
            }

            return returnList;
        }

        private void SetRadiance(FullPath path, double chainWeight, double chainSelectionPdf)
        {
            //path.MisWeight = (float)(path.MisWeight * chainWeight * this.chains[path.PathLength - 2].ImagePlaneLuminance / chainSelectionPdf / this.fullPathSampler.PixelCount); //Ohne Kelemen-MIS-Faktor
            path.MisWeight = (float)(path.MisWeight * chainWeight / chainSelectionPdf / this.fullPathSampler.PixelCount); //Achtung: Da ich wegen den Kelemen-MIS-Faktor die ImagePlaneLuminance-Multiplikation bereits im chainWeight drin habe, muss ich das hier nicht nochmal machen
            path.Radiance = path.PathContribution * path.MisWeight;
        }

        public ImageBuffer DoFramePostprocessing(int frameIterationNumber, ImageBuffer frame)
        {
            return frame;
        }
    }
}
