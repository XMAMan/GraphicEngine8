using System.Collections.Generic;

namespace IntersectionTests.BeamLine
{
    //Das Grid ist erst so ab 100-200 Beams schneller als lineare Suche. Deswegen gibt es diese Klasse hier
    public class EasyBeamLineIntersector : IBeamLineIntersector
    {
        private List<IIntersectableCylinder> cylinders;
        public EasyBeamLineIntersector(List<IIntersectableCylinder> cylinders)
        {
            this.cylinders = cylinders;
        }
        public List<LineBeamIntersectionPoint> GetAllIntersectionPoints(IQueryLine line)
        {
            List<LineBeamIntersectionPoint> points = new List<LineBeamIntersectionPoint>();

            foreach(var cylinder in this.cylinders)
            {
                var p = cylinder.GetIntersectionPoint(line);
                if (p != null) points.Add(p);
            }

            return points;
        }
    }
}
