using System;
using GraphicGlobal;
using GraphicGlobal.MathHelper;
using GraphicMinimal;
using RaytracingRandom;

namespace ParticipatingMedia.PhaseFunctions
{
    public class RayleighPhaseFunction : IPhaseFunction
    {
        //Quelle: https://www.scratchapixel.com/lessons/procedural-generation-virtual-worlds/simulating-sky
        //Erklärungen: Forschungen\ParticipatingMedia_2019\ParticipatingMedia-Mitschriften.odt
        public PhaseFunctionResult GetBrdf(Vector3D directionToBrdfPoint, Vector3D brdfPoint, Vector3D outDirection)
        {
            float cos = directionToBrdfPoint * outDirection;
            float brdf = (float)(3 / (16 * Math.PI) * (1 + cos * cos));

            return new PhaseFunctionResult()
            {
                Brdf = brdf,
                PdfW = brdf,
                PdfWReverse = brdf,
            };
        }

        public PhaseSampleResult SampleDirection(Vector3D mediaPoint, Vector3D directionToPoint, IRandom rand)
        {
            //Zeitvergleich für den SampleDirection_Rayleigh_FunctionPdfMatchWithHistogram-Test aus der IPhaseFunctionTest-Klasse
            //return SampleDirectionWithRejectionSampling(mediaPoint, directionToPoint, rand); //13.1 Sekunden
            return SampleDirectionWithInverseCdf(mediaPoint, directionToPoint, rand); //13.4 Sekunden
            //return SampleDirectionWithRejectionCosSampling(mediaPoint, directionToPoint, rand); //15.7 Sekunden
            //return SampleDirectionWithTabulatedCdf(mediaPoint, directionToPoint, rand); //13.6 Sekunden
        }

        //Quelle: Importance sampling the Rayleigh phase function (2011) Seite 4 -> Ich nehme das verbesserte Rejection-Sampling, dessen Pdf gleichmäßig (Isotrophisch) ist
        //Achtung: Alle Rejection-Sampling-Verfahren sampeln die Zielfunktion letztendlich mit perfekten importancesampling. Nur benötigt es pro Sample mehr Zeit, 
        //wenn die Pdf, mit der Versuchssamples erzeugt werden, schlecht ist. Aber sobalt das Sample dann gefundne wurde, ist dessen Pdf somit perfektes ImportanceSampling
        public PhaseSampleResult SampleDirectionWithRejectionSampling(Vector3D mediaPoint, Vector3D directionToPoint, IRandom rand)
        {
            double cosTheta, cosThetaSquare;
            do
            {
                cosTheta = 2 * rand.NextDouble() - 1; //Erzeuge cosTheta Isotrophisch über die Kugel
                cosThetaSquare = cosTheta * cosTheta;
            } while (rand.NextDouble() > 0.5 * (1 + cosThetaSquare));
            double phi = 2 * Math.PI * rand.NextDouble();

            Vector3D w = directionToPoint,
               u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
               v = Vector3D.Cross(w, u);

            float sinTheta = (float)Math.Sqrt(1 - cosThetaSquare);
            Vector3D directionOut = Vector3D.Normalize((u * (float)Math.Cos(phi) * sinTheta + v * (float)Math.Sin(phi) * sinTheta + w * (float)cosTheta));

            float pdfW = (float)(3 / (16 * Math.PI) * (1 + cosThetaSquare));
            return new PhaseSampleResult()
            {
                BrdfDividedByPdfW = 1,
                Ray = new Ray(mediaPoint, directionOut),
                PdfW = pdfW,
                PdfWReverse = pdfW //PdfW hängt nur vom Winkel zwischen Input- und Output-Richtung ab. Somit muss PdfW in beide Richtungen gleich sein
            };
        }

        //Quelle: Importance sampling the Rayleigh phase function (2011) Seite 5 -> Inverse CDF der Rayleigh-Funktion (DirectSampling im Paper genannt)
        public PhaseSampleResult SampleDirectionWithInverseCdf(Vector3D mediaPoint, Vector3D directionToPoint, IRandom rand)
        {
            double u1 = rand.NextDouble(), u2 = rand.NextDouble();
            double d1 = 2 * u1 - 1;
            //double d = -Math.Pow(2 * d1 + Math.Sqrt(4 * d1 * d1 + 1), 1.0 / 3);
            double d = -MathExtensions.Cube_Root(2 * d1 + Math.Sqrt(4 * d1 * d1 + 1));
            double cosTheta = d - 1 / d;
            double cosThetaSquare = cosTheta * cosTheta;
            double phi = 2 * Math.PI * u2;

            Vector3D w = directionToPoint,
               u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
               v = Vector3D.Cross(w, u);

            float sinTheta = (float)Math.Sqrt(1 - cosThetaSquare);
            Vector3D directionOut = Vector3D.Normalize((u * (float)Math.Cos(phi) * sinTheta + v * (float)Math.Sin(phi) * sinTheta + w * (float)cosTheta));

            float pdfW = (float)(3 / (16 * Math.PI) * (1 + cosThetaSquare));
            return new PhaseSampleResult()
            {
                BrdfDividedByPdfW = 1,
                Ray = new Ray(mediaPoint, directionOut),
                PdfW = pdfW,
                PdfWReverse = pdfW //PdfW hängt nur vom Winkel zwischen Input- und Output-Richtung ab. Somit muss PdfW in beide Richtungen gleich sein
            };
        }

        //Quelle: Importance sampling the Rayleigh phase function (2011) Seite 4 -> RejectionSampling mit Pdf=cosinus
        public PhaseSampleResult SampleDirectionWithRejectionCosSampling(Vector3D mediaPoint, Vector3D directionToPoint, IRandom rand)
        {
            double cosTheta, cosThetaSquare;
            double fix = 9 / (4 * Math.Sqrt(6));
            do
            {
                cosTheta = Math.Cos(Math.PI * rand.NextDouble());
                cosThetaSquare = cosTheta * cosTheta;
            } while (rand.NextDouble() > fix * (1+ cosThetaSquare)*Math.Sqrt(1 - cosThetaSquare));
            double phi = 2 * Math.PI * rand.NextDouble();

            Vector3D w = directionToPoint,
               u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
               v = Vector3D.Cross(w, u);

            float sinTheta = (float)Math.Sqrt(1 - cosThetaSquare);
            Vector3D directionOut = Vector3D.Normalize((u * (float)Math.Cos(phi) * sinTheta + v * (float)Math.Sin(phi) * sinTheta + w * (float)cosTheta));

            float pdfW = (float)(3 / (16 * Math.PI) * (1 + cosThetaSquare));
            return new PhaseSampleResult()
            {
                BrdfDividedByPdfW = 1,
                Ray = new Ray(mediaPoint, directionOut),
                PdfW = pdfW,
                PdfWReverse = pdfW //PdfW hängt nur vom Winkel zwischen Input- und Output-Richtung ab. Somit muss PdfW in beide Richtungen gleich sein
            };
        }

        //Quelle: Importance sampling the Rayleigh phase function (2011) Seite 6 -> CDF per Tabulation sampeln
        private PdfWithTableSampler cosThetaSampler = PdfWithTableSampler.CreateFromCdf(CosThetaCdf, Math.Cos(0), Math.Cos(Math.PI), 16);
        public PhaseSampleResult SampleDirectionWithTabulatedCdf(Vector3D mediaPoint, Vector3D directionToPoint, IRandom rand)
        {
            double u1 = rand.NextDouble(), u2 = rand.NextDouble();

            //double d1 = 2 * u1 - 1;
            //double d = -Math.Pow(2 * d1 + Math.Sqrt(4 * d1 * d1 + 1), 1.0 / 3);
            //double cosTheta1 = d - 1 / d;

            double cosTheta = this.cosThetaSampler.GetXValue(u1);

            double cosThetaSquare = cosTheta * cosTheta;
            double phi = 2 * Math.PI * u2;

            Vector3D w = directionToPoint,
               u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
               v = Vector3D.Cross(w, u);

            float sinTheta = (float)Math.Sqrt(1 - cosThetaSquare);
            Vector3D directionOut = Vector3D.Normalize((u * (float)Math.Cos(phi) * sinTheta + v * (float)Math.Sin(phi) * sinTheta + w * (float)cosTheta));

            float pdfW = (float)(3 / (16 * Math.PI) * (1 + cosThetaSquare));
            return new PhaseSampleResult()
            {
                BrdfDividedByPdfW = 1,
                Ray = new Ray(mediaPoint, directionOut),
                PdfW = pdfW,
                PdfWReverse = pdfW //PdfW hängt nur vom Winkel zwischen Input- und Output-Richtung ab. Somit muss PdfW in beide Richtungen gleich sein
            };
        }
        private static double CosThetaCdf(double cosTheta)
        {
            return 0.5 - 3 / 8.0 * cosTheta - 1 / 8.0 * cosTheta * cosTheta * cosTheta;
        }
    }
}
