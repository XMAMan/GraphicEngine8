using GraphicPipelineCPU.Textures;
using System.Collections.Generic;

namespace GraphicPipelineCPU.PipelineHelper
{
    class FramebufferCollection
    {
        private Dictionary<int, Framebuffer> framebuffers = new Dictionary<int, Framebuffer>();

        public Framebuffer this[int id]
        {
            get
            {
                return this.framebuffers[id];
            }
        }

        public int AddFramebuffer(ColorTexture color, DepthTexture depth)
        {
            int id = this.framebuffers.Count + 1;

            this.framebuffers.Add(id, new Framebuffer(color, depth));

            return id;
        }
    }
}
