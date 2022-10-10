using GraphicMinimal;
using System;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.SlopSpace.SlopeDistribution
{
    class GgxSlopeDistribution : ISlopeDistribution
    {
        public float SlopeMax { get { return 16.0f; } }

        public float P22(float xm, float ym)
        {
            float c = 1 + xm * xm + ym * ym;
            return (float)(1.0 / (Math.PI * c * c));
        }

        public Vector2D SampleVisibleSlope(double thetaInputDirection, double u1, double u2)
        {
            //Special Case (Normal Incidence)
            if (thetaInputDirection < 0.0001)
            {
                double r = Math.Sqrt(u1 / (1 - u1));
                double phi = 6.28318530718 * u2;
                return new Vector2D((float)(r * Math.Cos(phi)), (float)(r * Math.Sin(phi)));
            }

            //Precomputations
            double tan_theta_i = Math.Tan(thetaInputDirection);
            double a = 1 / tan_theta_i;
            double g1 = 2 / (1 + Math.Sqrt(1 + 1 / (a * a)));

            Vector2D slope = new Vector2D(0, 0);
            //Sample SlopeX
            double A = 2 * u1 / g1 - 1;
            double tmp = 1 / (A * A - 1);
            double B = tan_theta_i;
            double D = Math.Sqrt(B * B * tmp * tmp - (A * A - B * B) * tmp);
            double slope_x_1 = B * tmp - D;
            double slope_x_2 = B * tmp + D;
            slope.X = (float)((A < 0 || slope_x_2 > 1.0 / tan_theta_i) ? slope_x_1 : slope_x_2);

            //Sample SlopeY
            double S;
            if (u2 > 0.5)
            {
                S = 1.0f;
                u2 = 2 * (u2 - 0.5);
            }
            else
            {
                S = -1;
                u2 = 2 * (0.5 - u2);
            }
            double z = (u2 * (u2 * (u2 * 0.27385 - 0.73369) + 0.46341)) / (u2 * (u2 * (u2 * 0.093073 + 0.309420) - 1.000000) + 0.597999);
            slope.Y = (float)(S * z * Math.Sqrt(1.0 + slope.X * slope.X));

            return slope;
        }
    }
}
