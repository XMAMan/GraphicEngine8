using GraphicGlobal;
using GraphicMinimal;
using ParticipatingMedia;
using ParticipatingMedia.DistanceSampling;
using ParticipatingMedia.Media;
using RayTracerGlobal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IntersectionTests
{
    //Diese Punkt wurde von GetIntersectionPoint getroffen
    public class MediaIntersectionPointResult
    {
        public List<VolumeSegment> Segments { get; set; } //0, 1 oder 2 Einträge (0 wenn ich im Vacuum nichts treffe; 1 wenn ich SurfacePunkt treffe; 2 = Wenn ich mit Distanzsampling auf Partikel ende und Longrays erzeuge)
        public MediaIntersectionPoint EndPoint { get; set; }   //Liegt am Ende vom ersten Segment
    }

    //Sucht den nächsten Schnittpunkt entlang eines Strahls. Das kann ein SurfacePunkt, MediaPartikel oder MediaBorder sein
    //Wenn kein Schnittpunkt gefunden wurde, wird Null(Ohne GlobalMedia) oder Infinity(Mit GlobalMedia mit Scattering) zurück gegeben
    //Wenn NoDistanceSampling oder ShortRay verwendet wird, wird maximal ein Segment zurück gegeben
    //Wenn LongRayWithDistanceSampling verwendet wird, werden maximal zwei Segmente zurück gegeben
    public class MediaIntersectionFinder
    {
        public enum IntersectionMode
        {
            NoDistanceSampling,             //Strahl geht bis zum nächsten Surface/Infinity oder MediaBorder-Punkt aber niemals auf MediaPartikel
            ShortRayWithDistanceSampling,   //In dem Medium, wo der Strahl startet, wird Distanzsampling gemacht und Surface/Mediaborder oder MediaPartikel zurück gegeben
            LongRayOneSegmentWithDistanceSampling,     //Suche nächsten Surfacepunkt/MediaInfinity/MediaBorder aber bleibe dann vielleicht irgendwo in der Mitte mit Distancesampling stecken
            LongRayManySegmentsWithDistanceSampling //Suche per Distanzsampling Partikel und füge von dort über alle MediaAir-Borderpunkte hinausgehend alle restlichen Segmente noch an
        }
        private readonly IntersectionFinder intersectionFinder;
        private readonly IntersectionFinder mediaObjects;        //Für den GetMediaObjectPointIsInside()-Test
        private const float medialineLengthIfHittingNothing = 100000; //Wird zurück gegben, wenn kein Distanzsampling verwendet wird und Strahl aus GlobalMedia startet und kein Surface- oder Media-Objekt trifft

        public IParticipatingMedia GlobalParticipatingMediaFromScene { get; private set; }
        public BoundingBox GetBoundingBoxFromScene()
        {
            if (intersectionFinder.RayObjekteRawList.Any() == false) return new BoundingBox(new Vector3D(-1, -1, -1), new Vector3D(1, 1, 1));
            return IntersectionHelper.GetBoundingBoxFromIVolumeObjektCollection(intersectionFinder.RayObjekteRawList);
        }

        public MediaIntersectionFinder(List<IIntersecableObject> noMediaObjects, List<IIntersecableObject> mediaObjects, IParticipatingMedia globalParticipatingMediaFromScene, Action<string, float> progressChangeHandler)
        {
            List<IIntersecableObject> allObjects = new List<IIntersecableObject>();
            allObjects.AddRange(noMediaObjects);
            allObjects.AddRange(mediaObjects);
            this.intersectionFinder = new IntersectionFinder(allObjects, progressChangeHandler);
            this.mediaObjects = new IntersectionFinder(mediaObjects, progressChangeHandler);

            this.GlobalParticipatingMediaFromScene = globalParticipatingMediaFromScene;
        }

        public IIntersectableRayDrawingObject GetMediaObjectPointIsInside(Vector3D point)
        {
            float time = 0;
            Vector3D testDirection = new Vector3D(1, 0, 0);
            var point1 = this.mediaObjects.GetIntersectionPoint(new Ray(point, testDirection), time);
            var point2 = this.mediaObjects.GetIntersectionPoint(new Ray(point, -testDirection), time);

            if (point1 != null && point2 != null && point1.IntersectedRayHeigh == point2.IntersectedRayHeigh && point1.FlatNormal * testDirection > 0 && point2.FlatNormal * (-testDirection) > 0)
                return point1.IntersectedRayHeigh;
            else
                return null;
        }

        public MediaIntersectionPoint CreateCameraMediaStartPoint(Vector3D startPointPosition)
        {
            var mediaObjectCameraIsInside = GetMediaObjectPointIsInside(startPointPosition);
            if (mediaObjectCameraIsInside == null)
                return MediaIntersectionPoint.CreateCameraPoint(startPointPosition, this.GlobalParticipatingMediaFromScene);
            else
                return MediaIntersectionPoint.CreateCameraPoint(startPointPosition, this.GlobalParticipatingMediaFromScene, mediaObjectCameraIsInside);
        }

        public MediaIntersectionPoint CreateLightMediaStartPoint(IntersectionPoint lightSurfacePoint)
        {
            var mediaObjectCameraIsInside = GetMediaObjectPointIsInside(lightSurfacePoint.Position);
            if (mediaObjectCameraIsInside == null)
                return MediaIntersectionPoint.CreatePointOnLight(lightSurfacePoint, this.GlobalParticipatingMediaFromScene);
            else
                return MediaIntersectionPoint.CreatePointOnLight(lightSurfacePoint, this.GlobalParticipatingMediaFromScene, mediaObjectCameraIsInside);
        }

        //Erstellt eine Short-MediaLine, welche bis maxDistance geht und dabei auch Luft-Objekte durchläuft (Wird von den VisibleTest benötigt)
        public MediaLine CreateMediaShortLineNoAirBlocking(MediaIntersectionPoint start, Vector3D direction, float maxDistance, float time)
        {
            return CreateMediaLineNoAirBlocking(start, direction, null, IntersectionMode.NoDistanceSampling, time, maxDistance, null);
        }

        //Erzeugt eine MediaLine die vom Start-Punkt bis zum nächsten Partikel/Surface/Light/EnvironmentLight/Infinity-Punkt reicht (Wird für den SubPathSampler benötigt)
        public MediaLine CreateMediaLineNoAirBlocking(MediaIntersectionPoint startPoint, Vector3D direction, IRandom rand, IntersectionMode intersectionMode, float pathCreationTime, float maxDistance, IIntersectableEnvironmentLight light)
        {
            var mode = TransformIntersectionMode(intersectionMode);

            MediaIntersectionPoint runningPoint = new MediaIntersectionPoint(startPoint);

            List<VolumeSegment> segments = new List<VolumeSegment>();
            int lastSegmentCount = -1;

            for (int i = 0; i < 100; i++) //Verhindere Unendlichschleife
            {
                float maxiDistance = maxDistance;
                float rayMin = (runningPoint.Position - startPoint.Position).Length();
                if (maxDistance < float.MaxValue)
                {
                    maxiDistance = Math.Max(0, maxDistance - rayMin);
                }
                var result = this.GetIntersectionPoint(runningPoint, new Ray(runningPoint.Position, direction), runningPoint.SurfacePoint?.IntersectedObject, rand, mode, pathCreationTime, maxiDistance);
                if (result == null)
                {
                    if (light != null && light.ContainsEnvironmentLight)
                    {
                        var lightPoint = light.GetIntersectionPointWithEnvironmentLight(new Ray(runningPoint.Position, direction));

                        var toLightSegment = new VolumeSegment(new Ray(startPoint.Position, direction), runningPoint, new RaySampleResult() { PdfL = 1, ReversePdfL = 1 }, rayMin, (lightPoint.Position - startPoint.Position).Length());
                        segments.Add(toLightSegment);

                        runningPoint = MediaIntersectionPoint.CreateSurfacePoint(runningPoint, lightPoint);
                    }
                    if (segments.Any()) break;
                    return null; //Distanz zum nächsten Surfacepunkt ist zu klein wenn segments.Count == 0
                }
                runningPoint = new MediaIntersectionPoint(result.EndPoint);

                float max = segments.Any() ? segments.Last().RayMax : 0;
                foreach (var s in result.Segments)
                {                    
                    s.RayMin += max;
                    s.RayMax += max;
                    segments.Add(s);
                }
                lastSegmentCount = result.Segments.Count;

                bool isOnAirMediaBorder = result.EndPoint.Location == MediaPointLocationType.NullMediaBorder;
                if (isOnAirMediaBorder) runningPoint.JumpToNextMediaBorder(result.EndPoint.SurfacePoint);

                if (isOnAirMediaBorder == false) break;
            }

            if (segments.Any() == false) return null; //Wenn Partikel zu nah am Rand liegt, dann klappt VisibleTest nicht

            if (intersectionMode == IntersectionMode.LongRayOneSegmentWithDistanceSampling || intersectionMode == IntersectionMode.LongRayManySegmentsWithDistanceSampling)
            {
                //Ein Longray-Segment kann nur existieren, wenn der Endpunkt auf ein Partikel liegt und der letzte GetIntersectionPoint-Aufruf zwei Segmente zurück gegeben hat
                //Das wird er nur dann tun, wenn das Partikel zum MediaBorder-Eingangs- und Ausgangspunkt ein größeren Abstand als MagicNumbers.MinAllowedPathPointDistance hat
                bool isThereAreSegmentAfterShortRayLength = lastSegmentCount == 2 && runningPoint.Location == MediaPointLocationType.MediaParticle;
                int shortRaySegmentCount = segments.Count - (isThereAreSegmentAfterShortRayLength ? 1 : 0);

                if (intersectionMode == IntersectionMode.LongRayManySegmentsWithDistanceSampling)
                {
                    //Ermittle noch alle restlichen Segmente innerhalb aller nachfolgenden AirMedias
                    if (isThereAreSegmentAfterShortRayLength) segments.RemoveAt(segments.Count - 1);
                    var runningExtra = new MediaIntersectionPoint(runningPoint);
                    AddManyLongRaySegemnts(segments, maxDistance, runningExtra, new Ray(startPoint.Position, direction), rand, pathCreationTime);
                }

                return MediaLine.CreateLongRayLine(new Ray(startPoint.Position, direction), startPoint, runningPoint, segments, shortRaySegmentCount);
            }                
            else
                return MediaLine.CreateShortRayLine(new Ray(startPoint.Position, direction), startPoint, runningPoint, segments);
        }

        //Geht durch alle AirMedias durch und sucht nach nächsten Surface/Infinity-Punkt
        private void AddManyLongRaySegemnts(List<VolumeSegment> segments, float maxDistance, MediaIntersectionPoint runningExtra, Ray ray, IRandom rand, float pathCreationTime)
        {
            for (int i = 0; i < 100; i++) //Verhindere Unendlichschleife
            {
                float maxiDistance = maxDistance;
                if (maxDistance < float.MaxValue)
                {
                    float rayMin = (runningExtra.Position - ray.Start).Length();
                    maxiDistance = Math.Max(0, maxDistance - rayMin);
                }
                var result = this.GetIntersectionPoint(runningExtra, new Ray(runningExtra.Position, ray.Direction), runningExtra.SurfacePoint?.IntersectedObject, rand, GetIntersectionPointMode.NoDistanceSampling, pathCreationTime, maxiDistance);
                if (result == null) break;
                runningExtra = new MediaIntersectionPoint(result.EndPoint);

                float max = segments.Any() ? segments.Last().RayMax : 0;
                foreach (var s in result.Segments)
                {
                    s.RayMin += max;
                    s.RayMax += max;
                    segments.Add(s);
                }

                bool isOnAirMediaBorder = result.EndPoint.Location == MediaPointLocationType.NullMediaBorder;
                if (isOnAirMediaBorder) runningExtra.JumpToNextMediaBorder(result.EndPoint.SurfacePoint);

                if (isOnAirMediaBorder == false) break;
            }
        }

        private GetIntersectionPointMode TransformIntersectionMode(IntersectionMode mode)
        {
            switch(mode)
            {
                case IntersectionMode.NoDistanceSampling:
                    return GetIntersectionPointMode.NoDistanceSampling;
                case IntersectionMode.ShortRayWithDistanceSampling:
                    return GetIntersectionPointMode.ShortRayWithDistanceSampling;
                case IntersectionMode.LongRayOneSegmentWithDistanceSampling:
                case IntersectionMode.LongRayManySegmentsWithDistanceSampling:
                    return GetIntersectionPointMode.LongRayWithDistanceSampling;
            }
            throw new Exception("Unknown mode " + mode);
        }

        internal enum GetIntersectionPointMode
        {
            NoDistanceSampling,             //Strahl geht bis zum nächsten Surface/Infinity oder MediaBorder-Punkt aber niemals auf MediaPartikel
            ShortRayWithDistanceSampling,   //In dem Medium, wo der Strahl startet, wird Distanzsampling gemacht und Surface/Mediaborder oder MediaPartikel zurück gegeben
            LongRayWithDistanceSampling     //Suche nächsten Surfacepunkt/MediaInfinity/MediaBorder aber bleibe dann vielleicht irgendwo in der Mitte mit Distancesampling stecken
        }
        internal MediaIntersectionPointResult GetIntersectionPoint(MediaIntersectionPoint startPoint, Ray ray, IIntersecableObject excludedObject, IRandom rand, GetIntersectionPointMode intersectionMode, float pathCreationTime, float maxDistance)
        {
            bool useDistanceSampling = intersectionMode == GetIntersectionPointMode.ShortRayWithDistanceSampling || intersectionMode == GetIntersectionPointMode.LongRayWithDistanceSampling;
            bool createSegmentFromParticleToNextIntersectionPoint = intersectionMode == GetIntersectionPointMode.LongRayWithDistanceSampling;

            var point = this.intersectionFinder.GetIntersectionPoint(ray, pathCreationTime, excludedObject, maxDistance);

            //Es wurde kein Surface- oder Media-Punkt getroffen
            if (point == null)
            {
                if (startPoint.CurrentMedium.HasScatteringSomeWhereInMedium() == false) return null;
                if (startPoint.IsInGlobalMedia == false && maxDistance == float.MaxValue) return null;//Strahl befindet sich in ein Media-Objekt aber es wird nicht andere Borderwand getroffen (Kugel oben am Rand-Problem)

                if (useDistanceSampling) //Strahl befindet sich im Global-Medium (Luft mit Scattering-Teilchen)
                {
                    RaySampleResult sampleResult = startPoint.CurrentMedium.DistanceSampler.SampleRayPositionWithPdfFromRayMinToInfinity(ray, 0, float.MaxValue, rand, startPoint.Location == MediaPointLocationType.MediaParticle);
                    if (sampleResult.RayPosition == float.MaxValue) return null; //Kein Schnittpunkt. Strahl fliegt ins unendliche weg

                    //Ich befinde mich auf Scatter- oder Absorbation-Partikel
                    var sampledParticle = MediaIntersectionPoint.CreateMediaPoint(startPoint, ray.Start + ray.Direction * sampleResult.RayPosition, MediaPointLocationType.MediaParticle);

                    MediaIntersectionPointResult result = new MediaIntersectionPointResult() { EndPoint = sampledParticle, Segments = new List<VolumeSegment>() };
                    if (sampleResult.RayPosition >= MagicNumbers.MinAllowedPathPointDistance) result.Segments.Add(new VolumeSegment(ray, startPoint, sampleResult, 0, sampleResult.RayPosition));
                    if (createSegmentFromParticleToNextIntersectionPoint) result.Segments.Add(new VolumeSegment(ray, sampledParticle, new RaySampleResult() { RayPosition = medialineLengthIfHittingNothing, PdfL = 1, ReversePdfL = 1 }, sampleResult.RayPosition, medialineLengthIfHittingNothing));
                    return result;                    
                }else //Kein Distanzsampling im GlobalMedia
                {
                    float dist = maxDistance == float.MaxValue ? medialineLengthIfHittingNothing : maxDistance;
                    return new MediaIntersectionPointResult()
                    {
                        EndPoint = MediaIntersectionPoint.CreateMediaPoint(startPoint, startPoint.Position + ray.Direction * dist, maxDistance == float.MaxValue ? MediaPointLocationType.MediaInfinity : MediaPointLocationType.MediaParticle),
                        Segments = new List<VolumeSegment>()
                        {
                            new VolumeSegment(ray, startPoint, new RaySampleResult() { RayPosition = dist, PdfL = 1, ReversePdfL = 1 }, 0, dist)
                        }
                    };

                }
                    
            }

            float distanceToPoint = (point.Position - startPoint.Position).Length();

            if (useDistanceSampling)
            {                
                RaySampleResult sampleResult = startPoint.CurrentMedium.DistanceSampler.SampleRayPositionWithPdfFromRayMinToInfinity(ray, 0, distanceToPoint, rand, startPoint.Location == MediaPointLocationType.MediaParticle);

                if (sampleResult.RayPosition < distanceToPoint) //Bleibe im Medium stecken
                {
                    //Ich befinde mich auf Scatter- oder Absorbation-Partikel
                    var sampledParticle = MediaIntersectionPoint.CreateMediaPoint(startPoint, ray.Start + ray.Direction * sampleResult.RayPosition, MediaPointLocationType.MediaParticle);
                    MediaIntersectionPointResult result = new MediaIntersectionPointResult() { EndPoint = sampledParticle, Segments = new List<VolumeSegment>() };
                    if (sampleResult.RayPosition >= MagicNumbers.MinAllowedPathPointDistance) result.Segments.Add(new VolumeSegment(ray, startPoint, sampleResult, 0, sampleResult.RayPosition));
                    if (createSegmentFromParticleToNextIntersectionPoint)
                    {
                        float lengthFromSecondSegment = Math.Max(0, distanceToPoint - sampleResult.RayPosition);
                        if (lengthFromSecondSegment >= MagicNumbers.MinAllowedPathPointDistance) result.Segments.Add(new VolumeSegment(ray, startPoint, new RaySampleResult() { RayPosition = distanceToPoint, PdfL = 1, ReversePdfL = 1 }, sampleResult.RayPosition, distanceToPoint));
                    }
                    return result;
                }else //Medium wurde durchlaufen
                {
                    MediaIntersectionPoint hittedPoint = GetMediaPointFromIntersectionPoint(startPoint, point);
                    MediaIntersectionPointResult result = new MediaIntersectionPointResult() { EndPoint = hittedPoint, Segments = new List<VolumeSegment>() };
                    if (sampleResult.RayPosition >= MagicNumbers.MinAllowedPathPointDistance) result.Segments.Add(new VolumeSegment(ray, startPoint, sampleResult, 0, sampleResult.RayPosition));
                    return result; //Hier gibt es garkeine Segmente
                }
            }else
            {
                MediaIntersectionPoint hittedPoint = GetMediaPointFromIntersectionPoint(startPoint, point);
                MediaIntersectionPointResult result = new MediaIntersectionPointResult() { EndPoint = hittedPoint, Segments = new List<VolumeSegment>() };
                if (distanceToPoint >= MagicNumbers.MinAllowedPathPointDistance) result.Segments.Add(new VolumeSegment(ray, startPoint, new RaySampleResult() { RayPosition = distanceToPoint, PdfL = 1, ReversePdfL = 1 }, 0, distanceToPoint));
                return result; //Hier gibt es kein zweites LongRay-Segment
            }                       
        }

        private MediaIntersectionPoint GetMediaPointFromIntersectionPoint(MediaIntersectionPoint oldPointRayComesFrome, IntersectionPoint point)
        {
            MediaIntersectionPoint mediaPoint;
            if (point.Propertys.MediaDescription == null) //Surface getroffen
            {
                mediaPoint = MediaIntersectionPoint.CreateSurfacePoint(oldPointRayComesFrome, point);
            }
            else //Media-Rand getroffen
            {
                mediaPoint = MediaIntersectionPoint.CreateMediaBorderPoint(oldPointRayComesFrome, point);
            }
            return mediaPoint;
        }

    }
}
