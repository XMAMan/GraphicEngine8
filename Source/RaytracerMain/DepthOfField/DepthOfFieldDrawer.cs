using GraphicGlobal;
using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RaytracerMain
{
    //Benutzt ein Rasterizer um das DepthOfField-Grid auszugeben
    public class DepthOfFieldDrawer : DrawingPanelSynchron, IDrawingPanel
    {
        private Dictionary<DrawingObject, RasterizerDrawingObject> drawingObjectCache = new Dictionary<DrawingObject, RasterizerDrawingObject>();

        public DepthOfFieldDrawer(IGraphicPipeline pipeline)
            : base(pipeline)
        {
        }

        private RasterizerDrawingObject GetDrawingObjectFromCache(DrawingObject key)
        {
            if (this.drawingObjectCache.ContainsKey(key) == false) this.drawingObjectCache.Add(key, new RasterizerDrawingObject(key, this.pipeline));
            return this.drawingObjectCache[key];
        }
        public override void Draw3DObjects(Frame3DData data)
        {
            var drawingObjects = data.DrawingObjects.Select(x => GetDrawingObjectFromCache(x));
            var globalProps = data.GlobalObjektPropertys as IRaytracerGlobalDrawingProps;


            this.pipeline.ClearDepthAndStencilBuffer();
            this.pipeline.SetModelViewMatrixToCamera(globalProps.Camera);
            this.pipeline.SetProjectionMatrix3D(this.Width, this.Height, globalProps.Camera.OpeningAngleY, globalProps.Camera.zNear, globalProps.Camera.zFar);

            foreach (var obj in drawingObjects) obj.Draw();

            DrawDepthOfFieldForRaytracerTest(globalProps);
        }

        private void DrawDepthOfFieldForRaytracerTest(IRaytracerGlobalDrawingProps globalProps)
        {
            //Draw DepthOfField
            if (globalProps.DepthOfFieldIsEnabled)
            {
                this.pipeline.SetModelViewMatrixToIdentity();

                this.pipeline.DisableTexturemapping();
                this.pipeline.SetColor(0, 0, 1, 0);
                this.pipeline.SetLineWidth(5);
                float h = (float)(Math.Tan(globalProps.Camera.OpeningAngleY * Math.PI / 180 / 2) * globalProps.DistanceDephtOfFieldPlane); //Halbe Bildschirmhöhe
                float size = h / 16 * 0.9f;
                for (int i = -16; i <= 16; i += 2)
                {
                    this.pipeline.DrawLine(new Vector3D(i * size, -16 * size, -globalProps.DistanceDephtOfFieldPlane), new Vector3D(i * size, 16 * size, -globalProps.DistanceDephtOfFieldPlane));
                    this.pipeline.DrawLine(new Vector3D(-16 * size, i * size, -globalProps.DistanceDephtOfFieldPlane), new Vector3D(16 * size, i * size, -globalProps.DistanceDephtOfFieldPlane));
                }
                this.pipeline.SetLineWidth(2);
                this.pipeline.SetColor(0, 1, 0, 0);
                for (int i = -10; i <= 10; i += 2)
                {
                    this.pipeline.DrawLine(new Vector3D(i * size, -10 * size, -globalProps.DistanceDephtOfFieldPlane + globalProps.WidthDephtOfField * size), new Vector3D(i * size, 10 * size, -globalProps.DistanceDephtOfFieldPlane + globalProps.WidthDephtOfField * size));
                    this.pipeline.DrawLine(new Vector3D(-10 * size, i * size, -globalProps.DistanceDephtOfFieldPlane + globalProps.WidthDephtOfField * size), new Vector3D(10 * size, i * size, -globalProps.DistanceDephtOfFieldPlane + globalProps.WidthDephtOfField * size));
                    this.pipeline.DrawLine(new Vector3D(i * size, -10 * size, -globalProps.DistanceDephtOfFieldPlane - globalProps.WidthDephtOfField * size), new Vector3D(i * size, 10 * size, -globalProps.DistanceDephtOfFieldPlane - globalProps.WidthDephtOfField * size));
                    this.pipeline.DrawLine(new Vector3D(-10 * size, i * size, -globalProps.DistanceDephtOfFieldPlane - globalProps.WidthDephtOfField * size), new Vector3D(10 * size, i * size, -globalProps.DistanceDephtOfFieldPlane - globalProps.WidthDephtOfField * size));
                }
            }
        }

        public override DrawingObject MouseHitTest(Frame3DData data, Point mousePosition)
        {
            return null;
        }
    }
}
