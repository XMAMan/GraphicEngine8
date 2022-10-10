namespace ParticipatingMedia
{
    public enum MediaPointLocationType
    {
        Camera,
        Surface,
        MediaParticle,
        MediaBorder,   //Glas-MediaBorder: Rand von Medium mit Brechungsindex != 1. Kann man sich als Glas-Punkt vorstellen, der Brechungsindex von 1(Luft) oder höher(Wasser/Kerze) hat. Ist somit ein spekularer Punkt.
        NullMediaBorder, //Luft-MediaBorder: Wird immer dann erzeugt, wenn es kein GlobalMedia gibt und Strahl ein Medium mit Brechungsindex = 1 durchschreitet und danach dann im Vacuum endet und von kein Surface-Punkt mehr gestoppt wird. Ist ein virtueller Punkt, der Physikalisch nicht existiert. 
        MediaInfinity, //Unendlich entferntes MediaParticel: InfinityPunkt wird dann erzeugt, wenn es ein Globales Medium gibt und Strahl ohne Distanzsampling von kein Surface-Punkt gestoppt wird. Da es kein Distanzsampling gibt, endet es auf ein unendlich entfernten Media-Particel. Ist ein virtueller Punkt, der Physikalisch nicht existiert. 
        Unknown
    }
}
