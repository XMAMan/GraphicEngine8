using GraphicGlobal;
using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RasterizerTest.Helper
{
    //Stellt kleine pixelige Bitmaps, wo Dreiecke drauf sind, in großer Form dar
    class SmallBitmapVisualizer
    {
        public static Bitmap TransformSmallImageToMiddleImage(Bitmap small, Triangle[] triangles)
        {
            int border = 4; //So viele Pixel Abstand zum Bildrand bleibt frei            
            int s = 25, lineWidth = 1; //Kleines Bild

            return TransformSmallImageToBigImage(small, triangles, border, s, lineWidth);
        }

        public static Bitmap TransformSmallImageToBigImage(Bitmap small, Triangle[] triangles)
        {
            int border = 4; //So viele Pixel Abstand zum Bildrand bleibt frei            
            int s = 100, lineWidth = 3; //Großes Bild

            return TransformSmallImageToBigImage(small, triangles, border, s, lineWidth);
        }

        private static Bitmap TransformSmallImageToBigImage(Bitmap small, Triangle[] triangles, int border, int s, int lineWidth)
        {
            int width = small.Width * s + border * 2;
            int height = small.Height * s + border * 2;

            var pipeline = new GraphicPipelineOpenGLv1_0.GraphicPipelineOpenGLv1_0();
            PipelineHelper.Set2DDrawingArea(pipeline, width, height);

            pipeline.ClearColorDepthAndStencilBuffer(Color.White);
            pipeline.SetColor(0, 0, 0, 1);
            pipeline.SetLineWidth(lineWidth + 2);

            for (int x = 0; x < small.Width; x++)
                for (int y = 0; y < small.Height; y++)
                {
                    Color col = small.GetPixel(x, y);
                    pipeline.SetColor(col.R / 255f, col.G / 255f, col.B / 255f, 1);
                    pipeline.DrawTriangleStrip(new Vector3D(x * s, y * s, -1), new Vector3D((x + 1) * s, y * s, -1), new Vector3D(x * s, (y + 1) * s, -1), new Vector3D((x + 1) * s, (y + 1) * s, -1));
                }

            pipeline.SetColor(0, 0, 0, 1);

            for (int x = 0; x <= small.Width; x++)
            {
                pipeline.DrawLine(new Vector3D(x * s + border, border, -1), new Vector3D(x * s + border, height - border, -1));
            }
            for (int y = 0; y <= small.Height; y++)
            {
                pipeline.DrawLine(new Vector3D(border, border + y * s, -1), new Vector3D(width - border, border + y * s, -1));
            }

            pipeline.SetLineWidth(lineWidth);
            for (int x = 0; x < small.Width; x++)
                for (int y = 0; y < small.Height; y++)
                {
                    Vector3D pixelCenter = new Vector3D(border + (x + 0.5f) * s, border + (y + 0.5f) * s, -1);
                    pipeline.DrawLine(pixelCenter - new Vector3D(0.25f * s, 0.25f * s, 0), pixelCenter + new Vector3D(0.25f * s, 0.25f * s, 0));
                    pipeline.DrawLine(pixelCenter - new Vector3D(0.25f * s, -0.25f * s, 0), pixelCenter + new Vector3D(0.25f * s, -0.25f * s, 0));
                }

            for (int x = 0; x <= small.Width; x++)
            {
                pipeline.DrawString((int)((x + 0.5f) * s + border), 6, Color.Black, 8, x.ToString());
            }
            for (int y = 0; y <= small.Height; y++)
            {
                pipeline.DrawString(6, (int)(border + (y + 0.5f) * s) - 3, Color.Black, 8, y.ToString());
            }


            pipeline.SetColor(0, 0, 0, 1);
            pipeline.SetLineWidth(lineWidth + 4);
            foreach (var t in triangles)
            {
                var triangle = new Triangle(t.V[0].Position * s + new Vector3D(border, border, -1),
                                            t.V[1].Position * s + new Vector3D(border, border, -1),
                                            t.V[2].Position * s + new Vector3D(border, border, -1));


                pipeline.DrawLine(triangle.V[0].Position, triangle.V[1].Position);
                pipeline.DrawLine(triangle.V[1].Position, triangle.V[2].Position);
                pipeline.DrawLine(triangle.V[2].Position, triangle.V[0].Position);
            }

            pipeline.FlippBuffer();
            Bitmap result = pipeline.GetDataFromColorBuffer();
            pipeline.DrawingControl.Dispose();
            return result;
        }
    }
}
