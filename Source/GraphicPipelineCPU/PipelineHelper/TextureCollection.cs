using GraphicPipelineCPU.Textures;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GraphicPipelineCPU.PipelineHelper
{
    class TextureCollection
    {
        private Dictionary<int, ITexture2D> textures = new Dictionary<int, ITexture2D>();

        private int GetNewTextureId()
        {
            return this.textures.Any() ? this.textures.Keys.Max() + 1 : 1;
        }

        public ITexture2D this[int textureId]
        {
            get
            {
                return this.textures[textureId];
            }
            set
            {
                this.textures[textureId] = value;
            }
        }

        public ColorTexture GetColorTexture(int textureId)
        {
            return (ColorTexture)this.textures[textureId];
        }

        public ColorTexture AddColorTexture(Bitmap image)
        {
            var color = new ColorTexture(GetNewTextureId(), image);
            this.textures.Add(color.Id, color);
            return color;
        }

        public ColorTexture AddEmptyColorTexture(int width, int height)
        {
            var color = new ColorTexture(GetNewTextureId(), width, height); //Farbtextur bekommt ID
            this.textures.Add(color.Id, color);
            return color;
        }

        public DepthTexture AddEmptyDepthTexture(int width, int height)
        {
            var depth = new DepthTexture(GetNewTextureId(), width, height);
            this.textures.Add(depth.Id, depth);
            return depth;
        }
    }
}
