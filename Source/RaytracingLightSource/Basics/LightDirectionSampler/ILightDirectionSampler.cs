using GraphicMinimal;
using RaytracingRandom;

namespace RaytracingLightSource.Basics.LightDirectionSampler
{
    //Sampelt ein Richtungsvektor um eine gegebene Normale
    interface ILightDirectionSampler
    {
        LightDirectionSamplerResult SampleDirection(double u1, double u2, double u3);
        float GetPdfW(Vector3D direction);
        SimpleFunction GetBrdfOverThetaFunction();
    }

    class LightDirectionSamplerResult
    {
        public Vector3D Direction;
        public float PdfW;
    }
}
