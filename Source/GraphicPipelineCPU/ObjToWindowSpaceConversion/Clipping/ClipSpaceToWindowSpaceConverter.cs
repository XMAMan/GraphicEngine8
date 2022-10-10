using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper;
using System.Collections.Generic;
using System.Linq;

namespace GraphicPipelineCPU.ObjToWindowSpaceConversion.Clipping
{
    static class ClipSpaceToWindowSpaceConverter
    {
        public static List<WindowSpacePoint> ConvertClipPointsToWindowPoints(List<ClipSpacePoint> points, ViewPort viewPort)
        {
            return points.Select(x => new WindowSpacePoint(TransformClipSpaceToWindowSpace(x.Position, viewPort), x.Interpolationvariables)).ToList();
        }

        private static Vector3D TransformClipSpaceToWindowSpace(Vector4D clipCoordinate, ViewPort viewPort)
        {
            Vector3D v = clipCoordinate.XYZ / clipCoordinate.W;

            return viewPort.TransformIntoViewPort(v);
        }
    }
}
