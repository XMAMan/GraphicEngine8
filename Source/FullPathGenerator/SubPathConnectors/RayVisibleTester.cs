using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia;
using ParticipatingMedia.DistanceSampling;
using ParticipatingMedia.Media;
using RayTracerGlobal;
using RaytracingLightSource;
using SubpathGenerator;
using System.Collections.Generic;

namespace FullPathGenerator
{
    //Teste ob entlang eines Strahls von 0 bis max(Einstellbar von 0..float-max) ein Objekt liegt
    public class RayVisibleTester
    {
        private readonly IntersectionFinder intersectionFinder;
        public MediaIntersectionFinder MediaIntersectionFinder { get; private set; }

        public bool ContainsMedia { get; private set; }
        public IParticipatingMedia GlobalParticipatingMediaFromScene { get { return this.MediaIntersectionFinder.GlobalParticipatingMediaFromScene; } }

        public RayVisibleTester(IntersectionFinder intersectionFinder, MediaIntersectionFinder mediaIntersectionFinder)
        {
            this.intersectionFinder = intersectionFinder;
            this.MediaIntersectionFinder = mediaIntersectionFinder;
            this.ContainsMedia = mediaIntersectionFinder != null;
        }

        public MediaIntersectionPoint CreateCameraMediaStartPoint(Vector3D startPointPosition)
        {
            return this.MediaIntersectionFinder.CreateCameraMediaStartPoint(startPointPosition);
        }

        //RaytracerSimple
        public IntersectionPoint GetPointOnIntersectableLight(IntersectionPoint point, float time, Vector3D lightPosition, IIntersectableRayDrawingObject lightRayHeigh)
        {
            if (LightPointIsInParallaxShadow(point, lightPosition)) return null;
            var p = this.intersectionFinder.GetIntersectionPoint(new Ray(point.Position, Vector3D.Normalize(lightPosition - point.Position)), time, point.IntersectedObject);
            if (p == null || p.IntersectedRayHeigh != lightRayHeigh) return null;
            return p;
        }

        //DirectLighting (NoMedia)
        public IntersectionPoint GetPointOnLightsource(IntersectionPoint point, float time, DirectLightingSampleResult toLightDirection)
        {
            if (LightDirectionIsInParallaxShadow(point, toLightDirection.DirectionToLightPoint)) return null;

            var lightPoint = this.intersectionFinder.GetIntersectionPoint(new Ray(point.Position, toLightDirection.DirectionToLightPoint), time, point.IntersectedObject, float.MaxValue);

            if (toLightDirection.IsLightIntersectable)
            {
                //Prüfe, das Lichtquelle vom EyePoint aus getroffen wird
                if (lightPoint == null || lightPoint.IntersectedRayHeigh != toLightDirection.LightSource) return null;
            }
            else
            {
                //Prüfe, das Strahl vom EyePoint Richtung Lichtquelle ins Leere fliegt (Richtungslicht/Umgebungslicht ist nicht im IntersectionFinder mit drin)
                if (lightPoint != null) return null; //Richtungslichtquelle wird durch Surfacepoint versperrt

                lightPoint = toLightDirection.LightPointIfNotIntersectable;
            }

            return lightPoint;
        }

        //DirectLighting (With Media)
        public MediaLine GetLineToLightSource(MediaIntersectionPoint eyePoint, float time, DirectLightingSampleResult toLightDirection)
        {
            if (LightDirectionIsInParallaxShadow(eyePoint.SurfacePoint, toLightDirection.DirectionToLightPoint)) return null;


            float maxDistance = float.MaxValue;
            if (toLightDirection.LightSourceIsInfinityAway == false && toLightDirection.LightPointIfNotIntersectable != null) maxDistance = (toLightDirection.LightPointIfNotIntersectable.Position - eyePoint.Position).Length();
            var mediaLine = this.MediaIntersectionFinder.CreateMediaShortLineNoAirBlocking(new MediaIntersectionPoint(eyePoint), toLightDirection.DirectionToLightPoint, maxDistance, time);
            if (toLightDirection.IsLightIntersectable)
            {
                //Prüfe, das Lichtquelle vom EyePoint aus getroffen wird
                if (mediaLine == null || mediaLine.EndPointLocation != MediaPointLocationType.Surface || mediaLine.EndPoint.SurfacePoint.IntersectedRayHeigh != toLightDirection.LightSource) return null;
            }
            else
            {
                //Prüfe, das Strahl vom EyePoint Richtung Lichtquelle ins Leere fliegt (Richtungslicht/Umgebungslicht ist nicht im IntersectionFinder mit drin)
                bool noBlockingSurfaceOrInfinityMediaAvailable = mediaLine == null || (mediaLine.EndPointLocation == MediaPointLocationType.NullMediaBorder && mediaLine.EndPoint.IsInGlobalMedia) || mediaLine.EndPointLocation == MediaPointLocationType.MediaParticle;
                if (noBlockingSurfaceOrInfinityMediaAvailable == false) return null; //Es gibt Blockingobjekt
                if (mediaLine != null && mediaLine.EndPointLocation == MediaPointLocationType.NullMediaBorder)
                {
                    //Environmentlight oder FarAwayLight hinter Wolke/Atmospährenkugel
                    var mediaLightPoint = MediaIntersectionPoint.CreatePointOnLight(toLightDirection.LightPointIfNotIntersectable, this.GlobalParticipatingMediaFromScene);
                    List<VolumeSegment> segments = new List<VolumeSegment>();
                    segments.AddRange(mediaLine.Segments);
                    float distanceFromBorderToLightPoint = (toLightDirection.LightPointIfNotIntersectable.Position - mediaLine.EndPoint.Position).Length();
                    segments.Add(new VolumeSegment(mediaLine.Ray, mediaLine.EndPoint, new RaySampleResult() { RayPosition = mediaLine.ShortRayLength, PdfL = 1, ReversePdfL = 1 }, mediaLine.ShortRayLength, mediaLine.ShortRayLength + distanceFromBorderToLightPoint));
                    return MediaLine.CreateShortRayLine(mediaLine.Ray, mediaLine.StartPoint, mediaLightPoint, segments);
                }
                if (mediaLine != null && mediaLine.EndPointLocation == MediaPointLocationType.MediaParticle)
                {
                    //EnvironmentLight mit GlobalMedia
                    return MediaLine.CreateShortRayLine(mediaLine.Ray, mediaLine.StartPoint, MediaIntersectionPoint.CreateSurfacePoint(mediaLine.EndPoint, toLightDirection.LightPointIfNotIntersectable), mediaLine.Segments);
                }
                if (mediaLine == null)
                {
                    //Environmentlight oder FarAwayLight im Vacuum ohne das es Wolke/Atmosähre gibt
                    var mediaLightPoint = MediaIntersectionPoint.CreatePointOnLight(toLightDirection.LightPointIfNotIntersectable, this.GlobalParticipatingMediaFromScene);
                    var rayToLightPoint = new Ray(eyePoint.Position, toLightDirection.DirectionToLightPoint);
                    VolumeSegment vacuumSegment = new VolumeSegment(rayToLightPoint, eyePoint, new RaySampleResult() { RayPosition = 1, PdfL = 1, ReversePdfL = 1 }, 0, (toLightDirection.LightPointIfNotIntersectable.Position - eyePoint.Position).Length());
                    return MediaLine.CreateShortRayLine(rayToLightPoint, eyePoint, mediaLightPoint, new List<VolumeSegment>() { vacuumSegment });
                }
            }

            return mediaLine;
        }

        //LightTracing (NoMedia)
        public bool IsCameraVisible(IntersectionPoint lightPoint, float time, Vector3D cameraPosition)
        {
            if (LightPointIsInParallaxShadow(lightPoint, cameraPosition)) return false;

            Ray primaryRay = new Ray(cameraPosition, Vector3D.Normalize(lightPoint.Position - cameraPosition));
            var point = this.intersectionFinder.GetIntersectionPoint(primaryRay, time);
            bool isVisible = point != null && point.IntersectedObject == lightPoint.IntersectedObject && (point.Position - lightPoint.Position).Length() < MagicNumbers.DistanceForPoint2PointVisibleCheck;
            return isVisible;
        }

        //LightTracing (With Media)
        public MediaLine GetLineFromCameraToLightPoint(MediaIntersectionPoint cameraPoint, Vector3D cameraToLightPointDirection, float cameraToLightPointDistance, PathPoint lightPoint)
        {
            if (LightPointIsInParallaxShadow(lightPoint.SurfacePoint, cameraPoint.Position)) return null;

            //Beim Lighttracing verbinde ich mich nie direkt mit der Lichtquelle sondern immer nur mit ein LightSubPath-Punkt mit Index > 0
            //Deswegen kann ich davon ausgehen, dass PathPoint auf ein Intersectable-Surface oder Partikel liegt

            var mediaLine = this.MediaIntersectionFinder.CreateMediaShortLineNoAirBlocking(new MediaIntersectionPoint(cameraPoint), cameraToLightPointDirection, lightPoint.LocationType == MediaPointLocationType.Surface ? float.MaxValue : cameraToLightPointDistance, lightPoint.AssociatedPath.PathCreationTime);
            bool isVisible = mediaLine != null && (mediaLine.EndPoint.Position - lightPoint.Position).Length() < MagicNumbers.DistanceForPoint2PointVisibleCheck;
            if (isVisible == false) return null;

            return mediaLine;
        }

        //VertexConnection (NoMedia)
        public bool IsVisibleFromSurfaceToSurface(IntersectionPoint point1, float time, Vector3D point1ToPoint2Direction, IntersectionPoint point2)
        {
            if (LightPointIsInParallaxShadow(point1, point2.Position)) return false;
            if (LightPointIsInParallaxShadow(point2, point1.Position)) return false;

            var p = this.intersectionFinder.GetIntersectionPoint(new Ray(point1.Position, point1ToPoint2Direction), time, point1.IntersectedObject);
            bool isVisible = p != null && p.IntersectedObject == point2.IntersectedObject && (p.Position - point2.Position).SquareLength() < MagicNumbers.DistanceForPoint2PointVisibleCheck;
            return isVisible;
        }

        //VertexConnection (With Media)
        public MediaLine GetLineFromP1ToP2(PathPoint eyePoint, Vector3D point1ToPoint2Direction, float point1ToPoint2Distance, PathPoint lightPoint)
        {
            if (LightPointIsInParallaxShadow(eyePoint.SurfacePoint, lightPoint.Position)) return null;
            if (LightPointIsInParallaxShadow(lightPoint.SurfacePoint, eyePoint.Position)) return null;

            var mediaLine = this.MediaIntersectionFinder.CreateMediaShortLineNoAirBlocking(new MediaIntersectionPoint(eyePoint.MediaPoint), point1ToPoint2Direction, lightPoint.LocationType == MediaPointLocationType.Surface ? float.MaxValue : point1ToPoint2Distance, eyePoint.AssociatedPath.PathCreationTime);
            bool isVisible = mediaLine != null && (mediaLine.EndPoint.Position - lightPoint.Position).Length() < MagicNumbers.DistanceForPoint2PointVisibleCheck;
            if (isVisible == false) return null;

            return mediaLine;
        }


        private bool LightPointIsInParallaxShadow(IntersectionPoint point, Vector3D lightPosition)
        {
            // Wenn ich innerhalb einer Textur starte, dann prüfe, ob ich schon direkt beim Start in der Textur irgendwo stecken bleibe
            if (point != null && point.ParallaxPoint != null)
            {
                var px = point.IntersectedRayHeigh.ParallaxMap.GetParallaxIntersectionPointStartingInside(point.ParallaxPoint, Vector3D.Normalize(lightPosition - point.ParallaxPoint.WorldSpacePoint));

                //Es gibt Schnittpunkt innerhalb der Textur. Der RayVisibleTester wird nun feststellen, dass hier Schatten ist
                if (px.PointIsOnTopHeight == false)
                {
                    return true;
                }
            }

            return false;
        }

        private bool LightDirectionIsInParallaxShadow(IntersectionPoint point, Vector3D toLightDirection)
        {
            // Wenn ich innerhalb einer Textur starte, dann prüfe, ob ich schon direkt beim Start in der Textur irgendwo stecken bleibe
            if (point != null && point.ParallaxPoint != null)
            {
                var px = point.IntersectedRayHeigh.ParallaxMap.GetParallaxIntersectionPointStartingInside(point.ParallaxPoint, toLightDirection);

                //Es gibt Schnittpunkt innerhalb der Textur. Der RayVisibleTester wird nun feststellen, dass hier Schatten ist
                if (px.PointIsOnTopHeight == false)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
