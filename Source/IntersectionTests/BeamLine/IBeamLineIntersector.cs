using System.Collections.Generic;

namespace IntersectionTests.BeamLine
{
    public interface IBeamLineIntersector
    {
        List<LineBeamIntersectionPoint> GetAllIntersectionPoints(IQueryLine line);
    }
}
