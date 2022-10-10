using System;
using GraphicGlobal;
using GraphicMinimal;

namespace ParticipatingMedia.PhaseFunctions
{
    public class IsotrophicPhaseFunction : IPhaseFunction
    {
        public PhaseFunctionResult GetBrdf(Vector3D directionToBrdfPoint, Vector3D brdfPoint, Vector3D outDirection)
        {
            float uniform = 1.0f / (4 * (float)Math.PI);
            return new PhaseFunctionResult()
            {
                Brdf = uniform,
                PdfW = uniform,         
                PdfWReverse = uniform,
            };
        }

        public PhaseSampleResult SampleDirection(Vector3D mediaPoint, Vector3D directionToPoint, IRandom rand)
        {
            //cosTheta = 1 - 2 * u2
            //cosTheta² = (1 - 2 * u2)² = 1 - 2*1*2*u2 + 2²*u2² = 1 - 4*u2 + 4*u2²
            //sinTheta² = 1 - cosTheta² = 4*u2 - 4*u2² = 4 * (u2-u2²)
            //sinTheta = 2 * Sqrt(u2 - u2*u2)
            double u1 = rand.NextDouble(), u2 = rand.NextDouble();
            double term1 = 2 * Math.PI * u1;    //term1 = phi
            double term2 = 2 * Math.Sqrt(u2 - u2 * u2); //term2 = sinTheta

            Vector3D direction = new Vector3D(
                (float)(Math.Cos(term1) * term2),
                (float)(Math.Sin(term1) * term2),
                (float)(1 - 2 * u2)); //cosTheta = 1 - 2 * u2
            
            float uniform = 1.0f / (4 * (float)Math.PI);

            return new PhaseSampleResult()
            {
                PdfW = uniform,
                PdfWReverse = uniform,
                BrdfDividedByPdfW = 1,
                Ray = new Ray(mediaPoint, direction)
            };
        }
    }
}
