using GraphicMinimal;
using RaytracingRandom;

namespace RaytracingLightSource.Basics.LightDirectionSampler
{
    class MixTwoFunctionsLightDirectionSampler : ILightDirectionSampler
    {
        private readonly ILightDirectionSampler sampler1;
        private readonly ILightDirectionSampler sampler2;
        private readonly float f;

        //f=0..1 (0 = Nimm nur sampler1; 1 = Nimm nur sampler 2)
        public MixTwoFunctionsLightDirectionSampler(ILightDirectionSampler sampler1, ILightDirectionSampler sampler2, float f)
        {
            this.sampler1 = sampler1;
            this.sampler2 = sampler2;
            this.f = f;
        }

        public LightDirectionSamplerResult SampleDirection(double u1, double u2, double u3)
        {
            LightDirectionSamplerResult result;
            if (u3 > this.f)
            {
                result = this.sampler1.SampleDirection(u1, u2, u3);

            }
            else
            {
                result = this.sampler2.SampleDirection(u1, u2, u3);
            }

            result.PdfW = GetPdfW(result.Direction);
            return result;
        }

        public float GetPdfW(Vector3D direction)
        {
            return this.sampler1.GetPdfW(direction) * (1 - f) + this.sampler2.GetPdfW(direction) * f;
        }

        public SimpleFunction GetBrdfOverThetaFunction()
        {
            var func1 = this.sampler1.GetBrdfOverThetaFunction();
            var func2 = this.sampler2.GetBrdfOverThetaFunction();
            return new SimpleFunction((theta) =>
            {
                return func1(theta) * (1 - f) + func2(theta) * f;
            });
        }
    }
}
