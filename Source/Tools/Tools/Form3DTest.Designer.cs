
namespace Tools.Tools
{
    partial class Form3DTest
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.graphicPanel = new GraphicPanels.GraphicPanel3D();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // graphicPanel3D1
            // 
            this.graphicPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphicPanel.Location = new System.Drawing.Point(0, 0);
            this.graphicPanel.Mode = GraphicPanels.Mode3D.CPU;
            this.graphicPanel.Name = "graphicPanel3D1";
            this.graphicPanel.Size = new System.Drawing.Size(800, 450);
            this.graphicPanel.TabIndex = 0;
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // Form3DTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.graphicPanel);
            this.Name = "Form3DTest";
            this.Text = "Form3DTest";
            this.ResumeLayout(false);

        }

        #endregion

        private GraphicPanels.GraphicPanel3D graphicPanel;
        private System.Windows.Forms.Timer timer1;
    }
}