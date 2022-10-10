namespace Tools.Tools.SceneEditor
{
    partial class SceneEditor
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.graphicPanel = new GraphicPanels.GraphicPanel3D();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // grafikPanel3D1
            // 
            this.graphicPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphicPanel.Location = new System.Drawing.Point(0, 0);
            this.graphicPanel.Mode = GraphicPanels.Mode3D.CPU;
            this.graphicPanel.Name = "grafikPanel3D1";
            this.graphicPanel.Size = new System.Drawing.Size(420, 328);
            this.graphicPanel.TabIndex = 0;
            this.graphicPanel.MouseClick += new System.Windows.Forms.MouseEventHandler(this.GraphicPanel_MouseClick);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(420, 328);
            this.Controls.Add(this.graphicPanel);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private GraphicPanels.GraphicPanel3D graphicPanel;
        private System.Windows.Forms.Timer timer1;
    }
}

