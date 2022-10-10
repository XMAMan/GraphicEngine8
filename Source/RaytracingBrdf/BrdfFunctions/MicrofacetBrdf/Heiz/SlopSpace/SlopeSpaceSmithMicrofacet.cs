using GraphicMinimal;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.SlopSpace.SlopeDistribution;
using System;
using System.Linq;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.SlopSpace
{
    //Stellt eine Microfacetfläche(Highmap) in der XY-Ebene da, dessen Höhenwerte in der Z-Achse liegen
    //Die Kanten von den Einzelflächen von dieser Highmap berühren sich nicht. Von oben betrachtet sieht die Highmap zusammenhängend aus
    //Jede Einzelfläche hat eine Normale. Die Verteilung der Normalen ist eine Gaußverteilung(Normalverteilung) oder GGX-Verteilung
    class SlopeSpaceSmithMicrofacet : ISlopeSpaceMicrofacet
    {
        private readonly ISlopeDistribution slopeDistribution;
        private readonly float alphaX;
        private readonly float alphaY;

        //slopeDistribution laut dieser Verteilungsfunktion sind die Slopes und somit die Normalen der Microflächen verteilt
        //alphaX,alphaY = Roughnessfaktoren. Um diesen Faktor wird die Fläche in der X- und Y-Richtung gestreckt, was dazu führt, dass die Highmap flacher wird
        public SlopeSpaceSmithMicrofacet(ISlopeDistribution slopeDistribution, float alphaX, float alphaY)
        {
            this.slopeDistribution = slopeDistribution;
            this.alphaX = alphaX;
            this.alphaY = alphaY;

            CreateTableData();
        }

        //Gibt die Verteilung(Häufigkeit) der Slopes zurück, welche den Slopwert (xm,ym) haben.
        public float GetSlopeDistribution(float xm, float ym)
        {
            return this.slopeDistribution.P22(this.alphaX * xm, this.alphaY * ym);
        }

        //phi und theta beschreiben den wi-Vektor, von welcher aus die Microsurface beobachtet wird. 
        //phi ist in der xy-Ebene gemessen und geht von 0 bis 360 (Angabe im Bogenmaß)
        //theata ist der Winkel zwischen der xy-Ebene und der Z-Achse und geht von 0 bis 90 (Angabe im Bogenmaß)
        public float GetVisibleSlopeDistribution(float phi, float theta, float xm, float ym)
        {
            Vector3D wi = new Vector3D((float)(Math.Cos(phi) * Math.Sin(theta)), (float)(Math.Sin(phi) * Math.Sin(theta)), (float)Math.Cos(theta));
            float slopeDot = -wi.X * xm + -wi.Y * ym + wi.Z;
            float projectionFactor = HeavisideFunction(slopeDot) * slopeDot;
            return projectionFactor * GetSlopeDistribution(xm, ym);
        }

        private float HeavisideFunction(float f)
        {
            return f > 0 ? 1 : 0;
        }

        public Vector3D SampleMicronormalAnalytical(Vector3D inputDirection, float u1, float u2)
        {
            return SampleMicronormal(inputDirection, u1, u2, this.slopeDistribution.SampleVisibleSlope);
        }

        public Vector3D SampleMicronormalFromTableData(Vector3D inputDirection, float u1, float u2)
        {
            return SampleMicronormal(inputDirection, u1, u2, SampleVisibleSlopeFromTableData);
        }

        //Erzeugt eine Micronormale, dessen Fläche von der InputDirection aus zu sehen ist. u1,u2=Zufallszahlen im Bereich von 0 bis 1
        //inputDirection = Muss im lokalen Space von der Microfläche angegeben werden. D.h die Fläche liegt in der XY-Ebene und die Macronormale ist (0,0,1) 
        private Vector3D SampleMicronormal(Vector3D inputDirection, float u1, float u2, Func<double, double, double, Vector2D> slopeSampleFunction)
        {
            //1. Stretch InputDirection with Roughnessfactor
            Vector3D omegaI = Vector3D.Normalize(new Vector3D(inputDirection.X * this.alphaX, inputDirection.Y * this.alphaY, inputDirection.Z));

            //Get Polar Coordinates of omagaI
            float theta = 0;
            float phi = 0;
            if (omegaI.Z < 0.99999f)
            {
                theta = (float)Math.Acos(omegaI.Z);
                phi = (float)Math.Atan2(omegaI.Y, omegaI.X);
            }

            //2. Sample P22_{omega_i}(x_slope, y_slope, 1, 1)
            Vector2D slope = slopeSampleFunction(theta, u1, u2);

            //3. Rotate
            float tmp = (float)(Math.Cos(phi) * slope.X - Math.Sin(phi) * slope.Y);
            slope.Y = (float)(Math.Sin(phi) * slope.X + Math.Cos(phi) * slope.Y);
            slope.X = tmp;

            //4. Unstretch
            slope.X *= this.alphaX;
            slope.Y *= this.alphaY;

            //slope = new Vector2D(slope.Y / 2, slope.X / 2);
            //return new Vector3D(slope.X, slope.Y, 0);

            //5. Compuete Normal from Slope
            float inv_omaga_m = (float)Math.Sqrt(slope.X * slope.X + slope.Y * slope.Y + 1);
            return new Vector3D(-slope.X / inv_omaga_m,
                              -slope.Y / inv_omaga_m,
                              1.0f / inv_omaga_m);
        }

        private double[,] data_Tx; //Hiermit kann ein Slope.X-Wert gesampelt werden. Zugriff über [theta_i,u1]
        private double[,] data_Ty; //Hiermit kann ein Slope.Y-Wert gesampelt werden. Zugriff über [Slope.X,u2]

        private void CreateTableData()
        {
            //int size = 1024;
            int size = 256;
            this.data_Tx = CreateDataX(size, size, size * 2);
            this.data_Ty = CreateDataY(size, size, size * 2);
        }

        private double[,] CreateDataX(int thetaIndexCount, int u1IndexCount, int cdfIndexCount)
        {
            //Allocate Tx
            double[,] data_Tx = new double[thetaIndexCount, u1IndexCount];

            //Allocate temporary data
            double[] slope_x = new double[cdfIndexCount];
            double[] CDF_P22_omega_i = new double[cdfIndexCount];

            //Loop over incident directions
            for (int index_theta = 0; index_theta < thetaIndexCount; index_theta++)
            {
                //Incident vector
                double theta = 0.5 * Math.PI * index_theta / (thetaIndexCount - 1);
                double sin_theta = Math.Sin(theta); //Sin theta_i = x_i
                double cos_theta = Math.Cos(theta); //Cos theta_i = z_i

                //for a given incident vector integrate P22_{omega_i}(x_slope,1,1), Eq (10)
                slope_x[0] = -this.slopeDistribution.SlopeMax;
                CDF_P22_omega_i[0] = 0;
                for (int index_slope_x = 1; index_slope_x < cdfIndexCount; index_slope_x++)
                {
                    //slope_x
                    slope_x[index_slope_x] = -this.slopeDistribution.SlopeMax + 2 * this.slopeDistribution.SlopeMax * index_slope_x / (cdfIndexCount - 1);

                    //dot product with incident vector
                    double dot_product = Math.Max(0, -slope_x[index_slope_x] * sin_theta + cos_theta);

                    //Marginalize P22_{omega_i}(x_slope,1,1), Eq. (10)
                    double P22_omega_i = 0;
                    for (int j = 0; j < 100; j++)
                    {
                        double slope_y = -this.slopeDistribution.SlopeMax + 2 * this.slopeDistribution.SlopeMax * j / 99.0;
                        P22_omega_i += dot_product * this.slopeDistribution.P22((float)slope_x[index_slope_x], (float)slope_y);
                    }

                    //CDF of P22_{omega_i}(x_slope, 1, 1), Eq. (10)
                    CDF_P22_omega_i[index_slope_x] = CDF_P22_omega_i[index_slope_x - 1] + P22_omega_i;
                }

                //renormalize CDF_P22_omega_i
                for (int index_slope_x = 1; index_slope_x < cdfIndexCount; index_slope_x++)
                    CDF_P22_omega_i[index_slope_x] /= CDF_P22_omega_i.Last();

                //loop over random number U1
                {
                    int index_slope_x = 0;
                    for (int index_U = 0; index_U < u1IndexCount; index_U++)
                    {
                        double U = 0.0000001 + 0.9999998 * index_U / (double)(u1IndexCount - 1);

                        // inverse CDF_P22_omega_i, solve Eq.(11)
                        while (CDF_P22_omega_i[index_slope_x] <= U)
                            ++index_slope_x;

                        double interp = (CDF_P22_omega_i[index_slope_x] - U) /
                                (CDF_P22_omega_i[index_slope_x] - CDF_P22_omega_i[index_slope_x - 1]);

                        //store value
                        data_Tx[index_theta, index_U] = interp * slope_x[index_slope_x - 1] + (1.0f - interp) * slope_x[index_slope_x];
                    }
                }
            }

            return data_Tx;
        }

        private double[,] CreateDataY(int slopeXIndexCount, int u2IndexCount, int cdfIndexCount)
        {
            //Allocate Tx
            double[,] data_Ty = new double[slopeXIndexCount, u2IndexCount];

            //allocate temporary data
            double[] slope_y = new double[cdfIndexCount];
            double[] CDF_P22y = new double[cdfIndexCount];

            //loop over slope_x
            for (int index_slope_x = 0; index_slope_x < slopeXIndexCount; index_slope_x++)
            {
                //slope_x
                double slope_x = -this.slopeDistribution.SlopeMax + 2 * this.slopeDistribution.SlopeMax * index_slope_x / (double)(slopeXIndexCount - 1);

                // CDF of P22y, Eq.(13)
                slope_y[0] = -this.slopeDistribution.SlopeMax;
                CDF_P22y[0] = 0;
                for (int index_slope_y = 1; index_slope_y < cdfIndexCount; index_slope_y++)
                {
                    slope_y[index_slope_y] = -this.slopeDistribution.SlopeMax + 2 * this.slopeDistribution.SlopeMax * index_slope_y / (cdfIndexCount - 1);
                    CDF_P22y[index_slope_y] = CDF_P22y[index_slope_y - 1] + this.slopeDistribution.P22((float)slope_x, (float)slope_y[index_slope_y]);
                }

                // renormalize CDF_P22y
                for (int index_slope_y = 1; index_slope_y < cdfIndexCount; index_slope_y++)
                {
                    CDF_P22y[index_slope_y] /= CDF_P22y.Last();
                }

                // loop over random number U2
                {
                    int index_slope_y = 0;
                    for (int index_U = 0; index_U < u2IndexCount; index_U++)
                    {
                        double U = 0.0000001 + 0.9999998 * index_U / (double)(u2IndexCount - 1);

                        // inverse CDF_P22y, solve Eq.(13)
                        while (CDF_P22y[index_slope_y] <= U)
                            ++index_slope_y;

                        double interp = (CDF_P22y[index_slope_y] - U) /
                            (CDF_P22y[index_slope_y] - CDF_P22y[index_slope_y - 1]);

                        // store value
                        data_Ty[index_slope_x, index_U] = interp * slope_y[index_slope_y - 1] + (1.0f - interp) * slope_y[index_slope_y];
                    }
                }
            }

            return data_Ty;
        }

        private Vector2D SampleVisibleSlopeFromTableData(double thetaInputDirection, double u1, double u2)
        {
            double slope_X = Tx(thetaInputDirection, u1);
            double slope_Y = Ty(slope_X, u2);
            return new Vector2D((float)slope_X, (float)slope_Y);
        }

        private double Tx(double theta, double U)
        {
            // indices and interpolation weight in dimension 1 (theta)
            double t = theta / (0.5 * Math.PI) * (double)(this.data_Tx.GetLength(0) - 1);
            int index_theta1 = Math.Max(0, (int)Math.Floor(t));
            int index_theta2 = Math.Min(index_theta1 + 1, this.data_Tx.GetLength(0) - 1);
            double interp_theta = t - (double)index_theta1;
            // indices and interpolation weight in dimension 2 (U1)
            double u = U * (double)(this.data_Tx.GetLength(1) - 1);
            int index_u1 = Math.Max(0, (int)Math.Floor(u));
            int index_u2 = Math.Min(index_u1 + 1, this.data_Tx.GetLength(1) - 1);
            double interp_u = u - (double)index_u1;
            double slope_x = (1.0f - interp_theta) * (1.0f - interp_u) * data_Tx[index_theta1, index_u1]
                + (1.0f - interp_theta) * interp_u * data_Tx[index_theta1, index_u2]
                + interp_theta * (1.0f - interp_u) * data_Tx[index_theta2, index_u1]
                + interp_theta * interp_u * data_Tx[index_theta2, index_u2];
            return slope_x;
        }

        private double Ty(double slope_x, double U)
        {
            // indices and interpolation weight in dimension 1 (slope_x)
            double x = (slope_x + this.slopeDistribution.SlopeMax) / (2.0 * this.slopeDistribution.SlopeMax) * (double)(data_Ty.GetLength(0) - 1);
            int index_x1 = Math.Max(0, (int)Math.Floor(x));
            int index_x2 = Math.Min(index_x1 + 1, data_Ty.GetLength(0) - 1);
            double interp_x = x - (double)index_x1;
            // indices and interpolation weight in dimension 2 (U2)
            double u = U * (double)(data_Ty.GetLength(1) - 1);
            int index_u1 = Math.Max(0, (int)Math.Floor(u));
            int index_u2 = Math.Min(index_u1 + 1, data_Ty.GetLength(1) - 1);
            double interp_u = u - (double)index_u1;
            double slope_y = (1.0f - interp_x) * (1.0f - interp_u) * data_Ty[index_x1, index_u1]
                + (1.0f - interp_x) * interp_u * data_Ty[index_x1, index_u2]
                + interp_x * (1.0f - interp_u) * data_Ty[index_x2, index_u1]
                + interp_x * interp_u * data_Ty[index_x2, index_u2];
            return slope_y;
        }
    }
}
