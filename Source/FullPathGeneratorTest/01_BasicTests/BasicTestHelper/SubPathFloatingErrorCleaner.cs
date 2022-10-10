using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using ParticipatingMedia.DistanceSampling;
using RayTracerGlobal;
using RaytracingBrdf;
using RaytracingBrdf.BrdfFunctions;
using RaytracingBrdf.SampleAndRequest;
using SubpathGenerator;
using System;
using System.Text;

namespace FullPathGeneratorTest._01_BasicTests.BasicTestHelper
{
    class SubPathFloatingErrorCleaner
    {
        private readonly BoxTestScene testSzene;
        public SubPathFloatingErrorCleaner(BoxTestScene testSzene)
        {
            this.testSzene = testSzene;
        }
        //die PdfW vom SubPath stimmt nicht perfekt, da der Punkt, der in Richtung W anvisiert wird, dann nicht in Richtung W liegt nachdem er per Schnittpunktabfrage ermittelt wurde
        public void RemoveFloatingPointErrors(SubPath path, PathSamplingType samplingType)
        {
            if (path == null) return;

            //Der Unterschied zwischen dem pathWeightLog hier, was "pathWeight * pdfA" berechnet und dem Debug-Log beim PathTroughputCalculator
            //ist, dass hier in der Rechunung noch *PdfW /PdfW und *PdfL /PdfL auftaucht. Außerdem ist die Reihenfolge der Multiplikationen/Divisionen
            //nicht genau gleich. Durch diese beiden Unterschiede kommt es zu Double-Ungenauigkeiten
            StringBuilder pathWeightLog = new StringBuilder(); //pathWeight * pdfA
            double pathWeight = 1; //Pathweight über die SubPath-Formel aber mit korrigierten Richtungsvektoren/PdfW

            StringBuilder geometrySumLog = new StringBuilder();
            float geometrySum = 1; //geometrySum/PdfA -> PathWeight über die GeometrySum-Formel mit dem Ziel, dass der GeometrySum-Error so kleiner wird

            //Wenn man beim SubPathPoint und FullPath das PathWeight/PathContribution als Double speichert und die geometrySum-Formel nimmt, 
            //um das Pfadgewicht zu speichern, dann ist der GeometrySum-Error 0. D.h. der jetzige Fehler von 2E-28 kommt durch die Konvertierung
            //das Pfadgewichts von double nach float.

            for (int i = 0; i < path.Points.Length; i++)
            {
                if (i < path.Points.Length -1)
                {
                    pathWeightLog.AppendLine($"P[{i}]={path.Points[i].Position}\tP[{i + 1}]={path.Points[i + 1].Position}");
                }
 
                //Schritt 1: PdfW-Korrektur

                //Kamera-PdfW-Korrektur
                if (path.Points[i].LocationType == MediaPointLocationType.Camera && path.Points.Length > 1)
                {
                    Vector3D newDirection = Vector3D.Normalize(path.Points[1].Position - path.Points[0].Position);
                    float cameraPdfW = this.testSzene.Camera.GetPixelPdfW(this.testSzene.PixX, this.testSzene.PixY, newDirection);
                    DifferenceCheck(cameraPdfW, path.Points[i].BrdfSampleEventOnThisPoint.PdfW);
                    path.Points[i].BrdfSampleEventOnThisPoint.PdfW = cameraPdfW;
                    path.Points[i].BrdfSampleEventOnThisPoint.Ray.Direction = newDirection;

                    pathWeight = 1;
                    if (this.testSzene.Camera.UseCosAtCamera)
                    {
                        float cosAtCamera = Math.Abs(this.testSzene.Camera.Forward * newDirection);
                        pathWeight *= cosAtCamera;
                        pathWeightLog.AppendLine($"* CosAtCamera={cosAtCamera:R}");
                    }

                    geometrySum *= cameraPdfW; //PixelFilter
                    geometrySumLog.AppendLine($"* PixelFilter={cameraPdfW:R}");
                }

                if (i < path.Points.Length - 1)
                {
                    geometrySumLog.AppendLine($"P[{i}]={path.Points[i].Position}\tP[{i + 1}]={path.Points[i + 1].Position}");
                    geometrySum *= GeometryTermWithMedia(path.Points, i, i + 1, geometrySumLog);
                }

                //Light-PdfW-Korrektur
                if (i == 0 && path.Points[i].IsLocatedOnLightSource && path.Points.Length > 1)
                {
                    Vector3D newDirection = Vector3D.Normalize(path.Points[1].Position - path.Points[0].Position);
                    float lightPdfW = BrdfDiffuseCosinusWeighted.PDFw(path.Points[i].Normal, newDirection);
                    DifferenceCheck(lightPdfW, path.Points[i].BrdfSampleEventOnThisPoint.PdfW);
                    path.Points[i].BrdfSampleEventOnThisPoint.PdfW = lightPdfW;
                    float lightPdfAF = 1.0f / this.testSzene.LightSourceArea;
                    path.Points[i].PdfA = (double)lightPdfAF;
                    path.Points[i].BrdfSampleEventOnThisPoint.Ray.Direction = newDirection;

                    float cosAtLight = Math.Abs(path.Points[i].Normal * newDirection);
                    pathWeight = path.Points[i].PathWeight.X * cosAtLight / lightPdfW;
                    pathWeightLog.AppendLine($"* CosAtLight={cosAtLight:R}");
                    pathWeightLog.AppendLine($"/ lightPdfW={lightPdfW:R}");

                    geometrySum *= this.testSzene.EmissionPerArea; //EmissionPerArea
                    geometrySumLog.AppendLine($"* EmissionPerArea={this.testSzene.EmissionPerArea:R}");
                }

                //Surface-PdfW-Korrektur
                if (path.Points[i].IsDiffusePoint && path.Points[i].LocationType == MediaPointLocationType.Surface)
                {
                    if (i > 0)
                    {
                        Vector3D inDirection = Vector3D.Normalize(path.Points[i].Position - path.Points[i - 1].Position);
                        path.Points[i].BrdfPoint.DirectionToThisPoint = inDirection;
                    }

                    if (path.Points[i].IsLocatedOnLightSource == false && i + 1 < path.Points.Length)
                    {
                        Vector3D outDirection = Vector3D.Normalize(path.Points[i + 1].Position - path.Points[i].Position);
                        var brdfResult = path.Points[i].BrdfPoint.Evaluate(outDirection);

                        DifferenceCheck(brdfResult.PdfW, path.Points[i].BrdfSampleEventOnThisPoint.PdfW);
                        DifferenceCheck(brdfResult.PdfWReverse, path.Points[i].BrdfSampleEventOnThisPoint.PdfWReverse);
                        path.Points[i].BrdfSampleEventOnThisPoint.PdfW = brdfResult.PdfW;
                        path.Points[i].BrdfSampleEventOnThisPoint.PdfWReverse = brdfResult.PdfWReverse;
                        path.Points[i].BrdfSampleEventOnThisPoint.Ray.Direction = outDirection;

                        pathWeight *= brdfResult.Brdf.X / brdfResult.PdfW * brdfResult.CosThetaOut;
                        pathWeightLog.AppendLine($"* SurfaceBrdf={brdfResult.Brdf.X:R}");
                        pathWeightLog.AppendLine($"/ SurfacePdfW={brdfResult.PdfW:R}");
                        pathWeightLog.AppendLine($"* CosThetaOut={brdfResult.CosThetaOut:R}");

                        geometrySum *= brdfResult.Brdf.X;
                        geometrySumLog.AppendLine($"* SurfaceBrdf={brdfResult.Brdf.X:R}");
                    }
                }

                //Partikel-PdfW-Korrektur
                if (path.Points[i].IsDiffusePoint && path.Points[i].LocationType == MediaPointLocationType.MediaParticle && i + 1 < path.Points.Length)
                {
                    Vector3D inDirection = Vector3D.Normalize(path.Points[i].Position - path.Points[i - 1].Position);
                    Vector3D outDirection = Vector3D.Normalize(path.Points[i + 1].Position - path.Points[i].Position);
                    var brdfResult = PhaseFunction.EvaluateBsdf(inDirection, path.Points[i].MediaPoint, outDirection);

                    DifferenceCheck(brdfResult.PdfW, path.Points[i].BrdfSampleEventOnThisPoint.PdfW);
                    DifferenceCheck(brdfResult.PdfWReverse, path.Points[i].BrdfSampleEventOnThisPoint.PdfWReverse);
                    path.Points[i].BrdfSampleEventOnThisPoint.PdfW = brdfResult.PdfW;
                    path.Points[i].BrdfSampleEventOnThisPoint.PdfWReverse = brdfResult.PdfWReverse;
                    path.Points[i].BrdfSampleEventOnThisPoint.Ray.Direction = outDirection;

                    pathWeight *= brdfResult.Brdf.X / brdfResult.PdfW;
                    pathWeightLog.AppendLine($"* PartikelBrdf={brdfResult.Brdf.X:R}");
                    pathWeightLog.AppendLine($"/ PartikelPdfW={brdfResult.PdfW:R}");

                    geometrySum *= brdfResult.Brdf.X;
                    geometrySumLog.AppendLine($"* PartikelBrdf={brdfResult.Brdf.X:R}");
                }

                //Schritt 2: PdfL-Korrektur
                var pdfLResult = path.Points[i].LineToNextPoint != null ? path.Points[i].LineToNextPoint.GetPdfLIfDistanceSamplingWouldBeUsed() : new DistancePdf() { PdfL = 1, ReversePdfL = 1 };
                if (samplingType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling) pdfLResult = new DistancePdf() { PdfL = 1, ReversePdfL = 1 };

                //Schritt 3: PdfA/PdfAReverse/PathWeight-Korrektur
                if (i + 1 < path.Points.Length && path.Points[i + 1].LocationType != MediaPointLocationType.NullMediaBorder)
                {
                    var line = path.Points[i].LineToNextPoint;
                    if (line != null)
                    {
                        float attenuation = line.AttenuationWithoutPdf().X;
                        pathWeight *= attenuation / pdfLResult.PdfL;

                        pathWeightLog.AppendLine($"* Attenuation={attenuation:R}");
                        pathWeightLog.AppendLine($"/ PdfL={pdfLResult.PdfL:R}");

                        geometrySum *= attenuation;
                        geometrySumLog.AppendLine($"* Attenuation={attenuation:R}");
                    }

                    pathWeightLog.AppendLine($"* PdfW={path.Points[i].BrdfSampleEventOnThisPoint.PdfW:R}");
                    float pdfAOnNext = PathPdfACalculator.PdfWToPdfAOrV(path.Points[i].BrdfSampleEventOnThisPoint.PdfW, path.Points[i].Position, path.Points[i + 1], pathWeightLog);
                    double nextPdfA = path.Points[i].PdfA * pdfAOnNext * pdfLResult.PdfL;

                    pathWeightLog.AppendLine($"* PdfL={pdfLResult.PdfL:R}");

                    DifferenceCheck(nextPdfA, path.Points[i + 1].PdfA);
                    DifferenceCheck(pdfLResult.ReversePdfL, path.Points[i].PdfLFromNextPointToThis);

                    path.Points[i + 1].PdfA = nextPdfA;
                    path.Points[i].PdfLFromNextPointToThis = pdfLResult.ReversePdfL;                    

                    //Assert.IsTrue(Math.Abs(path.Points[i + 1].PathWeight.X - pathWeight.X) < 0.0001);

                    //So ist der Geomtry-Error bei Pathtracing etwas höher
                    float geometrySumFromPathWeight = (float)(pathWeight * nextPdfA);
                    pathWeightLog.AppendLine($"== geometrySum[{0}-{i + 1}]={geometrySumFromPathWeight:R}");

                    //Wenn ich das Pfade-Gewicht über diese Formel festlege, entspricht PathWeight*PdfA eher der GeometrySum aus dem PathTroughputCalculator wodurch der Geometry-Error kleiner wird
                    double pathWeightFromGeometrySum = geometrySum / nextPdfA;
                    geometrySumLog.AppendLine($"== geometrySum[{0}-{i + 1}]={geometrySum:R}");

                    //PathWeight-Korrektur
                    path.Points[i + 1].PathWeight.X = (float)pathWeightFromGeometrySum;
                    path.Points[i + 1].PathWeight.Y = (float)pathWeightFromGeometrySum;
                    path.Points[i + 1].PathWeight.Z = (float)pathWeightFromGeometrySum;
                }
                if (i > 0 && i + 1 < path.Points.Length)
                {
                    double predecessorPdfA = PdfHelper.PdfWToPdfAOrV(path.Points[i].BrdfSampleEventOnThisPoint.PdfWReverse, path.Points[i], path.Points[i - 1]);

                    //DifferenceCheck(predecessorPdfA, path.Points[i - 1].PdfAReverse);

                    path.Points[i - 1].PdfAReverse = predecessorPdfA;
                }
            }

            string debugString1 = pathWeightLog.ToString();
            string debugString2 = geometrySumLog.ToString();
        }

        private static void DifferenceCheck(double d1, double d2)
        {
            double diff = Math.Abs(d1 - d2);
            if (d1 < 1)
            {
                Assert.IsTrue(diff < 0.1, "diff=" + diff);
            }
            else if (d1 < 100)
            {
                Assert.IsTrue(diff < 0.1, "diff=" + diff);
            }
            else
            {
                Assert.IsTrue(diff < 1, "diff=" + diff);
            }
        }

        private static float GeometryTermWithMedia(PathPoint[] points, int i1, int i2, StringBuilder debugLog)
        {
            PathPoint p1 = points[i1];
            PathPoint p2 = points[i2];

            Vector3D dir = Vector3D.Normalize(p2.Position - p1.Position);
            float r2 = Math.Max((p2.Position - p1.Position).SquareLength(), MagicNumbers.MinAllowedPathPointSqrDistance);
            debugLog.AppendLine($"/ Distance[{i1}-{i2}]={r2:R}");
            float sum = 1.0f / r2;
            if (p1.LocationType == MediaPointLocationType.Surface || p1.LocationType == MediaPointLocationType.Camera || p1.LocationType == MediaPointLocationType.MediaBorder)
            {
                sum *= Math.Abs(p1.Normal * dir); //Der Kamerapuntk ist auch nciht auf ein Surface, hat aber kein RayHeigh im Gegensatz zu ein Mediapunkt
                debugLog.AppendLine($"* CosP1[{i1}]={Math.Abs(p1.Normal * dir):R}");
            }
            if (p2.LocationType == MediaPointLocationType.Surface || p2.LocationType == MediaPointLocationType.Camera || p2.LocationType == MediaPointLocationType.MediaBorder)
            {
                sum *= Math.Abs(p2.Normal * (-dir));
                debugLog.AppendLine($"* CosP2[{i2}]={Math.Abs(p2.Normal * (-dir)):R}");
            }
            return (float)sum;
        }
    }
}
