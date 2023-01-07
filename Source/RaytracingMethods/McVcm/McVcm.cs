using BitmapHelper;
using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;
using Photonusmap;
using RaytracingColorEstimator;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Hier wird PT+DL mit MarkovChain-VC/VM/LT vereint.
//Es gibt hier zwei Markovketten, dessen Targetfunktion die Luminance/Visible von der ImagePlane ist.
//Arbeitet man mit Markovketten, dann muss man IMMER als Targetfunktion die ImagePlane und nie ein Pixel haben, da man ja ein Histogram über
//die ImagePlane und nicht über ein einzelnes Pixel will.
//Grobablauf:
//1. Am Frameanfang für jedes Pixel ein EyeSubpath erstellen
//2. Am Frameanfang 10000 ImagePlane-Schätzwerte errechnen und 2 davon zufällig aus Seedwert ausählen
//3. Mit zwei Markovketten(VisibleChain/ContribtutionChain) PixelCount Iterationen durchlaufen. Die Ketten tauschen per Replica Exchange ihre Zustände
//  -> Die Ketten erzeugen pro Iteration mit VC+VM+LT eine ImagePlane-Schätzwert
//  -> Jede Kette hat ein LightSubpath, den sie mutiert und wo sie versucht ihn so zu verändert, dass er zusammen mit den EyeSubpaths ein hohen
//     TargetFunktionswert bekommt
//4. Pro Pixel wird per PT und DL auch noch ein Schätzwert genommen
//5. Am Frameende wird das PT_DL-Bild und das VC_VM_LT-Bild zusammen addiert

//Hinweis: Für die VC/VM/LT-Samples wähle ich zufällig ein Pixel aus. Deswegen müsste ich den EyeSubpath mit der PixelCount wichten und dessen Pdf mit der PixelCount dividieren
//Wenn ich das machen würde, dann müsste ich am Frameende das Bild auch mit der Ketten-Normalisierungskonstante multiplizieren und mit der PixelCount divideren
//Da ich aber die EyeSubpath-Pixel-Count-Multiplikation nicht gemacht habe, fällt auch die PixelCount-Division am Frameende weg

//Offene Fragen:

//-Warum muss ich die LightTracing-Samples durch die PixelCount dividieren aber VC/VM nicht?
// ->Wegen der fehlenden EyeSubpath-PixelSelection-Pdf im EyeSubpath fehlt dieser Faktor bei VC/VM/FrameEnde
// ->Eigentlich müsste der PixelCount-Faktor bei VC/VM/Frameende rein und dafür bei LT raus. Ich habe hier quasi alle 4 Stellen um den
//   PixelCount-Faktor gekürzt. Diese PixelCount-Division ist also die Kürzung.

//-Was bedeutet LuminanceCorrectionFactor=AcceptWeightSum / PixelCount ?
// -> Meine Vermutung: Ich vereine ja in der MIS-Formel PT_DL und VC_VM_LT obwohl das eine mit Pathtracing gesampelt wird und das andere per MarkovKette.
//    Die Pdf von ein Markovketten-Sample wäre ja TargetFunktion/Normalisierungskonstante. Da ich aber die Kettensamples mit der "normalen" MIS-Formel
//    verwende, ist diese LuminanceCorrectionFactor vermutlich der Ausgleich für diese Kette-Nicht-Kette-MIS-Vereinigung. 

namespace RaytracingMethods.McVcm
{
    //Robust Light Transport Simulation via Metropolised Bidirectional Estimators - Martin Šik, Hisanari Otsu, Toshiya Hachisuka, Jaroslav Křivánek 2016
    //https://cgg.mff.cuni.cz/~sik/meb/index.html
    public class McVcm : IFrameEstimator
    {
        //Daten für alle Threads vom Konstruktor
        private readonly bool withMedia;

        //Daten für alle Threads aus dem BuildUp
        private RaytracingFrame3DData frameData;
        private PixelRadianceData pixelData;
        private SplatListSampler splatListSampler;

        //Daten pro Thread
        private PixelRadianceCalculator pixelRadianceCalculator; //Speichert und erzeugt die Eye-Map
        private FramePrepareHelper framePrepareHelper = null;       
        private EyeMap eyeMap;
        private MarkovChain[] chains; //2 Ketten (VisibleChain + ContributionChain)

        public bool CreatesLigthPaths { get; } = true;

        public McVcm(bool withMedia)
        {
            this.withMedia = withMedia;
        }

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.frameData = data;

            this.pixelData = PixelRadianceCreationHelper.CreatePixelRadianceData(data, new SubPathSettings()
            {
                EyePathType = this.withMedia ? PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling : PathSamplingType.NoMedia,
                LightPathType = this.withMedia ? PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling : PathSamplingType.NoMedia,
            },null);

            this.pixelRadianceCalculator = new PixelRadianceCalculator(pixelData);
            this.pixelRadianceCalculator.PixelPhotonenCounter = new PixelPhotonenCounter(this.pixelRadianceCalculator);

            this.splatListSampler = new  SplatListSampler(new FullPathKonstruktorData()
            {
                EyePathSamplingType = this.pixelData.EyePathSampler.PathSamplingType,
                LightPathSamplingType = this.pixelData.LightPathSampler.PathSamplingType,
                PointToPointConnector = new PointToPointConnector(new RayVisibleTester(this.pixelData.IntersectionFinder, this.pixelData.MediaIntersectionFinder), this.pixelData.RayCamera, this.pixelData.EyePathSampler.PathSamplingType, this.pixelData.PhaseFunction),
                RayCamera = this.pixelData.RayCamera,
                LightSourceSampler = this.pixelData.LightSourceSampler,
                MaxPathLength = data.GlobalObjektPropertys.RecursionDepth,
            },
            new FullPathSettings()
            {
                UsePathTracing = true,
                UseDirectLighting = true,
                //UseMultipleDirectLighting = true,
                UseVertexConnection = true,
                UseVertexMerging = true,
                UseLightTracing = true,
            });

            
        }

        public McVcm() { }
        private McVcm(McVcm copy)
        {
            this.withMedia = copy.withMedia;
            this.frameData = copy.frameData;
            this.pixelData = copy.pixelData;
            this.splatListSampler = copy.splatListSampler;
            this.pixelRadianceCalculator = new PixelRadianceCalculator(copy.pixelRadianceCalculator);
        }

        public IFrameEstimator CreateCopy()
        {
            return new McVcm(this);
        }

        //Erzeuge für jeden Pixel ein Subpfad und speichere ihn in einer Photonmap
        public void DoFramePrepareStep(int frameIterationCount, IRandom rand)
        {
            if (this.framePrepareHelper == null) 
                this.framePrepareHelper = new FramePrepareHelper(this.frameData, this.pixelData, this.splatListSampler, rand);

            this.pixelRadianceCalculator.PixelPhotonenCounter.FrameIterationCount = frameIterationCount;

            //Schritt 1: Am Frameanfang eine Eyemap erzeugen
            this.eyeMap = this.framePrepareHelper.CreateEyeMap();
            eyeMap.FrameData.PhotonMaps.GlobalSurfacePhotonmap.SearchRadius = PhotonMapSearchRadiusCalculator.GetSearchRadiusForPhotonmapWithPixelFootprint(this.pixelData.IntersectionFinder, this.pixelData.RayCamera);

            //Schritt 2: Am Frameanfang 10000 SplatList-Werte erzeugen und 2 davon als Seed-Wert auswählen
            var seeds = this.framePrepareHelper.CreateSeedValues(this.eyeMap);


            //Schritt 3: Markov-Ketten anlegen / Initialisieren
            if (this.chains == null)
            {
                this.chains = new MarkovChain[]
                {
                    new MarkovChain(seeds[0], this.frameData.PixelRange),
                    new MarkovChain(seeds[1], this.frameData.PixelRange)
                };
            }else
            {
                this.chains[0].Reset(seeds[0]);
                this.chains[1].Reset(seeds[1]);
            }
            
        }

        private static float Target(float luminance, int chainIndex)
        {
            return chainIndex == 0 ? (luminance > 0 ? 1f : 0f) : luminance;
        }

        //MIS-Faktor um die Visible- und Contribution-Chain zu vereinen
        //Siehe Paper Formel (5) / target(luminance) -> Der Faktor target(luminance) kommt aus Algorithm2 Zeile 14/15
        private float Mist(float luminance, int chainIndex)
        {
            return (1f / this.chains[chainIndex].Normalization) / (1f / chains[0].Normalization + luminance / chains[1].Normalization);
        }

        //Schritt 4: Von den Ketten Samples nehmen; Die PT/DL-Samples landen im ImageCreatorFrame-Puffer; Die VC/VM/LT-Samples landen im ImageBuffer von den Ketten
        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            FullPathSampleResult result = new FullPathSampleResult();
            result.RadianceFromRequestetPixel = new Vector3D(0, 0, 0);

            //Wähle Eye-Subpfade aus
            int pixX = x - this.frameData.PixelRange.Left;
            int pixY = y - this.frameData.PixelRange.Top;
            var eyePathPT = eyeMap.GetPathtracingPath(pixX, pixY);      //Zurückgegebener EyeSubPath zeigt auf die angegebene PixelPosition (Wird für PT und DL genutzt)
            var eyePathVC = eyeMap.GetVertexConnectionPath(pixX, pixY); //Zurückgegebener EyeSubPath zeigt auf zufälliges anderes Pixel (Wird nur für VC genutzt)

            //Wähle abwechselnd immer die Visible- oder ContributionChain aus
            int chainIndex = (y * this.frameData.PixelRange.Width + x) % 2; //0 = VisibleChain; 1 = ContributionChain
            var chain = this.chains[chainIndex];

            float largeStepProp = 0.3f;
            bool doLargeStep = chainIndex == 0 && rand.NextDouble() < largeStepProp; //Nur in der VisibleChain erfolgen LargeSteps

            chain.State.LightSampler.StartIteration(doLargeStep);
            chain.State.DirectSampler.StartIteration(true);         //DirectLighting macht immer ein LargeStep

            //Sample Light-Subpfad
            var lightPath = this.pixelData.LightPathSampler.SamplePathFromLighsource(chain.State.LightSampler);

            //Sample SplatList
            var proposed = this.splatListSampler.SampleSplatList(eyePathPT, eyePathVC, lightPath, this.eyeMap.FrameData, chain.State.DirectSampler, this.chains[0].Normalization);

            float currentTarget = Target(chain.SplatList.Luminance, chainIndex);
            float proposedTarget = Target(proposed.Luminance, chainIndex);

            if (doLargeStep)
            {
                this.chains[0].LargeStepCounter++;
                this.chains[1].LargeStepCounter++;

                if (proposedTarget != 0)
                {
                    this.chains[0].LargeStepTargetSum += Target(proposed.Luminance, 0);
                    this.chains[1].LargeStepTargetSum += Target(proposed.Luminance, 1);

                    if (proposed.Luminance / this.frameData.PixelRange.Count > this.chains[1].Normalization)
                    {
                        // Simple hack to remove fireflies from normalization estimation
                        this.chains[1].LargeStepTargetSum -= proposed.Luminance;
                        this.chains[1].LargeStepCounter--;
                    }
                }
            }

            //Pathtracing / DirectLighting
            if (proposed.PT_DL.Any())
            {
                foreach (var path in proposed.PT_DL)
                {
                    result.RadianceFromRequestetPixel += path.Radiance;
                }
                result.MainPaths.AddRange(proposed.PT_DL);
            }

            //Acceptance-Pdf
            float a = currentTarget != 0 ? Math.Min(1, proposedTarget / currentTarget) : 1;

            //Accepte den Propsed-Wert
            if (a == 1 || a > rand.NextDouble())
            {
                //Speichere alle Pfade aus dem letzten Accepted-Schritt im Ergebnis
                float mis = Mist(chain.SplatList.Luminance, chainIndex);
                foreach (var path in chain.SplatList.VC_VM_LT)
                {
                    chain.AddPixel(path.PixelPosition, path.Radiance, path.MisWeight * chain.CumulativeWeight * mis);
                }
                chain.AcceptWeightSum += chain.CumulativeWeight;

                chain.CumulativeWeight = a;
                chain.SplatList = proposed; //Merke fürs nächste Mal die aktuellen Accepted-Fullpaths
                chain.State.LightSampler.Accept();
                chain.State.DirectSampler.Accept();
            }else
            {
                //Rejecte den Proposed-Wert
                chain.CumulativeWeight += 1 - a; //Mit dem Faktor würden ich den zuletzt Accepted-Wert wichten

                //Speichere die Rejected-Pfade im Ergebnis
                if (a > 0)
                {
                    float mis = Mist(proposed.Luminance, chainIndex);
                    foreach (var path in proposed.VC_VM_LT)
                    {
                        chain.AddPixel(path.PixelPosition, path.Radiance, path.MisWeight * a * mis);
                    }
                    chain.AcceptWeightSum += a;
                }

                chain.State.LightSampler.Reject();
                chain.State.DirectSampler.Reject();
            }

            //Replica Exchange
            if (chainIndex == 1)
            {
                float swapPdf = Math.Min(1, chains[0].SplatList.Luminance / chains[1].SplatList.Luminance);
                if (swapPdf == 1 || swapPdf > rand.NextDouble())
                {
                    //Speichere die zuletzt Accpeted-SplatList von beiden Ketten im Ergebnis
                    for (int i=0;i<2;i++)
                    {
                        var c = this.chains[i];
                        if (c.CumulativeWeight > 0)
                        {
                            float mis = Mist(c.SplatList.Luminance, i);
                            foreach (var path in c.SplatList.VC_VM_LT)
                            {
                                c.AddPixel(path.PixelPosition, path.Radiance, path.MisWeight * c.CumulativeWeight * mis);
                            }
                            c.AcceptWeightSum += c.CumulativeWeight;
                        }
                    }

                    //Vertausche die Ketten
                    MarkovChain.Swap(this.chains[0], this.chains[1]);
                    this.chains[0].CumulativeWeight = this.chains[1].CumulativeWeight = 0;
                }
            }

            bool isLastPixel = x == this.frameData.PixelRange.Right - 1 && y == this.frameData.PixelRange.Bottom - 1;
            if (isLastPixel)
            {
                //Führe den letzten Splat aus
                for (int i = 0; i < 2; i++)
                {
                    var c = this.chains[i];
                    if (c.CumulativeWeight > 0)
                    {
                        float mis = Mist(c.SplatList.Luminance, i);
                        foreach (var path in c.SplatList.VC_VM_LT)
                        {
                            c.AddPixel(path.PixelPosition, path.Radiance, path.MisWeight * c.CumulativeWeight * mis);
                        }
                        c.AcceptWeightSum += c.CumulativeWeight;
                    }
                }
            }

            bool eyePathIsEmpty = eyePathPT != null && eyePathPT.Points.Length == 1; //Primärstrahl fliegt ins Leere
            result.MainPixelHitsBackground = eyePathIsEmpty;
            return result;
        }

        //Schritt 5: Summe über alle 3 Bildpuffer
        public ImageBuffer DoFramePostprocessing(int frameIterationNumber, ImageBuffer frame)
        {
            ImageBuffer sum = new ImageBuffer(frame); //frame enthält nur die PT/DL-Samples

            //Addiere noch die Ketten-Samples mit drauf
            foreach (var chain in this.chains)
            {
                chain.UpdateNormalization();
                float luminanceFactor = chain.GetLuminanceCorrectionFactor();

                if (luminanceFactor > 0)
                {
                    //Man würde ja erwarten, dass ich hier durch die PixelCount divideren muss, um somit alle ImagePlane-Schätzwerte durch die
                    //SampleCount zu dividieren um somit ein Histogram zu erhalten.
                    //Der Grund warum diese Division hier fehlt ist, weil man den Eye-Subpfad eigentlich durch die PixelSelectionPdf dividieren müsste
                    //was bedeutet, dass man mit der PixelCount multipliziert. Dieser Faktor steht also implizit da und kürzt sich mit der Histogram-
                    //Sample-Count-Division dann weg.
                    sum.AddFrame(chain.Image.GetColorScaledImage(luminanceFactor * 2));
                }
            }

            //So kann ich sehen ob die AcceptRatio immer so bei 23% liegt
            //File.AppendAllLines("AcceptRatio.txt", new string[] { chains[0].GetAcceptRatio() + "\t" + chains[1].GetAcceptRatio() });

            return sum;
        }
    }
}
