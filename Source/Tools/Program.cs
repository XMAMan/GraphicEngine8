using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Tools.CommandLineParsing;

namespace Tools
{

    static class Program
    {
        //20.8.2021: Achtung: Unter Windows 10 gibt es unter den Anzeigeeinstellungen "Skalierung und Anordnung - Größe von Text, Apps und anderen Elementen ändern = 150% (empfohlen)"
        //Diese Einstellungen findet man unter dem Begriff "Dpi Awareness". Wegen der hohen Bildschirmauflösung ist der Text so klein. Damit
        //man noch sinnvoll arbeiten kann, hat jeder Process die Möglichkeit den Dpi-Wert abzufragen (Default ist 96 Dpi) und dann entsprechend
        //zu skalieren. Alte Anwendungen machen das aber nicht. Ich hatte nun das Problem, dass wenn ich auf OpenGL3.0 (OpenTK) umgestellt habe,
        //dann war das Grafik8-Fenster auf einmal ganz klein, da der Konstruktur von "new OpenTK.GLControl()" DpiAwarenss=DpiAware setzt.
        //Damit ich bei jeden Grafikmodus die gleiche Fenstergröße habe, setze ich hier explizit DpiAwareness=Unaware. Damit bleibt das Fenster
        //selbst beim Umstellen auf OpenGL3.0 groß. 
        //Hinweis: Man kann sich mit dem ProcessExplorer "C:\Program Files\ProcessExplorer\procexp.exe" -> Select Colum="DPI Awarness" für jeden
        //Process anzeigen lassen, wie der Dpi-Wert eingestellt ist. Dadurch konnte ich auch sehen, wie beim OpenGL-Wechsel der DPI-Wert sich änderte.
        //Quelle: https://stackoverflow.com/questions/32148151/setprocessdpiawareness-not-having-effect
        [DllImport("SHCore.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

        [DllImport("SHCore.dll", SetLastError = true)]
        private static extern void GetProcessDpiAwareness(IntPtr hprocess, out PROCESS_DPI_AWARENESS awareness);

        private enum PROCESS_DPI_AWARENESS
        {
            Process_DPI_Unaware = 0,            //Bild ist um 150% größer. Beispiel: Mein Bildviewer
            Process_System_DPI_Aware = 1,       //Bild ist ganz klein. Beispielanwendung: Paint
            Process_Per_Monitor_DPI_Aware = 2
        }

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            _ = SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.Process_DPI_Unaware); //Damit ich unter Windows 10 kein kleines OpenGL3.0-Fenster erhalte

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            CommandLineExecutor.ExecuteCommandLineAction(args);            
        }
    }
}
