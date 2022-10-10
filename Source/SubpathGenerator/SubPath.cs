using ParticipatingMedia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubpathGenerator
{
    public class SubPath
    {
        public PathPoint[] Points { get; private set; }
        public float PathCreationTime { get; private set; }

        public SubPath(PathPoint[] points, float pathCreationTime)
        {
            this.Points = points;
            this.PathCreationTime = pathCreationTime;
        }

        public string ToPathSpaceString()
        {
            return this.PathCreationTime + "\t" + string.Join(" ", this.Points.Select(x => GetLocationTypeFromPathPoint(x)));
        }

        private static string GetLocationTypeFromPathPoint(PathPoint point)
        {
            if (point.LocationType == MediaPointLocationType.Camera) return "C";
            if (point.LocationType == MediaPointLocationType.Surface && point.IsDiffusePoint && point.IsLocatedOnLightSource == false) return "D";
            if (point.LocationType == MediaPointLocationType.Surface && point.IsDiffusePoint == false && point.IsLocatedOnLightSource == false) return "S";
            if (point.LocationType == MediaPointLocationType.MediaParticle) return "P";
            if (point.LocationType == MediaPointLocationType.MediaBorder) return "B";
            if (point.LocationType == MediaPointLocationType.NullMediaBorder) return "U";
            if (point.IsLocatedOnLightSource) return "L";
            throw new Exception("Unknown Location");
        }
    }
}
