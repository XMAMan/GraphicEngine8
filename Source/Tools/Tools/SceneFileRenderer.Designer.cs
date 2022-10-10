namespace Tools.Tools
{
    partial class SceneFileRenderer
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
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.graphicPanel = new GraphicPanels.GraphicPanel3D();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // graphicPanel
            // 
            this.graphicPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphicPanel.Location = new System.Drawing.Point(0, 0);
            this.graphicPanel.Mode = GraphicPanels.Mode3D.CPU;
            this.graphicPanel.Name = "graphicPanel";
            this.graphicPanel.Size = new System.Drawing.Size(800, 450);
            this.graphicPanel.TabIndex = 0;
            // 
            // SceneFileRenderer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.graphicPanel);
            this.Name = "SceneFileRenderer";
            this.Text = "SceneFileRenderer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SceneFileRenderer_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer timer1;
        private GraphicPanels.GraphicPanel3D graphicPanel;
    }
}