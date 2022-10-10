using ParticipatingMedia;
using System;
using System.Linq;

namespace FullPathGenerator.AnalyseHelper
{
    //Ein PathSpace ist das Integral(Menge aller Pfade), dessen Pfadknoten auf einen bestimmten LocationTyp liegen
    //Beispiel für ein einzelnen PathSpace "C D S L" ->Camera DiffuseSurface SpecularSurface LigthSource
    public static class FullPathToPathSpaceConverter
    {
        enum LocationType { Camera, DiffuseSurface, SpecularSurface, Particle, LightSource, Border, UNullMediaBorder }

        private static LocationType GetLocationTypeFromPathPoint(FullPathPoint point)
        {
            if (point.LocationType == MediaPointLocationType.Camera) return LocationType.Camera;
            if (point.LocationType == MediaPointLocationType.Surface && point.IsDiffusePoint && point.IsLocatedOnLightSource == false) return LocationType.DiffuseSurface;
            if (point.LocationType == MediaPointLocationType.Surface && point.IsDiffusePoint == false && point.IsLocatedOnLightSource == false) return LocationType.SpecularSurface;
            if (point.LocationType == MediaPointLocationType.MediaParticle) return LocationType.Particle;
            if (point.LocationType == MediaPointLocationType.MediaBorder) return LocationType.Border;
            if (point.LocationType == MediaPointLocationType.NullMediaBorder) return LocationType.UNullMediaBorder;
            if (point.IsLocatedOnLightSource) return LocationType.LightSource;
            throw new Exception("Unknown Location");
        }

        public static string ConvertPathToPathSpaceString(FullPath path)
        {
            return string.Join(" ", path.Points.Select(x => GetLocationTypeFromPathPoint(x).ToString()[0]));
        }
    }
}
