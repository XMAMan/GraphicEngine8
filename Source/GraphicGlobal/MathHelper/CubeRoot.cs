using System;

namespace GraphicGlobal.MathHelper
{
    //Cube Root = Die 3-Fache Wurzel (Ziel: Schneller als Pow(x,1/3.0) zu sein)
    //https://github.com/servo/skia/blob/master/experimental/Intersection/CubeRoot.cpp
    public static class CubeRoot
    {
        //Quelle für diese Region: http://metamerist.blogspot.com/2007/09/faster-cube-root.html
        #region
        //Given an approximation a of the cube root of R, the function will return an approximation of the cube root of R that is closer than a.
        static double Lancaster(double a, double R)
        {
            double a3 = a * a * a;
            double b = a * (a3 + R + R) / (a3 + a3 + R);
            return b;
        }

        public static double LancasterCubeRoot(double d)
        {
            double a = cbrt_5d(d);
            a = Lancaster(a, d);
            a = Lancaster(a, d);
            return a;
        }

        #endregion


        // cube root via x^(1/3)
        public static float pow_cbrtf(float x)
        {
            return (float)Math.Pow(x, 1.0f / 3.0f);
        }

        // cube root via x^(1/3)
        public static double pow_cbrtd(double x)
        {
            return Math.Pow(x, 1.0 / 3.0);
        }

        // cube root approximation using bit hack for 32-bit float
        public unsafe static float cbrt_5f(float f)
        {
            UInt32* p = (UInt32*)&f;
            *p = *p / 3 + 709921077;
            return f;
        }

        // cube root approximation using bit hack for 64-bit float
        // adapted from Kahan's cbrt
        public unsafe static double cbrt_5d(double d)
        {
            const UInt32 B1 = 715094163;
            double t = 0.0;
            UInt32* pt = (UInt32*)&t;
            UInt32* px = (UInt32*)&d;
            pt[1] = px[1] / 3 + B1;
            return t;
        }

        //This functions do this: Math.Sqrt(Math.Sqrt(d)); with speed
        // cube root approximation using bit hack for 64-bit float
        // adapted from Kahan's cbrt
        unsafe static double quint_5d(double d)
        {
            

            const UInt32 B1 = 71509416 * 5 / 3;
            double t = 0.0;
            UInt32* pt = (UInt32*)&t;
            UInt32* px = (UInt32*)&d;
            pt[1] = px[1] / 5 + B1;
            return t;
        }

        // iterative cube root approximation using Halley's method (float)
        static float cbrta_halleyf(float a, float R)
        {
            float a3 = a * a * a;
            float b = a * (a3 + R + R) / (a3 + a3 + R);
            return b;
        }

        // iterative cube root approximation using Halley's method (double)
        public static double cbrta_halleyd(double a, double R)
        {
            double a3 = a * a * a;
            double b = a * (a3 + R + R) / (a3 + a3 + R);
            return b;
        }

        // iterative cube root approximation using Newton's method (float)
        static float cbrta_newtonf(float a, float x)
        {
            //    return (1.0 / 3.0) * ((a + a) + x / (a * a));
            return a - (1.0f / 3.0f) * (a - x / (a * a));
        }

        // iterative cube root approximation using Newton's method (double)
        static double cbrta_newtond(double a, double x)
        {
            return (1.0 / 3.0) * (x / (a * a) + 2 * a);
        }

        // cube root approximation using 1 iteration of Halley's method (double)
        public static double halley_cbrt1d(double d)
        {
            double a = cbrt_5d(d);
            return cbrta_halleyd(a, d);
        }

        // cube root approximation using 1 iteration of Halley's method (float)
        public static float halley_cbrt1f(float d)
        {
            float a = cbrt_5f(d);
            return cbrta_halleyf(a, d);
        }

        // cube root approximation using 2 iterations of Halley's method (double)
        public static double halley_cbrt2d(double d)
        {
            double a = cbrt_5d(d);
            a = cbrta_halleyd(a, d);
            return cbrta_halleyd(a, d);
        }

        // cube root approximation using 3 iterations of Halley's method (double)
        public static double halley_cbrt3d(double d)
        {
            double a = cbrt_5d(d);
            a = cbrta_halleyd(a, d);
            a = cbrta_halleyd(a, d);
            return cbrta_halleyd(a, d);
        }

        // cube root approximation using 2 iterations of Halley's method (float)
        public static float halley_cbrt2f(float d)
        {
            float a = cbrt_5f(d);
            a = cbrta_halleyf(a, d);
            return cbrta_halleyf(a, d);
        }

        // cube root approximation using 1 iteration of Newton's method (double)
        public static double newton_cbrt1d(double d)
        {
            double a = cbrt_5d(d);
            return cbrta_newtond(a, d);
        }

        // cube root approximation using 2 iterations of Newton's method (double)
        public static double newton_cbrt2d(double d)
        {
            double a = cbrt_5d(d);
            a = cbrta_newtond(a, d);
            return cbrta_newtond(a, d);
        }

        // cube root approximation using 3 iterations of Newton's method (double)
        public static double newton_cbrt3d(double d)
        {
            double a = cbrt_5d(d);
            a = cbrta_newtond(a, d);
            a = cbrta_newtond(a, d);
            return cbrta_newtond(a, d);
        }

        // cube root approximation using 4 iterations of Newton's method (double)
        public static double newton_cbrt4d(double d)
        {
            double a = cbrt_5d(d);
            a = cbrta_newtond(a, d);
            a = cbrta_newtond(a, d);
            a = cbrta_newtond(a, d);
            return cbrta_newtond(a, d);
        }

        // cube root approximation using 2 iterations of Newton's method (float)
        public static float newton_cbrt1f(float d)
        {
            float a = cbrt_5f(d);
            return cbrta_newtonf(a, d);
        }

        // cube root approximation using 2 iterations of Newton's method (float)
        public static float newton_cbrt2f(float d)
        {
            float a = cbrt_5f(d);
            a = cbrta_newtonf(a, d);
            return cbrta_newtonf(a, d);
        }

        // cube root approximation using 3 iterations of Newton's method (float)
        public static float newton_cbrt3f(float d)
        {
            float a = cbrt_5f(d);
            a = cbrta_newtonf(a, d);
            a = cbrta_newtonf(a, d);
            return cbrta_newtonf(a, d);
        }

        // cube root approximation using 4 iterations of Newton's method (float)
        public static float newton_cbrt4f(float d)
        {
            float a = cbrt_5f(d);
            a = cbrta_newtonf(a, d);
            a = cbrta_newtonf(a, d);
            a = cbrta_newtonf(a, d);
            return cbrta_newtonf(a, d);
        }
    }
}
