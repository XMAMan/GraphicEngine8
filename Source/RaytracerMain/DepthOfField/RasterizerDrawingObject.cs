using System.Linq;
using GraphicMinimal;
using BitmapHelper;
using GraphicGlobal;

namespace RaytracerMain
{
    //Für die Ausgabe des DepthOfField-Gitters zusammen mit den 3D-Objekten
    class RasterizerDrawingObject
    {
        public TriangleObject TriangleData { get; private set; }
        public IRasterizerDrawingProps DrawingProps { get; private set; }

        private IGraphicPipeline pipeline;
        private ColorTextureCache colorTextureCache;
        private int triangleArrayId = 0;

        public RasterizerDrawingObject(DrawingObject drawingObject, IGraphicPipeline pipeline)
        {
            this.TriangleData = drawingObject.TriangleData;
            this.DrawingProps = drawingObject.DrawingProps as IRasterizerDrawingProps;
            this.pipeline = pipeline;
            this.colorTextureCache = new ColorTextureCache(this.pipeline.GetTextureId);
        }

        public void Draw()
        {
            this.pipeline.NormalSource = NormalSource.ObjectData;
            this.pipeline.SetNormalInterpolationMode(InterpolationMode.Flat);

            this.pipeline.SetSpecularHighlightPowExponent(this.DrawingProps.SpecularHighlightPowExponent);
            DrawObjekt();
            this.pipeline.DisableExplosionEffect();
        }

        private Matrix4x4 GetObjectToWorldMatrix()
        {
            if (this.DrawingProps.HasBillboardEffect)
            {
                return Matrix4x4.BilboardMatrixFromCameraMatrix(this.DrawingProps.Position, this.DrawingProps.Orientation, this.DrawingProps.Size, this.pipeline.GetModelViewMatrix());
            }
            else
            {
                return pipeline.GetModelMatrix(this.DrawingProps.Position, this.DrawingProps.Orientation, this.DrawingProps.Size);
            }
        }

        private void DrawObjekt()
        {
            this.pipeline.PushMatrix();
            this.pipeline.MultMatrix(GetObjectToWorldMatrix());

            this.pipeline.SetActiveTexture0();
            this.pipeline.DisableLighting();

            //Textur
            var textureData = this.colorTextureCache.GetEntry(this.DrawingProps.Color);
            if (textureData is TextureEntry)
            {
                this.pipeline.SetColor(1, 1, 1, 1); //4. Wert = Alphawert welcher 1 sein muss, wenn es null durchsichtig sein soll
                this.pipeline.SetTexture((textureData as TextureEntry).TextureId);
                this.pipeline.EnableTexturemapping();

                var tex = this.DrawingProps.Color.As<ColorFromTexture>();
                this.pipeline.SetTextureFilter(tex.TextureFilter);
                this.pipeline.SetTextureMode(tex.TextureMode);

                this.pipeline.SetTextureMatrix(tex.TextureMatrix);
                this.pipeline.SetTextureScale(Matrix3x3.GetTexturScale(tex.TextureMatrix));
            }
            else //Farbfestlegung über glColor wenn keine Texture angegeben
            {
                this.pipeline.DisableTexturemapping();
                var color = (textureData as ColorEntry).Color;
                this.pipeline.SetColor(color[0], color[1], color[2], color[3]);
            }

            DrawObjectWithoutShadows();

            this.pipeline.DisableBlending();
            this.pipeline.PopMatrix();
        }

        private void DrawObjectWithoutShadows()
        {
            if (this.DrawingProps.ShowFromTwoSides == false)
                this.pipeline.EnableCullFace();
            else
                this.pipeline.DisableCullFace();

            this.pipeline.EnableDepthTesting();

            if (this.triangleArrayId == 0)
                this.triangleArrayId = this.pipeline.GetTriangleArrayId(this.TriangleData.Triangles.ToArray());

            this.pipeline.DrawTriangleArray(this.triangleArrayId);
        }

        public override string ToString()
        {
            return this.TriangleData.Name;
        }
    }
}
