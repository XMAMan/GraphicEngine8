using System.Drawing;

namespace GraphicPipelineCPU.Textures
{
    class CubemappedFrame
    {
        private Framebuffer framebuffer;
        public Cubemap Cubemap { get; private set; }

        public CubemappedFrame(int width, int height)
        {
            this.framebuffer = new Framebuffer(width, height);
            this.Cubemap = new Cubemap(width, height);
        }

        public Framebuffer GetFramebufferFromSide(int side)
        {
            this.framebuffer.UpdateColorTexture(this.Cubemap.Color[side]);
            return this.framebuffer;
        }

        public Bitmap GetColorDataFromCubeMapSide(int side)
        {
            return this.Cubemap.Color[side].GetAsBitmap();
        }
    }
}
