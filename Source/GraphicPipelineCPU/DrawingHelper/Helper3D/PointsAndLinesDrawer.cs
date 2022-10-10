using GraphicMinimal;
using GraphicPipelineCPU.ObjToWindowSpaceConversion;
using GraphicPipelineCPU.Rasterizer;
using GraphicPipelineCPU.Shader;

namespace GraphicPipelineCPU.DrawingHelper.Helper3D
{
    class PointsAndLinesDrawer
    {
        private PropertysForDrawing prop = null;

        public PointsAndLinesDrawer(PropertysForDrawing prop)
        {
            this.prop = prop;
        }

        public void DrawLine(Vector3D v1, Vector3D v2)
        {
            var line = ObjectSpaceToWindowSpaceConverter.TransformLineFromObjectToWindowSpace(v1, v2,
                new ShaderDataForLines()
                {
                    WorldViewProj = prop.ModelviewMatrix * prop.ProjectionMatrix
                },
               VertexShader.VertexShaderForLines,
               prop.ViewPort);
            if (line == null) return;

            WindowSpacePoint w1 = line.P1;
            WindowSpacePoint w2 = line.P2;

            LineRasterizer.DrawLine(w1.XY, w2.XY, (pix, f) =>
            {
                if (pix.X >= 0 && pix.X < prop.Buffer.Width && pix.Y >= 0 && pix.Y < prop.Buffer.Height)
                {
                    float windowZ = w1.Z * (1 - f) + w2.Z * f; //Z-Wert(liegt zwischen 0 und 1)
                    DrawPixel(pix.X, pix.Y, windowZ, prop.LineWidth);
                }
            });
        }

        public void DrawPoint(Vector3D position)
        {
            bool isPointInScreen;
            Vector3D w = ObjectSpaceToWindowSpaceConverter.TransformObjectSpacePositionToWindowCoordinates(position, prop.ModelviewMatrix, prop.ProjectionMatrix, prop.ViewPort, out isPointInScreen);
            if (isPointInScreen == false) return;
            DrawPixel((int)(w.X + 0.5f), (int)(w.Y + 0.5f), w.Z, prop.PointSize);
        }

        private void DrawPixel(int windowX, int windowY, float windowZ, float size)
        {
            if (windowX < 0 || windowX >= prop.Buffer.Width) return;
            if (windowY < 0 || windowY >= prop.Buffer.Height) return;

            if (prop.DepthTestingIsEnabled)                       // Dephtest
            {
                if (windowZ < 0 || windowZ > 1) return;           //Pixel liegt nicht im Sichtbereich
            }

            for (int wx1 = (int)(windowX - size / 4.0f); wx1 < (int)(windowX + size / 4.0f); wx1++)
                for (int wy1 = (int)(windowY - size / 4.0f); wy1 < (int)(windowY + size / 4.0f); wy1++)
                    if (wx1 >= 0 && wx1 < prop.Buffer.Width && wy1 >= 0 && wy1 < prop.Buffer.Height)
                    {
                        if ((prop.DepthTestingIsEnabled && prop.Buffer.Depth[wx1, wy1] > windowZ) || prop.DepthTestingIsEnabled == false) //Liegt dort noch kein Pixel?
                        {
                            //Schreibe Tiefenwert
                            if (prop.WritingToDepthBuffer)
                            {
                                prop.Buffer.Depth[wx1, wy1] = windowZ;
                            }

                            //Schreibe Farbwert
                            if (prop.WritingToColorBuffer)
                            {
                                prop.Buffer.Color[wx1, wy1] = prop.CurrentColor.ToColor();
                            }                                
                        }
                    }
        }
    }
}
