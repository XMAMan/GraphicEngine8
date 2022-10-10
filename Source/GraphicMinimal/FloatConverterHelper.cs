using System;
using System.Globalization;

namespace GraphicMinimal
{
    public static class FloatConverterHelper
    {
        public static float ToSingle(this object o)
        {
            return Convert.ToSingle(o, new CultureInfo("en-us", false));
        }

        public static string ToEnString(this float f)
        {
            return f.ToString(new CultureInfo("en-us", false));
        }
    }
}
