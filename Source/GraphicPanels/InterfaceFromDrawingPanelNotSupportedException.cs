using System;

namespace GraphicPanels
{
    //Diese Exception wird geworfen, wenn von ein DrawingPanel erwartet wird, dass es ein bestimmtes Interface implementiert obwohl es das nicht kann
    public class InterfaceFromDrawingPanelNotSupportedException : Exception
    {
        public Type InterfaceNotSupported { get; private set; }
        public string GraphicMode { get; private set; }

        public InterfaceFromDrawingPanelNotSupportedException(Type interfaceNotSupported, string graphicMode)
        {
            this.InterfaceNotSupported = interfaceNotSupported;
            this.GraphicMode = graphicMode;
        }

        public InterfaceFromDrawingPanelNotSupportedException(Type interfaceNotSupported, string graphicMode, string message)
            : base(message)
        {
            this.InterfaceNotSupported = interfaceNotSupported;
            this.GraphicMode = graphicMode;
        }

        public InterfaceFromDrawingPanelNotSupportedException(Type interfaceNotSupported, string graphicMode, string message, Exception inner)
            : base(message, inner)
        {
            this.InterfaceNotSupported = interfaceNotSupported;
            this.GraphicMode = graphicMode;
        }
    }
}
