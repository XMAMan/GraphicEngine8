using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ToolsTest
{
    static class StringRegexHelper
    {
        public static string ReplaceInteger(this string stringWithInteger, string regExWithIntGrep, Func<int, int> intConvert)
        {
            return ReplaceRegExGroups(stringWithInteger, regExWithIntGrep, (x) => intConvert(Convert.ToInt32(x)).ToString());
        }

        public static string ReplaceFloat(this string stringWithInteger, string regExWithIntGrep, Func<float, float> intConvert)
        {
            return ReplaceRegExGroups(stringWithInteger, regExWithIntGrep, (x) => intConvert(Convert.ToSingle(x)).ToString());
        }

        public static string ReplaceRegExGroups(this string stringWithGroups, string regEx, Func<string, string> groupConvert)
        {
            var matches = new Regex(regEx).Matches(stringWithGroups);
            if (matches.Count == 0) return stringWithGroups;

            List<Group> groups = new List<Group>(); //Die Menge aller ()-Treffer
            foreach (Match match in matches) //Jedes match ist String-Abschnitt, der auf regEx passt
            {
                for (int i=1;i<match.Groups.Count;i++) //Group[0] = Ganzer String; Index > 0 einzelne Klammern
                    groups.Add(match.Groups[i]); //Jede Group ist ein ()-Paar innerhalb des regEx-Strings
            }

            List<string> elements = new List<string>();
            elements.Add(stringWithGroups.Substring(0, groups[0].Index)); //0 bis erster Match
            for (int i = 0; i < groups.Count - 1; i++)
            {
                string replacedGroup = groupConvert(groups[i].Value);
                elements.Add(replacedGroup);
                int start = groups[i].Index + groups[i].Value.Length;
                int end = groups[i + 1].Index;
                string betweeniAndNext = stringWithGroups.Substring(start, end - start);
                elements.Add(betweeniAndNext);
            }

            var l = groups[groups.Count - 1];
            string lastGroup = groupConvert(l.Value);
            elements.Add(lastGroup);
            elements.Add(stringWithGroups.Substring(l.Index + l.Value.Length));

            return string.Join("", elements);
        }
    }
}
