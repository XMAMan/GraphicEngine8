using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;
using Photonusmap;
using RaytracingColorEstimator;
using RaytracingRandom;
using SubpathGenerator;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RaytracingMethods.McVcm
{
    class FramePrepareHelper
    {
        private readonly RaytracingFrame3DData frameData;
        private readonly PixelRadianceData pixelData;
        private readonly SplatListSampler splatListSampler;

        private readonly IRandom rand;
        
        public FramePrepareHelper(RaytracingFrame3DData frameData, PixelRadianceData pixelData, SplatListSampler splatListSampler, IRandom rand)
        {
            this.frameData = frameData;
            this.pixelData = pixelData;
            this.splatListSampler = splatListSampler;

            this.rand = rand;            
        }

        public EyeMap CreateEyeMap()
        {
            var eyeMap = new EyeSubPath[this.frameData.PixelRange.Width, this.frameData.PixelRange.Height];

            List<EyeSubPath> eyePaths = new List<EyeSubPath>();
            for (int x = this.frameData.PixelRange.Left; x < this.frameData.PixelRange.Right; x++)
                for (int y = this.frameData.PixelRange.Top; y < this.frameData.PixelRange.Bottom; y++)
                {
                    if (this.frameData.StopTrigger.IsCancellationRequested) return null;

                    var eyePath = new EyeSubPath(this.pixelData.EyePathSampler.SamplePathFromCamera(x, y, this.rand), new Vector2D(x, y));

                    foreach (var p in eyePath.Points)
                    {
                        p.AssociatedPath = eyePath;
                    }

                    eyePaths.Add(eyePath);
                    eyeMap[x - this.frameData.PixelRange.Left, y - this.frameData.PixelRange.Top] = eyePath;
                }

            var photonmap = new Photonmaps() { GlobalSurfacePhotonmap = new PhotonMap(eyePaths.Cast<SubPath>().ToList(), (int)this.frameData.PixelRange.Count, (text, zahl) => { }, 1, int.MaxValue) };

            return EyeMap.Create(eyeMap, new FullPathFrameData() { PhotonMaps = photonmap }, this.rand);
        }

        public ChainSeed[] CreateSeedValues(EyeMap eyeMap)
        {
            bool adaptivity = true; //Soll die SmallStep-Size nach jeden Accept/Reject angepasst werden?

            List<ChainSeed> states = new List<ChainSeed>();
            List<double> luminance = new List<double>(); //Schätzwerte für die Bildebene

            for (int i = 0; i < this.frameData.GlobalObjektPropertys.MetropolisBootstrapCount; i++)
            {
                var lightSampler = new MLTSampler(this.rand, adaptivity);
                var directSampler = new MLTSampler(this.rand, adaptivity);

                //Wähle zufälligen Pixel aus
                int pixX = this.rand.Next(this.frameData.PixelRange.Width);
                int pixY = this.rand.Next(this.frameData.PixelRange.Height);

                //Wähle Eye-Subpfade aus
                var eyePathPT = eyeMap.GetPathtracingPath(pixX, pixY);      //Zurückgegebener EyeSubPath zeigt auf die angegebene PixelPosition (Wird für PT und DL genutzt)
                var eyePathVC = eyeMap.GetVertexConnectionPath(pixX, pixY); //Zurückgegebener EyeSubPath zeigt auf zufälliges anderes Pixel (Wird nur für VC genutzt)

                //Sample Light-Subpfad
                lightSampler.StartIteration(true);
                var lightPath = this.pixelData.LightPathSampler.SamplePathFromLighsource(lightSampler);
                lightSampler.Accept();

                //Sample SplatList
                directSampler.StartIteration(true);
                var splatList = this.splatListSampler.SampleSplatList(eyePathPT, eyePathVC, lightPath, eyeMap.FrameData, directSampler, 1);
                directSampler.Accept();

                if (splatList.Luminance > 0)
                {
                    states.Add(new ChainSeed(new ChainState(lightSampler, directSampler), splatList));
                    luminance.Add(splatList.Luminance);
                }                
            }

            if (luminance.Any() == false)
            {
                return new ChainSeed[]
                {
                    new ChainSeed(new ChainState(new MLTSampler(rand, adaptivity), new MLTSampler(rand, adaptivity)), new SplatList()),
                    new ChainSeed(new ChainState(new MLTSampler(rand, adaptivity), new MLTSampler(rand, adaptivity)), new SplatList())
                };
            }

            PdfWithTableSampler bootstrap = PdfWithTableSampler.CreateFromUnnormalisizedFunctionArray(luminance.ToArray());

            var visibleChainSeed = states[this.rand.Next(states.Count)];                            //Wähle gleichmäßig zufällig den Visible-Seed aus
            var contributionChainSeed = states[bootstrap.SampleDiscrete(this.rand.NextDouble())];   //Wähle laut Luminance den Contribution-Seed aus

            return new ChainSeed[] { visibleChainSeed, contributionChainSeed };
        }
    }
}
