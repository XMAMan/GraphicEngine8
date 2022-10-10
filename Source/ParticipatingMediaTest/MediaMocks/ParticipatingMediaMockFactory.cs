using GraphicMinimal;
using ParticipatingMedia;
using ParticipatingMedia.Media;
using ParticipatingMedia.PhaseFunctions;
using System;
using System.Collections.Generic;

namespace ParticipatingMediaTest.MediaMocks
{
    public class ParticipatingMediaMockData
    {
        public List<float> ReturnValuesForDistanceSampling;
        public List<Vector3D> ExpectedMediaPointsForDistanceSampling;
        public List<PhaseSampleResult> ReturnValuesForPhaseDirectionSampling;
        public List<Vector3D> ExpectedMediaPointsForDirectionSampling;
        public float ScatteringCoeffizient = -1;
        public float RefractionIndex = -1;
    }

    public class ParticipatingMediaMockFactory : IParticipatingMediaFactory
    {
        private readonly ParticipatingMediaMock mocki;
        public ParticipatingMediaMockFactory(ParticipatingMediaMockData data)
        {
            this.mocki = new ParticipatingMediaMock(2, data.RefractionIndex, data);
        }

        public IParticipatingMedia CreateFromDescription(BoundingBox box, IParticipatingMediaDescription mediaDescription, int priority, float refractionIndex)
        {
            if (mediaDescription != null)
            {
                if (mediaDescription is DescriptionForHomogeneousMedia) return this.mocki;
                if (mediaDescription is DescriptionForSkyMedia) return new ParticipatingMediaSky(priority, refractionIndex, mediaDescription as DescriptionForSkyMedia);
                if (mediaDescription is DescriptionForVacuumMedia) return new ParticipatingMediaVacuum(priority, refractionIndex);
                throw new Exception("Unknown media " + mediaDescription.GetType());
            }
            else
            {
                return new ParticipatingMediaVacuum(priority, refractionIndex);
            }
        }
    }
}
