using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfHistogram;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaytracingBrdfTest
{
    
    [TestClass]
    public class BrdfFunctionPlotter
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        //Ich plotte hier nur testweise mal etwas um die Glanzpunktfunktion besser zu verstehen
        [TestMethod]
        public void PlotGlanzpunkt()
        {
            List<FunctionToPlot> functions = new List<FunctionToPlot>();
            functions.Add(new FunctionToPlot() { Function = BrdfGlanzpunkt, Color = Color.Blue, Text = "Brdf-Glanzpunkt" });
            
            FunctionPlotter plotter = new FunctionPlotter(0, 1, new Size(400, 300));

            plotter.PlotFunctions(functions).Save(WorkingDirectory + "BrdfGlanzpunkt.bmp");
        }

        private double BrdfGlanzpunkt(double x)
        {
            float GlanzPunktGroese = 30;
            float GlanzPunktCutoff1 = 10;// 1.2f;
            float GlanzPunktCutoff2 = 10;

            float dot_R_wi = (float)x;// perfektReflection * lightGoingOutDirection;
            if (dot_R_wi <= 1e-6f) return 0;

            float rho = (GlanzPunktGroese + 2) * 0.5f / (float)Math.PI;
            //float pow = Math.Min(GlanzPunktCutoff1, rho * (float)Math.Pow(dot_R_wi, GlanzPunktGroese));
            //if (pow < 1) pow = 0;
            //float f = pow / GlanzPunktCutoff2; ;
            float f = Math.Min(GlanzPunktCutoff1, rho * (float)Math.Pow(dot_R_wi, GlanzPunktGroese)) / GlanzPunktCutoff2;
            //float f = rho * (float)Math.Pow(dot_R_wi, GlanzPunktGroese);

            return f;
        }
    }
}
