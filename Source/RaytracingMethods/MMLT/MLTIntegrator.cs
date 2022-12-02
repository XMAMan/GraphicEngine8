using BitmapHelper;
using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;
using RaytracingRandom;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaytracingMethods.MMLT
{
    internal class MLTIntegrator
    {
        private static int CameraStreamIndex = 0;
        private static int LightStreamIndex = 1;
        private static int ConnectionStreamIndex = 2;
        private static int StreamCount = 3;

        private readonly SingleFullPathSampler singleFullPathSampler;
        private readonly int nBootstrap;
        private readonly int nChains;
        private readonly int mutationsPerPixel;
        private readonly float sigma;
        private readonly float largeStepProbability;
        

        public MLTIntegrator(
            SingleFullPathSampler singleFullPathSampler, //Enthält die Scenendaten
            int nBootstrap, //nBootstrap = 100k = So viel Fullpaths werden am Anfang erzeugt
            int nChains,            //nChains = 1000 = Anzahl der Markovketten welche über die gesamte Bildebene verteilt sind
            int mutationsPerPixel,//mutationsPerPixel = 100 = Anzahl der Mutationen pro Markov-Kette
            float sigma,          //sigma=0.01f = Pertubationsweite bei der SmallStep-Mutation
            float largeStepProbability)//=0.3f = Wahrscheinlichkeit eine LargeStep-Mutation zu machen
        {
            this.singleFullPathSampler = singleFullPathSampler;
            this.nBootstrap = nBootstrap;
            this.nChains = nChains;
            this.mutationsPerPixel = mutationsPerPixel;
            this.sigma = sigma;
            this.largeStepProbability = largeStepProbability;
        }

        //Erzeugt einen Fullpfad mit genau der gewünschten Länge
        //Wenn der MLTSampler eine LargeStep-Iteration erzeugt, dann wird ein neuer Fullpfad erzeugt
        //Ist der MLTSampler in einer SmallStep-Iteration, dann wird der zuletzt vom MLTSampler erzeugte Pfad genommen und modifiziert
        private FullPath SamplePath(MLTSampler sampler, int fullPathLength)
        {
            //1 Wähle im Kamera-Stream eine Fullpath-Erzeugungsstrategie aus
            sampler.StartStream(CameraStreamIndex); 
            var strategy = this.singleFullPathSampler.SampleFullpathStrategy(fullPathLength, sampler);
            if (strategy == null) return null;

            //2. Erzeuge Eyepfad mit genau der Länge, wie es der Fullpathsampler braucht
            SubPath eyePath = this.singleFullPathSampler.SampleEyeSubPath(strategy.NeededEyePathLength, sampler, out Point pix);
            if (eyePath == null) return null;

            //3. Erzeuge Lightpfad mit genau der Länge, wie es der Fullpathsampler braucht
            sampler.StartStream(LightStreamIndex);
            SubPath lightPath = this.singleFullPathSampler.SampleLightSubPath(strategy.NeededLightPathLength, sampler);
            if (lightPath == null) return null;

            //4. Erzeuge Fullpfad laut ausgewählter Strategie
            sampler.StartStream(ConnectionStreamIndex);
            var fullPath = strategy.Sampler.SampleFullPathFromSingleStrategy(eyePath, lightPath, fullPathLength, strategy.StrategyIndex, sampler);
            if (fullPath == null) return null;

            //Wenn der Fullpathsampler kein Lighttracing ist, dann nimm die im EyeSubpfad zufällig erzeugte Pixelposition
            if (fullPath.PixelPosition == null)
                fullPath.PixelPosition = new Vector2D(pix.X + 0.5f, pix.Y + 0.5f);

            //Für jede Fullpfadlänge wähle ich gleichmäßig zufällig eine Strategie mit Pmf = 1/nStrategies aus; 
            //Der Fullpfad muss durch diese Pmf dividiert werden (entspricht *nStrategies)
            int nStrategies = this.singleFullPathSampler.GetStrategyCountForFullPathLength(fullPathLength);
            fullPath.Radiance = fullPath.PathContribution;// * nStrategies;

            return fullPath;
        }

        //Erzeugt über die gesamte Bildebene zufällige Fullpaths um somit einerseits die Gesamthelligkeit der Bildebene zu ermitteln als auch Start-Werte für die Markovketten zu bekommen
        //Die Luminancewerte von diesen Fullpaths stehen in der zurück gegebenen PdfWithTableSampler (1D-Distribution)
        public PdfWithTableSampler CreateBootstrapSamples()
        {
            //Für jeden bootstrapWeights-Eintrag wird ein Fullpfad erzeugt und dessen Luminancewert im Array gespeichert
            //Wenn ich eine Maximalpfadlänge von 4 habe, dann sind die Pfadlängen im Array: 2,3,4, 2,3,4, 2,3,4, ...
            double[] bootstrapWeights = new double[this.nBootstrap * (this.singleFullPathSampler.MaxFullPathLength - 1)];

            Parallel.For(0, this.nBootstrap, (i) => //Entspricht for (int i=0;i<this.nBootstrap;i++)
            {
                for (int fullPathLength = 2; fullPathLength <= this.singleFullPathSampler.MaxFullPathLength; fullPathLength++)
                {
                    int rngIndex = i * (this.singleFullPathSampler.MaxFullPathLength - 1) + (fullPathLength - 2);
                    MLTSampler sampler = new MLTSampler(new Rand(rngIndex), this.sigma, this.largeStepProbability, StreamCount);

                    FullPath path = SamplePath(sampler, fullPathLength);
                    if (path == null)
                        bootstrapWeights[rngIndex] = 0;
                    else
                        bootstrapWeights[rngIndex] = PixelHelper.ColorToGray(path.Radiance);
                }
            });            

            PdfWithTableSampler bootstrap = PdfWithTableSampler.CreateFromUnnormalisizedFunctionArray(bootstrapWeights);

            return bootstrap;
        }

        public ImageBuffer RunMarkovChains(PdfWithTableSampler bootstrap)
        {
            var range = this.singleFullPathSampler.PixelRange;

            //So viele Mutationsschritte sollen insgesamt gemacht werden
            int nTotalMutations = this.mutationsPerPixel * range.Width * range.Height;

            //Gehe mit i über alle Markov-Ketteen            
            ParallelForData<ImageBuffer> imagesPerThread = new ParallelForData<ImageBuffer>(() => new ImageBuffer(range.Width, range.Height, new Vector3D(0,0,0)));
            //Parallel.For(0, this.nChains, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (i) => //So kann ich die Threadanzahl festlegen
            Parallel.For(0, this.nChains, (i) => //Entspricht for (int i=0;i<this.nChains;i++)
            {           
                //Pro Thread speichere ich ein ImageBuffer auf was ich hier zugreife
                var image = imagesPerThread.GetDataFromCurrentThread();

                //nTotalMutations wird in 1000 Abschnitte/MarkovKetten unterteilt.
                //Jede Kette enthält nChainMutations Mutationen. D.h. jede Kette mutiert 0.1% der Gesamtmutationen. 
                //Alle Ketten enthalten gleichviele Mutationen außer die letzte 1000te Kette. Sie hat weniger wegen den Min-Clamping
                //nChainMutations = So viel Mutationen soll die i-te Kette mutieren
                int nChainMutations = Math.Min((i + 1) * nTotalMutations / this.nChains, nTotalMutations) - i * nTotalMutations / this.nChains;

                //Wähle zufällig von den 600k Fullpaths einen aus
                Rand rng = new Rand(i);
                int bootstrapIndex = bootstrap.SampleDiscrete(rng.NextDouble());
                int fullPathLength = (bootstrapIndex % (this.singleFullPathSampler.MaxFullPathLength - 1)) + 2;

                //sampler entspricht den X-Wert
                MLTSampler sampler = new MLTSampler(new Rand(bootstrapIndex), this.sigma, this.largeStepProbability, StreamCount);
                FullPath path = SamplePath(sampler, fullPathLength); //path entspricht f(X)

                //Dieser ausgewählte Fullpfad wird nun nChainMutations mal mutiert
                for (int j=0;j<nChainMutations;j++)
                {
                    sampler.StartIteration();
                    FullPath proposed = SamplePath(sampler, fullPathLength);
                    float accept = proposed != null ? Math.Min(1, PixelHelper.ColorToGray(proposed.Radiance) / PixelHelper.ColorToGray(path.Radiance)) : 0;

                    //Farbwerte vom alten und Proposed-Pfad auf ImageBuffer speichern
                    if (accept > 0)
                    {
                        if (IsPathInPixelRange(proposed))
                        {
                            Vector3D proposedColor = proposed.Radiance * accept / PixelHelper.ColorToGray(proposed.Radiance);

                            AddColorToImageBuffer(image, proposed.PixelPosition, proposedColor);
                        }                        
                    }
                    
                    if (IsPathInPixelRange(path))
                    {
                        Vector3D currentColor = path.Radiance * (1 - accept) / PixelHelper.ColorToGray(path.Radiance);
                        AddColorToImageBuffer(image, path.PixelPosition, currentColor);
                    }
                    
                    //Den Proposed-Pfad accepten und rejecten
                    if (rng.NextDouble() < accept)
                    {
                        path = proposed;
                        sampler.Accept();
                    }else
                    {
                        sampler.Reject();
                    }
                }
            });

            float imagePlaneLuminance = (float)bootstrap.NormalisationConstant / this.nBootstrap;
            return imagesPerThread.Sum().GetColorScaledImage(imagePlaneLuminance / this.mutationsPerPixel);            
        }

        private bool IsPathInPixelRange(FullPath path)
        {
            return path.PixelPosition.X >= this.singleFullPathSampler.PixelRange.Left &&
                   path.PixelPosition.X <= this.singleFullPathSampler.PixelRange.Right &&
                   path.PixelPosition.Y >= this.singleFullPathSampler.PixelRange.Top &&
                   path.PixelPosition.Y <= this.singleFullPathSampler.PixelRange.Bottom;
        }

        private void AddColorToImageBuffer(ImageBuffer image, Vector2D pixelPosition, Vector3D color)
        {
            int rx = (int)Math.Floor(pixelPosition.X - this.singleFullPathSampler.PixelRange.XStart);
            int ry = (int)Math.Floor(pixelPosition.Y - this.singleFullPathSampler.PixelRange.YStart);
            if (rx >= 0 && rx < image.Width && ry >= 0 && ry < image.Height)
            {
                image[rx, ry] += color;
            }
        }
    }
}
