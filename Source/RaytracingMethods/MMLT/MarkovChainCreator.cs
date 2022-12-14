using BitmapHelper;
using FullPathGenerator;
using GraphicGlobal;
using RaytracingRandom;
using System;
using System.Threading.Tasks;

namespace RaytracingMethods.MMLT
{
    //Erzeugt ein MarkovChain-Objekt für eine gegebene Pfadlänge
    class MarkovChainCreatorForSinglePathLength
    {
        private static int StreamCount = 3;

        private readonly MLTFullPathSampler singleFullPathSampler;
        private readonly float sigma;
        private readonly float largeStepProbability;
        private readonly int fullPathLength;
        private PdfWithTableSampler bootstrap;

        public float ImagePlaneLuminance { get; private set; }

        public MarkovChainCreatorForSinglePathLength(
            MLTFullPathSampler fullPathSampler,
            int nBootstrap, //nBootstrap = 100k = So viel Fullpaths werden am Anfang erzeugt
            float sigma,          //sigma=0.01f = Pertubationsweite bei der SmallStep-Mutation
            float largeStepProbability,//=0.3f = Wahrscheinlichkeit eine LargeStep-Mutation zu machen
            int fullPathLength
            )
        {
            this.singleFullPathSampler = fullPathSampler;
            this.sigma = sigma;
            this.largeStepProbability = largeStepProbability;
            this.fullPathLength = fullPathLength;
            this.bootstrap = CreateBootstrapSamples(nBootstrap);

            this.ImagePlaneLuminance = (float)bootstrap.NormalisationConstant / nBootstrap;
        }

        //Erzeugt über die gesamte Bildebene zufällige Fullpaths um somit einerseits die Gesamthelligkeit der Bildebene zu ermitteln als auch Start-Werte für die Markovketten zu bekommen
        //Die Luminancewerte von diesen Fullpaths stehen in der zurück gegebenen PdfWithTableSampler (1D-Distribution)
        private PdfWithTableSampler CreateBootstrapSamples(int nBootstrap)
        {
            //Für jeden bootstrapWeights-Eintrag wird ein Fullpfad erzeugt und dessen Luminancewert im Array gespeichert
            double[] bootstrapWeights = new double[nBootstrap];

            Parallel.For(0, nBootstrap, (i) => //Entspricht for (int i=0;i<nBootstrap;i++)
            {
                MLTSampler sampler = new MLTSampler(new Rand(i), this.sigma, this.largeStepProbability, StreamCount);

                FullPath path = this.singleFullPathSampler.SamplePath(sampler, this.fullPathLength);
                if (path == null)
                    bootstrapWeights[i] = 0;
                else
                    bootstrapWeights[i] = PixelHelper.ColorToGray(path.PathContribution * path.MisWeight);
            });

            PdfWithTableSampler bootstrap = PdfWithTableSampler.CreateFromUnnormalisizedFunctionArray(bootstrapWeights);

            return bootstrap;
        }

        public MarkovChain CreateMarkovChain(IRandom rand)
        {
            //Wähle zufällig von den 100k Fullpaths einen aus
            int bootstrapIndex = this.bootstrap.SampleDiscrete(rand.NextDouble());

            //sampler entspricht den X-Wert
            MLTSampler sampler = new MLTSampler(new Rand(bootstrapIndex), this.sigma, this.largeStepProbability, StreamCount);
            FullPath path = this.singleFullPathSampler.SamplePath(sampler, this.fullPathLength); //path entspricht f(X)

            if (path != null && path.PathLength != this.fullPathLength) throw new Exception("Pathlength missmatch");

            return new MarkovChain(rand, sampler, path, this.ImagePlaneLuminance);
        }
    }

    class MarkovChainCreator
    {
        private readonly MarkovChainCreatorForSinglePathLength[] creators;

        public MarkovChainCreator(
            Action<string, float> progressChanged,
            MLTFullPathSampler fullPathSampler,
            int nBootstrap, //nBootstrap = 100k = So viel Fullpaths werden am Anfang erzeugt
            float sigma,          //sigma=0.01f = Pertubationsweite bei der SmallStep-Mutation
            float largeStepProbability//=0.3f = Wahrscheinlichkeit eine LargeStep-Mutation zu machen
            )
        {
            this.creators = new MarkovChainCreatorForSinglePathLength[fullPathSampler.MaxFullPathLength - 1];
            for (int i=0;i<this.creators.Length;i++)
            {
                progressChanged("Create Bootstrap-Samples", i / (float)this.creators.Length * 100);
                this.creators[i] = new MarkovChainCreatorForSinglePathLength(fullPathSampler, nBootstrap, sigma, largeStepProbability, i + 2);
            }
        }

        public MarkovChain[] CreateChainForEachPathLength(IRandom rand)
        {
            MarkovChain[] chains = new MarkovChain[this.creators.Length];

            for (int i = 0; i < this.creators.Length; i++)
            {
                chains[i] = this.creators[i].CreateMarkovChain(rand);
            }

            return chains;
        }
    }
}
