//https://github.com/opentk/opentk-dependencies -> Von diesen Dlls ist OpenTK abhängig
//Damit OpenTK im Debug-Mode keine Exception wirft, habe ich all die Dlls von hier
//C:\Data\C#\OpenTK\opentk-dependencies-master(Abhängige Dlls für 32 und 64 Bit).zip\opentk-dependencies-master\x86
//nach hier:
//C:\Windows\SysWOW64
//kopiert

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using GraphicMinimal;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.ComponentModel;
using System.Runtime.InteropServices;
using BitmapHelper;
using GraphicGlobal;
using GraphicGlobal.Rasterizer2DFunctions;

namespace GraphicPipelineOpenGLv3_0
{
    public class GraphicPipelineOpenGLv3_0 : IGraphicPipeline
    {
        class MyTexture
        {
            public enum TexturType
            {
                Color2D,
                Depth2D
            }
            public int TextureID;
            public int Width, Height;   // Wird benötigt, um bei GetTextureData die Textur aus der Grafikkarte wieder auszulesen
            public TexturType TextureType;

            public MyTexture(int textureId, int width, int height, TexturType type)
            {
                this.TextureID = textureId;
                this.Width = width;
                this.Height = height;
                this.TextureType = type;
            }
        }

        class Cubemap
        {
            public int[] FBO;
            public int[] ColorTexture;
            public int DepthTexture;
            public int CubeMapSize;
        }

        #region Variablen
        private OpenTK.GLControl simpleOpenGlControl = null;
        private List<MyTexture> texturen = new List<MyTexture>(); //Texturedateipfad+Texturgröße | Gl.glGenTextures()
        private Dictionary<int, Cubemap> cubemaps = new Dictionary<int, Cubemap>(); //[TexturID | 6 2D-Texturen]
        private int activeTexture = 0;
        //private int[] selectBufferForMouseHitTest;
        private ShaderHelper shader = null;
        private Dictionary<string, int> texte = new Dictionary<string, int>(); //Hier werden die Textur-IDs für die Texte gespeichert
        
        private Matrix4 modelViewMatrix, projectionMatrix;
        private Stack<Matrix4> modelViewMatrixStack = new Stack<Matrix4>();
        private Stack<Matrix4> projectionMatrixStack = new Stack<Matrix4>();
        #endregion

        public GraphicPipelineOpenGLv3_0()
        {
            this.simpleOpenGlControl = CreateControl();
            simpleOpenGlControl.SwapBuffers();
            simpleOpenGlControl.SizeChanged += new EventHandler(resize);
            simpleOpenGlControl.Paint += new PaintEventHandler(simpleOpenGlControl_Paint);

            resize(null, null);
            shader = new ShaderHelper();
            GL.Disable(EnableCap.Multisample); //Antialiasing im Colorpuffer
        }

        //Wenn ich z.B. den NoWindowRoom-Test ausführe, dann steht folgender Fehler im Ausgabefenster:
        //[Warning] OpenGL context 131073 leaked. Did you forget to call IGraphicsContext.Dispose()?
        //Deswegen versuche ich hiermit Dispose zu rufen aber Fehler kommt immer noch
        /*~GraphicPipelineOpenGLv3_0()
        {
            shader.Dispose();
            this.simpleOpenGlControl.Dispose();
            OpenTK.Graphics.GraphicsContext.CurrentContext.Dispose();
        }*/

        private static OpenTK.GLControl CreateControl()
        {
            return new OpenTK.GLControl()
            {
                BackColor = System.Drawing.Color.Black,
                Dock = System.Windows.Forms.DockStyle.Fill,
                Location = new System.Drawing.Point(0, 0),
                Margin = new System.Windows.Forms.Padding(4, 4, 4, 4),
                Name = "glControl1",
                Size = new System.Drawing.Size(282, 255),
                TabIndex = 0,
                VSync = false,
            };
        }

        #region IGraphicPipeline Member

        public int Width
        {
            get { return simpleOpenGlControl.Width; }
        }

        public int Height
        {
            get { return simpleOpenGlControl.Height; }
        }

        void simpleOpenGlControl_Paint(object sender, PaintEventArgs e)
        {
            simpleOpenGlControl.SwapBuffers();
        }

        public Control DrawingControl
        {
            get
            {
                return this.simpleOpenGlControl;
            }
        }
        public bool UseDisplacementMapping 
        { 
            get
            {
                return this.shader.Mode == ShaderHelper.ShaderMode.Displacement;
            }
            set
            {
                if (value) shader.Mode = ShaderHelper.ShaderMode.Displacement;
            }
        }

        private NormalSource normalSource = NormalSource.ObjectData;
        public NormalSource NormalSource
        {
            get
            {
                return this.normalSource;
            }
            set
            {
                this.normalSource = value;

                if (this.normalSource == NormalSource.ObjectData || this.normalSource == NormalSource.Normalmap)
                    shader.Mode = ShaderHelper.ShaderMode.Normal;

                if (this.normalSource == NormalSource.Parallax)
                    shader.Mode = ShaderHelper.ShaderMode.Parallax;
            }
        }
        public void Use2DShader()
        {
            shader.Mode = ShaderHelper.ShaderMode.None;
        }

        public void SetNormalInterpolationMode(GraphicMinimal.InterpolationMode mode)
        {
            if (mode == InterpolationMode.Flat)
            {
                shader["DoFlatShading"] = 1;
                GL.ShadeModel(ShadingModel.Flat);
                GL.ProvokingVertex(ProvokingVertexMode.FirstVertexConvention);//Von diesen Vertex wird der Normalvektor für alle Pixelshader-Inputs genommen
            }
            else
            {
                shader["DoFlatShading"] = 0;
                GL.ShadeModel(ShadingModel.Smooth);
            }
        }

        public void Resize(int width, int height)
        {
            //Tue nichts, da nur der Veränderung des SimpleOpenGlControl(DrawingArea)-Objektes zum Resize führt
        }

        private void resize(object sender, EventArgs e)
        {
            GL.ClearColor(1.0f, 1.0f, 1.0f, 0.5f);					// black background
            GL.Enable(EnableCap.Texture2D);
                        
            GL.ClearDepth(1.0f);								    // depth buffer setup
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest); // nice perspective calculations

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.AlphaFunc(AlphaFunction.Greater, 0.01f);
            GL.Enable(EnableCap.AlphaTest);

            GL.LineWidth(5);
            GL.LineStipple(1, unchecked((short)0xAAAA));

            SetProjectionMatrix3D();
            GL.Viewport(0, 0, simpleOpenGlControl.Width, simpleOpenGlControl.Height);
   
            modelViewMatrix = Matrix4.Identity;
            UpdateModelViewMatrix();
        }

        public void ClearColorBuffer(Color clearColor)
        {
            GL.ClearColor(clearColor);
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }

        public void ClearColorDepthAndStencilBuffer(Color clearColor)
        {
            //GL.ClearColor(clearColor); //Hier wird der Alpha-Wert nicht mit übergeben
            GL.ClearColor(clearColor.R / 255f, clearColor.G / 255f, clearColor.B / 255f, clearColor.A / 255f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        }

        public void ClearStencilBuffer()
        {
            GL.Clear(ClearBufferMask.StencilBufferBit); 
        }

        public void ClearDepthAndStencilBuffer()
        {
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        }

        public void EnableWritingToTheColorBuffer()
        {
            GL.ColorMask(true, true, true, true); //bestimmt, welche der Farbkomponenten in den Framebuffer geschrieben werden können.
        }

        public void DisableWritingToTheColorBuffer()
        {
            GL.ColorMask(false, false, false, false);
        }

        public void EnableWritingToTheDepthBuffer()
        {
            GL.DepthMask(true);
        }

        public void DisableWritingToTheDepthBuffer()
        {
            GL.DepthMask(false);
        }

        public void FlippBuffer()
        {
            simpleOpenGlControl.MakeCurrent();
            GL.Flush();
            simpleOpenGlControl.Invalidate();
        }

        //Ein OpenGL-SceenShoot klappt nur, wenn die Beleuchtung an ist
        public Bitmap GetDataFromColorBuffer()
        {
            System.Drawing.Imaging.BitmapData bitmapdata = new System.Drawing.Imaging.BitmapData();
            Bitmap image = new Bitmap(this.DrawingControl.Width, this.DrawingControl.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                bitmapdata = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }
            catch (Exception) { return null; }


            GL.ReadPixels(0, 0, this.DrawingControl.Width, this.DrawingControl.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapdata.Scan0);//Bilddaten von OpenGL anfordern 
            image.UnlockBits(bitmapdata);
            image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return image;
        }

        public Bitmap GetDataFromDepthBuffer()
        {
            int width = this.DrawingControl.Width;
            int height = this.DrawingControl.Height;
            float[] depthValues = new float[width * height];
            GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL.PixelFormat.DepthComponent, PixelType.Float, depthValues);//Bilddaten von OpenGL anfordern 

            return BitmapHelp.ConvertDepthValuesToBitmap(BitmapHelp.ConvertFlatArrayTo2DArray(depthValues, width, height), true);
        }

        public Bitmap GetDataFromStencilBuffer()
        {
            //return GetDataFromBuffer(OpenTK.Graphics.OpenGL.PixelFormat.StencilIndex, PixelType.Float);

            int[] Viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, Viewport);	//Größe des Viewports abfragen 	
            int width = Viewport[2];
            int height = Viewport[3];
            float[] data = new float[width * height];
            GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL.PixelFormat.StencilIndex, PixelType.Float, data );//Bilddaten von OpenGL anfordern 

            Dictionary<int, Color> colorMap = new Dictionary<int, Color>()
            {
               {-6, Color.Plum },
                {-5, Color.Orchid },
                {-4, Color.Turquoise },
                {-3, Color.DeepPink },
                {-2, Color.Yellow },
                {-1, Color.Blue },
                {+0, Color.White },
                {+1, Color.Black },
                {+2, Color.Green },
                {+3, Color.Red },
                {+4, Color.AliceBlue },
                {+5, Color.Azure },
                {+6, Color.Gainsboro },
                {+7, Color.Lavender },
                {+8, Color.MediumPurple },
            };
            var bitmapBuffer = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    bitmapBuffer.SetPixel(x, height - y - 1, colorMap[(int)data[x + y * width]]);
                }

            return bitmapBuffer;
        }

        public void SetModelViewMatrixToIdentity()
        {
            modelViewMatrix = Matrix4.Identity;
            GL.LoadMatrix(ref modelViewMatrix);
            UpdateModelViewMatrix();
        }

        public void SetProjectionMatrix3D(int screenWidth = 0, int screenHight = 0, float fov = 45, float zNear = 0.001f, float zFar = 3000)
        {
            if (screenWidth == 0) screenWidth = simpleOpenGlControl.Width;
            if (screenHight == 0) screenHight = simpleOpenGlControl.Height;

            GL.MatrixMode(MatrixMode.Projection);					// Select The Projection Matrix
            GL.LoadIdentity();

            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView((float)(fov * Math.PI / 180), (float)screenWidth / (float)screenHight, zNear, zFar);
            GL.LoadMatrix(ref projectionMatrix);

            GL.MatrixMode(MatrixMode.Modelview);
        }

        public void SetProjectionMatrix2D(float left = 0, float right = 0, float bottom = 0, float top = 0, float znear = 0, float zfar = 0)
        {
            GL.MatrixMode(MatrixMode.Projection);					// Select The Projection Matrix
            GL.LoadIdentity();

            if (left == 0 && right == 0)
            {
                projectionMatrix = Matrix4.CreateOrthographic(simpleOpenGlControl.Width, simpleOpenGlControl.Height, -1000.0f, 1000.0f);
                GL.Ortho(0.0f, simpleOpenGlControl.Width, simpleOpenGlControl.Height, 0.0f, -1000.0f, 1000.0f);
            }
            else
            {
                projectionMatrix = Matrix4.CreateOrthographic(right - left, top - bottom, znear, zfar);
                GL.Ortho(left, right, bottom, top, znear, zfar);
            }

            GL.MatrixMode(MatrixMode.Modelview);
        }

        public void SetViewport(int startX, int startY, int width, int height)
        {
            GL.Viewport(startX, this.Height - startY - height, width, height);
        }
        
        public int GetTextureId(System.Drawing.Bitmap bitmap)
        {
            MyTexture tex = new MyTexture(CreateTextureFromBitmap(bitmap/*, false*/), bitmap.Width, bitmap.Height, MyTexture.TexturType.Color2D);
            texturen.Add(tex);
            return tex.TextureID; 
        }

        //gibt die Nr. der geladenen Texture zurück, im Fehlerfall 0
        private int CreateTextureFromBitmap(Bitmap image)
        {
            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            BitmapData bmp_data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);

            image.UnlockBits(bmp_data);

            // We haven't uploaded mipmaps, so disable mipmapping (otherwise the texture will not appear).
            // On newer video cards, we can use GL.GenerateMipmaps() or GL.Ext.GenerateMipmaps() to create
            // mipmaps automatically. In that case, use TextureMinFilter.LinearMipmapLinear to enable them.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            return id;
        }

        public Bitmap GetTextureData(int textureID)
        {
            var tex = texturen.FirstOrDefault(x => x.TextureID == textureID);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            byte[] data = new byte[4 * tex.Width * tex.Height];
            switch (tex.TextureType)
            {
                case MyTexture.TexturType.Color2D: 
                    GL.GetTexImage(TextureTarget.Texture2D, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data);
                    break;
                case MyTexture.TexturType.Depth2D:
                    GL.GetTexImage(TextureTarget.Texture2D, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.DepthComponent, PixelType.Float, data);
                    break;
                default:
                    throw new Exception("Unsuported Type : " + tex.TextureType);
            }

            if (tex.TextureType == MyTexture.TexturType.Depth2D)
            {
                return BitmapHelp.ConvertByteArrayFromDepthTextureToBitmap(data, tex.Width, tex.Height, true);
            }
            else
            {
                Bitmap result = new Bitmap(tex.Width, tex.Height);
                BitmapData resultData = result.LockBits(new Rectangle(0, 0, tex.Width, tex.Height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Marshal.Copy(data, 0, resultData.Scan0, data.Length);
                result.UnlockBits(resultData);

                result.RotateFlip(RotateFlipType.RotateNoneFlipY);

                return result;
            }
        }

        public int CreateEmptyTexture(int width, int height)
        {
            MyTexture tex = new MyTexture(GL.GenTexture(), width, height, MyTexture.TexturType.Color2D);
            texturen.Add(tex);
            return tex.TextureID;
        }

        public void CopyScreenToTexture(int textureID)
        {
            int[] viewport = new int[4];									   
            GL.GetInteger(GetPName.Viewport, viewport);         // Retrieves The Viewport Values (X, Y, Width, Height)
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.CopyTexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 0,0, this.Width, this.Height, 0);
        }

        class Framebuffer
        {
            public int? ColorTextureId;
            public int? DepthTextureId;
            public int Width;
            public int Height;
        }

        private Dictionary<int, Framebuffer> framebuffers = new Dictionary<int, Framebuffer>();

        //http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-14-render-to-texture/
        public int CreateFramebuffer(int width, int height, bool withColorTexture, bool withDepthTexture)
        {
            // The framebuffer, which regroups 0, 1, or more textures, and 0 or 1 depth buffer.
            int[] FramebufferName = new int[1];
            GL.GenFramebuffers(1, FramebufferName);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferName[0]);

            int? colorTextureId = null, depthTextureId = null;

            if (withColorTexture)
            {
                int renderedTexture = CreateColorTexture(width, height);

                // Set "renderedTexture" as our colour attachement #0
                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, renderedTexture, 0);
 
                // Set the list of draw buffers.
                DrawBuffersEnum[] DrawBuffers = new DrawBuffersEnum[] //Hier wird nur die Verknüpfung der Farbtexturen mit den Pixel-Shader layout(location = 0) hergestellt.
                {                                                     //Für die Tiefenpuffertextur braucht man das nicht, da es ja nur eine gibt.
                    DrawBuffersEnum.ColorAttachment0                    //Das 1. Feld ist ColorAttachment0. Dort wurde die Farbtextur gebunden. Das heißt beim Pixelshader landet die Variable 'color' in der Farbtextur, weil sie auf 'location = 0' zeigt. -> layout(location = 0) out vec3 color;
                };
                GL.DrawBuffers(1, DrawBuffers); // "1" is the size of DrawBuffers

                colorTextureId = renderedTexture;
            }

            if (withDepthTexture) //Render die Tiefenwerte in Textur (Langsamer, als wenn ich in Puffer render)
            {
                int depthTexture = CreateDepthTexture(width, height);
                //GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthTexture, 0); //Nur Depth ohne Stencil
                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, depthTexture, 0);

                depthTextureId = depthTexture;

                //GL.DrawBuffer(DrawBufferMode.None);
            }
            else //Render die Tiefenwerte in 'Standard'-Puffer
            {
                int depthrenderbuffer = CreateDepthBuffer(width, height);
                //GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthrenderbuffer); // attach a renderbuffer object to a framebuffer object
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, depthrenderbuffer); // attach a renderbuffer object to a framebuffer object
            }

            // Always check that our framebuffer is ok
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                string fboMsg = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer).ToString();
                throw new Exception("OpenGL 3.0: Framebuffer konnte nicht erstellt werden -> " + fboMsg);
            }

            framebuffers.Add(FramebufferName[0], new Framebuffer() { ColorTextureId = colorTextureId, DepthTextureId = depthTextureId, Width = width, Height = height });

            return FramebufferName[0];
        }

        private int CreateColorTexture(int width, int height)
        {
            // The texture we're going to render to
            int[] renderedTexture = new int[1];
            GL.GenTextures(1, renderedTexture);

            // "Bind" the newly created texture : all future texture functions will modify this texture
            GL.BindTexture(TextureTarget.Texture2D, renderedTexture[0]);

            // Give an empty image to OpenGL ( the last "0" )
            //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            // Poor filtering. Needed !
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

            texturen.Add(new MyTexture(renderedTexture[0], width, height, MyTexture.TexturType.Color2D));

            return renderedTexture[0];
        }

        private int CreateDepthTexture(int width, int height)
        {
            int[] depthTexture = new int[1];
            GL.GenTextures(1, depthTexture);
            GL.BindTexture(TextureTarget.Texture2D, depthTexture[0]);
            //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24, width, height, 0, OpenTK.Graphics.OpenGL.PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth24Stencil8, width, height, 0, OpenTK.Graphics.OpenGL.PixelFormat.DepthStencil, PixelType.UnsignedInt248, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);


            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            //Wenn ich beim Shadowmapping verhindern will, das Bereiche schattiert werden, die außerhalb des Sichtbereits der Kamera
            //sind, dann muss ich ClampToBorder und BorderColor=1 verwenden (https://learnopengl.com/Advanced-Lighting/Shadows/Shadow-Mapping)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder); //
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 1, 1, 1, 1 });

            texturen.Add(new MyTexture(depthTexture[0], width, height, MyTexture.TexturType.Depth2D));

            return depthTexture[0];
        }

        private int CreateDepthBuffer(int width, int height)
        {
            int[] depthrenderbuffer = new int[1];
            GL.GenRenderbuffers(1, depthrenderbuffer);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthrenderbuffer[0]);
            //GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height); //create and initialize a renderbuffer object's data store
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width, height); //create and initialize a renderbuffer object's data store

            return depthrenderbuffer[0];
        }        
        
        public void EnableRenderToFramebuffer(int framebufferId)
        {
            // Render to our framebuffer
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);
            GL.Viewport(0, 0, framebuffers[framebufferId].Width, framebuffers[framebufferId].Height); // Render on the whole framebuffer, complete from the lower left corner to the upper right
        }

        public void DisableRenderToFramebuffer()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, simpleOpenGlControl.Width, simpleOpenGlControl.Height);
        }

        public int GetColorTextureIdFromFramebuffer(int framebufferId)
        {
            return (int)framebuffers[framebufferId].ColorTextureId;
        }

        public int GetDepthTextureIdFromFramebuffer(int framebufferId)
        {
            return (int)framebuffers[framebufferId].DepthTextureId;
        }

        public Size GetTextureSize(int textureId)
        {
            var tex = texturen.FirstOrDefault(x => x.TextureID == textureId);
            return new Size(tex.Width, tex.Height);
        }

        public void SetTexture(int textureID)
        {
            GL.BindTexture(TextureTarget.Texture2D, textureID);
        }

        public int CreateCubeMap(int cubeMapSize = 256)
        {
            Cubemap cub = new Cubemap();
            cub.CubeMapSize = cubeMapSize;

            cub.FBO = new int[1];
            cub.ColorTexture = new int[1];
            GL.GenTextures(1, cub.ColorTexture);
            GL.BindTexture(TextureTarget.TextureCubeMap, cub.ColorTexture[0]);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            
            
            for (int i = 0; i < 6; i++)
            {
                //glTexImage2D = Damit kann ich von ein Array Daten zu einer Textur in der Grafikkarte schicken
                //void glTexImage2D(
                //     GLenum target,       //TexturTyp
                //     GLint level,         //Level of Detail. 0=Höchste Auflösung
                //     GLint internalformat,//Format: RGBA;DEPTH;DEPTH_STENCIL
                //     GLsizei width,       //
                //     GLsizei height,
                //     GLint border,        //Muss immer 0 sein
                //     GLenum format,       //In diesen Format(Reihenfolge von RGBA_D) liefere ich die Daten
                //     GLenum type,         //Datentyp wie ich die Daten liefere
                //     const void* data);   //Daten, die ich liefere und in die Textur kopieren will
                GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgba16f, cubeMapSize, cubeMapSize, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.HalfFloat, IntPtr.Zero);
            }
            GL.BindTexture(TextureTarget.TextureCubeMap, 0);

            GL.GenFramebuffers(1, cub.FBO);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, cub.FBO[0]);
            for (int i = 0; i < 6; i++)
            {
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, TextureTarget.TextureCubeMapPositiveX + i, cub.ColorTexture[0], 0);
            }
 
            cub.DepthTexture = CreateDepthTexture(cubeMapSize, cubeMapSize);
            //GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, cub.DepthTexture, 0); //Nur Depth ohne Stencil (Bei MirrorSphere ist beim StencilSchatten der Boden schwarz wenn ich das hier mache)
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, cub.DepthTexture, 0);

            string  fboMsg = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer).ToString();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            int newID = 1;
            if (cubemaps.Keys.Count > 0)
                newID = cubemaps.Keys.Max() + 1;

            cubemaps.Add(newID, cub);
            return newID;
        }

        public void EnableRenderToCubeMap(int cubemapID, int cubemapSide, Color clearColor)
        {
            Cubemap cub = cubemaps[cubemapID];

            GL.Viewport(0, 0, cub.CubeMapSize, cub.CubeMapSize);
            GL.Enable(EnableCap.TextureCubeMapSeamless);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, cub.FBO[0]);

            GL.DrawBuffer(DrawBufferMode.ColorAttachment0 + cubemapSide);
            //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + cubemapSide, TextureTarget.TextureCubeMapPositiveX + cubemapSide, cub.ColorTexture[0], 0);
            
            ClearColorDepthAndStencilBuffer(clearColor);
        }

        public Bitmap GetColorDataFromCubeMapSide(int cubemapID, int cubemapSide)
        {
            Cubemap cub = cubemaps[cubemapID];
            GL.BindTexture(TextureTarget.TextureCubeMap, cub.ColorTexture[0]);
            byte[] data = new byte[4 * cub.CubeMapSize * cub.CubeMapSize];
            GL.GetTexImage(TextureTarget.TextureCubeMapPositiveX + cubemapSide, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data);

            Bitmap result = new Bitmap(cub.CubeMapSize, cub.CubeMapSize);
            BitmapData resultData = result.LockBits(new Rectangle(0, 0, cub.CubeMapSize, cub.CubeMapSize), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Marshal.Copy(data, 0, resultData.Scan0, data.Length);
            result.UnlockBits(resultData);

            result.RotateFlip(RotateFlipType.RotateNoneFlipY);

            return result;
        }

        public void DisableRenderToCubeMap()
        {
            GL.Viewport(0, 0, Width, Height);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);            
        }

        public void EnableAndBindCubemapping(int cubemapID)
        {
            shader["UseCubemap"] = 1;
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.TextureCubeMap, cubemaps[cubemapID].ColorTexture[0]);
        }

        public void DisableCubemapping()
        {
            shader["UseCubemap"] = 0;
        }

        public bool ReadFromShadowmap
        {
            set => shader["UseShadowmap"] = value ? 1 : 0;
        }

        //Quelle: http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-16-shadow-mapping/
        public int CreateShadowmap(int width, int height)
        {
            return CreateFramebuffer(width, height, true, true);
        }

        public void EnableRenderToShadowmap(int shadowmapId)
        {
            EnableRenderToFramebuffer(shadowmapId);
            
            //GL.DrawBuffer(DrawBufferMode.None); // No color buffer is drawn to.

            shader.Mode = ShaderHelper.ShaderMode.CreateShadowMap;
            shader.LockShaderModeWriting = true;
        }

        public void BindShadowTexture(int shadowmapId)
        {
            if (shader.Mode != ShaderHelper.ShaderMode.CreateShadowMap)
            {             
                GL.ActiveTexture(TextureUnit.Texture3);
                GL.BindTexture(TextureTarget.Texture2D, GetDepthTextureIdFromFramebuffer(shadowmapId));
            }
        }

        public void DisableRenderToShadowmapTexture()
        {
            shader.LockShaderModeWriting = false;
            GL.Viewport(0, 0, Width, Height);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void SetShadowmapMatrix(Matrix4x4 shadowMatrix)
        {
            shader["ShadowmappingTextureId"] = 0;
            var shadowMatrixOpi = TransformMatrix4x4ToOpenGlMatrix(shadowMatrix);

            //Matrix für Shadow-Mapping setzen
            shader.SetUniformVariable("ShadowMapMatrix[" + 0 + "]", shadowMatrixOpi, true);
        }

        public bool IsRenderToShadowmapEnabled()
        {
            return shader.Mode == ShaderHelper.ShaderMode.CreateShadowMap;
        }

        public Bitmap GetShadowmapAsBitmap(int shadowmapId)
        {
            //return GetTextureData(GetColorTextureIdFromFramebuffer(shadowmapId));
            return GetTextureData(GetDepthTextureIdFromFramebuffer(shadowmapId));
        }

        public void SetActiveTexture0() //Farbtextur
        {
            activeTexture = 0;
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        public void SetActiveTexture1() //Bumpmaptextur
        {
            activeTexture = 1;
            GL.ActiveTexture(TextureUnit.Texture1);
        }

        public void EnableTexturemapping()
        {
            if (activeTexture == 0)
            {
                shader["UseTexture0"] = 1;
            }


            if (activeTexture == 1)
            {
                shader["UseTexture1"] = 1;
            }

            GL.Enable(EnableCap.Texture2D);
            GL.Disable(EnableCap.ColorMaterial); //keine Farbfestlegung mit glColor möglich; Dafür ist nun Texturmapping möglich
        }

        //Vorher muss BindTexture (SetTexture) aufgerufen werden
        public void SetTextureFilter(TextureFilter filter)
        {
            switch (filter)
            {
                case TextureFilter.Point:
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);//GL_NEAREST ist im Normalfall schneller als GL_LINEAR, aber produziert auch Bilder mit schärferen Kanten, 
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest);//da der Übergang zwischen den Texturelementen nicht so weich ist. Voreinstellung für GL_TEXTURE_MAG_FILTER ist GL_LINEAR.
                    break;
                case TextureFilter.Linear:
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);//GL_NEAREST ist im Normalfall schneller als GL_LINEAR, aber produziert auch Bilder mit schärferen Kanten, 
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear);//da der Übergang zwischen den Texturelementen nicht so weich ist. Voreinstellung für GL_TEXTURE_MAG_FILTER ist GL_LINEAR.
                    break;
                case TextureFilter.Anisotroph: 
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)OpenTK.Graphics.ES20.All.TextureMaxAnisotropyExt);//GL_NEAREST ist im Normalfall schneller als GL_LINEAR, aber produziert auch Bilder mit schärferen Kanten, 
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)OpenTK.Graphics.ES20.All.TextureMaxAnisotropyExt);//da der Übergang zwischen den Texturelementen nicht so weich ist. Voreinstellung für GL_TEXTURE_MAG_FILTER ist GL_LINEAR.
                    break;
            }
        }

        public void DisableTexturemapping()
        {
            if (activeTexture == 0)
                shader["UseTexture0"] = 0;

            if (activeTexture == 1)
                shader["UseTexture1"] = 0;

            GL.Disable(EnableCap.Texture2D);
            GL.Enable(EnableCap.ColorMaterial); //Farbfestlegung mit glColor möglich
        }

        public void SetTextureMatrix(Matrix3x3 matrix3x3)
        {
            if (shader != null) shader.SetUniformVariable("TextureMatrix", TransformFloatMatrix3x3ToOpenGlMatrix(matrix3x3.Values));
        }

        private Matrix3 TransformFloatMatrix3x3ToOpenGlMatrix(float[] matrix3x3)
        {
            float[] m = matrix3x3;
            return new Matrix3(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8]);
        }

        public void SetTextureScale(Vector2D scale)
        {
            if (shader != null) shader.SetUniformVariable("TexturScaleFaktorX", scale.X);
            if (shader != null) shader.SetUniformVariable("TexturScaleFaktorY", scale.Y);
        }

        public void SetTesselationFactor(float tesselationFactor)              // Wird bei Displacementmapping benötigt. In so viele Dreiecke wird Dreieck zerlegt
        {
            if (shader != null) shader.SetUniformVariable("TesselationFactor", tesselationFactor);
        }

        public void SetTextureHeighScaleFactor(float textureHeighScaleFactor)    // Höhenskalierung bei Displacement- und Parallaxmapping
        {
            if (shader != null) shader.SetUniformVariable("HeighScaleFactor", textureHeighScaleFactor);
        }

        public void SetTextureMode(TextureMode textureMode)
        {
            int modus = 0;
            switch (textureMode)
            {
                case TextureMode.Repeat:
                    modus = Convert.ToInt32(TextureWrapMode.Repeat);
                    break;
                case TextureMode.Clamp:
                    modus = Convert.ToInt32(TextureWrapMode.Clamp);
                    break;
            }
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, modus); //GL_REPEAT, GL_CLAMP
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, modus);
        }

        public int GetTriangleArrayId(Triangle[] data)
        {           
            return shader.GetTriangleArrayID(data);
        }

        private void UpdateModelViewMatrix()
        {
            if (shader != null)
            {
                Matrix4 objToWorld = this.modelViewMatrix * this.inverseCameraMatrix;
                Matrix4 worldToObj = Matrix4.Invert(objToWorld);
                shader.SetUniformVariable("ObjToClipMatrix", this.modelViewMatrix * this.projectionMatrix);
                shader.SetUniformVariable("ObjToWorldMatrix", objToWorld);
                shader.SetUniformVariable("CameraMatrix", this.cameraMatrix);
                shader.SetUniformVariable("NormalMatrix", Matrix4.Transpose(worldToObj));
                shader.SetUniformVariable("WorldToObj", worldToObj);
            }
        }

        public void DrawTriangleArray(int triangleArrayId)
        {
            shader.DrawTriangleArray(triangleArrayId);
        }

        public void RemoveTriangleArray(int triangleArrayId)
        {
            shader.RemoveTriangleArray(triangleArrayId);
        }

        public void DrawTriangleStrip(Vector3D v1, Vector3D v2, Vector3D v3, Vector3D v4)
        {
            GL.Begin(PrimitiveType.TriangleStrip);
            GL.Vertex3(v1.X, v1.Y, v1.Z);
            GL.Vertex3(v2.X, v2.Y, v2.Z);
            GL.Vertex3(v3.X, v3.Y, v3.Z);
            GL.Vertex3(v4.X, v4.Y, v4.Z);
            GL.End();
        }

        public void DrawLine(Vector3D v1, Vector3D v2)
        {
            var shadermodeBefore = shader.Mode;
            shader.Mode = ShaderHelper.ShaderMode.None;
            GL.Disable(EnableCap.LineStipple);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(v1.X, v1.Y, v1.Z);
            GL.Vertex3(v2.X, v2.Y, v2.Z);
            GL.End();
            
            shader.Mode = shadermodeBefore;
        }

        public void SetLineWidth(float lineWidth)
        {
            GL.LineWidth(lineWidth);
        }

        public void DrawPoint(Vector3D position)
        {
            var shaderModusBevore = shader.Mode;
            shader.Mode = ShaderHelper.ShaderMode.None;
            GL.Begin(PrimitiveType.Points);
            GL.Vertex3(position.Float3f);
            GL.End();
            shader.Mode = shaderModusBevore;
        }

        public void SetPointSize(float size)
        {
            GL.PointSize(size);
        }

        public Color GetPixelColorFromColorBuffer(int x, int y)
        {
            byte[] data = new byte[4];
            GL.ReadPixels(x, this.DrawingControl.Height - y, 1, 1, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data);//Bilddaten von OpenGL anfordern 
            return Color.FromArgb(data[3], data[2], data[1], data[0]);
        }

        public Matrix4x4 GetInverseModelMatrix(Vector3D position, Vector3D orientation, float size)
        {
            float[] matrix = new float[16];

            GL.PushMatrix();
            GL.LoadIdentity();

            GL.Scale(1 / size, 1 / size, 1 / size);
            GL.Rotate(-orientation.Z, 0.0f, 0.0f, 1.0f);
            GL.Rotate(-orientation.Y, 0.0f, 1.0f, 0.0f);
            GL.Rotate(-orientation.X, 1.0f, 0.0f, 0.0f);

            GL.Translate(-position.X, -position.Y, -position.Z);

            GL.GetFloat(GetPName.ModelviewMatrix , matrix);
            GL.PopMatrix();

            return new Matrix4x4(matrix);
        }

        public Matrix4x4 GetModelMatrix(Vector3D position, Vector3D orientation, float size)
        {
            float[] matrix = new float[16];

            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Translate(position.X, position.Y, position.Z);
            GL.Rotate(orientation.X, 1.0f, 0.0f, 0.0f);
            GL.Rotate(orientation.Y, 0.0f, 1.0f, 0.0f);
            GL.Rotate(orientation.Z, 0.0f, 0.0f, 1.0f);
            GL.Scale(size, size, size);

            GL.GetFloat(GetPName.ModelviewMatrix, matrix);
            GL.PopMatrix();

            return new Matrix4x4(matrix);
        }

        public void PushMatrix()
        {
            modelViewMatrixStack.Push(modelViewMatrix);
            GL.PushMatrix();
        }

        public void PopMatrix()
        {
            modelViewMatrix = modelViewMatrixStack.Pop();
            GL.PopMatrix();
            UpdateModelViewMatrix();
        }

        public void PushProjectionMatrix()
        {
            projectionMatrixStack.Push(projectionMatrix);
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
        }

        public void PopProjectionMatrix()
        {
            projectionMatrix = projectionMatrixStack.Pop();
        }

        public void MultMatrix(Matrix4x4 matrix)
        {
            modelViewMatrix = TransformMatrix4x4ToOpenGlMatrix(matrix) * modelViewMatrix;
            GL.LoadMatrix(ref modelViewMatrix);
            UpdateModelViewMatrix();
        }

        private Matrix4 TransformMatrix4x4ToOpenGlMatrix(Matrix4x4 matrix)
        {
            var m = matrix.Values;
            return new Matrix4(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);
        }

        public void Scale(float size)
        {
            modelViewMatrix = Matrix4.Mult(Matrix4.CreateScale(size), modelViewMatrix);
            GL.LoadMatrix(ref modelViewMatrix);
            UpdateModelViewMatrix();
        }

        public Matrix4x4 GetProjectionMatrix()
        {            
            return new Matrix4x4(new float[] {projectionMatrix.M11, projectionMatrix.M12, projectionMatrix.M13, projectionMatrix.M14,
                                              projectionMatrix.M21, projectionMatrix.M22, projectionMatrix.M23, projectionMatrix.M24,
                                              projectionMatrix.M31, projectionMatrix.M32, projectionMatrix.M33, projectionMatrix.M34,
                                              projectionMatrix.M41, projectionMatrix.M42, projectionMatrix.M43, projectionMatrix.M44});
        }

        public Matrix4x4 GetModelViewMatrix()
        {
            return new Matrix4x4(new float[] {modelViewMatrix.M11, modelViewMatrix.M12, modelViewMatrix.M13, modelViewMatrix.M14,
                                              modelViewMatrix.M21, modelViewMatrix.M22, modelViewMatrix.M23, modelViewMatrix.M24,
                                              modelViewMatrix.M31, modelViewMatrix.M32, modelViewMatrix.M33, modelViewMatrix.M34,
                                              modelViewMatrix.M41, modelViewMatrix.M42, modelViewMatrix.M43, modelViewMatrix.M44});
        }

        public void SetColor(float R, float G, float B, float A)
        {
            shader.SetUniformVariable("color", new Vector4(R, G, B, A));
            GL.Color4(R, G, B, A);
        }

        public void SetSpecularHighlightPowExponent(float specularHighlightPowExponent)
        {
            //Diese Werte hier werden eh nicht beachtet wenn man Shader verwendet. Das ist nur für den Fall, falls man ohne Shader arbeiten will/muss
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, new float[] { 0.1f, 0.1f, 0.1f, 1 });
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Diffuse, new float[] { 1, 1, 1, 1 });
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Specular, new float[] { 1, 1, 1, 1 });	// set the reflection of the material 
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Emission, new float[] { 0, 0, 0, 0 });
            
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Shininess, specularHighlightPowExponent);	// set the brightness of the material 
            shader.SetUniformVariable("lightStruct.SpecularHighlightPowExponent", specularHighlightPowExponent, false);
        }

        private Matrix4 cameraMatrix = Matrix4.Identity;
        private Matrix4 inverseCameraMatrix = Matrix4.Identity;
        public void SetModelViewMatrixToCamera(Camera kamera)
        {
            this.cameraMatrix = Matrix4.LookAt(kamera.Position.X, kamera.Position.Y, kamera.Position.Z, kamera.Position.X + kamera.Forward.X, kamera.Position.Y + kamera.Forward.Y, kamera.Position.Z + kamera.Forward.Z, kamera.Up.X, kamera.Up.Y, kamera.Up.Z);
            this.inverseCameraMatrix = TransformMatrix4x4ToOpenGlMatrix(Matrix4x4.InverseLookAt(kamera.Position, kamera.Forward, kamera.Up));
            modelViewMatrix = this.cameraMatrix;
            GL.LoadMatrix(ref modelViewMatrix);
            UpdateModelViewMatrix();

            shader.SetUniformVariable("CameraPosition", new Vector3(kamera.Position.X, kamera.Position.Y, kamera.Position.Z), true);
        }

        public void SetPositionOfAllLightsources(List<RasterizerLightsource> lights)
        {
            int i;
            for (i = 0; i < lights.Count; i++)
            {
                GL.Enable(EnableCap.Light0 + i);
                shader.SetUniformVariable("lightStruct.LightPositions[" + i + "]", new Vector3(lights[i].Position.X, lights[i].Position.Y, lights[i].Position.Z), true);
                shader.SetUniformVariable("lightStruct.ConstantAttenuation[" + i + "]", lights[i].ConstantAttenuation, true);
                shader.SetUniformVariable("lightStruct.LinearAttenuation[" + i + "]", lights[i].LinearAttenuation, true);
                shader.SetUniformVariable("lightStruct.QuadraticAttenuation[" + i + "]", lights[i].QuadraticAttenuation, true);

                shader.SetUniformVariable("lightStruct.SpotCosCutoff[" + i + "]", (float)Math.Cos(lights[i].SpotCutoff * Math.PI / 180), true); //Der Öffnungswinkel muss zwischen 0 und 90 liegen. Wenn 180, dann ist Richtungslicht deaktiviert
                shader.SetUniformVariable("lightStruct.SpotDirection[" + i + "]", Vector3.Normalize(new Vector3(lights[i].SpotDirection.X, lights[i].SpotDirection.Y, lights[i].SpotDirection.Z)), true);//Angabe in Weltkoordinaten
                shader.SetUniformVariable("lightStruct.SpotExponent[" + i + "]", lights[i].SpotExponent, true);
            }
            GL.LightModel(LightModelParameter.LightModelAmbient, new float[] { 0.1f, 0.1f, 0.1f, 1.0f }); //ambient RGBA intensity of the entire scene
            GL.LightModel(LightModelParameter.LightModelLocalViewer, 0.0f); //how specular reflection angles are computed
            GL.LightModel(LightModelParameter.LightModelTwoSide, 0.0f); //choose between one-sided or two-sided lighting
            int maxLights;
            GL.GetInteger(GetPName.MaxLights, out maxLights);
            while (i < maxLights) GL.Disable(EnableCap.Light0 + i++);

            shader.SetUniformVariable("lightStruct.LightCount", lights.Count, true);
        }

        public void EnableLighting()
        {
            GL.Enable(EnableCap.Lighting);
            shader["LightingIsEnabled"] = 1;
        }

        public void DisableLighting()
        {
            GL.Disable(EnableCap.Lighting);
            shader["LightingIsEnabled"] = 0;
        }

        //Reihenfolge: Vertexshader->Pixelshader->glAlphaFunc(Verwerfe Pixel, wenn Alphawert > Schwellwert)->glBlendFunc(Blending = Pixelshaderfarbe * Faktor1 + Farbpufferfabe * Faktor2)->Farbpuffer
        public void SetBlendingWithBlackColor()
        {
            GL.Disable(EnableCap.Blend);
            GL.AlphaFunc(AlphaFunction.Greater, 0.01f);   //Verwerfe die Ausgabe aus dem Pixelshader(Kein Schreiben im Farbpuffer), wenn der Alphawert > 0.01 ist
            //GL.Enable(EnableCap.AlphaTest); //Der Topf verschwindet, wenn das hier aktiviert wird			//GL_ALPHA_TEST muss aktiviert sein, wenn man glAlphaFunc verwenden will
            if (shader != null) shader["BlendingWithBlackColor"] = 1;
        }

        //Alphablending = Sourcecolor * SourceAlpha + Destinationcolor * (1 - SourceAlpha)
        //ColorBlending = Sourcecolor + DestinationColor
        //Sourcesolor = Die Farbe, die aus dem Pixelshader kommt
        //Desinationcolor = Die Farbe, die Bereits im Farbpuffer steht
        public void SetBlendingWithAlpha()
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc( BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);//glBlendFunc(SourcecolorFactor, DestinationcolorFactor)
            GL.Enable(EnableCap.ColorMaterial); //Farbfestlegung mit glColor möglich
            if (shader != null) shader["BlendingWithBlackColor"] = 0;
        }

        public void DisableBlending()
        {
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.AlphaTest);
            if (shader != null) shader["BlendingWithBlackColor"] = 0;
        }

        public void EnableWireframe()
        {
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        }

        public void DisableWireframe()
        {
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        public void EnableExplosionEffect()
        {
            if (shader != null) shader["ExplosionEffectIsEnabled"] = 1;
        }

        public void DisableExplosionEffect()
        {
            if (shader != null) shader["ExplosionEffectIsEnabled"] = 0;
        }

        public float ExplosionsRadius 
        {
            get
            {
                return shader["ExplosionsRadius"];
            }
            set
            {
                shader.SetUniformVariable("ExplosionsRadius", value, false);
            }
        } 

        public int Time // Explosionseffekt braucht Timerwert
        {
            get
            {
                return shader["Time"];
            }
            set
            {
                shader["Time"] = value;
            }
        } 

        public void EnableCullFace()
        {
            GL.Enable(EnableCap.CullFace);
            if (shader != null) shader["CullFaceIsEnabled"] = 1;
        }

        public void DisableCullFace()
        {
            GL.Disable(EnableCap.CullFace);
            if (shader != null) shader["CullFaceIsEnabled"] = 0;
        }

        public void SetFrontFaceConterClockWise()
        {
            GL.FrontFace(FrontFaceDirection.Ccw);
        }

        public void SetFrontFaceClockWise()
        {
            GL.FrontFace(FrontFaceDirection.Cw);
        }

        public void EnableDepthTesting()
        {
            GL.Enable(EnableCap.DepthTest);
        }

        public void DisableDepthTesting()
        {
            GL.Disable(EnableCap.DepthTest);
        }

        public System.Drawing.Bitmap GetStencilTestImage()
        {
            return GetDataFromStencilBuffer();
        }
        public void EnableStencilTest()
        {
            GL.Enable(EnableCap.StencilTest);
        }

        public void DisableStencilTest()
        {
            GL.Disable(EnableCap.StencilTest);
        }

        public bool SetStencilWrite_TwoSide()
        {
            return false;
        }

        public void SetStencilWrite_Increase()
        {
            //Parameter 1: StencilFunktion: GL_ALWAYS: Test wird immer bestanden
            //Parameter 2: Referenzwert: Mit diesen Wert wird nach dem Zeichnen verglichen (Zeichnen bedeutet Stencilpuffer wird auf 1 gesetzt). 
            //             Nur wenn Stencilpufferwert == Referenzwert, dann ist der Stenciltest bestanden
            //Parameter 3: Make: Bevor der Test durchgeführt wird: StencilWertNeu = Referenzwert & Make & StencilWert (Bitweises UND)
            GL.StencilFunc(StencilFunction.Always, 1, 0xfffffff);

            //procedure glStencilOp (fail,zfail,zpass: TGLenum);
            //Parameter 1: fail	 Legt fest, was gemacht wird, wenn der Stencil-Test fehlschlägt.
            //Parameter 2: zfail Legt fest, was passiert, wenn der Stencil-Test erfolgreich ist aber der Tiefentest fehlschlägt
            //Parameter 3: zpass Legt fest, was passiert, wenn sowohl der Stenciltest als auch Tiefentest erfolgreich ist. Ist der Tiefentest deaktiviert, so gilt dieser als bestanden
            //GL_KEEP ... Keine Veränderung
            //GL_ZERO ... Setzt den Wert im Schablonenpuffer auf Null
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Incr); //Nach erfolgreichen Stenciltest wird Stencilwert um 1 erhöht
        }

        public void SetStencilWrite_Decrease()
        {
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Decr); //Nach erfolgreichen Stenciltest wird Stencilwert um 1 verringert
        }

        public void SetStencilRead_NotEqualZero()
        {
            GL.StencilFunc(StencilFunction.Notequal, 0, 0xfffffff); //Es kann nur an der Stelle im Farbpuffer was gezeichnet werden, wo
            //im Stencilpuffer ein Wert != 0 steht
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep); //Stencilpuffer wird auf Readonly gesetzt
        }

        private Vector2D mouseHitTestPosition = null;
        //Quelle: http://www.opentk.com/node/2050
        public void StartMouseHitTest(Point mousePosition)
        {
            this.mouseHitTestPosition = new Vector2D(mousePosition.X, mousePosition.Y);
            shader.Mode = ShaderHelper.ShaderMode.MouseHitTest;
            shader.LockShaderModeWriting = true;
            return;

            //mouseY = simpleOpenGlControl.Height - mouseY;

            //selectBufferForMouseHitTest = new int[objektCount + 10];				//buffer for selected object names (1..Gelenk 1,...                                                  
            //int[] viewport = new int[4];									    //tmpvar for viewport 
            //GL.GetInteger(GetPName.Viewport, viewport);						/* get current viewport, save in "viewport" */
            //GL.SelectBuffer(selectBufferForMouseHitTest.Length, selectBufferForMouseHitTest);

            //PushProjectionMatrix();

            //GL.RenderMode(RenderingMode.Select);								/* change render mode for selection */

            //Matrix4 m = GluPickMatrix(mouseX, mouseY, 5, 5, viewport);
            //projectionMatrix = projectionMatrix * m;

            //GL.InitNames();										/* init name stack */
            //GL.PushName(0);										/* push one element on stack to avoid GL_INVALID_OPERATION error @ next pop from stack */
        }

        private Matrix4 GluPickMatrix(float x, float y, float width, float height, int[] viewport)
        {
            Matrix4 result = Matrix4.Identity;
            result = Matrix4.Mult(Matrix4.CreateTranslation((viewport[2] - (2.0f * (x - viewport[0]))) / width, (viewport[3] - (2.0f * (y - viewport[1]))) / height, 0.0f), result);
            result = Matrix4.Mult(Matrix4.CreateScale(viewport[2] / width, viewport[3] / height, 1.0f), result);
            return result;
        }

        public void AddObjektIdForMouseHitTest(int objektId)
        {
            if (shader != null) shader["MouseHitId"] = objektId;
            GL.LoadName(objektId);
        }

        public int GetMouseHitTestResult()
        {
            shader.LockShaderModeWriting = false;
            shader.Mode = ShaderHelper.ShaderMode.Normal;            
            return (int)GetPixelColorFromColorBuffer((int)this.mouseHitTestPosition.X, (int)this.mouseHitTestPosition.Y).R;

            //int hits;                                   //number of hits with mouse
            //GL.PopName();

            //PopProjectionMatrix();

            //if ((hits = GL.RenderMode(RenderingMode.Render)) != 0)
            //{
            //    //selectBufferForMouseHitTest: Immer 4 Zahlen gehören zusammen: 
            //    //  1. Anzahl der Namen auf dem Stack
            //    //  2. Kleinster Z-Wert des getroffenen Objektes
            //    //  3. Größter Z-Wert des getroffenen Objektes
            //    //  4. Name(Objekt-ID) des Objektes
            //    int lastObjektID = selectBufferForMouseHitTest[3];
            //    int lastMinZ = selectBufferForMouseHitTest[1];
            //    try
            //    {
            //        for (int i = 1; i < hits; i++)
            //        {
            //            int minZ = selectBufferForMouseHitTest[i * 4 + 1];
            //            int objID = selectBufferForMouseHitTest[i * 4 + 3];
            //            if (minZ < lastMinZ)
            //            {
            //                lastObjektID = objID;
            //                lastMinZ = minZ;
            //            }
            //        }
            //    }
            //    catch (Exception) { }

            //    return lastObjektID;
            //}

            //return -1;
        }
        #endregion

        #region 2D
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
                GL.Enable(EnableCap.AlphaTest);
                GL.AlphaFunc(AlphaFunction.Greater, 0.01f);
                GL.Disable(EnableCap.Blend);
            }
        }

        public float ZValue2D { get; set; } = 0;

        public void DrawLine(System.Drawing.Pen pen, Vector2D p1, Vector2D p2)
        {
            GL.LoadMatrix(ref modelViewMatrix);
            GL.Disable(EnableCap.Texture2D);
            GL.LineWidth(pen.Width);
            if (pen.DashStyle == System.Drawing.Drawing2D.DashStyle.Dot)
                GL.Enable(EnableCap.LineStipple);//Linien dürfen gepunktet sein
            else
                GL.Disable(EnableCap.LineStipple);
            GL.Color3(pen.Color.R / 255.0f, pen.Color.G / 255.0f, pen.Color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(p1.X, p1.Y, this.ZValue2D); //OpenGL verschiebt Linien immer um 0.5f Pixel nach links. Damit gleiche ich das aus
            GL.Vertex3(p2.X, p2.Y, this.ZValue2D);
            GL.End();
        }

        public void DrawPixel(Vector2D pos, System.Drawing.Color color, float size)
        {
            GL.PointSize(size);
            GL.Disable(EnableCap.Texture2D);
            GL.Begin(PrimitiveType.Points);
            GL.Color3(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            GL.Vertex3(pos.X, pos.Y, this.ZValue2D);
            GL.End(); 
        }

        private SizeF singleLetterSize = new SizeF(0, 0);
        public System.Drawing.Size GetStringSize(float size, string text)
        {
            if (singleLetterSize.Width == 0)
            {
                Bitmap bild = BitmapHelp.GetBitmapText("WWww", size, Color.Black, Color.White);
                Rectangle reci = BitmapHelp.SearchRectangleInBitmap(bild, Color.White);
                singleLetterSize = new SizeF(reci.Width / 4.0f / size, reci.Height / size);//4.. Länge des Textes "WWww"
            }

            return new Size((int)(singleLetterSize.Width * text.Length * size), (int)(singleLetterSize.Height * size * 1.3f));
        }

        //http://www.opentk.com/book/export/html/1555
        public void DrawString(float x, float y, System.Drawing.Color color, float size, string text)
        {
            GL.LoadMatrix(ref modelViewMatrix);
            string key = text + "_" + size + "_" + color.R + "_" + color.G + "_" + color.B;
            if (!texte.Keys.Contains(key))
            {
                texte.Add(key, CreateTextureFromBitmap(BitmapHelp.GetBitmapText(text, size, color, Color.Transparent)));
            }

            PushProjectionMatrix();

            SetProjectionMatrix2D();

            GL.Color4(1f, 1f, 1f, 1);
            SetTexture(texte[key]);
            GL.Enable(EnableCap.Texture2D);
            SetTextureFilter(TextureFilter.Point);
            //SetBlendingWithBlackColor();
            SetBlendingWithAlpha();

            SizeF sizef = GetStringSize(size, text);

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0f, 0f); GL.Vertex3(x, y, this.ZValue2D);
            GL.TexCoord2(1f, 0f); GL.Vertex3(x + sizef.Width, y, this.ZValue2D);
            GL.TexCoord2(1f, 1f); GL.Vertex3(x + sizef.Width, y + sizef.Height, this.ZValue2D);
            GL.TexCoord2(0f, 1f); GL.Vertex3(x, y + sizef.Height, this.ZValue2D);
            GL.End();

            PopProjectionMatrix();
            GL.Disable(EnableCap.Blend);
            //DisableBlending();
        }

        public void DrawRectangle(System.Drawing.Pen pen, int x, int y, int width, int height)
        {
            GL.Disable(EnableCap.Texture2D);
            GL.LineWidth(pen.Width);
            if (pen.DashStyle == System.Drawing.Drawing2D.DashStyle.Dot)
                GL.Enable(EnableCap.LineStipple);//Linien dürfen gepunktet sein
            else
                GL.Disable(EnableCap.LineStipple);
            GL.Color3(pen.Color.R / 255.0f, pen.Color.G / 255.0f, pen.Color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            GL.Begin(PrimitiveType.LineStrip);
            GL.Vertex3(x, y, this.ZValue2D);
            GL.Vertex3(x + width, y, this.ZValue2D);
            GL.Vertex3(x + width, y + height, this.ZValue2D);
            GL.Vertex3(x, y + height, this.ZValue2D);
            GL.Vertex3(x, y, this.ZValue2D);
            GL.End();
        }

        public void DrawImage(int textureId, int x, int y, int width, int height, int sourceX, int sourceY, int sourceWidth, int sourceHeight, Color colorFactor)
        {
            float f = 0;// 0.01f;

            Size tex = GetTextureSize(textureId);
            UseAlphaBlendingAndDiscardTransparent(colorFactor);// SetBlendingWithAlpha();
            GL.Enable(EnableCap.Texture2D);
            SetTextureFilter(TextureFilter.Point);
            //GL.Color3(colorFactor.X, colorFactor.Y, colorFactor.Z);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.Begin(PrimitiveType.Quads);

            GL.TexCoord2(sourceX / (float)tex.Width + f, sourceY / (float)tex.Height + f); GL.Vertex3(x, y, this.ZValue2D);
            GL.TexCoord2((sourceX + sourceWidth) / (float)tex.Width - f, sourceY / (float)tex.Height + f); GL.Vertex3(x + width, y, this.ZValue2D);
            GL.TexCoord2((sourceX + sourceWidth) / (float)tex.Width - f, (sourceY + sourceHeight) / (float)tex.Height - f); GL.Vertex3(x + width, y + height, this.ZValue2D);
            GL.TexCoord2(sourceX / (float)tex.Width + f, (sourceY + sourceHeight) / (float)tex.Height - f); GL.Vertex3(x, y + height, this.ZValue2D);

            GL.End();
            DisableBlending();
        }

        public void DrawImage(int textureId, int x, int y, int width, int height, int sourceX, int sourceY, int sourceWidth, int sourceHeight, Color colorFactor, float zAngle, float yAngle)
        {
            GL.Translate(x, y, 0);
            GL.Rotate(zAngle, 0, 0, 1);
            GL.Rotate(yAngle, 0, 1, 0);
            DrawImage(textureId, -width / 2, -height / 2, width, height, sourceX, sourceY, sourceWidth, sourceHeight, colorFactor);
            GL.Rotate(-yAngle, 0, 1, 0);
            GL.Rotate(-zAngle, 0, 0, 1);
            GL.Translate(-x, -y, 0);
            DisableBlending();
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor)
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor);// SetBlendingWithAlpha();

            GL.Enable(EnableCap.Texture2D);
            SetTextureFilter(TextureFilter.Point);
            //GL.Color3(colorFactor.X, colorFactor.Y, colorFactor.Z);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0.01f, 0.01f); GL.Vertex3(x, y, this.ZValue2D);
            GL.TexCoord2(0.99f, 0.01f); GL.Vertex3(x + width, y, this.ZValue2D);
            GL.TexCoord2(0.99f, 0.99f); GL.Vertex3(x + width, y + height, this.ZValue2D);
            GL.TexCoord2(0.01f, 0.99f); GL.Vertex3(x, y + height, this.ZValue2D);
            GL.End();
            DisableBlending();
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor, float angle)
        {
            //SetBlendingWithAlpha();
            GL.Translate(x, y, 0);
            GL.Rotate(angle, 0, 0, 1);
            DrawFillRectangle(textureId, -width / 2, -height / 2, width, height, colorFactor);
            GL.Rotate(-angle, 0, 0, 1);
            GL.Translate(-x, -y, 0);
            DisableBlending();  
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor, float zAngle, float yAngle)
        {
            //SetBlendingWithAlpha();
            GL.Translate(x, y, 0);
            GL.Rotate(zAngle, 0, 0, 1);
            GL.Rotate(yAngle, 0, 1, 0);
            DrawFillRectangle(textureId, -width / 2, -height / 2, width, height, colorFactor);
            GL.Rotate(-yAngle, 0, 1, 0);
            GL.Rotate(-zAngle, 0, 0, 1);
            GL.Translate(-x, -y, 0);
            DisableBlending();
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height)
        {
            GL.Disable(EnableCap.Texture2D);
            GL.Color3(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0.01f, 0.01f); GL.Vertex3(x, y, this.ZValue2D);
            GL.TexCoord2(0.99f, 0.01f); GL.Vertex3(x + width, y, this.ZValue2D);
            GL.TexCoord2(0.99f, 0.99f); GL.Vertex3(x + width, y + height, this.ZValue2D);
            GL.TexCoord2(0.01f, 0.99f); GL.Vertex3(x, y + height, this.ZValue2D);
            GL.End();
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float angle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            GL.Translate(x, y, 0);
            GL.Rotate(angle, 0, 0, 1);
            DrawFillRectangle(color, -width / 2, -height / 2, width, height);
            GL.Rotate(-angle, 0, 0, 1);
            GL.Translate(-x, -y, 0);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float zAngle, float yAngle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            GL.Translate(x, y, 0);
            GL.Rotate(zAngle, 0, 0, 1);
            GL.Rotate(yAngle, 0, 1, 0);
            DrawFillRectangle(color, -width / 2, -height / 2, width, height);
            GL.Rotate(-yAngle, 0, 1, 0);
            GL.Rotate(-zAngle, 0, 0, 1);
            GL.Translate(-x, -y, 0);
        }

        public void DrawPolygon(System.Drawing.Pen pen, List<Vector2D> points)
        {
            GL.Disable(EnableCap.Texture2D);
            GL.LineWidth(pen.Width);
            if (pen.DashStyle == System.Drawing.Drawing2D.DashStyle.Dot)
                GL.Enable(EnableCap.LineStipple);//Linien dürfen gepunktet sein
            else
                GL.Disable(EnableCap.LineStipple);
            GL.Color3(pen.Color.R / 255.0f, pen.Color.G / 255.0f, pen.Color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            GL.Begin(PrimitiveType.LineStrip);
            foreach (Vector2D V in points)
            {
                GL.Vertex3(V.X, V.Y, this.ZValue2D);
            }
            GL.Vertex3(points[0].X, points[0].Y, this.ZValue2D);
            GL.End();
        }

        public void DrawFillPolygon(int textureId, Color colorFactor, List<Triangle2D> triangleList)
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor);// SetBlendingWithAlpha();
            GL.Enable(EnableCap.Texture2D);
            SetTextureFilter(TextureFilter.Point);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            GL.Begin(PrimitiveType.Triangles);
            foreach (Triangle2D triangle in triangleList)
            {
                GL.TexCoord2(triangle.P1.Textcoord.X, triangle.P1.Textcoord.Y);
                GL.Vertex3(triangle.P1.Position.X, triangle.P1.Position.Y, this.ZValue2D);

                GL.TexCoord2(triangle.P2.Textcoord.X, triangle.P2.Textcoord.Y);
                GL.Vertex3(triangle.P2.Position.X, triangle.P2.Position.Y, this.ZValue2D);

                GL.TexCoord2(triangle.P3.Textcoord.X, triangle.P3.Textcoord.Y);
                GL.Vertex3(triangle.P3.Position.X, triangle.P3.Position.Y, this.ZValue2D);
            }
            GL.End();
        }

        public void DrawFillPolygon(Color color, List<Triangle2D> triangleList)
        {
            GL.Disable(EnableCap.Texture2D);
            GL.Color3(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.

            GL.Begin(PrimitiveType.Triangles);
            foreach (Triangle2D triangle in triangleList)
            {
                GL.TexCoord2(triangle.P1.Textcoord.X, triangle.P1.Textcoord.Y);
                GL.Vertex3(triangle.P1.Position.X, triangle.P1.Position.Y, this.ZValue2D);

                GL.TexCoord2(triangle.P2.Textcoord.X, triangle.P2.Textcoord.Y);
                GL.Vertex3(triangle.P2.Position.X, triangle.P2.Position.Y, this.ZValue2D);

                GL.TexCoord2(triangle.P3.Textcoord.X, triangle.P3.Textcoord.Y);
                GL.Vertex3(triangle.P3.Position.X, triangle.P3.Position.Y, this.ZValue2D);
            }
            GL.End();
        }

        public void DrawCircle(System.Drawing.Pen pen, Vector2D pos, int radius)
        {
            GL.Disable(EnableCap.Texture2D);
            GL.PointSize(pen.Width);
            GL.Color3(pen.Color.R / 255.0f, pen.Color.G / 255.0f, pen.Color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            GL.Begin(PrimitiveType.Points);

            ShapeDrawer.DrawCircle(pos, radius, (p) => GL.Vertex3(p.X, p.Y, this.ZValue2D));

            GL.End();
        }

        public void DrawFillCircle(Color color, Vector2D pos, int radius)
        {
            GL.Disable(EnableCap.Texture2D);
            GL.PointSize(2);
            GL.Color3(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            GL.Begin(PrimitiveType.Points);

            ShapeDrawer.DrawFillCircle(pos, radius, (p) => GL.Vertex3(p.X, p.Y, this.ZValue2D));

            GL.End();
        }

        public void DrawCircleArc(Pen pen, Vector2D pos, int radius, float startAngle, float endAngle, bool withBorderLines)
        {
            CircleArcDrawer.DrawCircleArc(pos, radius, startAngle, endAngle, withBorderLines, (p) => DrawPixel(p, pen.Color, pen.Width));
        }
        public void DrawFillCircleArc(Color color, Vector2D pos, int radius, float startAngle, float endAngle)
        {
            GL.Disable(EnableCap.Texture2D);
            GL.PointSize(2);
            GL.Color3(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            GL.Begin(PrimitiveType.Points);

            CircleArcDrawer.DrawFillCircleArc(pos, radius, startAngle, endAngle, (p) => GL.Vertex3(p.X, p.Y, this.ZValue2D));

            GL.End();
        }

        public void DrawSprite(int textureId, int xCount, int yCount, int xBild, int yBild, int x, int y, int width, int height, Color colorFactor)
        {
            float xf = 1.0f / xCount, yf = 1.0f / yCount;
            UseAlphaBlendingAndDiscardTransparent(colorFactor);// SetBlendingWithAlpha();
            GL.Enable(EnableCap.Texture2D);
            SetTextureFilter(TextureFilter.Point);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(xBild * xf + 0.01f, yBild * yf + 0.01f); GL.Vertex3(x, y, this.ZValue2D);
            GL.TexCoord2((xBild + 1) * xf - 0.01f, yBild * yf + 0.01f); GL.Vertex3(x + width, y, this.ZValue2D);
            GL.TexCoord2((xBild + 1) * xf - 0.01f, (yBild + 1) * yf - 0.01f); GL.Vertex3(x + width, y + height, this.ZValue2D);
            GL.TexCoord2(xBild * xf + 0.01f, (yBild + 1) * yf - 0.01f); GL.Vertex3(x, y + height, this.ZValue2D);
            GL.End();
            DisableBlending();
        }

        public void EnableScissorTesting(int x, int y, int width, int height)
        {
            GL.Scissor(x, Height - y - height, width, height);	// Define Scissor Region
            GL.Enable(EnableCap.ScissorTest);								// Enable Scissor Testing
        }

        public void DisableScissorTesting()
        {
            GL.Disable(EnableCap.ScissorTest);								// Disable Scissor Testing
        }

        #endregion


        #region IDisposable Member

        public void Dispose()
        {
            //...
        }

        #endregion
    }
}
