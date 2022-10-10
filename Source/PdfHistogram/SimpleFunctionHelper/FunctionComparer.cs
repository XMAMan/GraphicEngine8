using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RaytracingRandom;

namespace PdfHistogram
{
    public class FunctionComparer
    {
        //Rechnet die Funktionen in ein y = 0 bis 100%;x=0 bis 1 Bereich um und gibt die Differenzfläche zurück (Zahl zwischen 0 und 100)
        public static double GetMaxDifferenceAreaBetweenFunctions(double minX, double maxX, params SimpleFunction[] functions)
        {
            //minYRange = Hat man eine Equal-Sample-Funktion, dann sind die beiden Striche unendlich dicht zusammen. Deswegen muss
            //            mit diesen Parameter hier verhindert werden, dass der Error sonst ganz hoch wird obwohl die Strich doch parallel sind
            double minYRange = 1;

            return GetMaxDifferenceAreaBetweenFunctions(minX, maxX, minYRange, functions);
        }

        //Rechnet die Funktionen in ein 0 bis 100% Bereich um und gibt die Differenz als Prozentzahl(0 bis 100) zurück
        public static double GetMaxDifferenceAreaBetweenFunctions(double minX, double maxX, double minYRange, params SimpleFunction[] functions)
        {
            int comparePointCount = 401; //Mit so vielen Punkten werden beide Funktionen abgetastet und verglichen

            double[] samplePointErrors = new double[comparePointCount];
                       
            double minY = double.MaxValue, maxY = double.MinValue, maxDifference = double.MinValue;
            for (int i=0;i<comparePointCount;i++)
            {
                double x = i / (double)comparePointCount * (maxX - minX) + minX;

                double localMinY = double.MaxValue, localMaxY = double.MinValue, localDifference = double.NaN;
                for (int j=0;j<functions.Length;j++)
                {
                    double y = functions[j](x);
                    if (y < localMinY) localMinY = y;
                    if (y > localMaxY) localMaxY = y;
                }
                localDifference = localMaxY - localMinY;

                samplePointErrors[i] = localDifference;

                if (localDifference > maxDifference) maxDifference = localDifference;
                if (localMinY < minY) minY = localMinY;
                if (localMaxY > maxY) maxY = localMaxY;
            }

            double dx = (maxX - minX) / comparePointCount;
            //double xScale = 1.0 / (maxX - minX); //Normiere die Breite auf 1
            double yScale = 100 / Math.Max(minYRange, (maxY - minY)); //Normiere die Höhe auf 100
            int relativeError = (int)samplePointErrors.Sum(y => (dx * y * yScale)); //Gebe Zahl zwischen 0 und 100 zurück
            int absolutError = (int)(samplePointErrors.Sum(y => (dx * y)) * 100);
            return relativeError;

            //int[] errorValues = samplePointErrors.Select(y => (int)(y * yScale)).ToArray();
            //int maxError = errorValues.Max();
            //return maxError; //Gebe Zahl zwischen 0 und 100 zurück
            //return maxDifference * scale; //Gebe Zahl zwischen 0 und 100 zurück
        }
    }
}
