using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper.Helper3D;
using System.Collections.Generic;
using System.Linq;

namespace GraphicPipelineCPU.ObjToWindowSpaceConversion.Clipping
{
    static class TriangleClipping
    {
        private static float W_CLIPPING_PLANE = 0.00001f;

        //http://wiki.delphigl.com/index.php/Clipping_Plane
        //Clipping -> Für X,Y,Z muss gelten, dass sie im Bereich -W bis +W liegen (W muss Positiv sein. Wenn W Negativ ist, dann liegt die Z-Koordinate hinter dem Betrachter)
        //https://www.opengl.org/wiki/Vertex_Post-Processinghttps://www.opengl.org/wiki/Vertex_Post-Processing -> Hier steht, dass es für Linien und Dreiecke unterschiedliche Clippingroutingen gibt
        //https://www.opengl.org/discussion_boards/showthread.php/169177-OpenGL-clipping-how-is-it-done -> Hier stehen folgende Schlagwörter: "homogenous clipping" "Sutherland-Hodgman polygon clipping"
        //Clipping-Beschreibungen
        //http://fabiensanglard.net/polygon_codec/ -> Wo in der Grafikpipline ordnet sich Clipping ein. Was bedeutet die W-Koordinate? Wie kann man sich die Perspektifische Division grafisch vorstellen?
        //http://research.microsoft.com/pubs/73937/p245-blinn.pdf -> CLIPPING USING HOMOGENEOUS COORDINATES. So geht das
        //http://fabiensanglard.net/polygon_codec/clippingdocument/Clipping.pdf -> Weit
        public static List<ClipSpacePoint> ClipPolygonOnWAxis(List<ClipSpacePoint> polygon)
        {
            int index = 0;
            List<ClipSpacePoint> newPolygon = new List<ClipSpacePoint>();

            ClipSpacePoint currentVertice;
            ClipSpacePoint previousVertice;

            bool previousPointIsInside;
            bool currentPointIsInside;

            float intersectionFactor;
            Vector4D intersectionPoint;

            previousVertice = polygon.Last();
            previousPointIsInside = previousVertice.W < W_CLIPPING_PLANE ? false : true;
            currentVertice = polygon[index];
            while (true)
            {
                currentPointIsInside = currentVertice.W < W_CLIPPING_PLANE ? false : true;

                if (previousPointIsInside != currentPointIsInside)
                {
                    //Die Formel, um einen Schnittpunkt einer Homo-Linie mit der w=0-Plane zu machen steht hier bei Seite 14 -> http://fabiensanglard.net/polygon_codec/clippingdocument/Clipping.pdf

                    //Need to clip against plan w=0
                    intersectionFactor = (previousVertice.W) / (previousVertice.W - currentVertice.W);

                    //Laut dieser Seite muss die Zeile hier W_CLIPPING_PLANE mit enthalten: https://fabiensanglard.net/polygon_codec/
                    //Wenn ich das aber mache, erzeugt der AnisotrophTextureFilter ein falsches Bild
                    //intersectionFactor = (W_CLIPPING_PLANE - previousVertice.W) / (previousVertice.W - currentVertice.W);

                    intersectionPoint = previousVertice.Position * (1 - intersectionFactor) + currentVertice.Position * intersectionFactor;

                    // Insert
                    newPolygon.Add(new ClipSpacePoint(Interpolationvariables.InterpolateLinear(previousVertice.Interpolationvariables, currentVertice.Interpolationvariables, intersectionFactor), intersectionPoint));
                }

                if (currentPointIsInside)
                {
                    // Insert
                    newPolygon.Add(currentVertice);
                }

                previousPointIsInside = currentPointIsInside;

                //Move forward
                previousVertice = currentVertice;
                if (currentVertice == polygon.Last()) break;
                currentVertice = polygon[++index];
            }

            return newPolygon;
        }

        public static List<ClipSpacePoint> ClipPolygonForAxis(List<ClipSpacePoint> polygon, Axis axis, float signFromAxis)
        {
            int index = 0;
            List<ClipSpacePoint> newPolygon = new List<ClipSpacePoint>();

            ClipSpacePoint currentVertice;
            ClipSpacePoint previousVertice;

            bool previousPointIsInside;
            bool currentPointIsInside;

            float intersectionFactor;
            Vector4D intersectionPoint;

            if (polygon.Any() == false) return polygon;

            //Clip against first plane
            previousVertice = polygon.Last();
            previousPointIsInside = previousVertice.GetAxis(axis, signFromAxis) <= previousVertice.W ? true : false;
            currentVertice = polygon[0];
            while (true)
            {
                currentPointIsInside = currentVertice.GetAxis(axis, signFromAxis) <= currentVertice.W ? true : false;

                if (previousPointIsInside != currentPointIsInside)
                {
                    //Die Formel, um eine Homo-Linie mit der W=y-Plane zu machen steht hier bei Seite 15 -> http://fabiensanglard.net/polygon_codec/clippingdocument/Clipping.pdf

                    //Need to clip against plan w=0

                    intersectionFactor = (previousVertice.W - previousVertice.GetAxis(axis, signFromAxis)) /
                        ((previousVertice.W - previousVertice.GetAxis(axis, signFromAxis)) - (currentVertice.W - currentVertice.GetAxis(axis, signFromAxis)));

                    intersectionPoint = previousVertice.Position * (1 - intersectionFactor) + currentVertice.Position * intersectionFactor;

                    // Insert
                    newPolygon.Add(new ClipSpacePoint(Interpolationvariables.InterpolateLinear(previousVertice.Interpolationvariables, currentVertice.Interpolationvariables, intersectionFactor), intersectionPoint));
                }

                if (currentPointIsInside)
                {
                    // Insert
                    newPolygon.Add(currentVertice);
                }

                previousPointIsInside = currentPointIsInside;

                //Move forward
                previousVertice = currentVertice;
                if (currentVertice == polygon.Last()) break;
                currentVertice = polygon[++index];
            }

            return newPolygon;
        }
    }
}
