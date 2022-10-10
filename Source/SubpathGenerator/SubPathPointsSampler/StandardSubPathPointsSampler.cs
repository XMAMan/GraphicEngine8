using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using RaytracingBrdf;
using RaytracingLightSource;
using IntersectionTests;
using GraphicGlobal;
using RaytracingBrdf.SampleAndRequest;

namespace SubpathGenerator.SubPathSampler
{
    class StandardSubPathPointsSampler : ISubPathPointsSampler
    {
        private readonly IntersectionFinder intersectionFinder;
        private readonly LightSourceSampler lightSourceSampler;
        private readonly int maxLength;
        private readonly IBrdfSampler sampleDirectionHandler;

        public StandardSubPathPointsSampler(IntersectionFinder intersectionFinder, LightSourceSampler lightSourceSampler, int maxLength, IBrdfSampler sampleDirectionHandler)
        {
            this.intersectionFinder = intersectionFinder;
            this.lightSourceSampler = lightSourceSampler;
            this.maxLength = maxLength;
            this.sampleDirectionHandler = sampleDirectionHandler;
        }

        public PathPoint[] SamplePointsFromCamera(Vector3D cameraForward, BrdfSampleEvent sampleEvent, Vector3D pathWeight, float pathCreationTime, IRandom rand)
        {
            var startPoint = PathPoint.CreateCameraPoint(sampleEvent.Ray.Start, cameraForward, sampleEvent);
            var rayWalkData = RayWalkData.CreateEyePathData(sampleEvent, pathWeight, pathCreationTime, startPoint);            
            
            return SamplePoints(rayWalkData, rand);
        }
        public PathPoint[] SamplePointsFromLightSource(IntersectionPoint lightPoint, Vector3D pathWeightFromPointOnLight, float positionPdfA, BrdfSampleEvent sampleEvent, Vector3D pathWeight, float pathCreationTime, bool lightSourceIsInfinityAway, IRandom rand)
        {
            var startPoint = PathPoint.CreateLightsourcePointWithoutSurroundingMedia(lightPoint, pathWeightFromPointOnLight, lightSourceIsInfinityAway, sampleEvent);
            var rayWalkData = RayWalkData.CreateLightPathData(sampleEvent, pathWeight, pathCreationTime, lightSourceIsInfinityAway, startPoint, positionPdfA);            
            
            return SamplePoints(rayWalkData, rand);
        }

        //Modifiziert das rayWalkData-Objekt um am Ende dann ein PathPoint-Array zu erhalten
        private PathPoint[] SamplePoints(RayWalkData rayWalkData, IRandom rand)
        {
            bool isOutside = true;
            for (int i = 1; i < this.maxLength; i++)//Index 0 ist Punkt auf Kamera oder Lichtquelle und wird in SamplePointsFromCamera/SamplePointsFromLightSource zur Liste hinzugefügt
            {
                var point = this.intersectionFinder.GetIntersectionPoint(rayWalkData.Ray, rayWalkData.PathCreationTime, rayWalkData.ExcludedObject, float.MaxValue);

                if (point == null)
                {
                    if (this.lightSourceSampler != null && this.lightSourceSampler.ContainsEnvironmentLight)
                    {
                        var lightPoint = this.lightSourceSampler.GetIntersectionPointWithEnvironmentLight(rayWalkData.Ray);
                        if (lightPoint != null) AddPoint(rayWalkData.Points, PathPoint.CreateLightsourcePointWithoutSurroundingMedia(lightPoint, rayWalkData.PathWeight, true, null), rayWalkData.SampleEvent);
                        return rayWalkData.Points.ToArray(); //Abbruch da Strahl auf Lichtquelle endet
                    }
                    else
                    {
                        return rayWalkData.Points.ToArray(); //Abbruch, da Strahl ins Leere fliegt
                    }
                }

                if (isOutside)
                {
                    rayWalkData.RefractionIndexCurrentMedium = 1;
                    rayWalkData.RefractionIndexNextMedium = point.RefractionIndex;
                }
                else
                {
                    rayWalkData.RefractionIndexCurrentMedium = point.RefractionIndex;
                    rayWalkData.RefractionIndexNextMedium = 1;
                }

                var pathPoint = PathPoint.CreateSurfacePointWithoutSurroundingMedia(new BrdfPoint(point, rayWalkData.RayDirection, rayWalkData.RefractionIndexCurrentMedium, rayWalkData.RefractionIndexNextMedium), rayWalkData.PathWeight);
                AddPoint(rayWalkData.Points, pathPoint, rayWalkData.SampleEvent);

                if (pathPoint.PdfA  == 0) return rayWalkData.Points.ToArray(); //Abbruch, da Pfad zu unwahrscheinlich
                if (pathPoint.IsLocatedOnLightSource) return rayWalkData.Points.ToArray(); //Abbruch, da Strahl da Strahl Lichtquelle (Schwarzstrahler) berührt

                

                var newDirection = pathPoint.BrdfPoint.SampleDirection(this.sampleDirectionHandler, rand);
                if (newDirection == null) return rayWalkData.Points.ToArray(); //Abbruch, da Photon absorbiert wurde

                if (newDirection.RayWasRefracted)
                {
                    isOutside = !isOutside;

                    if (rayWalkData.IsEyePath)
                    {
                        //Siehe MediaDirectionSampler Zeile 27 für eine Erklärung was hier passiert
                        float relativeIOR = (rayWalkData.RefractionIndexCurrentMedium / rayWalkData.RefractionIndexNextMedium);
                        newDirection.Brdf *= (relativeIOR * relativeIOR);
                    }
                }

                rayWalkData.Points.Last().UpdateBrdfSampleEvent(newDirection);
                rayWalkData.PathWeight = Vector3D.Mult(rayWalkData.PathWeight, newDirection.Brdf);
                rayWalkData.SampleEvent = newDirection;
            }

            return rayWalkData.Points.ToArray(); //Abbruch, da MaxPath-Length überschritten
        }

        private void AddPoint(List<PathPoint> points, PathPoint newPoint, BrdfSampleEvent brdfSampleEventToReachTheNewPoint)
        {
            var predecessor = points.Last();
            newPoint.Predecessor = predecessor;
            newPoint.Index = points.Count;
            newPoint.PdfA = predecessor.PdfA * PdfHelper.PdfWToPdfAOrV(newPoint.Predecessor.BrdfSampleEventOnThisPoint.PdfW, predecessor, newPoint); //Die Umrechnung erfolgt deswegen, weil das Pfadgewicht = (Brdf/PdfA) und die Pfad-PdfA zwei verschiedene Variablen sind. Pfadgewicht ist die Übertragene Radiacne. PfadPdfA dient zur MIS-Berechnung.

            //newPoint.PdfA = predecessor.PdfA * brdfSampleEventToReachTheNewPoint.PdfW; //Die Kürzung des PdfWToPdfA-Umrechnungsfaktor passiert nur im Pfadgewicht. Nicht aber in der Pfad-PdfA (Das sind zwei verschiedene Variablen)
            predecessor.PdfLFromNextPointToThis = 1; //Wir gehen vom Vacuum aus

            if (points.Count > 1)
            {
                var predecessor1 = points[points.Count - 2];
                predecessor1.PdfAReverse = PdfHelper.PdfWToPdfAOrV(brdfSampleEventToReachTheNewPoint.PdfWReverse, predecessor, predecessor1);
            }
            points.Add(newPoint);
        }
    }
}
