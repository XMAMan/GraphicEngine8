namespace IntersectionTests.BeamLine
{
    public class LineBeamIntersectionPoint
    {
        public IIntersectableCylinder IntersectedBeam;
        public IQueryLine QueryLine;
        public float LineIntersectionPosition;  //Distanz zwischen QueryLine.Start und ShortestPointOnQueryLine
        public float BeamIntersectionPosition;
        public float Distance;                  //Distanz zwischen den beiden Linien (ShortestPointOnQueryLine - ShortestPointOnIntersectedBeam)
        public float SinTheta;
    }
}
