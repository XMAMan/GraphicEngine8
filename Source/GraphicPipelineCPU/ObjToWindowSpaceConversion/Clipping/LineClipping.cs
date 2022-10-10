using GraphicPipelineCPU.DrawingHelper.Helper3D;
using System.Collections.Generic;
using System.Linq;

namespace GraphicPipelineCPU.ObjToWindowSpaceConversion.Clipping
{
    static class LineClipping
    {

        private static float W_CLIPPING_PLANE = 0.00001f;
        public static List<ClipSpacePoint> ClipLineOnWAxis(List<ClipSpacePoint> polygon)
        {
            List<ClipSpacePoint> newPolygon = new List<ClipSpacePoint>();

            bool p1IsInside = polygon[0].W < W_CLIPPING_PLANE ? false : true;
            bool p2IsInside = polygon[1].W < W_CLIPPING_PLANE ? false : true;

            if (p1IsInside != p2IsInside)
            {
                //Hier lande ich nur, wenn ich eine Perspectivische Matrix nehme und ein Punkt vor und einer hinter der zNear-Ebene liegt
                //Die z-Koordinante, die auf der zNear-Ebene liegt wird dann zu 0

                //Need to clip against plan w=0
                float intersectionFactor = (polygon[0].W) / (polygon[0].W - polygon[1].W);
 
                var intersectionPoint = polygon[0].Position * (1 - intersectionFactor) + polygon[1].Position * intersectionFactor;

                // Insert
                newPolygon.Add(new ClipSpacePoint(Interpolationvariables.InterpolateLinear(polygon[0].Interpolationvariables, polygon[1].Interpolationvariables, intersectionFactor), intersectionPoint));
            }

            if (p1IsInside)
            {
                newPolygon.Add(polygon[0]);
            }

            if (p2IsInside)
            {
                newPolygon.Add(polygon[1]);
            }

            return newPolygon;
        }

        public static List<ClipSpacePoint> ClipLineForAxis(List<ClipSpacePoint> polygon, Axis axis, float signFromAxis)
        {
            List<ClipSpacePoint> newPolygon = new List<ClipSpacePoint>();

            if (polygon.Any() == false) return newPolygon;

            bool p1IsInside = polygon[0].GetAxis(axis, signFromAxis) < polygon[0].W ? true : false;
            bool p2IsInside = polygon[1].GetAxis(axis, signFromAxis) < polygon[1].W ? true : false;

            if (p1IsInside != p2IsInside)
            {
                float intersectionFactor = (polygon[0].W - polygon[0].GetAxis(axis, signFromAxis)) /
                        ((polygon[0].W - polygon[0].GetAxis(axis, signFromAxis)) - (polygon[1].W - polygon[1].GetAxis(axis, signFromAxis)));

                var intersectionPoint = polygon[0].Position * (1 - intersectionFactor) + polygon[1].Position * intersectionFactor;

                // Insert
                newPolygon.Add(new ClipSpacePoint(Interpolationvariables.InterpolateLinear(polygon[0].Interpolationvariables, polygon[1].Interpolationvariables, intersectionFactor), intersectionPoint)); //Wenn ich das so mache, dann wir
            }

            if (p1IsInside)
            {
                newPolygon.Add(polygon[0]);
            }

            if (p2IsInside)
            {
                newPolygon.Add(polygon[1]);
            }

            return newPolygon;
        }
    }
}
