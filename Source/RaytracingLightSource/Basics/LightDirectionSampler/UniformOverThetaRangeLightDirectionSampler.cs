using GraphicMinimal;
using RaytracingRandom;
using System;

namespace RaytracingLightSource.Basics.LightDirectionSampler
{
    class UniformOverThetaRangeLightDirectionSampler : ILightDirectionSampler
    {
        private readonly Vector3D normal;
        private readonly double minTheta;
        private readonly double maxTheta;
        private readonly double pdfW;

        //maxTheta geht von 0(Alles Licht geht in Richtung Normale) bis PI/2 (Gleichmäßig innerhalbl Halbkugel)
        public UniformOverThetaRangeLightDirectionSampler(Vector3D normal, double minTheta, double maxTheta)
        {
            this.normal = normal;
            this.minTheta = minTheta;
            this.maxTheta = maxTheta;

            this.pdfW = 1.0 / ((Math.Cos(this.minTheta) - Math.Cos(this.maxTheta)) * (2 * Math.PI));
        }

        public LightDirectionSamplerResult SampleDirection(double u1, double u2, double u3)
        {
            double theta = Math.Acos(Math.Cos(0) + u1 * (Math.Cos(this.maxTheta) - Math.Cos(minTheta)));
            double phi = (2 * Math.PI) * u2;

            Vector3D w = this.normal,
                  u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
                  v = Vector3D.Cross(w, u);

            Vector3D direction = Vector3D.Normalize((u * (float)Math.Cos(phi) * (float)Math.Sin(theta) + v * (float)Math.Sin(phi) * (float)Math.Sin(theta) + normal * (float)Math.Cos(theta)));

            return new LightDirectionSamplerResult()
            {
                Direction = direction,
                PdfW = (float)this.pdfW
            };
        }
        public float GetPdfW(Vector3D direction)
        {
            if ((this.normal * direction + 0.0001f) < (float)Math.Cos(this.maxTheta)) return 0; //Eine gesampelte Richtung darf nicht wegen Rundungsfehlern eine PdfW von 0 haben
            return (float)this.pdfW;
        }

        public SimpleFunction GetBrdfOverThetaFunction()
        {
            return new SimpleFunction((theta) =>
            {
                if (theta > this.maxTheta) return 0;
                return 1;
            });
        }
    }
}
