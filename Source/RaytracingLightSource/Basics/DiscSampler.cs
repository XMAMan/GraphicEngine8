using System;
using GraphicMinimal;

namespace RaytracingLightSource.Basics
{
    //Sample innerhalb eines Kreises, der im 3D-Raum liegt
    class DiscSampler
    {
        private readonly Vector3D u;
        private readonly Vector3D v;
        private readonly Vector3D center;
        public float Radius { get; private set; }

        public DiscSampler(Vector3D center, Vector3D normal, float radius)
        {
            this.center = center;
            this.Radius = radius;

            Vector3D w = Vector3D.Normalize(normal);
            this.u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w));
            this.v = Vector3D.Cross(w, u);

            this.PdfA = (float)(1.0 / (radius * radius * Math.PI));
        }

        public Vector3D SamplePointOnDisc(double u1, double u2)
        {
            float r = (float)Math.Sqrt(u1) * this.Radius;
            double phi = 2 * Math.PI * u2;

            return this.center + Vector3D.Normalize((u * (float)Math.Cos(phi) + v * (float)Math.Sin(phi))) * r;
        }

        public float PdfA { get; private set; }
    }
}
