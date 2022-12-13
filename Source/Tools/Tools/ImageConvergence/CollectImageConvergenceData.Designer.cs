
namespace Tools.Tools.ImageConvergence
{
    partial class CollectImageConvergenceData
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
            this.panelWithoutFlickers1 = new GraphicPanels.PanelWithoutFlickers();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // panelWithoutFlickers1
            // 
            this.panelWithoutFlickers1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panelWithoutFlickers1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelWithoutFlickers1.Location = new System.Drawing.Point(0, 0);
            this.panelWithoutFlickers1.Name = "panelWithoutFlickers1";
            this.panelWithoutFlickers1.Size = new System.Drawing.Size(756, 447);
            this.panelWithoutFlickers1.TabIndex = 0;
            // 
            // CollectImageConvergenceData
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(756, 447);
            this.Controls.Add(this.panelWithoutFlickers1);
            this.Name = "CollectImageConvergenceData";
            this.Text = "DataCollector";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DataCollector_FormClosing);
            this.Resize += new System.EventHandler(this.CollectImageConvergenceData_Resize);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer timer1;
        private GraphicPanels.PanelWithoutFlickers panelWithoutFlickers1;
    }
}