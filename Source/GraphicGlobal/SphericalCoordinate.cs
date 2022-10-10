using System;

namespace GraphicGlobal
{
    public class SphericalCoordinate
    {
        private double phi;
        public double Phi
        {
            get
            {
                return this.phi;
            }
            set
            {
                if (value < 0 || value > 2 * Math.PI) throw new ArgumentOutOfRangeException("Phi has to be in range of 0 to 2*PI");
                this.phi = value;
            }
        }

        private double theta;
        public double Theta
        {
            get
            {
                return this.theta;
            }
            set
            {
                if (value < 0 || value > Math.PI) throw new ArgumentOutOfRangeException("Theta has to be in range of 0 to PI");
                this.theta = value;
            }
        }

        public SphericalCoordinate(double phi, double theta)
        {
            this.Phi = phi;
            this.Theta = theta;
        }
    }
}
