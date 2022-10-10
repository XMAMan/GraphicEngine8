using GraphicGlobal;
using GraphicMinimal;
using ParticipatingMedia.DistanceSampling;
using ParticipatingMedia.PhaseFunctions;
using System;

namespace ParticipatingMedia.Media
{
    public class ParticipatingMediaVacuum : IParticipatingMedia
    {
        public int Priority { get; private set; }
        public float RefractionIndex { get; private set; } //Im GlobalMedia steht hier 1. In Glas-Würfeln ohne Media steht hier dann ein Wert > 1 (Je nach Angabe der RefractionIndex-Property)
        public IPhaseFunction PhaseFunction { get; private set; } = null;
        public IDistanceSampler DistanceSampler { get; private set; }

        public ParticipatingMediaVacuum(int priority, float refractionIndex)
        {
            this.Priority = priority;
            this.RefractionIndex = refractionIndex;
            this.DistanceSampler = new VacuumDistanceSampler();
        }

        public bool HasScatteringSomeWhereInMedium()
        {
            return false;
        }

        public bool HasScatteringOnPoint(Vector3D point)
        {
            return false;
        }
        public Vector3D EvaluateAttenuation(Ray ray, float rayMin, float rayMax)
        {
            return new Vector3D(1, 1, 1);
        }

        public Vector3D EvaluateEmission(Ray ray, float rayMin, float rayMax)
        {
            return new Vector3D(0, 0, 0);
        }

        public Vector3D GetScatteringCoeffizient(Vector3D position)
        {
            return new Vector3D(0, 0, 0);
        }

        public Vector3D GetAbsorbationCoeffizient(Vector3D position)
        {
            return new Vector3D(0, 0, 0);
        }
    }
}
