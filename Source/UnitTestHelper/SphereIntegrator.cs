using GraphicMinimal;
using System;

namespace UnitTestHelper
{
    //Dient zum numerischen integrieren von Lebesg-Integralen(Oberflächenintegral auf Kugel), wo ein Richtungsvektor über eine Kugel oder Halbkugel mit dem Solid-Angle-Maß integriert wird
    //Das Phi von der (Halb)Kugel geht in der XY-Ebent von 0 bis 360 Grad. Das Theta ist der Winkel zwischen der Z-Achse und dem Laufwinkel und geht von 0 bis Maximal 180 Grad
    //Der Boden von der Halbkugel liegt in der XY-Ebene und die Normale von der Halbkugel zeigt auf Z-Achse (0,0,1)
    public class SphereIntegrator
    {
        public delegate double UserFunction(Vector3D loopDirection, double loopPhi, double loopTheta);

        //Integral userFunction(w) * dw -> w geht über Halbkugel
        public static double IntegrateWithForLoop(UserFunction userFunction, double phiMin = 0, double phiMax = 360, double thetaMin = 0, double thetaMax = 180, double dPhi = 0.005, double dTheta = 0.005)
        {
            if (phiMin < 0 || phiMax > 360 || phiMax <= phiMin) throw new ArgumentException("Phi-Min-Max must be in the range of 0-360 degrees");
            if (thetaMin < 0 || thetaMax > 180 || thetaMax <= thetaMin) throw new ArgumentException("Theta-Min-Max must be in the range of 0-180 degrees");
            if (dPhi < 0 || dPhi > 2 * Math.PI) throw new ArgumentException("dPhi must be in the range of 0 to 2*PI");
            if (dTheta < 0 || dTheta > Math.PI) throw new ArgumentException("dTheta must be in the range of 0 to PI");

            phiMin = phiMin * (2 * Math.PI) / 360;
            phiMax = phiMax * (2 * Math.PI) / 360;
            thetaMin = thetaMin * Math.PI / 180;
            thetaMax = thetaMax * Math.PI / 180;

            double integral = 0;

            for (double theta = thetaMin; theta < thetaMax; theta += dTheta)
                for (double phi = phiMin; phi < phiMax; phi += dPhi)
                {
                    Vector3D direction = new Vector3D((float)(Math.Cos(phi) * Math.Sin(theta)), (float)(Math.Sin(phi) * Math.Sin(theta)), (float)Math.Cos(theta));
                    double differentialSolidAngleMeasure = Math.Sin(theta) * dPhi * dTheta; //Flächeninhalt von den Kugelstück, was um 'direction' liegt
                    integral += userFunction(direction, phi, theta) * differentialSolidAngleMeasure;
                }

            return integral;
        }

        //Integral userFunction(w) * dw -> w geht über Halbkugel
        //Erklärung dafür befindet sich bei mein Microfacet-Mitschriften auf Seite 57
        public static double IntegrateWithMonteCarlo(UserFunction userFunction, double phiMin = 0, double phiMax = 360, double thetaMin = 0, double thetaMax = 180, int sampleCount = 10000)
        {
            if (phiMin < 0 || phiMax > 360 || phiMax <= phiMin) throw new ArgumentException("Phi-Min-Max must be in the range of 0-360 degrees");
            if (thetaMin < 0 || thetaMax > 180 || thetaMax <= thetaMin) throw new ArgumentException("Theta-Min-Max must be in the range of 0-180 degrees");

            phiMin = phiMin * (2 * Math.PI) / 360;
            phiMax = phiMax * (2 * Math.PI) / 360;
            thetaMin = thetaMin * Math.PI / 180;
            thetaMax = thetaMax * Math.PI / 180;

            double pdf = 1.0 / ((-Math.Cos(thetaMax) + Math.Cos(thetaMin)) * (phiMax - phiMin));

            Random rand = new Random(0);
            double integral = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                double u1 = rand.NextDouble();
                double u2 = rand.NextDouble();

                double theta = Math.Acos(Math.Cos(thetaMin) + u1 * (Math.Cos(thetaMax) - Math.Cos(thetaMin)));
                double phi = (phiMax - phiMin) * u2 + phiMin;
                Vector3D direction = new Vector3D((float)(Math.Cos(phi) * Math.Sin(theta)), (float)(Math.Sin(phi) * Math.Sin(theta)), (float)Math.Cos(theta));
                integral += userFunction(direction, phi, theta) / pdf;
            }

            return integral / sampleCount;
        }
    }
}
