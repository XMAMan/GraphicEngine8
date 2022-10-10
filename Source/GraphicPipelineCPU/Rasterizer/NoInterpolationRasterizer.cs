using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper;
using GraphicPipelineCPU.ObjToWindowSpaceConversion;
using GraphicPipelineCPU.Shader;
using System.Drawing;

namespace GraphicPipelineCPU.Rasterizer
{
    //Wenn ich einfarbige Pixel/Linien/Dreiecke zeichnen will
    //Input: Punkt/Linie/Dreieck im Objektspace; Ausgabe: Pixelcallback im Windowspace
    public class NoInterpolationRasterizer
    {
        public delegate void PixelCallBack(Point pix, float windowZ);

        private Matrix4x4 worldViewProj;
        private ViewPort viewPort;

        public NoInterpolationRasterizer(Matrix4x4 worldViewProj, int windowWidth, int windowHeight, ImagePixelRange range)
        {
            this.worldViewProj = worldViewProj;
            this.viewPort = new ViewPort(range.Left, range.Top, range.Width, range.Height);
        }

        public void DrawLine(Vector3D v1, Vector3D v2, PixelCallBack pixelCallBack)
        {
            var clippedLine = ObjectSpaceToWindowSpaceConverter.TransformLineFromObjectToWindowSpace(v1, v2,
                new ShaderDataForLines()
                {
                    WorldViewProj = this.worldViewProj
                },
               VertexShader.VertexShaderForLines,
               this.viewPort);

            if (clippedLine == null) return;

            LineRasterizer.DrawLine(clippedLine.P1.XY, clippedLine.P2.XY, (pix, f) =>
            {
                float windowZ = clippedLine.P1.Z * (1 - f) + clippedLine.P2.Z * f; //Z-Wert(liegt zwischen 0 und 1)
                pixelCallBack(pix, windowZ);
            });
        }

        public void DrawPixel(Vector3D position, PixelCallBack pixelCallBack)
        {
            var clippedPoint = ObjectSpaceToWindowSpaceConverter.TransformObjectSpacePositionToWindowCoordinates(
                position, Matrix4x4.Ident(), this.worldViewProj, viewPort, out bool pointIsInScreen);

            if (pointIsInScreen)
            {
                pixelCallBack(new Point((int)clippedPoint.X, (int)clippedPoint.Y), clippedPoint.Z);
            }
        }
    }
}
