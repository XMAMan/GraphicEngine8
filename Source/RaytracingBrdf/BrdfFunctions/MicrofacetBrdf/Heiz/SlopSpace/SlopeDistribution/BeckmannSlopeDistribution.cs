using GraphicGlobal.MathHelper;
using GraphicMinimal;
using System;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.SlopSpace.SlopeDistribution
{
    class BeckmannSlopeDistribution : ISlopeDistribution
    {
        public float SlopeMax { get { return 6.0f; } }

        //Die Slope-Verteilungsfunktion von Beckmann ist die 2D-Gaussfunktion
        public float P22(float xm, float ym)
        {
            return (float)(1 / Math.PI * Math.Exp(-(xm * xm) - (ym * ym)));
        }

        public Vector2D SampleVisibleSlope(double thetaInputDirection, double u1, double u2)
        {
            //Special Case (Normal incidence)
            if (thetaInputDirection < 0.0001f)
            {
                double r = Math.Sqrt(-Math.Log(u1));
                double phi = 6.28318530718 * u2;
                return new Vector2D((float)(r * Math.Cos(phi)), (float)(r * Math.Sin(phi)));
            }

            //Precomputations
            double sin_theata_i = Math.Sin(thetaInputDirection);
            double cos_theata_i = Math.Cos(thetaInputDirection);
            double tan_theta_i = sin_theata_i / cos_theata_i;
            double a = 1.0 / tan_theta_i;
            double erf_a = MathExtensions.Erf(a);
            double exp_a2 = Math.Exp(-a * a);
            double sqrt_pi_inv = 0.56418958354;
            double lambda = 0.5 * (erf_a - 1) + 0.5 * sqrt_pi_inv * exp_a2 / a;
            double g1 = 1.0 / (1.0 + lambda); //Masking
            double c = 1.0 - g1 * erf_a;

            Vector2D slope = new Vector2D(0, 0);

            //Sample Slope X
            if (u1 < c)
            {
                //Rescale u1
                u1 /= c;

                double w_1 = 0.5 * sqrt_pi_inv * sin_theata_i * exp_a2;
                double w_2 = cos_theata_i * (0.5 - 0.5 * erf_a);
                double p = w_1 / (w_1 + w_2);

                if (u1 < p)
                {
                    u1 /= p;
                    slope.X = (float)(-Math.Sqrt(-Math.Log(u1 * exp_a2)));
                }
                else
                {
                    u1 = (u1 - p) / (1.0f - p);
                    slope.X = (float)MathExtensions.InverseErrorFunction(u1 - 1 - u1 * erf_a);
                }
            }
            else
            {
                //Rescale U1
                u1 = (u1 - c) / (1 - c);
                slope.X = (float)MathExtensions.InverseErrorFunction((-1 + 2 * u1) * erf_a);

                double p = (-slope.X * sin_theata_i + cos_theata_i) / (2 * cos_theata_i);
                if (u2 > p)
                {
                    slope.X = -slope.X;
                    u2 = (u2 - p) / (1 - p);
                }
                else
                    u2 /= p;
            }

            //Sample Slope Y
            slope.Y = (float)MathExtensions.InverseErrorFunction(2 * u2 - 1);

            return slope;
        }
    }
}
