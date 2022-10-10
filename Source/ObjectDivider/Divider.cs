using System.Collections.Generic;
using System.Linq;
using GraphicGlobal;

namespace ObjectDivider
{
    //Unterteilt eine Liste von Dreiecken und Vierecken so lange, bis die Abbruchbedingung (Callback von außen) erfüllt ist
    public static class Divider
    {
        public delegate bool NoMoreDividePlease(IDivideable source, IDivideable divideable); //source = Objekt aus der ursprünglichen divideables-Liste. Also das 'Master'-Objekt

        public static List<IDivideable> Subdivide(IDivideable divideable, NoMoreDividePlease noMoreDividePlease)
        {
            return Subdivide(divideable, divideable, noMoreDividePlease);
        }

        public static List<IDivideable> Subdivide(IEnumerable<IDivideable> divideables, NoMoreDividePlease noMoreDividePlease)
        {
            return divideables.SelectMany(x => Subdivide(x, x, noMoreDividePlease)).ToList();
        }

        private static List<IDivideable> Subdivide(IDivideable sourceDividable, IDivideable divideable, NoMoreDividePlease noMoreDividePlease)
        {
            if (noMoreDividePlease(sourceDividable, divideable))
                return new List<IDivideable>() { divideable };
            else
            {
                var divideables = divideable.Divide();
                return divideables.SelectMany(x => Subdivide(sourceDividable, x, noMoreDividePlease)).ToList();
            }
        }
    }
}
