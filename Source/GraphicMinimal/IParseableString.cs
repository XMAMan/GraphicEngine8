namespace GraphicMinimal
{
    //Hiermit kann ein Objekt in ein C#-Konstruktoraufruf übersetzt werden
    //Wenn ich ein Fehler beim KD-Baum nachstellen will, wo ich als Input viele Dreiecke benötige, dann kann
    //ich mit diesen Interface mir leicht die Testdaten erzeugen indem ich im IntersectionFinder ToCtorString aufrufe
    public interface IParseableString
    {
        string ToCtorString(); //ctor = ConstrucTOR; [ctor + press tab twice] = default constructor.
    }

    public static class ParseableStringExtension
    {
        public static string ToCtorString(this IParseableString obj)
        {
            if (obj == null) return "null";
            return obj.ToCtorString();
        }

        public static string ToFloatString(this float f)
        {
            return f.ToString("G9").Replace(",", ".") + "f";
        }
    }
}
