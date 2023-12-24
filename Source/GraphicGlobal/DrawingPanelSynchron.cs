using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GraphicMinimal;

namespace GraphicGlobal
{
    public abstract class DrawingPanelSynchron : IDrawingPanel, IDrawing3D, IDrawing2D
    {
        protected IGraphicPipeline pipeline;

        public IGraphicPipeline Pipeline { get { return this.pipeline; } }

        public DrawingPanelSynchron(IGraphicPipeline pipeline)
        {
            this.pipeline = pipeline;
        }

        public Control DrawingControl
        {
            get { return this.pipeline.DrawingControl; }
        }

        public int Width
        {
            get { return this.pipeline.Width; }
        }

        public int Height
        {
            get { return this.pipeline.Height; }
        }

        public void ClearScreen(int textureId)
        {
            this.pipeline.DrawFillRectangle(textureId, 0, 0, this.Width, this.Height, Color.FromArgb(255, 255, 255, 255));
            this.pipeline.ClearDepthAndStencilBuffer();
        }

        public void ClearScreen(Color color)
        {
            this.pipeline.ClearColorDepthAndStencilBuffer(color);
        }

        public void Enable2DModus()
        {
            this.pipeline.SetProjectionMatrix2D();
            this.pipeline.SetModelViewMatrixToIdentity();
            this.pipeline.DisableDepthTesting();
            this.pipeline.DisableTexturemapping();
            this.pipeline.DisableLighting();
            this.pipeline.DisableBlending();
            this.pipeline.SetBlendingWithAlpha(); //Neu
            this.pipeline.SetActiveTexture1();
            this.pipeline.DisableTexturemapping();
            this.pipeline.SetActiveTexture0();
            this.pipeline.DisableCullFace();
            this.pipeline.Use2DShader();
            this.pipeline.SetTextureMatrix(Matrix3x3.Ident());
            this.pipeline.SetTextureScale(new Vector2D(1, 1));
            this.pipeline.SetTextureFilter(TextureFilter.Point);
        }

        public abstract void Draw3DObjects(Frame3DData data);
        public abstract DrawingObject MouseHitTest(Frame3DData data, Point mousePosition);

        public void FlipBuffer()
        {
            this.pipeline.FlippBuffer();
        }

        

        public Bitmap GetDataFromFrontbuffer()
        {
            //Weg 1 (Farbpuffer auslesen)   (Geht bei allen 4 Modien)
            return this.pipeline.GetDataFromColorBuffer();

            //Weg 2 (Farbpuffer auf Textur kopieren und diese dann auslesen)        (Geht bei allen 4 Modien)
            //int textureId = this.pipeline.CreateEmptyTexture(this.Width, this.Height);
            //this.pipeline.CopyScreenToTexture(textureId);
            //return this.pipeline.GetTextureData(textureId);

            //So können verschiedene Wege aussehen, wie man an den Farbpuffer gelangt
            //Weg 1 (Farbpuffer auslesen)                                           (Geht bei allen 4 Modien so lange man bei den PixelShader die w-Componente (Alpha) für den Farbwert auf 1 setzt)
            //return this.GetPanel().Pipeline.GetDataFromColorBuffer();           //Geht nicht bei OpenGL wenn ich farbige Linien habe -> Wenn ich das so mache, fällt der RasterizerImageTests.ShadowsAndBlending-Test um, da die Linien beim Spiegelrand weiß anstatt rot sind
            //return this.GetPanel().Pipeline.GetDataFromDepthBuffer();           //Bei DirectX kommt komischerweise der Farbpuffer zurück

            //Weg 2 (Farbpuffer auf Textur kopieren und diese dann auslesen)        (Geht bei allen 4 Modien)
            //int textureId = this.GetPanel().Pipeline.CreateEmptyTexture(this.Width, this.Height);
            //this.GetPanel().Pipeline.CopyScreenToTexture(textureId);
            //return this.GetPanel().Pipeline.GetTextureData(textureId);

            //Weg 3 (In Textur anstatt in Farbpuffer rendern)                       (Geht bei allen 4 Modien so lange kein Shadowmapping verwendet wird)
            /*this.GlobalSettings.SchattenArtOpenGlDirectX = SchattenArtOpenGlDirectX.Stencil;
            int framebufferId = this.GetPanel().Pipeline.CreateFramebuffer(this.Width, this.Height, true, true);
            this.GetPanel().Pipeline.EnableRenderToFramebuffer(framebufferId);
            DrawAndFlip();
            this.GetPanel().Pipeline.DisableRenderToFramebuffer();
            int colorTexture = this.GetPanel().Pipeline.GetColorTextureIdFromFramebuffer(framebufferId); //Es geht bei CPU,DirectX,OpenGL3.0; Bei beiden OpenGL1.0 sieht der Hintergrund grau aus. Außerdem scheint der Stencilpuffer nicht zu funktionieren (Alles ist gespiegel und das Grau kommt vom Stencil-Schatten)
            //int colorTexture = this.GetPanel().Pipeline.GetDepthTextureIdFromFramebuffer(framebufferId); //Es geht bei DirectX; Bei CPU und OpenGL3-0 fehlt der Cubemapwürfel; OpenGL 1.0 liefert weißes Bild
            return this.GetPanel().Pipeline.GetTextureData(colorTexture);*/

        }

        public float ZValue2D
        {
            get => this.pipeline.ZValue2D;
            set => this.pipeline.ZValue2D = value;
        }

        public void EnableDepthTesting()
        {
            this.pipeline.EnableDepthTesting();
        }
        public void DisableDepthTesting()
        {
            this.pipeline.DisableDepthTesting();
        }
        public void EnableWritingToTheDepthBuffer()
        {
            this.pipeline.EnableWritingToTheDepthBuffer();
        }
        public void DisableWritingToTheDepthBuffer()
        {
            this.pipeline.DisableWritingToTheDepthBuffer();
        }
        
        public void DrawLine(Pen pen, Vector2D p1, Vector2D p2)
        {
            this.pipeline.DrawLine(pen, p1, p2);
        }

        public void DrawPixel(Vector2D pos, Color color, float size)
        {
            this.pipeline.DrawPixel(pos, color, size);
        }

        public Size GetStringSize(float size, string text)
        {
            return this.pipeline.GetStringSize(size, text);
        }

        public void DrawString(float x, float y, Color color, float size, string text)
        {
            this.pipeline.DrawString(x, y, color, size, text);
        }

        public void DrawRectangle(Pen pen, int x, int y, int width, int height)
        {
            this.pipeline.DrawRectangle(pen, x, y, width, height);
        }

        public void DrawImage(int textureId, int x, int y, int width, int height, int sourceX, int sourceY, int sourceWidth, int sourceHeight, Color colorFactor)
        {
            this.pipeline.DrawImage(textureId, x, y, width, height, sourceX, sourceY, sourceWidth, sourceHeight, colorFactor);
        }

        public void DrawImage(int textureId, int x, int y, int width, int height, int sourceX, int sourceY, int sourceWidth, int sourceHeight, Color colorFactor, float zAngle, float yAngle)
        {
            this.pipeline.DrawImage(textureId, x, y, width, height, sourceX, sourceY, sourceWidth, sourceHeight, colorFactor, zAngle, yAngle);
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor)
        {
            this.pipeline.DrawFillRectangle(textureId, x, y, width, height, colorFactor);
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor, float angle)
        {
            this.pipeline.DrawFillRectangle(textureId, x, y, width, height, colorFactor, angle);
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor, float zAngle, float yAngle)
        {
            this.pipeline.DrawFillRectangle(textureId, x, y, width, height, colorFactor, zAngle, yAngle);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height)
        {
            this.pipeline.DrawFillRectangle(color, x, y, width, height);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float angle)
        {
            this.pipeline.DrawFillRectangle(color, x, y, width, height, angle);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float zAngle, float yAngle)
        {
            this.pipeline.DrawFillRectangle(color, x, y, width, height, zAngle, yAngle);
        }

        public void DrawPolygon(Pen pen, List<Vector2D> points)
        {
            this.pipeline.DrawPolygon(pen, points);
        }

        public void DrawFillPolygon(int textureId, Color colorFactor, List<Triangle2D> triangleList)
        {
            this.pipeline.DrawFillPolygon(textureId, colorFactor, triangleList);
        }

        public void DrawFillPolygon(Color color, List<Triangle2D> triangleList)
        {
            this.pipeline.DrawFillPolygon(color, triangleList);
        }

        public void DrawCircle(Pen pen, Vector2D pos, int radius)
        {
            this.pipeline.DrawCircle(pen, pos, radius);
        }

        public void DrawFillCircle(Color color, Vector2D pos, int radius)
        {
            this.pipeline.DrawFillCircle(color, pos, radius);
        }

        public void DrawCircleArc(Pen pen, Vector2D pos, int radius, float startAngle, float endAngle, bool withBorderLines)
        {
            this.pipeline.DrawCircleArc(pen, pos, radius, startAngle, endAngle, withBorderLines);
        }
        public void DrawFillCircleArc(Color color, Vector2D pos, int radius, float startAngle, float endAngle)
        {
            this.pipeline.DrawFillCircleArc(color, pos, radius, startAngle, endAngle);
        }

        public void DrawSprite(int textureId, int xCount, int yCount, int xBild, int yBild, int x, int y, int width, int height, Color colorFactor)
        {
            this.pipeline.DrawSprite(textureId, xCount, yCount, xBild, yBild, x, y, width, height, colorFactor);
        }

        public void EnableScissorTesting(int x, int y, int width, int height)
        {
            this.pipeline.EnableScissorTesting(x, y, width, height);
        }

        public void DisableScissorTesting()
        {
            this.pipeline.DisableScissorTesting();
        }

        public int GetTextureId(Bitmap bitmap)
        {
            return this.pipeline.GetTextureId(bitmap);
        }

        public Bitmap GetTextureData(int textureID)
        {
            return this.pipeline.GetTextureData(textureID);
        }

        public int CreateFramebuffer(int width, int height, bool withColorTexture, bool withDepthTexture)
        {
            return this.pipeline.CreateFramebuffer(width, height, withColorTexture, withDepthTexture);
        }

        public void EnableRenderToFramebuffer(int framebufferId)
        {
            this.pipeline.EnableRenderToFramebuffer(framebufferId);
        }

        public void DisableRenderToFramebuffer()
        {
            this.pipeline.DisableRenderToFramebuffer();
        }

        public int GetColorTextureIdFromFramebuffer(int framebufferId)
        {
            return this.pipeline.GetColorTextureIdFromFramebuffer(framebufferId);
        }
    }
}
