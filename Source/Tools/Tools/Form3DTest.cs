using GraphicMinimal;
using GraphicPanels;
using System;
using System.Drawing;
using System.Windows.Forms;
using Tools.Tools.SceneEditor;

namespace Tools.Tools
{
    public partial class Form3DTest : Form
    {
        private float angle = 0;
        private readonly Panel3DProgressText fpsCounter;

        public Form3DTest()
        {
            InitializeComponent();
        }

        public Form3DTest(string dataFolder)
        {
            InitializeComponent();

            try
            {
                this.fpsCounter = new Panel3DProgressText(this.graphicPanel);

                Scenes.DataDirectory = dataFolder;
                Scenes.AddTestszene1_RingSphere(this.graphicPanel);

                //Fußboden bekommt Parallax-Effekt
                var tex = this.graphicPanel.GetObjectById(1).Color.As<ColorFromTexture>();
                this.graphicPanel.GetObjectById(1).NormalSource = new NormalFromParallax() { ParallaxMap = tex.TextureFile, TextureMatrix = tex.TextureMatrix, ConvertNormalMapFromColor = true, TexturHeightFactor = 0.07f, IsParallaxEdgeCutoffEnabled = true };
                this.graphicPanel.GetObjectById(1).DisplacementData.UseDisplacementMapping = false;

                //Feuer-Sprite
                this.graphicPanel.AddSquareXY(0.5f, 0.7f, 1, new ObjectPropertys() { Size = 1, Position = new Vector3D(5, 20, 30) * 0.1f, Orientation = new Vector3D(0, 0, 0), Color = new ColorFromTexture() { TextureFile = dataFolder + "Fire2.jpg", TextureMode = TextureMode.Clamp, TextureMatrix = Matrix3x3.SpriteMatrix(5, 3, 8) }, BlackIsTransparent = true, HasBillboardEffect = true, CanReceiveLight = false, HasStencilShadow = true });

                //Ersetze Mario2 durch Kooper
                this.graphicPanel.RemoveObjekt(7);
                int kooper = this.graphicPanel.Add3DBitmap(dataFolder + "Kooper.png", 2, new ObjectPropertys() { Position = new Vector3D(+30, 13, 3) * 0.1f, Orientation = new Vector3D(0, 70 + 90, 0), Size = 0.03f, NormalInterpolation = InterpolationMode.Flat, TextureFile = dataFolder + "Kooper.png", BrdfModel = BrdfModel.Diffus, ShowFromTwoSides = false  });

                this.graphicPanel.GlobalSettings.ShadowsForRasterizer = RasterizerShadowMode.Shadowmap;
                this.graphicPanel.Mode = Mode3D.OpenGL_Version_1_0;
                this.timer1.Start();
            }
            catch (Exception ex)
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

            if (this.graphicPanel.Mode == Mode3D.CPU)
                this.graphicPanel.DrawAndFlip();

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            this.angle += 0.1f;

            //Mario1
            this.graphicPanel.GetObjectById(6).Position.X = (float)Math.Cos(angle) * 5;
            this.graphicPanel.GetObjectById(6).Position.Z = (float)Math.Sin(angle) * 5;

            //Mario2 (7 = Mario2; 9=Kooper)
            this.graphicPanel.GetObjectById(9).Position.X = (float)Math.Cos(angle + Math.PI) * 5;
            this.graphicPanel.GetObjectById(9).Position.Z = (float)Math.Sin(angle + Math.PI) * 5;
            this.graphicPanel.GetObjectById(9).Orientation.Y = -angle * 52 + 180;

            //Feuer-Sprite
            this.graphicPanel.GetObjectById(8).Color.As<ColorFromTexture>().TextureMatrix = Matrix3x3.SpriteMatrix(5, 3, ((int)(angle * 10)) % 15);

            if (this.graphicPanel.Mode != Mode3D.CPU)
            {
                this.graphicPanel.DrawWithoutFlip();
                this.graphicPanel.DrawString(0, 10, Color.Black, 15, "Press 1=OpenGL1; 2=OpenGL3; 3=DirectX; 4=CPU");
                this.graphicPanel.FlipBuffer();

                //this.graphicPanel.DrawAndFlip();
            }

            string text = this.fpsCounter.GetProgressText();
            if (text != null) this.Text = this.graphicPanel.Mode + " " + text;
        }
    }
}
