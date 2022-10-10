using System;
using GraphicGlobal;
using GraphicMinimal;

namespace ParticipatingMedia.PhaseFunctions
{
    //Quelle: SmallUPBP
    //Das ist die Henyey-Greenstein-Phasenfunktion
    public class AnisotropicPhaseFunction : IPhaseFunction
    {
        private float anisotropyCoeffizient;
        private float phaseFunctionExtraFactor;

        public AnisotropicPhaseFunction(float anisotropyCoeffizient, float phaseFunctionExtraFactor)
        {
            this.anisotropyCoeffizient = anisotropyCoeffizient;
            this.phaseFunctionExtraFactor = phaseFunctionExtraFactor;
        }

        private static float PdfW(Vector3D directionToBrdfPoint, float anisotropyCoeffizient, Vector3D outDirection)
        {
            float cosTheta = directionToBrdfPoint * outDirection;
            float squareMeanCosine = anisotropyCoeffizient * anisotropyCoeffizient;
            float d = 1 + squareMeanCosine - (anisotropyCoeffizient + anisotropyCoeffizient) * cosTheta;

            return d > 0 ? (float)((1.0f / (4 * Math.PI) * (1 - squareMeanCosine) / (d * Math.Sqrt(d)))) : 0;
        }

        //r1 = rand.NextDouble()
        //r2 = rand.NextDouble()
        private static Vector3D SampleDirectionVector(double u1, double u2, float anisotropyCoeffizient, Vector3D directionToPoint, out float pdf)
        {
            //u1 == ((1 - (anisotropyCoeffizient * anisotropyCoeffizient)) / Math.Sqrt(1 + anisotropyCoeffizient * anisotropyCoeffizient) - 1 + anisotropyCoeffizient) / (anisotropyCoeffizient + anisotropyCoeffizient)

            double squareMeanCosine = anisotropyCoeffizient * anisotropyCoeffizient;
            double twoCosine = anisotropyCoeffizient + anisotropyCoeffizient;
            double sqrtt = (1 - squareMeanCosine) / (1 - anisotropyCoeffizient + twoCosine * u1);
            double cosTheta = (1 + squareMeanCosine - sqrtt * sqrtt) / twoCosine;
            double sinTheta = Math.Sqrt(Math.Max(0, 1 - cosTheta * cosTheta));
            double phi = 2 * Math.PI * u2;
            double sinPhi = Math.Sin(phi);
            double cosPhi = Math.Cos(phi);
            double d = 1 + squareMeanCosine - twoCosine * cosTheta;

            Vector3D w = directionToPoint,
                   u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.99f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
                   v = Vector3D.Cross(w, u);
            Vector3D outdirection = Vector3D.Normalize((u * (float)(cosPhi * sinTheta) + v * (float)(sinPhi * sinTheta) + w * (float)cosTheta));

            pdf = d > 0 ? (float)((1.0f / (4 * Math.PI) * (1 - squareMeanCosine) / (d * Math.Sqrt(d)))) : 0;
            //pdf = PdfW(directionToPoint, anisotropyCoeffizient, outdirection);

            return outdirection;
        }

        public PhaseFunctionResult GetBrdf(Vector3D directionToBrdfPoint, Vector3D brdfPoint, Vector3D outDirection)
        {
            float pdf = PdfW(directionToBrdfPoint, this.anisotropyCoeffizient, outDirection);
            //float pdfW = Math.Max(MagicNumbers.MinAllowedPdfW, pdf);            
            float pdfW = pdf;

            return new PhaseFunctionResult()
            {
                Brdf = pdf * this.phaseFunctionExtraFactor,
                PdfW = pdfW,
                PdfWReverse = pdfW,
            };
        }

        public PhaseSampleResult SampleDirection(Vector3D mediaPoint, Vector3D directionToPoint, IRandom rand)
        {
            float pdf;
            Vector3D direction = SampleDirectionVector(rand.NextDouble(), rand.NextDouble(), this.anisotropyCoeffizient, directionToPoint, out pdf);
            return new PhaseSampleResult()
            {
                PdfW = pdf,
                PdfWReverse = pdf,
                BrdfDividedByPdfW = 1 * this.phaseFunctionExtraFactor,
                Ray = new Ray(mediaPoint, direction)
            };
        }
    }
}
