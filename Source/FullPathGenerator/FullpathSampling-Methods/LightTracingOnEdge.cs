using System.Collections.Generic;
using System.Linq;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia;
using ParticipatingMedia.DistanceSampling;
using RayCameraNamespace;
using RayTracerGlobal;
using SubpathGenerator;

namespace FullPathGenerator
{
    public class LightTracingOnEdge : IFullPathSamplingMethod
    {
        private readonly IRayCamera rayCamera;
        private readonly PointToPointConnector pointToPointConnector;
        private readonly bool noDistanceSampling;

        public LightTracingOnEdge(IRayCamera rayCamera, PointToPointConnector pointToPointConnector, PathSamplingType usedLightSubPathType)
        {
            this.noDistanceSampling = usedLightSubPathType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling;
            this.rayCamera = rayCamera;
            this.pointToPointConnector = pointToPointConnector;
        }

        public SamplingMethod Name => SamplingMethod.LightTracingOnEdge;
        public List<FullPath> SampleFullPaths(SubPath eyePath, SubPath lightPath, FullPathFrameData frameData, IRandom rand)
        {
            List<FullPath> paths = new List<FullPath>();
            if (lightPath == null) return paths;

            for (int i = 0; i < lightPath.Points.Length - 1; i++)
            {
                foreach (var segment in lightPath.Points[i].LineToNextPoint.Segments)
                {
                    if (segment.Media.HasScatteringSomeWhereInMedium())
                    {
                        paths.AddRange(CreateFullPathFromLineSegment(lightPath.Points[i], segment, rand));
                    }
                }
            }

            return paths;
        }

        private List<FullPath> CreateFullPathFromLineSegment(PathPoint lightPoint, VolumeSegment segment, IRandom rand)
        {
            List<FullPath> paths = new List<FullPath>();
            float t = float.NaN, pdfForSamplingT = float.NaN;
            bool distanceIsOk = false;
            for (int j = 0; j < 5; j++)
            {
                var tSample = segment.Media.DistanceSampler.SampleRayPositionWithPdfFromRayMinToRayMax(segment.Ray, segment.RayMin, segment.RayMax, rand);
                t = tSample.RayPosition;
                pdfForSamplingT = tSample.PdfL;

                //distanceIsOk = t > segment.RayMin; //Wenn u==0, dann ist die Distance == RayMin. Das ist nicht erlaubt
                distanceIsOk = t - segment.RayMin > MagicNumbers.MinAllowedPathPointDistance && segment.RayMax - t > MagicNumbers.MinAllowedPathPointDistance;
                if (distanceIsOk) break;
            }
            if (distanceIsOk == false) return paths;

            var subLine = lightPoint.LineToNextPoint.CreateLongMediaSubLine(t);

            if (subLine.EndPoint.CurrentMedium.HasScatteringOnPoint(subLine.EndPoint.Position) == false) return paths;

            lightPoint.BrdfSampleEventOnThisPoint.Ray.Direction = Vector3D.Normalize(subLine.EndPoint.Position - lightPoint.Position); //Korrigiere die Richtung da es numerische Fehler enthält
            Vector3D pathweightOnPointT = Vector3D.Mult(lightPoint.PathWeight, lightPoint.BrdfSampleEventOnThisPoint.Brdf);
            pathweightOnPointT /= pdfForSamplingT;

            //Der GeometryTerm und der PdfW-To-PdfA-Umrechnungsfaktor zwischen dem eyePoint und dem pointOnT kürzen sich gegenseitig weg. Somit bleibt nur noch der AttenuationTerm übrig
            pathweightOnPointT = Vector3D.Mult(pathweightOnPointT, subLine.AttenuationWithoutPdf());

            PathPoint pointOnT = PathPoint.CreateMediaParticlePoint(subLine.EndPoint, pathweightOnPointT);
            pointOnT.PdfA = lightPoint.PdfA * PdfHelper.PdfWToPdfAOrV(lightPoint.BrdfSampleEventOnThisPoint.PdfW, lightPoint, pointOnT);
            pointOnT.AssociatedPath = lightPoint.AssociatedPath;
            pointOnT.Predecessor = lightPoint;
            pointOnT.Index = lightPoint.Index + 1;

            var connectData = this.pointToPointConnector.TryToConnectToCamera(pointOnT);

            if (connectData != null)
            {
                var cameraPoint = connectData.CameraPoint;
                cameraPoint.AssociatedPath = lightPoint.AssociatedPath;
                cameraPoint.LineToNextPoint = connectData.LineFromCameraToLightPoint;

                if (this.rayCamera.SamplingMode == PixelSamplingMode.Tent)
                    paths.AddRange(CreatePathForEachNeighborPixel(cameraPoint, pointOnT, pdfForSamplingT, new PathPoint(lightPoint) { LineToNextPoint = subLine }, connectData));
                else
                    paths.Add(CreatePath(cameraPoint, pointOnT, pdfForSamplingT, new PathPoint(lightPoint) { LineToNextPoint = subLine }, connectData.PixelPosition, connectData));
            }

            return paths;
        }

        private List<FullPath> CreatePathForEachNeighborPixel(PathPoint cameraPoint, PathPoint pointOnT, float pdfForSamplingT, PathPoint lightPoint, LightPoint2CameraConnectionData connectData)
        {
            float f = 1;

            return new List<FullPath>()
            {
                 CreatePath(cameraPoint, pointOnT, pdfForSamplingT, lightPoint, connectData.PixelPosition + new Vector2D(+0, +0), connectData),
                 CreatePath(cameraPoint, pointOnT, pdfForSamplingT, lightPoint, connectData.PixelPosition + new Vector2D(-f, -f), connectData),
                 CreatePath(cameraPoint, pointOnT, pdfForSamplingT, lightPoint, connectData.PixelPosition + new Vector2D(-f, +f), connectData),
                 CreatePath(cameraPoint, pointOnT, pdfForSamplingT, lightPoint, connectData.PixelPosition + new Vector2D(+f, -f), connectData),
                 CreatePath(cameraPoint, pointOnT, pdfForSamplingT, lightPoint, connectData.PixelPosition + new Vector2D(+f, +f), connectData),
                 CreatePath(cameraPoint, pointOnT, pdfForSamplingT, lightPoint, connectData.PixelPosition + new Vector2D(+0, -f), connectData),
                 CreatePath(cameraPoint, pointOnT, pdfForSamplingT, lightPoint, connectData.PixelPosition + new Vector2D(+0, +f), connectData),
                 CreatePath(cameraPoint, pointOnT, pdfForSamplingT, lightPoint, connectData.PixelPosition + new Vector2D(-f, 0), connectData),
                 CreatePath(cameraPoint, pointOnT, pdfForSamplingT, lightPoint, connectData.PixelPosition + new Vector2D(+f, 0), connectData),
            }.Where(x => x != null).ToList();
        }

        private FullPath CreatePath(PathPoint cameraPoint, PathPoint pointOnT, float pdfForSamplingT, PathPoint lightPoint, Vector2D pixelPosition, LightPoint2CameraConnectionData connectData)
        {
            SubPath lightPath = pointOnT.AssociatedPath;

            float cameraPdfW = this.rayCamera.GetPixelPdfW((int)pixelPosition.X, (int)pixelPosition.Y, connectData.CameraToLightPointDirection);
            if (cameraPdfW == 0) return null;

            Vector3D pathContribution = Vector3D.Mult(pointOnT.PathWeight, connectData.LightBrdf.Brdf) * connectData.GeometryTerm / this.rayCamera.PixelCountFromScreen * cameraPdfW;
            pathContribution = Vector3D.Mult(pathContribution, connectData.AttenuationTerm);
            double pathPdfA = pointOnT.PdfA * pdfForSamplingT * this.rayCamera.PixelCountFromScreen;

            var points = new FullPathPoint[pointOnT.Index + 2];
            double eyePdfA = 1;

            var linePdf = this.noDistanceSampling ? new DistancePdf() { PdfL = 1, ReversePdfL = 1 } : lightPoint.LineToNextPoint.GetPdfLIfDistanceSamplingWouldBeUsed();

            points[0] = new FullPathPoint(cameraPoint, cameraPoint.LineToNextPoint, null, cameraPdfW, float.NaN, BrdfCreator.BrdfSampling)
            {
                EyePdfA = 1,
                LightPdfA = pointOnT.PdfA * linePdf.PdfL * PdfHelper.PdfWToPdfAOrV(connectData.LightBrdf.PdfW, pointOnT, cameraPoint) * connectData.PdfLForCameraToLightPoint.ReversePdfL
            };

            eyePdfA *= PdfHelper.PdfWToPdfAOrV(cameraPdfW, cameraPoint, pointOnT) * connectData.PdfLForCameraToLightPoint.PdfL;
            points[1] = new FullPathPoint(pointOnT, null, null, connectData.LightBrdf.PdfWReverse, connectData.LightBrdf.PdfW, BrdfCreator.BrdfEvaluation)
            {
                EyePdfA = eyePdfA,
                LightPdfA = pointOnT.PdfA * linePdf.PdfL
            };
            //points[1].Point.BrdfSampleEventToReachThisPoint.PdfW = cameraPdfW;

            double pdfAReverseIndex2 = PdfHelper.PdfWToPdfAOrV(connectData.LightBrdf.PdfWReverse, pointOnT, lightPoint);
            eyePdfA = eyePdfA * pdfAReverseIndex2 * linePdf.ReversePdfL;
            points[2] = new FullPathPoint(lightPoint, null, lightPoint.LineToNextPoint, lightPoint.BrdfSampleEventOnThisPoint.PdfWReverse, lightPoint.BrdfSampleEventOnThisPoint.PdfW, BrdfCreator.BrdfSampling)
            {
                EyePdfA = eyePdfA,
                LightPdfA = lightPoint.PdfA,
                PdfWContainsNumericErrors = true
            };

            for (int i = 3, j = pointOnT.Index - 2; i < points.Length; i++, j--)
            {
                eyePdfA = eyePdfA * lightPath.Points[j].PdfAReverse * lightPath.Points[j].PdfLFromNextPointToThis;
                points[i] = new FullPathPoint(lightPath.Points[j], null, lightPath.Points[j].LineToNextPoint, lightPath.Points[j].BrdfSampleEventOnThisPoint.PdfWReverse, lightPath.Points[j].BrdfSampleEventOnThisPoint.PdfW, BrdfCreator.BrdfSampling)
                {
                    EyePdfA = eyePdfA,
                    LightPdfA = lightPath.Points[j].PdfA
                };
            }

            FullPath path = new FullPath(pathContribution, pathPdfA, points, this)
            {
                PixelPosition = pixelPosition
            };
            return path;
        }

        public int SampleCountForGivenPath(FullPath path)
        {
            if (path.PathLength > 2 && path.Points[1].LocationType == MediaPointLocationType.MediaParticle) return 1;
            return 0;
        }

        public double GetPathPdfAForAGivenPath(FullPath path, FullPathFrameData frameData)
        {
            if (path.Points[1].LocationType != MediaPointLocationType.MediaParticle) return 0;

            if (path.Points[2].LightLineToNext == null ||
                path.Points[2].LightLineToNext.EndPoint.Position != path.Points[1].Point.MediaPoint.Position ||
                path.Points[2].LightLineToNext.Segments.Count <= path.Points[2].LightLineToNext.ShortRaySegmentCount)
            {
                var longLine = this.pointToPointConnector.CreateLongLineWithEndPointOnGivenParticle(path.Points[2].Point, path.Points[1].Point);
                if (longLine == null) return 0; //Wenn Particle zu kleinen Abstand zur dahinter liegenden Wand hat dann ist LongRay-Erzeugung nicht möglich
                path.Points[2].LightLineToNext = longLine;
            }

            var mediaLine = path.Points[2].LightLineToNext;
            int ti = mediaLine.ShortRaySegmentCount - 1;
            var segments = mediaLine.Segments;

            //t liegt zwischen den Segmenten ti und (ti+1)
            float pdfForSamplingT = segments[ti].Media.DistanceSampler.GetSamplePdfFromRayMinToRayMax(segments[ti].Ray, segments[ti].RayMin, segments[ti + 1].RayMax, segments[ti].RayMax).PdfL;

            double lightPointPdfA = path.Points[2].LightPdfA;
            double pointOnTPdfA = PdfHelper.PdfWToPdfAOrV(path.Points[2].LightPdfWOnThisPoint, path.Points[2].Point, path.Points[1].Point);
            return lightPointPdfA * pointOnTPdfA * pdfForSamplingT * this.rayCamera.PixelCountFromScreen;
        }
    }
}
