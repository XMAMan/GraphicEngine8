using GraphicGlobal;
using GraphicMinimal;

namespace ParticipatingMedia.DistanceSampling
{
    //Hilft dem Distancesampler(RayMarching/WoodCock) im Medium die Attenuation zu berechnen
    public interface IMediaOnWaveLength
    {
        float ExtinctionCoeffizientOnWave(Vector3D position);//AttenuationCoeffizient / ExtinctionCoeffizient
        float EvaluateAttenuationOnWave(Ray ray, float rayMin, float rayMax);
        Vector3D MaxExtinctionCoeffizient { get; }
    }
}
