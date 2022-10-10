using System;

namespace RaytracingRandom
{
    public delegate double SimpleFunction(double xValue);

    //Erzeugt eine Zufallzahl mit einer per Delegate übergenenen Pdf. Man kann auch die CDF von dieser Pdf angeben, um die Zufallszahl zu erzeugen
    //Wenn man lediglich die Pdf hat, dann sind die Ergebnisse nicht so genau wie wenn man die CDF als Delegate reinreicht
    //Diese Klasse nimmt man immer dann, wenn man analytisch nicht die inverse von der CDS bilden kann. Die Inverse wird hier über binäre Suche gemacht
    public class PdfWithTableSampler
    {       
        private double[] cdf;
        private double[] xValues;
        private double[] pdfValues; //Normalisierte Y-Werte == Pdf
        private double minXValue;
        private double maxXValue;
        private double deltaX;
        private bool useInterpolationForPdfFunction;

        public double NormalisationConstant { get; private set; } = 1;

        private PdfWithTableSampler(double[] cdf, double[] xValues, double[] pdfValues, double minXValue, double maxXValue, double deltaX, bool useInterpolationForPdfFunction)
        {
            this.cdf = cdf;
            this.xValues = xValues;
            this.pdfValues = pdfValues;
            this.minXValue = minXValue;
            this.maxXValue = maxXValue;
            this.deltaX = deltaX;
            this.useInterpolationForPdfFunction = useInterpolationForPdfFunction;

        }

        //Es gilt: f(x) = yValues[x] mit x = 0 .. yValues.Lenght - 1 (Funktion besteht aus lauter Rechtecken mit Sprungstellen)
        //Beim Sampeln erhalte ich über GetXValue(u) dann eine Double-Zahl, welche von 0 bis yValues.Lenght - 1 geht
        public static PdfWithTableSampler CreateFromUnnormalisizedFunctionArray(double[] yValues)
        {
            double[] cdf_ = new double[yValues.Length + 1];
            double[] xValues_ = new double[yValues.Length + 1]; //Die Abtastpunkte liegen an der rechten Kante; Der 1. Abtastpunkt (Linke Kante vom ersten Kästchen) ist gleich dem letzten Abtastpunkte (Rechte Kante vom letzten Kästchen)
            double[] pdfValues = new double[yValues.Length + 1];
            double cdfSum = 0;

            xValues_[0] = 0;
            cdf_[0] = 0;
            pdfValues[0] = yValues[yValues.Length - 1]; //Erstes Kästchen Linke Kante entspricht letzten Kästchen rechte Kante

            for (int i = 1; i <= yValues.Length; i++)
            {
                xValues_[i] = i;
                pdfValues[i] = yValues[i - 1]; //Im yValues steht immer jeweils der Wert von der rechten Kante
                cdfSum += pdfValues[i];
                cdf_[i] = cdfSum;               
            }

            double normalisationConstant = cdf_[cdf_.Length - 1];

            if (double.IsInfinity(normalisationConstant) || double.IsNaN(normalisationConstant)) throw new Exception("PdfWithTableSampler Infinity/NaN-Problem");

            for (int i = 0; i < cdf_.Length; i++)
            {
                cdf_[i] /= normalisationConstant;
                pdfValues[i] /= normalisationConstant;
            }
            return new PdfWithTableSampler(cdf_, xValues_, pdfValues, 0, yValues.Length, 1, false) { NormalisationConstant = normalisationConstant };

        }

        public static PdfWithTableSampler CreateFromUnnormalisizedFunction(SimpleFunction simpleFunction, double minXValue, double maxXValue, int tableEntrys)
        {
            double[] cdf_ = new double[tableEntrys + 1];
            double[] xValues_ = new double[tableEntrys + 1];
            double[] pdfValues = new double[tableEntrys + 1];
            double cdfSum = 0;

            double deltaX_ = (maxXValue - minXValue) / tableEntrys;

            xValues_[0] = minXValue; //Linke Kante
            cdf_[0] = 0;
            pdfValues[0] = simpleFunction(minXValue);

            for (int i = 1; i <= tableEntrys; i++) //Gehe über alle linken Kanten
            {
                xValues_[i] = minXValue + i * deltaX_; //Linke Kante
                pdfValues[i] = simpleFunction(xValues_[i] - deltaX_ / 2);
                cdfSum += pdfValues[i] * Math.Abs(deltaX_); //Mittelpunkt vom vorherigen Kästchen
                cdf_[i] = cdfSum;   //Linke Kante                
            }

            double normalisationConstant = cdf_[cdf_.Length - 1];

            if (minXValue == maxXValue) throw new Exception("Min und Max müssen auseinander liegen");
            if (double.IsInfinity(normalisationConstant) || double.IsNaN(normalisationConstant)) throw new Exception("PdfWithTableSampler Infinity/NaN-Problem");

            for (int i = 0; i < cdf_.Length; i++)
            {
                cdf_[i] /= normalisationConstant;
                pdfValues[i] /= normalisationConstant;
            }
            return new PdfWithTableSampler(cdf_, xValues_, pdfValues, minXValue, maxXValue, deltaX_, true) { NormalisationConstant = normalisationConstant };
        }

        public static PdfWithTableSampler CreateFromPdf(SimpleFunction pdf, double minXValue, double maxXValue, int tableEntrys)
        {
            double[] cdf_ = new double[tableEntrys + 1];
            double[] xValues_ = new double[tableEntrys + 1];
            double[] pdfValues = new double[tableEntrys + 1];
            double cdfSum = 0;

            double deltaX_ = (maxXValue - minXValue) / tableEntrys;

            xValues_[0] = minXValue; //Linke Kante
            cdf_[0] = 0;
            pdfValues[0] = pdf(minXValue);

            for (int i = 1; i <= tableEntrys; i++)
            {
                xValues_[i] = minXValue + i * deltaX_;
                pdfValues[i] = pdf(xValues_[i] - deltaX_ / 2);
                cdfSum += pdfValues[i] * Math.Abs(deltaX_);
                cdf_[i] = cdfSum;                
            }

            return new PdfWithTableSampler(cdf_, xValues_, pdfValues, minXValue, maxXValue, deltaX_, true);
        }

        public static PdfWithTableSampler CreateFromCdf(SimpleFunction cdf, double minXValue, double maxXValue, int tableEntrys)
        {
            double[] cdf_ = new double[tableEntrys + 1];
            double[] xValues_ = new double[tableEntrys + 1];
            double[] pdfValues = null;

            double deltaX_ = (maxXValue - minXValue) / tableEntrys;

            for (int i = 0; i <= tableEntrys; i++)
            {
                xValues_[i] = minXValue + i * deltaX_;
                cdf_[i] = cdf(xValues_[i]);
            }

            return new PdfWithTableSampler(cdf_, xValues_, pdfValues, minXValue, maxXValue, deltaX_, true);
        }

        public double PdfValue(double x)
        {
            double indexD = (x - this.minXValue) / (this.maxXValue - this.minXValue) * (this.cdf.Length - 1);
            int index = Math.Min((int)indexD, this.cdf.Length - 2);
            if (this.useInterpolationForPdfFunction)
            {
                double u = indexD - index;
                return (1 - u) * this.pdfValues[index] + u * this.pdfValues[index + 1];
            }
            else
            {
                return this.pdfValues[index + 1];
            }           
        }

        //x muss im Bereich von minXValue und maxXValue liegen
        public double CdfValue(double x)
        {
            double indexD = (x - this.minXValue) / (this.maxXValue - this.minXValue) * (this.cdf.Length - 1);
            int index = Math.Min((int)indexD, this.cdf.Length - 2);
            double u = indexD - index;
            return (1 - u) * this.cdf[index] + u * this.cdf[index + 1];
        }

        public double IntegralFromMinXValueToX(double x)
        {
            return CdfValue(x) * this.NormalisationConstant;
        }


        //Transformiert ein CDF-Wert in ein X-Wert. Das ist also die inverse CDF.
        //u = Zahl im Bereich von 0 bis 1 (das ist ein CDF-Wert)
        public double GetXValue(double u)
        {
            if (u == 1) return this.maxXValue;

            int index = BinarySeach(u);

            double f = (u - this.cdf[index]) / (this.cdf[index + 1] - this.cdf[index]);
            return this.xValues[index] + f * this.deltaX;
        }       

        private int BinarySeach(double u)
        {
            int index = -1;
            int l = 0;
            int r = this.cdf.Length - 1;

            while (l <= r)
            {
                index = (int)((l + r) / 2);
                if (this.cdf[index] < u)
                    l = index + 1;
                else if (this.cdf[index] > u)
                    r = index - 1;
                else
                    return index;
            }

            if (this.cdf[index] > u)
                return index - 1;
            else
                return index;
        }
    }
}
