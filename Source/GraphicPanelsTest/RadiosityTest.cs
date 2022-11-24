using BitmapHelper;
using GraphicMinimal;
using GraphicPanels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GraphicPanelsTest
{
    //Mit Radioquick-Settings beträgt die Renderzeit 2,2 Minuten

    [TestClass]
    public class RadiosityTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        enum Version { High, Low };

        [TestMethod]
        public void RadiosityCompare()
        {
            Version version = Version.Low;

            Bitmap result = BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>()
            {
                GetRowFromOneMethod(Mode3D.RadiositySolidAngle, version),
                GetRowFromOneMethod(Mode3D.RadiosityHemicube, version),
            });
            result.Save(WorkingDirectory + "Radiosity.bmp");

            Bitmap expected = new Bitmap(WorkingDirectory + "ExpectedValues\\Radiosity_Expected.bmp");

            Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(expected, result));
        }

        

        private Bitmap GetRowFromOneMethod(Mode3D mode, Version version)
        {
            //Wenn hemiCubeResolution == 30 -> maxPatchArea: 0.01 = 13.8 Minuten; 0.1 = 2,4 Minuten; 0.5 = 2.1 Minuten
            //Wenn hemiCubeResolution == 20 -> maxPatchArea: 0.5 = 1.4 Minuten; 0.1 = 1.7 Minuten
            //Wenn hemiCubeResolution == 10 -> maxPatchArea: 0.5 = 56 Sekunden; 0.1 = 1.2 Minuten

            float maxPatchArea;
            int hemiCubeResolution;
            switch(version)
            {
                case Version.Low:
                    {
                        maxPatchArea = 0.1f;
                        hemiCubeResolution = 20;
                        break;
                    }
                case Version.High:
                    {
                        maxPatchArea = 0.01f;
                        hemiCubeResolution = 30;
                        break;
                    }
                default:
                    throw new ArgumentException(version.ToString());
            }
            
            List<Bitmap> images = new List<Bitmap>
            {
                GetRaytracingImage(mode, 420, 328, TestScenes.AddTestscene1_RingSphere,maxPatchArea, hemiCubeResolution),
                GetRaytracingImage(mode, 420, 328, TestScenes.AddTestscene2_NoWindowRoom,maxPatchArea, hemiCubeResolution),
                GetRaytracingImage(mode, 420, 328, (g)=>{ TestScenes.AddTestscene5_Cornellbox(g); g.RemoveObjekt(8); },maxPatchArea / 4, hemiCubeResolution),
                GetRaytracingImage(mode, 420, 328, TestScenes.AddTestscene8_WindowRoom, maxPatchArea * 2, hemiCubeResolution),
            };

            return BitmapHelp.TransformBitmapListToRow(images);
        }

        private Bitmap GetRaytracingImage(Mode3D mode, int width, int height, Action<GraphicPanel3D> addSzeneMethod, float maxPatchArea, int hemiCubeResolution)
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = width, Height = height };
            addSzeneMethod(graphic);
            graphic.GlobalSettings.SamplingCount = 1;
            graphic.Mode = mode;

            graphic.GlobalSettings.RadiositySettings.MaxAreaPerPatch = maxPatchArea;
            graphic.GlobalSettings.RadiositySettings.HemicubeResolution = hemiCubeResolution;
            graphic.GlobalSettings.RadiositySettings.RadiosityColorMode = RadiosityColorMode.WithoutColorInterpolation;
            if (graphic.GlobalSettings.RadiositySettings.RadiosityColorMode == RadiosityColorMode.RandomColors) 
                graphic.GetAllObjects().ForEach(x => x.TextureFile = "#FFFFFF"); //Wenn ich RandomColors nehme darf ich keine Texturen verwenden


            Bitmap image = graphic.GetSingleImage(graphic.Width, graphic.Height);
            graphic.Dispose(); //Ich bekomme eine AccessMode-Violation-Exception, wenn ich zu schnell erst mit OpenGL und dann mit DirectX rendere und nicht zwischendurch Dispose
            return image;
        }
    }
}
