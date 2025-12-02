using GraphicPanels;
using GraphicPanelWpf;
using System.Windows;

namespace Wpf2DDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Achtung: In der Wpf2DDeomo.csproj mus noch folgendes rein, damit man unter
            //WPF das WinForm-GraphicPanel2D nutzen kann: <UseWindowsForms>true</UseWindowsForms>
            var panel = new GraphicPanel2D() { Width = 100, Height = 100, Mode = Mode2D.OpenGL_Version_3_0 }; //Unter .NET Core kann man leider kein DirectX nutzen
            this.graphicControlBorder.Child = new GraphicControl(panel); //Sowohl die View kennt das GraphicPanel2D um es darstellen zu können

            this.DataContext = new ViewModel(panel); //Das ViewModel kennt das GraphicPanel2D auch, um Zeichenbefehle hinsenden zu können
        }
    }
}
