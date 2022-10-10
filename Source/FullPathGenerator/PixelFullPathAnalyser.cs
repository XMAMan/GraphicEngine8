using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphicMinimal;

namespace FullPathGenerator
{
    //Samelt Menge von Fullpaths und stellt sie dann übersichtlich dar, damit ich verstehe, aus welchen Fullpaths sich eine einzelne Pixelfarbe zusammen setzt
    public class PixelFullPathAnalyser
    {
        private readonly Vector2D pix;
        private readonly List<FullPath> paths = new List<FullPath>();
        public PixelFullPathAnalyser(int pixX, int pixY)
        {
            this.pix = new Vector2D(pixX, pixY);
        }

        public void AddSampleResult(FullPathSampleResult result)
        {
            result.MainPaths.ForEach(x => this.AddMainPath(x));
            result.LighttracingPaths.ForEach(x => this.TryToAddLightPath(x));
        }

        public void AddMainPath(FullPath path)
        {
            paths.Add(path);
        }

        public void TryToAddLightPath(FullPath path)
        {
            if (path.PixelPosition.X >= this.pix.X && path.PixelPosition.X <= this.pix.X + 1 &&
                path.PixelPosition.Y >= this.pix.Y && path.PixelPosition.Y <= this.pix.Y + 1)
            {
                paths.Add(path);
            }
        }

        public string GetOverview(int sampleCount)
        {
            //Laufende Summe
            /*float sum = 0;
            StringBuilder str = new StringBuilder();
            foreach (var path in this.paths.OrderByDescending(x => x.Radiance.Z))
            {
                sum += path.Radiance.Z;
                str.AppendLine(path.SamplingMethod + "\t" + path.GetPathSpaceString() + "\tRadiance=" + path.Radiance.Z + "\tMis=" + path.MisWeight + "\tSum=" + sum);
                str.AppendLine(path.GetLocationAndPathWeightInformation());
                str.AppendLine();
            }
            return str.ToString();*/

            Vector3D radianceSum = this.paths.Select(x => x.Radiance).Sum();
            Vector3D radianceWithGammaAndClamping = (radianceSum / sampleCount).Pow(1 / 2.2).Clamp(0, 1);

            string allPaths = string.Join(System.Environment.NewLine, this.paths.Select(x => Header(x))) + System.Environment.NewLine;
            allPaths += "PixelColor with Gamma and Clampling=" + radianceWithGammaAndClamping + "\t" + "RGB=" + (radianceWithGammaAndClamping * 255).ToInt().ToShortString() + System.Environment.NewLine;
            allPaths += $"Radiance-Sum({this.paths.Count})=" + radianceSum + System.Environment.NewLine;
            allPaths += string.Join(Environment.NewLine, this.paths.GroupBy(x => x.SamplingMethod).Select(x => x.Key + $"-Sum({x.Count()})=" + x.Select(y => y.Radiance).Sum())) + Environment.NewLine;
            //allPaths += string.Join(Environment.NewLine, this.paths.Where(x => x.SamplingMethod == SamplingMethod.Pathtracing && x.Points.Last().Point.IsLocatedOnInfinityAwayLightSource == false).Select(x => Header(x) + Environment.NewLine+ x.GetLocationAndPathWeightInformation())) + Environment.NewLine;
            return allPaths;
        }

        private static string Header(FullPath x)
        {
            //return x.SamplingMethod + "\t" + x.GetPathSpaceString() + "\tRadiance=" + x.Radiance.ToShortString() + "\tPathContribution=" + x.PathContribution.ToShortString() + "\tMis=" + x.MisWeight;
            return x.SamplingMethod + "\t" + x.GetPathSpaceString() + "\t" + x.Radiance.X + "\t" + x.PathContribution.X + "\t" + x.MisWeight + "\t" + (x.Points.Last().Point.IsLocatedOnInfinityAwayLightSource ? "Environment" : "AreaLight" + "\t" + x.PathPdfA);
        }
    }
}
