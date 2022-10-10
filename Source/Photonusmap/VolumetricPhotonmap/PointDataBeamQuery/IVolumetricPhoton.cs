using RayTracerGlobal;

namespace Photonusmap
{
    //Beschreibt eine Kugel über sein Mittelpunkt + Radius
    interface ISphere : IPoint
    {
        float Radius { get; set; }
    }
}
