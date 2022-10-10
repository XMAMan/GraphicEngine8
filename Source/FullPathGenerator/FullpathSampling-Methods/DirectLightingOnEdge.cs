using System.Collections.Generic;
using System.Linq;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia;
using ParticipatingMedia.DistanceSampling;
using RayTracerGlobal;
using RaytracingLightSource;
using SubpathGenerator;

namespace FullPathGenerator
{
    //Es wird zufällig ein Punkt auf der Kante von ein Eye-Subpath bestimmt und dieser wird mit der Lichtquelle verbunden
    //Der Eye-Subpath muss ohne Distance-Sampling erzeugt werden oder aber mit DistanceSampling und LongRay-Erzeugung. 
    //Man braucht sozusagen ein Strahl, der die komplette Media-Wolke bis zum nächsten Surface-Punkt durchläuft. Nur auf 
    //solchen komplett-langen Linien kann man dann zufällig ein Punkt innerhalb der Wolke auf die Linie erzeugen.
    //Wenn man ohne Distancesampling arbeitet, dann erzeugt das Verfahren diese Pfade: "C {D} P L" -> "C P L", "C D P L", "C D D P L", "C D D D P L", "C D D D D P L"
    //Mit Distancesampling und LongRays: "C {D/P} P L" -> "C P L", "C P P L", "C D P L", "C P D P L"
    //Um das Verfahren komplett zu machen, könnte man noch 'normales' DirectLighting hinzufügen
    public class DirectLightingOnEdge : IFullPathSamplingMethod
    {
        private readonly LightSourceSampler lightSourceSampler;
        private readonly PointToPointConnector pointToPointConnector;
        private readonly int maxPathLength;

        private readonly bool useSegmentSampling = true; //Wenn true, dann wird auf jeden Segment zufällig ein Punkt erzeugt. 
                                                //Ansonsten wird das Segment in stepsPerSegment gleichgroße Teile zerlegt
                                                //D.h. wenn hier false steht, dann solte stepsPerSegment ein Wert größer 1 haben (z.B. 10-20)
        private readonly int stepsPerSegment;
        private readonly bool noDistanceSampling;

        public DirectLightingOnEdge(LightSourceSampler lightSourceSampler, PointToPointConnector pointToPointConnector, PathSamplingType usedEyeSubPathType, int maxPathLength, bool useSegmentSampling)
        {
            this.noDistanceSampling = usedEyeSubPathType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling;
            this.lightSourceSampler = lightSourceSampler;
            this.pointToPointConnector = pointToPointConnector;
            this.maxPathLength = maxPathLength;

            this.useSegmentSampling = useSegmentSampling;
            if (this.useSegmentSampling) this.stepsPerSegment = 1; else this.stepsPerSegment = 20;
        }

        public SamplingMethod Name => SamplingMethod.DirectLightingOnEdge;
        public int SampleCountForGivenPath(FullPath path)
        {
            if (path.PathLength > 2 && path.PathLength <= this.maxPathLength && path.Points[path.Points.Length - 2].LocationType == MediaPointLocationType.MediaParticle) return 1;
            return 0;
        }

        //Erzeuge für jedes Scattering-Segment 'stepsPerSegment' Fullpaths
        public List<FullPath> SampleFullPaths(SubPath eyePath, SubPath lightPath, FullPathFrameData frameData, IRandom rand)
        {
            List<FullPath> paths = new List<FullPath>();
            if (eyePath == null) return paths;

            for (int i = 0; i < eyePath.Points.Length - 1; i++)
            {
                if (i + 2 < this.maxPathLength)
                {
                    foreach (var segment in eyePath.Points[i].LineToNextPoint.Segments)
                    {
                        if (segment.Media.HasScatteringSomeWhereInMedium())
                        {
                            paths.AddRange(CreateNFullPathsFromLineSegment(eyePath.Points[i], segment, this.stepsPerSegment, rand));
                        }
                    }
                }
            }

            return paths;
        }

        //Ich tue in der Methode so, als ob beim Subpatherstellen der Distancesampler gesagt hätte, dass der Punkt auf ein Partikel endet
        private List<FullPath> CreateNFullPathsFromLineSegment(PathPoint eyePoint, VolumeSegment segment, int stepCount, IRandom rand)
        {
            List<FullPath> paths = new List<FullPath>();

            float stepSize = segment.SegmentLength / stepCount;

            for (int i = 0; i < stepCount; i++)
            {
                float t = float.NaN, pdfForSamplingT = float.NaN;
                if (this.useSegmentSampling)
                {
                    bool distanceIsOk = false;
                    for (int j = 0; j < 5; j++)
                    {
                        var tSample = segment.Media.DistanceSampler.SampleRayPositionWithPdfFromRayMinToRayMax(segment.Ray, segment.RayMin, segment.RayMax, rand);
                        t = tSample.RayPosition;
                        pdfForSamplingT = tSample.PdfL;

                        //Hier bei diesen Gleichmäßigen Sampeln kann es passieren, dass t zu nah an Segmentgrenze liegt und man dann das falsche Segment bei der PdfA-Formel auswählt
                        //t = (float)(rand.NextDouble() * segment.SegmentLength + segment.RayMin); //Gleichmäßig sampeln
                        //pdfForSamplingT = 1.0f / segment.SegmentLength;

                        //distanceIsOk = t > segment.RayMin; //Wenn u==0, dann ist die Distance == RayMin. Das ist nicht erlaubt
                        distanceIsOk = t - segment.RayMin > MagicNumbers.MinAllowedPathPointDistance && segment.RayMax - t > MagicNumbers.MinAllowedPathPointDistance;
                        if (distanceIsOk) break;
                    }
                    if (distanceIsOk == false) continue;
                }
                else
                {
                    t = segment.RayMin + i * stepSize + stepSize / 2;
                    pdfForSamplingT = 1;
                }

                //t = segment.RayMin + MagicNumbers.MinAllowedPathPointDistance; //Hiermit kann ich ein Grenzfall testen
                var subLine = eyePoint.LineToNextPoint.CreateLongMediaSubLine(t);

                if (subLine.EndPoint.CurrentMedium.HasScatteringOnPoint(subLine.EndPoint.Position) == false) continue;

                var toLightDirection = this.lightSourceSampler.GetRandomPointOnLight(subLine.EndPoint.Position, rand); //Ligthsource-Sampling
                if (toLightDirection == null) continue;

                Vector3D newDirection = subLine.EndPoint.Position - eyePoint.Position;
                float newDirectionLength = newDirection.Length();
                if (newDirectionLength < MagicNumbers.MinAllowedPathPointDistance) continue;
                newDirection /= newDirectionLength;
                eyePoint.BrdfSampleEventOnThisPoint.Ray.Direction = newDirection; //Korrigiere die Richtung da es numerische Fehler enthält
                
                Vector3D pathweightOnPointT = Vector3D.Mult(eyePoint.PathWeight, eyePoint.BrdfSampleEventOnThisPoint.Brdf);
                if (this.useSegmentSampling)
                {
                    pathweightOnPointT /= this.stepsPerSegment;
                    pathweightOnPointT /= pdfForSamplingT;
                }
                else
                    pathweightOnPointT *= stepSize;

                //Der GeometryTerm und der PdfW-To-PdfA-Umrechnungsfaktor zwischen dem eyePoint und dem pointOnT kürzen sich gegenseitig weg. Somit bleibt nur noch der AttenuationTerm übrig
                pathweightOnPointT = Vector3D.Mult(pathweightOnPointT, subLine.AttenuationWithoutPdf());


                PathPoint pointOnT = PathPoint.CreateMediaParticlePoint(subLine.EndPoint, pathweightOnPointT);
                pointOnT.PdfA = eyePoint.PdfA * pdfForSamplingT * PdfHelper.PdfWToPdfAOrV(eyePoint.BrdfSampleEventOnThisPoint.PdfW, eyePoint, pointOnT);
                pointOnT.AssociatedPath = eyePoint.AssociatedPath;
                pointOnT.Predecessor = eyePoint;

                var connectData = this.pointToPointConnector.TryToConnectToLightSource(pointOnT, toLightDirection);
                if (connectData == null) continue;

                pointOnT.LineToNextPoint = connectData.LineFromEyePointToLightSource;

                //Der LightPoint, welcher durch den Visible-Test erzeugt wurde liegt nicht exakt dort, wo der gesampelte LightPoint liegt. Deswegen kommt es zu PdfA-Abweichungen/MIS-OutOfRange-Exception. Hiermit vermeide ich diesen Fehler.
                double directLightingPdfA = this.lightSourceSampler.GetDirectLightingPdfA(subLine.EndPoint.Position, connectData.LightPoint, eyePoint.AssociatedPath.PathCreationTime);
                if (directLightingPdfA == 0) continue;

                var path = CreatePath(new PathPoint(eyePoint) { LineToNextPoint = subLine }, pointOnT, connectData, directLightingPdfA);
                paths.Add(path);
            }

            return paths;
        }
        private FullPath CreatePath(PathPoint eyePoint, PathPoint pointOnT, EyePoint2LightSourceConnectionData connectData, double directLightingPdfA)
        {
            var eyePath = eyePoint.AssociatedPath;
            var lightPoint = connectData.LightPoint;

            double pathPdfA = pointOnT.PdfA * directLightingPdfA * this.stepsPerSegment;

            Vector3D pathContribution = Vector3D.Mult(Vector3D.Mult(pointOnT.PathWeight, connectData.EyeBrdf.Brdf), lightPoint.Color) * connectData.GeometryTerm * this.lightSourceSampler.GetEmissionForEyePathHitLightSourceDirectly(lightPoint, eyePoint.Position, connectData.DirectionToLightPoint) / (float)directLightingPdfA;// toLightDirection.PdfA;
            pathContribution = Vector3D.Mult(pathContribution, connectData.AttenuationTerm);
            if (this.useSegmentSampling) pathContribution /= this.stepsPerSegment;

            var points = new FullPathPoint[eyePoint.Index + 3];
            double lightPdfA = this.lightSourceSampler.PdfAFromRandomPointOnLightSourceSampling(lightPoint);

            //Eye-PdfAs
            var linePdf = this.noDistanceSampling ? new DistancePdf() { PdfL = 1, ReversePdfL = 1 } : eyePoint.LineToNextPoint.GetPdfLIfDistanceSamplingWouldBeUsed();
            double eyePointPdfAOnT = eyePoint.PdfA * PdfHelper.PdfWToPdfAOrV(eyePoint.BrdfSampleEventOnThisPoint.PdfW, eyePoint, pointOnT) * linePdf.PdfL; //pointOnT.PdfA;

            //Punkt auf Lichtquelle
            PathPoint pathPointOnLight = PathPoint.CreateLightsourcePointWithSurroundingMedia(lightPoint, null, connectData.MediaLightPoint, connectData.LighIsInfinityAway);
            float pdfWFromLightDirectionSampling = this.lightSourceSampler.PdfWFromLightDirectionSampling(lightPoint, -connectData.DirectionToLightPoint);
            points[points.Length - 1] = new FullPathPoint(pathPointOnLight, null, null, float.NaN, pdfWFromLightDirectionSampling, BrdfCreator.BrdfSampling) { EyePdfA = eyePointPdfAOnT * PdfHelper.PdfWToPdfAOrV(connectData.EyeBrdf.PdfW, pointOnT, pathPointOnLight) * connectData.PdfLForEyepointToLightSource.PdfL, LightPdfA = lightPdfA };
            points[points.Length - 1].Point.AssociatedPath = eyePath;

            //pointOnT
            lightPdfA = lightPdfA * PdfHelper.PdfWToPdfAOrV(pdfWFromLightDirectionSampling, pathPointOnLight, pointOnT) * connectData.PdfLForEyepointToLightSource.ReversePdfL;
            points[points.Length - 2] = new FullPathPoint(pointOnT, connectData.LineFromEyePointToLightSource, null, connectData.EyeBrdf.PdfW, connectData.EyeBrdf.PdfWReverse, BrdfCreator.BrdfEvaluation) { EyePdfA = eyePointPdfAOnT, LightPdfA = lightPdfA }; //Die InputDirection enthält Fehler

            //eyePoint
            lightPdfA = lightPdfA * PdfHelper.PdfWToPdfAOrV(connectData.EyeBrdf.PdfWReverse, pointOnT, eyePoint) * linePdf.ReversePdfL;
            points[points.Length - 3] = new FullPathPoint(eyePoint, eyePoint.LineToNextPoint, null, eyePoint.BrdfSampleEventOnThisPoint.PdfW, eyePoint.BrdfSampleEventOnThisPoint.PdfWReverse, BrdfCreator.BrdfSampling) { EyePdfA = eyePoint.PdfA, LightPdfA = lightPdfA, PdfWContainsNumericErrors = true };

            for (int i = eyePoint.Index - 1; i >= 0; i--)
            {
                lightPdfA = lightPdfA * eyePath.Points[i].PdfAReverse * eyePath.Points[i].PdfLFromNextPointToThis;
                points[i] = new FullPathPoint(eyePath.Points[i], eyePath.Points[i].LineToNextPoint, null, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfW, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfWReverse, BrdfCreator.BrdfSampling)
                {
                    EyePdfA = eyePath.Points[i].PdfA,
                    LightPdfA = lightPdfA,
                };
            }

            return new FullPath(pathContribution, pathPdfA, points, this);
        }

        public double GetPathPdfAForAGivenPath(FullPath path, FullPathFrameData frameData)
        {
            if (path.Points.Length < 3 || path.Points.Length > this.maxPathLength) return 0;
            if (path.Points[path.Points.Length - 2].LocationType != MediaPointLocationType.MediaParticle) return 0;

            if (path.Points[path.Points.Length - 3].EyeLineToNext == null ||
                path.Points[path.Points.Length - 3].EyeLineToNext.EndPoint.Position != path.Points[path.Points.Length - 2].Position ||
                path.Points[path.Points.Length - 3].EyeLineToNext.Segments.Count <= path.Points[path.Points.Length - 3].EyeLineToNext.ShortRaySegmentCount)
            {
                var longLine = this.pointToPointConnector.CreateLongLineWithEndPointOnGivenParticle(path.Points[path.Points.Length - 3].Point, path.Points[path.Points.Length - 2].Point);
                if (longLine == null) return 0; //Wenn Particle zu kleinen Abstand zur dahinter liegenden Wand hat dann ist LongRay-Erzeugung nicht möglich
                path.Points[path.Points.Length - 3].EyeLineToNext = longLine;
            }

            double directLightingPdfA = this.lightSourceSampler.GetDirectLightingPdfA(path.Points[path.Points.Length - 2].Position, path.Points.Last().Point.SurfacePoint, path.Points.First().Point.AssociatedPath.PathCreationTime);

            var mediaLine = path.Points[path.Points.Length - 3].EyeLineToNext;
            int ti = mediaLine.ShortRaySegmentCount - 1;
            var segments = mediaLine.Segments;

            //t liegt zwischen den Segmenten ti und (ti+1)
            float pdfForSamplingT;
            if (this.useSegmentSampling)
            {
                pdfForSamplingT = segments[ti].Media.DistanceSampler.GetSamplePdfFromRayMinToRayMax(segments[ti].Ray, segments[ti].RayMin, segments[ti + 1].RayMax, segments[ti].RayMax).PdfL;

                //Nimm nur die beiden Segmente, welchen vor und hinter dem Particle an Punkt t liegen und berechne deren Längensumme (Wenn ich gleichmäßig sample)
                //float scatteringLength = segments[ti].SegmentLength + segments[ti + 1].SegmentLength;
                //pdfForSamplingT = 1.0f / scatteringLength * this.stepsPerSegment;
            }
            else
            {
                pdfForSamplingT = 1;
            }

            double eyePointPdfA = path.Points[path.Points.Length - 3].EyePdfA;
            double pointOnTPdfA = PdfHelper.PdfWToPdfAOrV(path.Points[path.Points.Length - 3].EyePdfWOnThisPoint, path.Points[path.Points.Length - 3].Point, path.Points[path.Points.Length - 2].Point);
            return eyePointPdfA * pointOnTPdfA * directLightingPdfA * pdfForSamplingT * this.stepsPerSegment;
        }
    }
}
