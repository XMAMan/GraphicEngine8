//http://www.opengl-tutorial.org/   -> OpenGL 3.3+ Tutorial
//http://en.wikipedia.org/wiki/Vertex_Buffer_Object
//http://antongerdelan.net/opengl/vertexbuffers.html


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tao.OpenGl;
using Tao.Platform.Windows;
using Tao.FreeGlut;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GraphicMinimal;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using BitmapHelper;
using GraphicGlobal;
using GraphicGlobal.Rasterizer2DFunctions;

namespace GraphicPipelineOpenGLv1_0
{
    public class GraphicPipelineOpenGLv1_0 : IGraphicPipeline
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
        private SimpleOpenGlControl simpleOpenGlControl = null;
        private List<MyTexture> texturen = new List<MyTexture>(); //Texturedateipfad+Texturgröße | Gl.glGenTextures()
        private Dictionary<int, Cubemap> cubemaps = new Dictionary<int, Cubemap>(); //[TexturID | 6 2D-Texturen]        
        //private Dictionary<int, TriangleArrayData> triangleArrays = new Dictionary<int, TriangleArrayData>(); //TriangleArray-ID | Vertexpuffer-IDs
        //private int[] selectBufferForMouseHitTest;
        private int fontbase; //OpenGL-Displayliste zum speichern der Schrift

        private ShaderHelper shader = null;
        private int activeTexture = 0;
        #endregion

        #region Interface-Methoden

        public int Width
        {
            get
            {
                return simpleOpenGlControl.Width;
            }
        }
        public int Height
        {
            get
            {
                return simpleOpenGlControl.Height;
            }
        }

        #region 3D
        public GraphicPipelineOpenGLv1_0(bool useOldShaders = false)
        {
            this.simpleOpenGlControl = (SimpleOpenGlControl)CreateControl();
            simpleOpenGlControl.InitializeContexts();
            simpleOpenGlControl.SwapBuffers();
            simpleOpenGlControl.SizeChanged += new EventHandler(resize);
            resize(null, null);
            fontbase = CreateFont("Consolas");
            shader = new ShaderHelper(useOldShaders);
        }

        private static Control CreateControl()
        {
            return new Tao.Platform.Windows.SimpleOpenGlControl()
            {
                AccumBits = ((byte)(0)),
                AutoCheckErrors = false,
                AutoFinish = false,
                AutoMakeCurrent = true,
                AutoSwapBuffers = true,
                BackColor = System.Drawing.Color.White,
                ColorBits = ((byte)(32)),
                DepthBits = ((byte)(16)),
                Dock = System.Windows.Forms.DockStyle.Fill,
                Location = new System.Drawing.Point(0, 0),
                Name = "simpleOpenGlControl1",
                Size = new System.Drawing.Size(295, 254),
                StencilBits = ((byte)(8)),
                TabIndex = 1,
            };
        }

        public Control DrawingControl
        {
            get
            {
                return this.simpleOpenGlControl;
            }
        }

        public bool UseDisplacementMapping { get; set; } = false;

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
                Gl.glShadeModel(Gl.GL_FLAT);
            }
            else
            {
                shader["DoFlatShading"] = 0;
                Gl.glShadeModel(Gl.GL_SMOOTH);
            }
        }

        public void Resize(int width, int height)
        {
            //Tue nichts, da nur der Veränderung des SimpleOpenGlControl(DrawingArea)-Objektes zum Resize führt
        }

        private void resize(object sender, EventArgs e)
        {
            Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.5f);					// black background
 
            //Gl.glEnable(Gl.GL_NORMALIZE);

            //Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glClearDepth(1.0f);										// depth buffer setup
            Gl.glEnable(Gl.GL_DEPTH_TEST);								// enables depth testing
            Gl.glDepthFunc(Gl.GL_LEQUAL);								// type of depth testing
            Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);	// nice perspective calculations

            //Gl.glEnable(Gl.GL_BLEND);
            //Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);			// Enable Alpha Blending (disable alpha testing)
            //Gl.glAlphaFunc(Gl.GL_GREATER, 0.01f);								// Set Alpha Testing     (disable blending)
            //Gl.glEnable(Gl.GL_ALPHA_TEST);									// Enable Alpha Testing  (disable blending)

            Gl.glLineWidth(5);

            Gl.glLineStipple(1, unchecked((short)0xAAAA));

            Gl.glViewport(0, 0, simpleOpenGlControl.Width, simpleOpenGlControl.Height);
            SetProjectionMatrix3D();

            Gl.glMatrixMode(Gl.GL_MODELVIEW);								// Select The Modelview Matrix
            Gl.glLoadIdentity();									// Reset The Modelview Matrix

            UpdateModelViewMatrix();
        }

        public void SetProjectionMatrix3D(int screenWidth = 0, int screenHight = 0, float fov = 45, float zNear = 0.001f, float zFar = 3000)
        {
            if (screenWidth == 0) screenWidth = simpleOpenGlControl.Width;
            if (screenHight == 0) screenHight = simpleOpenGlControl.Height;

            Gl.glMatrixMode(Gl.GL_PROJECTION);							// Select The Projection Matrix
            Gl.glLoadIdentity();										// Reset The Projection Matrix

            if (shader != null) shader.Mode = ShaderHelper.ShaderMode.Normal;
            Glu.gluPerspective(fov, (float)screenWidth / (float)screenHight, zNear, zFar);// Calculate The Aspect Ratio Of The Window
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
        }

        public void SetProjectionMatrix2D(float left = 0, float right = 0, float bottom = 0, float top = 0, float znear = 0, float zfar = 0)
        {
            if (shader != null) shader.Mode = ShaderHelper.ShaderMode.None;

            Gl.glMatrixMode(Gl.GL_PROJECTION);							// Select The Projection Matrix
            Gl.glLoadIdentity();										// Reset The Projection Matrix
            
            if (left == 0 && right == 0)
            {
                Gl.glOrtho(0.0f, simpleOpenGlControl.Width, simpleOpenGlControl.Height, 0.0f, -1000.0f, 1000.0f);				// Create Ortho 640x480 View (0,0 At Top Left)
            }
            else
            {
                Gl.glOrtho(left, right, bottom, top, znear, zfar);
            }

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
        }

        public void SetViewport(int startX, int startY, int width, int height)
        {
            //https://registry.khronos.org/OpenGL-Refpages/gl4/html/glViewport.xhtml
            //(x,y) Specify the lower left corner of the viewport rectangle, in pixels. The initial value is (0,0).
            Gl.glViewport(startX, this.Height - startY - height, width, height);
        }

        public void FlippBuffer()
        {
            simpleOpenGlControl.MakeCurrent();
            Gl.glFlush();
            if (simpleOpenGlControl != null) simpleOpenGlControl.Invalidate();
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


            Gl.glReadPixels(0, 0, this.DrawingControl.Width, this.DrawingControl.Height, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, bitmapdata.Scan0);//Bilddaten von OpenGL anfordern 
            image.UnlockBits(bitmapdata);
            image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return image;
        }

        public Bitmap GetDataFromDepthBuffer()
        {	
            int width = this.DrawingControl.Width;
            int height = this.DrawingControl.Height;
            float[] depthValues = new float[width * height];
            Gl.glReadPixels(0, 0, width, height, Gl.GL_DEPTH_COMPONENT, Gl.GL_FLOAT, depthValues);//Bilddaten von OpenGL anfordern 

            return BitmapHelp.ConvertDepthValuesToBitmap(BitmapHelp.ConvertFlatArrayTo2DArray(depthValues, width, height), true);
        }

        public void ClearColorBuffer(Color clearColor)
        {
            Gl.glClearColor(clearColor.R / 255.0f, clearColor.G / 255.0f, clearColor.B / 255.0f, 0.5f);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
        }

        public void ClearColorDepthAndStencilBuffer(Color clearColor)
        {
            Gl.glClearColor(clearColor.R / 255.0f, clearColor.G / 255.0f, clearColor.B / 255.0f, clearColor.A / 255f);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT | Gl.GL_STENCIL_BUFFER_BIT); 	       
        }

        public void ClearStencilBuffer()
        {
            Gl.glClear(Gl.GL_STENCIL_BUFFER_BIT);
        }

        public void ClearDepthAndStencilBuffer()
        {
            Gl.glClear(Gl.GL_DEPTH_BUFFER_BIT | Gl.GL_STENCIL_BUFFER_BIT); 	       
        }

        public void EnableWritingToTheColorBuffer()
        {
            Gl.glColorMask(1, 1, 1, 1); //bestimmt, welche der Farbkomponenten in den Framebuffer geschrieben werden können.
        }

        public void DisableWritingToTheColorBuffer()
        {
            Gl.glColorMask(0, 0, 0, 0); //aktiviert und deaktiviert das Schreiben der Tiefenkomponenten in den Tiefenpuffer.
        }

        public void EnableWritingToTheDepthBuffer()
        {
            Gl.glDepthMask(Gl.GL_TRUE);
        }

        public void DisableWritingToTheDepthBuffer()
        {
            Gl.glDepthMask(Gl.GL_FALSE);
        }

        public void SetModelViewMatrixToIdentity()
        {
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();

            UpdateModelViewMatrix();
        }

        //gibt die Nr. der geladenen Texture zurück, im Fehlerfall 0
        public int GetTextureId(Bitmap image)
        {
            MyTexture tex = new MyTexture(CreateTextureFromBitmap(image/*, false*/), image.Width, image.Height, MyTexture.TexturType.Color2D);
            texturen.Add(tex);
            return tex.TextureID; 
        }

        //gibt die Nr. der geladenen Texture zurück, im Fehlerfall 0
        private int CreateTextureFromBitmap(Bitmap image/*, bool makeFirstPixelTransparent*/)
        {
            /*if (makeFirstPixelTransparent)
                image = BitmapHelp.TransformColorToMaxAlpha(image, image.GetPixel(0, 0));
            else
                image = BitmapHelp.TransformBlackColorToMaxAlpha(image);*/

            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);//rect ist der Bereich aus dem Bild, aus dem die Texture erstellet wird. Muss 2^n groß sein(Bsp: 512). So groß ist dann auch die Texture
            int[] texture = new int[1];	// Texture array
            System.Drawing.Imaging.BitmapData bitmapdata;

            try
            {
                bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }
            catch (Exception) { return 0; }

            Gl.glGenTextures(1, texture);

            // Create Linear Filtered Texture
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture[0]);
            Gl.glTexParameterf(texture[0], Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP);
            Gl.glTexParameterf(texture[0], Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, /*(int)Gl.GL_RGB*/ Gl.GL_RGBA, rect.Width, rect.Height, 0, Gl.GL_BGRA_EXT, Gl.GL_UNSIGNED_BYTE, bitmapdata.Scan0);
            image.UnlockBits(bitmapdata);

            return texture[0];
        }

        public Bitmap GetTextureData(int textureID)
        {
            var tex = texturen.FirstOrDefault(x => x.TextureID == textureID);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, textureID);

            byte[] data = new byte[4 * tex.Width * tex.Height];
            switch (tex.TextureType)
            {
                case MyTexture.TexturType.Color2D:
                    Gl.glGetTexImage(Gl.GL_TEXTURE_2D, 0, Gl.GL_BGRA_EXT, Gl.GL_UNSIGNED_BYTE, data);
                    break;
                case MyTexture.TexturType.Depth2D:
                    Gl.glGetTexImage(Gl.GL_TEXTURE_2D, 0, Gl.GL_DEPTH_COMPONENT, Gl.GL_FLOAT, data);
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
            int[] texture = new int[1];	// Texture array
            Gl.glGenTextures(1, texture);

            MyTexture tex = new MyTexture(texture[0], width, height, MyTexture.TexturType.Color2D);
            texturen.Add(tex);
            return tex.TextureID;
        }

        public void CopyScreenToTexture(int textureID)
        {
            int[] viewport = new int[4];									
            Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);						// Retrieves The Viewport Values (X, Y, Width, Height)
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, textureID);
            Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 0, 0, this.Width, this.Height, 0);
        }

        class Framebuffer
        {
            public int? ColorTextureId;
            public int? DepthTextureId;
            public int Width;
            public int Height;
        }

        private Dictionary<int, Framebuffer> framebuffers = new Dictionary<int, Framebuffer>();

        public int CreateFramebuffer(int width, int height, bool withColorTexture, bool withDepthTexture)
        {
            // The framebuffer, which regroups 0, 1, or more textures, and 0 or 1 depth buffer.
            int[] FramebufferName = new int[1];
            Gl.glGenFramebuffersEXT(1, FramebufferName);
            Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, FramebufferName[0]);

            int? colorTextureId = null, depthTextureId = null;

            if (withColorTexture)
            {
                int renderedTexture = CreateColorTexture(width, height);

                // Set "renderedTexture" as our colour attachement #0
                Gl.glFramebufferTextureEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_COLOR_ATTACHMENT0_EXT, renderedTexture, 0);

                // Set the list of draw buffers.
                int[] DrawBuffers = new int[] //Hier wird nur die Verknüpfung der Farbtexturen mit den Pixel-Shader layout(location = 0) hergestellt.
                {                                                     //Für die Tiefenpuffertextur braucht man das nicht, da es ja nur eine gibt.
                    Gl.GL_COLOR_ATTACHMENT0_EXT                                       //Das 1. Feld ist ColorAttachment0. Dort wurde die Farbtextur gebunden. Das heißt beim Pixelshader landet die Variable 'color' in der Farbtextur, weil sie auf 'location = 0' zeigt. -> layout(location = 0) out vec3 color;
                };
                Gl.glDrawBuffers(1, DrawBuffers); // "1" is the size of DrawBuffers

                colorTextureId = renderedTexture;
            }

            if (withDepthTexture) //Render die Tiefenwerte in Textur (Langsamer, als wenn ich in Puffer render)
            {
                int depthTexture = CreateDepthTexture(width, height);
                Gl.glFramebufferTextureEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_DEPTH_ATTACHMENT_EXT, depthTexture, 0);
                //Gl.glFramebufferTextureEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_DEPTH_STENCIL_EXT, depthTexture, 0);

                depthTextureId = depthTexture;

                //GL.DrawBuffer(DrawBufferMode.None);
            }
            else //Render die Tiefenwerte in 'Standard'-Puffer
            {
                int depthrenderbuffer = CreateDepthBuffer(width, height);
                //Gl.glFramebufferRenderbufferEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_DEPTH_ATTACHMENT_EXT, Gl.GL_RENDERBUFFER_EXT, depthrenderbuffer); // attach a renderbuffer object to a framebuffer object
                Gl.glFramebufferRenderbufferEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_DEPTH_STENCIL_EXT, Gl.GL_RENDERBUFFER_EXT, depthrenderbuffer); // attach a renderbuffer object to a framebuffer object
            }

            // Always check that our framebuffer is ok
            if (Gl.glCheckFramebufferStatusEXT(Gl.GL_FRAMEBUFFER_EXT) != Gl.GL_FRAMEBUFFER_COMPLETE_EXT)
            {
                string fboMsg = Gl.glCheckFramebufferStatusEXT(Gl.GL_FRAMEBUFFER_EXT).ToString();
                throw new Exception("OpenGL 1.0: Framebuffer konnte nicht erstellt werden -> " + fboMsg);
            }

            framebuffers.Add(FramebufferName[0], new Framebuffer() { ColorTextureId = colorTextureId, DepthTextureId = depthTextureId, Width = width, Height = height });

            return FramebufferName[0];
        }

        private int CreateColorTexture(int width, int height)
        {
            // The texture we're going to render to
            int[] renderedTexture = new int[1];
            Gl.glGenTextures(1, renderedTexture);

            // "Bind" the newly created texture : all future texture functions will modify this texture
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, renderedTexture[0]);

            // Give an empty image to OpenGL ( the last "0" )
            //Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, width, height, 0, Gl.GL_RGB, Gl.GL_UNSIGNED_BYTE, IntPtr.Zero);
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, width, height, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, IntPtr.Zero);


            // Poor filtering. Needed !
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
            
            texturen.Add(new MyTexture(renderedTexture[0], width, height, MyTexture.TexturType.Color2D));

            return renderedTexture[0];
        }

        private int CreateDepthTexture(int width, int height)
        {
            int[] depthTexture = new int[1];
            Gl.glGenTextures(1, depthTexture);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, depthTexture[0]);
            //Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_DEPTH_COMPONENT24, width, height, 0, Gl.GL_DEPTH_COMPONENT, Gl.GL_FLOAT, IntPtr.Zero);
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_DEPTH24_STENCIL8_EXT, width, height, 0, Gl.GL_DEPTH_STENCIL_EXT, Gl.GL_UNSIGNED_INT_24_8_EXT, IntPtr.Zero);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);

            //Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP);
            //Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP);

            Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_BORDER);
            Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_BORDER);
            Gl.glTexParameterfv(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_BORDER_COLOR, new float[] { 1, 1, 1, 1 });

            texturen.Add(new MyTexture(depthTexture[0], width, height, MyTexture.TexturType.Depth2D));

            return depthTexture[0];
        }

        private int CreateDepthBuffer(int width, int height)
        {
            int[] depthrenderbuffer = new int[1];
            Gl.glGenRenderbuffersEXT(1, depthrenderbuffer);
            Gl.glBindRenderbufferEXT(Gl.GL_RENDERBUFFER_EXT, depthrenderbuffer[0]);
            Gl.glRenderbufferStorageEXT(Gl.GL_RENDERBUFFER_EXT, Gl.GL_DEPTH_COMPONENT24, width, height); //create and initialize a renderbuffer object's data store

            return depthrenderbuffer[0];
        }  

        public void EnableRenderToFramebuffer(int framebufferId)
        {
            // Render to our framebuffer
            Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, framebufferId);
            Gl.glViewport(0, 0, framebuffers[framebufferId].Width, framebuffers[framebufferId].Height); // Render on the whole framebuffer, complete from the lower left corner to the upper right
        }

        public void DisableRenderToFramebuffer()
        {
            Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, 0);
            Gl.glViewport(0, 0, simpleOpenGlControl.Width, simpleOpenGlControl.Height);
        }

        public int GetColorTextureIdFromFramebuffer(int framebufferId)
        {
            return (int)framebuffers[framebufferId].ColorTextureId;
        }

        public int GetDepthTextureIdFromFramebuffer(int framebufferId)
        {
            return (int)framebuffers[framebufferId].DepthTextureId;
        }

        public void SetTextureMatrix(Matrix3x3 matrix3x3)
        {
            if (shader != null) shader.SetUniformVariableMatrix3x3("TextureMatrix", matrix3x3);
        }

        public void SetTextureScale(Vector2D scale)
        {
            shader.SetUniformVariable("TexturScaleFaktorX", scale.X);
            shader.SetUniformVariable("TexturScaleFaktorY", scale.Y);
        }

        public void SetTesselationFactor(float tesselationFactor)              // Wird bei Displacementmapping benötigt. In so viele Dreiecke wird Dreieck zerlegt
        {
            shader.SetUniformVariable("TesselationFactor", tesselationFactor);
        }

        public void SetTextureHeighScaleFactor(float textureHeighScaleFactor)    // Höhenskalierung bei Displacement- und Parallaxmapping
        {
            shader.SetUniformVariable("HeighScaleFactor", textureHeighScaleFactor);
        }

        public void SetTextureMode(TextureMode textureMode)
        {
            int modus = 0;
            switch (textureMode)
            {
                case TextureMode.Repeat:
                    modus = Gl.GL_REPEAT;
                    break;
                case TextureMode.Clamp:
                    modus = Gl.GL_CLAMP;
                    break;
            }

            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, modus);//GL_REPEAT, GL_CLAMP
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, modus);
        }

        public int GetTriangleArrayId(Triangle[] data)
        {
            return shader.TriangleDrawer.GetTriangleArrayId(data);
        }

        public void DrawTriangleArray(int triangleArrayId)
        {
            shader.TriangleDrawer.DrawTriangleArray(triangleArrayId);
        }

        public void RemoveTriangleArray(int triangleArrayId)
        {
            shader.TriangleDrawer.RemoveTriangleArray(triangleArrayId);
        }        

        public void DrawTriangleStrip(Vector3D v1, Vector3D v2, Vector3D v3, Vector3D v4)
        {
            Gl.glBegin(Gl.GL_TRIANGLE_STRIP);
            Gl.glVertex3f(v1.X, v1.Y, v1.Z);
            Gl.glVertex3f(v2.X, v2.Y, v2.Z);
            Gl.glVertex3f(v3.X, v3.Y, v3.Z);
            Gl.glVertex3f(v4.X, v4.Y, v4.Z);
            Gl.glEnd();
        }

        public void DrawLine(Vector3D v1, Vector3D v2)
        {
            shader.SetModeForDrawingLinesOrPoints();

            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex3f(v1.X, v1.Y, v1.Z);
            Gl.glVertex3f(v2.X, v2.Y, v2.Z);
            Gl.glEnd();
        }

        public void SetLineWidth(float lineWidth)
        {
            Gl.glLineWidth(lineWidth);
        }

        public void DrawPoint(Vector3D position)
        {
            shader.SetModeForDrawingLinesOrPoints();

            Gl.glBegin(Gl.GL_POINTS);
            Gl.glVertex3fv(position.Float3f);
            Gl.glEnd();
        }

        public void SetPointSize(float size)
        {
            Gl.glPointSize(size);
        }

        public Color GetPixelColorFromColorBuffer(int x, int y)
        {	
            byte[] data = new byte[4];
            Gl.glReadPixels(x, this.DrawingControl.Height - y, 1, 1, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, data);//Bilddaten von OpenGL anfordern 
            return Color.FromArgb(data[3], data[2], data[1], data[0]);
        }

        public Size GetTextureSize(int textureId)
        {
            var tex = texturen.FirstOrDefault(x => x.TextureID == textureId);
            return new Size(tex.Width, tex.Height);
        }

        public Matrix4x4 GetInverseModelMatrix(Vector3D position, Vector3D orientation, float size)
        {
            float[] matrix = new float[16];

            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            Gl.glScalef(1 / size, 1 / size, 1 / size);
            Gl.glRotatef(-orientation.Z, 0.0f, 0.0f, 1.0f);
            Gl.glRotatef(-orientation.Y, 0.0f, 1.0f, 0.0f);
            Gl.glRotatef(-orientation.X, 1.0f, 0.0f, 0.0f);                
            
            Gl.glTranslatef(-position.X, -position.Y, -position.Z);

            Gl.glGetFloatv(Gl.GL_MODELVIEW_MATRIX, matrix);
            Gl.glPopMatrix();

            return new Matrix4x4(matrix);
        }

        public Matrix4x4 GetModelMatrix(Vector3D position, Vector3D orientation, float size)
        {
            float[] matrix = new float[16];

            Gl.glPushMatrix();
            Gl.glLoadIdentity();
            Gl.glTranslatef(position.X, position.Y, position.Z);            
            Gl.glRotatef(orientation.X, 1.0f, 0.0f, 0.0f);
            Gl.glRotatef(orientation.Y, 0.0f, 1.0f, 0.0f);
            Gl.glRotatef(orientation.Z, 0.0f, 0.0f, 1.0f);
            Gl.glScalef(size, size, size);
            
            Gl.glGetFloatv(Gl.GL_MODELVIEW_MATRIX, matrix);
            Gl.glPopMatrix();

            return new Matrix4x4(matrix);
        }

        public void PushMatrix()
        {
            Gl.glPushMatrix();
        }

        public void PopMatrix()
        {
            Gl.glPopMatrix();

            UpdateModelViewMatrix();
        }

        public void PushProjectionMatrix()
        {
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
        }

        public void PopProjectionMatrix()
        {
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
        }

        public void MultMatrix(Matrix4x4 matrix)
        {
            Gl.glMultMatrixf(matrix.Values);

            UpdateModelViewMatrix();
        }

        public void Scale(float size)
        {
            Gl.glScalef(size, size, size);

            UpdateModelViewMatrix();
        }

        public Matrix4x4 GetProjectionMatrix()
        {
            float[] p = new float[16];   // projection matrix
            Gl.glGetFloatv(Gl.GL_PROJECTION_MATRIX, p);
            return new Matrix4x4(p);
        }

        public Matrix4x4 GetModelViewMatrix()
        {
            float[] mv = new float[16];  // model-view matrix
            Gl.glGetFloatv(Gl.GL_MODELVIEW_MATRIX, mv);
            return new Matrix4x4(mv);
        }

        public void SetActiveTexture0() //Farbtextur
        {
            activeTexture = 0;
            Gl.glActiveTextureARB(Gl.GL_TEXTURE0_ARB);
        }

        public void SetActiveTexture1() //Bumpmaptextur
        {
            activeTexture = 1;
            Gl.glActiveTextureARB(Gl.GL_TEXTURE1_ARB);
        }

        public void EnableTexturemapping()
        {
            if (activeTexture == 0)
                shader["UseTexture0"] = 1;

            if (activeTexture == 1)
                shader["UseTexture1"] = 1;

            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glDisable(Gl.GL_COLOR_MATERIAL); //keine Farbfestlegung mit glColor möglich; Dafür ist nun Texturmapping möglich
        }

        public void SetTextureFilter(TextureFilter filter)
        {
            switch (filter)
            {
                case TextureFilter.Point:
                    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST); //GL_NEAREST ist im Normalfall schneller als GL_LINEAR, aber produziert auch Bilder mit schärferen Kanten, 
                    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST); //da der Übergang zwischen den Texturelementen nicht so weich ist. Voreinstellung für GL_TEXTURE_MAG_FILTER ist GL_LINEAR.
                    break;
                case TextureFilter.Linear:
                    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR); //GL_NEAREST ist im Normalfall schneller als GL_LINEAR, aber produziert auch Bilder mit schärferen Kanten, 
                    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR); //da der Übergang zwischen den Texturelementen nicht so weich ist. Voreinstellung für GL_TEXTURE_MAG_FILTER ist GL_LINEAR.
                    break;
                case TextureFilter.Anisotroph:
                    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_TEXTURE_MAX_ANISOTROPY_EXT); //GL_NEAREST ist im Normalfall schneller als GL_LINEAR, aber produziert auch Bilder mit schärferen Kanten, 
                    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_TEXTURE_MAX_ANISOTROPY_EXT); //da der Übergang zwischen den Texturelementen nicht so weich ist. Voreinstellung für GL_TEXTURE_MAG_FILTER ist GL_LINEAR.
                    break;
            }
        }

        public void DisableTexturemapping()
        {
            if (activeTexture == 0)
                shader["UseTexture0"] = 0;

            if (activeTexture == 1)
                shader["UseTexture1"] = 0;

            Gl.glDisable(Gl.GL_TEXTURE_2D);
            Gl.glEnable(Gl.GL_COLOR_MATERIAL); //Farbfestlegung mit glColor möglich
        }

        public int CreateCubeMap(int cubeMapSize = 256)
        {
            Cubemap cub = new Cubemap();
            cub.CubeMapSize = cubeMapSize;

            cub.FBO = new int[1];
            cub.ColorTexture = new int[1];
            Gl.glGenTextures(1, cub.ColorTexture);
            Gl.glBindTexture(Gl.GL_TEXTURE_CUBE_MAP, cub.ColorTexture[0]);
            Gl.glTexParameteri(Gl.GL_TEXTURE_CUBE_MAP, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexParameteri(Gl.GL_TEXTURE_CUBE_MAP, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexParameteri(Gl.GL_TEXTURE_CUBE_MAP, Gl.GL_TEXTURE_WRAP_R, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexParameteri(Gl.GL_TEXTURE_CUBE_MAP, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
            Gl.glTexParameteri(Gl.GL_TEXTURE_CUBE_MAP, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);

            for (int i = 0; i < 6; i++)
            {
                Gl.glTexImage2D(Gl.GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, 0, Gl.GL_RGBA16F_ARB, cubeMapSize, cubeMapSize, 0, Gl.GL_BGRA, Gl.GL_HALF_FLOAT_ARB, IntPtr.Zero);
            }
            Gl.glBindTexture(Gl.GL_TEXTURE_CUBE_MAP, 0);

            Gl.glGenFramebuffersEXT(1, cub.FBO);
            Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, cub.FBO[0]);

            //glFramebufferTexture2DEXT = Attach Textur to FrameBuffer
            for (int i = 0; i < 6; i++)
            {
                Gl.glFramebufferTexture2DEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_COLOR_ATTACHMENT0_EXT + i, Gl.GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, cub.ColorTexture[0], 0);
            }

            cub.DepthTexture = CreateDepthTexture(cubeMapSize, cubeMapSize);
            Gl.glFramebufferTextureEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_DEPTH_ATTACHMENT_EXT, cub.DepthTexture, 0); //Gl.GL_DEPTH_ATTACHMENT_EXT | Gl.GL_STENCIL_ATTACHMENT_EXT

            string fboMsg = (Gl.glCheckFramebufferStatusEXT(Gl.GL_FRAMEBUFFER_EXT) == Gl.GL_FRAMEBUFFER_COMPLETE_EXT).ToString(); //https://docs.gl/gl3/glCheckFramebufferStatus
            Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, 0);

            int newID = 1;
            if (cubemaps.Keys.Count > 0)
                newID = cubemaps.Keys.Max() + 1;

            cubemaps.Add(newID, cub);
            return newID;
        }

        public void EnableRenderToCubeMap(int cubemapID, int cubemapSide, Color clearColor)
        {
            Cubemap cub = cubemaps[cubemapID];

            Gl.glViewport(0, 0, cub.CubeMapSize, cub.CubeMapSize);
            Gl.glEnable(Gl.GL_TEXTURE_CUBE_MAP_EXT);//EnableCap.TextureCubeMapSeamless 
            Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, cub.FBO[0]);

            Gl.glDrawBuffer(Gl.GL_COLOR_ATTACHMENT0_EXT + cubemapSide); //gibt an, in welche Farbpuffer gezeichnet werden soll
            Gl.glFramebufferTexture2DEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_COLOR_ATTACHMENT0_EXT + cubemapSide, Gl.GL_TEXTURE_CUBE_MAP_POSITIVE_X + cubemapSide, cub.ColorTexture[0], 0);
            Gl.glFramebufferTexture2DEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_STENCIL_ATTACHMENT_EXT | Gl.GL_DEPTH_ATTACHMENT_EXT, Gl.GL_TEXTURE_2D, cub.DepthTexture, 0);


            ClearColorDepthAndStencilBuffer(clearColor);
        }

        public Bitmap GetColorDataFromCubeMapSide(int cubemapID, int cubemapSide)
        {
            Cubemap cub = cubemaps[cubemapID];
            Gl.glBindTexture(Gl.GL_TEXTURE_CUBE_MAP, cub.ColorTexture[0]);
            byte[] data = new byte[4 * cub.CubeMapSize * cub.CubeMapSize];
            Gl.glGetTexImage(Gl.GL_TEXTURE_CUBE_MAP_POSITIVE_X + cubemapSide, 0, Gl.GL_BGRA_EXT, Gl.GL_UNSIGNED_BYTE, data);

            Bitmap result = new Bitmap(cub.CubeMapSize, cub.CubeMapSize);
            BitmapData resultData = result.LockBits(new Rectangle(0, 0, cub.CubeMapSize, cub.CubeMapSize), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Marshal.Copy(data, 0, resultData.Scan0, data.Length);
            result.UnlockBits(resultData);

            result.RotateFlip(RotateFlipType.RotateNoneFlipY);

            return result;
        }

        public void DisableRenderToCubeMap()
        {
            Gl.glViewport(0, 0, Width, Height);
            Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, 0); 
            Gl.glDisable(Gl.GL_TEXTURE_CUBE_MAP_EXT);
        }

        public void EnableAndBindCubemapping(int cubemapID)
        {
            shader["UseCubemap"] = 1;
            Gl.glActiveTextureARB(Gl.GL_TEXTURE2_ARB);
            Gl.glBindTexture(Gl.GL_TEXTURE_CUBE_MAP_EXT, cubemaps[cubemapID].ColorTexture[0]);
        }

        public void DisableCubemapping()
        {
            shader["UseCubemap"] = 0;
        }

        public bool ReadFromShadowmap
        {
            set => shader["UseShadowmap"] = value ? 1 : 0;
        }

        public int CreateShadowmap(int width, int height)
        {
            return CreateFramebuffer(width, height, true, true);
        }

        public void EnableRenderToShadowmap(int shadowmapId)
        {
            EnableRenderToFramebuffer(shadowmapId);

            //Gl.glDrawBuffer(Gl.GL_NONE); // No color buffer is drawn to.
            //Gl.glDrawBuffer(Gl.GL_COLOR_ATTACHMENT0_EXT);//gibt an, in welche Farbpuffer gezeichnet werden soll
            //Gl.glDrawBuffer(Gl.GL_DEPTH_ATTACHMENT_EXT);

            shader.Mode = ShaderHelper.ShaderMode.CreateShadowMap;
            shader.LockShaderModeWriting = true;
        }

        public void BindShadowTexture(int shadowmapId)
        {
            if (shader.Mode != ShaderHelper.ShaderMode.CreateShadowMap)
            {
                Gl.glActiveTexture(Gl.GL_TEXTURE3);
                Gl.glBindTexture(Gl.GL_TEXTURE_2D, GetDepthTextureIdFromFramebuffer(shadowmapId));
            }
        }

        public void DisableRenderToShadowmapTexture()
        {
            shader.LockShaderModeWriting = false;
            Gl.glViewport(0, 0, Width, Height);
            Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, 0);
        }

        public void SetShadowmapMatrix(Matrix4x4 shadowMatrix)
        {
            shader["ShadowmappingTextureId"] = 0;
            //Matrix für Shadow-Mapping setzen
            shader.SetUniformVariable("ShadowMapMatrix[" + 0 + "]", shadowMatrix);
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

        private void UpdateModelViewMatrix()
        {
            if (shader != null)
            {
                Matrix4x4 modelviewMatrix = GetModelViewMatrix();
                Matrix4x4 projectionMatrix = GetProjectionMatrix();
                Matrix4x4 objToWorld = modelviewMatrix * this.inverseCameraMatrix;
                Matrix4x4 objToClipMatrix = modelviewMatrix * projectionMatrix;
                Matrix4x4 worldToObj = Matrix4x4.Invert(objToWorld);
                shader.SetUniformVariable("ObjToClipMatrix", objToClipMatrix);
                shader.SetUniformVariable("ObjToWorldMatrix", objToWorld);
                shader.SetUniformVariable("CameraMatrix", this.cameraMatrix);
                shader.SetUniformVariable("NormalMatrix", Matrix4x4.Transpose(worldToObj));
                shader.SetUniformVariable("WorldToObj", worldToObj);
            }
        }

        private Matrix4x4 cameraMatrix = Matrix4x4.Ident();
        private Matrix4x4 inverseCameraMatrix = Matrix4x4.Ident();
        public void SetModelViewMatrixToCamera(Camera camera)
        {
            this.cameraMatrix = Matrix4x4.LookAt(camera.Position, camera.Forward, camera.Up);
            this.inverseCameraMatrix = Matrix4x4.InverseLookAt(camera.Position, camera.Forward, camera.Up);
            SetModelViewMatrixToIdentity();
            Glu.gluLookAt(camera.Position.X, camera.Position.Y, camera.Position.Z, camera.Position.X + camera.Forward.X, camera.Position.Y + camera.Forward.Y, camera.Position.Z + camera.Forward.Z, camera.Up.X, camera.Up.Y, camera.Up.Z);

            UpdateModelViewMatrix();

            shader.SetUniformVariable("CameraPosition", camera.Position);
        }

        //OpenGL1 multipliziert die angegebenen Position bei glLightfv mit der aktuellen ModelViewMatrix und die
        //Spotdirection mit der aktuellen Transpose(Inverse(ModelviewMatrix)). Ich kann nun entweder die MV-Matrix
        //auf Ident setzen und selber dann die Position/Richtung der Lichter in Welt- oder Eye-Space transformieren
        //oder ich lasse das durch glLightfv erledigen.
        public void SetPositionOfAllLightsources(List<RasterizerLightsource> lights)
        {
            Gl.glPushMatrix();
            Gl.glLoadIdentity(); //Setze Matrix auf Ident um die Lichtposition in Weltkoordinaten angeben zu können

            int i;
            float[] lightPosition;
            for (i = 0; i < lights.Count; i++)
            {
                lightPosition = new float[] { lights[i].Position.X, lights[i].Position.Y, lights[i].Position.Z, 1}; //4. Parameter muss immer 1 sein. Er sagt nichts darüber aus, ob die Lichtquelle eine Punkt- oder Richtungslichtquelle ist

                Gl.glEnable(Gl.GL_LIGHT0 + i);
                Gl.glLightfv(Gl.GL_LIGHT0 + i, Gl.GL_POSITION, lightPosition); //Die Koordinaten werden mit der aktuellen Modelviewmatrix in Eye-Koordinaten transformiert. D.h., die momentan gesetzte MV muss die Kamera-Matrix sein.
                Gl.glLightf(Gl.GL_LIGHT0 + i, Gl.GL_CONSTANT_ATTENUATION, lights[i].ConstantAttenuation);
                Gl.glLightf(Gl.GL_LIGHT0 + i, Gl.GL_LINEAR_ATTENUATION, lights[i].LinearAttenuation);
                Gl.glLightf(Gl.GL_LIGHT0 + i, Gl.GL_QUADRATIC_ATTENUATION, lights[i].QuadraticAttenuation);

                Gl.glLightf(Gl.GL_LIGHT0 + i, Gl.GL_SPOT_CUTOFF, lights[i].SpotCutoff); //Der Öffnungswinkel muss zwischen 0 und 90 liegen. Wenn 180, dann ist das ein Punktlicht, was in alle Richtungen strahlt
                if (lights[i].SpotCutoff == 180)
                {
                    Gl.glLightfv(Gl.GL_LIGHT0 + i, Gl.GL_SPOT_DIRECTION, new float[] { 0, 0, 0 }); //Bei Punktlicht muss man (0,0,0) als Spot-Direction angeben, da er sonst trotzdem in eine Richtung strahlt
                }
                else
                {
                    Gl.glLightfv(Gl.GL_LIGHT0 + i, Gl.GL_SPOT_DIRECTION, lights[i].SpotDirection.Float3f);//Richtung: Muss in Objektkoordinaten angegeben werden
                }
                Gl.glLightf(Gl.GL_LIGHT0 + i, Gl.GL_SPOT_EXPONENT, lights[i].SpotExponent);

                shader.SetUniformVariable("lightStruct.LightPositions[" + i + "]", lights[i].Position);
                shader.SetUniformVariable("lightStruct.ConstantAttenuation[" + i + "]", lights[i].ConstantAttenuation);
                shader.SetUniformVariable("lightStruct.LinearAttenuation[" + i + "]", lights[i].LinearAttenuation);
                shader.SetUniformVariable("lightStruct.QuadraticAttenuation[" + i + "]", lights[i].QuadraticAttenuation);

                shader.SetUniformVariable("lightStruct.SpotCosCutoff[" + i + "]", (float)Math.Cos(lights[i].SpotCutoff * Math.PI / 180)); //Der Öffnungswinkel muss zwischen 0 und 90 liegen. Wenn 180, dann ist Richtungslicht deaktiviert
                shader.SetUniformVariable("lightStruct.SpotDirection[" + i + "]", lights[i].SpotDirection);//Angabe in Weltkoordinaten
                shader.SetUniformVariable("lightStruct.SpotExponent[" + i + "]", lights[i].SpotExponent);
            }
            Gl.glLightModelfv(Gl.GL_LIGHT_MODEL_AMBIENT, new float[] { 0.1f, 0.1f, 0.1f, 1.0f }); //ambient RGBA intensity of the entire scene
            Gl.glLightModelf(Gl.GL_LIGHT_MODEL_LOCAL_VIEWER, 0.0f ); //how specular reflection angles are computed
            Gl.glLightModelf(Gl.GL_LIGHT_MODEL_TWO_SIDE,  0.0f ); //choose between one-sided or two-sided lighting
            while (i < Gl.GL_MAX_LIGHTS) Gl.glDisable(Gl.GL_LIGHT0 + i++);

            Gl.glPopMatrix();

            shader["LightCount"] = lights.Count;
            shader.SetUniformVariable("lightStruct.LightCount", lights.Count);
        }

        public void EnableLighting()
        {
            Gl.glEnable(Gl.GL_LIGHTING);
            shader["LightingIsEnabled"] = 1;
        }

        public void DisableLighting()
        {
            Gl.glDisable(Gl.GL_LIGHTING);
            shader["LightingIsEnabled"] = 0;
        }

        public void SetTexture(int textureID)
        {
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, textureID);
        }

        public void SetColor(float R, float G, float B, float A)
        {
            shader.SetUniformVariable("color", new Vector3D(R,G,B), A);
            Gl.glColor4f(R, G, B, A);
        }

        //Reihenfolge: Vertexshader->Pixelshader->glAlphaFunc(Verwerfe Pixel, wenn Alphawert > Schwellwert)->glBlendFunc(Blending = Pixelshaderfarbe * Faktor1 + Farbpufferfabe * Faktor2)->Farbpuffer
        public void SetBlendingWithBlackColor()
        {
            /*Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);			// Enable Alpha Blending (disable alpha testing)
            Gl.glAlphaFunc(Gl.GL_GREATER, 0.01f);								// Set Alpha Testing     (disable blending)
            Gl.glEnable(Gl.GL_ALPHA_TEST);									// Enable Alpha Testing  (disable blending)*/

            Gl.glDisable(Gl.GL_BLEND);
            Gl.glAlphaFunc(Gl.GL_GREATER, 0.01f);   //Verwerfe die Ausgabe aus dem Pixelshader(Kein Schreiben im Farbpuffer), wenn der Alphawert > 0.01 ist
            Gl.glEnable(Gl.GL_ALPHA_TEST);			//GL_ALPHA_TEST muss aktiviert sein, wenn man glAlphaFunc verwenden will
            shader["BlendingWithBlackColor"] = 1;
        }

        public void SetBlendingWithAlpha() 
        {
            //Alphablending = Sourcecolor * SourceAlpha + Destinationcolor * (1 - SourceAlpha)
            //ColorBlending = Sourcecolor + DestinationColor
            //Sourcesolor = Die Farbe, die aus dem Pixelshader kommt
            //Desinationcolor = Die Farbe, die Bereits im Farbpuffer steht

            //Gl.glEnable(Gl.GL_BLEND);
            //Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE);	// Blending Based On Source Alpha And 1 Minus Dest Alpha
            //Gl.glEnable(Gl.GL_COLOR_MATERIAL); //Farbfestlegung mit glColor möglich

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);//glBlendFunc(SourcecolorFactor, DestinationcolorFactor)
            Gl.glEnable(Gl.GL_COLOR_MATERIAL); //Farbfestlegung mit glColor möglich
            shader["BlendingWithBlackColor"] = 0;
        }

        public void DisableBlending()
        {
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_ALPHA_TEST);
            shader["BlendingWithBlackColor"] = 0;
        }

        public void EnableWireframe()
        {
            Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_LINE);
        }

        public void DisableWireframe()
        {
            Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
        }

        public void EnableExplosionEffect()
        {
            shader["ExplosionEffectIsEnabled"] = 1;
        }

        public void DisableExplosionEffect()
        {
            shader["ExplosionEffectIsEnabled"] = 0;
        }

        public float ExplosionsRadius
        {
            get
            {
                return shader["ExplosionsRadius"];
            }
            set
            {
                shader.SetUniformVariable("ExplosionsRadius", value);
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
            Gl.glEnable(Gl.GL_CULL_FACE);
            shader["CullFaceIsEnabled"] = 1;
        }

        public void DisableCullFace()
        {
            Gl.glDisable(Gl.GL_CULL_FACE);
            shader["CullFaceIsEnabled"] = 0;
        }

        public void SetFrontFaceConterClockWise()
        {
            Gl.glFrontFace(Gl.GL_CCW);
        }

        public void SetFrontFaceClockWise()
        {
            Gl.glFrontFace(Gl.GL_CW);
        }

        public void SetSpecularHighlightPowExponent(float specularHighlightPowExponent)
        {
            //Diese Werte hier werden eh nicht beachtet wenn man Shader verwendet. Das ist nur für den Fall, falls man ohne Shader arbeiten will/muss
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT, new float[] { 0.1f, 0.1f, 0.1f, 1 });
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_DIFFUSE, new float[] { 1, 1, 1, 1 });
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SPECULAR, new float[] { 1, 1, 1, 1 });	// set the reflection of the material 
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_EMISSION, new float[] { 0, 0, 0, 0 });
            
            Gl.glMaterialf(Gl.GL_FRONT_AND_BACK, Gl.GL_SHININESS, specularHighlightPowExponent);	// set the brightness of the material 
            shader.SetUniformVariable("lightStruct.SpecularHighlightPowExponent", specularHighlightPowExponent);
        }

        public void EnableDepthTesting()
        {
            Gl.glEnable(Gl.GL_DEPTH_TEST);
        }

        public void DisableDepthTesting()
        {
            Gl.glDisable(Gl.GL_DEPTH_TEST);
        }

        public System.Drawing.Bitmap GetStencilTestImage()
        {
            throw new NotImplementedException();
        }

        public void EnableStencilTest()
        {
            Gl.glEnable(Gl.GL_STENCIL_TEST);
        }

        public void DisableStencilTest()
        {
            Gl.glDisable(Gl.GL_STENCIL_TEST);
        }

        //Überall, wo im Farbpuffer was gezeichnet wird, wird im Stencilpuffer eine 1 geschrieben
        /*public void SetStencilFunc_SetBufferToOne()
        {
            //Parameter 1: StencilFunktion: GL_ALWAYS: Test wird immer bestanden
            //Parameter 2: Referenzwert: Mit diesen Wert wird nach dem Zeichnen verglichen (Zeichnen bedeutet Stencilpuffer wird auf 1 gesetzt). 
            //             Nur wenn Stencilpufferwert == Referenzwert, dann ist der Stenciltest bestanden
            //Parameter 3: Make: Bevor der Test durchgeführt wird: StencilWertNeu = Referenzwert & Make & StencilWert (Bitweises UND)
            //Gl.glStencilFunc(Gl.GL_ALWAYS, 1, 0xfffffff);            
        }*/

        public void SetStencilRead_NotEqualZero()
        {
            Gl.glStencilFunc(Gl.GL_NOTEQUAL, 0, 0xfffffff); //Es kann nur an der Stelle im Farbpuffer was gezeichnet werden, wo
                                                            //im Stencilpuffer ein Wert != 0 steht
            Gl.glStencilOp(Gl.GL_KEEP, Gl.GL_KEEP, Gl.GL_KEEP); //Stencilpuffer wird auf Readonly gesetzt
        }

        public bool SetStencilWrite_TwoSide()
        {
            return false;
            /*Gl.glEnable(Gl.GL_STENCIL_TEST_TWO_SIDE_EXT);
            Gl.glActiveStencilFaceEXT(Gl.GL_BACK);
            Gl.glStencilOp(Gl.GL_KEEP, Gl.GL_KEEP, Gl.GL_DECR_WRAP_EXT);
            Gl.glStencilMask(0);
            Gl.glStencilFunc(Gl.GL_ALWAYS, 0, 0);

            Gl.glActiveStencilFaceEXT(Gl.GL_FRONT);
            Gl.glStencilOp(Gl.GL_KEEP, Gl.GL_KEEP, Gl.GL_INCR_WRAP_EXT);
            Gl.glStencilMask(0);
            Gl.glStencilFunc(Gl.GL_ALWAYS, 0, 0);
            return true;*/
        }

        public void SetStencilWrite_Increase()
        {
            //Parameter 1: StencilFunktion: GL_ALWAYS: Test wird immer bestanden
            //Parameter 2: Referenzwert: Mit diesen Wert wird nach dem Zeichnen verglichen (Zeichnen bedeutet Stencilpuffer wird auf 1 gesetzt). 
            //             Nur wenn Stencilpufferwert == Referenzwert, dann ist der Stenciltest bestanden
            //Parameter 3: Make: Bevor der Test durchgeführt wird: StencilWertNeu = Referenzwert & Make & StencilWert (Bitweises UND)
            Gl.glStencilFunc(Gl.GL_ALWAYS, 1, 0xfffffff); 

            //procedure glStencilOp (fail,zfail,zpass: TGLenum);
            //Parameter 1: fail	 Legt fest, was gemacht wird, wenn der Stencil-Test fehlschlägt.
            //Parameter 2: zfail Legt fest, was passiert, wenn der Stencil-Test erfolgreich ist aber der Tiefentest fehlschlägt
            //Parameter 3: zpass Legt fest, was passiert, wenn sowohl der Stenciltest als auch Tiefentest erfolgreich ist. Ist der Tiefentest deaktiviert, so gilt dieser als bestanden
            Gl.glStencilOp(Gl.GL_KEEP, Gl.GL_KEEP, Gl.GL_INCR); //Nach erfolgreichen Stenciltest wird Stencilwert um 1 erhöht
        }

        public void SetStencilWrite_Decrease()
        {
            Gl.glStencilOp(Gl.GL_KEEP, Gl.GL_KEEP, Gl.GL_DECR); //Nach erfolgreichen Stenciltest wird Stencilwert um 1 verringert
        }

        /*public void SetStencilOp_Keep()
        {
            Gl.glStencilOp(Gl.GL_KEEP, Gl.GL_KEEP, Gl.GL_KEEP); //Stencilpuffer wird auf Readonly gesetzt
        }*/

        private Vector2D mouseHitTestPosition = null;
        public void StartMouseHitTest(Point mousePosition)
        {
            this.mouseHitTestPosition = new Vector2D(mousePosition.X, mousePosition.Y);
            shader.Mode = ShaderHelper.ShaderMode.MouseHitTest;
            shader.LockShaderModeWriting = true;
            return;

            //mouseY = simpleOpenGlControl.Height - mouseY;

            //selectBufferForMouseHitTest = new int[objektCount + 10];				//buffer for selected object names (1..Gelenk 1,...                                                  
            //int[] viewport = new int[4];									    //tmpvar for viewport 
            //float[] projektionsMatrix = new float[16];
            //Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);						/* get current viewport, save in "viewport" */
            //Gl.glSelectBuffer(selectBufferForMouseHitTest.Length, selectBufferForMouseHitTest);

            //Gl.glMatrixMode(Gl.GL_PROJECTION);								/* switch to projection mode */
            //Gl.glPushMatrix();
            //Gl.glGetFloatv(Gl.GL_PROJECTION_MATRIX, projektionsMatrix);
            //Gl.glLoadIdentity();

            //Gl.glRenderMode(Gl.GL_SELECT);								/* change render mode for selection */
            //Glu.gluPickMatrix(mouseX, mouseY, 2, 2, viewport);	            // Dieser Befehl verändert die Projektionsmatrix so, das nur noch unmittelbar um den Mauszeiger gezeichnet wird 
            //Gl.glMultMatrixf(projektionsMatrix);
            //Gl.glMatrixMode(Gl.GL_MODELVIEW);

            //Gl.glInitNames();										/* init name stack */
            //Gl.glPushName(0);										/* push one element on stack to avoid GL_INVALID_OPERATION error @ next pop from stack */
        }

        public void AddObjektIdForMouseHitTest(int objektId)
        {
            Gl.glLoadName(objektId);
            shader["MouseHitId"] = objektId;
        }

        public int GetMouseHitTestResult()
        {
            shader.LockShaderModeWriting = false;
            shader.Mode = ShaderHelper.ShaderMode.Normal;            
            return (int)GetPixelColorFromColorBuffer((int)this.mouseHitTestPosition.X, (int)this.mouseHitTestPosition.Y).R;


            //int hits;                                   //number of hits with mouse
            //Gl.glPopName();

            //Gl.glMatrixMode(Gl.GL_PROJECTION);								/* switch to projection mode */
            //Gl.glPopMatrix();
            //Gl.glMatrixMode(Gl.GL_MODELVIEW);									/* set back to modelview mode */

            //if ((hits = Gl.glRenderMode(Gl.GL_RENDER)) != 0)    
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
                Gl.glEnable(Gl.GL_ALPHA_TEST);
                Gl.glAlphaFunc(Gl.GL_GREATER, 0.01f);
                Gl.glDisable(Gl.GL_BLEND);               
            }
        }
        public float ZValue2D { get; set; } = 0;

        //erzeugt ein neuen Schriftsatz vom Typ "fondType" und speichert ihn in Form einer OpenGL-Displayliste. 
        //Es wird die Startnummer des ersten Zeichens der Displayliste zurück gegeben
        private int CreateFont(string fondType)
        {
            int Fontbase = 0;
            //Graphics graphics = Graphics.FromHwnd(simpleOpenGLControl.Handle);
            Bitmap img = new Bitmap(100, 100);
            img.SetResolution(96, 96);
            Graphics graphics = Graphics.FromImage(img);
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
            graphics.TextContrast = 4;
            Font font = new Font(fondType, 10); // now we can call OpenGL API
            IntPtr hdc = graphics.GetHdc();
            Gdi.GLYPHMETRICSFLOAT[] agmf = new Gdi.GLYPHMETRICSFLOAT[256];
            Gdi.SelectObject(hdc, font.ToHfont());// make the system font the device context's selected font
            Fontbase = Gl.glGenLists(256);								// Storage For 256 Characters
            Wgl.wglUseFontOutlines(hdc, 0, 255, Fontbase, 0.0f/*So genau wie möglich*/, 10.1f/*Z-Tiefe*/, Wgl.WGL_FONT_POLYGONS/*Polygons... Ausgefüllte Schrift; LINES...Nicht ausgefüllt*/, agmf);
            return Fontbase;
        }

        private void KillFont()
        {
            Gl.glDeleteLists(fontbase, 256);								// Delete All 256 Characters
        }

        public void Dispose()
        {
            KillFont();
        }

        public void DrawLine(Pen pen, Vector2D p1, Vector2D p2)
        {
            //Gl.glDisable(Gl.GL_TEXTURE_2D);
            DisableTexturemapping();
            Gl.glLineWidth(pen.Width);
            if (pen.DashStyle == System.Drawing.Drawing2D.DashStyle.Dot)
                Gl.glEnable(Gl.GL_LINE_STIPPLE);//Linien dürfen gepunktet sein
            else
                Gl.glDisable(Gl.GL_LINE_STIPPLE);
            Gl.glColor3f(pen.Color.R / 255.0f, pen.Color.G / 255.0f, pen.Color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex3f(p1.X + 0.5f, p1.Y, this.ZValue2D);  //OpenGL verschiebt Linien immer um 0.5f Pixel nach links. Damit gleiche ich das aus
            Gl.glVertex3f(p2.X + 0.5f, p2.Y, this.ZValue2D);
            Gl.glEnd();
        }

        public void DrawPixel(Vector2D pos, Color color, float size)
        {
            Gl.glPointSize(size);
            //Gl.glDisable(Gl.GL_TEXTURE_2D);
            DisableTexturemapping();
            Gl.glBegin(Gl.GL_POINTS);
            Gl.glColor3f(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            Gl.glVertex3f(pos.X, pos.Y, this.ZValue2D);
            Gl.glEnd(); 
        }

        private SizeF singleLetterSize = new SizeF(0, 0);
        public Size GetStringSize(float size, string text)
        {
            //Mit diesem Algorithmus hier unten kann man die größe Ausmessen, um erstmal zu sehen, wie groß ein einzelner Buchstabe von einer Schriftart überhaupt ist.
            if (singleLetterSize.Width == 0)
            {
                IntPtr pixels = Marshal.AllocHGlobal(this.Width * this.Height * 4);
                Gl.glReadPixels(0, 0, this.Width, this.Height, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, pixels);//Bilddaten von OpenGL anfordern 

                Gl.glClearColor(1, 1, 1, 0);
                Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
                DrawString(0, Width / 2, Color.Black, size, "WWww");

                Bitmap screen = GetDataFromColorBuffer();
                Rectangle reci = BitmapHelp.SearchRectangleInBitmap(screen, Color.White);

                Gl.glDrawPixels(this.Width, this.Height, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, pixels);
                Marshal.FreeHGlobal(pixels);

                singleLetterSize = new SizeF(reci.Width / 4.0f / size, reci.Height / size);//4.. Länge des Textes "WWww"
            }

            return new Size((int)(singleLetterSize.Width * text.Length * size), (int)(singleLetterSize.Height * size * 1.3f)); 
        }

        public void DrawString(float x, float y, Color color, float size, string text)
        {
            Gl.glDisable(Gl.GL_TEXTURE_2D);
            Gl.glMatrixMode(Gl.GL_PROJECTION);	/* specifies the current matrix */
            Gl.glPushMatrix();
            Gl.glLoadIdentity();			/* Sets the currant matrix to identity */
            Glu.gluOrtho2D(0, simpleOpenGlControl.Width, 0, simpleOpenGlControl.Height);	/* Sets the clipping rectangle extends */

            
            Gl.glColor3f(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();
            Gl.glTranslatef(x - size * 0.05f, simpleOpenGlControl.Height - y - size * 0.82f, this.ZValue2D);//Damit (x,y) genau links Oben liegt
            size *= 1.5f;//Damit der OpenGL-Text so groß wie die GDI+ Schrift ist
            Gl.glScalef(size, size, size);
            Gl.glListBase(fontbase);
            Gl.glCallLists(text.Length, Gl.GL_UNSIGNED_SHORT, text);// now draw the characters in a string
            Gl.glFrontFace(Gl.GL_CCW); //glCallLists setzt die FrontFace auf ClockWise. Deswegen setze ich es hier zurück auf sein Default-Wert
            Gl.glPopMatrix();

            Gl.glMatrixMode(Gl.GL_PROJECTION);	/* specifies the current matrix */
            Gl.glPopMatrix();
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
        }

        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            DisableTexturemapping();
            Gl.glDisable(Gl.GL_TEXTURE_2D);
            Gl.glLineWidth(pen.Width);
            if (pen.DashStyle == System.Drawing.Drawing2D.DashStyle.Dot)
                Gl.glEnable(Gl.GL_LINE_STIPPLE);//Linien dürfen gepunktet sein
            else
                Gl.glDisable(Gl.GL_LINE_STIPPLE);
            Gl.glColor3f(pen.Color.R / 255.0f, pen.Color.G / 255.0f, pen.Color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            Gl.glBegin(Gl.GL_LINE_STRIP);
            Gl.glVertex3f(x, y, this.ZValue2D);
            Gl.glVertex3f(x + width, y, this.ZValue2D);
            Gl.glVertex3f(x + width, y + height, this.ZValue2D);
            Gl.glVertex3f(x, y + height, this.ZValue2D);
            Gl.glVertex3f(x, y, this.ZValue2D);
            Gl.glEnd();
        }

        public void DrawPolygon(Pen pen, List<Vector2D> points)
        {
            DisableTexturemapping();
            Gl.glDisable(Gl.GL_TEXTURE_2D);
            Gl.glLineWidth(pen.Width);
            if (pen.DashStyle == System.Drawing.Drawing2D.DashStyle.Dot)
                Gl.glEnable(Gl.GL_LINE_STIPPLE);//Linien dürfen gepunktet sein
            else
                Gl.glDisable(Gl.GL_LINE_STIPPLE);
            Gl.glColor3f(pen.Color.R / 255.0f, pen.Color.G / 255.0f, pen.Color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            Gl.glBegin(Gl.GL_LINE_STRIP);
            foreach (Vector2D V in points)
            {
                Gl.glVertex3f(V.X, V.Y, this.ZValue2D);
            }
            Gl.glVertex3f(points[0].X, points[0].Y, this.ZValue2D);
            Gl.glEnd();
        }

        public void DrawCircle(Pen pen, Vector2D pos, int radius)
        {
            DisableTexturemapping();
            Gl.glDisable(Gl.GL_TEXTURE_2D);
            Gl.glPointSize(pen.Width);
            Gl.glColor3f(pen.Color.R / 255.0f, pen.Color.G / 255.0f, pen.Color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            Gl.glBegin(Gl.GL_POINTS);

            ShapeDrawer.DrawCircle(pos, radius, (p) => Gl.glVertex3f(p.X, p.Y, this.ZValue2D));

            Gl.glEnd();
        }

        public void DrawFillCircle(Color color, Vector2D pos, int radius)
        {
            DisableTexturemapping();
            Gl.glDisable(Gl.GL_TEXTURE_2D);
            Gl.glPointSize(1);

            Gl.glColor3f(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            Gl.glBegin(Gl.GL_POINTS);

            ShapeDrawer.DrawFillCircle(pos, radius, (p) => Gl.glVertex3f(p.X, p.Y, this.ZValue2D));

            Gl.glEnd();
        }

        public void DrawCircleArc(Pen pen, Vector2D pos, int radius, float startAngle, float endAngle, bool withBorderLines)
        {
            CircleArcDrawer.DrawCircleArc(pos, radius, startAngle, endAngle, withBorderLines, (p) => DrawPixel(p, pen.Color, pen.Width));
        }
        public void DrawFillCircleArc(Color color, Vector2D pos, int radius, float startAngle, float endAngle)
        {
            DisableTexturemapping();
            Gl.glDisable(Gl.GL_TEXTURE_2D);
            Gl.glPointSize(1);
            Gl.glColor3f(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            Gl.glBegin(Gl.GL_POINTS);

            CircleArcDrawer.DrawFillCircleArc(pos, radius, startAngle, endAngle, (p) => Gl.glVertex3f(p.X, p.Y, this.ZValue2D));

            Gl.glEnd();
        }

        public void DrawImage(int textureId, int x, int y, int width, int height, int sourceX, int sourceY, int sourceWidth, int sourceHeight, Color colorFactor)
        {
            Size tex = GetTextureSize(textureId);
            float f = 0;// 0.01f;
            UseAlphaBlendingAndDiscardTransparent(colorFactor); // SetBlendingWithAlpha();
            EnableTexturemapping();
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            SetTextureFilter(TextureFilter.Point);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, textureId);
            Gl.glBegin(Gl.GL_QUADS);
            Gl.glTexCoord2f(sourceX / (float)tex.Width + f, sourceY / (float)tex.Height + f); Gl.glVertex3f(x, y, this.ZValue2D);
            Gl.glTexCoord2f((sourceX + sourceWidth) / (float)tex.Width - f, sourceY / (float)tex.Height + f); Gl.glVertex3f(x + width, y, this.ZValue2D);
            Gl.glTexCoord2f((sourceX + sourceWidth) / (float)tex.Width - f, (sourceY + sourceHeight) / (float)tex.Height - f); Gl.glVertex3f(x + width, y + height, this.ZValue2D);
            Gl.glTexCoord2f(sourceX / (float)tex.Width + f, (sourceY + sourceHeight) / (float)tex.Height - f); Gl.glVertex3f(x, y + height, this.ZValue2D);
            Gl.glEnd();
            DisableBlending();
        }

        public void DrawImage(int textureId, int x, int y, int width, int height, int sourceX, int sourceY, int sourceWidth, int sourceHeight, Color colorFactor, float zAngle, float yAngle)
        {
            Gl.glTranslatef(x, y, 0);
            Gl.glRotatef(zAngle, 0, 0, 1);
            Gl.glRotatef(yAngle, 0, 1, 0);
            DrawImage(textureId, -width / 2, -height / 2, width, height, sourceX, sourceY, sourceWidth, sourceHeight, colorFactor);
            Gl.glRotatef(-yAngle, 0, 1, 0);
            Gl.glRotatef(-zAngle, 0, 0, 1);
            Gl.glTranslatef(-x, -y, 0);
            DisableBlending();
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor)
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor); // SetBlendingWithAlpha();
            EnableTexturemapping();
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            SetTextureFilter(TextureFilter.Point);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, textureId);
            Gl.glBegin(Gl.GL_QUADS);
            Gl.glTexCoord2f(0.01f, 0.01f); Gl.glVertex3f(x, y, this.ZValue2D);
            Gl.glTexCoord2f(0.99f, 0.01f); Gl.glVertex3f(x + width, y, this.ZValue2D);
            Gl.glTexCoord2f(0.99f, 0.99f); Gl.glVertex3f(x + width, y + height, this.ZValue2D);
            Gl.glTexCoord2f(0.01f, 0.99f); Gl.glVertex3f(x, y + height, this.ZValue2D);
            Gl.glEnd();
            DisableBlending();
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor, float angle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            //SetBlendingWithBlackColor();
            Gl.glTranslatef(x, y, 0);
            Gl.glRotatef(angle, 0, 0, 1);
            DrawFillRectangle(textureId, -width / 2, -height / 2, width, height, colorFactor);
            Gl.glRotatef(-angle, 0, 0, 1);
            Gl.glTranslatef(-x, -y, 0);
            DisableBlending();
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor, float zAngle, float yAngle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            //SetBlendingWithBlackColor();
            Gl.glTranslatef(x, y, 0);
            Gl.glRotatef(zAngle, 0, 0, 1);
            Gl.glRotatef(yAngle, 0, 1, 0);
            DrawFillRectangle(textureId, -width / 2, -height / 2, width, height, colorFactor);
            Gl.glRotatef(-yAngle, 0, 1, 0);
            Gl.glRotatef(-zAngle, 0, 0, 1);
            Gl.glTranslatef(-x, -y, 0);
            DisableBlending();
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height)
        {
            DisableTexturemapping();
            Gl.glDisable(Gl.GL_TEXTURE_2D);
            Gl.glColor3f(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.
            Gl.glBegin(Gl.GL_QUADS);
            Gl.glTexCoord2f(0.01f, 0.01f); Gl.glVertex3f(x, y, this.ZValue2D);
            Gl.glTexCoord2f(0.99f, 0.01f); Gl.glVertex3f(x + width, y, this.ZValue2D);
            Gl.glTexCoord2f(0.99f, 0.99f); Gl.glVertex3f(x + width, y + height, this.ZValue2D);
            Gl.glTexCoord2f(0.01f, 0.99f); Gl.glVertex3f(x, y + height, this.ZValue2D);
            Gl.glEnd();
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float angle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            Gl.glTranslatef(x, y, 0);
            Gl.glRotatef(angle, 0, 0, 1);
            DrawFillRectangle(color, -width / 2, -height / 2, width, height);
            Gl.glRotatef(-angle, 0, 0, 1);
            Gl.glTranslatef(-x, -y, 0);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float zAngle, float yAngle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            Gl.glTranslatef(x, y, 0);
            Gl.glRotatef(zAngle, 0, 0, 1);
            Gl.glRotatef(yAngle, 0, 1, 0);
            DrawFillRectangle(color, -width / 2, -height / 2, width, height);
            Gl.glRotatef(-yAngle, 0, 1, 0);
            Gl.glRotatef(-zAngle, 0, 0, 1);
            Gl.glTranslatef(-x, -y, 0);
        }

        public void DrawFillPolygon(int textureId, Color colorFactor, List<Triangle2D> triangleList)
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor); // SetBlendingWithAlpha();
            EnableTexturemapping();
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            SetTextureFilter(TextureFilter.Point);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, textureId);

            Gl.glBegin(Gl.GL_TRIANGLES);
            foreach (Triangle2D triangle in triangleList)
            {
                Gl.glTexCoord2f(triangle.P1.Textcoord.X, triangle.P1.Textcoord.Y);
                Gl.glVertex3f(triangle.P1.Position.X, triangle.P1.Position.Y, this.ZValue2D);

                Gl.glTexCoord2f(triangle.P2.Textcoord.X, triangle.P2.Textcoord.Y);
                Gl.glVertex3f(triangle.P2.Position.X, triangle.P2.Position.Y, this.ZValue2D);

                Gl.glTexCoord2f(triangle.P3.Textcoord.X, triangle.P3.Textcoord.Y);
                Gl.glVertex3f(triangle.P3.Position.X, triangle.P3.Position.Y, this.ZValue2D);
            }
            Gl.glEnd();
        }

        public void DrawFillPolygon(Color color, List<Triangle2D> triangleList)
        {
            DisableTexturemapping();
            Gl.glDisable(Gl.GL_TEXTURE_2D);
            Gl.glColor3f(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);//Der Aufruf von glColor3b klappt nicht. Ich weiß nicht warum.

            Gl.glBegin(Gl.GL_TRIANGLES);
            foreach (Triangle2D triangle in triangleList)
            {
                Gl.glTexCoord2f(triangle.P1.Textcoord.X, triangle.P1.Textcoord.Y);
                Gl.glVertex3f(triangle.P1.Position.X, triangle.P1.Position.Y, this.ZValue2D);

                Gl.glTexCoord2f(triangle.P2.Textcoord.X, triangle.P2.Textcoord.Y);
                Gl.glVertex3f(triangle.P2.Position.X, triangle.P2.Position.Y, this.ZValue2D);

                Gl.glTexCoord2f(triangle.P3.Textcoord.X, triangle.P3.Textcoord.Y);
                Gl.glVertex3f(triangle.P3.Position.X, triangle.P3.Position.Y, this.ZValue2D);
            }
            Gl.glEnd();
        }

        public void DrawSprite(int textureId, int xCount, int yCount, int xBild, int yBild, int x, int y, int width, int height, Color colorFactor)
        {
            float xf = 1.0f / xCount, yf = 1.0f / yCount;
            UseAlphaBlendingAndDiscardTransparent(colorFactor); // SetBlendingWithAlpha();
            EnableTexturemapping();
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            SetTextureFilter(TextureFilter.Point);
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, textureId);
            Gl.glBegin(Gl.GL_QUADS);
            Gl.glTexCoord2f(xBild * xf + 0.01f, yBild * yf + 0.01f); Gl.glVertex3f(x, y, this.ZValue2D);
            Gl.glTexCoord2f((xBild + 1) * xf - 0.01f, yBild * yf + 0.01f); Gl.glVertex3f(x + width, y, this.ZValue2D);
            Gl.glTexCoord2f((xBild + 1) * xf - 0.01f, (yBild + 1) * yf - 0.01f); Gl.glVertex3f(x + width, y + height, this.ZValue2D);
            Gl.glTexCoord2f(xBild * xf + 0.01f, (yBild + 1) * yf - 0.01f); Gl.glVertex3f(x, y + height, this.ZValue2D);
            Gl.glEnd();
            DisableBlending();
        }

        public void EnableScissorTesting(int x, int y, int width, int height)
        {
            Gl.glScissor(x, Height - y - height, width, height);	// Define Scissor Region
            Gl.glEnable(Gl.GL_SCISSOR_TEST);								// Enable Scissor Testing
        }

        public void DisableScissorTesting()
        {
            Gl.glDisable(Gl.GL_SCISSOR_TEST);								// Disable Scissor Testing
        }

        #endregion
        #endregion
    }
}
