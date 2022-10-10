using System;
using System.Windows.Forms;
using Tools.Tools.SceneEditor;

namespace Tools.Tools.MasterTestImage
{
    //Commandline-Arguments im VisualStudio: MasterTest Normal -size 4 -dataFolder ..\..\..\..\Data\
    internal partial class MasterTestForm : Form
    {
        private MasterTest masterTest = null;
        private Exception raytracerException = null;

        public MasterTestForm(MasterTest.Accuracy quality, int size, string dataFolder)
        {
            InitializeComponent();

            Scenes.DataDirectory = dataFolder;

            //MasterTest.accuracy = MasterTest.Accuracy.High;
            MasterTest.Quality = quality;
            MasterTest.Area = MasterTest.ImageArea.All;

            switch (size)
            {
                case 1:
                    this.masterTest = new MasterTest(42, 33, (bild) => { bild.Save("MasterTestImage.bmp"); }, (error) => { this.raytracerException = error; }); this.masterTest.GetResultAsynchron();
                    break;

                case 2:
                    this.masterTest = new MasterTest(84, 66, (bild) => { bild.Save("MasterTestImage.bmp"); }, (error) => { this.raytracerException = error; }); this.masterTest.GetResultAsynchron(); 
                    break;

                case 3:
                    this.masterTest = new MasterTest(210, 164, (bild) => { bild.Save("MasterTestImage.bmp"); }, (error) => { this.raytracerException = error; }); this.masterTest.GetResultAsynchron(); 
                    break;

                case 4:
                    this.masterTest = new MasterTest(420, 328, (bild) => { bild.Save("MasterTestImage.bmp"); }, (error) => { this.raytracerException = error; }); this.masterTest.GetResultAsynchron(); 
                    break;

                case 5:
                    this.masterTest = new MasterTest(630, 492, (bild) => { bild.Save("MasterTestImage.bmp"); }, (error) => { this.raytracerException = error; }); this.masterTest.GetResultAsynchron();
                    break;
            }

            this.timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.raytracerException != null) throw new Exception(this.raytracerException.ToString(), this.raytracerException);

            if (this.masterTest != null)
            {
                this.Text = this.masterTest.ProgressText;
                return;
            }
        }
    }
}
