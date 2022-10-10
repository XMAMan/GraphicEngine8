using GraphicGlobal;

namespace IntersectionTests
{
    //Eine unendlich dünne Linie mit Start- und Endpunkt, welche man gegen VolumetricPhotonen(Kugel) oder BeamRays (Zylinder) schießen kann
    public interface IQueryLine
    {
        Ray Ray { get; } //Startpunkt + Richtung der Linie
        float LongRayLength { get; } //Abstand des über die Linie hinausgenden Surface-Punktes zum Startpunkt, auf welchen die Linie zuläuft
    }
}
