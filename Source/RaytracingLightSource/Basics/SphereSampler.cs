using GraphicMinimal;
using System;

namespace RaytracingLightSource.Basics
{
    class SphereSampler
    {
        public Vector3D Center { get; private set; }
        public float Radius { get; private set; }

        public float Area { get; private set; }

        public SphereSampler(Vector3D center, float radius)
        {
            this.Center = center;
            this.Radius = radius;

            this.Area = (float)(4.0f * this.Radius * this.Radius * Math.PI);
            this.PdfA = (float)(1.0 / this.Area);
        }

        public Vector3D SamplePointOnDisc(double u1, double u2)
        {
            float phi = 2 * (float)(Math.PI * u1);
            float theta = (float)(Math.Acos(1 - 2 * u2));
            return this.Center + new Vector3D((float)(Math.Cos(phi) * Math.Sin(theta)), (float)(Math.Sin(phi) * Math.Sin(theta)), (float)(Math.Cos(theta))) * this.Radius;
        }

        public float PdfA { get; private set; }
    }
}
