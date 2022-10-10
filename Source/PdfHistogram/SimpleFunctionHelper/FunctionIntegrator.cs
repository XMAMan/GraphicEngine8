using RaytracingRandom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfHistogram
{
    public class FunctionIntegrator
    {
        public static double IntegrateWithRieman(SimpleFunction function, double minX, double maxX)
        {
            int stepCount = 100;
            double distance = maxX - minX;
            double stepWidth = distance / stepCount;
            double sum = 0;
            for (int i=0;i<stepCount;i++)
            {
                sum += function(minX + stepWidth * i + stepWidth / 2) * stepWidth;
            }
            return sum;
        }
    }
}
