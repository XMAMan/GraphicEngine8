namespace FullPathGenerator
{
    public class FullPathSettings
    {
        public bool UsePathTracing;         //Verbinde ein Eye-Point mit einer Lichtquelle, wenn dieser sie bereits berührt
        public bool UseSpecularPathTracing; //Es werden nur solche Pfade betrachtet, welche auf Specularpunkten
        public bool UseDirectLighting;      //Verbinde ein Eye-Point mit einer Lichtquelle, wenn Eye-Punkt noch nicht auf Lichtquelle liegt
        public bool UseMultipleDirectLighting; //Erzeuge mehrere LightPoints über allen Lichtquellen
        public bool UseDirectLightingOnEdge;//Erzeuge mitten auf der Kante von ein Eye-Subpath einen Punkt(Geht nur bei Media)
        public bool UseLightTracingOnEdge;
        public bool UseVertexMerging;       //Verbinde ein Eye-Point mit ein Light-Point, wenn dieser in der Nähe ist über eine Kernel-Funktion
        public bool UseVertexConnection;    //Verbinde ein Eye-Point mit ein Light-Point über eine neu erzeugte Kante
        public bool UseVertexConnectionWithImportanceSampling;
        public bool UseLightTracing;        //Verbinde ein Light-Point mit dem Kamera-Punkt
        public bool UsePointDataPointQuery; //Wie VertexMerging nur anstatt auf Oberfläche nun in Medium        
        public bool UsePointDataBeamQuery;  //Verbinde ein Eye-Pfad-Abschnitt mit ein LightPoint, der in der Luft liegt
        public bool UseBeamDataLineQuery;   //Beam-Beam

        //Einstellparameter für einzelne Fullpath-Sampler
        public int MaximumDirectLightingEyeIndex = int.MaxValue;
        public bool DoSingleScattering = false; //Wenn true, werden nur CPL-Pfade erzeugt (Kein mehrfache Partikel mehr möglich)
        public bool UseSegmentSamplingForDirectLightingOnEdge = true; //Wenn false, dann wird Rieman-Integral mit fester Schrittweite auf Segment verwendet
        
        public bool WithoutMis = false;
    }
}
