using BitmapHelper;
using GraphicPanels;
using System;
using System.Drawing;

namespace Tools.Tools.ImageConvergence
{
    class DataCollectorConstructorData
    {
        public GraphicPanel3D GraphicPanel;
        public Bitmap ReferenceImage;
        public int CollectRate;         //Aller so viele Sekunden wird ein Datenpunkt (CSV-Zeile) aufgezeichnet
        public int ProgressImageRate = 1;   //Aller CollectRate Datenpunkte wird ein ProgressImage erzeugt. 1 Bedeutet pro CSV-Zeile wird auch ein Bild erzeugt
        public string OutputFolder;
    }

    class DataCollector
    {
        private DataCollectorConstructorData data;
        
        private DateTime startTime;
        private int csvLineCounter = 0;
        

        public Bitmap ReferenceImage { get => this.data.ReferenceImage; }
        public Bitmap CurrentImage { get; private set; }
        public readonly CsvFile OutputCsvFile;

        public DataCollector(DataCollectorConstructorData data)
        {
            this.data = data;
            this.OutputCsvFile = new CsvFile(data.OutputFolder + "\\Output.csv");
        }

        public void Start()
        {
            this.startTime = DateTime.Now;
        }

        public void HandleTimerTick()
        {
            double secondsToStart = (DateTime.Now - this.startTime).TotalSeconds;
            if (secondsToStart >= this.csvLineCounter * this.data.CollectRate)
            {
                this.data.GraphicPanel.UpdateProgressImage();
                this.CurrentImage = this.data.GraphicPanel.GetScreenShoot();
                if (this.CurrentImage == null) return; //Wenn das Bild am Anfang noch nicht zurück gegeben werden kann dann mache garnichts

                UInt32 timeToStart = (UInt32)secondsToStart;
                this.OutputCsvFile.AddLine(new CsvFile.Line(timeToStart, (byte)(DifferenceImage.GetDifference(this.data.ReferenceImage, this.CurrentImage) * 100)));

                if (this.csvLineCounter % this.data.ProgressImageRate == 0)
                {
                    string imageFileName = $"{timeToStart}_Progress.bmp";
                    this.CurrentImage.Save(this.data.OutputFolder + "\\" + imageFileName);
                }
                this.csvLineCounter++;
            }
        }
    }
}
