using System;

namespace GraphicGlobal.MathHelper
{
    public static class MathExtensions
    {
        public static double Cube_Root(double x)
        {
            double result = CubeRoot.halley_cbrt3d(Math.Abs(x)); //Nullstellenfindung für die Gleichung, wo ich die CubeRoot von y finden will mit: x^3 - y = 0 über https://de.wikipedia.org/wiki/Halley-Verfahren
            //double result = CubeRoot.LancasterCubeRoot(Math.Abs(x)); //  Lancaster ist das gleiche Verfahren wie das von Halley nur er wußte nicht, dass Halley schon längst das Verfahren erfunden hat 
            if (x < 0)
            {
                result = -result;
            }
            return result;
        }

        public static double Pow(double value, int exponent)
        {
            double gsum = 1;
            for (int i=0;i<exponent;i++)
            {
                gsum *= value;
            }
            return gsum;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) value = min;
            if (value >= max) value = max - 1;
            return value;
        }

        public static double Max(double d1, double d2, double d3)
        {
            return Math.Max(Math.Max(d1, d2), d3); ;
        }

        public static double Min(double d1, double d2, double d3)
        {
            return Math.Min(Math.Min(d1, d2), d3);
        }

        public static double Range(double d1, double d2, double d3)
        {
            return Max(d1, d2, d3) - Min(d1, d2, d3);
        }

        public static double Range(double d1, double d2)
        {
            return Math.Abs(d1 - d2);
        }


        //Die Errorfunction entspricht der CDF von der Normalverteilung. Mit der inversen Error-Function kann ich normalverteilte Zufallszahlen erzeugen (Das geht aber auch mit dem Box-Müller-Verfahren
        //https://www.johndcook.com/blog/csharp_erf/
        public static double Erf(double x)
        {
            // constants
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return sign * y;
        }

        //https://gist.github.com/danielc192/5870580
        //http://en.wikipedia.org/wiki/Error_function#Inverse_functions -> Dieser Quelltext ist direkt von Wikipedia
        public static double InverseErrorFunction(double x)
        {
            int kmax = 100;
            double[] c = new double[kmax];
            c[0] = 1.0;
            c[1] = 1.0;
            double result = 0;
            for (int k = 0; k < kmax; k++)
            {
                //Calculate C sub k
                if (k > 1)
                {
                    c[k] = 0;
                    for (int m = 0; m < k - 1; m++)
                    {
                        double term = (c[m] * c[k - 1 - m]) / ((m + 1) * (2 * m + 1));
                        c[k] += term;
                    }
                }
                result += (c[k] / (2 * k + 1)) * Math.Pow(((Math.Sqrt(Math.PI) / 2) * x), (2 * k + 1));
            }
            return result;
        }

        //https://stackoverflow.com/questions/22834998/what-reference-should-i-use-to-use-erf-erfc-function
    }
}
