using IntersectionTests;
using GraphicMinimal;
using SubpathGenerator;

namespace Photonusmap
{
    public class PhotonIntersectionPoint : IIntersectionPointSimple
    {
        public Vector3D Position { get; private set; } //Kugelschnittpunkt
        public float DistanceToRayStart { get; private set; }
        public IIntersecableObject IntersectedObject { get; private set; }
        public float SquareDistanceToRayline { get; private set; } //Abstand zwischen LightPoint und Querry-Line
        public PathPoint LightPoint { get; private set; } //
        public float PhotonRadius { get { return (this.IntersectedObject as VolumetricPhoton).Radius; } }

        public PhotonIntersectionPoint(Vector3D position, float distanceToRayStart, IIntersecableObject intersectedObject, float squareDistanceToRayline, PathPoint lightPoint)
        {
            this.Position = position;
            this.DistanceToRayStart = distanceToRayStart;
            this.IntersectedObject = intersectedObject;
            this.SquareDistanceToRayline = squareDistanceToRayline;
            this.LightPoint = lightPoint;
        }
    }
}
