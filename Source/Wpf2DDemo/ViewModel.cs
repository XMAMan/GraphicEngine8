using GraphicMinimal;
using GraphicPanels;
using System;
using System.Drawing;

namespace Wpf2DDemo
{
    internal class ViewModel
    {
        private GraphicPanel2D panel;

        public ViewModel(GraphicPanel2D panel)
        {
            this.panel = panel;

            this.panel.MouseDown += Panel_MouseDown;
            this.panel.SizeChanged += Panel_SizeChanged;
        }

        private void Panel_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.panel.ClearScreen(Color.White);
            this.panel.DrawCircle(new Pen(Color.Red, 3), new Vector2D(panel.Width / 2, panel.Height / 2), 50);
            this.panel.FlipBuffer();
        }

        private async void Panel_SizeChanged(object sender, EventArgs e)
        {
            this.panel.ClearScreen(Color.White);
            this.panel.DrawLine(Pens.Blue, new Vector2D(0, 0), new Vector2D(panel.Width, panel.Height));
            this.panel.FlipBuffer();
        }        
    }
}
