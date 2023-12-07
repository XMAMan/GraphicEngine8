using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Graphic2DTest;
using GraphicMinimal;
using GraphicPanels;
using Tools.Tools;

namespace Tools
{
    public partial class Form2DTest : Form
    {
        private static string WorkingDirectory = FilePaths.DataDirectory;


        private List<Vertex2D[]> voronioPolygons = null;
        private List<Point> voronoiCellPoints = null;
        private HelperFor2D.TextureData marioTexture = null;

        public Form2DTest(string dataFolder)
        {
            InitializeComponent();

            WorkingDirectory = dataFolder + "\\";

            this.timer1.Tick += new EventHandler(Timer1_Tick);
            this.timer1.Start();

            this.graphicPanel2D.Mode = GraphicPanels.Mode2D.Direct3D_11;

            this.graphicPanel2D.MouseClick += new MouseEventHandler(GraphicPanel2D_MouseClick);

            this.graphicPanel2D.SizeChanged += GraphicPanel2D_SizeChanged;

            CreateVoronoiPolygons();          
        }

        private void GraphicPanel2D_SizeChanged(object sender, EventArgs e)
        {
            HelperFor2D.Draw2D(this.graphicPanel2D, WorkingDirectory, this.spriteNr, this.voronioPolygons, this.voronoiCellPoints, this.marioTexture, false, Matrix4x4.Ident());
        }

        void GraphicPanel2D_MouseClick(object sender, MouseEventArgs e)
        {
            this.voronoiCellPoints = GraphicPanel2D.GetRandomPointList(10, this.graphicPanel2D.Width, this.graphicPanel2D.Height);
            this.voronioPolygons = GraphicPanel2D.GetVoronoiPolygons(this.graphicPanel2D.Size, this.voronoiCellPoints);
        }

        //Erzeugt zuerst eine neue Texture-Bitmap, indem es in ein Framebuffer malt und daraus dann die Daten nimmt. Dieses Bitmap wird dann mit Voronoi in 
        //Polygone zerlegt und diese Polygone werden dann über die Polygon-Zeichenfunktion gemalt
        private void CreateVoronoiPolygons()
        {
            float yAngle = 10;

            this.marioTexture = HelperFor2D.CreateMarioTexture(this.graphicPanel2D, WorkingDirectory + "nes_super_mario_bros.png", yAngle);
            this.voronoiCellPoints = GraphicPanel2D.GetRandomPointList(10, marioTexture.Image.Width, marioTexture.Image.Height);
            this.voronioPolygons = GraphicPanel2D.GetVoronoiPolygons(marioTexture.Image.Size, this.voronoiCellPoints);

            this.voronioPolygons = this.voronioPolygons.Select(x => HelperFor2D.TransformPolygon(x, new Vector2D(340, 30))).ToList(); //Verschiebe an Position
        }

        private int spriteNr = 0;
        void Timer1_Tick(object sender, EventArgs e)
        {
            spriteNr++;
            //HelperFor2D.Draw2D(this.graphicPanel2D, WorkingDirectory, this.spriteNr, this.voronioPolygons, this.voronoiCellPoints, this.marioTexture, false);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.D1) this.graphicPanel2D.Mode = GraphicPanels.Mode2D.OpenGL_Version_1_0;
            if (keyData == Keys.D2) this.graphicPanel2D.Mode = GraphicPanels.Mode2D.OpenGL_Version_3_0;
            if (keyData == Keys.D3) this.graphicPanel2D.Mode = GraphicPanels.Mode2D.Direct3D_11;
            if (keyData == Keys.D4) this.graphicPanel2D.Mode = GraphicPanels.Mode2D.CPU;
            if (keyData == Keys.P) this.graphicPanel2D.GetScreenShoot().Save("ScreenShoot.bmp");

            if (keyData == Keys.Space) CreateVoronoiPolygons();
            
            return base.ProcessCmdKey(ref msg, keyData);
        }        
    }
}
