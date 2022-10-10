using GraphicGlobal;
using GraphicMinimal;

namespace RayObjects
{
    public interface IUniformRandomSurfacePointCreator
    {
        SurfacePoint GetRandomPointOnSurface(IRandom rand);
        float SurfaceArea { get; }
    }

    public class SurfacePoint
    {
        public Vector3D Position { get; private set; }
        public Vector3D Normal { get; private set; }
        public Vector3D Color { get; private set; }
        public IUniformRandomSurfacePointCreator PointSampler { get; private set; }
        public float PdfA { get; set; }

        public SurfacePoint(Vector3D position, Vector3D normal, Vector3D color, IUniformRandomSurfacePointCreator pointSampler, float pdfA)
        {
            this.Position = position;
            this.Normal = normal;
            this.Color = color;
            this.PointSampler = pointSampler;
            this.PdfA = pdfA;
        }
    }
}
