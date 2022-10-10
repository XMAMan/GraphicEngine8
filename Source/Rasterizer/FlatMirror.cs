using System.Collections.Generic;
using GraphicMinimal;
using GraphicGlobal;
using System;

namespace Rasterizer
{
    //Ein flacher Spiegel, welcher mit einer reflektierten Modelviewmatrix arbeitet
    class FlatMirror
    {
        private Plane MirrorPlane;     // Spiegel-Ebene in Weltkoordinaten
        private IRasterizerGlobalDrawingProps globalProps;
        private IGraphicPipeline pipeline;
        private RasterizerDrawingObject drawingObject;

        public static void DrawMirrorObject(IRasterizerGlobalDrawingProps globalProps, IGraphicPipeline pipeline, List<LightSource> lightSources, RasterizerDrawingObject mirrorObject, Action<Plane> drawSceneWithoutMirros)
        {
            FlatMirror mirror = new FlatMirror(globalProps, pipeline, mirrorObject);
            mirror.PreDraw(); //Zeichne im Stencilpuffer den Bereich, wo gezeichnet werden darf
            drawSceneWithoutMirros(mirror.MirrorPlane);
            mirror.PostDraw(lightSources); //Zeichne bläuliches Viereck per Blending über Spiegelfläche
        }

        private FlatMirror(IRasterizerGlobalDrawingProps globalProps, IGraphicPipeline pipeline, RasterizerDrawingObject drawingObject)
        {
            this.globalProps = globalProps;
            this.pipeline = pipeline;
            this.MirrorPlane = GetMirrorPlane(drawingObject);
            this.drawingObject = drawingObject;
        }

        private void PreDraw()
        {
            //Erzeuge im Stencilpuffer ein Bereich, in dem nur gezeichnet werden darf
            this.pipeline.EnableStencilTest();
            this.pipeline.ClearStencilBuffer();
            this.pipeline.SetStencilWrite_Increase();
            this.pipeline.DisableWritingToTheColorBuffer();
            this.pipeline.DisableWritingToTheDepthBuffer();
            this.drawingObject.DrawingProps.Opacity = 0;
            this.drawingObject.Draw(null, null, this.globalProps);//Zeichne Spiegel (Viereck)
            this.pipeline.EnableWritingToTheDepthBuffer();
            this.pipeline.EnableWritingToTheColorBuffer();

            //Zeichne in den Bereich, wo der Spiegel ist
            this.pipeline.SetStencilRead_NotEqualZero();
        }

        private void PostDraw(List<LightSource> lightSources)
        {
            pipeline.DisableStencilTest();

            this.drawingObject.DrawingProps.Opacity = 0.2f;
            this.drawingObject.Draw(lightSources, null, this.globalProps);//Zeichne Spiegel (Viereck)
        }

        //Es wird das 1. Dreieck in Weltkoordinaten umgerechnet und das als MirrorPlane (Verändert die Modelviewmatrix) genommen
        private static Plane GetMirrorPlane(RasterizerDrawingObject drawingObject)
        {
            Matrix4x4 mv_obj_to_world = Matrix4x4.Model(drawingObject.DrawingProps.Position, drawingObject.DrawingProps.Orientation, drawingObject.DrawingProps.Size);
            
            //Nimm das erste Dreieck um die Spiegel-Ebene aufzuspannen
            Vector3D p1 = Matrix4x4.MultPosition(mv_obj_to_world, drawingObject.TriangleData.Triangles[0].V[0].Position);
            Vector3D p2 = Matrix4x4.MultPosition(mv_obj_to_world, drawingObject.TriangleData.Triangles[0].V[1].Position);
            Vector3D p3 = Matrix4x4.MultPosition(mv_obj_to_world, drawingObject.TriangleData.Triangles[0].V[2].Position);

            return new Plane(p1, p2, p3);
        }
    }
}
