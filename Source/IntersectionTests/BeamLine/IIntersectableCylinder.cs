using GraphicGlobal;
using GraphicMinimal;

namespace IntersectionTests.BeamLine
{
    public interface IIntersectableCylinder
    {
        Ray Ray { get; } //Startpunkt + Richtung des Cylinders
        float Length { get; }
        float Radius { get; }
        float RadiusSqrt { get; }
        BoundingBox GetAxialAlignedBoundingBox();
        NonAlignedBoundingBox GetNonAlignedBoundingBox();
        LineBeamIntersectionPoint GetIntersectionPoint(IQueryLine line);
    }
}
