//http://www.rastertek.com/tutdx11.html                                                 -> Ähnlich wie NeHe für OpenGL nur leider in C++
//http://www.richardssoftware.net/2013/08/planar-reflections-and-shadows-using.html     -> SlimDX DirectX 11 Tutorials

//Gelerntes Wissen:
//Beim m_context.Rasterizer wird der Vertex + Pixelshader gesteuert. D.h., hier kann man den ViewPort, Cullface, WireFrame einstellen
//Beim m_context.OutputMerger werden Farbpufferoperationen eingestellt. Also Blending, WritingToColorBuffer, Stencilpuffer-Einstellungen
//Bei OpenGL ist Z=-10 nah vor ein und Z=-100 weit weg. Bei Direct3D ist Z=10 nah vor ein und Z=100 weit weg
//Das Bild ist bei Direct3D Y-Mäßig gespiegelt, weil die Vorzeichen von SlimDX.Kameramatrix und SlimDX.ProjectionsMatrix verdreht sind
//Bei OpenGL schreibt man: Clipping-Vektor = MV-Ident * MV-Kamera * MV-Objekt * Projektion * Objekt-Vektor
//Bei Direct3D: Clipping-Vektor = MV-Objekt * MV-Kamera * MV-Ident * Projektion * Objekt-Vektor             (Es köntne sein, das meine Matrix.MultMatrix-Funktion matrizen falsch herrum multipliziert)
//Eine Texture2D enthält Daten, wo einfach nur festgelegt wird, wie viele Bits ein Texel groß ist. Eine ShaderResourceView/DepthStencilView schaut dann auf diesen Datenblock in der GPU und sagt, wie die Texel-Bits interpretiert werden sollen
//Die Texture2D kann gelöscht werden, nachdem die View drauf zeigt, da die Daten dann bereits vom CPU-Speicher in den GPU-Speicher kopiert wurden. 

//So rendert man in mehrere Texturen/Buffer gleichzeitig:
//m_context.OutputMerger.SetTargets(m_renderTargetDepth, m_renderTarget0, m_renderTarget1)
//struct PS_OUTPUT
//{
//    float4 Color: SV_Target0;
//    float4 Normal: SV_Target1;
//};

//Damit DirectX und OpenGL3-0 läuft, muss man folgendes Setup einmalig ausführen: D:\C#\SlimDX-Beispiele\SlimDX SDK (January 2012)\DirectX Redist\DXSETUP.exe
//Mit dxdiag.exe kann geprüft werden ob und welches DirectX installiert ist

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SlimDX;
using DX11 = SlimDX.Direct3D11;
using DXGI = SlimDX.DXGI;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using SlimDX.D3DCompiler;
using System.Runtime.InteropServices;
using GraphicMinimal;
using SlimDX.Direct3D11;
using SlimDX.Direct2D;
using SlimDX.DirectWrite;
using SlimDX.DXGI;
using System.Drawing.Imaging;
using BitmapHelper;
using GraphicGlobal;
using GraphicGlobal.Rasterizer2DFunctions;

namespace GraphicPipelineDirect3D11
{
    public class GraphicPipelineDirect3D11 : IGraphicPipeline
    {
        class Texture
        {
            public int Width, Height;
            public DX11.ShaderResourceView TextureView;
            public string File = "";
            public int ID;
            public bool IsDepthTexture = false;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VertexDX
        {
            public Vector3 Position;            // = ObjektPos * ModelViewMatrix * ProjectionMatrix 
            public int Color; //RGBA
            public Vector2 TexCoord;
            public Vector3 Normal; //Normalenvektor
            public Vector3 Tangent; //Tangentialvektor (zeigt im Texturkoordinatenraum nach oben)

            public VertexDX(Vector3 position, int Color, Vector2 texCoord, Vector3 Normal, Vector3 Tangent)
            {
                this.Position = position;
                this.Color = Color;
                this.TexCoord = texCoord;
                this.Normal = Normal;
                this.Tangent = Tangent;
            }
        }

        class VertexArray
        {
            public DX11.Buffer vertexBuffer;
            public DX11.Buffer indexBuffer;
            public int indexCount;
        }

        #region Private Variablen

        private Control drawingArea;

        // DirectX-Variablen, die man in jedem Fall braucht
        private DX11.Device m_device;                       // Hauptvariable für DX11 (Vermutlich Verbindugnsstück zum Graka-Treiber)
        private DX11.DeviceContext m_context;               // Hat verweis auf Puffer. Nimmt Zeichenbefehle entgegen und zeichnet in die Puffer.
        private DXGI.SwapChain m_swapChain;                 // Farb+Tiefenpuffer zusammen. Hiermit wird auch Flipbuffer gemacht.
        private DX11.RenderTargetView m_renderTarget;       // Farbpuffer
        private DX11.DepthStencilView m_renderTargetDepth;  // Tiefenpuffer
        private DX11.RenderTargetView m_renderTargetActive; // Zeigt entweder auf m_renderTarget, Framebuffer oder Shadowmap
        private DX11.DepthStencilView m_renderTargetDepthActive;  // Zeigt entweder auf m_renderTarget, Framebuffer oder Shadowmap
        private DX11.BlendState m_blendState = null;        // Diese Variable wird dem Outputmerger zugewiesen. Hiermit kann das Blending eingestellt werden

        // Effektvariablen(Shaderinputvariablen, die für alle Vertixe gleich sind)
        private DX11.Effect m_effect;                       // Zum kopieren der Constant-Buffer-Variablen
        private DX11.InputLayout m_vertexLayout;            // Gibt an, wie ein einzelner Vertex aufgebaut ist(muss der Input-Assembler-Stage gesagt werden)
        private DX11.InputLayout vertexLayout2D = null;     // Beschreibt das Aussehen der Vertexshader-Eingangsdaten
        private DX11.EffectPass m_effectPass3DStandard;     // Damit werden die Effectvariablen(Textur, Farbe des Objektes, Modelmatrix des Objektes) vom Haupspeicher in den Grafikspeicher übertragen
        private DX11.EffectPass m_effectPass3DParallax;
        private DX11.EffectPass m_effectPass3DCreateShadowmap;
        private DX11.EffectPass m_effectPass3DMouseHitTest;
        private DX11.EffectPass m_effectPassLine2D;           // Damit werden die Effektfariablen in die GraKa kopiert und die Rendertechnik (Shaderprogramme) ausgewählt
        private DX11.EffectPass m_effectPassLine3D;
        private DX11.EffectPass m_effectPassPoint;
        private DX11.EffectResourceVariable m_TexturID0;    // Speichert die aktuell gesetzte Textur-ID. Wird mit m_effectPass vom HS in GraKa kopiert (Zum kopieren in den Shader)
        private DX11.EffectResourceVariable m_TexturID1;
        private DX11.EffectResourceVariable m_CubeMapTexture;
        private DX11.EffectResourceVariable m_CubeMapArrayTexture;
        private DX11.EffectResourceVariable m_ShadowTexture;
        private DX11.EffectVectorVariable m_colorVariable;  // Aktuell gesetzte RGB-Farbe (setColorf)
        private SamplerState samplerStateTexture0;          // Hiermit wird für Textur0 (Farbtextur) Clamp/Repeat und Point/Linear eingestellt

        // Eigene Variablen, die man auch so in OpenGL findet
        private Dictionary<int, VertexArray> triangleArrays = new Dictionary<int, VertexArray>(); // TriangleArray-ID | Daten
        private List<Texture> textures = new List<Texture>();
        private CubemapControlData cubemaps = new CubemapControlData(); 
        private Dictionary<int, MyFramebuffer> framebuffers = new Dictionary<int, MyFramebuffer>(); //[FramebufferId | 2D-Texture]
        private Stack<SlimDX.Matrix> modelviewMatrixStack = new Stack<SlimDX.Matrix>();
        private Stack<SlimDX.Matrix> projectionMatrixStack = new Stack<SlimDX.Matrix>();
        private SlimDX.Matrix m_modelviewMatrix;            // Aktuelle ModelViewmatrix
        private SlimDX.Matrix m_projMatrix;                 // Aktuelle Projektionsmatrix
        private int activeTextureDeck = 0;
        private Dictionary<string, int> texte = new Dictionary<string, int>(); // Hier werden die Textur-IDs für die Texte gespeichert
        private int time;
        private bool createShadowmap = false;
        private bool useMouseHitTestShader = false;
        #endregion

        #region IDrawingLow Member

        public int Width
        {
            get
            {
                return this.drawingArea.Width;
            }
        }
        public int Height
        {
            get
            {
                return this.drawingArea.Height;
            }
        }

        public GraphicPipelineDirect3D11()
        {
            this.drawingArea = new PanelWithoutFlickers() { Dock = DockStyle.Fill };

            //this.drawingArea.SetStyle(ControlStyles.ResizeRedraw, true);
            //this.drawingArea.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            //this.drawingArea.SetStyle(ControlStyles.Opaque, true);

            this.drawingArea.Disposed += (o, s) =>
            {
                this.Dispose();
            };

            this.drawingArea.Resize += (sender, e) =>
            {
                this.Resize(this.drawingArea.ClientSize.Width, this.drawingArea.ClientSize.Height);
            };

            string shaderErrors = "";
            try
            {
                //Teil 1: Farb- und Tiefenpuffer initialisieren
                DX11.Device.CreateWithSwapChain(DX11.DriverType.Warp, DX11.DeviceCreationFlags.None, new DXGI.SwapChainDescription()
                {
                    BufferCount = 2, //Anzahl Puffer
                    Usage = DXGI.Usage.RenderTargetOutput,//Puffer soll auf Bildschirm ausgegeben werden
                    OutputHandle = this.drawingArea.Handle,//Auf dieses Fenster wird gezeichnet
                    IsWindowed = true, //Fenstermodus / Vollbildmodus
                    ModeDescription = new DXGI.ModeDescription(0, 0, //Puffergröße entspricht Fenstergröße
                                      new Rational(60, 1), DXGI.Format.R8G8B8A8_UNorm), //Refreshrate
                    SampleDescription = new DXGI.SampleDescription(1, 0),//Multisamplingparameter
                    Flags = DXGI.SwapChainFlags.AllowModeSwitch,//Anzeigemodus darf umgeschalten werden
                    SwapEffect = DXGI.SwapEffect.Discard//Alte Puffer werden verworfen
                }, out m_device, out m_swapChain);

                using (var resource = DX11.Resource.FromSwapChain<DX11.Texture2D>(m_swapChain, 0))           //Farbpuffer anlegen
                    m_renderTarget = new DX11.RenderTargetView(m_device, resource);

                m_renderTargetDepth = new DX11.DepthStencilView(m_device, new DX11.Texture2D(m_device,       //Tiefenpuffer anlegen
                    new DX11.Texture2DDescription()
                    {
                        Width = this.drawingArea.ClientSize.Width,
                        Height = this.drawingArea.ClientSize.Height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format =  DXGI.Format.D24_UNorm_S8_UInt,
                        Usage = DX11.ResourceUsage.Default,     //Default = GPU RW, Immutable = GPU RO, DYNAMIC (CPU WO, GPU RO), STAGING (CPU RW, GPU RW)
                        SampleDescription = new DXGI.SampleDescription(1, 0),
                        BindFlags = DX11.BindFlags.DepthStencil,
                        CpuAccessFlags = DX11.CpuAccessFlags.None,
                        OptionFlags = DX11.ResourceOptionFlags.None
                    }));

                m_context = m_device.ImmediateContext;
                m_context.OutputMerger.SetTargets(m_renderTargetDepth, m_renderTarget);//Farb und Tiefen/Stencil - Puffer dem OutputMerger zuweisen
                m_context.Rasterizer.SetViewports(new DX11.Viewport(0.0f, 0.0f, this.drawingArea.ClientSize.Width, this.drawingArea.ClientSize.Height, 0f, 1f)); //Viewport dem Rasterizer-Stage zuweisen

                m_context.Rasterizer.State = RasterizerStateHelper.InitialValue(m_device);
                m_context.OutputMerger.DepthStencilState = DepthStencilStateHelper.InitialValue(m_device);

                m_blendState = BlendStateHelper.InitialValue(m_device);
                m_context.OutputMerger.BlendState = m_blendState;

                SetProjectionMatrix3D();

                m_renderTargetActive = m_renderTarget;
                m_renderTargetDepthActive = m_renderTargetDepth;

                //Teil 2: Shader initialisieren

                //Effektvariablen: Zum kopieren der Matrizen und Texture-IDs von HS in Graka
                //if (File.Exists("SimpleRendering.fx") == false) throw new FileNotFoundException(Directory.GetCurrentDirectory() + "\\SimpleRendering.fx");
                //var bytecode = ShaderBytecode.CompileFromFile(@"..\..\..\GraphicPipelineDirect3D11\Resources\SimpleRendering.fx.txt", "fx_5_0", ShaderFlags.None, SlimDX.D3DCompiler.EffectFlags.None, null, null, out shaderErrors);
                var shaderCode = Resources.Variablen + Resources.SimpleRendering + Resources.DisplacementMapping + Resources.ParallaxMapping + Resources.ShadowmapCreation + /*Resources.MouseHitTest +*/ Resources.LinesAndPoints;
                var bytecode = ShaderBytecode.Compile(shaderCode, "fx_5_0", ShaderFlags.None, SlimDX.D3DCompiler.EffectFlags.None, null, null, out shaderErrors); //http://slimdx.googlecode.com/svn-history/r1728/branches/v2/Source/SlimDX.D3DCompiler/ShaderBytecode.cs -> Suche nach shaderSource

                m_effect = new DX11.Effect(m_device, bytecode);                     // Wird benötigt, um aus den Shaderprogramm die verwendeten Input-Variablen(Textur, Modelviewmatrix) zu extrahieren
                m_effectPass3DStandard = m_effect.GetTechniqueByName("DrawTriangleNormalAndDisplacement").GetPassByIndex(0);
                m_effectPass3DParallax = m_effect.GetTechniqueByName("DrawTriangleParallax").GetPassByIndex(0);
                m_effectPass3DCreateShadowmap = m_effect.GetTechniqueByName("CreateShadowmap").GetPassByIndex(0);
                m_effectPass3DMouseHitTest = m_effect.GetTechniqueByName("MouseHitTest").GetPassByIndex(0);

                m_TexturID0 = m_effect.GetVariableByName("Texture0").AsResource();
                m_TexturID1 = m_effect.GetVariableByName("Texture1").AsResource();//Bumpmaptextur
                m_CubeMapTexture = m_effect.GetVariableByName("CubeMapTexture").AsResource(); //Reflektionstextur
                m_CubeMapArrayTexture = m_effect.GetVariableByName("CubeMapArrayTexture").AsResource();
                m_ShadowTexture = m_effect.GetVariableByName("ShadowTexture").AsResource(); //Shadowmapping
                m_colorVariable = m_effect.GetVariableByName("CurrentColor").AsVector();

                m_effectPassLine2D = m_effect.GetTechniqueByName("DrawLine").GetPassByIndex(0);
                m_effectPassLine3D = m_effect.GetTechniqueByName("DrawLine").GetPassByIndex(1);
                m_effectPassPoint = m_effect.GetTechniqueByName("DrawPoint").GetPassByIndex(0);
                vertexLayout2D = new InputLayout(m_device, m_effectPassLine2D.Description.Signature, new InputElement[] { new InputElement("POSITION", 0, SlimDX.DXGI.Format.R32G32B32_Float, 0) });

                m_vertexLayout = new DX11.InputLayout(m_device, m_effectPass3DStandard.Description.Signature, new DX11.InputElement[]
                    {
                        new DX11.InputElement("POSITION",0,SlimDX.DXGI.Format.R32G32B32_Float,0,0),
                        new DX11.InputElement("COLOR",0,SlimDX.DXGI.Format.R8G8B8A8_UNorm,12,0),
                        new DX11.InputElement("TEXCOORD",0,SlimDX.DXGI.Format.R32G32_Float,16,0),
                        new DX11.InputElement("NORMAL",0,SlimDX.DXGI.Format.R32G32B32_Float,24,0),
                        new DX11.InputElement("TANGENT",0,SlimDX.DXGI.Format.R32G32B32_Float,36,0)
                    });

                m_effect.GetConstantBufferByName("ConstantBufferWindowSize").GetMemberByName("WindowWidth").AsScalar().Set(this.drawingArea.ClientSize.Width);
                m_effect.GetConstantBufferByName("ConstantBufferWindowSize").GetMemberByName("WindowHeight").AsScalar().Set(this.drawingArea.ClientSize.Height);

                this.samplerStateTexture0 = SamplerStateHelper.InitialValue(m_device);
                m_effect.GetVariableByName("SamplerStateTexture0").AsSampler().SetSamplerState(0, this.samplerStateTexture0);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while initializing Direct3D11: \n" + ex.Message + " " + shaderErrors);
            }
        }

        public Control DrawingControl
        {
            get
            {
                return this.drawingArea;
            }
        }

        public bool UseDisplacementMapping { get; set; } = false;

        public NormalSource NormalSource { get; set; } = NormalSource.ObjectData;

        public void Use2DShader()
        {
            this.NormalSource = NormalSource.ObjectData;
        }

        public void SetNormalInterpolationMode(GraphicMinimal.InterpolationMode mode)
        {
            if (mode == GraphicMinimal.InterpolationMode.Flat)
                m_effect.GetConstantBufferByName("ConstantBufferLight").GetMemberByName("DoFlatShading").AsScalar().Set(true);
            else
                m_effect.GetConstantBufferByName("ConstantBufferLight").GetMemberByName("DoFlatShading").AsScalar().Set(false);
        }

        public void Resize(int width, int height)
        {
            m_renderTarget.Dispose();

            m_swapChain.ResizeBuffers(2, 0, 0, DXGI.Format.R8G8B8A8_UNorm, DXGI.SwapChainFlags.AllowModeSwitch);
            
            using (var resource = DX11.Resource.FromSwapChain<DX11.Texture2D>(m_swapChain, 0))           //Farbpuffer neu anlegen
                m_renderTarget = new DX11.RenderTargetView(m_device, resource);

            m_renderTargetDepth = new DX11.DepthStencilView(m_device, new DX11.Texture2D(m_device,       //Tiefenpuffer neu anlegen
                    new DX11.Texture2DDescription()
                    {
                        Width = width,
                        Height = height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = DXGI.Format.D24_UNorm_S8_UInt,
                        Usage = DX11.ResourceUsage.Default,
                        SampleDescription = new DXGI.SampleDescription(1, 0),
                        BindFlags = DX11.BindFlags.DepthStencil,
                        CpuAccessFlags = DX11.CpuAccessFlags.None,
                        OptionFlags = DX11.ResourceOptionFlags.None
                    }));

            m_context.OutputMerger.SetTargets(m_renderTargetDepth, m_renderTarget);
            m_context.Rasterizer.SetViewports(new DX11.Viewport(0.0f, 0.0f, width, height, 0f, 1f));

            SetProjectionMatrix3D();

            m_effect.GetConstantBufferByName("ConstantBufferWindowSize").GetMemberByName("WindowWidth").AsScalar().Set(width);
            m_effect.GetConstantBufferByName("ConstantBufferWindowSize").GetMemberByName("WindowHeight").AsScalar().Set(height);

            m_renderTargetActive = m_renderTarget;
            m_renderTargetDepthActive = m_renderTargetDepth;

            //So schaltet man im Full-Screen-Modus
            //bool isFull = m_swapChain.Description.IsWindowed;
            //m_swapChain.SetFullScreenState(isFull, null);
        }

        public void SetProjectionMatrix3D(int screenWidth = 0, int screenHight = 0, float fov = 45, float zNear = 0.001f, float zFar = 3000)
        {
            if (screenWidth == 0) screenWidth = this.drawingArea.Width;
            if (screenHight == 0) screenHight = this.drawingArea.Height;

            /* m_projMatrix = SlimDX.Matrix.PerspectiveFovLH(
                    (float)Math.PI * 0.25f,//Öffnungswinkel (entspricht der Brennweite)
                    screenWidth / (float)screenHight,//Seitenverhältnis
                    0.1f, 3000f);//kleinster und größter Z-Wert*/
            m_projMatrix = TransformMatrixToSlimdx(Matrix4x4.ProjectionMatrixPerspective(fov, (float)screenWidth / (float)screenHight, zNear, zFar));
            
            if (m_effect != null) m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("ProjectionMatrix").AsMatrix().SetMatrix(m_projMatrix);
        }

        public void SetProjectionMatrix2D(float left = 0, float right = 0, float bottom = 0, float top = 0, float znear = 0, float zfar = 0)
        {
            if (left == 0 && right == 0)
            {
                m_projMatrix = TransformMatrixToSlimdx(Matrix4x4.ProjectionMatrixOrtho(0.0f, this.drawingArea.Width, this.drawingArea.Height, 0.0f, -1000.0f, +1000.0f));
            }
            else
            {
                m_projMatrix = TransformMatrixToSlimdx(Matrix4x4.ProjectionMatrixOrtho(left, right, bottom, top, znear, zfar));
            }

            if (m_effect != null) m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("ProjectionMatrix").AsMatrix().SetMatrix(m_projMatrix);
        }

        public void SetViewport(int startX, int startY, int width, int height)
        {
            m_context.Rasterizer.SetViewports(new DX11.Viewport(startX, startY, width, height, 0f, 1f));
        }

        public void ClearColorBuffer(Color clearColor)
        {
            if (m_renderTargetActive != null) 
            {
                m_context.ClearRenderTargetView(m_renderTargetActive, new Color4(clearColor));
            }
        }

        public void ClearColorDepthAndStencilBuffer(Color clearColor)
        {
            if (m_renderTargetActive != null) //Man kann das rendern in den Farbpuffer auch weglassen (Siehe Shadowmapping). Dann muss Clear weggelassen werden
            {
                m_context.ClearRenderTargetView(m_renderTargetActive, new Color4(clearColor));
            }

            if (m_renderTargetDepthActive != null)
            {
                m_context.ClearDepthStencilView(m_renderTargetDepthActive, DX11.DepthStencilClearFlags.Depth | DX11.DepthStencilClearFlags.Stencil, 1f, 0);
            }
        }

        public void ClearStencilBuffer()
        {
            if (m_renderTargetDepthActive != null)
            {
                m_context.ClearDepthStencilView(m_renderTargetDepthActive, DX11.DepthStencilClearFlags.Stencil, 1f, 0);
            }
        }

        public void ClearDepthAndStencilBuffer()
        {
             if (m_renderTargetDepthActive != null)
            {
                m_context.ClearDepthStencilView(m_renderTargetDepthActive, DX11.DepthStencilClearFlags.Depth | DX11.DepthStencilClearFlags.Stencil, 1f, 0);
            }
        }

        public void EnableWritingToTheColorBuffer()
        {
            SetBlendState(m_context.OutputMerger.BlendState.Description.RenderTargets[0].BlendEnable, ColorWriteMaskFlags.All);
        }

        public void DisableWritingToTheColorBuffer()
        {
            SetBlendState(m_context.OutputMerger.BlendState.Description.RenderTargets[0].BlendEnable, ColorWriteMaskFlags.None);
        }

        public void EnableWritingToTheDepthBuffer()
        {
            m_context.OutputMerger.DepthStencilState = DepthStencilStateHelper.SetWriteMask(m_device, m_context.OutputMerger.DepthStencilState, DepthWriteMask.All);
        }

        public void DisableWritingToTheDepthBuffer()
        {
            m_context.OutputMerger.DepthStencilState = DepthStencilStateHelper.SetWriteMask(m_device, m_context.OutputMerger.DepthStencilState, DepthWriteMask.Zero);
        }

        public void FlippBuffer()
        {
            m_swapChain.Present(0, DXGI.PresentFlags.None);
            this.drawingArea.Invalidate();
        }

        public System.Drawing.Bitmap GetDataFromColorBuffer()
        {
            Texture2D duplicate = SlimDX.Direct3D11.Resource.FromSwapChain<Texture2D>(m_swapChain, 0);
            m_context.CopyResource(m_renderTarget.Resource, duplicate);
            System.IO.Stream stream = new MemoryStream();
            DX11.Texture2D.ToStream(m_context, duplicate, ImageFileFormat.Bmp, stream);
            System.Drawing.Bitmap bild = new System.Drawing.Bitmap(stream);
            duplicate.Dispose();
            return TextureHelper.ConvertDirectXBitmapToNormalBitmap(bild);
        }

        public System.Drawing.Bitmap GetDataFromDepthBuffer()
        {
            var tex = (Texture2D)m_renderTargetDepth.Resource;
            return TextureHelper.GetTextureData(m_device, m_renderTargetDepth.Resource, tex.Description.Width, tex.Description.Height, true);
        }

        public void SetModelViewMatrixToIdentity()
        {
            m_modelviewMatrix = SlimDX.Matrix.Identity;
            UpdateModelViewMatrix();
        }

        private int GenerateNewTextureId()
        {
            int newID = 1;
            if (textures.Count > 0)
                newID = textures.Max(x => x.ID) + 1;
            return newID;
        }

        private int CreateTextureFromBitmap(System.Drawing.Bitmap bitmap)
        {
            int newID = GenerateNewTextureId();

            DX11.Texture2D texture = TextureHelper.TextureFromBitmap(this.m_device, bitmap);
            textures.Add(new Texture() { ID = newID, Width = bitmap.Width, Height = bitmap.Height, TextureView = new DX11.ShaderResourceView(this.m_device, texture) });

            //m_context.GenerateMips(textures.Last().TextureView);

            return newID;
        }

        public int GetTextureId(System.Drawing.Bitmap bitmap)
        {
            return CreateTextureFromBitmap(bitmap);
        }

        public Size GetTextureSize(int textureId)
        {
            var tex = textures.FirstOrDefault(x => x.ID == textureId);
            return new Size(tex.Width, tex.Height);
        }

        public void SetTexture(int textureID)
        {
            Texture tex = textures.FirstOrDefault(x => x.ID == textureID);
            if (tex == null) return;

            if (activeTextureDeck == 0)
            {
                m_TexturID0.SetResource(tex.TextureView);//akeutelle Textur-ID übergeben
            }
            else
                if (activeTextureDeck == 1)
                {
                    m_TexturID1.SetResource(tex.TextureView);//akeutelle Textur-ID übergeben
                }
                else
                throw new Exception($"Unknown value for activeTextureDeck={activeTextureDeck}");

        }

        public System.Drawing.Bitmap GetTextureData(int textureID)
        {
            var texi = textures.FirstOrDefault(x => x.ID == textureID);

            return TextureHelper.GetTextureData(m_device, texi.TextureView.Resource, texi.Width, texi.Height, texi.IsDepthTexture);
        }

        public int CreateEmptyTexture(int width, int height)
        {
            int newID = GenerateNewTextureId();

            //Erzeuge eine leere (Bei allen RGBA-Werten steht 0) Textur
            Texture2D texture = new Texture2D(m_device, new DX11.Texture2DDescription()
            {
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                CpuAccessFlags = DX11.CpuAccessFlags.None,
                Format = DXGI.Format.R8G8B8A8_UNorm,
                OptionFlags = DX11.ResourceOptionFlags.None,
                MipLevels = 1,
                Usage = DX11.ResourceUsage.Default, //Default = GPU RW, Immutable = GPU RO, DYNAMIC (CPU WO, GPU RO), STAGING (CPU RW, GPU RW)
                Width = width,
                Height = height,
                ArraySize = 1,
                SampleDescription = new DXGI.SampleDescription(1, 0)
            });

            var view = new DX11.ShaderResourceView(this.m_device, texture);

            texture.Dispose();

            textures.Add(new Texture() { ID = newID, Width = width, Height = height, TextureView = view });

            return newID;
        }

        public void CopyScreenToTexture(int textureID)
        {
            var texi = textures.FirstOrDefault(x => x.ID == textureID);
            var tex2d = TextureHelper.TextureFromBitmap(this.m_device, GetDataFromColorBuffer());
            texi.TextureView = new DX11.ShaderResourceView(this.m_device, tex2d);
            tex2d.Dispose();
        }

        public int CreateFramebuffer(int width, int height, bool withColorTexture, bool withDepthTexture)
        {
            int newID = 1;
            if (framebuffers.Keys.Count > 0)
                newID = framebuffers.Keys.Max() + 1;

            var framebuffer = new MyFramebuffer(m_device, width, height, withColorTexture, withDepthTexture)
            {
                TextureIdColor = GenerateNewTextureId(),
                TextureIdDepth = GenerateNewTextureId() + 1
            };

            framebuffers.Add(newID, framebuffer);
            if (withColorTexture)
            {
                textures.Add(new Texture() { ID = framebuffer.TextureIdColor, Width = width, Height = height, TextureView = framebuffer.ShaderResourceViewColor });
            }
            if (withDepthTexture)
            {
                textures.Add(new Texture() { ID = framebuffer.TextureIdDepth, Width = width, Height = height, TextureView = framebuffer.ShaderResourceViewDepth, IsDepthTexture = true });
            }
                        
            return newID;
        }

        public void EnableRenderToFramebuffer(int framebufferId)
        {
            m_renderTargetActive = framebuffers[framebufferId].RenderTargetView;
            m_renderTargetDepthActive = framebuffers[framebufferId].DepthStencilView;

            m_context.Rasterizer.SetViewports(new Viewport(0, 0, framebuffers[framebufferId].Width, framebuffers[framebufferId].Height, 0, 1.0f));
            
            if (framebuffers[framebufferId].RenderTargetView != null)
            {
                m_context.ClearRenderTargetView(framebuffers[framebufferId].RenderTargetView, new Color4(0, 0, 0));
                //m_context.ClearRenderTargetView(m_renderTargetActive, new Color4(0, 1, 1, 1));
            }

            if (framebuffers[framebufferId].DepthStencilView != null)
            {
                m_context.ClearDepthStencilView(
                    framebuffers[framebufferId].DepthStencilView,
                    DepthStencilClearFlags.Depth |
                    DepthStencilClearFlags.Stencil,
                    1.0f, 0);
            }

            m_context.OutputMerger.SetTargets(m_renderTargetDepthActive, m_renderTargetActive);
        }

        public void DisableRenderToFramebuffer()
        {
            m_renderTargetActive = m_renderTarget;
            m_renderTargetDepthActive = m_renderTargetDepth;

            m_context.Rasterizer.SetViewports(new Viewport(0, 0, this.Width, this.Height, 0f, 1f));
            m_context.OutputMerger.SetTargets(m_renderTargetDepthActive, m_renderTargetActive);
        }

        public int GetColorTextureIdFromFramebuffer(int framebufferId)
        {
            return framebuffers[framebufferId].TextureIdColor;
        }

        public int GetDepthTextureIdFromFramebuffer(int framebufferId)
        {
            return framebuffers[framebufferId].TextureIdDepth;
        }

        public int CreateCubeMap(int cubeMapSize = 256)
        {
            return this.cubemaps.CreateCubeMap(m_device, cubeMapSize);
        }

        public void EnableRenderToCubeMap(int cubemapID, int cubemapSide, Color clearColor)
        {
            if (cubemapSide == 0) //Wenn 1. Durchlauf, dann merke, wohin es vor der Cubemap-Erstellung gerendert hat
            {
                cubemaps.renderTargetViewBevoreEnableWriteToCubemap = m_renderTargetActive;
                cubemaps.depthStencelViewBevoreEnableWriteToCubemap = m_renderTargetDepthActive;
                cubemaps.viewportBevoreEnableWriteToCubemap = m_context.Rasterizer.GetViewports()[0];
                cubemaps.CurrentCubmapToRenderIn = cubemaps[cubemapID];
            }

            m_renderTargetActive = cubemaps[cubemapID].DynamicCubeMapRTV[cubemapSide];
            m_renderTargetDepthActive = cubemaps[cubemapID].DynamicCubeMapDSV;

            m_context.Rasterizer.SetViewports(new Viewport(0, 0, cubemaps[cubemapID].CubeMapSize, cubemaps[cubemapID].CubeMapSize, 0, 1.0f));
            m_context.ClearRenderTargetView(cubemaps[cubemapID].DynamicCubeMapRTV[cubemapSide], clearColor);
            m_context.ClearDepthStencilView(
                cubemaps[cubemapID].DynamicCubeMapDSV,
                DepthStencilClearFlags.Depth |
                DepthStencilClearFlags.Stencil,
                1.0f, 0);
            m_context.OutputMerger.SetTargets(m_renderTargetDepthActive, m_renderTargetActive);
        }

        public System.Drawing.Bitmap GetColorDataFromCubeMapSide(int cubemapID, int cubemapSide)
        {
            //return GetDataFromColorOrDepthBuffer(cubemaps[cubemapID].DynamicCubeMapRTV[cubemapSide].Resource);

            //Der Grund warum ich kein GetDataFromColorOrDepthBuffer() nutzen kann ist, weil ich MipLevels auf 0 statt 1 udn ArraySize auf 6 statt 1 stellen muss
            Texture2D duplicate = new Texture2D(m_device, new Texture2DDescription()
            {
                Width = cubemaps[cubemapID].CubeMapSize,
                Height = cubemaps[cubemapID].CubeMapSize,
                MipLevels = 0,
                ArraySize = 6, 
                SampleDescription = new SampleDescription(1, 0),
                Format = Format.R8G8B8A8_UNorm,
                Usage = ResourceUsage.Default, //Default = GPU RW, Immutable = GPU RO, DYNAMIC (CPU WO, GPU RO), STAGING (CPU RW, GPU RW)
                BindFlags = BindFlags.RenderTarget,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
            m_context.CopyResource(cubemaps[cubemapID].DynamicCubeMapRTV[cubemapSide].Resource, duplicate);
            System.IO.Stream stream = new MemoryStream();
            DX11.Texture2D.ToStream(m_context, duplicate, ImageFileFormat.Bmp, stream);
            System.Drawing.Bitmap bild = new System.Drawing.Bitmap(stream);
            duplicate.Dispose();
            return bild;
        }

        public void DisableRenderToCubeMap()
        {
            m_renderTargetActive = cubemaps.renderTargetViewBevoreEnableWriteToCubemap;
            m_renderTargetDepthActive = cubemaps.depthStencelViewBevoreEnableWriteToCubemap;
            m_context.Rasterizer.SetViewports(cubemaps.viewportBevoreEnableWriteToCubemap);

            m_context.OutputMerger.SetTargets(m_renderTargetDepthActive, m_renderTargetActive);

            m_context.GenerateMips(cubemaps.CurrentCubmapToRenderIn.DynamicCubeMapSRV);
            m_context.GenerateMips(cubemaps.CurrentCubmapToRenderIn.TextureArrayShaderResourceView);
            cubemaps.CurrentCubmapToRenderIn = null;
        }

        public void EnableAndBindCubemapping(int cubemapID)
        {
            m_effect.GetConstantBufferByName("ConstantBufferTexture").GetMemberByName("UseCubemap").AsScalar().Set(true);
            m_CubeMapTexture.SetResource(cubemaps[cubemapID].DynamicCubeMapSRV);
            m_CubeMapArrayTexture.SetResource(cubemaps[cubemapID].TextureArrayShaderResourceView);
        }

        public void DisableCubemapping()
        {
            m_effect.GetConstantBufferByName("ConstantBufferTexture").GetMemberByName("UseCubemap").AsScalar().Set(false);
        }

        public bool ReadFromShadowmap
        {
            set => m_effect.GetConstantBufferByName("ConstantBufferTexture").GetMemberByName("UseShadowmap").AsScalar().Set(value);
        }

        //http://richardssoftware.net/Home/Post/37
        public int CreateShadowmap(int width, int height)
        {
            return CreateFramebuffer(width, height, false, true);
        }

        public void EnableRenderToShadowmap(int shadowmapId)
        {
            createShadowmap = true;
            m_ShadowTexture.SetResource(null); //Man darf nicht gleichzeitig in Textur rendern und diese lesend binden
            EnableRenderToFramebuffer(shadowmapId);
        }

        public void BindShadowTexture(int shadowmapId)
        {
            if (createShadowmap == false) //Es muss verhindert werden, das man einerseits in eine Textur rendert, anderseits diese Texture aber als ShaderResoruceView gebunden ist
            {
                m_ShadowTexture.SetResource(framebuffers[shadowmapId].ShaderResourceViewDepth);//akeutelle Textur-ID übergeben
            }
        }

        public void DisableRenderToShadowmapTexture()
        {
            createShadowmap = false;
            DisableRenderToFramebuffer();
        }

        public void SetShadowmapMatrix(Matrix4x4 shadowMatrix)
        {
            var shadowMatrixSlimmi = TransformMatrixToSlimdx(shadowMatrix);

            m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("ShadowMatrix").AsMatrix().SetMatrix(shadowMatrixSlimmi);
        }

        public bool IsRenderToShadowmapEnabled()
        {
            return createShadowmap;
        }

        public System.Drawing.Bitmap GetShadowmapAsBitmap(int shadowmapId)
        {
            return GetTextureData(framebuffers[shadowmapId].TextureIdDepth);
        }

        public void SetActiveTexture0()
        {
            activeTextureDeck = 0;
        }

        public void SetActiveTexture1()
        {
            activeTextureDeck = 1;
        }

        public void EnableTexturemapping()
        {
            if (activeTextureDeck == 0)
                m_effect.GetConstantBufferByName("ConstantBufferTexture").GetMemberByName("UseTexture0").AsScalar().Set(true);

            if (activeTextureDeck == 1)
                m_effect.GetConstantBufferByName("ConstantBufferTexture").GetMemberByName("UseTexture1").AsScalar().Set(true);
        }

        public void SetTextureFilter(TextureFilter filter)
        {
            Filter slimiFilter = Filter.MinMagMipPoint;
            switch (filter)
            {
                case TextureFilter.Point:
                    slimiFilter = Filter.MinMagMipPoint;
                    break;
                case TextureFilter.Linear:
                    slimiFilter = Filter.MinMagMipLinear;
                    break;
                case TextureFilter.Anisotroph:
                    slimiFilter = Filter.Anisotropic;
                    break;
            }

            if (activeTextureDeck == 0)
                this.samplerStateTexture0 = SamplerStateHelper.SetFilter(m_device, this.samplerStateTexture0, slimiFilter);
            //m_effect.GetConstantBufferByName("ConstantBufferTexture").GetMemberByName("TextureFilter0").AsScalar().Set((int)filter);

            //if (activeTextureDeck == 1)
            //    m_effect.GetConstantBufferByName("ConstantBufferTexture").GetMemberByName("TextureFilter1").AsScalar().Set((int)filter);

            m_effect.GetVariableByName("SamplerStateTexture0").AsSampler().SetSamplerState(0, this.samplerStateTexture0);
        }

        public void DisableTexturemapping()
        {
            if (activeTextureDeck == 0)
                m_effect.GetConstantBufferByName("ConstantBufferTexture").GetMemberByName("UseTexture0").AsScalar().Set(false);

            if (activeTextureDeck == 1)
                m_effect.GetConstantBufferByName("ConstantBufferTexture").GetMemberByName("UseTexture1").AsScalar().Set(false);
        }

        public void SetTextureMatrix(Matrix3x3 matrix3x3)
        {
            var slimMatrix = TransformMatrixToSlimdx(Matrix3x3.Get4x4Matrix(matrix3x3));
            m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("TextureMatrix").AsMatrix().SetMatrix(slimMatrix);
        }

        public void SetTextureScale(Vector2D scale)
        {
            m_effect.GetConstantBufferByName("ConstantBufferTexture").GetMemberByName("TexturScaleFaktorX").AsScalar().Set(scale.X);
            m_effect.GetConstantBufferByName("ConstantBufferTexture").GetMemberByName("TexturScaleFaktorY").AsScalar().Set(scale.Y);
        }

        public void SetTesselationFactor(float tesselationFactor)              // Wird bei Displacementmapping benötigt. In so viele Dreiecke wird Dreieck zerlegt
        {
            m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("TesselationFactor").AsScalar().Set(tesselationFactor);
        }

        public void SetTextureHeighScaleFactor(float textureHeighScaleFactor)    // Höhenskalierung bei Displacement- und Parallaxmapping
        {
            m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("HeighScaleFactor").AsScalar().Set(textureHeighScaleFactor);
        }

        public void SetTextureMode(TextureMode textureMode)
        {
            TextureAddressMode slimiAddress = TextureAddressMode.Wrap; //Wrap = Repeat, Clamp = No Repeat
            switch (textureMode)
            {
                case TextureMode.Repeat:
                    slimiAddress = TextureAddressMode.Wrap;
                    break;
                case TextureMode.Clamp:
                    slimiAddress = TextureAddressMode.Clamp;
                    break;
            }

            if (activeTextureDeck == 0)
                this.samplerStateTexture0 = SamplerStateHelper.SetAddressUVW(m_device, this.samplerStateTexture0, slimiAddress);

            m_effect.GetVariableByName("SamplerStateTexture0").AsSampler().SetSamplerState(0, this.samplerStateTexture0);
        }

        public int GetTriangleArrayId(GraphicGlobal.Triangle[] data)
        {
            int triangleArrayID = 1;
            if (triangleArrays.Count > 0) triangleArrayID = triangleArrays.Keys.Max() + 1;

            List<Vertex> vertexList;
            List<uint> indexList;
            TriangleHelper.TransformTriangleListToVertexIndexList(data, out vertexList, out indexList);

            List<VertexDX> vertexList1 = vertexList.Select(x => new VertexDX(new Vector3(x.Position.X, x.Position.Y, x.Position.Z),
                                                          Color.FromArgb((int)(m_colorVariable.GetVector().W * 255), (int)(m_colorVariable.GetVector().Z * 255), (int)(m_colorVariable.GetVector().Y * 255), (int)(m_colorVariable.GetVector().X * 255)).ToArgb(),
                                                          new Vector2(x.TexcoordU, x.TexcoordV),
                                                          new Vector3(x.Normal.X, x.Normal.Y, x.Normal.Z),
                                                          new Vector3(x.Tangent.X, x.Tangent.Y, x.Tangent.Z))).ToList();

            triangleArrays.Add(triangleArrayID, new VertexArray() 
            { 
                vertexBuffer = new SlimDX.Direct3D11.Buffer(m_device, new DataStream(vertexList1.ToArray(), true, true), Marshal.SizeOf(typeof(VertexDX)) * vertexList1.Count, ResourceUsage.Default, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, Marshal.SizeOf(typeof(VertexDX))),
                indexBuffer = new SlimDX.Direct3D11.Buffer(m_device, new DataStream(indexList.ToArray(), true, true), sizeof(uint) * indexList.Count, ResourceUsage.Default, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, sizeof(uint)),//new SlimDX.Direct3D11.Buffer(m_device, new DataStream(indexList.ToArray(), false, false), ibd),
                indexCount = indexList.Count 
            });
            //triangleArrays.Add(triangleArrayID, new VertexArray() { vertexBuffer = TransformTriangleArrayToDXVertexBuffer(data), vertexCount = data.Length * 3 });

            return triangleArrayID;
        }

        private void UpdateModelViewMatrix()
        {
            SlimDX.Matrix mv_obj_to_eye = m_modelviewMatrix * m_projMatrix;
            SlimDX.Matrix mv_obj_to_world = m_modelviewMatrix * this.inverseCameraMatrix;
            SlimDX.Matrix worldToObj = SlimDX.Matrix.Invert(mv_obj_to_world);
            m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("CameraMatrix").AsMatrix().SetMatrix(this.cameraMatrix);
            m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("WorldViewProj").AsMatrix().SetMatrix(mv_obj_to_eye);
            //m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("NormalMatrix").AsMatrix().SetMatrix(SlimDX.Matrix.Transpose(SlimDX.Matrix.Invert(m_modelviewMatrix)));
            m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("NormalMatrix").AsMatrix().SetMatrix(SlimDX.Matrix.Transpose(worldToObj));
            m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("ObjToWorld").AsMatrix().SetMatrix(mv_obj_to_world);
            m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("WorldToObj").AsMatrix().SetMatrix(worldToObj);
            
        }

        public void DrawTriangleArray(int triangleArrayId)
        {
            this.m_context.InputAssembler.InputLayout = m_vertexLayout;
            if (this.UseDisplacementMapping && createShadowmap == false)
                this.m_context.InputAssembler.PrimitiveTopology = DX11.PrimitiveTopology.PatchListWith3ControlPoints;
            else
                this.m_context.InputAssembler.PrimitiveTopology = DX11.PrimitiveTopology.TriangleList;
            this.m_context.InputAssembler.SetVertexBuffers(0, new DX11.VertexBufferBinding(triangleArrays[triangleArrayId].vertexBuffer, Marshal.SizeOf(typeof(VertexDX)), 0));
            this.m_context.InputAssembler.SetIndexBuffer(triangleArrays[triangleArrayId].indexBuffer, DXGI.Format.R32_UInt, 0);

            if (createShadowmap == true)
            {
                m_effectPass3DCreateShadowmap.Apply(m_context);
            }else
            if (this.NormalSource == NormalSource.Parallax)
            {
                m_effectPass3DParallax.Apply(m_context);
            } 
             else if (useMouseHitTestShader == true)
             {
                 m_effectPass3DMouseHitTest.Apply(m_context);
             }else //NormalSource = Normalmap oder ObjectData oder DisplacementMapping==True
             {
                 m_effectPass3DStandard.Apply(m_context);//Effekt-Variablen(Matrizen, Textur-ID) vom HS in GraKa übertragen
             }

            if (this.UseDisplacementMapping == false)
            {
                //this.m_context.GeometryShader.Set(null);
                this.m_context.HullShader.Set(null);
                this.m_context.DomainShader.Set(null);
            }

            //Wenn ich das so mache, MUSS ich die Textur über Texture0.Sample(SamplerStateTexture0, texCoords); auslesen
            //Das Auslesen einer Textur mit eigenen Filter geht dann nicht mehr. Die Aufrufe 
            //return Texture0.Sample( TextureFilterPoint, input.tex ) und return Texture0.Sample( TextureFilterLinear, input.tex ) 
            //verhalten sich dann so, als ob man immer nur Texture0.Sample(SamplerStateTexture0, texCoords) verwenden würde
            //m_context.PixelShader.SetSampler(this.samplerStateTexture0, 0);             

            m_context.DrawIndexed(triangleArrays[triangleArrayId].indexCount, 0, 0);
        }

        public void RemoveTriangleArray(int triangleArrayId)
        {
            if (triangleArrays.ContainsKey(triangleArrayId))
            {
                triangleArrays[triangleArrayId].vertexBuffer.Dispose();
                triangleArrays[triangleArrayId].indexBuffer.Dispose();
                triangleArrays.Remove(triangleArrayId);
            }
        }

        public void DrawTriangleStrip(Vector3D v1, Vector3D v2, Vector3D v3, Vector3D v4)
        {            
            int id = GetTriangleArrayId(new GraphicGlobal.Triangle[] { new GraphicGlobal.Triangle(v3, v2, v1), new GraphicGlobal.Triangle(v2, v3, v4) });
            DrawTriangleArray(id);
            RemoveTriangleArray(id);
        }

        private void DrawLineOrPoint(Vector3[] punkte, bool use2D)
        {
            if (this.createShadowmap) return;

            Vector3[] vertexList = punkte;
            var lineBuffer = new SlimDX.Direct3D11.Buffer(m_device, new DataStream(vertexList, true, true), Marshal.SizeOf(typeof(Vector3)) * vertexList.Length, ResourceUsage.Default, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, Marshal.SizeOf(typeof(Vector3)));

            this.m_context.InputAssembler.InputLayout = vertexLayout2D;
            if (punkte.Length == 2)
            {
                this.m_context.InputAssembler.PrimitiveTopology = DX11.PrimitiveTopology.LineList;
            }
            else
            {
                this.m_context.InputAssembler.PrimitiveTopology = DX11.PrimitiveTopology.PointList;
            }
            
            this.m_context.InputAssembler.SetVertexBuffers(0, new DX11.VertexBufferBinding(lineBuffer, Marshal.SizeOf(typeof(Vector3)), 0));
            
            SlimDX.Matrix mv_obj_to_eye = m_modelviewMatrix * m_projMatrix;

            m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("WorldViewProj").AsMatrix().SetMatrix(mv_obj_to_eye);
            if (this.createShadowmap)
            {
                var shadowMatrix = m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("ShadowMatrix").AsMatrix().GetMatrix();
                m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("WorldViewProj").AsMatrix().SetMatrix(shadowMatrix);
            }

            if (punkte.Length == 2)
            {
                if (use2D)
                    m_effectPassLine2D.Apply(this.m_context);//Effekt-Variablen(Matrizen, Textur-ID) vom HS in GraKa übertragen
                else
                    m_effectPassLine3D.Apply(this.m_context);//Effekt-Variablen(Matrizen, Textur-ID) vom HS in GraKa übertragen
            }
            else
                m_effectPassPoint.Apply(this.m_context);

            m_context.Draw(punkte.Length, 0);

            this.m_context.InputAssembler.SetIndexBuffer(null, Format.Unknown, 0);

            lineBuffer.Dispose();
        }
        
        public void DrawLine(Vector3D p1, Vector3D p2)
        {
            DrawLineOrPoint(new Vector3[] { new Vector3(p1.X, p1.Y, p1.Z), new Vector3(p2.X, p2.Y, p2.Z) }, false);
        }

        public void SetLineWidth(float lineWidth)
        {
            m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("LineWidth").AsScalar().Set(lineWidth);
        }

        public void DrawPoint(Vector3D position)
        {
            DrawLineOrPoint(new Vector3[] { new Vector3(position.X, position.Y, position.Z) }, false);
        }

        public void SetPointSize(float size)
        {
            m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("PointWidth").AsScalar().Set((int)size);
        }

        public Color GetPixelColorFromColorBuffer(int x, int y)
        {
            return GetDataFromColorBuffer().GetPixel(x, y);
        }

        public Matrix4x4 GetInverseModelMatrix(Vector3D position, Vector3D orientation, float size)
        {
            SlimDX.Matrix matrix = SlimDX.Matrix.Identity;
            matrix = SlimDX.Matrix.Scaling(1 / size, 1 / size, 1 / size) * matrix;
            matrix = SlimDX.Matrix.RotationZ((float)(-orientation.Z * Math.PI / 180.0f)) * matrix;
            matrix = SlimDX.Matrix.RotationY((float)(-orientation.Y * Math.PI / 180.0f)) * matrix;
            matrix = SlimDX.Matrix.RotationX((float)(-orientation.X * Math.PI / 180.0f)) * matrix;
            matrix = SlimDX.Matrix.Translation(-position.X, -position.Y, -position.Z) * matrix;
            return new Matrix4x4(matrix.ToArray());
        }

        public Matrix4x4 GetModelMatrix(Vector3D position, Vector3D orientation, float size)
        {
            SlimDX.Matrix matrix = SlimDX.Matrix.Identity;
            matrix = SlimDX.Matrix.Translation(position.X, position.Y, position.Z) * matrix;
            matrix = SlimDX.Matrix.RotationX((float)(orientation.X * Math.PI / 180.0f)) * matrix;
            matrix = SlimDX.Matrix.RotationY((float)(orientation.Y * Math.PI / 180.0f)) * matrix;
            matrix = SlimDX.Matrix.RotationZ((float)(orientation.Z * Math.PI / 180.0f)) * matrix;
            matrix = SlimDX.Matrix.Scaling(size, size, size) * matrix;
            return new Matrix4x4(matrix.ToArray());
        }

        public void PushMatrix()
        {
            modelviewMatrixStack.Push(m_modelviewMatrix);
        }

        public void PopMatrix()
        {
            m_modelviewMatrix = modelviewMatrixStack.Pop();
            UpdateModelViewMatrix();
        }

        public void PushProjectionMatrix()
        {
            projectionMatrixStack.Push(m_projMatrix);
        }

        public void PopProjectionMatrix()
        {
            m_projMatrix = projectionMatrixStack.Pop();
            m_effect.GetConstantBufferByName("ConstantBufferMatrix").GetMemberByName("ProjectionMatrix").AsMatrix().SetMatrix(m_projMatrix);
        }

        public void MultMatrix(Matrix4x4 matrix)
        {
            var m = matrix.Values;
            m_modelviewMatrix = new SlimDX.Matrix()
            {
                M11 = m[0],     M12 = m[1],     M13 = m[2],   M14 = m[3],
                M21 = m[4],     M22 = m[5],     M23 = m[6],   M24 = m[7],
                M31 = m[8],     M32 = m[9],     M33 = m[10],  M34 = m[11],
                M41 = m[12],    M42 = m[13],    M43 = m[14],  M44 = m[15]
            } * m_modelviewMatrix;
            UpdateModelViewMatrix();
        }

        public void Scale(float size)
        {
            m_modelviewMatrix = SlimDX.Matrix.Scaling(size, size, size) * m_modelviewMatrix;
        }

        public Matrix4x4 GetProjectionMatrix()
        {
            return new Matrix4x4(m_projMatrix.ToArray());
        }

        public Matrix4x4 GetModelViewMatrix()
        {
            return new Matrix4x4(m_modelviewMatrix.ToArray());
        }

        public void SetColor(float R, float G, float B, float A)
        {
            m_colorVariable.Set(new Vector4(PixelHelper.Clamp(R, 0, 1), PixelHelper.Clamp(G, 0, 1), PixelHelper.Clamp(B, 0, 1), PixelHelper.Clamp(A, 0, 1)));
        }

        public void SetSpecularHighlightPowExponent(float specularHighlightPowExponent)
        {
            m_effect.GetConstantBufferByName("ConstantBufferLight").GetMemberByName("SpecularHighlightPowExponent").AsScalar().Set(specularHighlightPowExponent);
        }

        private SlimDX.Matrix cameraMatrix = SlimDX.Matrix.Identity;
        private SlimDX.Matrix inverseCameraMatrix = SlimDX.Matrix.Identity;
        public void SetModelViewMatrixToCamera(Camera camera)
        {
            this.cameraMatrix = TransformMatrixToSlimdx(Matrix4x4.LookAt(camera.Position, camera.Forward, camera.Up));
            this.inverseCameraMatrix = TransformMatrixToSlimdx(Matrix4x4.InverseLookAt(camera.Position, camera.Forward, camera.Up));
            /*m_modelviewMatrix = SlimDX.Matrix.LookAtLH(
                 new Vector3(kamera.position.x, kamera.position.y, kamera.position.z),//Kameraposition
                 new Vector3(kamera.position.x + kamera.richtung1.x, kamera.position.y + kamera.richtung1.y, kamera.position.z + kamera.richtung1.z),//Auf diesen Punkt schaut die Kamera
                 new Vector3(kamera.richtung2.x, kamera.richtung2.y, kamera.richtung2.z)) * m_modelviewMatrix;//Zeigt, wo oben ist */
            m_modelviewMatrix = this.cameraMatrix;
            UpdateModelViewMatrix();

            m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("CameraPosition").AsVector().Set(new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));
        }

        public void SetPositionOfAllLightsources(List<RasterizerLightsource> lights)
        {
            Vector4[] lightPositions = new Vector4[lights.Count];
            float[] CONSTANT_ATTENUATIONS = new float[lights.Count];
            float[] LINEAR_ATTENUATIONS = new float[lights.Count];
            float[] QUADRATIC_ATTENUATIONS = new float[lights.Count];
            Vector4[] lightDirections = new Vector4[lights.Count];
            float[] lightSpotCutoff = new float[lights.Count];
            float[] lightSpotExponent = new float[lights.Count];
            
            for (int i = 0; i < lights.Count; i++)
            {
                lightPositions[i] = new Vector4(lights[i].Position.X, lights[i].Position.Y, lights[i].Position.Z, 1);
                CONSTANT_ATTENUATIONS[i] = lights[i].ConstantAttenuation;
                LINEAR_ATTENUATIONS[i] = lights[i].LinearAttenuation;
                QUADRATIC_ATTENUATIONS[i] = lights[i].QuadraticAttenuation;
                lightSpotCutoff[i] = (float)Math.Cos(lights[i].SpotCutoff * Math.PI / 180);
                lightDirections[i] = Vector4.Normalize(new Vector4(lights[i].SpotDirection.X, lights[i].SpotDirection.Y, lights[i].SpotDirection.Z, 0));
                lightSpotExponent[i] = lights[i].SpotExponent;
            }

            m_effect.GetConstantBufferByName("ConstantBufferLight").GetMemberByName("LightCount").AsScalar().Set(lights.Count);
            m_effect.GetConstantBufferByName("ConstantBufferLight").GetMemberByName("LightPositions").AsVector().Set(lightPositions);
            m_effect.GetConstantBufferByName("ConstantBufferLight").GetMemberByName("CONSTANT_ATTENUATIONS").AsScalar().Set(CONSTANT_ATTENUATIONS);
            m_effect.GetConstantBufferByName("ConstantBufferLight").GetMemberByName("LINEAR_ATTENUATIONS").AsScalar().Set(LINEAR_ATTENUATIONS);
            m_effect.GetConstantBufferByName("ConstantBufferLight").GetMemberByName("QUADRATIC_ATTENUATIONS").AsScalar().Set(QUADRATIC_ATTENUATIONS);
            m_effect.GetConstantBufferByName("ConstantBufferLight").GetMemberByName("LightDirections").AsVector().Set(lightDirections);
            m_effect.GetConstantBufferByName("ConstantBufferLight").GetMemberByName("LightSpotCutoffs").AsScalar().Set(lightSpotCutoff);
            m_effect.GetConstantBufferByName("ConstantBufferLight").GetMemberByName("LightSpotExponents").AsScalar().Set(lightSpotExponent);
        }

        public void EnableLighting()
        {
            m_effect.GetConstantBufferByName("ConstantBufferLight").GetMemberByName("LightingIsEnabled").AsScalar().Set(true);
        }

        public void DisableLighting()
        {
            m_effect.GetConstantBufferByName("ConstantBufferLight").GetMemberByName("LightingIsEnabled").AsScalar().Set(false);
        }

        public void SetBlendingWithBlackColor()
        {
            SetBlendState(false, m_context.OutputMerger.BlendState.Description.RenderTargets[0].RenderTargetWriteMask);
            m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("BlendingBlackColor").AsScalar().Set(true);
        }

        public void SetBlendingWithAlpha()
        {
            SetBlendState(true, m_context.OutputMerger.BlendState.Description.RenderTargets[0].RenderTargetWriteMask);
            m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("BlendingBlackColor").AsScalar().Set(false);
        }

        private void UseAlphaBlendingAndDiscardTransparent(Color colorFactor)
        {
            //Jemand möchte eine Figur teilweise transparent zeichnen
            if (colorFactor.A < 255)
            {
                //Es wird Alpha-Gewichtet in den ColorBuffer geschrieben
                SetBlendingWithAlpha();
                m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("Discard100Transparent").AsScalar().Set(false);
            }
            else
            {
                //Nutze kein Alpha-Blending sondern zeichne überhaupt nicht in den ColorBuffer, wenn 
                //das Pixel zu 100% Transparent ist (colorFactor.A ist 255 aber im Bitmap sind manche Pixel transparent)
                DisableBlending();
                m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("Discard100Transparent").AsScalar().Set(true);
            }
        }

        public void EnableWireframe()
        {
            m_context.Rasterizer.State = RasterizerStateHelper.SetFillMode(m_device, m_context.Rasterizer.State, DX11.FillMode.Wireframe);
        }

        public void DisableWireframe()
        {
            m_context.Rasterizer.State = RasterizerStateHelper.SetFillMode(m_device, m_context.Rasterizer.State, DX11.FillMode.Solid);
        }

        public void EnableExplosionEffect()
        {
            m_effect.GetConstantBufferByName("ConstantExplosionEffect").GetMemberByName("ExplosionEffectIsEnabled").AsScalar().Set(true);
        }

        public void DisableExplosionEffect()
        {
            m_effect.GetConstantBufferByName("ConstantExplosionEffect").GetMemberByName("ExplosionEffectIsEnabled").AsScalar().Set(false);
        }

        private float explosionsRadius = 1;
        public float ExplosionsRadius
        {
            get
            {
                return this.explosionsRadius;
            }
            set
            {
                this.explosionsRadius = value;
                m_effect.GetConstantBufferByName("ConstantExplosionEffect").GetMemberByName("ExplosionsRadius").AsScalar().Set(value);
            }
        } 

        public int Time // Explosionseffekt braucht Timewert
        { 
            get
            {
                return this.time;
            }
            set
            {
                this.time = value;
                m_effect.GetConstantBufferByName("ConstantExplosionEffect").GetMemberByName("Time").AsScalar().Set(time);
            } 
        } 

        public void DisableBlending()
        {
            SetBlendState(false, m_context.OutputMerger.BlendState.Description.RenderTargets[0].RenderTargetWriteMask);
            m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("BlendingBlackColor").AsScalar().Set(false);
            m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("Discard100Transparent").AsScalar().Set(false);
        }

        public void EnableCullFace()
        {
            m_context.Rasterizer.State = RasterizerStateHelper.SetCullMode(m_device, m_context.Rasterizer.State, DX11.CullMode.Back);

            m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("CullFaceIsEnabled").AsScalar().Set(true);
        }

        public void DisableCullFace()
        {
            m_context.Rasterizer.State = RasterizerStateHelper.SetCullMode(m_device, m_context.Rasterizer.State, DX11.CullMode.None);

            m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("CullFaceIsEnabled").AsScalar().Set(false);
        }

        public void SetFrontFaceConterClockWise()
        {
            m_context.Rasterizer.State = RasterizerStateHelper.SetFrontCounterClockwise(m_device, m_context.Rasterizer.State, true);
        }

        public void SetFrontFaceClockWise()
        {
            m_context.Rasterizer.State = RasterizerStateHelper.SetFrontCounterClockwise(m_device, m_context.Rasterizer.State, false);
        }

        public void EnableDepthTesting()
        {
            m_context.OutputMerger.DepthStencilState = DepthStencilStateHelper.SetDepthIsEnabled(m_device, m_context.OutputMerger.DepthStencilState, true);
            m_context.Rasterizer.State = RasterizerStateHelper.SetDepthClipEnabled(m_device, m_context.Rasterizer.State, true);
        }

        public void DisableDepthTesting()
        {
            m_context.OutputMerger.DepthStencilState = DepthStencilStateHelper.SetDepthIsEnabled(m_device, m_context.OutputMerger.DepthStencilState, false);
            m_context.Rasterizer.State = RasterizerStateHelper.SetDepthClipEnabled(m_device, m_context.Rasterizer.State, false);
        }

        public System.Drawing.Bitmap GetStencilTestImage()
        {
            throw new NotImplementedException();
        }

        public void EnableStencilTest()
        {
            m_context.OutputMerger.DepthStencilState = DepthStencilStateHelper.SetIsStencilEnabled(m_device, m_context.OutputMerger.DepthStencilState, true);
        }

        public void DisableStencilTest()
        {
            m_context.OutputMerger.DepthStencilState = DepthStencilStateHelper.SetIsStencilEnabled(m_device, m_context.OutputMerger.DepthStencilState, false);
        }

        //http://www.richardssoftware.net/2013/08/planar-reflections-and-shadows-using.html
        //m_effectPass.Description.StencilReference
        //              So arbeitet der Stenciltest bei Direct3D
        //              if ( stencilRef & stencilReadMask (operation) pixel_value & stencilReadMask) {
        //                  accept pixel;
        //              } else {
        //                  reject pixel;
        //              }
        //stencilRef = m_context.OutputMerger.DepthStencilReference
        //stencilReadMask = m_context.OutputMerger.DepthStencilState.Description.StencilReadMask
        public void SetStencilRead_NotEqualZero()
        {
            m_context.OutputMerger.DepthStencilState = DepthStencilStateHelper.SetStencilReadParameters(m_device, m_context.OutputMerger.DepthStencilState,
                0xFF,
                StencilOperation.Keep, // Does not update the stencil-buffer entry
                Comparison.NotEqual,   // If the source data is not equal to the destination data, the comparison passes
                StencilOperation.Keep,
                Comparison.NotEqual);

            m_context.OutputMerger.DepthStencilReference = 0;
        }

        public bool SetStencilWrite_TwoSide()
        {
            m_context.OutputMerger.DepthStencilState = DepthStencilStateHelper.SetStencilWriteParameters(m_device, m_context.OutputMerger.DepthStencilState,
                0xFF,
                StencilOperation.Decrement,
                Comparison.Always,
                StencilOperation.Increment,
                Comparison.Always);

            m_context.OutputMerger.DepthStencilReference = 1;
            return true;
        }

        public void SetStencilWrite_Increase()
        {
            m_context.OutputMerger.DepthStencilState = DepthStencilStateHelper.SetStencilWriteParameters(m_device, m_context.OutputMerger.DepthStencilState,
                0xFF,
                StencilOperation.Increment,
                Comparison.Always);

            m_context.OutputMerger.DepthStencilReference = 1;
        }

        public void SetStencilWrite_Decrease()
        {
            m_context.OutputMerger.DepthStencilState = DepthStencilStateHelper.SetStencilWriteParameters(m_device, m_context.OutputMerger.DepthStencilState,
                0xFF,
                StencilOperation.Decrement,
                Comparison.Always);
        }
        

        private Vector2D mouseHitTestPosition = null;
        public void StartMouseHitTest(Point mousePosition)
        {
            this.mouseHitTestPosition = new Vector2D(mousePosition.X, mousePosition.Y);
            useMouseHitTestShader = true;
            return;
        }

        public void AddObjektIdForMouseHitTest(int objektId)
        {
            m_effect.GetConstantBufferByName("ConstantBufferCommon").GetMemberByName("MouseHitId").AsScalar().Set(objektId);
        }

        public int GetMouseHitTestResult()
        {
            useMouseHitTestShader = false;
            return (int)GetPixelColorFromColorBuffer((int)this.mouseHitTestPosition.X, (int)this.mouseHitTestPosition.Y).R;
        }

        #endregion

        #region Private-Methoden

        
        private DX11.Buffer TransformTriangleArrayToDXVertexBuffer(GraphicGlobal.Triangle[] data)
        {
            VertexDX[] vertexArray = new VertexDX[data.Length * 3];
            for (int i = 0; i < data.Length; i++)
                for (int j = 0; j < 3; j++)
                {
                    vertexArray[i * 3 + j] = new VertexDX(new Vector3(data[i].V[j].Position.X, data[i].V[j].Position.Y, data[i].V[j].Position.Z),
                                                          Color.FromArgb((int)(m_colorVariable.GetVector().W * 255), (int)(m_colorVariable.GetVector().Z * 255), (int)(m_colorVariable.GetVector().Y * 255), (int)(m_colorVariable.GetVector().X * 255)).ToArgb(), 
                                                          new Vector2(data[i].V[j].TexcoordU, data[i].V[j].TexcoordV),
                                                          new Vector3(data[i].V[j].Normal.X, data[i].V[j].Normal.Y, data[i].V[j].Normal.Z),
                                                          new Vector3(data[i].V[j].Tangent.X, data[i].V[j].Tangent.Y, data[i].V[j].Tangent.Z));

                }

            SlimDX.DataStream vertexStream = new SlimDX.DataStream(vertexArray.Length * Marshal.SizeOf(typeof(VertexDX)), true, true);//Unmanaged Vertex-Array erzeugen
            vertexStream.WriteRange(vertexArray);
            vertexStream.Position = 0;

            DX11.Buffer m_vertexBuffer = new DX11.Buffer(this.m_device, vertexStream,                     //Vertexpuffer anlegen (Daten werden vom Hauptspeicher in Grafikkarte kopiert)
                new DX11.BufferDescription()
                {
                    BindFlags = DX11.BindFlags.VertexBuffer,
                    CpuAccessFlags = DX11.CpuAccessFlags.None,
                    OptionFlags = DX11.ResourceOptionFlags.None,
                    SizeInBytes = vertexArray.Length * Marshal.SizeOf(typeof(VertexDX)),
                    Usage = DX11.ResourceUsage.Default //Default = GPU RW, Immutable = GPU RO, DYNAMIC (CPU WO, GPU RO), STAGING (CPU RW, GPU RW)
                });
            vertexStream.Close();

            return m_vertexBuffer;
        }

        private void SetBlendState(bool blendEnable, ColorWriteMaskFlags renderTargetWriteMask)
        {
            if (m_blendState != null) m_blendState.Dispose();
            m_blendState = BlendStateHelper.SetBlendState(m_device, blendEnable, renderTargetWriteMask);
            m_context.OutputMerger.BlendState = m_blendState;
        }

        private string PrintMatrix(SlimDX.Matrix matrix)
        {
            return String.Format("|{0:+0.00;-0.00; 0.00}\t", matrix.M11) + String.Format("{0:+0.00;-0.00; 0.00}\t", matrix.M12) + String.Format("{0:+0.00;-0.00; 0.00}\t", matrix.M13) + String.Format("{0:+0.00;-0.00; 0.00}|\n", matrix.M14) +
                   String.Format("|{0:+0.00;-0.00; 0.00}\t", matrix.M21) + String.Format("{0:+0.00;-0.00; 0.00}\t", matrix.M22) + String.Format("{0:+0.00;-0.00; 0.00}\t", matrix.M23) + String.Format("{0:+0.00;-0.00; 0.00}|\n", matrix.M24) +
                   String.Format("|{0:+0.00;-0.00; 0.00}\t", matrix.M31) + String.Format("{0:+0.00;-0.00; 0.00}\t", matrix.M32) + String.Format("{0:+0.00;-0.00; 0.00}\t", matrix.M33) + String.Format("{0:+0.00;-0.00; 0.00}|\n", matrix.M34) +
                   String.Format("|{0:+0.00;-0.00; 0.00}\t", matrix.M41) + String.Format("{0:+0.00;-0.00; 0.00}\t", matrix.M42) + String.Format("{0:+0.00;-0.00; 0.00}\t", matrix.M43) + String.Format("{0:+0.00;-0.00; 0.00}|\n", matrix.M44);
        }

        private SlimDX.Matrix TransformMatrixToSlimdx(Matrix4x4 matrix)
        {
            var m = matrix.Values;
            return new SlimDX.Matrix()
            {
                M11 = m[0], M12 = m[1],  M13 = m[2],  M14 = m[3],
                M21 = m[4], M22 = m[5],  M23 = m[6],  M24 = m[7],
                M31 = m[8], M32 = m[9],  M33 = m[10], M34 = m[11],
                M41 = m[12],M42 = m[13], M43 = m[14], M44 = m[15]
            };
        }

        #endregion

        #region IDisposable Member

        public void Dispose()
        {
            m_renderTargetActive = null;
            m_renderTargetDepthActive = null;

            if (m_device != null) m_device.Dispose();
            if (m_context != null) m_context.Dispose();
            if (m_swapChain != null) m_swapChain.Dispose();
            if (m_renderTarget != null) m_renderTarget.Dispose();
            if (m_renderTargetDepth != null) m_renderTargetDepth.Dispose();
            if (m_vertexLayout != null) m_vertexLayout.Dispose();
            if (vertexLayout2D != null) vertexLayout2D.Dispose();

            foreach (var tex in textures)
                tex.TextureView.Dispose();

            cubemaps.Dispose();

            foreach (var frame in framebuffers.Values)
            {
                frame.Dispose();
            }

            foreach (var vb in triangleArrays.Values)
            {
                vb.vertexBuffer.Dispose();
                vb.indexBuffer.Dispose();
            }
        }

        #endregion

        #region 2D
        public float ZValue2D {get;set; } = 0;
        public void DrawLine(Pen pen, Vector2D p1, Vector2D p2)
        {
            SetColor(pen.Color.R / 255f, pen.Color.G / 255f, pen.Color.B / 255f, pen.Color.A / 255f);
            SetLineWidth(pen.Width);
            DrawLineOrPoint(new Vector3[] { new Vector3(p1.X, p1.Y, this.ZValue2D), new Vector3(p2.X, p2.Y, this.ZValue2D) }, true);
        }

        public void DrawPixel(Vector2D pos, Color color, float size)
        {
            SetPointSize(size);
            SetColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
            DrawLineOrPoint(new Vector3[] { new Vector3(pos.X, pos.Y, this.ZValue2D) }, true);
        }

        private SizeF singleLetterSize = new SizeF(0, 0);
        public Size GetStringSize(float size, string text)
        {
            if (singleLetterSize.Width == 0)
            {
                System.Drawing.Bitmap bild = BitmapHelp.GetBitmapText("WWww", size, Color.Black, Color.White);
                Rectangle reci = BitmapHelp.SearchRectangleInBitmap(bild, Color.White);
                singleLetterSize = new SizeF(reci.Width / 4.0f / size, reci.Height / size);//4.. Länge des Textes "WWww"
            }

            return new Size((int)(singleLetterSize.Width * text.Length * size), (int)(singleLetterSize.Height * size * 1.3f));
        }

        public void DrawString(int x, int y, Color color, float size, string text)
        {
            string key = text + "_" + size + "_" + color.R + "_" + color.G + "_" + color.B;
            if (!texte.Keys.Contains(key))
            {
                texte.Add(key, CreateTextureFromBitmap(BitmapHelp.GetBitmapText(text, size, color, Color.Transparent)));
            }

            PushProjectionMatrix();

            SetProjectionMatrix2D();
            SetColor(1, 1, 1, 1);
            SetTexture(texte[key]);
            SetBlendingWithAlpha();
            EnableTexturemapping();

            SizeF sizef = GetStringSize(size, text);

            int id = GetTriangleArrayId(new GraphicGlobal.Triangle[] 
            { 
                new GraphicGlobal.Triangle( new Vertex(x, y, this.ZValue2D, 0, 0){Normal = new Vector3D(0,0,1) }, new Vertex(x, y + sizef.Height, this.ZValue2D, 0, 1){Normal = new Vector3D(0,0,1) }, new Vertex(x + sizef.Width, y, this.ZValue2D, 1, 0){Normal = new Vector3D(0,0,1) }) , 
                new GraphicGlobal.Triangle(new Vertex(x, y + sizef.Height, this.ZValue2D, 0, 1){Normal = new Vector3D(0,0,1) }, new Vertex(x + sizef.Width, y + sizef.Height, this.ZValue2D, 1, 1){Normal = new Vector3D(0,0,1) }, new Vertex(x + sizef.Width, y, this.ZValue2D, 1, 0){Normal = new Vector3D(0,0,1) })
            });
            DrawTriangleArray(id);
            RemoveTriangleArray(id);

            PopProjectionMatrix();
            DisableBlending();
        }

        public void DrawRectangle(Pen pen, int x, int y, int width, int height)
        {
            DrawLine(pen, new Vector2D(x, y), new Vector2D(x + width, y));
            DrawLine(pen, new Vector2D(x + width, y), new Vector2D(x + width, y + height));
            DrawLine(pen, new Vector2D(x, y + height), new Vector2D(x + width, y + height));
            DrawLine(pen, new Vector2D(x, y), new Vector2D(x, y + height));
        }

        public void DrawPolygon(Pen pen, List<Vector2D> points)
        {
            for (int i = 0; i < points.Count; i++)
                DrawLine(pen, points[i], points[(i + 1) % points.Count]);
        }

        public void DrawCircle(Pen pen, Vector2D pos, int radius)
        {
            SetColor(pen.Color.R / 255f, pen.Color.G / 255f, pen.Color.B / 255f, pen.Color.A / 255f);
            SetPointSize(pen.Width);

            int x, y, d, dx, dxy, px = (int)pos.X, py = (int)pos.Y;
            x = 0; y = radius; d = 1 - radius;
            dx = 3; dxy = -2 * radius + 5;
            while (y >= x)
            {
                DrawLineOrPoint(new Vector3[] {  new Vector3(px + x, py + y, this.ZValue2D),  // alle 8 Oktanden werden
                                                 new Vector3(px + y, py + x, this.ZValue2D),  // gleichzeitig gezeichnet
                                                 new Vector3(px + y, py - x, this.ZValue2D),
                                                 new Vector3(px + x, py - y, this.ZValue2D),
                                                 new Vector3(px - x, py - y, this.ZValue2D),
                                                 new Vector3(px - y, py - x, this.ZValue2D),
                                                 new Vector3(px - y, py + x, this.ZValue2D),
                                                 new Vector3(px - x, py + y, this.ZValue2D)}, true);

                if (d < 0) { d = d + dx; dx = dx + 2; dxy = dxy + 2; x++; }
                else { d = d + dxy; dx = dx + 2; dxy = dxy + 4; x++; y--; }
            }
        }

        public void DrawFillCircle(Color color, Vector2D pos, int radius)
        {
            SetColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
            SetPointSize(1);

            int x0 = (int)pos.X, y0 = (int)pos.Y;
            int x = radius;
            int y = 0;
            int xChange = 1 - (radius << 1);
            int yChange = 0;
            int radiusError = 0;

            while (x >= y)
            {
                for (int i = x0 - x; i <= x0 + x; i++)
                {
                    DrawLineOrPoint(new Vector3[] {  new Vector3(i, y0 + y + 1, this.ZValue2D), 
                                                     new Vector3(i, y0 - y - 1, this.ZValue2D), }, true);
                }
                for (int i = x0 - y; i <= x0 + y; i++)
                {
                    DrawLineOrPoint(new Vector3[] {  new Vector3(i, y0 + x + 1, this.ZValue2D), 
                                                     new Vector3(i, y0 - x - 1, this.ZValue2D), }, true);
                }

                y++;
                radiusError += yChange;
                yChange += 2;
                if (((radiusError << 1) + xChange) > 0)
                {
                    x--;
                    radiusError += xChange;
                    xChange += 2;
                }
            }
        }

        public void DrawCircleArc(Pen pen, Vector2D pos, int radius, float startAngle, float endAngle, bool withBorderLines)
        {
            CircleArcDrawer.DrawCircleArc(pos, radius, startAngle, endAngle, withBorderLines, (p) => DrawPixel(p, pen.Color, pen.Width));
        }
        public void DrawFillCircleArc(Color color, Vector2D pos, int radius, float startAngle, float endAngle)
        {
            SetPointSize(1);
            SetColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

            CircleArcDrawer.DrawFillCircleArc(pos, radius, startAngle, endAngle, (p) => DrawLineOrPoint(new Vector3[] { new Vector3(p.X, p.Y, this.ZValue2D) }, true));
        }

        public void DrawImage(int textureId, int x, int y, int width, int height, int sourceX, int sourceY, int sourceWidth, int sourceHeight, Color colorFactor)
        {
            Size tex = GetTextureSize(textureId);
            UseAlphaBlendingAndDiscardTransparent(colorFactor); // SetBlendingWithAlpha();
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            EnableTexturemapping();
            SetTexture(textureId);
            int id = GetTriangleArrayId(new GraphicGlobal.Triangle[] 
            { 
                new GraphicGlobal.Triangle(
                    new Vertex(x, y, this.ZValue2D, sourceX / (float)tex.Width, sourceY / (float)tex.Height){Normal = new Vector3D(0,0,1) }, 
                    new Vertex(x, y + height, this.ZValue2D, sourceX / (float)tex.Width, (sourceY + sourceHeight)/ (float)tex.Height){Normal = new Vector3D(0,0,1) }, 
                    new Vertex(x + width, y, this.ZValue2D, (sourceX + sourceWidth) / (float)tex.Width, sourceY / (float)tex.Height){Normal = new Vector3D(0,0,1) }),

                new GraphicGlobal.Triangle(
                    new Vertex(x, y + height, this.ZValue2D, sourceX / (float)tex.Width, (sourceY + sourceHeight) / (float)tex.Height){Normal = new Vector3D(0,0,1) }, 
                    new Vertex(x + width, y + height, this.ZValue2D, (sourceX + sourceWidth) / (float)tex.Width, (sourceY + sourceHeight)/ (float)tex.Height){Normal = new Vector3D(0,0,1) }, 
                    new Vertex(x + width, y, this.ZValue2D, (sourceX + sourceWidth) / (float)tex.Width, sourceY / (float)tex.Height){Normal = new Vector3D(0,0,1) })
            });
            DrawTriangleArray(id);
            RemoveTriangleArray(id);
            DisableTexturemapping();
        }

        public void DrawImage(int textureId, int x, int y, int width, int height, int sourceX, int sourceY, int sourceWidth, int sourceHeight, Color colorFactor, float zAngle, float yAngle)
        {
            PushMatrix();
            m_modelviewMatrix = SlimDX.Matrix.Translation(x, y, 0) * m_modelviewMatrix;
            m_modelviewMatrix = SlimDX.Matrix.RotationZ((float)(zAngle * Math.PI / 180.0f)) * m_modelviewMatrix;
            m_modelviewMatrix = SlimDX.Matrix.RotationY((float)(yAngle * Math.PI / 180.0f)) * m_modelviewMatrix;
            UpdateModelViewMatrix();
            DrawImage(textureId, -width / 2, -height / 2, width, height, sourceX, sourceY, sourceWidth, sourceHeight, colorFactor);
            PopMatrix();
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor)
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor); // SetBlendingWithAlpha();
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            EnableTexturemapping();
            SetTexture(textureId);
            int id = GetTriangleArrayId(new GraphicGlobal.Triangle[] 
            { 
                new GraphicGlobal.Triangle(new Vertex(x, y, this.ZValue2D, 0, 0){Normal = new Vector3D(0,0,1) }, new Vertex(x, y + height, this.ZValue2D, 0, 1){Normal = new Vector3D(0,0,1) }, new Vertex(x + width, y, this.ZValue2D, 1, 0){Normal = new Vector3D(0,0,1) }),
                new GraphicGlobal.Triangle(new Vertex(x, y + height, this.ZValue2D, 0, 1){Normal = new Vector3D(0,0,1) }, new Vertex(x + width, y + height, this.ZValue2D, 1, 1){Normal = new Vector3D(0,0,1) }, new Vertex(x + width, y, this.ZValue2D, 1, 0){Normal = new Vector3D(0,0,1) })
            });
            DrawTriangleArray(id);
            RemoveTriangleArray(id);
            DisableTexturemapping();
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor, float angle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            PushMatrix();
            m_modelviewMatrix = SlimDX.Matrix.Translation(x, y, 0) * m_modelviewMatrix;
            m_modelviewMatrix = SlimDX.Matrix.RotationZ((float)(angle * Math.PI / 180.0f)) * m_modelviewMatrix;
            UpdateModelViewMatrix();
            DrawFillRectangle(textureId, -width / 2, -height / 2, width, height, colorFactor);
            PopMatrix();
        }

        public void DrawFillRectangle(int textureId, float x, float y, float width, float height, Color colorFactor, float zAngle, float yAngle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            PushMatrix();
            m_modelviewMatrix = SlimDX.Matrix.Translation(x, y, 0) * m_modelviewMatrix;
            m_modelviewMatrix = SlimDX.Matrix.RotationZ((float)(zAngle * Math.PI / 180.0f)) * m_modelviewMatrix;
            m_modelviewMatrix = SlimDX.Matrix.RotationY((float)(yAngle * Math.PI / 180.0f)) * m_modelviewMatrix;
            UpdateModelViewMatrix();
            DrawFillRectangle(textureId, -width / 2, -height / 2, width, height, colorFactor);
            PopMatrix();
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height)
        {
            //m_effect.GetConstantBufferByName("ConstantBufferTexture").GetMemberByName("Transparentcolor").AsVector().Set(new Vector3(2,2,2)); //Es gibt keine Farbe, die Transparent ist
            SetColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
            int id = GetTriangleArrayId(new GraphicGlobal.Triangle[] 
            { 
                new GraphicGlobal.Triangle(new Vertex(x, y, this.ZValue2D, 0, 0){Normal = new Vector3D(0,0,1) }, new Vertex(x, y + height, this.ZValue2D, 0, 1){Normal = new Vector3D(0,0,1) }, new Vertex(x + width, y, this.ZValue2D, 1, 0){Normal = new Vector3D(0,0,1) }), 
                new GraphicGlobal.Triangle(new Vertex(x, y + height, this.ZValue2D, 0, 1){Normal = new Vector3D(0,0,1) }, new Vertex(x + width, y + height, this.ZValue2D, 1, 1){Normal = new Vector3D(0,0,1) }, new Vertex(x + width, y, this.ZValue2D, 1, 0){Normal = new Vector3D(0,0,1) })
            });
            DrawTriangleArray(id);
            RemoveTriangleArray(id);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float angle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            PushMatrix();
            m_modelviewMatrix = SlimDX.Matrix.Translation(x, y, 0) * m_modelviewMatrix;
            m_modelviewMatrix = SlimDX.Matrix.RotationZ((float)(angle * Math.PI / 180.0f)) * m_modelviewMatrix;
            UpdateModelViewMatrix();
            DrawFillRectangle(color, -width / 2, -height / 2, width, height);
            PopMatrix();
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float zAngle, float yAngle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            PushMatrix();
            m_modelviewMatrix = SlimDX.Matrix.Translation(x, y, 0) * m_modelviewMatrix;
            m_modelviewMatrix = SlimDX.Matrix.RotationZ((float)(zAngle * Math.PI / 180.0f)) * m_modelviewMatrix;
            m_modelviewMatrix = SlimDX.Matrix.RotationY((float)(yAngle * Math.PI / 180.0f)) * m_modelviewMatrix;
            UpdateModelViewMatrix();
            DrawFillRectangle(color, -width / 2, -height / 2, width, height);
            PopMatrix();
        }

        public void DrawFillPolygon(int textureId, Color colorFactor, List<Triangle2D> triangleList)
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor); // SetBlendingWithAlpha();
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);
            EnableTexturemapping();
            foreach (Triangle2D triangle in triangleList)
            {
                SetTexture(textureId);
                int id = GetTriangleArrayId(new GraphicGlobal.Triangle[] { new GraphicGlobal.Triangle( 
                    new Vertex(triangle.P1.Position.X, triangle.P1.Position.Y, this.ZValue2D, triangle.P1.Textcoord.X, triangle.P1.Textcoord.Y){Normal = new Vector3D(0,0,1) }, 
                    new Vertex(triangle.P2.Position.X, triangle.P2.Position.Y, this.ZValue2D, triangle.P2.Textcoord.X, triangle.P2.Textcoord.Y){Normal = new Vector3D(0,0,1) }, 
                    new Vertex(triangle.P3.Position.X, triangle.P3.Position.Y, this.ZValue2D, triangle.P3.Textcoord.X, triangle.P3.Textcoord.Y){Normal = new Vector3D(0,0,1) })
                });
                DrawTriangleArray(id);
                RemoveTriangleArray(id);
            }
            DisableTexturemapping();
        }

        public void DrawFillPolygon(Color color, List<Triangle2D> triangleList)
        {
            SetColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

            foreach (Triangle2D triangle in triangleList)
            {
                int id = GetTriangleArrayId(new GraphicGlobal.Triangle[] { new GraphicGlobal.Triangle(
                    new Vertex(triangle.P1.Position.X, triangle.P1.Position.Y, this.ZValue2D, triangle.P1.Textcoord.X, triangle.P1.Textcoord.Y){Normal = new Vector3D(0,0,1) }, 
                    new Vertex(triangle.P2.Position.X, triangle.P2.Position.Y, this.ZValue2D, triangle.P2.Textcoord.X, triangle.P2.Textcoord.Y){Normal = new Vector3D(0,0,1) }, 
                    new Vertex(triangle.P3.Position.X, triangle.P3.Position.Y, this.ZValue2D, triangle.P3.Textcoord.X, triangle.P3.Textcoord.Y){Normal = new Vector3D(0,0,1) })
                });
                DrawTriangleArray(id);
                RemoveTriangleArray(id);
            }
        }

        public void DrawSprite(int textureId, int xCount, int yCount, int xBild, int yBild, int x, int y, int width, int height, Color colorFactor)
        {
            UseAlphaBlendingAndDiscardTransparent(colorFactor); // SetBlendingWithAlpha();
            SetColor(colorFactor.R / 255f, colorFactor.G / 255f, colorFactor.B / 255f, colorFactor.A / 255f);

            float xf = 1.0f / xCount, yf = 1.0f / yCount;
            EnableTexturemapping();
            SetTexture(textureId);
            int id = GetTriangleArrayId(new GraphicGlobal.Triangle[] { 
                new GraphicGlobal.Triangle(
                    new Vertex(x, y, this.ZValue2D, xBild * xf + 0.01f, yBild * yf + 0.01f){Normal = new Vector3D(0,0,1) },
                    new Vertex(x, y + height, this.ZValue2D, xBild * xf + 0.01f, (yBild + 1)* yf - 0.01f){Normal = new Vector3D(0,0,1) },
                    new Vertex(x + width, y, this.ZValue2D, (xBild+1) * xf - 0.01f, yBild * yf + 0.01f){Normal = new Vector3D(0,0,1) }),
                new GraphicGlobal.Triangle(
                    new Vertex(x, y + height, this.ZValue2D, xBild * xf + 0.01f, (yBild+1) * yf - 0.01f){Normal = new Vector3D(0,0,1) },
                    new Vertex(x + width, y + height, this.ZValue2D, (xBild+1) * xf - 0.01f, (yBild+1) * yf - 0.01f){Normal = new Vector3D(0,0,1) },
                    new Vertex(x + width, y, this.ZValue2D, (xBild+1) * xf - 0.01f, yBild * yf + 0.01f){Normal = new Vector3D(0,0,1) })
            });
            DrawTriangleArray(id);
            RemoveTriangleArray(id);
            DisableTexturemapping();
        }
        

        public void EnableScissorTesting(int x, int y, int width, int height)
        {
            m_context.Rasterizer.State = RasterizerStateHelper.SetIsScissorEnabled(m_device, m_context.Rasterizer.State, true);
            m_context.Rasterizer.SetScissorRectangles(new Rectangle(x, y, width, height));
        }

        public void DisableScissorTesting()
        {
            m_context.Rasterizer.State = RasterizerStateHelper.SetIsScissorEnabled(m_device, m_context.Rasterizer.State, true);	 // Disable Scissor Testing
            m_context.Rasterizer.SetScissorRectangles(new Rectangle(0, 0, Width, Height));
        }

        #endregion
    }

    //Da meine Klasse so heißt wie der Namespace, habe ich innerhalb der Klasse kein Zugriff auf Properties
    class Resources
    {
        public static string DisplacementMapping
        {
            get
            {
                return Properties.Resources.DisplacementMapping;
            }
        }
        public static string LinesAndPoints
        {
            get
            {
                return Properties.Resources.LinesAndPoints;
            }
        }
        public static string ParallaxMapping
        {
            get
            {
                return Properties.Resources.ParallaxMapping;
            }
        }
        public static string ShadowmapCreation
        {
            get
            {
                return Properties.Resources.ShadowmapCreation;
            }
        }
        public static string SimpleRendering
        {
            get
            {
                return Properties.Resources.SimpleRendering;
            }
        }
        public static string Variablen
        {
            get
            {
                return Properties.Resources.Variablen;
            }
        }
    }
}
