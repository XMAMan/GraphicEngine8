using GraphicMinimal;
using RaytracingBrdf.BrdfFunctions;
using RaytracingRandom;
using System;

namespace RaytracingLightSource.Basics.LightDirectionSampler
{
    class UniformOverHalfSphereLightDirectionSampler : ILightDirectionSampler
    {
        private readonly Vector3D normal;
        public UniformOverHalfSphereLightDirectionSampler(Vector3D normal)
        {
            this.normal = normal;
        }
        public LightDirectionSamplerResult SampleDirection(double u1, double u2, double u3)
        {
            Vector3D direction = BrdfDiffuseUniformWeighted.SampleDirection(u1, u2, this.normal);
            return new LightDirectionSamplerResult()
            {
                Direction = direction,
                PdfW = 1.0f / (2 * (float)Math.PI)
            };
        }
        public float GetPdfW(Vector3D direction)
        {
            if (this.normal * direction + 0.0001f < 0) return 0;
            return 1.0f / (2 * (float)Math.PI);
        }

        public SimpleFunction GetBrdfOverThetaFunction()
        {
            return new SimpleFunction((theta) =>
            {
                if (theta > Math.PI / 2) return 0;
                return 1;
            });
        }
    }
}
