namespace RayTracerGlobal
{
    //Nach und nach will ich hier alle Magic-Numbers aus mein Raytracer sammeln und in Rand-Fall-Unittest abdecken
    //Wenn man an diesen Zahlen rumspielt, dann hilft aus dem FullPathSampler der Compare_MinMaxPathWeightsAndPdfAs-Test um die resultierenden Min-Max-Werte zu sehen
    public static class MagicNumbers
    {
        public const float MinAllowedPathPointDistance = 1e-3f; //; 0.03162f//Der minimale Abstand zwischen zwei PathPunkten. (Betrifft IntersectionFinder/MediaDistanceSampler/PathPointConnector) Beim GeometryTerm/PdfW-To-A-Faktor muss ein Richtungsvektor von ein Punkt zum nächsten gebildet werden. Damit dieser Vektor normiert werden kann, darf der Abstand nicht zu klein sein.
        public const float MinAllowedPathPointSqrDistance = 1e-3f; //Durch diese Zahl hier wird minimal beim GeometryTerm/PdfW-To-A-Faktor dividiert. Somit legt diese Zahl hier fest, wie viel die PdfW2PdfA-Funktion maximal zurück gibt. Bei 1e-3f ergibt das 1/0.001 = 100. Habe ich eine maximale Pfadlänge von 7, dann ergibt das maximal 100^7 = 100000000000000 
        public const float MinAllowedPdfW = 1e-6f; //Kleinstmöglicher Wert, den die PdfW-Funktion einer Brdf/Bsdf annehmen darf
        public const float DistanceForPoint2PointVisibleCheck = 0.0001f; //Wenn die Distanz vom Schattenstrahlendpunkt zum PathPoint kleiner als dieser Wert ist, dann ist ein Point2Point-Conectionschritt möglich

        //Um nicht zu kurze PPP-Pfade zu erhalten, lege ich hiermit die minimale ContinationPdf bei der Phasenfunktion fest (Min-Wert-Phasenfunktion) (Kurze Media-Pfade führen zu Media-Rauschen)
        public const float MediaMinContinuationPdf = 0.8f; //Diese Zahl habe ich von SmallUPDP
        //public const float MinSurfaceContinuationPdf = 0.6f; //Wenn ich das so mache, erhalte ich bei der Stilllife-Kerze bei BPT ein zu dunkles Bild (Pathtracing alleine stimmt aber)
        public const float MinSurfaceContinuationPdf = 0;
    }
}
