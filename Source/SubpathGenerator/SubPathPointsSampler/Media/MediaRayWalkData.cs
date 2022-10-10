using IntersectionTests;

namespace SubpathGenerator.SubPathPointsSampler.Media
{
    class MediaRayWalkData : RayWalkData
    {
        public MediaIntersectionPoint MediaPoint { get; set; }
        public MediaRayWalkData(RayWalkData walkData)
            :base(walkData)
        {
            this.MediaPoint = this.Points[0].MediaPoint;
        }
    }
}
