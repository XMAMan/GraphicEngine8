using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools.Tools
{
    static class TimeSpanExtensions
    {
        public static string ToNiceString(this TimeSpan span)
        {
            List<KeyValuePair<int, string[]>> list = new List<KeyValuePair<int, string[]>>()
            {
                new KeyValuePair<int, string[]>(span.Days, new string[]{"Day","Days"}),
                new KeyValuePair<int, string[]>(span.Hours, new string[]{"Hour", "Hours"}),
                new KeyValuePair<int, string[]>(span.Minutes, new string[]{"Minute", "Minutes"}),
                new KeyValuePair<int, string[]>(span.Seconds, new string[]{"Second","Seconds"}),
            };

            try
            {
                int i1 = list.FindIndex(x => x.Key > 0);
                int i2 = list.FindIndex(i1 + 1, x => x.Key > 0);
                if (i1 == -1) return (int)span.TotalSeconds + " Seconds";
                string s1 = list[i1].Key + " " + (list[i1].Key == 0 ? list[i1].Value[0] : list[i1].Value[1]);
                string s2 = i2 >= 0 ? (list[i2].Key + " " + (list[i2].Key == 0 ? list[i2].Value[0] : list[i2].Value[1])) : "";
                return s1 + ", " + s2;
            }
            catch
            {
                return (int)span.TotalSeconds + " Seconds";
            }
        }
    }
}
