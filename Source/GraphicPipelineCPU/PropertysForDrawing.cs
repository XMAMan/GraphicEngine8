using System.Collections.Generic;
using GraphicMinimal;
using System.Drawing;
using GraphicGlobal;
using System.Windows.Forms;
using GraphicPipelineCPU.Textures;
using GraphicPipelineCPU.PipelineHelper;
using GraphicPipelineCPU.DrawingHelper;

namespace GraphicPipelineCPU
{
    enum BlendingMode
    {
        WithBlackColor, WithAlpha
    }

    enum StencilFunction
    {
        WriteOneSideIncrease, WriteOneSideDecrease, ReadNotEqualZero
    }

    class PropertysForDrawing
    {
        //Wird nur in GraphicPipelineCPU verwendet
        public Framebuffer StandardBuffer = null;
        public Stack<Matrix4x4> ModelviewMatrixStack = new Stack<Matrix4x4>();
        public Stack<Matrix4x4> ProjectionMatrixStack = new Stack<Matrix4x4>();
        public TextureCollection Textures = new TextureCollection();
        public CubemappedFrameCollection Cubemaps = new CubemappedFrameCollection();
        public TriangleArrayCollection TriangleArrays = new TriangleArrayCollection();
        public FramebufferCollection Framebuffers = new FramebufferCollection();
        public ColorTextureDeck ActiveColorTextureDeck = null;

        //Nur für 3D
        public Matrix4x4 ModelviewMatrix = Matrix4x4.Ident(); //= ObjToWorldMatrix * CameraMatrix (Transformiert von Objekt in Eye-Space)
        public Matrix4x4 ProjectionMatrix = null;
        public Matrix4x4 CameraMatrix = Matrix4x4.Ident();
        public Vector3D CameraPosition = new Vector3D(0, 0, 0);
        public Matrix4x4 InverseCameraMatrix = Matrix4x4.Ident();
        public Matrix4x4 ShadowMatrix = Matrix4x4.Ident();
        public MouseHitTest MouseHit = new MouseHitTest();
        public bool WritingToDepthBuffer = true;
        public bool WritingToColorBuffer = true;        
        public bool CullFaceIsEnabled;
        public bool FrontFaceIsClockWise = false;
        public Matrix3x3 TextureMatrix = Matrix3x3.Ident();
        public Vector2D TexturScaleFaktor;
        public bool WireframeModeIsActive = false;
        public NormalSource NormalSource = NormalSource.ObjectData;
        public InterpolationMode NormalInterpolationMode = InterpolationMode.Flat;
        public bool DepthTestingIsEnabled;
        public bool StenciltestIsEnabled;
        public StencilFunction StencilFunction;        
        public ColorTextureDeck Deck0 = new ColorTextureDeck(); // Farbtextur
        public ColorTextureDeck Deck1 = new ColorTextureDeck(); // Bumpmaptextur
        public Cubemap CubemapTexture = null; // Cubemaptextur
        public DepthTexture ShadowDepthTexture = null; //ShadowMapping-Texture (Tiefenwerte)
        public float CurrentTextureHeighScaleFactor = 1;
        public float CurrentTesselationFactor = 1;
        public float LineWidth = 1;
        public float PointSize = 1;
        public List<RasterizerLightsource> Lights = new List<RasterizerLightsource>();
        public bool LightingIsEnabled;
        public float SpecularHighlightPowExponent;
        public bool ExplosionEffectIsEnabled = false;
        public float ExplosionsRadius = 1;
        public int Time = 0;
        public bool RenderToShadowTexture = false;
        public bool UseShadowmap = false;

        //Nur für 2D
        public Control DrawingArea;
        public bool IsScissorEnabled = false;
        public Rectangle ScissorRectangle;
        public float ZValue2D = 0; //Wenn ich DepthTesting bei 2D-Grafiken verwenden will
        public float ZNearOrtho, ZFarOrtho, ZValue2DTransformed; //Parameter von SetProjectionMatrix2D(..)
        public bool Discard100Transparent = false; //Sollen Pixel, die 100% Transparent sind verworfen werden? Wird benötigt, wenn man bei 2D-Grafiken DepthTesting + MakeFirstPixelTransparent nutzen will

        //2D + 3D
        public Framebuffer Buffer = null; //[x,y]    -> Dieser Puffer zeigt entweder auf 'StandardBuffer' oder auf ein Framebuffer
        public ViewPort ViewPort;
        public bool BlendingIsEnabled;
        public BlendingMode BlendingMode;

        //Damit ich das gleiche Bild wie DirectX/OpenGL erhalte muss ich interne Farben als Float ohne 0-1-Clipping darstellen
        //Bei ShadowsAndBlending erhält der Boden ein Farbwert von 1.2 und durch den Schattenfaktor von 0.5 wird daraus dann 0.6
        //Wenn ich Farben intern mit Color oder 0-1-geclippten Floats darstelle, dann wird 1.2 zu 1 geclippt und dann der Boden, wo
        //der Schatten hinfliegt wird zu 0.5 anstatt 0.6
        public Vector4D CurrentColor;
    }
}
