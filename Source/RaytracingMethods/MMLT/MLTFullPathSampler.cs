using FullPathGenerator;
using GraphicMinimal;
using SubpathGenerator;
using System.Drawing;

namespace RaytracingMethods.MMLT
{
    //Erzeugt ein Fullpath (Komplett neu oder durch modifizieren eines vorhandenen Pfades) unter Verwendung des
    //MLTSampler (IRandom-Objekt was aber die Zufallszahlen vom zuletzt erzeugten Fullpfad enthält)
    class MLTFullPathSampler
    {
        private static int CameraStreamIndex = 0;
        private static int LightStreamIndex = 1;
        private static int ConnectionStreamIndex = 2;

        private SinglePathSampler singlePathSampler;
        private ImagePixelRange pixelRange { get => this.singlePathSampler.PixelRange; }
        public long PixelCount { get => (long)this.pixelRange.Width * this.pixelRange.Height; }


        public int MaxFullPathLength { get => this.singlePathSampler.MaxFullPathLength; }        

        public MLTFullPathSampler(SinglePathSampler singlePathSampler)
        {
            this.singlePathSampler = singlePathSampler;
        }

        //Erzeugt einen Fullpfad mit genau der gewünschten Länge
        //Wenn der MLTSampler eine LargeStep-Iteration erzeugt, dann wird ein neuer Fullpfad erzeugt
        //Ist der MLTSampler in einer SmallStep-Iteration, dann wird der zuletzt vom MLTSampler erzeugte Pfad genommen und modifiziert
        public FullPath SamplePath(MLTSampler sampler, int fullPathLength)
        {
            //1 Wähle im Kamera-Stream eine Fullpath-Erzeugungsstrategie aus
            sampler.StartStream(CameraStreamIndex);
            var strategy = this.singlePathSampler.SampleFullpathStrategy(fullPathLength, sampler);
            if (strategy == null) return null;

            //2. Erzeuge Eyepfad mit genau der Länge, wie es der Fullpathsampler braucht
            SubPath eyePath = this.singlePathSampler.SampleEyeSubPath(strategy.NeededEyePathLength, sampler, out Point pix);
            if (eyePath == null) return null;

            //3. Erzeuge Lightpfad mit genau der Länge, wie es der Fullpathsampler braucht
            sampler.StartStream(LightStreamIndex);
            SubPath lightPath = this.singlePathSampler.SampleLightSubPath(strategy.NeededLightPathLength, sampler);
            if (lightPath == null) return null;

            //4. Erzeuge Fullpfad laut ausgewählter Strategie
            sampler.StartStream(ConnectionStreamIndex);
            var fullPath = strategy.Sampler.SampleFullPathFromSingleStrategy(eyePath, lightPath, fullPathLength, strategy.StrategyIndex, sampler);
            if (fullPath == null) return null;

            //5. Wenn der Fullpathsampler kein Lighttracing ist, dann nimm die im EyeSubpfad zufällig erzeugte Pixelposition
            if (fullPath.PixelPosition == null)
                fullPath.PixelPosition = new Vector2D(pix.X + 0.5f, pix.Y + 0.5f);

            //6. Ich interessiere mich nur für Pfade, welche zur Bildebene was beitragen
            if (IsPathInPixelRange(fullPath) == false) return null;


            //7. Der Fake-MIS-Term. Die Idee dafür stammt von hier: https://cs.uwaterloo.ca/~thachisu/smallmmlt.cpp
            //Um zu verstehen, warum der hier gesampelte Pfad mit mis/strategySelectionPmf gewichtet wird muss man sich gedanklich 
            //vorstellen hier würde Variante 1 stehen und dann will ich aber dafür sorgen, dass ich nur noch ein Fullpath pro
            //Sampleschritt erzeugen, um somit dann zu Variante 2 zu kommen

            //Variante 1: Ich erzeuge mit zwei Fullpathsamplern zwei Fullpaths und kombiniere sie mit MIS
            //var path1 = SampleWithPathtracing()
            //var path2 = SampleWithDirectLighting()
            //float mis1 = pdf(path1) / (pdf(path1) + pdf(path2))
            //float mis2 = pdf(path2) / (pdf(path1) + pdf(path2))
            //Vector pixelRadiance = mis1 * path1.Contribution + mis2 * path2.Contribution

            //Variante 2: Ich ersetze die pixelRadiance-Summe durch eine MonteCarlo-Summe
            //int strategy = rand.Next(2); //0 = Pathtracing; 1 = DirectLighting
            //float strategySelectionPmf = 1f / 2;
            //switch (strategy) -> Erzeuge path1 und mis1 oder path2 und mis2
            //Vector pixelRadiance = mis * path.Contribution / strategySelectionPmf

            float mis = this.singlePathSampler.GetMisWeight(fullPath);
            int inverseStrategySelectionPmf = this.singlePathSampler.GetStrategyCountForFullPathLength(fullPathLength);
            fullPath.MisWeight = mis * inverseStrategySelectionPmf;

            return fullPath;
        }

        private bool IsPathInPixelRange(FullPath path)
        {
            return path.PixelPosition.X >= this.singlePathSampler.PixelRange.Left &&
                   path.PixelPosition.X <= this.singlePathSampler.PixelRange.Right &&
                   path.PixelPosition.Y >= this.singlePathSampler.PixelRange.Top &&
                   path.PixelPosition.Y <= this.singlePathSampler.PixelRange.Bottom;
        }
    }
}
