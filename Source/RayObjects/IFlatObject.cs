using GraphicMinimal;
using RayObjects.RayObjects;

namespace RayObjects
{
    //Kreis, Dreieck, Viereck
    public interface IFlatObject : IRayObject
    {
        Vector3D Normal { get; }
        Vector3D CenterOfGravity { get; }
        bool IsPointAbovePlane(Vector3D point);
    }
}
