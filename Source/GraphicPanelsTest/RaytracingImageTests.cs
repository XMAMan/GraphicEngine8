using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BitmapHelper;
using GraphicPanels;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestHelper;

namespace GraphicPanelsTest
{
    [TestClass]
    public class RaytracingImageTests
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void CreateSkyImage()
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = 40, Height = 40 };

            graphic.Mode = Mode3D.ThinMediaSingleScatteringBiased;
            graphic.GlobalSettings.SamplingCount = 1;
            //graphic.GlobalSettings.ThreadCount = 1;
            graphic.GlobalSettings.PhotonCount = 1;
            graphic.GlobalSettings.RecursionDepth = 7;
            graphic.GlobalSettings.BackgroundImage = "#000000";

            int stepCount = 8 * 2;

            List<Bitmap> images = new List<Bitmap>();
            for (int i = 0; i < stepCount; i++)
            {
                TestScenes.AddTestscene_SkyMedia(graphic, ((float)i / stepCount * 140 - 40));
                var image = graphic.GetRaytracingImageSynchron(graphic.Width, graphic.Height);
                images.Add(image.Bitmap);
            }

            var row1 = BitmapHelp.TransformBitmapListToRow(images.GetRange(0, 8));
            var row2 = BitmapHelp.TransformBitmapListToRow(images.GetRange(8, 8));

            Bitmap result = BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>() { row1, row2 });
            result.Save(WorkingDirectory + "SkyRowImages.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\SkyRowImages_Expected.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        [TestMethod]
        public void CreateSkyCompareImage()
        {
            //int samplingCount = 1000, width = 400, height = 400;
            int samplingCount = 10, width = 40, height = 40;
            List<Bitmap> images = new List<Bitmap>
            {
                //Multipe Scattering
                GetSkyImage(Mode3D.MediaBidirectionalPathTracing, samplingCount, width, height),
                GetSkyImage(Mode3D.ThinMediaMultipleScattering, samplingCount, width, height),

                //SingleScattering
                GetSkyImage(Mode3D.ThinMediaSingleScattering, samplingCount * 8, width, height),
                GetSkyImage(Mode3D.ThinMediaSingleScatteringBiased, 1, width, height)
            };
            Bitmap result = BitmapHelp.TransformBitmapListToRow(images);
            result.Save(WorkingDirectory + "SkyCompare.bmp");

            var diff = DifferenceImageCreator.GetDifferenceImage(new Bitmap(WorkingDirectory + "\\ExpectedValues\\SkyCompare_Expected.bmp"), result);
            diff.Image.Save(WorkingDirectory + "SkyCompareDifference.bmp");
            Assert.IsTrue(diff.GetMaxError() < 1, diff.GetMaxErrorWithName() + " (Max Allowed Error=1)");

        }

        private Bitmap GetSkyImage(Mode3D mode, int samplingCount, int width, int height)
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = width, Height = height };
            TestScenes.AddTestscene_SkyMedia(graphic, 50);
            graphic.Mode = mode;
            graphic.GlobalSettings.SamplingCount = samplingCount;
            //graphic.GlobalSettings.ThreadCount = 1;
            graphic.GlobalSettings.PhotonCount = 1;
            graphic.GlobalSettings.RecursionDepth = 7;
            graphic.GlobalSettings.BackgroundImage = "#000000";
            
            Bitmap image = graphic.GetRaytracingImageSynchron(graphic.Width, graphic.Height).Bitmap;
            return BitmapHelp.WriteToBitmap(image, mode.ToString(), Color.Black);
        }

        #region MasterTest
        private enum Genauigkeit { Quick, Middle, Exactly };

        [TestMethod]
        public void MasterTestImage()
        {
            Genauigkeit mode = Genauigkeit.Quick;

            int samplingCount = -1, width = -1, height = -1, pathTracingSampleCount = -1;
            bool useRadiosityQuickSettings = true;

            if (mode == Genauigkeit.Quick) //9 Minuten auf den IFX-i3
            {
                samplingCount = 1;
                width = 210;
                height = 164;
                pathTracingSampleCount = 100;
                useRadiosityQuickSettings = true;
            }
            if (mode == Genauigkeit.Middle) //11 Stunden 14 Minuten auf den IFX-i3, wenn ich useRadiosityQuickSettings auf True setze
            {                                //16 Stunden auf dem IFX-i3, wenn ich useRadiosityQuickSettings auf False setze
                //samplingCount=200;pathTracingSampleCount=4000 -> 2 Tage; 3 Stunden
                //samplingCount=240;pathTracingSampleCount=4800
                samplingCount = 50;
                width = 420;
                height = 328;
                pathTracingSampleCount = 1000;
                useRadiosityQuickSettings = false;
            }
            if (mode == Genauigkeit.Exactly)
            {
                samplingCount = 300;
                width = 420;
                height = 328;
                pathTracingSampleCount = 7000;
                useRadiosityQuickSettings = false;
            }

            List<Bitmap> images = new List<Bitmap>();
            
            images.AddRange(GetImagesFromManyModi(new List<Mode3D>()
            {
                Mode3D.OpenGL_Version_1_0,
                Mode3D.OpenGL_Version_3_0,
                Mode3D.Direct3D_11,
                Mode3D.CPU,
                
            }, 1, width, height, useRadiosityQuickSettings));

            if (mode == Genauigkeit.Quick)
                images.Add(BitmapHelp.ScaleImageUp(GetImagesFromSingleMode(Mode3D.PathTracer, pathTracingSampleCount, width / 4, height / 4, useRadiosityQuickSettings), 4));
            else
                images.Add(GetImagesFromSingleMode(Mode3D.PathTracer, pathTracingSampleCount, width, height, useRadiosityQuickSettings));


            images.AddRange(GetImagesFromManyModi(new List<Mode3D>()
            {
                Mode3D.FullBidirectionalPathTracing,
                Mode3D.Photonmapping,
                Mode3D.ProgressivePhotonmapping,
                Mode3D.VertexConnectionMerging,
            }, samplingCount, width, height, useRadiosityQuickSettings));
            
            images.AddRange(GetImagesFromManyModi(new List<Mode3D>()
            {
                Mode3D.RadiositySolidAngle,
                Mode3D.RadiosityHemicube,
            }, 1, width, height, useRadiosityQuickSettings));

            Size stillLife = BitmapHelp.GetImageSizeWithSpecificWidthToHeightRatio(width, height, 160, 70);  //Ich möchte, dass das Seitenverhältnis von der StillLife-Szene 160 zu 70 ist
            images.Add(BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>()
            {
                BitmapHelp.GetEmptyImage(width, height - stillLife.Height, Color.White),
                GetImage(Mode3D.UPBP, samplingCount, stillLife.Width, stillLife.Height, TestScenes.AddTestscene19_StillLife, useRadiosityQuickSettings),                
                GetImage(Mode3D.ThinMediaMultipleScattering, samplingCount, width, height, TestScenes.AddTestScene18_CloudsForTestImage, useRadiosityQuickSettings),
                GetImage(Mode3D.MediaFullBidirectionalPathTracing, samplingCount, width, height, TestScenes.AddTestscene5_MirrorCornellbox, useRadiosityQuickSettings),
            }));

            Bitmap result = BitmapHelp.TransformBitmapListToRow(images);
            result.Save(WorkingDirectory + "MasterTestImage.bmp");

            var diff = DifferenceImageCreator.GetDifferenceImage(new Bitmap(WorkingDirectory + "\\ExpectedValues\\MasterTestImage_Exprected.bmp"), new Bitmap(WorkingDirectory + "MasterTestImage.bmp"));
            diff.Image.Save(WorkingDirectory + "MasterTestImageDifference.bmp");
            Assert.IsTrue(diff.GetMaxError() < 16, diff.GetMaxErrorWithName() + " (Max Allowed Error=16)");

        }

        private List<Bitmap> GetImagesFromManyModi(List<Mode3D> modi, int samplingCount, int width, int height, bool useRadiosityQuickSettings)
        {
            return modi.Select(x => GetImagesFromSingleMode(x, samplingCount, width, height, useRadiosityQuickSettings)).ToList();
        }

        private Bitmap GetImagesFromSingleMode(Mode3D mode, int samplingCount, int width, int height, bool useRadiosityQuickSettings)
        {
            List<Bitmap> images = new List<Bitmap>()
            {
                GetImage(mode, samplingCount, width, height, (graphic)=>
                {
                    TestScenes.AddTestscene1_RingSphere(graphic);
                    graphic.GlobalSettings.ShadowsForRasterizer = RasterizerShadowMode.Shadowmap;
                }, useRadiosityQuickSettings),
                GetImage(mode, samplingCount, width, height, TestScenes.AddTestscene2_NoWindowRoom, useRadiosityQuickSettings),
                GetImage(mode, samplingCount, width, height, TestScenes.AddTestscene5_Cornellbox, useRadiosityQuickSettings),
            };
            Bitmap image = BitmapHelp.TransformBitmapListToCollum(images);
            return BitmapHelp.WriteToBitmap(image, mode.ToString(), Color.Black);
        }

        private Bitmap GetImage(Mode3D mode, int samplingCount, int width, int height, Action<GraphicPanel3D> addSceneMethod, bool useRadiosityQuickSettings)
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = width, Height = height };

            graphic.GlobalSettings.RecursionDepth = 7;

            //lowPoly = 4,8 Min; HighPoly = 7 Min
            addSceneMethod(graphic);

            if (useRadiosityQuickSettings)
            {
                //Radiosity-Quick-Settings
                graphic.GlobalSettings.RadiositySettings.IlluminationStepCount = 10;
                graphic.GlobalSettings.RadiositySettings.HemicubeResolution = 20;
                graphic.GlobalSettings.RadiositySettings.MaxAreaPerPatch = 0.02f;
                graphic.GlobalSettings.RadiositySettings.SampleCountForPatchDividerShadowTest = 10;
            }
            
            graphic.GlobalSettings.AutoSaveMode = RaytracerAutoSaveMode.Disabled;
            graphic.Mode = mode;
            graphic.GlobalSettings.SamplingCount = samplingCount;
            //graphic.GlobalSettings.ThreadCount = 1;
            graphic.GlobalSettings.PhotonCount = 100000;
            graphic.GlobalSettings.ShadowsForRasterizer = RasterizerShadowMode.Stencil;

            Bitmap image = graphic.GetSingleImage(graphic.Width, graphic.Height);

            graphic.Dispose(); //Ich bekomme eine AccessMode-Violation-Exception, wenn ich zu schnell erst mit OpenGL und dann mit DirectX rendere und nicht zwischendurch Dispose

            return image;
        }
        #endregion

        [TestMethod]
        public void RenderAllScenes()
        {
            //sizeFactor=1; 1=2 Minuten; 10 = 5 Minuten; 100 = 34 Minuten
            //sizeFactor=2; 1=4 Minuten; 10 = 14 Minuten; 100 = 308 Minuten; 1000 = 2733 Minuten; 10k = 8991 Minuten
            int samplingCount = 1; 
            int sizeFactor = 2;

            List<Bitmap> images = new List<Bitmap>
            {
                GetRaytracingImage(samplingCount, sizeFactor, 97, 95, TestScenes.AddTestscene5_WaterCornellbox),
                GetRaytracingImage(samplingCount, sizeFactor, 168, 98, TestScenes.AddTestscene6_ChinaRoom),
                GetRaytracingImage(samplingCount, sizeFactor, 168, 98, TestScenes.AddTestscene7_Chessboard),
                GetRaytracingImage(samplingCount, sizeFactor, 192, 101, TestScenes.AddTestscene11_PillarsOfficeGodRay),
                GetRaytracingImage(samplingCount, sizeFactor, 168, 98, TestScenes.AddTestscene12_Snowman),
                GetRaytracingImage(samplingCount, sizeFactor, 108, 94, TestScenes.AddTestscene15_MicrofacetSphereBox),
                GetRaytracingImage(samplingCount, sizeFactor, 160, 83, TestScenes.AddTestscene16_Graphic6Memories),
                GetRaytracingImage(samplingCount, sizeFactor, 192, 101, TestScenes.AddTestscene27_MirrorsEdge)
            };

            Bitmap result = BitmapHelp.TransformBitmapListToRow(images);
            result.Save(WorkingDirectory + "AllScenes.bmp");

            var diff = DifferenceImageCreator.GetDifferenceImage(new Bitmap(WorkingDirectory + "\\ExpectedValues\\AllScenes_Exprected.bmp"), new Bitmap(WorkingDirectory + "AllScenes.bmp"));
            diff.Image.Save(WorkingDirectory + "AllScenesDifference.bmp");
            Assert.IsTrue(diff.GetMaxError() < 31, diff.GetMaxErrorWithName() + " (Max Allowed Error=31)");
        }

        private Bitmap GetRaytracingImage(int samplingCount, int sizeFactor, int width, int height, Action<GraphicPanel3D> addSzeneMethod)
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = width * sizeFactor, Height = height * sizeFactor };
            addSzeneMethod(graphic);
            graphic.GlobalSettings.SamplingCount = samplingCount;
            Bitmap image = graphic.GetSingleImage(graphic.Width, graphic.Height);
            graphic.Dispose(); //Ich bekomme eine AccessMode-Violation-Exception, wenn ich zu schnell erst mit OpenGL und dann mit DirectX rendere und nicht zwischendurch Dispose
            return image;
        }
    }
}
