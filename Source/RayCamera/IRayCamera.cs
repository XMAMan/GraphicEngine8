using GraphicGlobal;
using GraphicMinimal;

namespace RayCameraNamespace
{
    public interface IRayCamera
    {
        int PixelCountFromScreen { get; }
        PixelSamplingMode SamplingMode { get; }
        bool DepthOfFieldIsEnabled { get; }
        Vector3D Position { get; }
        Vector3D Forward { get; } //Für den Cos-At-Camera-Geometryfactor beim Subpath-Erstellen
        Vector3D Up { get; } //Für das Hdr-Environmentlight
        bool UseCosAtCamera { get; } //Bei der SphereCamera ist das false
        Ray CreatePrimaryRay(int x, int y, IRandom rand);
        Ray CreatePrimaryRayWithPixi(int x, int y, Vector2D pix);
        Ray CreateRandomPrimaryRay(IRandom rand);
        Vector2D GetPixelFootprintSize(Vector3D point);
        float GetPixelPdfW(int x, int y, Vector3D primaryRayDirection);
        Vector2D GetPixelPositionFromEyePoint(Vector3D point);
        bool IsPointInVieldOfFiew(Vector3D point);
    }
}