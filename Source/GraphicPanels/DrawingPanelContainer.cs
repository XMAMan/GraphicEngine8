using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GraphicGlobal;

namespace GraphicPanels
{
    //Hat eine Menge von Zeichenflächen, welche per Lazy Loading erzeugt werden
    class DrawingPanelContainer
    {
        private Dictionary<Mode3D, IDrawingPanel> panels = new Dictionary<Mode3D, IDrawingPanel>();

        private Action<Control> controlWasCreated;
        private DrawingPanelFactory drawingPanelFactory = new DrawingPanelFactory();

        public DrawingPanelContainer(Action<Control> controlWasCreated)
        {
            this.controlWasCreated = controlWasCreated;
        }

        public IDrawingPanel GetPanel(Mode3D modus)
        {
            if (this.panels.ContainsKey(modus) == false)
            {
                var newDrawingPanel = this.drawingPanelFactory.CreateDrawingPanel(modus);
                this.panels.Add(modus, newDrawingPanel);
                controlWasCreated(newDrawingPanel.DrawingControl);
            }

            return this.panels[modus];
        }

        public IDrawingPanel GetPanel(Mode2D mode)
        {
            var modus3D = TranslateMode2DInto3D(mode);

            if (this.panels.ContainsKey(modus3D) == false)
            {
                var newDrawingPanel = this.drawingPanelFactory.CreateDrawingPanel(mode); //Ich nutze hier die CreateDrawingPanel-Funktion mit Mode2D da so die ganzen anderen Dlls nicht mit geladen werden
                this.panels.Add(modus3D, newDrawingPanel);
                controlWasCreated(newDrawingPanel.DrawingControl);
            }

            return this.panels[modus3D];
        }

        private Mode3D TranslateMode2DInto3D(Mode2D mode)
        {
            switch (mode)
            {
                case Mode2D.OpenGL_Version_1_0:
                    return Mode3D.OpenGL_Version_1_0;
                case Mode2D.OpenGL_Version_3_0:
                    return Mode3D.OpenGL_Version_3_0;
                case Mode2D.Direct3D_11:
                    return Mode3D.Direct3D_11;
                case Mode2D.CPU:
                    return Mode3D.CPU;
                default:
                    throw new Exception("Unknown Mode2D: " + mode);
            }
        }

        public IEnumerable<Control> GetAllLoadedControls()
        {
            return this.panels.Values.Select(x => x.DrawingControl);
        }
    }
}
