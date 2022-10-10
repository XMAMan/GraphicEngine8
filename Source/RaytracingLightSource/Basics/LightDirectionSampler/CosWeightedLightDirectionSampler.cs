using GraphicMinimal;
using RaytracingBrdf.BrdfFunctions;
using RaytracingRandom;
using System;

namespace RaytracingLightSource.Basics.LightDirectionSampler
{
    //Sampelt Theta-Cosinusgewichtet innerhalb einer Halbkugel
    class CosWeightedLightDirectionSampler : ILightDirectionSampler
    {
        private readonly Vector3D normal;
        public CosWeightedLightDirectionSampler(Vector3D normal)
        {
            this.normal = normal;
        }

        public LightDirectionSamplerResult SampleDirection(double u1, double u2, double u3)
        {
            Vector3D direction = BrdfDiffuseCosinusWeighted.SampleDirection(u1, u2, this.normal);
            return new LightDirectionSamplerResult()
            {
                Direction = direction,
                PdfW = BrdfDiffuseCosinusWeighted.PDFw(this.normal, direction)
            };
        }
        public float GetPdfW(Vector3D direction)
        {
            return Math.Max(0, this.normal * direction) / (float)Math.PI;
        }

        public SimpleFunction GetBrdfOverThetaFunction()
        {
            return new SimpleFunction((theta) =>
            {
                if (theta > Math.PI / 2) return 0;
                return Math.Cos(theta);
            });
        }
    }
}
