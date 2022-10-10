using System;
using System.Drawing;
using System.Windows.Forms;
using GraphicPanels;
using GraphicMinimal;
using System.IO;

namespace Tools.Tools
{
    internal partial class SceneFileRenderer : Form
    {
        private Exception raytracerException = null;
        private Panel3DProgressText raytracingProgress;

        public SceneFileRenderer()
        {
            InitializeComponent();

            this.SizeChanged += SceneFileRenderer_SizeChanged;
        }

        private void SceneFileRenderer_SizeChanged(object sender, EventArgs e)
        {
            //Diff=[16,39]
            this.Text = $"Window=[{this.Width},{this.Height}] Panel=[{this.graphicPanel.Width},{this.graphicPanel.Height}] Diff=[{this.Width - this.graphicPanel.Width},{this.Height - this.graphicPanel.Height}]";
        }

        public SceneFileRenderer(CreateImageArgs args)
        {
            InitializeComponent();
            this.raytracingProgress = new Panel3DProgressText(this.graphicPanel);
            this.timer1.Start();

            try
            {
                //Nur der Raytracer kann ein SubArea-Bereich anzeigen. Der Rasterizer muss erst das ganze Bild erzeugen und dann schneide ich den Bildbereich dort raus
                if (args.PixelRange != null && GraphicPanel3D.IsRasterizerMode(args.RenderMod) == false)
                {
                    this.Width = args.PixelRange.Width + 16;
                    this.Height = args.PixelRange.Height + 39;
                }else
                {
                    this.Width = args.Width + 16;
                    this.Height = args.Height + 39;
                }                

                LoadScene(args.DataFolder, args.SceneFile);

                this.graphicPanel.GlobalSettings.SaveFolder = args.SaveFolder;
                if (args.SaveFolder != "") this.graphicPanel.GlobalSettings.AutoSaveMode = RaytracerAutoSaveMode.FullScreen;

                this.graphicPanel.Mode = args.RenderMod;
                this.graphicPanel.GlobalSettings.Tonemapping = args.Tonemapping;
                this.graphicPanel.GlobalSettings.SamplingCount = args.SampleCount;
                this.graphicPanel.GlobalSettings.RaytracerRenderMode = args.SampleCount == 1 ? RaytracerRenderMode.SmallBoxes : RaytracerRenderMode.Frame;
                this.graphicPanel.GlobalSettings.ShadowsForRasterizer = RasterizerShadowMode.Shadowmap;

                this.graphicPanel.GlobalSettings.RadiositySettings.RadiosityColorMode = args.RadiosityColorMode;
                this.graphicPanel.GlobalSettings.RadiositySettings.MaxAreaPerPatch = args.RadiosityMaxAreaPerPatch;

                if (GraphicPanel3D.IsRasterizerMode(this.graphicPanel.Mode))
                {
                    Bitmap rasterImage = graphicPanel.GetSingleImage(args.Width, args.Height, args.PixelRange);
                    if (args.Output.EndsWith(".jpg"))
                        rasterImage.Save(args.Output, System.Drawing.Imaging.ImageFormat.Jpeg);
                    else if(args.Output.EndsWith(".png"))
                        rasterImage.Save(args.Output, System.Drawing.Imaging.ImageFormat.Png);
                    else
                        rasterImage.Save(args.Output, System.Drawing.Imaging.ImageFormat.Bmp);
                    
                    if (args.CloseWindowAfterRendering) this.Close();
                }
                else
                {                    
                    var range = args.PixelRange != null ? args.PixelRange : new ImagePixelRange(0, 0, args.Width, args.Height);

                    this.graphicPanel.StartRaytracing(args.Width, args.Height, range, (result) =>
                    {
                        if (result != null)
                        {
                            this.Text = result.RenderTime;

                            ImagePostProcessingHelper.SaveImageBuffer(result.RawImage, args.Output, args.Tonemapping);
      
                            if (args.CloseWindowAfterRendering) this.Close();
                        }
                    }, (error) => { this.raytracerException = error; });
                }                    
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                this.Close();
            }
        }

        private void LoadScene(string dataDirectory, string sceneFile)
        {
            this.graphicPanel.LoadExportDataFromJson(File.ReadAllText(sceneFile).Replace("<DataFolder>", dataDirectory.Replace("\\", "\\\\")));
        }
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.raytracerException != null) throw new Exception(this.raytracerException.ToString(), this.raytracerException);

            if (GraphicPanel3D.IsRasterizerMode(this.graphicPanel.Mode))
            {
                this.graphicPanel.DrawAndFlip();
            }

            string text = this.raytracingProgress.GetProgressText();
            if (text != null) this.Text = text;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.P) this.graphicPanel.GetScreenShoot().Save("ScreenShoot.bmp");
            if (keyData == Keys.S) this.graphicPanel.SaveCurrentRaytracingDataToFolder();
            if (keyData == Keys.Enter) this.graphicPanel.StopRaytracing();

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SceneFileRenderer_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.graphicPanel.StopRaytracing();
        }
    }
}
