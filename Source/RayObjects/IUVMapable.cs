using System.Drawing;
using GraphicMinimal;
using IntersectionTests;

namespace RayObjects
{
    //Stellt das Ein-Eindeutiges Mapping zwischen ein 3D-Punkt auf dem RayObjekt und einen UV-2D-Punkt im Einheitsrechteck dar.
    public interface IUVMapable : IIntersecableObject
    {
        SurfacePoint GetSurfacePointFromUAndV(double u, double v); //u,v = 0..1
        void GetUAndVFromSurfacePoint(Vector3D position, out double u, out double v);
        double GetSurfaceAreaFromUVRectangle(RectangleF uvRectangle); //uvRectangle = Rechteck im UV-Space. Es wird der Flächeninhalt von diesen Rechteck im 3D-Objektspace zurück gegeben
    }
}
