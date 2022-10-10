using GraphicGlobal;
using GraphicMinimal;
using ParticipatingMedia.DistanceSampling;
using ParticipatingMedia.Media;
using ParticipatingMedia.PhaseFunctions;
using System;
using System.Collections.Generic;

namespace ParticipatingMediaTest.MediaMocks
{
    public class ParticipatingMediaMock : IParticipatingMedia
    {
        public int Priority { get; private set; }
        public float RefractionIndex { get; private set; }

        public IPhaseFunction PhaseFunction { get; private set; }
        public IDistanceSampler DistanceSampler { get; private set; }

        private readonly float scatteringCoeffizient;

        //returnValuesForDistanceSampling = Diese Werte werden nach jeden n-ten Aufruf bei der SampleDistance-Funktion zurück gegeben
        //expectedMediaPointsForDistanceSampling = Diese Punkte müssen beim n-ten Aufruf der SampleDistance-Funktion übergeben werden. Stimmt das nicht, kommt eine Exception
        public ParticipatingMediaMock(int priority, float refractionIndex, ParticipatingMediaMockData data)
        {
            this.Priority = priority;
            this.RefractionIndex = refractionIndex;
            this.DistanceSampler = new DistanceSamplerMock(data.ReturnValuesForDistanceSampling, data.ExpectedMediaPointsForDistanceSampling);
            this.PhaseFunction = new PhaseDirectionSamplerMock(data.ReturnValuesForPhaseDirectionSampling, data.ExpectedMediaPointsForDirectionSampling);
            this.scatteringCoeffizient = data.ScatteringCoeffizient;
        }

        public Vector3D EvaluateAttenuation(Ray ray, float rayMin, float rayMax)
        {
            float a = (float)Math.Exp(-scatteringCoeffizient * (rayMax - rayMin));
            return new Vector3D(a, a, a);
        }

        public Vector3D EvaluateEmission(Ray ray, float rayMin, float rayMax)
        {
            return new Vector3D(-1, -1, -1);
        }

        public Vector3D GetAbsorbationCoeffizient(Vector3D position)
        {
            return new Vector3D(0, 0, 0);
        }

        public Vector3D GetScatteringCoeffizient(Vector3D position)
        {
            return new Vector3D(1, 1, 1) * this.scatteringCoeffizient;
        }

        public bool HasScatteringSomeWhereInMedium()
        {
            return true;
        }

        public bool HasScatteringOnPoint(Vector3D point)
        {
            return true;
        }
    }

    public class DistanceSamplerMock : IDistanceSampler
    {
        private int index = -1;
        private readonly List<float> returnValuesForDistanceSampling;
        private readonly List<Vector3D> expectedMediaPointsForDistanceSampling;

        public DistanceSamplerMock(List<float> returnValuesForDistanceSampling, List<Vector3D> expectedMediaPointsForDistanceSampling)
        {
            this.returnValuesForDistanceSampling = returnValuesForDistanceSampling;
            this.expectedMediaPointsForDistanceSampling = expectedMediaPointsForDistanceSampling;
        }

        public RaySampleResult SampleRayPositionWithPdfFromRayMinToInfinity(Ray ray, float rayMin, float rayMax, IRandom rand, bool startPointIsOnParticleInMedia)
        {
            if (this.returnValuesForDistanceSampling == null)
            {
                return new RaySampleResult()
                {
                     RayPosition = rayMax,
                     PdfL = -1,
                     ReversePdfL = -2
                };
            }
            
            this.index++;
            if (this.index >= this.returnValuesForDistanceSampling.Count) throw new Exception("Mehr Returnvalues habe ich nicht");
            Vector3D mediaPoint = ray.Start + ray.Direction * rayMin;
            if (mediaPoint != this.expectedMediaPointsForDistanceSampling[index]) throw new Exception("Erwarteter MediaPunkt " + this.expectedMediaPointsForDistanceSampling[index] + " stimmt nicht mit übergebenen Punkt " + mediaPoint + " überrein");
            return new RaySampleResult() { RayPosition = rayMin + this.returnValuesForDistanceSampling[this.index], PdfL = 1, ReversePdfL = 1 };
        }

        public DistancePdf GetSamplePdfFromRayMinToInfinity(Ray ray, float rayMin, float rayMax, float sampledRayPosition, bool startPointIsOnParticleInMedium, bool endPointIsOnParticleInMedium)
        {
            throw new NotImplementedException();
        }

        public RaySampleResult SampleRayPositionWithPdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, IRandom rand)
        {
            throw new NotImplementedException();
        }

        public DistancePdf GetSamplePdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, float sampledRayPosition)
        {
            throw new NotImplementedException();
        }
    }

    public class PhaseDirectionSamplerMock : IPhaseFunction
    {
        private int index = -1;
        private readonly List<PhaseSampleResult> returnValuesForPhaseDirectionSampling;
        private readonly List<Vector3D> expectedMediaPointsForDirectionSampling;

        public PhaseDirectionSamplerMock(List<PhaseSampleResult> returnValuesForPhaseDirectionSampling, List<Vector3D> expectedMediaPointsForDirectionSampling)
        {
            this.returnValuesForPhaseDirectionSampling = returnValuesForPhaseDirectionSampling;
            this.expectedMediaPointsForDirectionSampling = expectedMediaPointsForDirectionSampling;
        }

        public PhaseSampleResult SampleDirection(Vector3D mediaPoint, Vector3D directionToPoint, IRandom rand)
        {
            this.index++;
            if (this.index >= this.returnValuesForPhaseDirectionSampling.Count) throw new Exception("Mehr Returnvalues habe ich nicht");
            if (mediaPoint != this.expectedMediaPointsForDirectionSampling[index]) throw new Exception("Erwarteter MediaPunkt " + this.expectedMediaPointsForDirectionSampling[index] + " stimmt nicht mit übergebenen Punkt " + mediaPoint + " überrein");
            return this.returnValuesForPhaseDirectionSampling[this.index];
        }

        public PhaseFunctionResult GetBrdf(Vector3D directionToMediaPoint, Vector3D mediaPoint, Vector3D outDirection)
        {
            throw new NotImplementedException();
        }               
    }
}
