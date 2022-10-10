using GraphicGlobal;
using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper.Helper3D;
using GraphicPipelineCPU.ObjToWindowSpaceConversion;
using System.Collections.Generic;
using System.Drawing;

namespace GraphicPipelineCPU.Shader
{
    //Schritte fürs Dreieck-Zeichnen:
    //Triangle mit 3 Vertex-Punkten ergeben 3 VertexShaderInputs
    //Jedes VertexShaderInput geht in eine VertexShaderFunc rein und erzeugt ein VertexShaderOutput
    //Jedes VertexShaderOutput geht in eine GeometryShaderFunc rein und erzeugt ein modifiziertes VertexShaderOutput
    //Jedes VertexShaderOutput wird in ein InterpolationsvariablesAndHomogeneosCoordinate übersetzt (ClipSpace) und dort wird dann geclippt (Erzeugt Polygone)
    //Die geclippten InterpolationsvariablesAndHomogeneosCoordinate werden dann in VertexWithWindowPos übersetzt (Polygon)
    //Das VertexWithWindowPos-Polygon wird dann Trianguliert und in Liste von TriangleWithWindowCoorinates übersetzt
    //Der Rasterizer zeichnet dann jedes TriangleWithWindowCoorinates und es wird für jedes Pixel ein interpoliertes VertexWithWindowPos erzeugt
    //Das interpolierte VertexWithWindowPos wird dann in ein PixelShaderInput übersetzt
    //Für jedes PixelShaderInput wird die PixelShaderFunc gerufen und damit ein Farbwert erzeugt, der im Pixelbuffer dann landet

    delegate VertexShaderOutput VertexShaderFunc(VertexShaderInput input);
    delegate List<VertexShaderOutput> GeometryShaderFunc(List<VertexShaderOutput> input);
    delegate Color PixelShaderFunc(PixelShaderInput data);

    class VertexShaderInput
    {
        public Vertex Vertex; //Kommt direkt aus dem Objekt-Space-Dreieck
        public IUniformVariables UniformVariables;
    }

    class VertexShaderOutput
    {
        public Interpolationvariables Interpolationvariables { get; private set; }
        public Vector4D Vec4Position = null;                         //bei OpenGl sagt man gl_position dazu; bei DirextX ist das eine Vertex-Shader-Input-Variable vom Typ POSITION

        public VertexShaderOutput()
        {
            this.Interpolationvariables = new Interpolationvariables(1);
        }
    }

    //Stellen die Daten für ein einzelnes Pixel dar
    class PixelShaderInput
    {
        public WindowSpacePoint PixelCenter;

        public WindowSpacePoint PixelLeftTop;
        public WindowSpacePoint PixelRightTop;
        public WindowSpacePoint PixelLeftBottom;
        public WindowSpacePoint PixelRightBottom;

        public IUniformVariables UniformVariables;
        public Vector3D TriangleNormalWorldSpace;
    }
}
