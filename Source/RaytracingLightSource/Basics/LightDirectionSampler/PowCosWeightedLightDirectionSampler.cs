using GraphicMinimal;
using RayTracerGlobal;
using RaytracingRandom;
using System;

namespace RaytracingLightSource.Basics.LightDirectionSampler
{
    class PowCosWeightedLightDirectionSampler : ILightDirectionSampler
    {
        private readonly Vector3D normal;
        private readonly float powFactor;
        public PowCosWeightedLightDirectionSampler(Vector3D normal, float powFactor)
        {
            this.normal = normal;
            this.powFactor = powFactor;
        }

        public LightDirectionSamplerResult SampleDirection(double u1, double u2, double u3)
        {

            //Fehlerlhafte Implementierung (Hiermit kann ich meine Testfunktion testen)
            /*double phi = 2 * Math.PI;

            float term2 = (float)Math.Pow(u2, 1 / (this.powFactor + 1));
            float term3 = (float)Math.Sqrt(1 - term2 * term2);
            Vector3D w = normal,
                   u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
                   v = Vector3D.Cross(w, u);

            Vector3D direction = Vector3D.Normalize((u * (float)Math.Cos(phi) * term3 + v * (float)Math.Sin(phi) * term3 + w * term2));

            float cosThetaR = Math.Max(1e-6f, normal * direction);

            float jacobi = 1 / Math.Max(1e-6f, Math.Abs(normal * direction));
            float pdfW = Math.Max(MagicNumbers.MinAllowedPdfW, (float)((this.powFactor + 1) * Math.Pow(cosThetaR, this.powFactor) * ((1 / Math.PI) * 0.5f))) * jacobi;

            return new LightDirectionSamplerResult()
            {
                Direction = direction,
                PdfW = pdfW
            };*/

            double phi = u1 * 2 * Math.PI;

            float term2 = (float)Math.Pow(u2, 1 / (this.powFactor + 1));
            float term3 = (float)Math.Sqrt(1 - term2 * term2);
            Vector3D w = this.normal,
                   u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
                   v = Vector3D.Cross(w, u);

            Vector3D direction = Vector3D.Normalize((u * (float)Math.Cos(phi) * term3 + v * (float)Math.Sin(phi) * term3 + w * term2));

            float cosThetaR = Math.Max(1e-6f, normal * direction);

            float pdfW = Math.Max(MagicNumbers.MinAllowedPdfW, (float)((this.powFactor + 1) * Math.Pow(cosThetaR, this.powFactor) * ((1 / Math.PI) * 0.5f)));

            return new LightDirectionSamplerResult()
            {
                Direction = direction,
                PdfW = pdfW
            };
        }
        public float GetPdfW(Vector3D direction)
        {
            //Fehlerlhafte Implementierung (Hiermit kann ich meine Testfunktion testen)
            /*float dot_R_wi = this.normal * direction;
            if (dot_R_wi <= 1e-6f) return 0;

            //float jacobi = 1 / Math.Max(1e-6f, Math.Abs(this.point.Normale * lightGoingOutDirection)); //Die cos^n-Gleichung wurde so aufgestellt, dass Alpha der Winkel zwischen o und (0,0,1) ist und nicht zwischen o und PerfektSpecular

            float rho = (this.powFactor + 2) * 0.5f / (float)Math.PI;
            return rho * (float)Math.Pow(dot_R_wi, this.powFactor);// * jacobi;
            */

            float dot_R_wi = this.normal * direction;
            if (dot_R_wi <= 1e-6f) return 0;

            float pdfW = Math.Max(MagicNumbers.MinAllowedPdfW, (float)((this.powFactor + 1) * Math.Pow(dot_R_wi, this.powFactor) * ((1 / Math.PI) * 0.5f)));
            return pdfW;
        }

        public SimpleFunction GetBrdfOverThetaFunction()
        {
            return new SimpleFunction((theta) =>
            {
                if (theta > Math.PI / 2) return 0;
                return Math.Pow(Math.Cos(theta), this.powFactor);
            });
        }
    }
}
