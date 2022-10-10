using GraphicGlobal;
using GraphicMinimal;

namespace Rasterizer
{
    //Punktlichtquelle
    class LightSource : RasterizerLightsource
    {
        public IRasterizerDrawingProps DrawingProps { get; private set; } //Wird benötigt, um alle Objekte außer genau diese Lichtquelle in die ShadowMap zu rendern
        public int ShadowMapId { get; private set; } = -1;                // Hier wird eine Tiefentextur aus sicht der Lichtquelle gespeichert
        
        public LightSource(IRasterizerDrawingProps drawingProps, int shadowmapId)
            : base(drawingProps.RasterizerLightSource, drawingProps.Position)
        {
            this.DrawingProps = drawingProps;
            this.ShadowMapId = shadowmapId;
        }

        public LightSource(LightSource copy)
            : base(copy)
        {
            this.DrawingProps = copy.DrawingProps;
            this.ShadowMapId = copy.ShadowMapId;
        }
    }
}
