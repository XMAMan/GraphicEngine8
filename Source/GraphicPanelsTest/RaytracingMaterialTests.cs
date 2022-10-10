using BitmapHelper;
using GraphicPanels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnitTestHelper;

namespace GraphicPanelsTest
{
    [TestClass]
    public class RaytracingMaterialTests
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        private readonly int sampleCount = 1; //1 Sample bei Size=1 -> 20 Minuten
                                      //10 Samples 2 Stunden
        private readonly float size = 1;
        private Size imageSize = new Size(197, 328);
        private readonly int imagesPerRow = 8;

        private readonly Dictionary<TestScenes.LegoMaterial, Mode3D> materials = new Dictionary<TestScenes.LegoMaterial, Mode3D>()
        {
            { TestScenes.LegoMaterial.Diffuse, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.Plastic1, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.Plastic2, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.RoughMetal, Mode3D.BidirectionalPathTracing },
            { TestScenes.LegoMaterial.SmoothMetal1, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.SmoothMetal2, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.Glas, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.ProceduralMirror, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.Copper, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.BlockMode, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.Parallax, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.Rust, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.MirrorRust, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.Spider, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.BumpGlas, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.MotionBlure, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.MicofacetGlas, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.RoughnessmapGlas, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.Anisotroph, Mode3D.BidirectionalPathTracing },
            { TestScenes.LegoMaterial.Coffee, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.WaterBoy, Mode3D.FullBidirectionalPathTracing },
            { TestScenes.LegoMaterial.Wax, Mode3D.UPBP },
            { TestScenes.LegoMaterial.WaterIce, Mode3D.UPBP },
            { TestScenes.LegoMaterial.Cloud, Mode3D.ThinMediaMultipleScattering },
        };

        public RaytracingMaterialTests() { }

        public RaytracingMaterialTests(int sampleCount, string outputFolder)
        {
            this.sampleCount = sampleCount;
            this.WorkingDirectory = outputFolder;
        }

        [TestMethod]
        public void CreateMasterImage()
        {
            int rowCount = (int)Math.Ceiling(this.materials.Count / (float)imagesPerRow);
            string outputFileName = WorkingDirectory + "RaytracingMaterials.bmp";

            Bitmap output = new Bitmap((int)(imagesPerRow * this.imageSize.Width * size), (int)(rowCount * this.imageSize.Height * size));
            output.Save(outputFileName);
            output.Dispose();

            for (int index = 0; index < this.materials.Count;index++)               
            {
                //if (index == 23)
                CreateIndexImage(index, outputFileName);
            }

            var diff = DifferenceImageCreator.GetDifferenceImage(new Bitmap(WorkingDirectory + "\\ExpectedValues\\RaytracingMaterials_Expected.bmp"), new Bitmap(outputFileName));
            diff.Image.Save(WorkingDirectory + "RaytracingMaterialsDifference.bmp");
            Assert.IsTrue(diff.GetMaxError() < 24, diff.GetMaxErrorWithName() + " (Max Allowed Error=24)"); //Vergleiche ich 1-Sample mit 10-Smaple habe ich ein MaxError von 14
        }

        private void CreateIndexImage(int index, string outputFileName)
        {
            var key = this.materials.Keys.ToList()[index];

            Bitmap small = CreateImage(key, this.materials[key]);
            BitmapHelp.WriteToBitmap(small, key.ToString(), Color.Black);

            int xi = index % imagesPerRow;
            int yi = index / imagesPerRow;
            int x = xi * (int)(this.imageSize.Width * size);
            int y = yi * (int)(this.imageSize.Height * size);

            BitmapHelp.WriteIntoBitmapFile(outputFileName, new Point(x, y), small);
        }

        private Bitmap CreateImage(TestScenes.LegoMaterial material, Mode3D mode)
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = (int)(this.imageSize.Width * size), Height = (int)(this.imageSize.Height * size) };

            TestScenes.AddTestszene_LegoMan(graphic, material);

            graphic.Mode = mode;
            graphic.GlobalSettings.SamplingCount = this.sampleCount;
            graphic.GlobalSettings.PhotonCount = 60000;
            graphic.GlobalSettings.BeamDataLineQueryReductionFactor = 0.1f;

            //graphic.GlobalSettings.ThreadCount = 1;

            //Falls eine Exception bei GetSingleImage geworfen wird
            //string xmlString = "<?xml version=\"1.0\" encoding=\"utf-16\"?><RaytracingDebuggingData xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">  <ScreenSize>    <Width>197</Width>    <Height>328</Height>  </ScreenSize>  <PixelData>    <PixX>61</PixX>    <PixY>43</PixY>    <RandomObjectBase64Coded>AAEAAAD/////AQAAAAAAAAAEAQAAAA1TeXN0ZW0uUmFuZG9tAwAAAAVpbmV4dAZpbmV4dHAJU2VlZEFycmF5AAAHCAgIIgAAADcAAAAJAgAAAA8CAAAAOAAAAAgAAAAAQ86KFK8vDnPnlGBFl+0yIXtQEjGwZCtHF9t0cQzwaTkSeiM236pBV8JzWEjPGulhMrWjHp+Y+Bz+oMpczb/LFH+lEgwYT5VdvApmTr3TWwLwRol4l5EmG8FNA0Olde8CkL6XUGvDHU/zWBIfse00IT3+HB6B4BwYDGPWG49fvglRz6w6hgRXEUnrRhKb1swW/N31R4ZdRzgmrvA8h08zbZ2dUBBDeZAkfkt6EK0hqFVTOfRdFJtQDPs3+GqekkpwCF1gfh1Rwz5X3uZhk8/SCV7mcDyCA559sVGHXgs=</RandomObjectBase64Coded>  </PixelData>  <GlobalSettings>    <Camera>      <Position>        <X>1.14626873</X>        <Y>0.224639982</Y>        <Z>1.54702294</Z>      </Position>      <Forward>        <X>-0.494257063</X>        <Y>0.0597283728</Y>        <Z>-0.867261469</Z>      </Forward>      <Up>        <X>0</X>        <Y>1</Y>        <Z>0</Z>      </Up>      <OpeningAngleY>23</OpeningAngleY>      <zNear>0.001</zNear>      <zFar>3000</zFar>    </Camera>    <BackgroundImage>#000000</BackgroundImage>    <BackgroundColorFactor>1</BackgroundColorFactor>    <ExplosionRadius>1</ExplosionRadius>    <Time>0</Time>    <ShadowsForRasterizer>Stencil</ShadowsForRasterizer>    <UseFrustumCulling>true</UseFrustumCulling>    <DistanceDephtOfFieldPlane>100</DistanceDephtOfFieldPlane>    <WidthDephtOfField>2</WidthDephtOfField>    <DepthOfFieldIsEnabled>false</DepthOfFieldIsEnabled>    <UseCosAtCamera>true</UseCosAtCamera>    <IgnoreSpecularPaths>false</IgnoreSpecularPaths>    <CameraSamplingMode>Tent</CameraSamplingMode>    <SaveFolder />    <AutoSaveMode>Disabled</AutoSaveMode>    <SamplingCount>1</SamplingCount>    <RecursionDepth>10</RecursionDepth>    <ThreadCount>7</ThreadCount>    <MaxRenderTimeInSeconds>2147483647</MaxRenderTimeInSeconds>    <RaytracerRenderMode>SmallBoxes</RaytracerRenderMode>    <Tonemapping>GammaOnly</Tonemapping>    <BrightnessFactor>1</BrightnessFactor>    <PhotonCount>60000</PhotonCount>    <PhotonmapSearchRadiusFactor>1</PhotonmapSearchRadiusFactor>    <BeamDataLineQueryReductionFactor>0.1</BeamDataLineQueryReductionFactor>    <SearchRadiusForMediaBeamTracer>0.005</SearchRadiusForMediaBeamTracer>    <PhotonmapPixelSettings>ShowPixelPhotons</PhotonmapPixelSettings>    <RadiositySettings>      <RadiosityColorMode>WithColorInterpolation</RadiosityColorMode>      <MaxAreaPerPatch>0.01</MaxAreaPerPatch>      <HemicubeResolution>30</HemicubeResolution>      <IlluminationStepCount>10</IlluminationStepCount>      <GenerateQuads>true</GenerateQuads>      <SampleCountForPatchDividerShadowTest>40</SampleCountForPatchDividerShadowTest>    </RadiositySettings>    <LightPickStepSize>0</LightPickStepSize>  </GlobalSettings></RaytracingDebuggingData>";
            //graphic.GetColorFromSinglePixelForDebuggingPurpose(GraphicMinimal.RaytracingDebuggingData.CreateFromXmlString(xmlString));

            Bitmap image = graphic.GetSingleImage((int)(this.imageSize.Width * size), (int)(this.imageSize.Height * size));
            graphic.Dispose();
            return image;
        }
    }
}
