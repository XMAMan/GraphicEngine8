using System.Linq;
using RaytracingBrdf;
using GraphicMinimal;
using IntersectionTests;
using GraphicGlobal;
using ParticipatingMedia;
using RayCameraNamespace;
using RaytracingLightSource;
using RaytracingBrdf.SampleAndRequest;
using SubpathGenerator.SubPathSampler;
using System;
using ParticipatingMedia.DistanceSampling;
using System.Collections.Generic;

namespace SubpathGenerator.SubPathPointsSampler.Media
{
    class MediaSubPathPointsSampler : ISubPathPointsSampler
    {
        private readonly MediaIntersectionFinder intersectionFinder;
        private readonly LightSourceSampler lightSourceSampler;
        private readonly MediaDirectionSampler directionSampler;
        private readonly int maxLength;
        private readonly MediaIntersectionPoint cameraMediaPoint = null;
        private readonly MediaIntersectionFinder.IntersectionMode intersectionMode;

        public MediaSubPathPointsSampler(MediaIntersectionFinder intersectionFinder, LightSourceSampler lightSourceSampler, int maxLength, MediaIntersectionFinder.IntersectionMode intersectionMode, IBrdfSampler standardBrdfSampler, IPhaseFunctionSampler phaseFunction, IRayCamera rayCamera)
        {
            this.intersectionFinder = intersectionFinder;
            this.lightSourceSampler = lightSourceSampler;
            this.directionSampler = new MediaDirectionSampler(standardBrdfSampler, phaseFunction);
            this.maxLength = maxLength;
            this.cameraMediaPoint = intersectionFinder.CreateCameraMediaStartPoint(rayCamera.Position);
            this.intersectionMode = intersectionMode;
        }

        public PathPoint[] SamplePointsFromCamera(Vector3D cameraForward, BrdfSampleEvent sampleEvent, Vector3D pathWeight, float pathCreationTime, IRandom rand)
        {
            var startPoint = PathPoint.CreateCameraPoint(new MediaIntersectionPoint(this.cameraMediaPoint), cameraForward, sampleEvent);
            var rayWalkData = RayWalkData.CreateEyePathData(sampleEvent, pathWeight, pathCreationTime, startPoint);            

            return SamplePoints(new MediaRayWalkData(rayWalkData), rand);
        }
        public PathPoint[] SamplePointsFromLightSource(IntersectionPoint lightPoint, Vector3D pathWeightFromPointOnLight, float positionPdfA, BrdfSampleEvent sampleEvent, Vector3D pathWeight, float pathCreationTime, bool lightSourceIsInfinityAway, IRandom rand)
        {
            var startPoint = PathPoint.CreateLightsourcePointWithSurroundingMedia(lightPoint, pathWeightFromPointOnLight, intersectionFinder.CreateLightMediaStartPoint(lightPoint), lightSourceIsInfinityAway, sampleEvent); //Wenn die Lichtquelle im GlobalMedia oder in ein Medium liegt (teilweise oder auch komplett)
            var rayWalkData = RayWalkData.CreateLightPathData(sampleEvent, pathWeight, pathCreationTime, lightSourceIsInfinityAway, startPoint, positionPdfA);            
            
            return SamplePoints(new MediaRayWalkData(rayWalkData), rand);
        }

        //Modifiziert das rayWalkData-Objekt um am Ende dann ein PathPoint-Array zu erhalten
        private PathPoint[] SamplePoints(MediaRayWalkData rayWalkData, IRandom rand)
        {
            for (int i = 1; i < this.maxLength; i++) //Index 0 ist Punkt auf Kamera oder Lichtquelle und wird in SamplePointsFromCamera/SamplePointsFromLightSource zur Liste hinzugefügt
            {
                //Schritt 1: Erzeuge MediaLine vom aktuellen Pfad-Punkt bis zum nächsten Partikel oder Surface (Abbruch wenn Strahl ins Leere fliegt)
                var line = this.intersectionFinder.CreateMediaLineNoAirBlocking(rayWalkData.MediaPoint, rayWalkData.RayDirection, rand, this.intersectionMode, rayWalkData.PathCreationTime, float.MaxValue, this.lightSourceSampler);
                if (line == null) return rayWalkData.Points.ToArray(); //Abbruch da Strahl ins Leere fliegt oder Distanz zum nächsten SurfacePunkt zu klein ist

                //Schritt 2: Gehe bis zum Endpunkt von der MediaLine
                var linePdf = line.SampledPdfL();
                rayWalkData.PathWeight = Vector3D.Mult(rayWalkData.PathWeight, line.AttenuationWithoutPdf()) / linePdf.PdfL;

                //Ich treffe auf Glas-Objekt, was Medium enthält? -> Bestimme RefractionIndex von den Medium vor und hinter der Grenze
                if (line.EndPointLocation == MediaPointLocationType.MediaBorder)
                {
                    rayWalkData.RefractionIndexCurrentMedium = line.EndPoint.CurrentMedium.RefractionIndex;
                    rayWalkData.RefractionIndexNextMedium = line.EndPoint.GetNextMediaAfterCrossingBorder().RefractionIndex;
                }

                //Schritt 3: Erzeuge beim Endpunkt von der MediaLine ein PathPoint
                var pathPoint = CreatePathPoint(rayWalkData, line.EndPoint);
                AddPoint(rayWalkData.Points, pathPoint, line, linePdf, rayWalkData.SampleEvent);

                //Schritt 4: Breche ab, wenn beim PathPoint kein Richtungssampling erfolgen soll
                if (pathPoint.LocationType == MediaPointLocationType.NullMediaBorder) return rayWalkData.Points.ToArray(); //Strahl verläßt nach NullMediaBorder die Scene
                if (pathPoint.LocationType == MediaPointLocationType.MediaInfinity) return rayWalkData.Points.ToArray(); //Abbruch, da Pfad Medim verläßt und ins unendliche wegfliegt
                if (pathPoint.PdfA == 0) return rayWalkData.Points.ToArray(); //Abbruch, da Pfad zu unwahrscheinlich
                if (pathPoint.IsLocatedOnLightSource) return rayWalkData.Points.ToArray(); //Abbruch, da Strahl da Strahl Lichtquelle (Schwarzstrahler) berührt


                //Schritt 5: Sample auf den neu erstellten Pfadpunkt eine Richtung
                rayWalkData.MediaPoint = new MediaIntersectionPoint(pathPoint.MediaPoint);
                var newDirection = this.directionSampler.SampleDirection(pathPoint, rayWalkData, rand);
                if (newDirection == null) return rayWalkData.Points.ToArray(); //Abbruch, da Photon absorbiert wurde

                if (newDirection.RayWasRefracted) rayWalkData.MediaPoint.JumpToNextMediaBorder(pathPoint.SurfacePoint);
                rayWalkData.Points.Last().UpdateBrdfSampleEvent(newDirection);
                rayWalkData.PathWeight = Vector3D.Mult(rayWalkData.PathWeight, newDirection.Brdf);
                rayWalkData.SampleEvent = newDirection;
            }

            return rayWalkData.Points.ToArray(); //Abbruch, da MaxPath-Length überschritten
        }

        //Erzeugt ein Pfadpunkt auf ein Surface oder Partikel welcher vom aktuellen Ray aus erzeugt wurde
        private static PathPoint CreatePathPoint(RayWalkData rayWalkData, MediaIntersectionPoint point)
        {
            switch (point.Location)
            {
                case MediaPointLocationType.Surface:
                    return PathPoint.CreateSurfacePointWithSurroundingMedia(new BrdfPoint(point.SurfacePoint, rayWalkData.RayDirection, rayWalkData.RefractionIndexCurrentMedium, rayWalkData.RefractionIndexNextMedium), rayWalkData.PathWeight, point);
                case MediaPointLocationType.MediaBorder:
                case MediaPointLocationType.NullMediaBorder:
                    return PathPoint.CreateMediaBorderPoint(new BrdfPoint(point.SurfacePoint, rayWalkData.RayDirection, rayWalkData.RefractionIndexCurrentMedium, rayWalkData.RefractionIndexNextMedium), rayWalkData.PathWeight, point);
                case MediaPointLocationType.MediaParticle:
                    return PathPoint.CreateMediaParticlePoint(point, rayWalkData.PathWeight);
                case MediaPointLocationType.MediaInfinity:
                    return PathPoint.CreateMediaInfinityPoint(point, rayWalkData.PathWeight);
                default:
                    throw new Exception("Unknown EndpointLocation " + point.Location);
            }
        }

        //Fügt dem Subpath ein neuen Pfadpunkt hinzu
        private static void AddPoint(List<PathPoint> points, PathPoint newPoint, MediaLine mediaLineToReachNewPoint, DistancePdf mediaPdfToReachNewPoint, BrdfSampleEvent brdfSampleEventToReachTheNewPoint)
        {
            bool isVirtualPoint = newPoint.LocationType == MediaPointLocationType.NullMediaBorder || newPoint.LocationType == MediaPointLocationType.MediaInfinity; //Kein physikalisch vorhandener Punkt

            var predecessor = points.Last();
            newPoint.Predecessor = predecessor;
            newPoint.Index = points.Count;

            float pdfAOnNewPoint = PdfHelper.PdfWToPdfAOrV(brdfSampleEventToReachTheNewPoint.PdfW, predecessor, newPoint);
            newPoint.PdfA = predecessor.PdfA * mediaPdfToReachNewPoint.PdfL * pdfAOnNewPoint;

            if (isVirtualPoint) newPoint.PdfA = double.NaN; //Auf virtuellen Punkten darf man nicht landen

            predecessor.LineToNextPoint = mediaLineToReachNewPoint;
            predecessor.PdfLFromNextPointToThis = mediaPdfToReachNewPoint.ReversePdfL; //Distance-Sampling-Pdf um von newPoint zu points.Last() zu gehen

            if (points.Count > 1)
            {
                var predecessor1 = points[points.Count - 2];
                predecessor1.PdfAReverse = PdfHelper.PdfWToPdfAOrV(brdfSampleEventToReachTheNewPoint.PdfWReverse, predecessor, predecessor1);
                //predecessor1.PdfAReverse = brdfSampleEventToReachTheNewPoint.IsSpecualarReflected ? brdfSampleEventToReachTheNewPoint.PdfWReverse : PdfMisHelper.PdfWToPdfAOrV(brdfSampleEventToReachTheNewPoint.PdfWReverse, predecessor.Position, predecessor1); //Wenn die Zeile drin ist, wird die Kerze bei Stilllife dunkel
            }
            points.Add(newPoint);
        }
    }
}
