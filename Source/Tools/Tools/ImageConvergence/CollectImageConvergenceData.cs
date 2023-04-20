using BitmapHelper;
using GraphicMinimal;
using GraphicPanels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tools.CommandLineParsing;

namespace Tools.Tools.ImageConvergence
{
    //Erzeugt ein Bild per Raytracing und läßt sich im 10-Sekundentakt Fortschrittsbilder geben, welche er dann per MAPE
    //(Mean absolute percentage error) gegen ein Referenzbild vergleicht. Diese MAPE-Werte werden in eine Datei geschrieben
    internal partial class CollectImageConvergenceData : Form
    {
        private Exception raytracerException = null;
        private Panel3DProgressText raytracingProgress;
        private DataCollector collector;
        private ErrorCurve errorCurve;
        private GraphicPanel3D graphicPanel;
 
        public CollectImageConvergenceData()
        {
            InitializeComponent();
        }

        public CollectImageConvergenceData(CollectImageConvergenceDataArgs args)
        {
            InitializeComponent();

            this.panelWithoutFlickers1.Paint += (sender, obj) =>
            {
                if (this.panelWithoutFlickers1.BackgroundImage != null) obj.Graphics.DrawImage(this.panelWithoutFlickers1.BackgroundImage, new Rectangle(0, 0, this.panelWithoutFlickers1.Width, this.panelWithoutFlickers1.Height));
            };

            this.graphicPanel = new GraphicPanel3D();
            this.raytracingProgress = new Panel3DProgressText(this.graphicPanel);
            this.collector = new DataCollector(new DataCollectorConstructorData()
            {
                GraphicPanel = this.graphicPanel,
                ReferenceImage = new Bitmap(args.ReferenceImageInputFile),
                CollectRate = args.CollectionTimerTick,
                ProgressImageRate = args.ProgressImageSaveRate,
                OutputFolder = args.OutputFolder
            });
            this.errorCurve = new ErrorCurve(collector.ReferenceImage.Width, collector.ReferenceImage.Height, args.ProgressImageScaleUpFactor);
            
            this.timer1.Start();            

            try
            {
                //Output-Verzeichnis anlegen
                if (Directory.Exists(args.OutputFolder))
                {
                    throw new Exception("The output directory must not already exist");                    
                }
                Directory.CreateDirectory(args.OutputFolder);

                //Szene laden
                string dataFolder = new FileInfo(args.SceneFile).DirectoryName + "\\";
                LoadScene(dataFolder, args.SceneFile);

                this.graphicPanel.Mode = args.RenderMod;
                this.graphicPanel.GlobalSettings.Tonemapping = args.Tonemapping;
                this.graphicPanel.GlobalSettings.SamplingCount = args.SampleCount;
                this.graphicPanel.GlobalSettings.RaytracerRenderMode = RaytracerRenderMode.Frame;

                var range = args.PixelRange != null ? args.PixelRange : new ImagePixelRange(0, 0, args.Width, args.Height);

                this.collector.Start(); //Starte nach Laden der Szene um nur die Renderzeit zu tracken

                this.graphicPanel.StartRaytracing(args.Width, args.Height, range, (result) =>
                {
                    if (result != null)
                    {
                        this.Text = result.RenderTime;

                        ImagePostProcessingHelper.SaveImageBuffer(result.RawImage, args.OutputFolder + "\\Finish.bmp", args.Tonemapping, this.graphicPanel.GlobalSettings.BrightnessFactor);

                        this.Close();
                    }
                }, (error) => { this.raytracerException = error; });
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

            string text = this.raytracingProgress.GetProgressText();
            if (text != null) this.Text = text;

            //Daten in Datei sammeln
            this.collector.HandleTimerTick();

            //Anzeigebild aktualisieren
            if (this.collector.CurrentImage != null)
            {
                this.panelWithoutFlickers1.BackgroundImage = BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>()
                {
                    BitmapHelp.TransformBitmapListToRow(new List<Bitmap>()
                    {
                        BitmapHelp.ScaleImageUp(this.collector.ReferenceImage, errorCurve.ScaleFactor),
                        BitmapHelp.ScaleImageUp(this.collector.CurrentImage, errorCurve.ScaleFactor),
                    }),
                    BitmapHelp.TransformBitmapListToRow(new List<Bitmap>()
                    {
                        BitmapHelp.ScaleImageUp(DifferenceImage.GetImage(this.collector.ReferenceImage, this.collector.CurrentImage), errorCurve.ScaleFactor),
                        this.errorCurve.PlotImageFromSingleCsvFile(this.collector.OutputCsvFile, this.graphicPanel.Mode.ToString(), this.graphicPanel.ProgressPercent / 100)
                    })
                });

                this.panelWithoutFlickers1.Invalidate();
                SetFormWidthHeight();
            }            
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.P) this.graphicPanel.GetScreenShoot().Save("ScreenShoot.bmp");
            if (keyData == Keys.S) this.graphicPanel.SaveCurrentRaytracingDataToFolder();
            if (keyData == Keys.Enter) this.graphicPanel.StopRaytracing();

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void DataCollector_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.graphicPanel.StopRaytracing();
        }

        private void CollectImageConvergenceData_Resize(object sender, EventArgs e)
        {
            SetFormWidthHeight();
        }

        private void SetFormWidthHeight()
        {
            if (this.panelWithoutFlickers1.BackgroundImage == null) return;

            float aspectRatio = this.panelWithoutFlickers1.BackgroundImage.Width / (float)this.panelWithoutFlickers1.BackgroundImage.Height;

            this.Height = (int)((this.Width + 16) / aspectRatio - 39);            
        }
    }
}
