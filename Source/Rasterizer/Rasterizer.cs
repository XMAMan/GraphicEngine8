using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using System.Drawing;
using GraphicGlobal;

namespace Rasterizer
{
    public class Rasterizer : DrawingPanelSynchron, IDrawingPanel, IDrawing3D, IDrawing2D
    {
        private Dictionary<DrawingObject, RasterizerDrawingObject> drawingObjectCache = new Dictionary<DrawingObject, RasterizerDrawingObject>();
        private Dictionary<RasterizerLightSourceDescription, int> shadowmapCache = new Dictionary<RasterizerLightSourceDescription, int>();//Lichtquellen werden wegen der Shadowmap-Id gecacht

        public Rasterizer(IGraphicPipeline pipeline)
            :base(pipeline)
        {
        }

        private RasterizerDrawingObject GetDrawingObjectFromCache(DrawingObject key)
        {
            if (this.drawingObjectCache.ContainsKey(key) == false)
                this.drawingObjectCache.Add(key, new RasterizerDrawingObject(key, this.pipeline));

            return this.drawingObjectCache[key];
        }

        private int GetShadowMapIdFromCache(RasterizerLightSourceDescription key)
        {
            if (this.shadowmapCache.ContainsKey(key) == false) 
                this.shadowmapCache.Add(key, this.pipeline.CreateShadowmap(1024 / 4, 1024 / 4));

            return this.shadowmapCache[key];
        }

        public override void Draw3DObjects(Frame3DData data)
        {
            var drawingObjects = data.DrawingObjects.Where(x => (x.DrawingProps.RaytracingLightSource is EnvironmentLightDescription) == false).Select(x => GetDrawingObjectFromCache(x)).ToList();
            var globalProps = data.GlobalObjektPropertys as IRasterizerGlobalDrawingProps;

            this.pipeline.ClearDepthAndStencilBuffer();
            this.pipeline.SetModelViewMatrixToCamera(globalProps.Camera);
            this.pipeline.SetProjectionMatrix3D(this.Width, this.Height, globalProps.Camera.OpeningAngleY, globalProps.Camera.zNear, globalProps.Camera.zFar);
            if (globalProps.UseFrustumCulling) FrustumCulling.CalculateFrustumPlanes(this.pipeline.GetProjectionMatrix(), this.pipeline.GetModelViewMatrix());

            this.pipeline.Time = globalProps.Time;

            //Licht
            List<LightSource> lightSources = drawingObjects
                .Where(x => x.DrawingProps.RasterizerLightSource != null)
                .Select(x => new LightSource(
                    x.DrawingProps,
                    globalProps.ShadowsForRasterizer == RasterizerShadowMode.Shadowmap ? GetShadowMapIdFromCache(x.DrawingProps.RasterizerLightSource) : -1
                    ))
                .ToList();
            if (lightSources.Any()) this.pipeline.SetPositionOfAllLightsources(lightSources.Cast< RasterizerLightsource>().ToList()); //Die aktuelle ModelView-Matrix ist egal bei der Lichtangabe. Es werden die Weltkoordinaten aus der Light-Struktur verwendet

            if (globalProps.ShadowsForRasterizer == RasterizerShadowMode.Shadowmap)
            {
                CreateShadowMappingTextures(globalProps, drawingObjects, lightSources);
            }
 
            //Damit wird gesagt, ob im PixelShader der Pixel-Tiefenwert mit dem Shadowmap-Pixel verglichen werden soll
            this.pipeline.ReadFromShadowmap = globalProps.ShadowsForRasterizer == RasterizerShadowMode.Shadowmap;

            //Zeichne alles außer Spiegel
            DrawWithoutMirror(globalProps, drawingObjects, lightSources, null, null); //Alles außer Spiegel

            //this.pipeline.GetStencilTestImage().Save("..\\Stencil.bmp"); //Wenn ich mit Stencilschatten arbeite, sehe ich so ob der Stencilpuffer ok aussieht

            //Zeichne Spiegelflächen
            foreach (var obj in drawingObjects)
            {
                if (obj.DrawingProps.IsMirrorPlane)
                {
                    FlatMirror.DrawMirrorObject(globalProps, this.pipeline, lightSources, obj, (plane) => DrawWithoutMirror(globalProps, drawingObjects, lightSources, plane, null));
                }
                else if (obj.DrawingProps.UseCubemap)
                {
                    CubemappedMirror.DrawMirrorObject(globalProps, this.pipeline, lightSources, obj, () => DrawWithoutMirror(globalProps, drawingObjects, lightSources, null, null));
                }
            }
        }

        private void DrawWithoutMirror(IRasterizerGlobalDrawingProps globalProps, IEnumerable<RasterizerDrawingObject> drawingObjects, List<LightSource> lightsources, Plane mirrorPlane, LightSource excludedLight)
        {
            foreach (var obj in drawingObjects)
                if (obj.DrawingProps.IsMirrorPlane == false && obj.DrawingProps.UseCubemap == false && (excludedLight == null || obj.DrawingProps != excludedLight.DrawingProps))
                {
                    obj.Draw(lightsources.Any() ? lightsources : null, mirrorPlane, globalProps);
                    if (obj.DrawingProps.HasSilhouette) obj.DrawSiolette(5, globalProps.Camera.Position, 1, 0, 0);
                }
        }

        private void CreateShadowMappingTextures(IRasterizerGlobalDrawingProps globalProps, IEnumerable<RasterizerDrawingObject> drawingObjects, List<LightSource> lightsources)
        {
            if (lightsources.Count == 0) return;

            bool somethingWasDone = false;
            foreach (var light in lightsources)
            {
                if (light.CreateShadows)
                {
                    somethingWasDone = true;

                    this.pipeline.EnableRenderToShadowmap(light.ShadowMapId);

                    ClearScreen(Color.White);
                    this.pipeline.DisableWritingToTheColorBuffer();

                    DrawWithoutMirror(globalProps, drawingObjects, lightsources, null, light); //Zeichne Scene aus Sicht der Lichtquelle

                    this.pipeline.EnableWritingToTheColorBuffer();
                    this.pipeline.DisableRenderToShadowmapTexture();


                    //So sieht die Scene aus Sicht der Lichtquelle 'light' aus
                    //this.pipeline.GetShadowmapAsBitmap(light.ShadowMapId).Save(@"..\DepthTexture.bmp");
                }
            }

            if (somethingWasDone)
            {
                this.pipeline.ClearDepthAndStencilBuffer();
            }
        }

        public override DrawingObject MouseHitTest(Frame3DData data, Point mousePosition)
        {
            var drawingObjects = data.DrawingObjects.Select(x => GetDrawingObjectFromCache(x)).ToList();
            var globalProps = data.GlobalObjektPropertys as IRasterizerGlobalDrawingProps;

            this.pipeline.ClearDepthAndStencilBuffer();
            this.pipeline.ClearColorBuffer(Color.Black);
            this.pipeline.PushProjectionMatrix();
            this.pipeline.SetProjectionMatrix3D(this.Width, this.Height, globalProps.Camera.OpeningAngleY, globalProps.Camera.zNear, globalProps.Camera.zFar);

            this.pipeline.PushMatrix();
            this.pipeline.SetModelViewMatrixToIdentity();
            this.pipeline.SetModelViewMatrixToCamera(globalProps.Camera);
            if (globalProps.UseFrustumCulling) FrustumCulling.CalculateFrustumPlanes(this.pipeline.GetProjectionMatrix(), this.pipeline.GetModelViewMatrix());

            this.pipeline.StartMouseHitTest(mousePosition);
            foreach (var obj in drawingObjects)
            {
                this.pipeline.AddObjektIdForMouseHitTest(obj.DrawingProps.Id);
                obj.Draw(null, null, globalProps);
            }

            this.pipeline.PopMatrix();
            this.pipeline.PopProjectionMatrix();

            int hitResult = this.pipeline.GetMouseHitTestResult();
            if (hitResult <= 0) return null;

            var rasterObj = drawingObjects.First(x => x.DrawingProps.Id == hitResult);
            
            foreach (KeyValuePair<DrawingObject, RasterizerDrawingObject> pair in this.drawingObjectCache)
                if (rasterObj.Equals(pair.Value)) return pair.Key;

            return null;
        }

        
    }
}
