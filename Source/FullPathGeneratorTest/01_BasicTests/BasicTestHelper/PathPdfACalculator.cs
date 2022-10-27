using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FullPathGenerator;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using RayCameraNamespace;
using RayTracerGlobal;
using SubpathGenerator;

namespace FullPathGeneratorTest._01_BasicTests.BasicTestHelper
{
    class PathPdfACalculatorInputData
    {
        public PathSamplingType EyePathSamplingType;
        public PathSamplingType LightPathSamplingType;
        public RayCamera Camera;
        public int PixX = 1;
        public int PixY = 1;
        public bool SceneHasMedia;
        public float ScatteringFromMedia = 0.1f * 2;
        public float AbsorbationFromMedia = 0.05f * 2;
        public float ScatteringFromGlobalMedia = 0;
        public float AnisotrophyCoeffizient = 0;
        public float LightSourceArea;
        public float MaxMediaLineEndToPathPointDistance;  //Maximale Distance zwischen MediaLine-Endpoint und PathPoint, auf dem die MediaLine zeigt
        public float KernelDistance;
        public float MaxAllowedError;
    }

    class PathPdfACalculator
    {
        private readonly PathPdfACalculatorInputData data;
        private double maxDifference = 0; //Die maximal gefundene Differenz
        private readonly float maxPdfWErrorForNumericErrorPoints = 0.001f; //Bei Punkten, wo PdfWContainsNumericErrors==true steht, ist das hier der maximal erlaubte Fehler 
        private readonly float maxPdfWErrorForMergingPoints = 4.0f;
        public PathPdfACalculator(PathPdfACalculatorInputData data)
        {
            this.data = data;
        }

        public double CheckEyePdfA(FullPath path)
        {
            this.maxDifference = 0;

            StringBuilder debugLog = new StringBuilder();

            float cameraPdfW = this.data.Camera.GetPixelPdfW(this.data.PixX, this.data.PixY, Vector3D.Normalize(path.Points[1].Position - path.Points[0].Position));
            if (path.Points[0].PdfWContainsNumericErrors)
            {
                Assert.IsTrue(Math.Abs(cameraPdfW - path.Points[0].Point.BrdfSampleEventOnThisPoint.PdfW) < maxPdfWErrorForNumericErrorPoints);
                cameraPdfW = path.Points[0].Point.BrdfSampleEventOnThisPoint.PdfW;
            }
            CheckDifference(cameraPdfW, path.Points[0].EyePdfWOnThisPoint, "EyePdfW");

            double eyePdfA = 1;
            for (int i = 0; i < path.Points.Length - 1; i++)
            {
                debugLog.AppendLine($"EyePdfA[{i}]={eyePdfA:R}");

                CheckDifference(eyePdfA, path.Points[i].EyePdfA, $"EyePdfA[{i}]");

                if (path.Points[i].LocationType == MediaPointLocationType.Camera)
                {                    
                    float pdfAOnNextPoint = PdfWToPdfAOrV(cameraPdfW, path.Points[i].Position, path.Points[i + 1].Point, debugLog);

                    float pdfL = GetPdfLForEyeDirection(path.Points[i], path.Points[i + 1]);

                    debugLog.AppendLine($"* PdfL={pdfL:R}");
                    debugLog.AppendLine($"* PdfW*continuationPdf={cameraPdfW:R}");

                    eyePdfA = eyePdfA * pdfAOnNextPoint * pdfL;
                }
                else if (path.Points[i].LocationType == MediaPointLocationType.Surface)
                {
                    var surfacePoint = path.Points[i].Point.SurfacePoint;

                    float continuationPdf = Math.Min(1, Math.Max(MagicNumbers.MinSurfaceContinuationPdf, path.Points[i].Point.BrdfPoint.ContinuationPdf));

                    Vector3D directionToNextPoint = Vector3D.Normalize(path.Points[i + 1].Position - path.Points[i].Position);
                    float diffusePdfW = Math.Max(MagicNumbers.MinAllowedPdfW, Math.Max(0, surfacePoint.OrientedFlatNormal * directionToNextPoint) / (float)Math.PI);
                    

                    debugLog.AppendLine($"* continuationPdf={continuationPdf:R}");
                    diffusePdfW *= continuationPdf;

                    if (path.Points[i].PdfWContainsNumericErrors)
                    {
                        Assert.IsTrue(Math.Abs(diffusePdfW - path.Points[i].EyePdfWOnThisPoint) < maxPdfWErrorForNumericErrorPoints);
                        diffusePdfW = path.Points[i].EyePdfWOnThisPoint;
                    }
                    if (path.Points[i].IsMergingPoint == false) CheckDifference(diffusePdfW, path.Points[i].EyePdfWOnThisPoint, "EyePdfW");

                    float pdfAOnNextPoint = PdfWToPdfAOrV(diffusePdfW, path.Points[i].Position, path.Points[i + 1].Point, debugLog);

                    float pdfL = GetPdfLForEyeDirection(path.Points[i], path.Points[i + 1]);
    
                    debugLog.AppendLine($"* PdfL={pdfL:R}");
                    debugLog.AppendLine($"* PdfW={diffusePdfW:R}");

                    eyePdfA = eyePdfA * pdfAOnNextPoint *pdfL;

                    if (path.Points[i].IsMergingPoint) eyePdfA = path.Points[i + 1].EyePdfA; //Nächster Punkt nach Mergingpunkt muss wegen falscher LineToNext(PdfL) korrigiert werden
                }
                else if (path.Points[i].LocationType == MediaPointLocationType.MediaParticle)
                {
                    float continuationPdf = Math.Max(MagicNumbers.MediaMinContinuationPdf, this.data.ScatteringFromMedia / (this.data.ScatteringFromMedia + this.data.AbsorbationFromMedia));

                    float phaseFunctionPdfW;
                    if (this.data.AnisotrophyCoeffizient == 0)
                    {
                        phaseFunctionPdfW = 1.0f / (4 * (float)Math.PI); //isotrophicPdfW
                    }else
                    {
                        //anisotrophicPdfW
                        phaseFunctionPdfW = HenyeyGreensteinPhaseFunction(path.Points, i-1, i, i+1, this.data.AnisotrophyCoeffizient);
                    }
                    

                    debugLog.AppendLine($"* continuationPdf={continuationPdf:R}");
                    phaseFunctionPdfW *= continuationPdf;

                    if (path.Points[i].PdfWContainsNumericErrors)
                    {
                        if (path.Points[i+1].IsMergingPoint) //Merging-Punkt liegt genau auf EyePoint-Linie. Deswegen kann ich hier von kleinen Fehler ausgehen
                            Assert.IsTrue(Math.Abs(phaseFunctionPdfW - path.Points[i].EyePdfWOnThisPoint) < maxPdfWErrorForNumericErrorPoints);
                        else
                            Assert.IsTrue(Math.Abs(phaseFunctionPdfW - path.Points[i].EyePdfWOnThisPoint) < maxPdfWErrorForMergingPoints); //Die Eye-PdfW nach dem MergingPoint enthält ein besonders großen Fehler

                        phaseFunctionPdfW = path.Points[i].EyePdfWOnThisPoint;
                    }
                    if (path.Points[i].IsMergingPoint == false) CheckDifference(phaseFunctionPdfW, path.Points[i].EyePdfWOnThisPoint, "EyePdfW");
    
                    float pdfAOnNextPoint = PdfWToPdfAOrV(phaseFunctionPdfW, path.Points[i].Position, path.Points[i + 1].Point, debugLog);

                    float pdfL = GetPdfLForEyeDirection(path.Points[i], path.Points[i + 1]);

                    debugLog.AppendLine($"* PdfL={pdfL:R}");
                    debugLog.AppendLine($"* PdfW={phaseFunctionPdfW:R}");

                    eyePdfA = eyePdfA * pdfAOnNextPoint * pdfL;

                    if (path.Points[i].IsMergingPoint) eyePdfA = path.Points[i + 1].EyePdfA;
                }
                else if (path.Points[i].LocationType == MediaPointLocationType.MediaBorder)
                {
                    float continuationPdf = 1;

                    float pdfw = 1;

                    CheckDifference(pdfw * continuationPdf, path.Points[i].EyePdfWOnThisPoint, "EyePdfW");

                    float pdfAOnNextPoint = PdfWToPdfAOrV(pdfw, path.Points[i].Position, path.Points[i + 1].Point, debugLog);

                    float pdfL = GetPdfLForEyeDirection(path.Points[i], path.Points[i + 1]);

                    _ = debugLog.AppendLine($"* PdfL={pdfL:R}");
                    debugLog.AppendLine($"* PdfW*continuationPdf={pdfw * continuationPdf:R}");

                    eyePdfA = eyePdfA * continuationPdf * pdfAOnNextPoint * pdfL;
                    if (path.Points[i].IsMergingPoint) eyePdfA = path.Points[i + 1].EyePdfA; //Nächster Punkt nach Mergingpunkt muss wegen falscher LineToNext(PdfL) korrigiert werden
                }
                else
                {
                    throw new Exception("FullPaths dürfen momentan nur auf Surface, Camera und MediaParticeln liegen");
                }
            }

            debugLog.AppendLine($"== EyePdfA={eyePdfA:R}");
            string debugString = debugLog.ToString();

            CheckDifference(eyePdfA, path.Points.Last().EyePdfA, "EyePdfA");

            return this.maxDifference;
        }

        public double CheckLightPdfA(FullPath path)
        {
            this.maxDifference = 0;
            StringBuilder debugLog = new StringBuilder();

            float lightPdfAF = 1.0f / this.data.LightSourceArea;
            double lightPdfA = (double)lightPdfAF;

            for (int i = path.Points.Length - 1; i > 0; i--)
            {
                debugLog.AppendLine($"LightPdfA[{i}]={lightPdfA:R}");

                CheckDifference(lightPdfA, path.Points[i].LightPdfA, $"LightPdfA[{i}]");

                if (path.Points[i].LocationType == MediaPointLocationType.Surface)
                {
                    var surfacePoint = path.Points[i].Point.SurfacePoint;

                    float continuationPdf = surfacePoint.IsLocatedOnLightSource ? 1 : Math.Min(1, Math.Max(MagicNumbers.MinSurfaceContinuationPdf, path.Points[i].Point.BrdfPoint.ContinuationPdf));

                    Vector3D directionToNextPoint = Vector3D.Normalize(path.Points[i - 1].Position - path.Points[i].Position);
                    float diffusePdfW = Math.Max(MagicNumbers.MinAllowedPdfW, Math.Max(0, surfacePoint.OrientedFlatNormal * directionToNextPoint) / (float)Math.PI);

                    debugLog.AppendLine($"* continuationPdf={continuationPdf:R}");
                    diffusePdfW *= continuationPdf;

                    if (path.Points[i].PdfWContainsNumericErrors)
                    {
                        if (path.Points[i-1].IsMergingPoint) //Eins vor dem MergingPunkt (Die PdfW, die zum MergingPunkt zeigt, ist besonders ungenau)
                        {
                            Assert.IsTrue(Math.Abs(diffusePdfW - path.Points[i].LightPdfWOnThisPoint) < 0.1);
                        }else
                        {
                            Assert.IsTrue(Math.Abs(diffusePdfW - path.Points[i].LightPdfWOnThisPoint) < maxPdfWErrorForNumericErrorPoints);
                        }
                        
                        diffusePdfW = path.Points[i].LightPdfWOnThisPoint;
                    }
                    if (path.Points[i].IsMergingPoint == false) CheckDifference(diffusePdfW, path.Points[i].LightPdfWOnThisPoint, "LightPdfW");

                    float pdfAOnNextPoint = PdfWToPdfAOrV(diffusePdfW, path.Points[i].Position, path.Points[i - 1].Point, debugLog);

                    float pdfL = GetPdfLForLightDirection(path.Points[i], path.Points[i - 1]);

                    debugLog.AppendLine($"* PdfL={pdfL:R}");
                    debugLog.AppendLine($"* PdfW={diffusePdfW:R}");

                    lightPdfA = lightPdfA * pdfAOnNextPoint * pdfL;
                    if (path.Points[i - 1].IsMergingPoint) lightPdfA = path.Points[i - 1].LightPdfA; //Nächster Punkt ist Mergingpunkt? Überschreibe seine PdfA
                }
                else if (path.Points[i].LocationType == MediaPointLocationType.MediaParticle)
                {
                    float continuationPdf = Math.Max(MagicNumbers.MediaMinContinuationPdf, this.data.ScatteringFromMedia / (this.data.ScatteringFromMedia + this.data.AbsorbationFromMedia));

                    float phaseFunctionPdfW;
                    if (this.data.AnisotrophyCoeffizient == 0)
                    {
                        phaseFunctionPdfW = 1.0f / (4 * (float)Math.PI); //isotrophicPdfW
                    }
                    else
                    {
                        //anisotrophicPdfW
                        phaseFunctionPdfW = HenyeyGreensteinPhaseFunction(path.Points, i+1, i, i-1, this.data.AnisotrophyCoeffizient);
                    }

                    debugLog.AppendLine($"* continuationPdf={continuationPdf}");
                    phaseFunctionPdfW *= continuationPdf;

                    if (path.Points[i].PdfWContainsNumericErrors)
                    {
                        if (path.Points[i-1].IsMergingPoint) //Die LightPdfW vor dem MergingPunkt enthält ein besonders großen Fehler
                            Assert.IsTrue(Math.Abs(phaseFunctionPdfW - path.Points[i].LightPdfWOnThisPoint) < maxPdfWErrorForMergingPoints);
                        else
                            Assert.IsTrue(Math.Abs(phaseFunctionPdfW - path.Points[i].LightPdfWOnThisPoint) < maxPdfWErrorForNumericErrorPoints);
                        
                        phaseFunctionPdfW = path.Points[i].LightPdfWOnThisPoint;
                    }
                    if (path.Points[i].IsMergingPoint)
                    {
                        Assert.IsTrue(Math.Abs(phaseFunctionPdfW - path.Points[i].LightPdfWOnThisPoint) < maxPdfWErrorForMergingPoints);
                        phaseFunctionPdfW = path.Points[i].LightPdfWOnThisPoint;
                    }
                    if (path.Points[i].IsMergingPoint == false) CheckDifference(phaseFunctionPdfW, path.Points[i].LightPdfWOnThisPoint, "LightPdfW");

                    float pdfAOnNextPoint = PdfWToPdfAOrV(phaseFunctionPdfW, path.Points[i].Position, path.Points[i - 1].Point, debugLog);

                    float pdfL = GetPdfLForLightDirection(path.Points[i], path.Points[i - 1]);

                    debugLog.AppendLine($"* PdfL={pdfL:R}");
                    debugLog.AppendLine($"* PdfW={phaseFunctionPdfW:R}");

                    lightPdfA = lightPdfA * pdfAOnNextPoint * pdfL;
                    if (path.Points[i - 1].IsMergingPoint) lightPdfA = path.Points[i - 1].LightPdfA; //Nächster Punkt ist Mergingpunkt? Überschreibe seine PdfA
                }
                else if (path.Points[i].LocationType == MediaPointLocationType.MediaBorder)
                {
                    float continuationPdf = 1;

                    float pdfW = 1;

                    if (path.Points[i - 1].IsMergingPoint == false) CheckDifference(pdfW * continuationPdf, path.Points[i].LightPdfWOnThisPoint, "LightPdfW");

                    float pdfAOnNextPoint = PdfWToPdfAOrV(pdfW, path.Points[i].Position, path.Points[i - 1].Point, debugLog);

                    float pdfL = GetPdfLForLightDirection(path.Points[i], path.Points[i - 1]);

                    debugLog.AppendLine($"* PdfL={pdfL:R}");
                    debugLog.AppendLine($"* PdfW*continuationPdf={pdfW * continuationPdf:R}");

                    lightPdfA = lightPdfA * continuationPdf * pdfAOnNextPoint * pdfL;
                    if (path.Points[i - 1].IsMergingPoint) lightPdfA = path.Points[i - 1].LightPdfA;
                }
                else
                {
                    throw new Exception("FullPaths dürfen momentan nur auf Surface, Camera und MediaParticeln liegen");
                }
            }

            debugLog.AppendLine($"== LightPdfA={lightPdfA:R}");
            string debugString = debugLog.ToString();

            CheckDifference(lightPdfA, path.Points[0].LightPdfA, "LightPdfA");

            return this.maxDifference;
        }

        private void CheckDifference(double expected, double actual, string errorText)
        {
            double difference = Math.Abs(expected - actual);
            if (difference > this.maxDifference) this.maxDifference = difference;
            Assert.IsTrue(difference <= this.data.MaxAllowedError, $"{errorText}-Error=" + difference);
        }

        private static float HenyeyGreensteinPhaseFunction(FullPathPoint[] points, int i1, int i2, int i3, float anisotropyCoeffizient)
        {
            PathPoint p1 = points[i1].Point;
            PathPoint p2 = points[i2].Point;
            PathPoint p3 = points[i3].Point;

            Vector3D directionToBrdfPoint = Vector3D.Normalize(p2.Position - p1.Position);
            Vector3D outDirection = Vector3D.Normalize(p3.Position - p2.Position);

            float cosTheta = directionToBrdfPoint * outDirection;
            float squareMeanCosine = anisotropyCoeffizient * anisotropyCoeffizient;
            float d = 1 + squareMeanCosine - (anisotropyCoeffizient + anisotropyCoeffizient) * cosTheta;

            return d > 0 ? (float)((1.0f / (4 * Math.PI) * (1 - squareMeanCosine) / (d * Math.Sqrt(d)))) : 0;
        }

        private float GetPdfLForEyeDirection(FullPathPoint p1, FullPathPoint p2)
        {
            bool useDistanceSampling =
                this.data.EyePathSamplingType == PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling ||
                this.data.EyePathSamplingType == PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling ||
                this.data.EyePathSamplingType == PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling;

            float pdfL = 1;
            if (this.data.SceneHasMedia)
            {
                var line = MediaLineData.CreateMediaLineForEyeDirection(p1, p2);
                CheckMediaLine(line);
                pdfL = GetDistanzeSamplingPdfL(line);
            }
            if (useDistanceSampling == false) pdfL = 1;

            return pdfL;
        }

        private float GetPdfLForLightDirection(FullPathPoint p1, FullPathPoint p2)
        {
            bool useDistanceSampling =
                this.data.LightPathSamplingType == PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling ||
                this.data.LightPathSamplingType == PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling ||
                this.data.LightPathSamplingType == PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling;

            float pdfL = 1;
            if (this.data.SceneHasMedia)
            {
                var line = MediaLineData.CreateMediaLineForLightDirection(p1, p2);
                CheckMediaLine(line);
                pdfL = GetDistanzeSamplingPdfL(line);
            }
            if (useDistanceSampling == false) pdfL = 1;

            return pdfL;
        }

        private float GetDistanzeSamplingPdfL(MediaLineData line)
        {
            if (this.data.SceneHasMedia == false) return 1;

            //float shortRayLength = (line.P2.Position - line.P1.Position).Betrag();
            float pdfL = 1;
            for (int i = 0; i < line.MediaLine.ShortRaySegmentCount; i++)
            {
                VolumeSegment s = line.MediaLine.Segments[i];
                if (s.Media.HasScatteringSomeWhereInMedium())
                {
                    float attenuationCoef = this.data.ScatteringFromMedia + this.data.AbsorbationFromMedia;
                    float distance = Math.Min(s.RayMax - s.RayMin, line.MediaLine.ShortRayLength - s.RayMin);
                    float segmentPdfL = (float)Math.Exp(-attenuationCoef * distance); //Pdf wenn Medium durchschriten wird                   
                    if (line.GoBackWards == false && i == line.MediaLine.ShortRaySegmentCount - 1 && line.MediaLine.EndPointLocation == MediaPointLocationType.MediaParticle) //Strahl bleibt in Medium stecken
                    {
                        segmentPdfL *= (float)(this.data.ScatteringFromMedia + this.data.AbsorbationFromMedia);
                    }
                    if (line.GoBackWards == true && s == line.MediaLine.Segments.First() && line.MediaLine.StartPoint.Location == MediaPointLocationType.MediaParticle) //Strahl bleibt in Medium stecken
                    {
                        segmentPdfL *= (float)(this.data.ScatteringFromMedia + this.data.AbsorbationFromMedia);
                    }
                    pdfL *= segmentPdfL;
                }
            }

            return pdfL;
        }

        private void CheckMediaLine(MediaLineData line)
        {
            if (line.GoBackWards == false)
            {
                Assert.AreEqual(line.P1.Point.LocationType, line.MediaLine.StartPoint.Location);
                Assert.AreEqual(line.P2.Point.LocationType, line.MediaLine.EndPoint.Location);

                float maxDistance1 = line.P1.IsMergingPoint ? data.KernelDistance : MagicNumbers.DistanceForPoint2PointVisibleCheck;
                Assert.IsTrue((line.P1.Position - line.MediaLine.StartPoint.Position).Length() < maxDistance1);

                float maxDistance2 = line.P2.IsMergingPoint ? data.KernelDistance : MagicNumbers.DistanceForPoint2PointVisibleCheck;
                Assert.IsTrue((line.P2.Position - line.MediaLine.EndPoint.Position).Length() < maxDistance2);
            }
            else
            {
                Assert.AreEqual(line.P1.Point.LocationType, line.MediaLine.EndPoint.Location);
                Assert.AreEqual(line.P2.Point.LocationType, line.MediaLine.StartPoint.Location);

                float maxDistance1 = line.P1.IsMergingPoint ? data.KernelDistance : MagicNumbers.DistanceForPoint2PointVisibleCheck;
                Assert.IsTrue((line.P1.Position - line.MediaLine.EndPoint.Position).Length() < maxDistance1);

                float maxDistance2 = line.P2.IsMergingPoint ? data.KernelDistance : MagicNumbers.DistanceForPoint2PointVisibleCheck;
                Assert.IsTrue((line.P2.Position - line.MediaLine.StartPoint.Position).Length() < maxDistance2);
            }
        }

        //Verbindungslinie von P1 zu P2
        class MediaLineData
        {
            public MediaLine MediaLine;
            public bool GoBackWards = false;
            public FullPathPoint P1;
            public FullPathPoint P2;

            public static MediaLineData CreateMediaLineForEyeDirection(FullPathPoint p1, FullPathPoint p2)
            {
                return new MediaLineData()
                {
                    P1 = p1,
                    P2 = p2,
                    MediaLine = p1.EyeLineToNext ?? p2.LightLineToNext,
                    GoBackWards = p2.LightLineToNext != null
                };
            }

            public static MediaLineData CreateMediaLineForLightDirection(FullPathPoint p1, FullPathPoint p2)
            {
                return new MediaLineData()
                {
                    P1 = p1,
                    P2 = p2,
                    MediaLine = p1.LightLineToNext ?? p2.EyeLineToNext,
                    GoBackWards = p2.EyeLineToNext != null
                };
            }
        }

        private static float PdfWtoA(float brdfPdfW, Vector3D brdfPosition, IntersectionPoint lightPoint, StringBuilder debugLog) //Umrechnung einer Pdf W.r.t Solid Angle in einer Pdf W.r.t Survace Area dP / dA
        {
            Vector3D lightToBrdf = brdfPosition - lightPoint.Position;
            float distSqr = lightToBrdf.SquareLength(); //Das hier ist mein Notfallplan, falls die Normierung des lightToBrdf-Vektors eine Nulldivision verursacht
            float distLength = (float)Math.Sqrt(distSqr);
            if (distLength == 0)
            {
                debugLog.AppendLine($"/ Distance={MagicNumbers.MinAllowedPathPointSqrDistance:R}");

                return brdfPdfW / MagicNumbers.MinAllowedPathPointSqrDistance;
            }

            float pdfAOnNewPoint = brdfPdfW * Math.Abs((lightToBrdf / distLength) * lightPoint.OrientedFlatNormal) / Math.Max(distSqr, MagicNumbers.MinAllowedPathPointSqrDistance); //http://graphics.stanford.edu/papers/veach_thesis/thesis.pdf Seite 254

            //debugLog.AppendLine($"* PdfW={brdfPdfW}");
            debugLog.AppendLine($"* Cos={Math.Abs((lightToBrdf / distLength) * lightPoint.OrientedFlatNormal):R}");
            debugLog.AppendLine($"/ Distance={distSqr:R} -> pdfAOnNewPoint={pdfAOnNewPoint:R}");

            return pdfAOnNewPoint;
        }

        //Vom brdfPosition aus wird mit der PdfW 'brdfPdfW' Richtung mediaPoint gesampelt. Dieser Funktion gibt die PdfV für den mediaPoint zurück
        private static float PdfWtoASampleToMediaPoint(float brdfPdfW, Vector3D brdfPosition, Vector3D mediaPoint, StringBuilder debugLog) //Umrechnung einer Pdf W.r.t Solid Angle in einer Pdf W.r.t Survace Area dP / dV
        {
            Vector3D lightToBrdf = brdfPosition - mediaPoint;
            float distSqr = Math.Max(lightToBrdf.SquareLength(), MagicNumbers.MinAllowedPathPointSqrDistance);

            float pdfAOnNewPoint = brdfPdfW / distSqr;

            //debugLog.AppendLine($"* PdfW={brdfPdfW}");
            debugLog.AppendLine($"/ Distance={distSqr:R} -> pdfAOnNewPoint={pdfAOnNewPoint:R}");

            return pdfAOnNewPoint;
        }

        public static float PdfWToPdfAOrV(float pdfW, Vector3D brdfSamplePoint, PathPoint destinationPoint, StringBuilder debugLog)
        {
            if (destinationPoint.LocationType == MediaPointLocationType.Surface || destinationPoint.LocationType == MediaPointLocationType.Camera || destinationPoint.LocationType == MediaPointLocationType.MediaBorder)
            {
                return PdfWtoA(pdfW, brdfSamplePoint, destinationPoint.SurfacePoint, debugLog);
            }
            else
            {
                return PdfWtoASampleToMediaPoint(pdfW, brdfSamplePoint, destinationPoint.Position, debugLog);
            }
        }
    }
}
