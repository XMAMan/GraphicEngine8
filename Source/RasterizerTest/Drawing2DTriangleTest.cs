using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RasterizerTest.Helper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RasterizerTest
{
    //Ich prüfe, dass bei ein einfarbigen Dreieck exakt die gleichen Pixel zwischen den unterschiedlichen Verfahren gezeichnet werden
    [TestClass]
    public class Drawing2DTriangleTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;


        [TestMethod]
        public void TriangleList_CPU()
        {
            Bitmap result = GetTriangleListImage(new GraphicPipelineCPU.GraphicPipelineCPU());
            result.Save(WorkingDirectory + "Triangles2D_CPU.bmp");
            Bitmap expected = new Bitmap(WorkingDirectory + "\\ExpectedValues\\TriangleList_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void TriangleList_OpenGL1()
        {
            Bitmap result = GetTriangleListImage(new GraphicPipelineOpenGLv1_0.GraphicPipelineOpenGLv1_0());
            result.Save(WorkingDirectory + "Triangles2D_OpenGL1.bmp");
            Bitmap expected = new Bitmap(WorkingDirectory + "\\ExpectedValues\\TriangleList_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void TriangleList_OpenGL3()
        {
            Bitmap result = GetTriangleListImage(new GraphicPipelineOpenGLv3_0.GraphicPipelineOpenGLv3_0());
            result.Save(WorkingDirectory + "Triangles2D_OpenGL3.bmp");
            Bitmap expected = new Bitmap(WorkingDirectory + "\\ExpectedValues\\TriangleList_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void TriangleList_DirectX()
        {
            Bitmap result = GetTriangleListImage(new GraphicPipelineDirect3D11.GraphicPipelineDirect3D11());
            result.Save(WorkingDirectory + "Triangles2D_DirectX.bmp");
            Bitmap expected = new Bitmap(WorkingDirectory + "\\ExpectedValues\\TriangleList_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        private Bitmap GetTriangleListImage(IGraphicPipeline pipeline)
        {
            int width = 7;
            int height = 7;
            var triangles = TriangleRasterizerTestData.GetSmallTriangles(width, height);

            //Zu Testzwecken. So kann ich ein einzelnes Dreieck untersuchen
            int index = -1; 
            if (index != -1)
            {
                //Das Dreieck mit diesen Index will ich in groß sehen
                triangles = new Triangle[] { MoveTriangle(triangles[index], new Vector3D(0, -index * height, 0)) };
            }
            

            PipelineHelper.Set2DDrawingArea(pipeline, width, height * triangles.Length);

            pipeline.ClearColorDepthAndStencilBuffer(Color.White);
            pipeline.SetColor(0.25f, 0.25f, 0.25f, 1);
            pipeline.DrawTriangleArray(pipeline.GetTriangleArrayId(triangles));
            pipeline.FlippBuffer();
            Bitmap result = pipeline.GetDataFromColorBuffer();
            pipeline.DrawingControl.Dispose();

            result = UnitTestHelper.BitmapHelper.MarkPixelsWhichDifferFromExpected(new Bitmap(WorkingDirectory + "\\ExpectedValues\\TriangleListSmall_Expected.bmp"), result);

            //Wenn ich ein einzelnes Dreieck untersuche dann wird hier das Bild vergrößert
            if (triangles.Length == 1)
            {
                Bitmap small = BitmapHelp.GetSubImage(result, new Rectangle(0, 0, width, height));
                Bitmap smallScaledUp = SmallBitmapVisualizer.TransformSmallImageToBigImage(small, triangles);
                smallScaledUp.Save(WorkingDirectory + $"Triangle_Index{index}_{pipeline.GetType().Name.Replace("GraphicPipeline", "")}.bmp");

            }


            return SmallBitmapVisualizer.TransformSmallImageToMiddleImage(result, triangles);
        }

        private static Triangle MoveTriangle(Triangle t, Vector3D trans)
        {
            return new Triangle(
                new Vertex(t.V[0].Position + trans, t.V[0].Normal, t.V[0].Tangent, t.V[0].TexcoordU, t.V[0].TexcoordV),
                new Vertex(t.V[1].Position + trans, t.V[1].Normal, t.V[1].Tangent, t.V[1].TexcoordU, t.V[1].TexcoordV),
                new Vertex(t.V[2].Position + trans, t.V[2].Normal, t.V[2].Tangent, t.V[2].TexcoordU, t.V[2].TexcoordV));
        }


        [TestMethod]
        public void RasterizerStageRules_DirectX()
        {
            var pipeline = new GraphicPipelineDirect3D11.GraphicPipelineDirect3D11();
            Bitmap result = GetRasterizerRulesImage(pipeline);

            result.Save(WorkingDirectory + "Triangles2D_DirectXRasterizerStageRules.bmp");
            Bitmap expected = new Bitmap(WorkingDirectory + "\\ExpectedValues\\Triangles2D_RasterizerStageRules_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void RasterizerStageRules_OpenGL3()
        {
            var pipeline = new GraphicPipelineOpenGLv3_0.GraphicPipelineOpenGLv3_0();
            Bitmap result = GetRasterizerRulesImage(pipeline);

            result.Save(WorkingDirectory + "Triangles2D_OpenGL3RasterizerStageRules.bmp");
            Bitmap expected = new Bitmap(WorkingDirectory + "\\ExpectedValues\\Triangles2D_RasterizerStageRules_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void RasterizerStageRules_CPU()
        {
            var pipeline = new GraphicPipelineCPU.GraphicPipelineCPU();
            Bitmap result = GetRasterizerRulesImage(pipeline);

            result.Save(WorkingDirectory + "Triangles2D_CPURasterizerStageRules.bmp");
            Bitmap expected = new Bitmap(WorkingDirectory + "\\ExpectedValues\\Triangles2D_RasterizerStageRules_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        private Bitmap GetRasterizerRulesImage(IGraphicPipeline pipelineToTest)
        {
            int w = 16, h = 8; //Anzahl der Kästchen

            var triangles = TriangleRasterizerTestData.GetTrianglesFromDirectXWebPage();
            Bitmap small = GetSmallRasterizerRulesImage(pipelineToTest, triangles, w, h);

            return SmallBitmapVisualizer.TransformSmallImageToBigImage(small, triangles);
        }

        private Bitmap GetSmallRasterizerRulesImage(IGraphicPipeline pipeline, Triangle[] triangles, int width, int height)
        {
            PipelineHelper.Set2DDrawingArea(pipeline, width, height);

            Vector3D[] colors = new Vector3D[] { new Vector3D(1, 1, 1) * 0.5f, new Vector3D(1, 1, 1) * 0.25f };

            pipeline.ClearColorDepthAndStencilBuffer(Color.White);
            for (int i = 0; i < triangles.Length; i++)
            {
                var col = colors[i % colors.Length];
                pipeline.SetColor(col.X, col.Y, col.Z, 1);
                pipeline.DrawTriangleArray(pipeline.GetTriangleArrayId(new Triangle[] { triangles[i] }));
            }

            pipeline.FlippBuffer();
            Bitmap result = pipeline.GetDataFromColorBuffer();
            pipeline.DrawingControl.Dispose();

            return result;
        }
    }
}
