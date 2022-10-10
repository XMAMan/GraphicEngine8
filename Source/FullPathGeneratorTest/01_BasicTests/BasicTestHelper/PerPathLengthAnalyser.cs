using BitmapHelper;
using FullPathGenerator;
using PdfHistogram;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullPathGeneratorTest.BasicTests.BasicTestHelper
{
    class PerPathLengthAnalyser
    {
        public class CombinationData
        {
            public SamplingMethod[] Sampler;
        }
        public class CombinationResult
        {
            public string Name;
            public string ShortName;
            public Dictionary<int, PerPathLengthData> DataPerPathLength;
        }
        public class PerPathLengthData
        {
            public double Radiance = 0;
            public double MinRadiance = double.MaxValue;
            public double MaxRadiance = double.MinValue;
            public double AvgContribution = 0;
            public double MinContribution = double.MaxValue;
            public double MaxContribution = double.MinValue;
            public double AvgPdfA = 0;
            public double MinPdfA = double.MaxValue;
            public double MaxPdfA = double.MinValue;
            public double AvgMis = 0;
            public int PathCount = 0;
            public double AvgRadiance = 0; //AvgContribution * AvgMis * PathCount
            public Dictionary<IFullPathSamplingMethod, double> RadiancePerSampler = new Dictionary<IFullPathSamplingMethod, double>();
            public Dictionary<IFullPathSamplingMethod, double> MisPerSampler = new Dictionary<IFullPathSamplingMethod, double>();
            public Dictionary<IFullPathSamplingMethod, int> PathCountPerSampler = new Dictionary<IFullPathSamplingMethod, int>();
            public MinMaxPlotter PdfPlotter = new MinMaxPlotter(1000);
            public MinMaxPlotter ContributionPlotter = new MinMaxPlotter(1000);
            public MinMaxPlotter RadiancePlotter = new MinMaxPlotter(1000);
            public static string ToString(Dictionary<IFullPathSamplingMethod, double> radiancePerSampler)
            {
                return string.Join("|", radiancePerSampler.Select(x => new string(x.Key.Name.ToString().Where(c => c >= 'A' && c <= 'Z').ToArray()) + "=" + (int)(x.Value * 100)));
            }

            public static string ToStringD(Dictionary<IFullPathSamplingMethod, double> misPerSampler)
            {
                return string.Join("|", misPerSampler.Select(x => new string(x.Key.Name.ToString().Where(c => c >= 'A' && c <= 'Z').ToArray()) + "=" + (x.Value.ToString("0.###"))));
            }

            public void AddPath(FullPath path, double misWeight, double indexPercent)
            {
                double radiance = path.PathContribution.X * misWeight;
                this.Radiance += radiance;
                this.MinRadiance = Math.Min(this.MinRadiance, radiance);
                this.MaxRadiance = Math.Max(this.MaxRadiance, radiance);
                this.AvgContribution += path.PathContribution.X;
                this.MinContribution = Math.Min(this.MinContribution, path.PathContribution.X);
                this.MaxContribution = Math.Max(this.MaxContribution, path.PathContribution.X);
                this.AvgPdfA += path.PathPdfA;
                this.MinPdfA = Math.Min(this.MinPdfA, path.PathPdfA);
                this.MaxPdfA = Math.Max(this.MaxPdfA, path.PathPdfA);
                this.AvgMis += misWeight;
                this.PathCount++;
                if (this.RadiancePerSampler.ContainsKey(path.Sampler) == false) this.RadiancePerSampler.Add(path.Sampler, 0);
                this.RadiancePerSampler[path.Sampler] += radiance;
                if (this.MisPerSampler.ContainsKey(path.Sampler) == false) this.MisPerSampler.Add(path.Sampler, 0);
                this.MisPerSampler[path.Sampler] += misWeight;
                if (this.PathCountPerSampler.ContainsKey(path.Sampler) == false) this.PathCountPerSampler.Add(path.Sampler, 0);
                this.PathCountPerSampler[path.Sampler]++;

                this.PdfPlotter.AddSample(indexPercent, path.PathPdfA);
                this.ContributionPlotter.AddSample(indexPercent, path.PathContribution.X);
                this.RadiancePlotter.AddSample(indexPercent, radiance);
            }

            public PerPathLengthData GetData(int sampleCount)
            {
                return new PerPathLengthData()
                {
                    Radiance = this.Radiance / sampleCount,
                    MinRadiance = this.MinRadiance,
                    MaxRadiance = this.MaxRadiance,
                    AvgContribution = this.AvgContribution / Math.Max(1, this.PathCount),
                    MinContribution = this.MinContribution,
                    MaxContribution = this.MaxContribution,
                    AvgPdfA = this.AvgPdfA / Math.Max(1, this.PathCount),
                    MinPdfA = this.MinPdfA,
                    MaxPdfA = this.MaxPdfA,
                    AvgMis = this.AvgMis / Math.Max(1, this.PathCount),
                    PathCount = this.PathCount,
                    AvgRadiance = this.Radiance / Math.Max(1, this.PathCount),
                    RadiancePerSampler = this.RadiancePerSampler.ToDictionary(v => v.Key, v => v.Value / this.Radiance),
                    MisPerSampler = this.MisPerSampler.ToDictionary(v => v.Key, v => v.Value / Math.Max(1, this.PathCountPerSampler[v.Key])),
                    PathCountPerSampler = this.PathCountPerSampler,
                    PdfPlotter = this.PdfPlotter,
                    ContributionPlotter = this.ContributionPlotter,
                    RadiancePlotter = this.RadiancePlotter
                };
            }
        }

        public class Combination
        {
            public string Name { get; private set; }
            public string ShortName { get; private set; }
            private Dictionary<int, PerPathLengthData> DataPerPathLength = new Dictionary<int, PerPathLengthData>();

            private IFullPathSamplingMethod[] fullSampler;
            private Dictionary<int, double> primarySampleRadiance = new Dictionary<int, double>();
            private List<double> radiances = new List<double>();
            private int sampleCount;
            public Combination(IFullPathSamplingMethod[] samplingMethods, CombinationData data, int sampleCount)
            {
                this.fullSampler = data.Sampler.Select(x => samplingMethods.First(y => y.Name == x)).ToArray();
                for (int i = 0; i < 100; i++) this.DataPerPathLength.Add(i, new PerPathLengthData());
                this.radiances.Add(0);
                this.Name = string.Join("+", this.fullSampler.Select(x => x.Name));
                this.ShortName = string.Join("_", this.fullSampler.Select(x => new string(x.Name.ToString().Where(c => c >= 'A' && c <= 'Z').ToArray())));
                this.sampleCount = sampleCount;
            }


            public void StartNewPrimarySample()
            {
                this.primarySampleRadiance = new Dictionary<int, double>();
                for (int i = 0; i <= 100; i++) this.primarySampleRadiance.Add(i, 0);
            }

            public void EndPrimarySample(int sampleIndex)
            {
                double radiance = this.primarySampleRadiance[20]; //Ich untersuche Pfadlänge 20
                radiances.Add(((radiances.Last() * sampleIndex) + radiance) / (sampleIndex + 1));
            }

            public double[] GetRadianceEstimates()
            {
                return this.radiances.ToArray();
            }

            public void ProcessPath(int sampleIndex, FullPath path)
            {
                if (this.fullSampler.Any(x => x.Name == path.SamplingMethod))
                {
                    double mis = MisWeight(path);
                    this.DataPerPathLength[path.PathLength].AddPath(path, mis, sampleIndex / (double)this.sampleCount);
                    this.primarySampleRadiance[path.PathLength] += path.PathContribution.X * mis;
                }
            }

            public CombinationResult GetResult(int sampleCount, int maxPathLength)
            {
                Dictionary<int, PerPathLengthData> dataPerPathLength = new Dictionary<int, PerPathLengthData>();
                foreach (var pair in this.DataPerPathLength)
                {
                    if (pair.Key <= maxPathLength)
                    {
                        dataPerPathLength.Add(pair.Key, pair.Value.GetData(sampleCount));
                    }
                }

                return new CombinationResult()
                {
                    Name = this.Name,
                    ShortName = this.ShortName,
                    DataPerPathLength = dataPerPathLength
                };
            }

            private double MisWeight(FullPath path)
            {
                double sum = 0;

                foreach (var sampler in this.fullSampler)
                {
                    sum += sampler.GetPathPdfAForAGivenPath(path, null);
                    //sum += sampler.SampleCountForGivenPath(path); //Ohne MIS
                }

                return path.PathPdfA / sum;
                //return 1.0 / sum; //Ohne MIS
            }
            
        }
        /*class PathSimple
        {
            public double PdfA;
            public double Contribution;
            public double Mis;
        }*/
        public static string GetDataPerPathLengthFromMultipleCombinations(BoxTestScene testSzene, IFullPathSamplingMethod[] samplingMethods, int sampleCount, CombinationData[] combinations)
        {
            Combination[] combinations1 = combinations.Select(x => new Combination(samplingMethods, x, sampleCount)).ToArray();

            for (int i = 0; i < sampleCount; i++)
            {
                foreach (var combination in combinations1) combination.StartNewPrimarySample();

                var eyePath = testSzene.EyePathSampler.SamplePathFromCamera(testSzene.PixX, testSzene.PixY, testSzene.rand);
                var lightPath = testSzene.LightPathSampler.SamplePathFromLighsource(testSzene.rand);

                foreach (var sampleMethod in samplingMethods)
                {
                    var paths = sampleMethod.SampleFullPaths(eyePath, lightPath, null, testSzene.rand);
                    foreach (var path in paths)
                    {
                        if (path.PixelPosition == null || (path.PixelPosition.X >= testSzene.PixX && path.PixelPosition.X <= testSzene.PixX + 1 && path.PixelPosition.Y >= testSzene.PixY && path.PixelPosition.Y <= testSzene.PixY + 1))
                        {
                            foreach (var combination in combinations1)
                            {
                                combination.ProcessPath(i, path);
                            }
                        }
                    }
                }

                foreach (var combination in combinations1) combination.EndPrimarySample(i);
            }
            
            var data = combinations1.Select(x => x.GetResult(sampleCount, testSzene.MaxPathLength)).ToArray();

            StringBuilder str = new StringBuilder();
            str.AppendLine("Radiance\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => x.Value.Radiance)))));
            str.AppendLine("MinRadiance\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => x.Value.MinRadiance)))));
            str.AppendLine("MaxRadiance\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => x.Value.MaxRadiance)))));
            str.AppendLine("AvgContribution\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => x.Value.AvgContribution)))));
            str.AppendLine("MinContribution\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => x.Value.MinContribution)))));
            str.AppendLine("MaxContribution\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => x.Value.MaxContribution)))));
            str.AppendLine("AvgPdfA\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => x.Value.AvgPdfA)))));
            str.AppendLine("MinPdfA\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => x.Value.MinPdfA)))));
            str.AppendLine("MaxPdfA\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => x.Value.MaxPdfA)))));
            str.AppendLine("AvgMis\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => x.Value.AvgMis)))));
            str.AppendLine("PathCount\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => x.Value.PathCount)))));
            str.AppendLine("AvgRadiance\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => x.Value.AvgRadiance)))));
            str.AppendLine("RadiancePerSampler\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => PerPathLengthData.ToString(x.Value.RadiancePerSampler))))));
            str.AppendLine("MisPerSampler\n" + string.Join("\n", data.Select(d => d.Name + "\t" + string.Join("\t", d.DataPerPathLength.Select(x => PerPathLengthData.ToStringD(x.Value.MisPerSampler))))));

            return str.ToString();
        }
    }
}
