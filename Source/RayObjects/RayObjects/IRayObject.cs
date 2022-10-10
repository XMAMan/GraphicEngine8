using IntersectionTests;

namespace RayObjects.RayObjects
{
    public interface IRayObject : IIntersecableObject, IUniformRandomSurfacePointCreator
    {
        //IIntersectableRayDrawingObject RayHeigh { get; }        //Diese Property ist bereits in IIntersecableObject definiert
    }
}
