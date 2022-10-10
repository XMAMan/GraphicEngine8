using GraphicGlobal;
using GraphicMinimal;
using ParticipatingMedia.DistanceSampling;
using ParticipatingMedia.PhaseFunctions;

namespace ParticipatingMedia.Media
{
    class CompoundParticipatingMedia : IParticipatingMedia, ICompoundPhaseWeighter, IMediaOnWaveLength
    {
        public int Priority { get; private set; }
        public float RefractionIndex { get; private set; }
        public IPhaseFunction PhaseFunction { get; private set; }
        public IDistanceSampler DistanceSampler { get; private set; }
        public Vector3D MaxExtinctionCoeffizient { get; private set; }

        private IInhomogenMedia media1;
        private IInhomogenMedia media2;
        public CompoundParticipatingMedia(IInhomogenMedia media1, IInhomogenMedia media2)
        {
            this.media1 = media1;
            this.media2 = media2;
            this.Priority = media1.Priority;
            this.RefractionIndex = this.media1.RefractionIndex;
            this.MaxExtinctionCoeffizient = media1.MaxExtinctionCoeffizient + media2.MaxExtinctionCoeffizient;
            this.PhaseFunction = new CompoundPhaseFunction(this.media1.PhaseFunction, this.media2.PhaseFunction, this);
            this.DistanceSampler = new WoodCockTrackingDistanceSamplerWithEqualSegmentSampling(this);
        }

        public float GetCompoundPhaseFunctionWeight(Vector3D mediaPoint)
        {
            float os1 = this.media1.GetScatteringCoeffizient(mediaPoint).Z;
            float os2 = this.media2.GetScatteringCoeffizient(mediaPoint).Z;
            return os1 / (os1 + os2);
        }

        public Vector3D EvaluateAttenuation(Ray ray, float rayMin, float rayMax)
        {
            return Vector3D.Mult(this.media1.EvaluateAttenuation(ray, rayMin, rayMax), this.media2.EvaluateAttenuation(ray, rayMin, rayMax));
        }

        public Vector3D EvaluateEmission(Ray ray, float rayMin, float rayMax)
        {
            return new Vector3D(0, 0, 0);
        }

        public Vector3D GetAbsorbationCoeffizient(Vector3D position)
        {
            return this.media1.GetAbsorbationCoeffizient(position) + this.media2.GetAbsorbationCoeffizient(position);
        }

        public Vector3D GetScatteringCoeffizient(Vector3D position)
        {
            return this.media1.GetScatteringCoeffizient(position) + this.media2.GetScatteringCoeffizient(position);
        }

        public bool HasScatteringSomeWhereInMedium()
        {
            return true;
        }

        public bool HasScatteringOnPoint(Vector3D point)
        {
            return this.media1.HasScatteringOnPoint(point) || this.media2.HasScatteringOnPoint(point);
        }

        public float ExtinctionCoeffizientOnWave(Vector3D position)
        {
            return this.media1.ExtinctionCoeffizientOnWave(position) + this.media2.ExtinctionCoeffizientOnWave(position);
        }

        float IMediaOnWaveLength.EvaluateAttenuationOnWave(Ray ray, float rayMin, float rayMax)
        {
            return this.media1.EvaluateAttenuationOnWave(ray, rayMin, rayMax) * this.media2.EvaluateAttenuationOnWave(ray, rayMin, rayMax);
        }
    }
}
