using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphicGlobal;

namespace LineIntersectionTest
{
    public interface ILineLineIntersection
    {
        List<LineLineIntersectionResult> GetIntersections(Ray querryRay, float rayLength);
        List<ILine> RawList { get; }
    }

    public interface ILine
    {
        Ray Ray { get; }
        float RayLength { get; }
        LineLineIntersectionResult GetIntersection(Ray querryRay, float rayLength);
    }

    public class LineLineIntersectionResult
    {
        public ILine Line;
        public float LineIntersectionPosition;
        public float RayIntersectionPosition;
        public float Distance;
        public float LineRadiusByIntersectionPoint;
        public float SinTheta;
        public Ray QuerryRay;
    }
}
