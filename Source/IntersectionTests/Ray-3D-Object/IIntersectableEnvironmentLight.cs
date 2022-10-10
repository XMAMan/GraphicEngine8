using GraphicGlobal;

namespace IntersectionTests
{
    public interface IIntersectableEnvironmentLight
    {
        bool ContainsEnvironmentLight { get; }
        IntersectionPoint GetIntersectionPointWithEnvironmentLight(Ray ray);
    }
}
