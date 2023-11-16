namespace GraphicPanelWpf
{
    //Da die Ansicht zwischen den Editor und Simulator ungeschaltet werden kann und ich immer nur beim aktiven UserControl die 
    //Maus- und Tastaturereignisse verarbeiten will, habe ich dieses Interface, was vom MainWindowViewModel verwendet wird (Er ist die Weiche)
    public interface IGraphicPanelHandler : ISizeChangeable
    {
        void HandleMouseClick(System.Windows.Forms.MouseEventArgs e);
        void HandleMouseWheel(System.Windows.Forms.MouseEventArgs e);
        void HandleMouseMove(System.Windows.Forms.MouseEventArgs e);
        void HandleMouseDown(System.Windows.Forms.MouseEventArgs e);
        void HandleMouseUp(System.Windows.Forms.MouseEventArgs e);
        void HandleKeyDown(System.Windows.Input.KeyEventArgs e);
        void HandleKeyUp(System.Windows.Input.KeyEventArgs e);
    }

    public interface ISizeChangeable
    {
        void HandleSizeChanged(int width, int height);
    }
}
