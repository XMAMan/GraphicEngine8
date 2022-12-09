using GraphicMinimal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FullPathGenerator.AnalyseHelper
{
    public class PathContributionForEachPathSpace : IEnumerable<string>
    {
        private readonly Dictionary<string, Vector3D> pathContribution = new Dictionary<string, Vector3D>();

        public float this[string pathSpace]
        {
            get
            {
                return this.pathContribution.ContainsKey(pathSpace) ? this.pathContribution[pathSpace].X : 0;
            }
        }

        public PathContributionForEachPathSpace() { }
        private PathContributionForEachPathSpace(Dictionary<string, Vector3D> pathContribution)
        {
            this.pathContribution = pathContribution;
        }

        public PathContributionForEachPathSpace(string textFile)
            :this(ParseString(File.ReadAllText(textFile)))
        {
        }
        public static PathContributionForEachPathSpace FromString(string pathSpace)
        {
            return new PathContributionForEachPathSpace(ParseString(pathSpace));
        }

        private static Dictionary<string, Vector3D> ParseString(string pathSpace)
        {
            string[] lines = pathSpace.Split(new[] { System.Environment.NewLine }, StringSplitOptions.None);
            var firstFreeLineObj = lines.Select((value, index) => new { value, index }).FirstOrDefault(x => x.value == "");
            if (firstFreeLineObj != null)
            {
                lines = lines.Skip(firstFreeLineObj.index + 1).ToArray(); //Entferne alle Zeilen über der Leerzeile (wenn Leerzeile vorhanden ist)
            }

            Dictionary<string, Vector3D> pathContribution = new Dictionary<string, Vector3D>();
            foreach (var line in lines)
            {
                if (line.StartsWith("#")) continue; //Kommentarzeile
                if (line.Contains("=") == false && lines.Contains("[") == false) continue;
                var fields = line.Split('=');
                pathContribution.Add(fields[0], Vector3D.Parse(fields[1]));
            }
            return pathContribution;
        }

        public Vector3D SumOverAllPathSpaces()
        {
            Vector3D sum = new Vector3D(0, 0, 0);
            this.pathContribution.Values.ToList().ForEach(x => sum += x);
            return sum;
        }

        public void AddEntry(string pathSpace, Vector3D pathRadiance)
        {
            if (this.pathContribution.ContainsKey(pathSpace) == false)
                this.pathContribution.Add(pathSpace, pathRadiance);
            else
                this.pathContribution[pathSpace] += pathRadiance;
        }

        public void AddEntry(FullPath path, Vector3D pathRadiance)
        {
            AddEntry(FullPathToPathSpaceConverter.ConvertPathToPathSpaceString(path), pathRadiance);
        }

        public override string ToString()
        {
            return string.Join(System.Environment.NewLine, this.pathContribution.OrderBy(x => x.Key.Length).Select(x => x.Key + "=" + x.Value.ToShortString()));
        }

        public static string CompareAll(params PathContributionForEachPathSpace[] spaces)
        {
            StringBuilder str = new StringBuilder();
            foreach (string space in spaces.SelectMany(x => x.pathContribution.Keys).Distinct())
            {
                for (int i = 0; i < spaces.Length; i++)
                {
                    if (i == 0) str.Append(space + "\t");
                    str.Append(spaces[i].pathContribution.ContainsKey(space) ? ((int)spaces[i].pathContribution[space].X).ToString() : "N/A");
                    str.Append("\t");
                }
                str.AppendLine();
            }

            return str.ToString();
        }

        public static string CompareAllWithFactor(params PathContributionForEachPathSpace[] spaces)
        {
            List<KeyValuePair<float, string>> lines = new List<KeyValuePair<float, string>>(); //Radiance von spaces[0].X; Line

            string header = "Space\t" + string.Join("\t", spaces.Select((space, index) => $"Space{index}")) + "\t" + string.Join("\t", spaces.Select((space, index) => index).Where(index => index > 0).Select(index => $"Factor{index}")) + Environment.NewLine;

            foreach (string space in spaces.SelectMany(x => x.pathContribution.Keys).Distinct())
            {
                StringBuilder str = new StringBuilder();

                //Radiance.X
                for (int i = 0; i < spaces.Length; i++)
                {
                    if (i == 0) str.Append(space + "\t");
                    str.Append(spaces[i].pathContribution.ContainsKey(space) ? spaces[i].pathContribution[space].X.ToString() : "N/A");
                    str.Append("\t");
                }

                //Faktor in Bezug auf spaces[0]
                if (spaces[0].pathContribution.ContainsKey(space))
                {
                    for (int i = 1; i < spaces.Length; i++)
                    {
                        str.Append(spaces[i].pathContribution.ContainsKey(space) ? (spaces[i].pathContribution[space].X / spaces[0].pathContribution[space].X).ToString() : "N/A");
                        str.Append("\t");
                    }
                }else
                {
                    for (int i = 1; i < spaces.Length; i++) str.Append("N/A\t");
                }

                int j = -1;
                for (j = 0; j < spaces.Length; j++)
                    if (spaces[j].pathContribution.ContainsKey(space)) break;
                lines.Add(new KeyValuePair<float, string>(spaces[j].pathContribution[space].X, str.ToString().Trim()));
            }

            return header + string.Join(Environment.NewLine, lines.OrderByDescending(x => x.Key).Where(x => x.Value.Contains("N/A") == false).Select(x => x.Value));
        }


        public string CompareWithOther(PathContributionForEachPathSpace other)
        {
            StringBuilder str = new StringBuilder();

            List<KeyValuePair<string, Vector3D>> allKeyValues = new List<KeyValuePair<string, Vector3D>>();
            allKeyValues.AddRange(this.pathContribution.ToList<KeyValuePair<string, Vector3D>>());
            allKeyValues.AddRange(other.pathContribution.ToList<KeyValuePair<string, Vector3D>>());
            var allKeys = allKeyValues.OrderByDescending(x => x.Value.X).Select(x => x.Key).Distinct().ToList();

            foreach (var keyValue in allKeys)
            {
                if (this.pathContribution.ContainsKey(keyValue) && other.pathContribution.ContainsKey(keyValue) == false)
                {
                    str.AppendLine(keyValue + "=" + this.pathContribution[keyValue].ToShortString() + " <-> Missing");
                }
                else
                if(this.pathContribution.ContainsKey(keyValue) == false && other.pathContribution.ContainsKey(keyValue))
                {
                    str.AppendLine(keyValue + "=" + "Missing <-> " + other.pathContribution[keyValue].ToShortString());
                }else
                {
                    var v1 = this.pathContribution[keyValue];
                    var v2 = other.pathContribution[keyValue];

                    Vector3D factor = v1.Max() == 0 ? new Vector3D(0, 0, 0) : new Vector3D(v2.X / v1.X, v2.Y / v1.Y, v2.Z / v1.Z); 

                    str.AppendLine(keyValue + "=" + v1.ToShortString() + " <-> " + v2.ToShortString() + " Factor=" + factor.ToShortString());
                }
            }

            return str.ToString();
        }

        //Gibt all die Pfade zurück, welche in beiden PathSpace-Auflistungen vorhanden sind
        public string GetCompareArray(PathContributionForEachPathSpace expected)
        {
            return "new string[] {" + string.Join(",", this.pathContribution.Where(x => expected.pathContribution.ContainsKey(x.Key)).Select(x => "\"" + x.Key + "\"")) + "}";
        }

        //Gibt all die Pfade zurück, welche nicht in expected auftauchen
        public string GetAllExceptExcludetArray(PathContributionForEachPathSpace expected)
        {
            return "new string[] {" + string.Join(",", expected.pathContribution.Where(x => this.pathContribution.ContainsKey(x.Key) == false).Select(x => "\"" + x.Key + "\"")) + "}";
        }

        public string CompareOnlyProvidedPathsWithOther(PathContributionForEachPathSpace other, float maxError, string[] useOnlyThisForCompare)
        {
            return GetCompareError(other, maxError, null, useOnlyThisForCompare);
        }

        public string CompareAllPathsWithOther(PathContributionForEachPathSpace other, float maxError)
        {
            return GetCompareError(other, maxError, null, null);
        }

        public string CompareAllExceptExcludetPathsWithOther(PathContributionForEachPathSpace other, float maxError, string[] useNotForCompare)
        {
            return GetCompareError(other, maxError, useNotForCompare, null);
        }

        private string GetCompareError(PathContributionForEachPathSpace other, float maxError, string[] useNotForCompare, string[] useOnlyThisForCompare)
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
                        if (Difference(keyValue.Value, other.pathContribution[keyValue.Key]) > maxError)
                            str.AppendLine(keyValue.Key + "=" + keyValue.Value + " <-> " + other.pathContribution[keyValue.Key]);
                    }
                }
                    
            }

            return str.ToString();
        }

        private float Difference(Vector3D v1, Vector3D v2)
        {
            Vector3D v = new Vector3D(Math.Abs(v1.X - v2.X), Math.Abs(v1.Y - v2.Y), Math.Abs(v1.Z - v2.Z));
            return Math.Max(Math.Max(v.X, v.Y), v.Z);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return this.pathContribution.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.pathContribution.Keys.GetEnumerator();
        }
    }
}
