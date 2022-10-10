using System;
using GraphicMinimal;
using IntersectionTests;
using GraphicGlobal;

namespace RaytracingLightSource
{
    //Es ist bereits ein anderer Punkt(hitPointPosition) in der Szene gegeben. Dieser Sampler
    //erzeugt einen zufälligen Punkt auf einer Lichtquelle. 
    interface ISphereSamplerForDirectLighting
    {
        //Gibt null zurück, wenn der VisibleTerm zwischen gesampelten Punkt und hitPointPosition false ist
        Vector3D SamplePointOnSphere(float u1, float u2, Vector3D hitPointPosition);

        //Annahme: Der Visibleterm zwischen eyePoint und pointOnLightSource ist true
        float PdfA(Vector3D eyePoint, IntersectionPoint pointOnLightSource);
    }

    //http://www.cs.utah.edu/~shirley/papers/tog94.pdf -> Seite 10 (Sampling uniformly in directional space)
    class SphereSamplingShirley : ISphereSamplerForDirectLighting
    {
        private readonly Vector3D lightSourceCenter;
        private readonly float lightSourceRadius;
        public SphereSamplingShirley(Vector3D lightSourceCenter, float lightSourceRadius)
        {
            this.lightSourceCenter = lightSourceCenter;
            this.lightSourceRadius = lightSourceRadius;
        }

        //u1 = rand.NextDouble()
        //u2 = rand.NextDouble()
        public Vector3D SamplePointOnSphere(float u1, float u2, Vector3D hitPointPosition)
        {
            Vector3D toCenterVector = lightSourceCenter - hitPointPosition;
            float distance = toCenterVector.Length();
            Vector3D toCenterDirection = toCenterVector / distance;
            float f1 = lightSourceRadius / distance;
            if (f1 > 1) return null;
            f1 = (float)Math.Sqrt(1 - f1 * f1);
            float azimuthalCos = 1 - u1 + u1 * f1;
            float azimuthalAngle = (float)Math.Acos(azimuthalCos);
            float polarAngle = (float)(2 * Math.PI * u2);

            Vector3D w = toCenterDirection,
                   u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
                   v = Vector3D.Cross(w, u);

            float azimuthalSin = (float)Math.Sin(azimuthalAngle);

            Vector3D d = new Vector3D((float)(Math.Cos(polarAngle) * azimuthalSin),
                                  (float)(Math.Sin(polarAngle) * azimuthalSin),
                                  azimuthalCos);

            Vector3D toLightPointDirection = new Vector3D(u.X * d.X + v.X * d.Y + w.X * d.Z,
                                                      u.Y * d.X + v.Y * d.Y + w.Y * d.Z,
                                                      u.Z * d.X + v.Z * d.Y + w.Z * d.Z);

            Vector3D point = IntersectionHelper.GetIntersectionPointBetweenRayAndSphere(new Ray(hitPointPosition, toLightPointDirection), lightSourceCenter, lightSourceRadius);
            //if (point != null && point.Betrag() == 0) return null;
            return point;
        }

        public float PdfA(Vector3D hitPoint, IntersectionPoint pointOnLightSource)
        {
            Vector3D toLightPointDirection = hitPoint - pointOnLightSource.Position;
            float hitPointLightPointDistance = toLightPointDirection.Length();
            if (hitPointLightPointDistance < 0.00001f) return 0;
            toLightPointDirection /= hitPointLightPointDistance;
            float f1 = 1 - lightSourceRadius * lightSourceRadius / (hitPoint - lightSourceCenter).SquareLength();
            if (f1 < 0) return 0;
            float pdfA = (float)(Math.Max(pointOnLightSource.OrientedFlatNormal * toLightPointDirection, 0) / (2 * Math.PI * hitPointLightPointDistance * hitPointLightPointDistance * (1 - Math.Sqrt(f1))));
            if (float.IsNaN(pdfA)) throw new Exception("Alarm" + f1);
            return pdfA;
        }
    }

    //Diese Funktion stammt von mir. Sie funktioniert genau so gut wie Shirley, wenn du Abstand zur Kugel nicht zu klein wird
    class SphereSamplingNonUniform : ISphereSamplerForDirectLighting
    {
        private readonly Vector3D lightSourceCenter;
        private readonly float lightSourceRadius;
        public SphereSamplingNonUniform(Vector3D lightSourceCenter, float lightSourceRadius)
        {
            this.lightSourceCenter = lightSourceCenter;
            this.lightSourceRadius = lightSourceRadius;
        }

        //u1 = rand.NextDouble()
        //u2 = rand.NextDouble()
        public Vector3D SamplePointOnSphere(float u1, float u2, Vector3D hitPointPosition)
        {
            u1 *= 2 * (float)Math.PI;
            Vector3D w = Vector3D.Normalize(hitPointPosition - lightSourceCenter),
                   u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
                   v = Vector3D.Cross(w, u);

            u2 = lightSourceRadius * (float)Math.Sqrt(u2);
            Vector3D lightPoint = lightSourceCenter + (u * (float)Math.Cos(u1) * u2 + v * (float)Math.Sin(u1) * u2);

            lightPoint += w * (float)Math.Sqrt(lightSourceRadius * lightSourceRadius - (u2 * u2));            

            return lightPoint;
        }

        public float PdfA(Vector3D hitPoint, IntersectionPoint pointOnLightSource)
        {
            return Vector3D.Normalize(hitPoint - this.lightSourceCenter) * Vector3D.Normalize(pointOnLightSource.Position - this.lightSourceCenter) / (float)Math.PI;
        }
    }

    class SphereSamplingUniform : ISphereSamplerForDirectLighting
    {
        private readonly Vector3D lightSourceCenter;
        private readonly float lightSourceRadius;
        public SphereSamplingUniform(Vector3D lightSourceCenter, float lightSourceRadius)
        {
            this.lightSourceCenter = lightSourceCenter;
            this.lightSourceRadius = lightSourceRadius;
        }

        //u1 = rand.NextDouble()
        //u2 = rand.NextDouble()
        public Vector3D SamplePointOnSphere(float u1, float u2, Vector3D hitPointPosition)
        {
            Vector3D lightPoint = this.lightSourceCenter + Vector3D.GetRandomDirection(u1, u2) * this.lightSourceRadius;

            return lightPoint;
        }

        public float PdfA(Vector3D hitPoint, IntersectionPoint pointOnLightSource)
        {
            float area = lightSourceRadius * lightSourceRadius * 4 * (float)Math.PI;
            return 1.0f / area;
        }
    }
}
