using GraphicGlobal;

namespace RaytracingBrdf.SampleAndRequest
{
    public interface IBrdfSampler
    {
        BrdfSampleEvent CreateDirection(BrdfPoint brdfPoint, IRandom rand);
    }
}
