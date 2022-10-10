using GraphicGlobal;
using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Rasterizer
{
    class CubemappedMirror
    {
        public static void DrawMirrorObject(IRasterizerGlobalDrawingProps globalProps, IGraphicPipeline pipeline, List<LightSource> lightSources, RasterizerDrawingObject mirrorObject, Action drawSceneWithoutMirros)
        {
            //Schaue vom Mirror-Objekt aus in alle 6 Würfelrichtungen und rendere die Scene in ein Bitmap
            Camera[] cameras = GetCubemapCameras(mirrorObject.DrawingProps.Position, mirrorObject.DrawingProps.Orientation, mirrorObject.DrawingProps.Size);
            Bitmap[] cubemapImages = new Bitmap[6];
            for (int i = 0; i < 6; i++)
            {
                pipeline.EnableRenderToCubeMap(mirrorObject.GetCubemapID(), i, Color.White);
                pipeline.SetModelViewMatrixToCamera(cameras[i]);
                pipeline.SetProjectionMatrix3D(1, 1, 90);
                if (globalProps.UseFrustumCulling) FrustumCulling.CalculateFrustumPlanes(pipeline.GetProjectionMatrix(), pipeline.GetModelViewMatrix());

                drawSceneWithoutMirros();
   
                cubemapImages[i] = pipeline.GetColorDataFromCubeMapSide(mirrorObject.GetCubemapID(), i);
                //pipeline.GetColorDataFromCubeMapSide(mirrorObject.GetCubemapID(), i).Save("..\\Cubemap_" + i + ".bmp");
            }

            //Testausgabe aller 6 Würfelseiten als ein Bild
            //BitmapHelper.BitmapHelp.SetAlpha(BitmapHelper.BitmapHelp.GetCubemapImage(cubemapImages, true), 255).Save("..\\Cubemap"+ pipeline.ToString().Split('.')[1].Replace("GraphicPipeline", "") + ".bmp");

            pipeline.DisableRenderToCubeMap();
            pipeline.SetProjectionMatrix3D(pipeline.Width, pipeline.Height, globalProps.Camera.OpeningAngleY, globalProps.Camera.zNear, globalProps.Camera.zFar);
            pipeline.SetModelViewMatrixToCamera(globalProps.Camera);
            if (globalProps.UseFrustumCulling) FrustumCulling.CalculateFrustumPlanes(pipeline.GetProjectionMatrix(), pipeline.GetModelViewMatrix());

            mirrorObject.Draw(lightSources, null, globalProps);
        }

        private static Camera[] GetCubemapCameras(Vector3D position, Vector3D orientation, float size)
        {
            Camera[] cameras = new Camera[6];
            Matrix4x4 mv_obj_to_world = Matrix4x4.Model(position, orientation, size);
            Matrix4x4 normalMatrix = Matrix4x4.Transpose(Matrix4x4.Invert(mv_obj_to_world));

            var targets = new[] {
                new Vector3D(+1,  0,  0), //Right
                new Vector3D(-1,  0,  0), //Left
                new Vector3D( 0, +1,  0), //Top
                new Vector3D( 0, -1,  0), //Bottom
                new Vector3D( 0,  0, +1), //Back
                new Vector3D( 0,  0, -1)  //Front
            };
            var ups = new[] {
                new Vector3D(0, 1, 0),
                new Vector3D(0, 1, 0),
                new Vector3D(0, 0, -1),
                new Vector3D(0, 0, +1),
                new Vector3D(0, 1, 0),
                new Vector3D(0, 1, 0),
            };

            Vector3D center = Matrix4x4.MultPosition(mv_obj_to_world, new Vector3D(0, 0, 0));

            for (int i = 0; i < 6; i++)
            {
                cameras[i] = new Camera(center, Vector3D.Normalize(Matrix4x4.MultDirection(normalMatrix, targets[i])), Vector3D.Normalize(Matrix4x4.MultDirection(normalMatrix, -ups[i])), 90);
            }

            return cameras;
        }
    }
}
