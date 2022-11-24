using BitmapHelper;
using GraphicPanels;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GraphicPanelsTest
{
    [TestClass]
    public class RasterizerImageTests
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        private readonly List<Mode3D> modesToTest = new List<Mode3D>()
            {
                Mode3D.OpenGL_Version_1_0,
                Mode3D.OpenGL_Version_3_0,
                Mode3D.Direct3D_11,
                Mode3D.CPU,
                Mode3D.Raytracer
            };

        [TestMethod]
        public void RingSphere()
        {
            var resultList = RenderSzeneWithMultipleModi(modesToTest, 420, 328, (graphic) =>
            {
                TestScenes.AddTestscene1_RingSphereWithParallaxGround(graphic);
            });

            resultList.Add(new ImageResult()
            {
                Text = Mode3D.Direct3D_11.ToString() + " with Displacementmapping",
                Image = CreateImage(Mode3D.Direct3D_11, 420, 328, (graphic) =>
                {
                    TestScenes.AddTestscene1_RingSphereWithParallaxGround(graphic);

                    var tex = graphic.GetObjectById(1).Color.As<ColorFromTexture>();
                    graphic.GetObjectById(1).NormalSource = new NormalFromMap() { NormalMap = tex.TextureFile, TextureMatrix = tex.TextureMatrix, ConvertNormalMapFromColor = true };
                    graphic.GetObjectById(1).DisplacementData.UseDisplacementMapping = true;
                    graphic.GetObjectById(1).DisplacementData.DisplacementHeight = 1;
                    graphic.GetObjectById(1).DisplacementData.TesselationFaktor = 40;
                })
            });

            Bitmap result = TransformImagesToBitmap(resultList);

            

            result.Save(WorkingDirectory + "Rasterizer_RingSphere.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\Rasterizer_RingSphere_Expected.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void NoWindowRoom()
        {
            Bitmap result = TransformImagesToBitmap(RenderSzeneWithMultipleModi(modesToTest, 420, 328, (graphic) =>
            {
                TestScenes.AddTestscene2_NoWindowRoom(graphic);
                graphic.GlobalSettings.ShadowsForRasterizer = RasterizerShadowMode.Stencil;
            }));

            result.Save(WorkingDirectory + "Rasterizer_NoWindowRoom.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\Rasterizer_NoWindowRoom_Expected.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void TexturMapping()
        {
            Bitmap result = TransformImagesToBitmap(RenderSzeneWithMultipleModi(modesToTest, 420, 328, (graphic) =>
            {
                TestScenes.AddTestscene22_ToyBox(graphic);
                graphic.GlobalSettings.ShadowsForRasterizer = RasterizerShadowMode.Shadowmap;
            }));
            
            result.Save(WorkingDirectory + "Rasterizer_Texturmapping.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\Rasterizer_Texturmapping_Expected.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void ShadowsAndBlending()
        {
            Bitmap resultShadowMap = TransformImagesToBitmap(RenderSzeneWithMultipleModi(modesToTest, 420, 328, (graphic) => { TestScenes.AddTestscene23_MirrorShadowNoSphere(graphic); graphic.GlobalSettings.ShadowsForRasterizer = RasterizerShadowMode.Shadowmap; graphic.GlobalSettings.Time = 0; }));

            Bitmap resultStencilShadow = TransformImagesToBitmap(RenderSzeneWithMultipleModi(modesToTest, 420, 328, (graphic) => { TestScenes.AddTestscene23_MirrorShadowNoSphere(graphic); graphic.GlobalSettings.ShadowsForRasterizer = RasterizerShadowMode.Stencil; graphic.GlobalSettings.Time = 7000; }));
            Bitmap result = BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>() { resultShadowMap, resultStencilShadow });

            result.Save(WorkingDirectory + "Rasterizer_ShadowsAndBlending.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\Rasterizer_ShadowsAndBlending_Expected.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        //Achtung: Bei OpenGL1 fehlt der rote Spiegelrand wenn ich im ShaderHelper.UseOldWay=false verwende, da dann Linien
        //über den ShaderMode=None anstatt ShaderMode=Normal gezeichnet werden. Interssanterweise fehlt die rote Farbe nur
        //in der CubeMode-Side Nummer 1. In Side 5 ist der rote Rand zu sehen. Der Vorteil im UseOldWay=false ist, dass dann
        //der Explosionseffekt geht, da dann der GeometryShader genutzt wird.
        //Auffällig ist, dass bei OpenGL3 der CubemapFrame-Depth-Buffer ein Stencilbit hat und bei OpenGL1 ist das nicht so.
        //Wenn ich dort das Stencil-Bit verwende, dann sieht die Cubemap falsch aus
        [TestMethod]
        public void MirrorSphere()
        {
            Bitmap resultShadowMap = TransformImagesToBitmap(RenderSzeneWithMultipleModi(modesToTest, 420, 328, (graphic) => { TestScenes.AddTestscene23_MirrorShadowWithSphere(graphic); graphic.GlobalSettings.ShadowsForRasterizer = RasterizerShadowMode.Shadowmap; graphic.GlobalSettings.Time = 0; }));
            Bitmap resultStencilShadow = TransformImagesToBitmap(RenderSzeneWithMultipleModi(modesToTest, 420, 328, (graphic) => { TestScenes.AddTestscene23_MirrorShadowWithSphere(graphic); graphic.GlobalSettings.ShadowsForRasterizer = RasterizerShadowMode.Stencil; graphic.GlobalSettings.Time = 7000; }));
            Bitmap result = BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>() { resultShadowMap, resultStencilShadow });

            result.Save(WorkingDirectory + "Rasterizer_MirrorSphere.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\Rasterizer_MirrorSphere_Expected.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void DepthOfFieldGrid()
        {
            Bitmap result = CreateImage(Mode3D.DepthOfField, 420, 328, (graphic) => 
            { 
                TestScenes.AddTestscene7_Chessboard(graphic);
            });

            result.Save(WorkingDirectory + "Rasterizer_DepthOfFieldGrid.bmp");
            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\Rasterizer_DepthOfFieldGrid_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void MouseHitTest()
        {
            var modesForMouseHitTest = new List<Mode3D>()
            {
                Mode3D.OpenGL_Version_1_0,
                Mode3D.OpenGL_Version_1_0_OldShaders,
                Mode3D.OpenGL_Version_3_0,
                Mode3D.Direct3D_11,
                Mode3D.CPU,
            };

            foreach (var mode in modesForMouseHitTest)
            {
                string ground = MouseHitTest(mode, 420, 328, TestScenes.AddTestscene1_RingSphere, new Point(223, 267));
                string bottle = MouseHitTest(mode, 420, 328, TestScenes.AddTestscene1_RingSphere, new Point(235, 268));

                Assert.AreEqual("CreateSquareXY:1:1:1", ground, $"{mode} -> {ground} != CreateSquareXY:1:1:1");
                Assert.AreEqual("CreateBottle:1:2:6", bottle, $"{mode} -> {bottle} != CreateBottle:1:2:6");
            }            
        }

        [TestMethod]
        public void OpenGL1WithOldShaders()
        {
            Bitmap result = CreateImage(Mode3D.OpenGL_Version_1_0_OldShaders, 420, 328, TestScenes.AddTestscene1_RingSphereWithParallaxGround);
            result.Save(WorkingDirectory + "Rasterizer_OpenGL1WithOldShaders.bmp");
            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\Rasterizer_OpenGL1WithOldShaders_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void AnisotrophTextureFilter()
        {
            Bitmap result = CreateImage(Mode3D.CPU, 100, 400, TestScenes.AddTestscene_AnisotrophTextureFilter);
            result.Save(WorkingDirectory + "Rasterizer_AnisotrophTextureFilter.bmp");
            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\Rasterizer_AnisotrophTextureFilter_Expected.bmp");
            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }        

        private Bitmap TransformImagesToBitmap(List<ImageResult> images)
        {
            return BitmapHelp.TransformBitmapListToRow(images.Select(x => BitmapHelp.WriteToBitmap(x.Image, x.Text, Color.Black)).ToList());
        }

        class ImageResult
        {
            public string Text;
            public Bitmap Image;
        }

        private List<ImageResult> RenderSzeneWithMultipleModi(List<Mode3D> modi, int width, int height, Action<GraphicPanel3D> addSceneMethod)
        {
            return modi.Select(mod => new ImageResult()
            {
                Image = CreateImage(mod, width, height, addSceneMethod),
                Text = mod.ToString()
            }).ToList();

            
        }

        private Bitmap CreateImage(Mode3D mode, int width, int height, Action<GraphicPanel3D> addSceneMethod)
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = width, Height = height };

            addSceneMethod(graphic);

            graphic.Mode = mode;
            graphic.GlobalSettings.SamplingCount = 1;
            graphic.GlobalSettings.ThreadCount = 1;
            

            Bitmap image =  graphic.GetSingleImage(graphic.Width, graphic.Height);

            graphic.Dispose(); //Ich bekomme eine AccessMode-Violation-Exception, wenn ich zu schnell erst mit OpenGL und dann mit DirectX rendere und nicht zwischendurch Dispose

            return image;
        }

        private string MouseHitTest(Mode3D mode, int width, int height, Action<GraphicPanel3D> addSceneMethod, Point mousePosition)
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = width, Height = height };

            addSceneMethod(graphic);

            graphic.Mode = mode;
            graphic.GlobalSettings.SamplingCount = 1;
            graphic.GlobalSettings.ThreadCount = 1;

            int objId = graphic.MouseHitTest(mousePosition);
            if (objId == -1) return "NoValue";
            string returnValue = graphic.GetObjectById(objId).Name;
            graphic.Dispose();
            return returnValue;
        }
    }
}
