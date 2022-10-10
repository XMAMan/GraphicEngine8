namespace Tools
{
    partial class Form2DTest
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
            this.graphicPanel2D = new GraphicPanels.GraphicPanel2D();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // grafikPanel2D1
            // 
            this.graphicPanel2D.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphicPanel2D.Location = new System.Drawing.Point(0, 0);
            this.graphicPanel2D.Mode = GraphicPanels.Mode2D.CPU;
            this.graphicPanel2D.Name = "grafikPanel2D1";
            this.graphicPanel2D.Size = new System.Drawing.Size(477, 359);
            this.graphicPanel2D.TabIndex = 0;
            // 
            // Form2DTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(477, 359);
            this.Controls.Add(this.graphicPanel2D);
            this.Name = "Form2DTest";
            this.Text = "Form2DTest";
            this.ResumeLayout(false);

        }

        #endregion

        private GraphicPanels.GraphicPanel2D graphicPanel2D;
        private System.Windows.Forms.Timer timer1;

    }
}