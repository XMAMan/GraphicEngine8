using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper.Helper3D;
using System.Drawing;

namespace GraphicPipelineCPU.Textures
{
    class ColorTextureDeck
    {
        public bool IsEnabled = false;
        public ColorTexture Texture = null;
        public TextureFilter TextureFilter = TextureFilter.Point;
        public TextureMode TextureMode = TextureMode.Repeat;

        public Color ReadTexelWithPointFilter(float texcoordU, float texcoordV)
        {
            return this.Texture.TextureMappingPoint(this.TextureMode, texcoordU, texcoordV);
        }

        public Color ReadTexelWithLinearFilter(float texcoordU, float texcoordV)
        {
            return this.Texture.TextureMappingLinear(this.TextureMode, texcoordU, texcoordV);
        }

        public Color ReadTexel(Vector2D point, Footprint texelPos)
        {
            return this.Texture.TextureMapping(this.TextureMode, point, texelPos, this.TextureFilter);
        }
    }
}
