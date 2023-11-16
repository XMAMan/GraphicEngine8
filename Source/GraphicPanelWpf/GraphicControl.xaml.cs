using GraphicPanels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GraphicPanelWpf
{
    /// <summary>
    /// Interaktionslogik für GraphicControl.xaml
    /// </summary>
    public partial class GraphicControl : System.Windows.Controls.UserControl
    {
        public GraphicControl()
        {
            InitializeComponent();
        }

        public GraphicControl(GraphicPanel2D panel)
           : this()
        {
            this.panel = panel;
        }

        private GraphicPanel2D panel;

        //Schritte um das GraphicPanel2D in WPF unter .NET 6 zu nutzen
        //1. Die csproj-Datei in Notepad++ öffnen und <UseWindowsForms>true</UseWindowsForms> einfügen damit ich das WindowsFormsHost nutzen kann
        //Quelle für den Tipp: https://stackoverflow.com/questions/57908184/using-system-windows-forms-classes-in-a-net-core-3-0-preview9-project
        //2. Für Window den Loaded-Handler einfügen und dort das WindowsFormsHost-Objekt nutzen
        //3. Erst kam beim Laden der GraphicPanels.dll bei Programstart eine BadImageFormat-Exception. Um rauszufinden, wo das Problem liegt
        //   habe ich mit Assembly.LoadFile alle Dll-Dateien aus dem GraphicPanels-bin-Ordner geladen und so rausgefunden, dass
        //   SlimDX.dll nicht geht und durch Probieren habe ich noch rausgefunden, dass ich im GraphicPanels den Ausgabetyp von x86 nach AnyCPU umstellen musste
        //   damit .net core die alte .net framework-Dll laden kann.
        //https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/walkthrough-hosting-a-windows-forms-control-in-wpf?view=netframeworkdesktop-4.8

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Create the interop host control.
            System.Windows.Forms.Integration.WindowsFormsHost host =
                new System.Windows.Forms.Integration.WindowsFormsHost();

            // Assign the MaskedTextBox control as the host control's child.
            host.Child = this.panel;

            // Add the interop host control to the Grid
            // control's collection of child controls.
            this.graphicGrid.Children.Add(host);
        }
    }
}
