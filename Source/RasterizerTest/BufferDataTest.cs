using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;
using GraphicPanels;
using GraphicPipelineCPU;
using GraphicPipelineDirect3D11;
using GraphicPipelineOpenGLv1_0;
using GraphicPipelineOpenGLv3_0;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriangleObjectGeneration;

namespace RasterizerTest
{
    [TestClass]
    public class BufferDataTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        //Testet die Methoden:
        //  GetDataFromColorBuffer
        //  GetDataFromDepthBuffer
        //  GetTextureData(CopyScreenToTexture)
        //  pipeline.GetTextureData(pipeline.GetColorTextureIdFromFramebuffer(frameId))
        //  pipeline.GetTextureData(pipeline.GetDepthTextureIdFromFramebuffer(frameId))
        [TestMethod]
        public void GetBufferData()
        {
            Bitmap result = BitmapHelp.SetAlpha(
            BitmapHelp.TransformBitmapListToRow(new List<Bitmap>()
            {
                GetSceneImage(new GraphicPipelineOpenGLv1_0.GraphicPipelineOpenGLv1_0(), "OpenGL1"),
                GetSceneImage(new GraphicPipelineOpenGLv3_0.GraphicPipelineOpenGLv3_0(), "OpenGL3"),
                GetSceneImage(new GraphicPipelineDirect3D11.GraphicPipelineDirect3D11(), "DirectX"),
                GetSceneImage(new GraphicPipelineCPU.GraphicPipelineCPU(), "CPU")
            }, true), 255);

  
            result.Save(WorkingDirectory + "Pipeline.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\Pipeline_Expected.bmp"); 
            if (UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result, false) == false)
            {
                //OpenGL1 mit großer Schrift (Aus ein unbekannten Grund wird die durch ein mir unbekannten vorherigen Test groß wenn ich RunAll mache)
                Bitmap expected1 = new Bitmap(WorkingDirectory + "ExpectedValues\\Pipeline_ExpectedBigFont.bmp");
                Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected1, result));
            }            
        }

        [TestMethod]
        public void Pipeline_CPU()
        {
            IGraphicPipeline pipeline = new GraphicPipelineCPU.GraphicPipelineCPU();
            RenderScene(pipeline);
            BitmapHelp.SetAlpha(pipeline.GetDataFromColorBuffer(), 255).Save(WorkingDirectory + " Pipeline_CPU.bmp");
            pipeline.DrawingControl.Dispose();
        }

        [TestMethod]
        public void Panel_CPU()
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Mode = Mode3D.CPU };
            graphic.GlobalSettings.BackgroundImage = "#FFFFFF";
            graphic.Add3DText("Hallo", 10, 2, new ObjectPropertys() { Position = new Vector3D(0, 0, -15), Orientation = new Vector3D(0, 40, 0), Size = 0.5f, TextureFile = "#FF0000" });
            graphic.AddSphere(1, 3, 3, new ObjectPropertys() { Position = new Vector3D(0, 5, 0), RasterizerLightSource = new RasterizerLightSourceDescription() });
            graphic.GlobalSettings.Camera = new Camera(45);
            graphic.GetSingleImage(400, 200, null).Save(WorkingDirectory + "Panel_CPU.bmp");
            graphic.Dispose();
        }

        private Bitmap GetSceneImage(IGraphicPipeline pipeline, string text)
        {
            pipeline.DrawingControl.Width = 400;
            pipeline.DrawingControl.Height = 200;

            Bitmap image = BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>()
            {
                RenderNormal(pipeline),
                CopyScreenToTexture(pipeline),
                RenderWithFrameBuffer(pipeline),
                RenderIntoViewPort(pipeline)
            });
            image = BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>()
            {
                BitmapHelp.GetEmptyImage(image.Width, 40, Color.White),
                BitmapHelp.GetEmptyImage(image.Width, 4, Color.Black),
                image
            });
            
            pipeline.DrawingControl.Dispose();
            return WriteToBitmap(image, text, Color.Black);             
        }

        private Bitmap RenderNormal(IGraphicPipeline pipeline)
        {
            RenderScene(pipeline);
            Bitmap image1 = WriteToBitmap(pipeline.GetDataFromColorBuffer(), "GetDataFromColorBuffer", Color.Black);
            Bitmap image2 = WriteToBitmap(pipeline.GetDataFromDepthBuffer(), "GetDataFromDepthBuffer", Color.Black);
            return BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>() { image1, image2 });
        }

        private Bitmap CopyScreenToTexture(IGraphicPipeline pipeline)
        {
            RenderScene(pipeline);
            int textureId = pipeline.CreateEmptyTexture(pipeline.Width, pipeline.Height);
            pipeline.CopyScreenToTexture(textureId);
            return WriteToBitmap(pipeline.GetTextureData(textureId), "CopyScreenToTexture", Color.Black);
        }

        private Bitmap RenderWithFrameBuffer(IGraphicPipeline pipeline)
        {            
            int frameId = pipeline.CreateFramebuffer(pipeline.Width, pipeline.Height, true, true);
            pipeline.EnableRenderToFramebuffer(frameId);
            RenderScene(pipeline);
            pipeline.DisableRenderToFramebuffer();
            Bitmap image1 = WriteToBitmap(pipeline.GetTextureData(pipeline.GetColorTextureIdFromFramebuffer(frameId)), "GetColorTextureIdFromFramebuffer", Color.Black);
            Bitmap image2 = WriteToBitmap(pipeline.GetTextureData(pipeline.GetDepthTextureIdFromFramebuffer(frameId)), "GetDepthTextureIdFromFramebuffer", Color.Black);
            return BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>() { image1, image2 });
        }

        private Bitmap RenderIntoViewPort(IGraphicPipeline pipeline)
        {
            pipeline.SetViewport(pipeline.Width / 2, pipeline.Height / 2, pipeline.Width / 2, pipeline.Height / 2);
            RenderScene(pipeline);
            Bitmap image1 = WriteToBitmap(pipeline.GetDataFromColorBuffer(), "ViewPort Right Bottom", Color.Black);

            int textureId = pipeline.CreateEmptyTexture(pipeline.Width, pipeline.Height);
            pipeline.CopyScreenToTexture(textureId);
            Bitmap image3 = WriteToBitmap(pipeline.GetTextureData(textureId), "CopyScreenToTexture with ViewPort", Color.Black);

            pipeline.SetViewport(0, 0, pipeline.Width / 2, pipeline.Height / 2);
            RenderScene(pipeline);
            Bitmap image2 = WriteToBitmap(pipeline.GetDataFromColorBuffer(), "ViewPort Left Top", Color.Black);

            

            return BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>() { image1, image2, image3 });
        }

        private void RenderScene(IGraphicPipeline pipeline)
        {
            var camera = new Camera(45);
            var light = new List<RasterizerLightsource>() { new RasterizerLightsource(new RasterizerLightSourceDescription(), new Vector3D(0, 5, 0)) };
            var triangles = TriangleObjectGenerator.Create3DText("Hallo", 10, 2).Triangles;
            int trianglesId = pipeline.GetTriangleArrayId(triangles);

            //Wenn ich das DrawingControl ändere während ich in den Framebuffer render, dann werden beim Resize neue
            //Color- und Depth-Texturen angelegt und die Texturen vom Framebuffer hängen nun nicht mehr an der Grafikpipeline
            //sondern es hängen neue Texturen da so dass dann die GetTextureData-Funktion von den Framebuffer-Texturen ein
            //falsches Bild liefert. Dieser Fehler ist nur bei DirectX und CPU sichtbar aber nicht bei OpenGL.
            //D.h. OpenGL resized intern die Texturen, wenn das DrawingControl sich ändert. Da ich bei DirectX und CPU 
            //aber selbst verwalte und beim Resize neue Texturen anlege, führt das dann zu den Fehlerbild bei RenderWithFrameBuffer
            //pipeline.DrawingControl.Width = 400;
            //pipeline.DrawingControl.Height = 200;

            pipeline.ClearColorBuffer(Color.White);
            pipeline.ClearDepthAndStencilBuffer();
            pipeline.SetModelViewMatrixToCamera(camera);
            pipeline.SetProjectionMatrix3D(pipeline.Width, pipeline.Height, camera.OpeningAngleY, camera.zNear, camera.zFar);
            pipeline.SetPositionOfAllLightsources(light);
            pipeline.MultMatrix(Matrix4x4.Model(new Vector3D(0, 0, -15), new Vector3D(0, 40, 0), 0.5f));
            pipeline.EnableLighting();
            pipeline.NormalSource = NormalSource.ObjectData;
            pipeline.SetNormalInterpolationMode(InterpolationMode.Flat);
            pipeline.SetColor(1, 0, 0, 1);
            pipeline.SetSpecularHighlightPowExponent(20);
            pipeline.EnableDepthTesting();
            pipeline.EnableCullFace();
            pipeline.DrawTriangleArray(trianglesId);
            pipeline.FlippBuffer();
        }

        //Die Klasse aus BitmapHelp sorgt mit der bitmap.SetResolution(96, 96);-Anweisung, dass alle Bilder ganz groß skaliert werden wenn ich OpenGL3 verwende
        private static Bitmap WriteToBitmap(Bitmap bitmap, string text, Color color)
        {
            //bitmap.SetResolution(96, 96); //Diese Zeile verträgt sich nicht mit OpenGL3
            Graphics grx = Graphics.FromImage(bitmap);
            grx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
            grx.TextContrast = 4;
            grx.DrawString(text, new Font("Arial", 10), new SolidBrush(color), new PointF(0, 0));
            grx.Dispose();
            return bitmap;
        }
    }
}
