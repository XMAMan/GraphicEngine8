using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullPathGenerator.AnalyseHelper
{
    public class PathContributionForEachPathLength
    {
        private readonly Dictionary<int, float> pathContribution = new Dictionary<int, float>();

        public PathContributionForEachPathLength(PathContributionForEachPathSpace pathSpace)
        {
            foreach (var space in pathSpace)
            {
                int length = space.Replace(" ", "").Length;
                if (this.pathContribution.ContainsKey(length) == false) this.pathContribution.Add(length, 0);
                this.pathContribution[length] += pathSpace[space];
            }
        }

        public override string ToString()
        {
            return string.Join(System.Environment.NewLine, this.pathContribution.OrderBy(x => x.Key).Select(x => x.Key + "=" + x.Value.ToString("G9")));
        }

        public string CompareWithOther(PathContributionForEachPathLength other)
        {
            StringBuilder str = new StringBuilder();

            List<KeyValuePair<int, float>> allKeyValues = new List<KeyValuePair<int, float>>();
            allKeyValues.AddRange(this.pathContribution.ToList<KeyValuePair<int, float>>());
            allKeyValues.AddRange(other.pathContribution.ToList<KeyValuePair<int, float>>());
            var allKeys = allKeyValues.OrderByDescending(x => x.Value).Select(x => x.Key).Distinct().ToList();

            foreach (var keyValue in allKeys)
            {
                if (this.pathContribution.ContainsKey(keyValue) && other.pathContribution.ContainsKey(keyValue) == false)
                {
                    str.AppendLine(keyValue + "=" + this.pathContribution[keyValue].ToString("G9") + " <-> Missing");
                }
                else
                if (this.pathContribution.ContainsKey(keyValue) == false && other.pathContribution.ContainsKey(keyValue))
                {
                    str.AppendLine(keyValue + "=" + "Missing <-> " + other.pathContribution[keyValue].ToString("G9"));
                }
                else
                {
                    var v1 = this.pathContribution[keyValue];
                    var v2 = other.pathContribution[keyValue];

                    float factor = v1 == 0 ? 0 : v2 / v1;

                    str.AppendLine(keyValue + "=" + v1.ToString("G9") + " <-> " + v2.ToString("G9") + " Factor=" + factor.ToString("G9"));
                }
            }

            return str.ToString();
        }

        public string CompareAllPathsWithOther(PathContributionForEachPathLength other, float maxError)
        {
            return GetCompareError(other, maxError, null, null);
        }

        private string GetCompareError(PathContributionForEachPathLength other, float maxError, int[] useNotForCompare, int[] useOnlyThisForCompare)
        {
            StringBuilder str = new StringBuilder();
            foreach (var keyValue in this.pathContribution)
            {
                if (useOnlyThisForCompare != null && useOnlyThisForCompare.Contains(keyValue.Key) == false) continue;

                if (useNotForCompare == null || useNotForCompare.Contains(keyValue.Key) == false)
                {
                    if (other.pathContribution.ContainsKey(keyValue.Key) == false)
                    {
                        str.AppendLine(keyValue.Key + "=" + keyValue.Value + " <-> Missing");
                    }
                    else
                    {
                        if (Math.Abs(keyValue.Value - other.pathContribution[keyValue.Key]) > maxError)
                            str.AppendLine(keyValue.Key + "=" + keyValue.Value + " <-> " + other.pathContribution[keyValue.Key]);
                    }
                }

            }

            return str.ToString();
        }
    }
}
