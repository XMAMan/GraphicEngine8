namespace GraphicPanelWpf
{
    //Es gibt ein GraphicPanel2D, von mehreren GraphicControls/ViewModels genutzt werden kann.
    //Mit diesen Interface können dann die GraphicPanel2D-Events an das ViewModel weitergeleitet werden,
    //was gerade aktiv ist.
    public interface IGraphicPanelHandler : ISizeChangeable
    {
        void HandleMouseClick(System.Windows.Forms.MouseEventArgs e);
        void HandleMouseWheel(System.Windows.Forms.MouseEventArgs e);
        void HandleMouseMove(System.Windows.Forms.MouseEventArgs e);
        void HandleMouseDown(System.Windows.Forms.MouseEventArgs e);
        void HandleMouseUp(System.Windows.Forms.MouseEventArgs e);
        void HandleMouseEnter();
        void HandleMouseLeave();
        void HandleKeyDown(System.Windows.Input.KeyEventArgs e);
        void HandleKeyUp(System.Windows.Input.KeyEventArgs e);
    }

    public interface ISizeChangeable
    {
        void HandleSizeChanged(int width, int height);
    }
}
