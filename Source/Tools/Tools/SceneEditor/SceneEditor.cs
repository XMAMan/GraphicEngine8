using System;
using System.Drawing;
using System.Windows.Forms;
using GraphicPanels;
using GraphicMinimal;
using System.IO;

//Vorherige Projekte:                       - Besonderheiten
//Version 1 - 3DObjekte(12.7.2006)          - Erste 3D-Grafikengine; Stencilschatten
//Version 2 - Schattenraum(8.8.2006)        - Drahtkugel
//Version 3 - Dave(13.9.2006)               - Erstes 3D-Spiel mit OpenGL
//Version 4 - Schattenraum2 3(22.1.2009)    - Erste Schritte in globaler Beleuchtung mit Radiosity
//Version 5 - bombaCG(6.8.2009)             - Stencil-Spiegel
//Version 6 - Prince of Persia(7.2.2010)    - Cubemapped Spiegel; Partikelfeuer
//Version 7 - Bumpmapping(28.10.2010)       - lokal beleuchteteter Raytracer; Normalmapping für horizontales Viereck, Tiefenunschärfe
//Version 8 - Grafikengine7(12.7.2013)      - Pathtracer, BPT, VCM, Photonmapping, Radiosity, Parallaxmapping
//Version 9 - GraphicEngine8(31.5.2017)     - Wolken, UPBP, Shadowmapping, Parallax Occlusion Mapping, UnitTests

//31.5.2017  Projekt wurde angelegt
//1.5.2019 Ich beginne mit der Arbeit am Stilllifebild
//16.11.2021 Stilllife-Bild stimmt zu 100% mit Referenzbild überrein
//10.10.2022 Projekt geht auf GitHub
//Nächster Schritt: Spectrales Raytracing

namespace Tools.Tools.SceneEditor
{
    //Commandline-Arguments im VisualStudio: SceneEditor ..\..\..\..\Data\ ..\SaveFolder
    public partial class SceneEditor : Form
    {
        private Exception raytracerException = null;
        private readonly Panel3DProgressText raytracingProgress;

        public SceneEditor()
        {
            InitializeComponent();
        }

        public SceneEditor(string dataFolder, string saveFolder)
        {
            InitializeComponent();
            this.raytracingProgress = new Panel3DProgressText(this.graphicPanel);

            try
            {
                Scenes.DataDirectory = dataFolder;

                //Scenes.CreateSceneFiles(); return; //Immer wenn jemand in der Scenes-Klasse was geändert hat muss das hier gemacht werden

                int testscene = 0; //Wähle hier aus, welche Scene angezeigt werden soll

                if (testscene == 0) Scenes.AddTestszene1_RingSphere(this.graphicPanel);
                if (testscene == 1) Scenes.AddTestszene2_NoWindowRoom(this.graphicPanel);
                if (testscene == 2) Scenes.AddTestszene3_SpheresWithTexture(this.graphicPanel);
                if (testscene == 3) Scenes.AddTestszene4_ProceduralTextures(this.graphicPanel);
                if (testscene == 4) Scenes.AddTestszene5_Cornellbox(this.graphicPanel);
                if (testscene == 5) Scenes.AddTestszene5_WaterCornellbox(this.graphicPanel);
                if (testscene == 6) Scenes.AddTestszene5_MirrorCornellbox(this.graphicPanel);
                if (testscene == 7) Scenes.AddTestszene5_BlenderCornellbox(this.graphicPanel);
                if (testscene == 8) Scenes.AddTestszene5_WaterNoMediaCornellbox(this.graphicPanel);
                if (testscene == 9) Scenes.AddTestszene5_RisingSmokeCornellbox(this.graphicPanel);
                if (testscene == 10) Scenes.AddTestszene6_ChinaRoom(this.graphicPanel);
                if (testscene == 11) Scenes.AddTestszene7_Chessboard(this.graphicPanel);
                if (testscene == 12) Scenes.AddTestszene8_WindowRoom(this.graphicPanel);
                if (testscene == 13) Scenes.AddTestszene9_MultipleImportanceSampling(this.graphicPanel);
                if (testscene == 14) Scenes.AddTestszene10_MirrorGlassCaustic(this.graphicPanel);
                if (testscene == 15) Scenes.AddTestszene11_PillarsOfficeGodRay(this.graphicPanel);
                if (testscene == 16) Scenes.AddTestszene12_Snowman(this.graphicPanel);
                if (testscene == 17) Scenes.AddTestszene13_MicrofacetGlas(this.graphicPanel);
                if (testscene == 18) Scenes.AddTestszene14_MicrofacetSundial(this.graphicPanel);
                if (testscene == 19) Scenes.AddTestszene15_MicrofacetSphereBox(this.graphicPanel);
                if (testscene == 20) Scenes.AddTestszene16_Graphic6Memories(this.graphicPanel);
                if (testscene == 21) Scenes.AddTestszene17_TheFifthElement(this.graphicPanel);
                if (testscene == 22) Scenes.AddTestszene18_SkyWithClouds(this.graphicPanel, 0, 2, 1200, "#004400", 0, Scenes.CloudShape.Sphere); //Mittags
                if (testscene == 23) Scenes.AddTestszene18_SkyWithClouds(this.graphicPanel, 85, 7, 800, "#00AA00", 1, Scenes.CloudShape.Sphere);//Abends
                if (testscene == 24) Scenes.AddTestszene19_StillLife(this.graphicPanel);
                if (testscene == 25) Scenes.AddTestszene20_Mirrorballs(this.graphicPanel);
                if (testscene == 26) Scenes.AddTestszene21_Candle(this.graphicPanel);
                if (testscene == 27) Scenes.AddTestszene22_ToyBox(this.graphicPanel);
                if (testscene == 28) Scenes.AddTestszene23_MirrorShadowWithSphere(this.graphicPanel);
                if (testscene == 29) Scenes.AddTestszene23_MirrorShadowNoSphere(this.graphicPanel);
                if (testscene == 30) Scenes.AddTestszene24_EnvironmentMaterialTest(this.graphicPanel);
                if (testscene == 31) Scenes.AddTestszene25_SingleSphereForRapso(this.graphicPanel);
                if (testscene == 32) Scenes.AddTestszene26_SkyEnvironmapCreator(this.graphicPanel);
                if (testscene == 33) Scenes.AddTestszene27_MirrorsEdge(this.graphicPanel);
   
                //...................
                this.graphicPanel.Mode = Mode3D.Raytracer;

                this.graphicPanel.GlobalSettings.SaveFolder = saveFolder;
                //this.graphicPanel.GlobalSettings.AutoSaveMode = RaytracerAutoSaveMode.FullScreen; 

                this.graphicPanel.GlobalSettings.Tonemapping = TonemappingMethod.None;
                //this.graphicPanel.GlobalSettings.ThreadCount = 1;
                this.graphicPanel.GlobalSettings.SamplingCount = 1;
                this.graphicPanel.GlobalSettings.PhotonCount = 60000;//100000
                this.graphicPanel.GlobalSettings.RaytracerRenderMode = RaytracerRenderMode.Frame;
                this.graphicPanel.GlobalSettings.PhotonmapPixelSettings = PhotonmapDirectPixelSetting.ShowPixelPhotons; //Einstellungen, wenn man Mode=PhotonmapPixel verwendet
                this.graphicPanel.GlobalSettings.MetropolisBootstrapCount = 100000;
                //this.graphicPanel.GlobalSettings.MaxRenderTimeInSeconds = 9;
                //this.graphicPanel.GlobalSettings.RecursionDepth = 3; //3 = Direktes Licht
                //File.WriteAllText(FilePaths.DataDirectory+"FlipperResult.obj", this.graphicPanel.GetFlippedWavefrontFileFromCurrentSceneData(this.graphicPanel.Width, this.graphicPanel.Height));

                //Radiosity-Quick-Settings
                this.graphicPanel.GlobalSettings.RadiositySettings.IlluminationStepCount = 10;
                this.graphicPanel.GlobalSettings.RadiositySettings.HemicubeResolution = 20;
                this.graphicPanel.GlobalSettings.RadiositySettings.MaxAreaPerPatch = 0.02f;
                //this.graphicPanel.GlobalSettings.RadiositySettings.VisibleMatrixFileName = "VisibleMatrix.dat"; //437 Sekunden ohne Matrix-Speichern; 257 Mit Matrix-Laden (D.h. 437-257 = 180 Sekunden hat der KD-Baum benötigt)
                this.graphicPanel.GlobalSettings.RadiositySettings.SampleCountForPatchDividerShadowTest = 10;                
                this.graphicPanel.GlobalSettings.RadiositySettings.RadiosityColorMode = RadiosityColorMode.WithoutColorInterpolation;
                
                this.graphicPanel.GlobalSettings.ShadowsForRasterizer = RasterizerShadowMode.Shadowmap;
                this.graphicPanel.GlobalSettings.CameraSamplingMode = PixelSamplingMode.Tent;
                //...................

                //Wenn ein Fehler passiert ist, dann ersetze die xmlString-Zeile durch das, was in der Exception steht, um den Fehler im Debugger nachzustellen
                //GlobalSettings/Bildgröße/PixelRange/Pixel/Random-Zustände kommen alles aus dem xmlString. Es muss nur noch die richtige Szene hinzugefügt werden. GlobalMedia wird nicht serialisiert!
                //string xmlString = "<?xml version=\"1.0\" encoding=\"utf-16\"?><RaytracingDebuggingData xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">  <ScreenSize>    <Width>420</Width>    <Height>328</Height>  </ScreenSize>  <PixelData>    <PixX>70</PixX>    <PixY>211</PixY>    <RandomObjectBase64Coded>AAEAAAD/////AQAAAAAAAAAEAQAAAA1TeXN0ZW0uUmFuZG9tAwAAAAVpbmV4dAZpbmV4dHAJU2VlZEFycmF5AAAHCAgIHgAAADMAAAAJAgAAAA8CAAAAOAAAAAgAAAAALkUJXRZlMxB2899qX3cdasvk4Hhshc8RzxekOM67PxJ3/Rw4V0g7Tal7p3vSNkR3dxmsVxW+PwyAMnkg+KEoNUxgiTmamr0rVwBqXqszXk1mRI4MUTWPC7pc+0aralJT7IhDTQSSk2hxxl9qE93AGgWXpR87QBl3Gq9CJXrewynjlUFv9LPsD+biE2aXS7MroNArc8xd42G9/F8Sh77pMXyRqRYGcdALZzOwH6uAJHwww5MlrJ1EU/duEUTV9D9shkfiQDjKsheHcxN3tYSBALp/TDUEE1k05vETKws=</RandomObjectBase64Coded>  </PixelData>  <FramePrepareData>    <PixelRange>      <XStart>0</XStart>      <YStart>0</YStart>      <Width>420</Width>      <Height>328</Height>    </PixelRange>    <FrameIterationNumber>174</FrameIterationNumber>    <RandomObjectBase64Coded>AAEAAAD/////AQAAAAAAAAAEAQAAAA1TeXN0ZW0uUmFuZG9tAwAAAAVpbmV4dAZpbmV4dHAJU2VlZEFycmF5AAAHCAgIHAAAADEAAAAJAgAAAA8CAAAAOAAAAAgAAAAA6023RTZE2DnzaHs+ZX/2Qy+cMwrHTat0JX7aCWv2T2LNeV4si4ZuAacqIlXuPcldoYYqKrdxtFtQOpR14pn+MXlITFUI4fsg8ggLdgftzGtG2floUY43XbE4DxwJDb8bKn3nIsqVSiVvevY5cVf5CuUgRS2rqetljN6maAEhvT5bzuwLWsbbHfW6ZnGSgJcL5WCiAnECMzdsvSYe0/BBMX4UuWQ5vM4AK/OQf0X8Oyi6makOu9RdFMmgJxo4b0Y6y2PXRETqwHBEUutXtGUlLtJ6dlYbhT9TtPsEXws=</RandomObjectBase64Coded>  </FramePrepareData>  <GlobalSettings>    <Camera>      <Position>        <X>0</X>        <Y>6360030</Y>        <Z>0</Z>      </Position>      <Forward>        <X>0.928476632</X>        <Y>0.371390671</Y>        <Z>0</Z>      </Forward>      <Up>        <X>0</X>        <Y>1</Y>        <Z>0</Z>      </Up>      <OpeningAngleY>60</OpeningAngleY>      <zNear>0.001</zNear>      <zFar>3000</zFar>    </Camera>    <BackgroundImage>#000000</BackgroundImage>    <BackgroundColorFactor>1</BackgroundColorFactor>    <ExplosionRadius>1</ExplosionRadius>    <Time>0</Time>    <ShadowsForRasterizer>Shadowmap</ShadowsForRasterizer>    <UseFrustumCulling>true</UseFrustumCulling>    <DistanceDephtOfFieldPlane>100</DistanceDephtOfFieldPlane>    <WidthDephtOfField>2</WidthDephtOfField>    <DepthOfFieldIsEnabled>false</DepthOfFieldIsEnabled>    <UseCosAtCamera>true</UseCosAtCamera>    <CameraSamplingMode>Tent</CameraSamplingMode>    <SaveFolder></SaveFolder>    <AutoSaveMode>Disabled</AutoSaveMode>    <SamplingCount>10000</SamplingCount>    <RecursionDepth>10</RecursionDepth>    <ThreadCount>3</ThreadCount>    <MaxRenderTimeInSeconds>2147483647</MaxRenderTimeInSeconds>    <RaytracerRenderMode>Frame</RaytracerRenderMode>    <Tonemapping>None</Tonemapping>    <BrightnessFactor>1</BrightnessFactor>    <PhotonCount>60000</PhotonCount>    <PhotonmapSearchRadiusFactor>1</PhotonmapSearchRadiusFactor>    <BeamDataLineQueryReductionFactor>0.1</BeamDataLineQueryReductionFactor>    <SearchRadiusForMediaBeamTracer>0.005</SearchRadiusForMediaBeamTracer>    <PhotonmapPixelSettings>ShowPixelPhotons</PhotonmapPixelSettings>    <RadiositySettings>      <RadiosityColorMode>WithColorInterpolation</RadiosityColorMode>      <MaxAreaPerPatch>0.01</MaxAreaPerPatch>      <HemicubeResolution>30</HemicubeResolution>      <IlluminationStepCount>10</IlluminationStepCount>      <GenerateQuads>true</GenerateQuads>      <SampleCountForPatchDividerShadowTest>40</SampleCountForPatchDividerShadowTest>      <UseShadowRaysForVisibleTest>true</UseShadowRaysForVisibleTest>    </RadiositySettings>    <LightPickStepSize>0</LightPickStepSize>  </GlobalSettings></RaytracingDebuggingData>";
                //this.graphicPanel.GetColorFromSinglePixelForDebuggingPurpose(RaytracingDebuggingData.CreateFromXmlString(xmlString));

                this.timer1.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.D1) this.graphicPanel.Mode = Mode3D.OpenGL_Version_1_0;
            if (keyData == Keys.D2) this.graphicPanel.Mode = Mode3D.OpenGL_Version_3_0;
            if (keyData == Keys.D3) this.graphicPanel.Mode = Mode3D.Direct3D_11;
            if (keyData == Keys.D4) this.graphicPanel.Mode = Mode3D.CPU;
            if (keyData == Keys.D5) { this.graphicPanel.Mode = Mode3D.Raytracer; this.graphicPanel.StartRaytracing();  }
            if (keyData == Keys.Space)
            {
                this.graphicPanel.StartRaytracing(this.graphicPanel.Width, this.graphicPanel.Height, (result) =>
                //this.graphicPanel.StartRaytracing(420, 328, new ImagePixelRange(new Point(309, 7), new Point(409,32)), (result) => //Szene 22 WolkenTagsüber -> obere Kante von rechter Wolke
                //this.graphicPanel.StartRaytracingFromSubImage(new Bitmap("Fußboden.png"), (result) =>
                {
                    if (result != null)
                    {
                        this.Text = result.RenderTime;
                        result.Bitmap.Save("ausgabe.bmp");
                        result.RawImage.WriteToFile("rawImageData.raw");
                    }
                }, (error) => { this.raytracerException = error; });
            }

            //Wenn ich die Stillife-Szene mit 2000 Samples bei '160 * 2, 70 * 2' erzeuge, ist die frames.Dat-Datei 28Gb und die PixelRangeResult.txt 16 Gb groß. Renderzeit: 11 Stunden
            if (keyData == Keys.M)
            {
                if (Directory.Exists("PixelRangeResult") == false) Directory.CreateDirectory("PixelRangeResult");
                this.graphicPanel.StartImageAnalyser("PixelRangeResult",
                    //this.graphicPanel.Width, this.graphicPanel.Height, (result) =>
                    160*4, 70*4, new ImagePixelRange(new Point(17 * 4, 35 * 4), new Point(17 * 4 + 11, 35 * 4 + 10)), (result) => //Stillife Kerze RadianceWithGammaAndClamping = [106,121628;76,6627502;55,6862755] 
                    {
                    if (result != null)
                    {
                        this.Text = result.RenderTime;
                        result.Bitmap.Save("ImageAnalyser.bmp");
                    }
                }, (error) => { this.raytracerException = error; });
            }

            if (keyData == Keys.O) File.WriteAllText("Export.obj", this.graphicPanel.ExportToWavefront());
            if (keyData == Keys.P) this.graphicPanel.GetScreenShoot().Save(@"ScreenShoot.bmp");
            if (keyData == Keys.S) this.graphicPanel.SaveCurrentRaytracingDataToFolder();

            if (keyData == Keys.D6)
            {
                this.graphicPanel.GetObjectById(1).NormalSource = new NormalFromObjectData();
                this.graphicPanel.GetObjectById(1).DisplacementData.UseDisplacementMapping = false;
            }
            if (keyData == Keys.D7)
            {
                var tex = this.graphicPanel.GetObjectById(1).Color.As<ColorFromTexture>();
                this.graphicPanel.GetObjectById(1).NormalSource = new NormalFromMap() { NormalMap = tex.TextureFile, TextureMatrix = tex.TextureMatrix, ConvertNormalMapFromColor = true };
                this.graphicPanel.GetObjectById(1).DisplacementData.UseDisplacementMapping = false;
            }
            if (keyData == Keys.D8)
            {
                var tex = this.graphicPanel.GetObjectById(1).Color.As<ColorFromTexture>();
                this.graphicPanel.GetObjectById(1).NormalSource = new NormalFromParallax() { ParallaxMap = tex.TextureFile, TextureMatrix = tex.TextureMatrix, ConvertNormalMapFromColor = true, TexturHeightFactor = 0.07f, IsParallaxEdgeCutoffEnabled = true };
                this.graphicPanel.GetObjectById(1).DisplacementData.UseDisplacementMapping = false;
            }
            if (keyData == Keys.D9)
            {
                var tex = this.graphicPanel.GetObjectById(1).Color.As<ColorFromTexture>();
                this.graphicPanel.GetObjectById(1).NormalSource = new NormalFromMap() { NormalMap = tex.TextureFile, TextureMatrix = tex.TextureMatrix, ConvertNormalMapFromColor = true };
                this.graphicPanel.GetObjectById(1).DisplacementData.UseDisplacementMapping = true;
                this.graphicPanel.GetObjectById(1).DisplacementData.DisplacementHeight = 0.5f;
                this.graphicPanel.GetObjectById(1).DisplacementData.TesselationFaktor = 40;
            }
            if (keyData == Keys.D0)
            {
                GraphicPanel3D.CreateBumpmapFromObjFile(Scenes.DataDirectory + "Huckel.obj", new Size(256, 256), 0.7f).Save(Scenes.DataDirectory + "Huckel1.bmp");
                this.graphicPanel.GetObjectById(1).TextureFile = "#0000FF";
                this.graphicPanel.GetObjectById(1).NormalSource = new NormalFromParallax() { ParallaxMap = Scenes.DataDirectory + "Huckel1.bmp", TextureMatrix = Matrix3x3.Scale(10, 10), IsParallaxEdgeCutoffEnabled = true, TexturHeightFactor = 1.0f };

            }


            if (keyData == Keys.Q) this.graphicPanel.GetObjectById(1).Color.As<ColorFromTexture>().TextureFilter = TextureFilter.Point;
            if (keyData == Keys.W) this.graphicPanel.GetObjectById(1).Color.As<ColorFromTexture>().TextureFilter = TextureFilter.Linear;
            if (keyData == Keys.E) this.graphicPanel.GetObjectById(1).Color.As<ColorFromTexture>().TextureFilter = TextureFilter.Anisotroph;
            if (keyData == Keys.R) this.graphicPanel.GetObjectById(1).Color.As<ColorFromTexture>().TextureMode = TextureMode.Repeat;
            if (keyData == Keys.T) this.graphicPanel.GetObjectById(1).Color.As<ColorFromTexture>().TextureMode = TextureMode.Clamp;
            if (keyData == Keys.A) this.graphicPanel.GlobalSettings.ShadowsForRasterizer = GraphicMinimal.RasterizerShadowMode.Stencil;
            if (keyData == Keys.Y) this.graphicPanel.GlobalSettings.ShadowsForRasterizer = GraphicMinimal.RasterizerShadowMode.Shadowmap;

            

            if (keyData == Keys.Enter) this.graphicPanel.StopRaytracing();

            if (this.graphicPanel.Mode == Mode3D.CPU)
                this.graphicPanel.DrawAndFlip();

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (this.raytracerException != null) throw new Exception(this.raytracerException.ToString(), this.raytracerException);

            if (GraphicPanel3D.IsRasterizerMode(this.graphicPanel.Mode) && this.graphicPanel.Mode != Mode3D.CPU)
            {
                this.graphicPanel.DrawAndFlip();                
            }

            string text = this.raytracingProgress.GetProgressText();
            if (text != null) this.Text = text;
        }

        private int lastSelectedObjektID = -1;
        private void GraphicPanel_MouseClick(object sender, MouseEventArgs e)
        {
            int objID = this.graphicPanel.MouseHitTest(e.Location);
            if (lastSelectedObjektID > 0 && objID == -1) //Deselectieren
            {
                this.graphicPanel.GetObjectById(lastSelectedObjektID).HasSilhouette = false;
                lastSelectedObjektID = -1;
            }
            else if (lastSelectedObjektID == -1 && objID > 0)//Selectieren
            {
                lastSelectedObjektID = objID;
                this.graphicPanel.GetObjectById(lastSelectedObjektID).HasSilhouette = true;
            }
            else if (lastSelectedObjektID > 0 && objID > 0)//Umselectieren
            {
                this.graphicPanel.GetObjectById(lastSelectedObjektID).HasSilhouette = false;
                lastSelectedObjektID = objID;
                this.graphicPanel.GetObjectById(lastSelectedObjektID).HasSilhouette = true;
            }

            this.graphicPanel.DrawAndFlip();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.graphicPanel.StopRaytracing();
        }
    }
}
