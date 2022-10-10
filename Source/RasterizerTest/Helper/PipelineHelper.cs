using GraphicGlobal;
using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RasterizerTest.Helper
{
    class PipelineHelper
    {
        public static void Set2DDrawingArea(IGraphicPipeline pipeline, int width, int height)
        {
            pipeline.DrawingControl.Width = width;
            pipeline.DrawingControl.Height = height;

            pipeline.SetProjectionMatrix2D();
            pipeline.SetModelViewMatrixToIdentity();
            pipeline.DisableDepthTesting();
            pipeline.DisableTexturemapping();
            pipeline.DisableLighting();
            pipeline.DisableBlending();
            pipeline.SetBlendingWithAlpha(); //Neu
            pipeline.SetActiveTexture1();
            pipeline.DisableTexturemapping();
            pipeline.SetActiveTexture0();
            pipeline.DisableCullFace();
            pipeline.Use2DShader();
            pipeline.SetTextureMatrix(Matrix3x3.Ident());
            pipeline.SetTextureScale(new Vector2D(1, 1));
            pipeline.SetTextureFilter(TextureFilter.Point);
        }
    }
}
