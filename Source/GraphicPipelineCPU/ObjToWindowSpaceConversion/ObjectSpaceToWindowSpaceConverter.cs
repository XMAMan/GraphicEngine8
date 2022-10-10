using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using GraphicGlobal;
using GraphicPipelineCPU.Rasterizer;
using GraphicPipelineCPU.Shader;
using GraphicPipelineCPU.ObjToWindowSpaceConversion.Clipping;
using GraphicPipelineCPU.DrawingHelper;

namespace GraphicPipelineCPU.ObjToWindowSpaceConversion
{
    class ObjectSpaceToWindowSpaceConverter
    {
        //Schritte:
        //ObjectSpace -> ClipSpace
        //Clipping
        //ClipSpace -> WindowSpace
        public static List<WindowSpaceTriangle> TransformTriangleFromObjectToWindowSpace(Triangle triangle, IUniformVariables uniformVariables, VertexShaderFunc vertexShader, GeometryShaderFunc geometryShader, ViewPort viewPort)
        {
            //1. Triangle im Clipspace
            var points = ObjectToClipSpaceConverter.TransformTriangleToClipSpace(triangle,
                uniformVariables,
                vertexShader,
                geometryShader);

            //2. Clipping: Im Clipspace Triangle in Polygon zerlegen das nicht über das Viewfrustum hinaus ragt
            points = TriangleClipping.ClipPolygonOnWAxis(points);                // w=W_CLIPPING_PLANE
            points = TriangleClipping.ClipPolygonForAxis(points, Axis.X, +1);    // w=+x   
            points = TriangleClipping.ClipPolygonForAxis(points, Axis.X, -1);    // w=-x   
            points = TriangleClipping.ClipPolygonForAxis(points, Axis.Y, +1);    // w=+y   
            points = TriangleClipping.ClipPolygonForAxis(points, Axis.Y, -1);    // w=-y 
            points = TriangleClipping.ClipPolygonForAxis(points, Axis.Z, +1);    // w=+z   
            points = TriangleClipping.ClipPolygonForAxis(points, Axis.Z, -1);    // w=-z   

            if (points.Any() == false) return new List<WindowSpaceTriangle>();

            //3. Vom Clipspace in Windowspace gehen
            var windowPoints = ClipSpaceToWindowSpaceConverter.ConvertClipPointsToWindowPoints(points, viewPort);

            return Triangulate(windowPoints.ToList());
        }

        public static WindowSpaceLine TransformLineFromObjectToWindowSpace(Vector3D v1, Vector3D v2, IUniformVariables uniformVariables, VertexShaderFunc vertexShader, ViewPort viewPort)
        {
            var points = ObjectToClipSpaceConverter.TransformLineToClipSpace(v1, v2, uniformVariables, vertexShader);

            points = LineClipping.ClipLineOnWAxis(points);                // w=W_CLIPPING_PLANE
            points = LineClipping.ClipLineForAxis(points, Axis.X, +1);    // w=+x   
            points = LineClipping.ClipLineForAxis(points, Axis.X, -1);    // w=-x   
            points = LineClipping.ClipLineForAxis(points, Axis.Y, +1);    // w=+y   
            points = LineClipping.ClipLineForAxis(points, Axis.Y, -1);    // w=-y 
            points = LineClipping.ClipLineForAxis(points, Axis.Z, +1);    // w=+z   
            points = LineClipping.ClipLineForAxis(points, Axis.Z, -1);    // w=-z 

            if (points.Any() == false) return null;

            var windowPoints = ClipSpaceToWindowSpaceConverter.ConvertClipPointsToWindowPoints(points, viewPort);

            return new WindowSpaceLine(windowPoints[0], windowPoints[1]);
        }

        private static List<WindowSpaceTriangle> Triangulate(List<WindowSpacePoint> polygon)
        {
            var triangles = TriangleHelper.TransformPolygonToTriangleList(polygon.Cast<IPoint2D>().ToList());

            List<WindowSpaceTriangle> retList = new List<WindowSpaceTriangle>();
            foreach (var triangle in triangles)
            {
                //Ich erzeuge hier eine Kopie von den Dreieckspunkten, damit ReadVertex/ReadVec4 nur einmal pro Eckpunkt gerufen wird
                retList.Add(new WindowSpaceTriangle((triangle.P1 as WindowSpacePoint).GetCopy(),
                                                    (triangle.P2 as WindowSpacePoint).GetCopy(),
                                                    (triangle.P3 as WindowSpacePoint).GetCopy()
                                                             ));
            }

            return retList;
        }

        //Objektkoordinaten->[MV-Matrix des Objektes]->Weltkoordinaten->[MV-Matrix der Kamera]->Eye-Koordinaten->[Projektionsmatrix]->
        //ClipKoordinaten->[Homogeneos Clipping]->[PerspektiveDevision]->normalized devise Koordinates->
        //[Viewporttransformation]->WindowKoordinates
        //http://www.songho.ca/opengl/gl_transform.html
        //Hinweis: Das sind Homogene Koordinaten Koordinaten [x,y,z,w] -> D.h. man hat 4 Koordinaten. x,y,z sind die Richtung, w ist die Länge.
        //Will ich einen beliebigen Punkt [Px, Py, Pz] durch Homogone Koordinaten darstellen. Dann nehme ich einen Richtungsvektor, welchen vom
        //Koordinatenursprung [0,0,0] in Richtung P zeigt. Dieser Richtungsvektor hat eine beliebige Länge. Multizpliziert man diesen Richtungsvektor
        //mit der w-Koordinate, dann muss ich bei P rauskommen. Es gibt also unendlich viele Kombinationsmöglichkeiten für w und die Länge von [x,y,z]
        public static Vector3D TransformObjectSpacePositionToWindowCoordinates(Vector3D objSpacePosition, Matrix4x4 modelviewMatrix, Matrix4x4 projectionMatrix, ViewPort viewPort, out bool pointIsInScreen)
        {
            Vector4D ve = objSpacePosition.AsVector4D();
            ve = modelviewMatrix * ve;               // umrechnen von Objektkoordinaten in Eye-Koodinaten
            ve = projectionMatrix * ve;              // umrechnen von Eye-Koodinaten in Clip Koordinaten -> Clipkoordinaten heißen deswegen so, weil hier die Grafikkarte das Clipping macht(Alle Puntke, die Hinter der Kamera liegen werden an der z-Near-Plane geclipt)

            pointIsInScreen = ve.X >= -ve.W && ve.X <= +ve.W && // x liegt im Bereich -W .. +W
                              ve.Y >= -ve.W && ve.Y <= +ve.W && // y liegt im Bereich -W .. +W
                              ve.Z >= -ve.W && ve.Z <= +ve.W;   // z liegt im Bereich -W .. +W


            ve.X /= ve.W;                                     // umrechnen von Clip Koordinaten in Normalisierte Clip Koordinaten
            ve.Y /= ve.W;                                     // x, y und z liegen nun im Bereich [-1 ; +1]
            ve.Z /= ve.W;                                     // wenn z im Bereich [0 ; 1], dann ist Punkt sichtbar

            Vector3D window = viewPort.TransformIntoViewPort(new Vector3D(ve.X, ve.Y, ve.Z));

            return window;
        }
    }

    

    

    
}
