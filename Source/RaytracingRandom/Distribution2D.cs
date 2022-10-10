using System;

namespace RaytracingRandom
{
    //Sampelt den X- und Y-Index innerhalb der xyMap, welche eine Unnormalisierte Pmf-Funktion ist
    public class Distribution2D
    {
        public struct Sample
        {
            public double X; //Geht von 0 bis xyMap.GetLength(0)
            public double Y; //Geht von 0 bis xyMap.GetLength(1)
            public double PdfA; //Pdf im Bezug auf die Fläche xyMap.GetLength(0) * xyMap.GetLength(1)
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        private PdfWithTableSampler selectRowIndexSampler; //Y-Wert Sampler
        private PdfWithTableSampler[] sampleXInRow;        //X-Wert Sampler

        public Distribution2D(double[,] xyMap)
        {
            this.sampleXInRow = new PdfWithTableSampler[xyMap.GetLength(1)];

            double[] marginPdf = new double[this.sampleXInRow.Length];
            for (int y=0;y<xyMap.GetLength(1);y++)
            {
                double[] row = new double[xyMap.GetLength(0)];
                for (int x = 0; x < row.Length; x++) row[x] = xyMap[x, y];
                this.sampleXInRow[y] = PdfWithTableSampler.CreateFromUnnormalisizedFunctionArray(row);
                marginPdf[y] = this.sampleXInRow[y].NormalisationConstant;
            }
            this.selectRowIndexSampler = PdfWithTableSampler.CreateFromUnnormalisizedFunctionArray(marginPdf);

            this.Width = xyMap.GetLength(0);
            this.Height = xyMap.GetLength(1);
        }

        public Sample SampleXYIndex(double u1, double u2)
        {
            double y = this.selectRowIndexSampler.GetXValue(u1);
            int yIndex = Math.Min((int)y, this.sampleXInRow.Length - 1);
            double x = this.sampleXInRow[yIndex].GetXValue(u2);

            double yPdf = this.selectRowIndexSampler.PdfValue(y);
            double xPdf = this.sampleXInRow[yIndex].PdfValue(x); //Ohne Interpolation von xPdf
            double pdfA = xPdf * yPdf;
            
            
            return new Sample() { X = x, Y = y, PdfA = pdfA };
        }

        //x = 0 .. xyMap.GetLength(0); y = 0 .. xyMap.GetLength(1)
        public double Pdf(double x, double y)
        {
            double yPdf = this.selectRowIndexSampler.PdfValue(y);

            int yIndex = Math.Min((int)y, this.sampleXInRow.Length - 1);
            double xPdf = this.sampleXInRow[yIndex].PdfValue(x); //Ohne Interpolation von xPdf

            return xPdf * yPdf;
        }
    }
}
