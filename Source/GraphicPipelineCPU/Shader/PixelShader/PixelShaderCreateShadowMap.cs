using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper;
using System.Drawing;

namespace GraphicPipelineCPU.Shader.PixelShader
{
    class PixelShaderCreateShadowMap
    {
        private PropertysForDrawing prop = null;

        public PixelShaderCreateShadowMap(PropertysForDrawing prop)
        {
            this.prop = prop;
        }

        public Color GetPixelColor(PixelShaderInput data)
        {
            Vector3D textCoord = data.PixelCenter.V.ReadVector3D();

            //Nur RGB-Farbe von colorMaterial
            Color noLightColor = prop.CurrentColor.ToColor();

            //Texturmapping
            if (prop.Deck0.IsEnabled) noLightColor = prop.Deck0.ReadTexelWithPointFilter(textCoord.X, textCoord.Y);

            if (prop.BlendingIsEnabled && prop.BlendingMode == BlendingMode.WithBlackColor && noLightColor.IsBlackColor()) return Color.Transparent;

            return noLightColor;
        }
    }
}
