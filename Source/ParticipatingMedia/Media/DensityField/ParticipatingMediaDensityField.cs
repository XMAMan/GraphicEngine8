using GraphicGlobal;
using GraphicMinimal;
using ParticipatingMedia.DistanceSampling;
using ParticipatingMedia.PhaseFunctions;
using System;

namespace ParticipatingMedia.Media.DensityField
{
    class ParticipatingMediaDensityField : IParticipatingMedia, IMediaOnWaveLength, IInhomogenMedia
    {
        public int Priority { get; private set; }
        public float RefractionIndex { get; private set; }
        public IPhaseFunction PhaseFunction { get; private set; }
        public IDistanceSampler DistanceSampler { get; private set; }
        public Vector3D MaxExtinctionCoeffizient { get; private set; }

        private DescriptionForDensityFieldMedia mediaDescription;
        private DensityIntegrator density;
        private Vector3D attenuationCoeffizent;

        public ParticipatingMediaDensityField(IDensityField densityField, int priority, float refractionIndex, DescriptionForDensityFieldMedia mediaDescription)
        {
            this.Priority = priority;
            this.RefractionIndex = refractionIndex;
            this.mediaDescription = mediaDescription;

            this.density = new DensityIntegrator(densityField, mediaDescription.StepCountForAttenuationIntegration);
            this.attenuationCoeffizent = mediaDescription.ScatteringCoeffizent + mediaDescription.AbsorbationCoeffizent;

            this.MaxExtinctionCoeffizient = this.attenuationCoeffizent * this.density.MaxDensity;

            if (mediaDescription.AnisotropyCoeffizient == 0)
                this.PhaseFunction = new IsotrophicPhaseFunction();
            else
                this.PhaseFunction = new AnisotropicPhaseFunction(mediaDescription.AnisotropyCoeffizient, 1);

            this.DistanceSampler = new WoodCockTrackingDistanceSamplerWithEqualSegmentSampling(this);

        }

        public Vector3D EvaluateAttenuation(Ray ray, float rayMin, float rayMax)
        {
            float a = (this as IMediaOnWaveLength).EvaluateAttenuationOnWave(ray, rayMin, rayMax);
            return new Vector3D(1, 1, 1) * a;
        }

        public Vector3D EvaluateEmission(Ray ray, float rayMin, float rayMax)
        {
            return new Vector3D(0, 0, 0);
        }

        public Vector3D GetAbsorbationCoeffizient(Vector3D position)
        {
            return this.density.GetDensityFromPoint(position) * this.mediaDescription.AbsorbationCoeffizent;
        }

        public Vector3D GetScatteringCoeffizient(Vector3D position)
        {
            return this.density.GetDensityFromPoint(position) * this.mediaDescription.ScatteringCoeffizent;
        }

        public bool HasScatteringSomeWhereInMedium()
        {
            return true;
        }

        public bool HasScatteringOnPoint(Vector3D point)
        {
            return ExtinctionCoeffizientOnWave(point) > 0; //Diese Methode wird beim SegmentSampling und beim Einsammeln von Photonen über ein Strahl verwendet
        }

        public float ExtinctionCoeffizientOnWave(Vector3D position)
        {
            return this.density.GetDensityFromPoint(position) * this.attenuationCoeffizent.X;
        }

        public float EvaluateAttenuationOnWave(Ray ray, float rayMin, float rayMax)
        {
            return (float)Math.Exp(-this.attenuationCoeffizent.X * this.density.GetIntegralFromLine(ray, rayMin, rayMax));
        }

        //.............................

        class DensityIntegrator
        {
            private IDensityField density;
            private int stepCount;
            //private IRandom rand = new Rand(0);
            public DensityIntegrator(IDensityField density, int stepCount)
            {
                this.density = density;
                this.stepCount = stepCount;
            }

            public float MaxDensity { get { return this.density.MaxDensity; } }

            public float GetDensityFromPoint(Vector3D point)
            {
                return this.density.GetDensity(point);
            }

            public float GetIntegralFromLine(Ray ray, float rayMin, float rayMax)
            {
                float segmentLength = (rayMax - rayMin) / this.stepCount;
                float sum = 0;
                for (int i = 0; i < this.stepCount; i++)
                {
                    sum += density.GetDensity(ray.Start + ray.Direction * (rayMin + i * segmentLength + segmentLength / 2)) * segmentLength;

                    //Wenn ich hier mit der Rand-Funktion arbeite, dann sieht die Lego-Wolke heller aus, wenn ich im Framemodus oder ImageAnalyser 
                    //nur mit ein Thread rendere als wie mit 3 Threads.
                    //Wenn ich im Kästchenmodus mit mehreren Threads arbeite, sind manche Renderkästchen heller
                    //sum += density.GetDensity(ray.Start + ray.Direction * (rayMin + i * segmentLength + segmentLength * (float)rand.NextDouble())) * segmentLength;
                }
                return sum;
            }
        }
    }
}
