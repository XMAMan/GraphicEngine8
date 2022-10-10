using GraphicGlobal;
using GraphicMinimal;
using RaytracingRandom;
using System;

namespace RaytracingLightSource.Basics.LightDirectionSampler
{
    //Sampelt Theta-Cosinusgewichtet innerhalb eines Kugelsegments
    class CosWeightedSphereSegmentLightDirectionSampler : ILightDirectionSampler
    {
        public double SolidAngle { get; private set; } //Flächeninhalt von den Kugelsegmentstück (Wenn Kugel Radius von 1 hat)

        private readonly Frame frame;
        private readonly double phiMin;
        private readonly double phiMax;
        private readonly double thetaMin;
        private readonly double thetaMax;

        private readonly double pdfC; //Pdf-Normalisierungskonstante
        private readonly double phiK; //Konstante, die in der Phi-Sampler-Funktion auftaucht
        private readonly double thetaK; //Konstante, die in der Theta-Sampler-Funktion auftaucht
        private readonly double sinThetaMin2; //Konstante, die in der Theta-Sampler-Funktion auftaucht
        private readonly double cosThetaMax; //Konstante, die in der PdfW-Funktion auftaucht
        private readonly double cosThetaMin; //Konstante, die in der PdfW-Funktion auftaucht

        public CosWeightedSphereSegmentLightDirectionSampler(Vector3D normal, double phiMin, double phiMax, double thetaMin, double thetaMax)
            : this(new Frame(normal), phiMin, phiMax, thetaMin, thetaMax)
        {
        }


        public CosWeightedSphereSegmentLightDirectionSampler(Frame frame, double phiMin, double phiMax, double thetaMin, double thetaMax)
        {
            this.frame = frame;
            this.phiMin = phiMin;
            this.phiMax = phiMax;
            this.thetaMin = thetaMin;
            this.thetaMax = thetaMax;

            double sin2ThetaMax = Math.Sin(thetaMax);
            sin2ThetaMax *= sin2ThetaMax;
            double sin2ThetaMin = Math.Sin(thetaMin);
            sin2ThetaMin *= sin2ThetaMin;
            this.pdfC = (sin2ThetaMax - sin2ThetaMin) / 2 * (phiMax - phiMin);

            this.phiK = (sin2ThetaMax - sin2ThetaMin) / (2 * this.pdfC);
            this.thetaK = this.pdfC * 2 / (this.phiMax - this.phiMin);
            this.sinThetaMin2 = Math.Sin(this.thetaMin) * Math.Sin(this.thetaMin);
            this.cosThetaMax = Math.Cos(this.thetaMax);
            this.cosThetaMin = Math.Cos(this.thetaMin);

            this.SolidAngle = this.pdfC;
        }

        public LightDirectionSamplerResult SampleDirection(double u1, double u2, double u3)
        {
            double sinTheta2 = u1 * this.thetaK + this.sinThetaMin2;

            double cosTheta = Math.Sqrt(1 - sinTheta2);
            double phi = u2 / this.phiK + this.phiMin;

            return new LightDirectionSamplerResult()
            {
                Direction = this.frame.GetDirectionFromPhiAndCosTheta(cosTheta, phi),
                PdfW = (float)(cosTheta / this.pdfC)
                //PdfW = GetPdfW(this.frame.GetDirectionFromPhiAndCosTheta(cosTheta, phi))
            };
        }
        public float GetPdfW(Vector3D direction)
        {
            float cosTheta = Math.Max(0, this.frame.Normal * direction);
            if (cosTheta < this.cosThetaMax - 0.0001f || cosTheta > this.cosThetaMin + +0.0001f) return 0;
            return (float)(cosTheta / this.pdfC);
        }

        public SimpleFunction GetBrdfOverThetaFunction()
        {
            return new SimpleFunction((theta) =>
            {
                if (theta > this.thetaMax || theta < this.thetaMin) return 0;
                return Math.Cos(theta);
            });
        }
    }
}
