namespace GraphicMinimal
{
    public class DisplacementData
    {
        public bool UseDisplacementMapping { get; set; } = false;
        public float DisplacementHeight { get; set; } = 0; //So viel wird in Normalrichtung beim Displacementmapping der Vertize verschoben
        public float TesselationFaktor { get; set; } = 1;    // Um so größer, um so mehr Huckel werden beim Displacementmapping erzeugt
    }
}
