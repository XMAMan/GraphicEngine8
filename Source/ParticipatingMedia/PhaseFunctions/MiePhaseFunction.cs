using System;
using GraphicGlobal;
using GraphicMinimal;
using RaytracingRandom;

namespace ParticipatingMedia.PhaseFunctions
{
    //Quelle: https://www.scratchapixel.com/lessons/procedural-generation-virtual-worlds/simulating-sky
    public class MiePhaseFunction : IPhaseFunction
    {
        private float anisotropyCoeffizient;
        private PdfWithTableSampler cosThetaSampler;

        public MiePhaseFunction(float anisotropyCoeffizient)
        {
            this.anisotropyCoeffizient = anisotropyCoeffizient;
            this.cosThetaSampler = PdfWithTableSampler.CreateFromUnnormalisizedFunction(Brdf, Math.Cos(0), Math.Cos(Math.PI), 2048);
        }

        private double Brdf(double cosTheta)
        {
            float g = this.anisotropyCoeffizient;
            return 3 / (8 * Math.PI) * ((1 - g * g) * (1 + cosTheta * cosTheta)) / ((2 + g * g) * Math.Pow(1 + g * g - 2 * g * cosTheta, 1.5f));
        }

        public PhaseFunctionResult GetBrdf(Vector3D directionToBrdfPoint, Vector3D brdfPoint, Vector3D outDirection)
        {
            //float g = this.anisotropyCoeffizient;
            //float mu = directionToBrdfPoint * outDirection;
            //double brdf = 3 / (8 * Math.PI) * ((1 - g * g) * (1 + mu * mu)) / ((2 + g * g) * Math.Pow(1 + g * g - 2 * g * mu, 1.5f));

            double brdf = Brdf(directionToBrdfPoint * outDirection);
            double pdfW = brdf / this.cosThetaSampler.NormalisationConstant / (2 * Math.PI); //CosTheta-Pdf * Phi-Pdf

            return new PhaseFunctionResult()
            {
                Brdf = (float)brdf,
                PdfW = (float)pdfW,
                PdfWReverse = (float)pdfW,
            };
        }

        public PhaseSampleResult SampleDirection(Vector3D mediaPoint, Vector3D directionToPoint, IRandom rand)
        {
            double u1 = rand.NextDouble(), u2 = rand.NextDouble();

            double cosTheta = this.cosThetaSampler.GetXValue(u1);

            double cosThetaSquare = cosTheta * cosTheta;
            double phi = 2 * Math.PI * u2;

            Vector3D w = (directionToPoint),
               u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
               v = Vector3D.Cross(w, u);

            float sinTheta = (float)Math.Sqrt(1 - cosThetaSquare);
            Vector3D directionOut = Vector3D.Normalize((u * (float)Math.Cos(phi) * sinTheta + v * (float)Math.Sin(phi) * sinTheta + w * (float)cosTheta));

            double brdf = Brdf((directionToPoint) * directionOut);
            double pdfW = brdf / this.cosThetaSampler.NormalisationConstant / (2 * Math.PI); //CosTheta-Pdf * Phi-Pdf

            return new PhaseSampleResult()
            {
                BrdfDividedByPdfW = (float)this.cosThetaSampler.NormalisationConstant,
                Ray = new Ray(mediaPoint, directionOut),
                PdfW = (float)pdfW,
                PdfWReverse = (float)pdfW //PdfW hängt nur vom Winkel zwischen Input- und Output-Richtung ab. Somit muss PdfW in beide Richtungen gleich sein
            };
        }
    }
}
