using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;
using RaytracingColorEstimator;
using RaytracingMethods.MMLT;
using SubpathGenerator;
using System.Drawing;

namespace RaytracingMethods
{
    //BPT (PT,LT,DL,VC) wo pro Pixel-Sample-Schritt ein einzelner Fullpfad erzeugt wird der duch ein zufälliges Pixel geht
    //Diese Klasse hilft um besser zu verstehen, was MMLT macht
    public class SingleFullPathBPT : IFrameEstimator
    {
        public bool CreatesLigthPaths { get; } = true;

        private SinglePathSampler subPathSampler;
        private bool withMedia;

        public SingleFullPathBPT(bool withMedia)
        {
            this.withMedia = withMedia;
        }

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.subPathSampler = new SinglePathSampler(data, this.withMedia);
        }

        public SingleFullPathBPT() { }
        private SingleFullPathBPT(SingleFullPathBPT copy)
        {
            this.subPathSampler = copy.subPathSampler;
            this.withMedia = copy.withMedia;

        }
        public IFrameEstimator CreateCopy()
        {
            return new SingleFullPathBPT(this);
        }

        public void DoFramePrepareStep(int frameIterationNumber, IRandom rand)
        {
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            FullPathSampleResult result = new FullPathSampleResult();
            result.RadianceFromRequestetPixel = new Vector3D(0, 0, 0);

            //1. Wähle eine zufällige Fullpfadlänge
            int fullPathLength = rand.Next(this.subPathSampler.MaxFullPathLength - 1) + 2;

            //2. Wähle für jede Fullpfadlänge eine zufällige Samplingstrategie aus
            var strategy = this.subPathSampler.SampleFullpathStrategy(fullPathLength, rand);
            if (strategy == null) return result;

            //3. Erzeuge Eyepfad mit genau der Länge, wie es der Fullpathsampler braucht
            SubPath eyePath = this.subPathSampler.SampleEyeSubPath(strategy.NeededEyePathLength, rand, out Point pix);
            if (eyePath == null) return result;

            //4. Erzeuge Lightpfad mit genau der Länge, wie es der Fullpathsampler braucht
            SubPath lightPath = this.subPathSampler.SampleLightSubPath(strategy.NeededLightPathLength, rand);
            if (lightPath == null) return result;

            //5. Erzeuge Fullpfad laut ausgewählter Strategie
            var fullPath = strategy.Sampler.SampleFullPathFromSingleStrategy(eyePath, lightPath, fullPathLength, strategy.StrategyIndex, rand);

            //6. Füge Fullpath in Liste ein (Summe über alle Fullpaths wird im ImageCreatorFrame gemacht)
            if (fullPath != null)
            {
                //Fake-MIS (Monte-Carlo-Summe aus mis1*Strategie1 + mis2*Strategie2 + ...)
                float mis = this.subPathSampler.GetMisWeight(fullPath);
                int inverseStrategySelectionPmf = this.subPathSampler.GetStrategyCountForFullPathLength(fullPathLength);
                fullPath.MisWeight = mis * inverseStrategySelectionPmf;

                //Da ich die Pfadänge sample multipliziere ich mit der Inverse-Pathlength-Selection-Pmf
                fullPath.MisWeight *= (this.subPathSampler.MaxFullPathLength - 1);

                //Da ich pro Sample über die gesamte Bildebene integriere (Kommt durchs PixelSelection-Sampling zustande) und ich pro Frame
                //PixelCount Samples erzeuge, muss ich durch diese PixelCount dividieren um ein korrekten Frame-Estimate zu erhalten
                fullPath.MisWeight /= this.subPathSampler.PixelRange.Count;

                fullPath.Radiance = fullPath.PathContribution * fullPath.MisWeight;


                //Wenn der Fullpathsampler kein Lighttracing ist, dann nimm die oben zufällig erzeugte Pixelposition
                if (fullPath.PixelPosition == null)
                    fullPath.PixelPosition = new Vector2D(pix.X + 0.5f, pix.Y + 0.5f);

                result.LighttracingPaths.Add(fullPath);
            }

            return result;
        }
    }
}
