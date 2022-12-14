using BitmapHelper;
using FullPathGenerator;
using GraphicGlobal;
using System;

namespace RaytracingMethods.MMLT
{
    //Das ist ein Fullpath, welcher per RunIteration modifiziert werden kann. Somit kann ich eine Liste von lauter Fullpaths erzeugen
    //Für jede Fullpfadlänge gibt es eine eigene Kette
    class MarkovChain
    {
        private IRandom rng;
        private MLTSampler sampler; //Das entspricht einer Menge von Zufallszahlen. Das ist ein Punkt im Hyperwürfel.   -> X
        private FullPath currentPath;//Wenn man den Hyperwürfel-Punkt in ein FullPath umwandelt                         -> f(X)
        private float currentC; //Gray-Wert der CurrentPath-Contribution                                                -> Scalar(f(X))
        private int fullPathLength; //Länge von diesen Pfad

        public float ImagePlaneLuminance { get; private set; } //Diese Konstate braucht man, wenn man diese Kette zum Integrieren nutzen will   -> b

        public MarkovChain(IRandom rng, MLTSampler sampler, FullPath path, float imagePlaneLuminance)
        {
            this.rng = rng;
            this.sampler = sampler;
            this.currentPath = path;
            this.currentC = path != null ? PixelHelper.ColorToGray(this.currentPath.PathContribution * this.currentPath.MisWeight) : 0;
            this.ImagePlaneLuminance = imagePlaneLuminance;

            //Es ist möglich, dass keine der Bootstrapsamples für diese Pfadlänge ein Pfad erzeugen konnten
            //In so ein Fall wäre auch die ImagePlaneLuminance 0 und beim Chain-Selection-Sampeln würde diese Kette niemals ausgewählt werden
            this.fullPathLength = path != null ? path.PathLength : -1; 
        }

        public class IterationResult
        {
            public FullPath CurrentPath;
            public double CurrentWeight;
            public FullPath ProposedPath;
            public double ProposedWeight;
        }

        public IterationResult RunIteration(MLTFullPathSampler fullPathSampler)
        {
            IterationResult result = new IterationResult();
            
            sampler.StartIteration();

            var proposedPath = fullPathSampler.SamplePath(sampler, fullPathLength);
            float propsedC = proposedPath != null ? PixelHelper.ColorToGray(proposedPath.PathContribution * proposedPath.MisWeight) : 0;
            double accept = proposedPath != null ? Math.Min(1, propsedC / this.currentC) : 0;

            //Proposed-Pfad:
            if (accept > 0)
            {
                result.ProposedPath = new FullPath(proposedPath);
                result.ProposedWeight = accept / propsedC;
            }

            //Current-Pfad:
            if (accept < 1)
            {
                result.CurrentPath = new FullPath(this.currentPath);
                result.CurrentWeight = (1 - accept) / this.currentC;
            }

            //Den Proposed-Pfad accepten oder rejecten
            if (rng.NextDouble() < accept)
            {
                this.currentPath = proposedPath;
                this.currentC = PixelHelper.ColorToGray(this.currentPath.PathContribution * this.currentPath.MisWeight);
                sampler.Accept();                
            }
            else
            {
                sampler.Reject();
            }

            return result;
        }
    }
}
