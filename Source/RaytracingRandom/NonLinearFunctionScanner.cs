using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaytracingRandom
{
    public class ScanPosition
    {
        public int Index;
        public double F; //Geht von 0 bis 1 (0=ScanPunkt liegt bei XValues[Index]; 1=ScanPunt liegt bei XValues[Index+1])
    }

    //Taste eine Funktion laut einer unnormalisierten Pdf ab. D.h. es werden nur die X-Werte von den Abtastpunkten bereit gestellt
    //Da wo die Funktion größer ist, sind dann auch mehr Abtastpunkte. Erklärbild -> Siehe zugehöriger Unittest
    //Idee: Ich platziere an den Abtastpunkten, welche über XValues[] angegeben werden eine Funktion, dessen Returnwert
    //ich dann zwischen den Abtastpunkten interpoliere
    public class NonLinearFunctionScanner
    {
        public double[] XValues { get; private set; }

        private PdfWithTableSampler tableCdf;
        private double minX;
        private double maxX;

        //Erstellt samplePointCount X-Werte im Bereich von minX bis maxX laut der Pdf von unnormalisizedPdfFunction
        //Der erste und der letzte X-Wert liegt auf minX/maxX. Somit ist die kleinste erlaubte Zahl für samplePointCount zwei
        public NonLinearFunctionScanner(SimpleFunction unnormalisizedPdfFunction, double minX, double maxX, int samplePointCount)
        {
            if (samplePointCount < 2) throw new ArgumentException("samplePointCount muss mindestens zwei sein");

            this.minX = minX;
            this.maxX = maxX;

            this.tableCdf = PdfWithTableSampler.CreateFromUnnormalisizedFunction(unnormalisizedPdfFunction, minX, maxX, 1000);
            
            //Taste den CDF-Wert gleichmäßig ab, um X-Werte zu erhalten, welcher laut der Pdf verteilt sind
            this.XValues = new double[samplePointCount];
            for (int i=0;i<this.XValues.Length;i++)
            {
                double cdf = i / (double)(samplePointCount - 1);
                double x = this.tableCdf.GetXValue(cdf);
                this.XValues[i] = x;
            }
        }

        //xValue muss im Bereich von minX bis maxX liegen
        //xValue liegt im Segment index bis (index+1) und f (Zahl von 0 bis 1) gibt an, wo dazwischen
        //Zum Ausrechnen des genauen X-Wertes muss man dann "x = (1-f) * this.XValues[index] + f * this.XValues[index+1]" rechnen
        public ScanPosition GetScanPosition(double xValue)
        {
            if (xValue == this.maxX) return new ScanPosition() { Index = this.XValues.Length - 2, F = 1 };

            double cdf = this.tableCdf.CdfValue(xValue);
            double indexD = cdf * (this.XValues.Length - 1);
            int index = Math.Min((int)indexD, this.XValues.Length - 2);
            double f = indexD - index;
            return new ScanPosition() { Index = index, F = f }; 
        }
    }
}
