using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphicGlobal;
using GraphicGlobal.MathHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GraphicGlobalTest
{
    [TestClass]
    public class CubeRootTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void TestAllCubeRootMethods()
        {
            // a million uniform steps through the range from 0.0 to 1.0
            // (doing uniform steps in the log scale would be better)
            double a = 0.0;
            double b = 1.0;
            int n = 1000000;

            StringBuilder str = new StringBuilder();
            str.AppendLine("32-bit float tests");
            str.AppendLine("----------------------------------------");
            str.AppendLine(TestCubeRootf("cbrt_5f", CubeRoot.cbrt_5f, a, b, n));
            str.AppendLine(TestCubeRootf("pow", CubeRoot.pow_cbrtf, a, b, n));
            str.AppendLine(TestCubeRootf("halley x 1", CubeRoot.halley_cbrt1f, a, b, n));
            str.AppendLine(TestCubeRootf("halley x 2", CubeRoot.halley_cbrt2f, a, b, n));
            str.AppendLine(TestCubeRootf("newton x 1", CubeRoot.newton_cbrt1f, a, b, n));
            str.AppendLine(TestCubeRootf("newton x 2", CubeRoot.newton_cbrt2f, a, b, n));
            str.AppendLine(TestCubeRootf("newton x 3", CubeRoot.newton_cbrt3f, a, b, n));
            str.AppendLine(TestCubeRootf("newton x 4", CubeRoot.newton_cbrt4f, a, b, n));
            str.AppendLine();
            str.AppendLine();

            str.AppendLine("64-bit double tests");
            str.AppendLine("----------------------------------------");
            str.AppendLine(TestCubeRootd("cbrt_5d", CubeRoot.cbrt_5d, a, b, n));
            str.AppendLine(TestCubeRootd("pow", CubeRoot.pow_cbrtd, a, b, n));
            str.AppendLine(TestCubeRootd("halley x 1", CubeRoot.halley_cbrt1d, a, b, n));
            str.AppendLine(TestCubeRootd("halley x 2", CubeRoot.halley_cbrt2d, a, b, n));
            str.AppendLine(TestCubeRootd("halley x 3", CubeRoot.halley_cbrt3d, a, b, n));
            str.AppendLine(TestCubeRootd("newton x 1", CubeRoot.newton_cbrt1d, a, b, n));
            str.AppendLine(TestCubeRootd("newton x 2", CubeRoot.newton_cbrt2d, a, b, n));
            str.AppendLine(TestCubeRootd("newton x 3", CubeRoot.newton_cbrt3d, a, b, n));
            str.AppendLine(TestCubeRootd("newton x 4", CubeRoot.newton_cbrt4d, a, b, n));
            str.AppendLine();
            str.AppendLine();

            File.WriteAllText(WorkingDirectory + "CubeRoot.txt", str.ToString());
        }

        delegate float cuberootfnf(float f);
        delegate double cuberootfnd(double d);

        static string TestCubeRootf(string szName, cuberootfnf cbrt, double rA, double rB, int rN)
        {
            int N = rN;

            float dd = (float)((rB - rA) / N);

            // calculate 1M numbers
            int i = 0;
            float d = (float)rA;

            double s = 0.0;

            for (d = (float)rA, i = 0; i < N; i++, d += dd)
            {
                s += cbrt(d);
            }

            double bits = 0.0;
            double worstx = 0.0;
            double worsty = 0.0;
            int minbits = 64;

            for (d = (float)rA, i = 0; i < N; i++, d += dd)
            {
                float a = cbrt((float)d);
                float b = (float)Math.Pow((double)d, 1.0 / 3.0);

                int bc = bits_of_precision(a, b);
                bits += bc;

                if (b > 1.0e-6)
                {
                    if (bc < minbits)
                    {
                        minbits = bc;
                        worstx = d;
                        worsty = a;
                    }
                }
            }

            bits /= N;

            string result = string.Format("{0:000} mbp  {1:000000.000} abp\n", minbits, bits);

            return result;
            //return s;
        }

        static string TestCubeRootd(string szName, cuberootfnd cbrt, double rA, double rB, int rN)
        {
            int N = rN;

            double dd = (rB - rA) / N;

            int i = 0;

            double s = 0.0;
            double d = 0.0;

            for (d = rA, i = 0; i < N; i++, d += dd)
            {
                s += cbrt(d);
            }


            double bits = 0.0;
            double worstx = 0.0;
            double worsty = 0.0;
            int minbits = 64;
            for (d = rA, i = 0; i < N; i++, d += dd)
            {
                double a = cbrt(d);
                double b = Math.Pow(d, 1.0 / 3.0);

                int bc = bits_of_precision(a, b); // min(53, count_matching_bitsd(a, b) - 12);
                bits += bc;

                if (b > 1.0e-6)
                {
                    if (bc < minbits)
                    {
                        bits_of_precision(a, b);
                        minbits = bc;
                        worstx = d;
                        worsty = a;
                    }
                }
            }

            bits /= N;

            string result = string.Format("{0:000} mbp  {1:000000.000} abp\n", minbits, bits);

            return result;
            //return s;
        }

        // estimate bits of precision (32-bit float case)
        static int bits_of_precision(float a, float b)
        {
            double kd = 1.0 / Math.Log(2.0);

            if (a == b)
                return 23;

            double kdmin = Math.Pow(2.0, -23.0);

            double d = Math.Abs(a - b);
            if (d < kdmin)
                return 23;

            return (int)(-Math.Log(d) * kd);
        }

        // estiamte bits of precision (64-bit double case)
        static int bits_of_precision(double a, double b)
        {
            double kd = 1.0 / Math.Log(2.0);

            if (a == b)
                return 52;

            double kdmin = Math.Pow(2.0, -52.0);

            double d = Math.Abs(a - b);
            if (d < kdmin)
                return 52;

            return (int)(-Math.Log(d) * kd);
        }
    }
}
