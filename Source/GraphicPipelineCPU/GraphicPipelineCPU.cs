using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using GraphicMinimal;
using System.Windows.Forms;
using BitmapHelper;
using GraphicGlobal;
using GraphicPipelineCPU.Textures;
using GraphicPipelineCPU.DrawingHelper;
using GraphicPipelineCPU.DrawingHelper.Helper3D;

namespace GraphicPipelineCPU
{
    public class GraphicPipelineCPU : IGraphicPipeline
    {
        private PropertysForDrawing prop = new PropertysForDrawing();

        #region IGraphicPipeline Member
        public Control DrawingControl { get; private set; }
        public int Width
        {
            get
            {
                return DrawingControl.Width;
            }
        }
        public int Height
        {
            get
            {
                return DrawingControl.Height;
            }
        }

        public GraphicPipelineCPU(ImageLayout backgroundImageLayout = ImageLayout.None)
        {
            this.DrawingControl = new PanelWithoutFlickers() { Dock = DockStyle.Fill, BackgroundImageLayout = backgroundImageLayout };
            this.prop.DrawingArea = this.DrawingControl;
            Resize(DrawingControl.Width, DrawingControl.Height);
            if (this.DrawingControl != null) this.DrawingControl.Resize += (sender, obj) => { Resize(this.DrawingControl.Width, this.DrawingControl.Height); };
            if (this.DrawingControl != null) this.DrawingControl.Paint += (sender, obj) =>
            {
                var image = CreateBitmapFromBackgroundBuffer();
                if (image != null)
                {
                    obj.Graphics.DrawImage(BitmapHelp.SetAlpha(image, 255), 0, 0);
                }                
            };
        }

        public void Resize(int width, int height)
        {
            if (prop.StandardBuffer != null && prop.StandardBuffer.Width == width && prop.StandardBuffer.Height == height) return; //Tue nichts, wenn StandardBuffer bereits die gewünschte Größe hat

            prop.StandardBuffer = new Framebuffer(width, height);
            prop.Buffer = prop.StandardBuffer;
            SetProjectionMatrix3D();
            SetViewport(0, 0, width, height);
        }

        public void SetProjectionMatrix3D(int screenWidth = 0, int screenHight = 0, float fov = 45, float zNear = 0.001f, float zFar = 3000)
        {
            if (prop.Buffer == null || prop.Buffer.Width == 0) return;
            if (screenWidth == 0) screenWidth = prop.Buffer.Width;
            if (screenHight == 0) screenHight = prop.Buffer.Height;

            prop.ProjectionMatrix = Matrix4x4.ProjectionMatrixPerspective(fov, (float)screenWidth / (float)screenHight, zNear, zFar);
        }

        public void SetProjectionMatrix2D(float left = 0, float right = 0, float bottom = 0, float top = 0, float znear = 0, float zfar = 0)
        {
            if (left == 0 && right == 0)
            {
                prop.ZNearOrtho = -1000;
                prop.ZFarOrtho = 1000;
                prop.ProjectionMatrix = Matrix4x4.ProjectionMatrixOrtho(prop.ViewPort.Left, prop.ViewPort.Right, prop.ViewPort.Bottom, prop.ViewPort.Top, -1000.0f, +1000.0f);
            }
            else
            {
                prop.ProjectionMatrix = Matrix4x4.ProjectionMatrixOrtho(left, right, bottom, top, znear, zfar);
            }
        }

        public void SetViewport(int startX, int startY, int width, int height)
        {
            prop.ViewPort = new ViewPort(startX, startY, width, height);
        }

        public bool UseDisplacementMapping { get; set; } = false;

        public NormalSource NormalSource 
        { 
            get
            {
                return prop.NormalSource;
            }
            set
            {
                prop.NormalSource = value;
            }
        }

        public void Use2DShader()
        {
        }

        public void SetNormalInterpolationMode(InterpolationMode mode)
        {
            prop.NormalInterpolationMode = mode;
        }

        public void ClearColorBuffer(Color clearColor)
        {
            prop.Buffer.ClearColorBuffer(clearColor);
        }

        public void ClearColorDepthAndStencilBuffer(Color clearColor)
        {
            prop.Buffer.ClearColorDepthAndStencilBuffer(clearColor);
        }

        public void ClearDepthAndStencilBuffer()
        {
            prop.Buffer.ClearDepthAndStencilBuffer();
        }

        public void ClearStencilBuffer()
        {
            prop.Buffer.ClearStencilBuffer();
        }

        public void EnableWritingToTheColorBuffer()
        {
            prop.WritingToColorBuffer = true;
        }

        public void DisableWritingToTheColorBuffer()
        {
            prop.WritingToColorBuffer = false;
        }

        public void EnableWritingToTheDepthBuffer()
        {
            prop.WritingToDepthBuffer = true;
        }

        public void DisableWritingToTheDepthBuffer()
        {
            prop.WritingToDepthBuffer = false;
        }

        private Bitmap CreateBitmapFromBackgroundBuffer()
        {
            return prop.StandardBuffer.Color.GetAsBitmap();
        }

        public void FlippBuffer()
        {
            DrawingControl.Invalidate();//Lößt das Paint-Event aus. Dort wird der Farbpuffer mit DrawImage gezeichnet
        }

        public Bitmap GetDataFromColorBuffer()
        {
            return CreateBitmapFromBackgroundBuffer();
        }

        public Bitmap GetDataFromDepthBuffer()
        {
            return prop.Buffer.Depth.GetAsBitmap();
        }

        public void SetModelViewMatrixToIdentity()
        {
            prop.ModelviewMatrix = Matrix4x4.Ident();
        }

        public int GetTextureId(Bitmap bitmap)
        {
            return prop.Textures.AddColorTexture(bitmap).Id;
        }

        public Size GetTextureSize(int textureId)
        {
            return prop.Textures[textureId].GetSize();
        }

        public void SetTexture(int textureID)
        {
            prop.ActiveColorTextureDeck.Texture = prop.Textures.GetColorTexture(textureID);
        }

        public Bitmap GetTextureData(int textureID)
        {
            return prop.Textures[textureID].GetAsBitmap();
        }

        public int CreateEmptyTexture(int width, int height)
        {
            return GetTextureId(new Bitmap(width, height));
        }

        public void CopyScreenToTexture(int textureID)
        {
            prop.Textures[textureID] = new ColorTexture(textureID, CreateBitmapFromBackgroundBuffer());
        }

        public int CreateFramebuffer(int width, int height, bool withColorTexture, bool withDepthTexture)
        {
            return prop.Framebuffers.AddFramebuffer(
                withColorTexture ? prop.Textures.AddEmptyColorTexture(width, height) : null,
                withDepthTexture ? prop.Textures.AddEmptyDepthTexture(width, height) : null
                );
        }

        public void EnableRenderToFramebuffer(int framebufferId)
        {
            prop.Buffer = prop.Framebuffers[framebufferId];
            SetViewport(0, 0, prop.Framebuffers[framebufferId].Width, prop.Framebuffers[framebufferId].Height);
        }

        public void DisableRenderToFramebuffer()
        {
            prop.Buffer = prop.StandardBuffer;
            SetViewport(0, 0, this.Width, this.Height);
        }

        public int GetColorTextureIdFromFramebuffer(int framebufferId)
        {
            return prop.Framebuffers[framebufferId].Color.Id;
        }

        public int GetDepthTextureIdFromFramebuffer(int framebufferId)
        {
            return prop.Framebuffers[framebufferId].Depth.Id;
        }

        public int CreateCubeMap(int cubeMapSize = 256)
        {
            prop.Cubemaps.OldBuffer = prop.Buffer;
            return prop.Cubemaps.CreateCubeMapFrame(cubeMapSize);
        }

        public void EnableRenderToCubeMap(int cubemapID, int cubemapSide, Color clearColor)
        {
            prop.Buffer = prop.Cubemaps[cubemapID].GetFramebufferFromSide(cubemapSide);

            SetViewport(0, 0, prop.Buffer.Width, prop.Buffer.Height);
            ClearColorDepthAndStencilBuffer(clearColor);
        }

        public Bitmap GetColorDataFromCubeMapSide(int cubemapID, int cubemapSide)
        {
            return prop.Cubemaps[cubemapID].GetColorDataFromCubeMapSide(cubemapSide);
        }

        public void DisableRenderToCubeMap()
        {
            prop.Buffer = prop.Cubemaps.OldBuffer;
            SetViewport(0, 0, this.Width, this.Height);
        }

        public void EnableAndBindCubemapping(int cubemapID)
        {
            prop.CubemapTexture = prop.Cubemaps[cubemapID].Cubemap;
        }

        public void DisableCubemapping()
        {
            prop.CubemapTexture = null;
        }

        public bool ReadFromShadowmap 
        {
            set => prop.UseShadowmap = value;
        }

        public int CreateShadowmap(int width, int height)
        {
            return CreateFramebuffer(width, height, false, true);
        }

        public void EnableRenderToShadowmap(int shadowmapId)
        {
            EnableRenderToFramebuffer(shadowmapId);
            prop.RenderToShadowTexture = true; //Damit weiß ich dann, welchen Shader ich laden soll
        }

        public void BindShadowTexture(int shadowmapId)
        {
            prop.ShadowDepthTexture = prop.Framebuffers[shadowmapId].Depth;
        }

        public void DisableRenderToShadowmapTexture()
        {
            DisableRenderToFramebuffer();
            prop.RenderToShadowTexture = false;
        }

        public void SetShadowmapMatrix(Matrix4x4 shadowMatrix)
        {
            prop.ShadowMatrix = shadowMatrix;
        }

        public bool IsRenderToShadowmapEnabled()
        {
            return prop.RenderToShadowTexture;
        }

        public Bitmap GetShadowmapAsBitmap(int shadowmapId)
        {
            return GetTextureData(GetDepthTextureIdFromFramebuffer(shadowmapId));
        }

        public void SetActiveTexture0()
        {
            prop.ActiveColorTextureDeck = prop.Deck0;
        }

        public void SetActiveTexture1()
        {
            prop.ActiveColorTextureDeck = prop.Deck1;
        }

        public void EnableTexturemapping()
        {
            prop.ActiveColorTextureDeck.IsEnabled = true;
        }

        public void SetTextureFilter(TextureFilter filter)
        {
            prop.ActiveColorTextureDeck.TextureFilter = filter;
        }

        public void DisableTexturemapping()
        {
            if (prop.ActiveColorTextureDeck != null)
                prop.ActiveColorTextureDeck.IsEnabled = false;
        }

        public void SetTextureMatrix(Matrix3x3 matrix3x3)
        {
            prop.TextureMatrix = matrix3x3;
        }

        public void SetTextureScale(Vector2D scale)
        {
            prop.TexturScaleFaktor = scale;
        }

        public void SetTesselationFactor(float tesselationFactor)              // Wird bei Displacementmapping benötigt. In so viele Dreiecke wird Dreieck zerlegt
        {
            prop.CurrentTesselationFactor = tesselationFactor;
        }

        public void SetTextureHeighScaleFactor(float textureHeighScaleFactor)    // Höhenskalierung bei Displacement- und Parallaxmapping
        {
            prop.CurrentTextureHeighScaleFactor = textureHeighScaleFactor;
        }

        public void SetTextureMode(TextureMode textureMode)
        {
            prop.ActiveColorTextureDeck.TextureMode = textureMode;
        }

        public int GetTriangleArrayId(Triangle[] data)
        {
            return prop.TriangleArrays.AddTriangleArray(data);
        }

        public void DrawTriangleArray(int triangleArrayId)
        {
            new TriangleDrawer(prop).DrawTriangleArray(prop.TriangleArrays[triangleArrayId]);
        }

        public void RemoveTriangleArray(int triangleArrayId)
        {
            prop.TriangleArrays.TryToRemoveTriangleArray(triangleArrayId);
        }

        public void DrawTriangleStrip(Vector3D v1, Vector3D v2, Vector3D v3, Vector3D v4)
        {
            var drawer = new TriangleDrawer(prop);

            drawer.DrawTriangleArray(new Triangle[]
            {
                new Triangle(v1, v2, v3),
                new Triangle(v4, v3, v2)
            });
        }

        public void DrawLine(Vector3D v1, Vector3D v2)
        {
            new PointsAndLinesDrawer(prop).DrawLine(v1, v2);
        }

        public void SetLineWidth(float lineWidth)
        {
            prop.LineWidth = lineWidth * 2; //Faktor 2, damit es wie bei OpenGL 1.0 aussieht
        }

        public void DrawPoint(Vector3D position)
        {
            new PointsAndLinesDrawer(prop).DrawPoint(position);
        }

        public void SetPointSize(float size)
        {
            prop.PointSize = size;
        }

        public Color GetPixelColorFromColorBuffer(int x, int y)
        {
            return prop.Buffer.Color[x,y];
        }

        public Matrix4x4 GetInverseModelMatrix(Vector3D position, Vector3D orientation, float size)
        {
            //So lange man nur das Produkt aus Translations-Matrix * Rotations-Matrix * Skalierungs-Matrix invers bilden will, um somit von Welt- zu Objekt-Koordinaten zu transformieren, so lange kann man auch Matrix.Inverse(T*R*S) machen
            //Wenn man aber von Eye in Objekt-Koordinaten rechen will, dann scheint Matrix-Inverse(T*R*S*Kamera) nicht zu funktionieren. Was aber geht ist EyeToObj = Inverse(Kamera) * Inverse(T*R*S)
            //Dieses Wissen habe ich durch probieren beim Motionblur im Raytracer erworben
            //return Matrix.InvertMatrix(GetModelMatrix(position, orientation, size)); //Diese Zeile funktioniert auch. Man darf beide Wege nehmen, um die inverse ModelViewMatrix zu berechnen

            return Matrix4x4.InverseModel(position, orientation, size);
        }

        public Matrix4x4 GetModelMatrix(Vector3D position, Vector3D orientation, float size)
        {
            return Matrix4x4.Model(position, orientation, size);
        }

        public void PushMatrix()
        {
            prop.ModelviewMatrixStack.Push(prop.ModelviewMatrix);
        }

        public void PopMatrix()
        {
            prop.ModelviewMatrix = prop.ModelviewMatrixStack.Pop();
        }

        public void PushProjectionMatrix()
        {
            prop.ProjectionMatrixStack.Push(prop.ProjectionMatrix);
        }

        public void PopProjectionMatrix()
        {
            prop.ProjectionMatrix = prop.ProjectionMatrixStack.Pop();
        }

        public void MultMatrix(Matrix4x4 matrix)
        {
            prop.ModelviewMatrix = matrix * prop.ModelviewMatrix;
        }

        public void Scale(float size)
        {
            prop.ModelviewMatrix = Matrix4x4.Scale(size, size, size) * prop.ModelviewMatrix;
        }

        public Matrix4x4 GetProjectionMatrix()
        {
            return prop.ProjectionMatrix;
        }

        public Matrix4x4 GetModelViewMatrix()
        {
            return prop.ModelviewMatrix;
        }

        public void SetColor(float r, float g, float b, float a)
        {
            prop.CurrentColor = new Vector4D(
                Math.Max(0, Math.Min(1, r)),
                Math.Max(0, Math.Min(1, g)),
                Math.Max(0, Math.Min(1, b)),
                Math.Max(0, Math.Min(1, a))
                );
        }

        public void SetSpecularHighlightPowExponent(float specularHighlightPowExponent)
        {
            prop.SpecularHighlightPowExponent = specularHighlightPowExponent;
        }

        public void SetModelViewMatrixToCamera(Camera camera)
        {
            prop.CameraMatrix = Matrix4x4.LookAt(camera.Position, camera.Forward, camera.Up);
            prop.InverseCameraMatrix = Matrix4x4.InverseLookAt(camera.Position, camera.Forward, camera.Up);
            prop.ModelviewMatrix = prop.CameraMatrix;
            prop.CameraPosition = camera.Position;
        }

        public void SetPositionOfAllLightsources(List<RasterizerLightsource> lights)
        {
            prop.Lights = lights.Select(x => new RasterizerLightsource(x)
            {
                SpotCutoff = (float)Math.Cos(x.SpotCutoff * Math.PI / 180)
            }).ToList();
        }

        public void EnableLighting()
        {
            prop.LightingIsEnabled = true;
        }

        public void DisableLighting()
        {
            prop.LightingIsEnabled = false;
        }

        public void SetBlendingWithBlackColor()
        {
            prop.BlendingIsEnabled = true;
            prop.BlendingMode = BlendingMode.WithBlackColor;
        }

        public void SetBlendingWithAlpha()
        {
            prop.BlendingIsEnabled = true;
            prop.BlendingMode = BlendingMode.WithAlpha;
        }

        public void DisableBlending()
        {
            prop.BlendingIsEnabled = false;
            prop.Discard100Transparent = false;
        }

        public void EnableWireframe()
        {
            prop.WireframeModeIsActive = true;
        }

        public void DisableWireframe()
        {
            prop.WireframeModeIsActive = false;
        }

        public void EnableExplosionEffect()
        {
            prop.ExplosionEffectIsEnabled = true;
        }

        public void DisableExplosionEffect()
        {
            prop.ExplosionEffectIsEnabled = false;
        }

        public float ExplosionsRadius
        {
            get
            {
                return prop.ExplosionsRadius;
            }
            set
            {
                prop.ExplosionsRadius = value;
            }
        }

        public int Time // Explosionseffekt braucht Timerwert
        {
            get
            {
                return prop.Time;
            }
            set
            {
                prop.Time = value;
            }
        }

        public void EnableCullFace()
        {
            prop.CullFaceIsEnabled = true;
        }

        public void DisableCullFace()
        {
            prop.CullFaceIsEnabled = false;
        }

        public void SetFrontFaceConterClockWise()
        {
            prop.FrontFaceIsClockWise = false;
        }

        public void SetFrontFaceClockWise()
        {
            prop.FrontFaceIsClockWise = true;
        }

        public void EnableDepthTesting()
        {
            prop.DepthTestingIsEnabled = true;
        }

        public void DisableDepthTesting()
        {
            prop.DepthTestingIsEnabled = false;
        }

        public Bitmap GetStencilTestImage()
        {
            return prop.StandardBuffer.Stencil.GetAsBitmap();
        }

        public void EnableStencilTest()
        {
            prop.StenciltestIsEnabled = true;
        }

        public void DisableStencilTest()
        {
            prop.StenciltestIsEnabled = false;            
        }       

        public void SetStencilRead_NotEqualZero()
        {
            prop.StencilFunction = StencilFunction.ReadNotEqualZero;
        }

        public bool SetStencilWrite_TwoSide()
        {
            //Der Schatten vom Ring in der Ring-Kugel-Scene sieht falsch aus wenn ich das mache.
            return false;
        }

        public void SetStencilWrite_Increase()
        {
            prop.StencilFunction = StencilFunction.WriteOneSideIncrease;
        }

        public void SetStencilWrite_Decrease()
        {
            prop.StencilFunction = StencilFunction.WriteOneSideDecrease;
        }

        public void StartMouseHitTest(Point mousePosition)
        {
            prop.MouseHit.StartMouseHitTest(mousePosition, Framebuffer.ClearValueForDepth);
        }

        public void AddObjektIdForMouseHitTest(int objektId)
        {
            prop.MouseHit.CurrentObjectIdToDraw = objektId;
        }

        public int GetMouseHitTestResult()
        {
            return prop.MouseHit.GetMouseHitTestResult();
        }

        #endregion

        #region 2D

        public float ZValue2D 
        { 
            get => prop.ZValue2D; 
            set
            {
                prop.ZValue2D = value;
                prop.ZValue2DTransformed = OrthoZTransform(value);
            }
        }

        //Transformiert den Z-Wert in ein 0..1-Bereich (so wie es auch die Ortho-Matrix machen würde)
        public  float OrthoZTransform(float zValue)
        {
            return 1 - (zValue - prop.ZNearOrtho) / (prop.ZFarOrtho - prop.ZNearOrtho);
        }

        private void UseAlphaBlendingAndDiscardTransparent(Color colorFactor)
        {
            //Jemand möchte eine Figur teilweise transparent zeichnen
            if (colorFactor.A < 255)
            {
                //Es wird Alpha-Gewichtet in den ColorBuffer geschrieben
                SetBlendingWithAlpha();
            }
            else
            {
                //Nutze kein Alpha-Blending sondern zeichne überhaupt nicht in den ColorBuffer, wenn 
                //das Pixel zu 100% Transparent ist (colorFactor.A ist 255 aber im Bitmap sind manche Pixel transparent)
                DisableBlending();
                prop.Discard100Transparent = true;
            }
        }

        public void DrawLine(Pen pen, Vector2D p1, Vector2D p2)
        {
            //new PointsAndLinesDrawer(prop).DrawLine(new Vector3D(p1.X, p1.Y, 0), new Vector3D(p2.X, p2.Y, 0)); return;
            new DrawingHelper2D(prop).DrawLineWithTwoTriangles(pen, p1, p2);
        }

        public void DrawPixel(Vector2D pos, Color color, float size)
        {
            var helper = new DrawingHelper2D(prop);
            pos = helper.ViewPortTransformation(pos);
            helper.DrawPixel(pos + new Vector2D(0, 0.5f), color, size);
        }

        public Size GetStringSize(float size, string text)
        {
            return new DrawingHelper2D(prop).GetStringSize(size, text);
        }

        public void DrawString(float x, float y, Color color, float size, string text)
        {
            new DrawingHelper2D(prop).DrawString(x, y, color, size, text);
        }

        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            new DrawingHelper2D(prop).DrawRectangle(pen, x, y, width, height);
        }

        public void DrawPolygon(Pen pen, List<Vector2D> points)
        {
            new DrawingHelper2D(prop).DrawPolygon(pen, points);
        }

        public void DrawCircle(Pen pen, Vector2D pos, int radius)
        {
            new DrawingHelper2D(prop).DrawCircle(pen, pos, radius);
        }

        public void DrawFillCircle(Color color, Vector2D pos, int radius)
        {
            new DrawingHelper2D(prop).DrawFillCircle(color, pos, radius);
        }

        public void DrawCircleArc(Pen pen, Vector2D pos, int radius, float startAngle, float endAngle, bool withBorderLines)
        {
            new DrawingHelper2D(prop).DrawCircleArc(pen, pos, radius, startAngle, endAngle, withBorderLines);
        }
        public void DrawFillCircleArc(Color color, Vector2D pos, int radius, float startAngle, float endAngle)
        {
            new DrawingHelper2D(prop).DrawFillCircleArc(color, pos, radius, startAngle, endAngle);
        }

        public void DrawImage(int textureId, float x, float y, float width, float height, float sourceX, float sourceY, float sourceWidth, float sourceHeight, Color colorFactor)
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            new DrawingHelper2D(prop).DrawImage(prop.Textures.GetColorTexture(textureId), x, y, width, height, sourceX, sourceY, sourceWidth, sourceHeight);
        }

        public void DrawImage(int textureId, float x, float y, float width, float height, float sourceX, float sourceY, float sourceWidth, float sourceHeight, Color colorFactor, float zAngle, float yAngle)
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            new DrawingHelper2D(prop).DrawImage(prop.Textures.GetColorTexture(textureId), x, y, width, height, sourceX, sourceY, sourceWidth, sourceHeight, zAngle, yAngle);
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor)
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            new DrawingHelper2D(prop).DrawFillRectangle(prop.Textures.GetColorTexture(textureId), x, y, width, height);
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor, float angle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            new DrawingHelper2D(prop).DrawFillRectangle(prop.Textures.GetColorTexture(textureId), x, y, width, height, angle);
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor, float zAngle, float yAngle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            new DrawingHelper2D(prop).DrawFillRectangle(prop.Textures.GetColorTexture(textureId), x, y, width, height, zAngle, yAngle);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height)
        {
            new DrawingHelper2D(prop).DrawFillRectangle(color, x, y, width, height);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float angle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            new DrawingHelper2D(prop).DrawFillRectangle(color, x, y, width, height, angle);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float zAngle, float yAngle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            new DrawingHelper2D(prop).DrawFillRectangle(color, x, y, width, height, zAngle, yAngle);
        }

        public void DrawFillPolygon(int textureId, Color colorFactor, List<Triangle2D> triangleList)
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            new DrawingHelper2D(prop).DrawFillPolygon(prop.Textures.GetColorTexture(textureId), triangleList);
        }

        public void DrawFillPolygon(Color color, List<Triangle2D> triangleList)
        {
            new DrawingHelper2D(prop).DrawFillPolygon(color, triangleList);
        }

        public void DrawSprite(int textureId, int xCount, int yCount, int xBild, int yBild, float x, float y, float width, float height, float texBorder, Color colorFactor)
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            new DrawingHelper2D(prop).DrawSprite(prop.Textures.GetColorTexture(textureId), xCount, yCount, xBild, yBild, x, y, width, height, texBorder);
        }

        public void EnableScissorTesting(int x, int y, int width, int height)
        {
            prop.IsScissorEnabled = true;
            prop.ScissorRectangle = new Rectangle(x, y, width, height);
        }

        public void DisableScissorTesting()
        {
            prop.IsScissorEnabled = false;
        }

        #endregion
    }
}
