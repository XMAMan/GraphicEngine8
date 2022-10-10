using System.Windows.Forms;

namespace GraphicPanels
{
    public class PanelWithoutFlickers : Panel
    {
        public PanelWithoutFlickers()
        {
            //Diese Anweisungen sorgen dafür, dass das DirectX-Panel nicht flimmert
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.Opaque, true);
        }
    }
}
