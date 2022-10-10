using System.Drawing;

namespace GraphicPipelineCPU.Textures
{
    //Hat mehrere Texturen. Befinded sich am Ende der GraphicPipeline
    class Framebuffer
    {
        public const float ClearValueForDepth = 1.0f;
        private const byte ClearValueForStencil = 0;

        public ColorTexture Color { get; private set; }
        public DepthTexture Depth { get; private set; }
        public StencilTexture Stencil { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Framebuffer(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Color = new ColorTexture(-1, width, height);
            this.Depth = new DepthTexture(-1, width, height);
            this.Stencil = new StencilTexture(-1, width, height);
        }

        public Framebuffer(ColorTexture color, DepthTexture depth)
        {
            this.Width = color != null ? color.Width : depth.Width;
            this.Height = color != null ? color.Height : depth.Height;
            this.Color = color;
            this.Depth = depth;
            this.Stencil = new StencilTexture(-1, this.Width, this.Height);
        }

        public void UpdateColorTexture(ColorTexture texture)
        {
            this.Color = texture;
        }

        public void ClearColorBuffer(Color clearColor)
        {
            this.Color.SetForEachTexel(clearColor);
        }

        public void ClearColorDepthAndStencilBuffer(Color clearColor)
        {
            if (this.Color != null) this.Color.SetForEachTexel(clearColor);
            if (this.Depth != null) this.Depth.SetForEachTexel(ClearValueForDepth);
            if (this.Stencil != null) this.Stencil.SetForEachTexel(ClearValueForStencil);
        }

        public void ClearDepthAndStencilBuffer()
        {
            if (this.Depth != null) this.Depth.SetForEachTexel(ClearValueForDepth);
            if (this.Stencil != null) this.Stencil.SetForEachTexel(ClearValueForStencil);
        }

        public void ClearStencilBuffer()
        {
            this.Stencil.SetForEachTexel(ClearValueForStencil);
        }
    }
}
