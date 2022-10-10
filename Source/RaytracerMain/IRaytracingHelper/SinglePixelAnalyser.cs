using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using GraphicGlobal;
using GraphicMinimal;
using ImageCreator;
using RaytracingColorEstimator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RaytracerMain
{
    //Erzeugt für ein einzelnes Pixel Farbwerte / Fullpaths
    class SinglePixelAnalyser
    {
        private IPixelEstimator colorEstimator;
        private ImagePixelRange pixelRange;
        private int pixX;
        private int pixY;
        private int sampleCount;

        public SinglePixelAnalyser(IPixelEstimator colorEstimator, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            this.colorEstimator = colorEstimator;
            this.pixelRange = pixelRange;
            this.pixX = pixX;
            this.pixY = pixY;
            this.sampleCount = sampleCount;
        }

        public Vector3D GetColorFromSinglePixel(BackgroundColor backgroundColor)
        {
            Vector3D sum = new Vector3D(0, 0, 0);
            

            GetDataForSinglePixel(
                (result) =>
                {
                    Vector3D color = result.MainPixelHitsBackground == false ? result.RadianceFromRequestetPixel : backgroundColor.GetColor(this.pixelRange.XStart + this.pixX, this.pixelRange.YStart + this.pixY);
                    sum += color;
                },
                (lightPahts) =>
                {
                    lightPahts
                    .Where(path => (int)path.PixelPosition.X == this.pixelRange.XStart + this.pixX && (int)path.PixelPosition.Y == this.pixelRange.YStart + this.pixY)
                    .ToList()
                    .ForEach(path => sum += path.Radiance);

                });

            return sum / this.sampleCount;
        }

        public List<Vector3D> GetNPixelSamples(BackgroundColor backgroundColor)
        {
            List<Vector3D> list = new List<Vector3D>();
            //BackgroundColor backgroundColor = new BackgroundColor(data.GlobalObjektPropertys.BackgroundImage, data.GlobalObjektPropertys.BackgroundColorFactor, imageWidth, imageHeight);

            GetDataForSinglePixel(
                (result) =>
                {
                    Vector3D color = result.MainPixelHitsBackground == false ? result.RadianceFromRequestetPixel : backgroundColor.GetColor(this.pixelRange.XStart + this.pixX, this.pixelRange.YStart + pixY);
                    list.Add(color);
                },
                (lightPahts) =>
                {
                    lightPahts
                    .Where(path => (int)path.PixelPosition.X == this.pixelRange.XStart + this.pixX && (int)path.PixelPosition.Y == this.pixelRange.YStart + this.pixY)
                    .ToList()
                    .ForEach(path => list.Add(path.Radiance));

                });

            return list;
        }

        public string GetFullPathsFromSinglePixel()
        {
            PixelFullPathAnalyser analyser = new PixelFullPathAnalyser(this.pixelRange.XStart + this.pixX, this.pixelRange.YStart + this.pixY);

            GetDataForSinglePixel(
                (result) =>
                {
                    result.MainPaths.ForEach(P => analyser.AddMainPath(P));
                },
                (lightPahts) =>
                {
                    lightPahts.ForEach(p => analyser.TryToAddLightPath(p));

                });

            return analyser.GetOverview(this.sampleCount);
        }

        class SamplerData
        {
            public Vector3D RadianceSum = new Vector3D(0, 0, 0);
            public double MinPdfA = double.MaxValue;
            public double MaxPdfA = double.MinValue;

            public void AddPathData(FullPath path, Vector3D radiance)
            {
                this.RadianceSum += radiance;
                this.MinPdfA = Math.Min(this.MinPdfA, path.PathPdfA);
                this.MaxPdfA = Math.Max(this.MaxPdfA, path.PathPdfA);
            }
        }
        public string GetPathContributionsForSinglePixel()
        {
            PathContributionForEachPathSpace pathContribution = new PathContributionForEachPathSpace();
            Vector3D radianceSum = new Vector3D(0, 0, 0);
            Dictionary<SamplingMethod, SamplerData> sumPerFullPathSampler = new Dictionary<SamplingMethod, SamplerData>();

            void handlePath(FullPath path)
            {
                Vector3D radiance = path.PathContribution * path.MisWeight / this.sampleCount;//Mit MIS
                                                                                         //Vector3D radiance = path.PathContribution / path.Sampler.SampleCountForGivenPath(path) / sampleCount;//Ohne MIS (Wenn ich nur ein einzelnes Verfahren untersuchen will)
                pathContribution.AddEntry(path, radiance);
                radianceSum += radiance;
                if (sumPerFullPathSampler.ContainsKey(path.SamplingMethod) == false) sumPerFullPathSampler.Add(path.SamplingMethod, new SamplerData());
                sumPerFullPathSampler[path.SamplingMethod].AddPathData(path, radiance);
            }

            GetDataForSinglePixel(
                (result) =>
                {
                    foreach (var path in result.MainPaths)
                    {
                        handlePath(path);
                    }
                },
                (lightPahts) =>
                {
                    foreach (var path in lightPahts)
                    {
                        if (path.PixelPosition == null || ((int)path.PixelPosition.X == this.pixelRange.XStart + this.pixX && (int)path.PixelPosition.Y == this.pixelRange.YStart + this.pixY))
                        {
                            handlePath(path);
                        }
                    }

                });

            string radianceWithGamma = (radianceSum.Pow(1 / 2.2) * 255).ToInt().ToShortString();
            string percentPerFullPathSampler = string.Join(Environment.NewLine, sumPerFullPathSampler.Select(x => $"\t{x.Key}={(int)(x.Value.RadianceSum.X / radianceSum.X * 100)}% MinPdfA={x.Value.MinPdfA} MaxPdfA={x.Value.MaxPdfA}"));
            string pathSpaces = pathContribution.ToString();

            return "PixelColor=" + radianceWithGamma + Environment.NewLine +
                   "Samples=" + this.sampleCount + Environment.NewLine +
                   "PercentPerFullPathSampler" + Environment.NewLine + percentPerFullPathSampler + Environment.NewLine + Environment.NewLine +
                   pathSpaces;
        }

        private void GetDataForSinglePixel(Action<FullPathSampleResult> dataForRequestedPixelAvailable, Action<List<FullPath>> lightracingPathsAvailable)
        {
            //IRandom rand = new Rand((pixY - pixelRange.YStart) * pixelRange.Width + (pixX - pixelRange.XStart));
            IRandom rand = new Rand(pixY * this.pixelRange.Width + this.pixX);
            //rand = new Rand("AAEAAAD/////AQAAAAAAAAAEAQAAAA1TeXN0ZW0uUmFuZG9tAwAAAAVpbmV4dAZpbmV4dHAJU2VlZEFycmF5AAAHCAgIBQAAABoAAAAJAgAAAA8CAAAAOAAAAAgAAAAAohL1RfXH7SOv/qBUtbedEJiJKWFYwjY+TTIzGZ/mk33xlhdGLeGedx9a71FKuG4tgxrNIsuZJk4PRTl+kHDKXmrgfQ/xhZ8FbF7WKDXOE3knnfIKsCI5WbthU0mzKnQvbtNcdMK9zh7E3Ggqbwp5UxgdNF+j5YsVRHkgPF1JZ1Ia4ShI7xOqOPbpVVOrJ71TVyxRGpLssBI3qe8+aqcLGPawDEgHcjJ7updAV9MVuHtaSTQ281fnZkjRPAeO1SQPnkNqEqWYO2ikpWxRkx8fQCu+51qDX/R+9IrdPws="); //Diese Zeile hier


            if (this.colorEstimator.CreatesLigthPaths == false)
            {
                for (int i = 0; i < this.sampleCount; i++)
                {
                    //string randomObjectBase64Coded = rand.ToBase64String(); //Und diese Zeile hier für Analyse von einzelnen Sampel

                    var result = this.colorEstimator.GetFullPathSampleResult(this.pixelRange.XStart + this.pixX, this.pixelRange.YStart + this.pixY, rand);
                    dataForRequestedPixelAvailable(result);
                }
            }
            else
            {
                for (int i = 0; i < this.sampleCount; i++)
                {
                    if (this.colorEstimator is IFrameEstimator)
                        (this.colorEstimator as IFrameEstimator).DoFramePrepareStep(this.pixelRange, i, rand);

                    for (int x = 0; x < this.pixelRange.Width; x++)
                        for (int y = 0; y < this.pixelRange.Height; y++)
                        {
                            //rand = new Rand(x + y * pixelRange.Width);

                            var result = this.colorEstimator.GetFullPathSampleResult(this.pixelRange.XStart + x, this.pixelRange.YStart + y, rand);
                            lightracingPathsAvailable(result.LighttracingPaths);

                            if (x == this.pixX && y == this.pixY)
                            {
                                dataForRequestedPixelAvailable(result);
                            }
                        }
                }
            }
        }
    }
}
