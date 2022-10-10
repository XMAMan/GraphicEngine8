using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using ParticipatingMedia.PhaseFunctions;
using PdfHistogram;
using RayTracerGlobal;
using RaytracingBrdf;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SubpathGeneratorTest
{
    //Überprüfung des Pfadgewichts und der Pfad-PdfA anhand der Geometry-Summe
    [TestClass]
    public class SubpathSamplerPathWeightTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void SamplePathFromCamera_WithPdfAAndReversePdfA_PathWeightMatchWithGeometryTerm()
        {
            BoundingBox mediaBox = null;
            SamplePathFromCamera_CalledMultipleTimes_PathWeightMatchWithGeometryTerm(SubpathGenerator.PathSamplingType.NoMedia, mediaBox, false);
        }

        [TestMethod]
        public void SamplePathFromCamera_ParticipatingMediaWithoutDistanceSampling_NoMediaAvailable_PathWeightMatchWithGeometryTerm() 
        {
            BoundingBox mediaBox = null; //Der Media-Sampler muss sich bei Abwesenheit von Media-Objekten wie der Standardsampler verhalten
            SamplePathFromCamera_CalledMultipleTimes_PathWeightMatchWithGeometryTerm(SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling, mediaBox, false);
        }

        [TestMethod]
        public void SamplePathFromCamera_ParticipatingMediaWithDistanceSampling_NoMediaAvailable_PathWeightMatchWithGeometryTerm() 
        {
            BoundingBox mediaBox = null;//Der Media-Sampler muss sich bei Abwesenheit von Media-Objekten wie der Standardsampler verhalten
            SamplePathFromCamera_CalledMultipleTimes_PathWeightMatchWithGeometryTerm(SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, mediaBox, true);
        }

        [TestMethod]
        public void SamplePathFromCamera_ParticipatingMediaWithoutDistanceSampling_WithMediaBox_PathWeightMatchWithGeometryTerm()
        {
            BoundingBox mediaBox = new BoundingBox(new Vector3D(-1, 0, -1), new Vector3D(+1,0.5f,+1)); //MediaBox über der ersten Platte
            SamplePathFromCamera_CalledMultipleTimes_PathWeightMatchWithGeometryTerm(SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling, mediaBox, false);
        }

        [TestMethod]
        public void SamplePathFromCamera_ParticipatingMediaWithDistanceSampling_WithMediaBox_PathWeightMatchWithGeometryTerm()
        {
            BoundingBox mediaBox = new BoundingBox(new Vector3D(-1, 0, -1), new Vector3D(+1, 0.5f, +1)); //MediaBox über der ersten Platte
            SamplePathFromCamera_CalledMultipleTimes_PathWeightMatchWithGeometryTerm(SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, mediaBox, true);
        }

        private void SamplePathFromCamera_CalledMultipleTimes_PathWeightMatchWithGeometryTerm(PathSamplingType pathSamplingType, BoundingBox mediaBox, bool useDistanceSampling)
        {
            int sampleCount = 10000;            

            PdfATestSzene testSzene = new PdfATestSzene(pathSamplingType, mediaBox);

            for (int i = 0; i < sampleCount; i++)
            {
                PathPoint[] points = testSzene.SubpathSampler.SamplePathFromCamera(testSzene.PixX, testSzene.PixY, testSzene.rand).Points;

                CheckEyePdfA(points, testSzene, useDistanceSampling);

                float pixelFilter = testSzene.Camera.GetPixelPdfW(testSzene.PixX, testSzene.PixY, points[1].DirectionToThisPoint);
                if (mediaBox == null)
                    CheckPathWeightWithoutMedia(points, 1, pixelFilter);
                else
                    CheckPathWeightWithMedia(points, 1, pixelFilter, testSzene);
            }
        }



        [TestMethod]
        public void SamplePathFromLighsource_WithPdfAAndReversePdfA_PathWeightMatchWithGeometryTerm()
        {
            BoundingBox mediaBox = null;
            SamplePathFromLighsource_CalledMultipleTimes_PathWeightMatchWithGeometryTerm(SubpathGenerator.PathSamplingType.NoMedia, mediaBox, false);
        }

        [TestMethod]
        public void SamplePathFromLighsource_ParticipatingMediaWithoutDistanceSampling_NoMediaAvailable_PathWeightMatchWithGeometryTerm()
        {
            BoundingBox mediaBox = null;//Der Media-Sampler muss sich bei Abwesenheit von Media-Objekten wie der Standardsampler verhalten
            SamplePathFromLighsource_CalledMultipleTimes_PathWeightMatchWithGeometryTerm(SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling, mediaBox, false);
        }

        [TestMethod]
        public void SamplePathFromLighsource_ParticipatingMediaWithDistanceSampling_NoMediaAvailable_PathWeightMatchWithGeometryTerm()
        {
            BoundingBox mediaBox = null;//Der Media-Sampler muss sich bei Abwesenheit von Media-Objekten wie der Standardsampler verhalten
            SamplePathFromLighsource_CalledMultipleTimes_PathWeightMatchWithGeometryTerm(SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, mediaBox, true);
        }

        [TestMethod]
        public void SamplePathFromLighsource_ParticipatingMediaWithoutDistanceSampling_WithMediaBox_PathWeightMatchWithGeometryTerm()
        {
            BoundingBox mediaBox = new BoundingBox(new Vector3D(-1+8, 0 + 0.2f, -1), new Vector3D(+1+8, 0.5f + 0.2f, +1)); //MediaBox über der letzten Platte
            SamplePathFromLighsource_CalledMultipleTimes_PathWeightMatchWithGeometryTerm(SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling, mediaBox, false);
        }

        [TestMethod]
        public void SamplePathFromLighsource_ParticipatingMediaWithDistanceSampling_WithMediaBox_PathWeightMatchWithGeometryTerm()
        {
            BoundingBox mediaBox = new BoundingBox(new Vector3D(-1+8, 0+0.2f, -1), new Vector3D(+1+8, 0.5f + 0.2f, +1)); //MediaBox über der letzten Platte
            SamplePathFromLighsource_CalledMultipleTimes_PathWeightMatchWithGeometryTerm(SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, mediaBox, true);
        }

        [TestMethod]
        public void SamplePathFromLighsource_ParticipatingMediaWithDistanceSampling_WithMediaBox_FireflyTest()
        {
            BoundingBox mediaBox = new BoundingBox(new Vector3D(-1 + 8, 0 + 0.2f, -1), new Vector3D(+1 + 8, 0.5f + 0.2f, +1)); //MediaBox über der letzten Platte
            int sampleCount = 10000;// * 100;

            List<float> pathPointWeights = new List<float>();
            List<double> pathPointPdfAs = new List<double>();

            PdfATestSzene testSzene = new PdfATestSzene(SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, mediaBox);
            for (int i = 0; i < sampleCount; i++)
            {
                if (i== 548482)
                {
                    int h = 1;
                    int b = h + 1;
                }
                PathPoint[] points = testSzene.SubpathSampler.SamplePathFromLighsource(testSzene.rand).Points;

                for (int j = 0; j < points.Length; j++)
                {
                    pathPointWeights.Add(points[j].PathWeight.X);
                    pathPointPdfAs.Add(points[j].PdfA);
                    if (points[j].PdfA >= 123549)
                    {
                        throw new Exception("FireflyAlarm");
                    }
                }
            }

            string firefly =
                "PathPointWeights (Min-to-Max-Range)" + System.Environment.NewLine +
                string.Join(System.Environment.NewLine, pathPointWeights.OrderBy(x => x).ToList().GetRange(0, 10)) +
                System.Environment.NewLine + ".................." + System.Environment.NewLine +
                string.Join(System.Environment.NewLine, pathPointWeights.OrderByDescending(x => x).ToList().GetRange(0, 10)) +
                System.Environment.NewLine + "PathPointPdfAs (Min-to-Max-Range)" + System.Environment.NewLine +
                string.Join(System.Environment.NewLine, pathPointPdfAs.OrderBy(x => x).ToList().GetRange(0, 10)) +
                System.Environment.NewLine + ".................." + System.Environment.NewLine +
                string.Join(System.Environment.NewLine, pathPointPdfAs.OrderByDescending(x => x).ToList().GetRange(0, 10));
            File.WriteAllText(WorkingDirectory + "SubpathFireflytest.txt", firefly);
        }

        private void SamplePathFromLighsource_CalledMultipleTimes_PathWeightMatchWithGeometryTerm(PathSamplingType pathSamplingType, BoundingBox mediaBox, bool useDistanceSampling)
        {
            int sampleCount = 10000;

            List<float> pathPointWeights = new List<float>();
            List<double> pathPointPdfAs = new List<double>();

            PdfATestSzene testSzene = new PdfATestSzene(pathSamplingType, mediaBox);
            float lightArea = testSzene.Quads.Last().SurfaceArea;
            float emissionPerArea = 1 / lightArea;

            for (int i = 0; i < sampleCount; i++)
            {
                PathPoint[] points = testSzene.SubpathSampler.SamplePathFromLighsource(testSzene.rand).Points;

                if (points.Length > 1) Assert.IsFalse(points.Last().IsLocatedOnLightSource, "Letzter Lightpath-Point ist auf Lichtquelle"); //Der letzte Punkt darf bei Light-Subpaths nie auf der Lichtquelle enden!
          
                CheckLightPdfA(points, testSzene, useDistanceSampling);

                if (mediaBox == null)
                    CheckPathWeightWithoutMedia(points, emissionPerArea, 1);
                else
                    CheckPathWeightWithMedia(points, emissionPerArea, 1, testSzene);
            }
        }

        private void CheckPathWeightWithoutMedia(PathPoint[] points, float initialPathWeight, float pixelFilter)
        {
            float maxError = 0.001f;

            float expectedPathWeight = initialPathWeight;
            for (int i=0;i<points.Length - 1;i++)
            {
                float geometryTerm = GeometryTermWithoutMedia(points[i], points[i + 1]);
                expectedPathWeight *= geometryTerm;
                double actualPathWeight = points[i + 1].PathWeight.X * points[i + 1].PdfA;

                if (i == 0) expectedPathWeight *= pixelFilter; //Wenn der Subpfad von der Kamera startet, dann wichte den Pfad mit dem Pixelfilter (Bei Lightsubpahts wichte mit 1)

                Assert.IsTrue(Math.Abs(expectedPathWeight - actualPathWeight) < maxError);

                if ( i + 2 < points.Length)
                {
                    float diffuseBrdf = points[i + 1].SurfacePoint.Color.X / (float)Math.PI * points[i + 1].BrdfPoint.Albedo;
                    expectedPathWeight *= diffuseBrdf; //Diffuse Brdf
                }                
            }
        }
        private float GeometryTermWithoutMedia(PathPoint p1, PathPoint p2)
        {
            Vector3D dir = Vector3D.Normalize(p2.Position - p1.Position);
            return Math.Abs(p1.Normal * dir) / (p2.Position - p1.Position).SquareLength() * Math.Abs(p2.Normal * (-dir));
        }


        //Ich verwende die Formeln von hier: Unbiased Global Illumination with Participating Media - Raab et al (2008).pdf
        //Annahme: Oberflächen haben diffuses Material; Media ist Homogen
        private void CheckPathWeightWithMedia(PathPoint[] points, float initialPathWeight, float pixelFilter, PdfATestSzene testSzene)
        {
            float expectedPathWeight = initialPathWeight;
            for (int i = 0; i < points.Length - 1; i++)
            {
                float geometryTerm = GeometryTermWithMedia(points[i], points[i + 1]);
                float attenuation = Attenuation(points[i], points[i + 1], testSzene.ScatteringFromMedia, testSzene.AbsorbationFromMedia);
                expectedPathWeight *= geometryTerm * attenuation;
                double actualPathWeight = points[i + 1].PathWeight.X * points[i + 1].PdfA;

                if (i == 0) expectedPathWeight *= pixelFilter; //Wenn der Subpfad von der Kamera startet, dann wichte den Pfad mit dem Pixelfilter (Bei Lightsubpahts wichte mit 1)

                if (points[i + 1].LocationType != MediaPointLocationType.NullMediaBorder)
                {
                    ComparePdfA(expectedPathWeight, actualPathWeight);
                    //Assert.IsTrue(Math.Abs(expectedPathWeight - actualPathWeight) < (expectedPathWeight > 1000 ? 1 : maxError));
                }


                if (i + 2 < points.Length)
                {
                    if (points[i + 1].LocationType == MediaPointLocationType.Surface)
                    {
                        float diffuseBrdf = points[i + 1].SurfacePoint.Color.X / (float)Math.PI * points[i + 1].BrdfPoint.Albedo;
                        expectedPathWeight *= diffuseBrdf; //Diffuse Brdf
                    }
                    else if (points[i + 1].LocationType == MediaPointLocationType.MediaBorder)
                    {
                        //Dividiere die Speculare Brdf durch den Cos-Term, da ich beim SubPath-Erstellen die Speculare Brdf NICHT mit Cos-Term gewichtet habe
                        expectedPathWeight /= Math.Abs(points[i + 1].Normal * Vector3D.Normalize(points[i + 2].Position - points[i + 1].Position));
                    }
                    else
                    {
                        float phaseFunction = 1 / (float)(4 * Math.PI); //Bei ein Homogeonen Medium mit Anisotrophicfactor von 0 wird die Isotrophic-Phasenfunktion genutzt.
                        expectedPathWeight *= phaseFunction * testSzene.ScatteringFromMedia; //Phasefunction * Media-Scattering-Term
                    }
                    
                }
            }
        }

        private float GeometryTermWithMedia(PathPoint p1, PathPoint p2)
        {
            Vector3D dir = Vector3D.Normalize(p2.Position - p1.Position);
            float r2 = Math.Max((p2.Position - p1.Position).SquareLength(), MagicNumbers.MinAllowedPathPointSqrDistance);
            double sum = 1.0 / r2;
            //if (r2 <= MagicNumbers.MinAllowedPathPointSqrDistance) return (float)sum;
            if (p1.LocationType == MediaPointLocationType.Surface || p1.LocationType == MediaPointLocationType.Camera || p1.LocationType == MediaPointLocationType.MediaBorder) sum *= Math.Abs(p1.Normal * dir); //Der Kamerapuntk ist auch nciht auf ein Surface, hat aber kein RayHeigh im Gegensatz zu ein Mediapunkt
            if (p2.LocationType == MediaPointLocationType.Surface || p2.LocationType == MediaPointLocationType.Camera || p2.LocationType == MediaPointLocationType.MediaBorder) sum *= Math.Abs(p2.Normal * (-dir));
            return (float)sum;
        }

        //Ich gehe davon aus, dass von jeden Segment RayMin,RayMax und HasScattering korrekt sind
        private float Attenuation(PathPoint p1, PathPoint p2, float scatteringFromMedia, float absorbationFromMedia)
        {
            Assert.IsTrue((p1.LineToNextPoint.EndPoint.Position - p2.Position).SquareLength() < 0.001f);
            float attenuation = 1;
            foreach (var s in p1.LineToNextPoint.Segments)
            {
                if (s.Media.HasScatteringSomeWhereInMedium()) attenuation *= (float)Math.Exp(-(scatteringFromMedia + absorbationFromMedia) * (s.RayMax - s.RayMin));
            }

            return attenuation;
        }

        //....................... PDF-A....................

        private static void ComparePdfA(double actual, double expexted)
        {
            float maxError = 0.01f;
            if (expexted > 100) maxError = 1;
            if (expexted > 5000) maxError = 5;
            if (expexted > 10000) maxError = 60;
            Assert.IsTrue(Math.Abs(actual - expexted) < maxError, "Error = " + Math.Abs(actual - expexted) + " Maxerror " + maxError +" (" + actual + ")");
        }

        private static void CheckEyePdfA(PathPoint[] points, PdfATestSzene testSzene, bool useDistanceSampling)
        {
            float cameraPdfW = testSzene.Camera.GetPixelPdfW(testSzene.PixX, testSzene.PixY, Vector3D.Normalize(points[1].Position - points[0].Position));

            double eyePdfA = 1;
            for (int i = 0; i < points.Length - 1; i++)
            {
                ComparePdfA(points[i].PdfA, eyePdfA);

                if (points[i].LocationType == MediaPointLocationType.Camera)
                {
                    var surfacePoint = points[i].SurfacePoint;

                    float continuationPdf = 1;

                    Vector3D directionToNextPoint = Vector3D.Normalize(points[i + 1].Position - points[i].Position);
                    float pdfAOnNextPoint = PdfHelper.PdfWToPdfAOrV(cameraPdfW, points[i], points[i + 1]);

                    float pdfL = useDistanceSampling ? DistaneSamplingPdfHomogeonMedia(points[i], points[i + 1], testSzene.ScatteringFromMedia, testSzene.AbsorbationFromMedia, testSzene.SceneHasMedia) : 1;

                    eyePdfA *= continuationPdf * pdfAOnNextPoint * pdfL;
                }
                else if (points[i].LocationType == MediaPointLocationType.Surface)
                {
                    var surfacePoint = points[i].SurfacePoint;

                    float continuationPdf = Math.Min(1, Math.Max(MagicNumbers.MinSurfaceContinuationPdf, points[i].BrdfPoint.ContinuationPdf));

                    Vector3D directionToNextPoint = Vector3D.Normalize(points[i + 1].Position - points[i].Position);
                    float diffusePdfW = Math.Max(MagicNumbers.MinAllowedPdfW, Math.Max(0, surfacePoint.OrientedFlatNormal * directionToNextPoint) / (float)Math.PI);
                    diffusePdfW *= continuationPdf;
                    float pdfAOnNextPoint = PdfHelper.PdfWToPdfAOrV(diffusePdfW, points[i], points[i + 1]);

                    float pdfL = useDistanceSampling ? DistaneSamplingPdfHomogeonMedia(points[i], points[i + 1], testSzene.ScatteringFromMedia, testSzene.AbsorbationFromMedia, testSzene.SceneHasMedia) : 1;

                    eyePdfA *= pdfAOnNextPoint * pdfL;
                }
                else if (points[i].LocationType == MediaPointLocationType.MediaParticle)
                {
                    float continuationPdf = Math.Max(MagicNumbers.MediaMinContinuationPdf, testSzene.ScatteringFromMedia / (testSzene.ScatteringFromMedia + testSzene.AbsorbationFromMedia));

                    Vector3D directionToNextPoint = Vector3D.Normalize(points[i + 1].Position - points[i].Position);
                    float isotrophicPdfW = 1.0f / (4 * (float)Math.PI);
                    isotrophicPdfW *= continuationPdf;
                    float pdfAOnNextPoint = PdfHelper.PdfWToPdfAOrV(isotrophicPdfW, points[i], points[i + 1]);

                    float pdfL = useDistanceSampling ? DistaneSamplingPdfHomogeonMedia(points[i], points[i + 1], testSzene.ScatteringFromMedia, testSzene.AbsorbationFromMedia, testSzene.SceneHasMedia) : 1;

                    eyePdfA *= pdfAOnNextPoint * pdfL;
                }
                else if (points[i].LocationType == MediaPointLocationType.MediaBorder)
                {
                    var surfacePoint = points[i].SurfacePoint;

                    float continuationPdf = 1;
   
                    Vector3D directionToNextPoint = Vector3D.Normalize(points[i + 1].Position - points[i].Position);
                    float diffusePdfW = 1;
                    diffusePdfW *= continuationPdf;
                    float pdfAOnNextPoint = PdfHelper.PdfWToPdfAOrV(diffusePdfW, points[i], points[i + 1]);

                    float pdfL = useDistanceSampling ? DistaneSamplingPdfHomogeonMedia(points[i], points[i + 1], testSzene.ScatteringFromMedia, testSzene.AbsorbationFromMedia, testSzene.SceneHasMedia) : 1;

                    eyePdfA *= pdfAOnNextPoint * pdfL;
                }
                else
                {
                    throw new Exception("FullPaths dürfen momentan nur auf Surface, Camera und MediaParticeln liegen");
                }
            }

            if (points.Last().LocationType != MediaPointLocationType.NullMediaBorder)
            {
                ComparePdfA(points.Last().PdfA, eyePdfA);
            }            
        }

        private static void CheckLightPdfA(PathPoint[] points, PdfATestSzene testSzene, bool useDistanceSampling)
        {
            float lightArea = testSzene.Quads.Last().SurfaceArea;
            double lightPdfA = 1.0f / lightArea;
            for (int i=0;i<points.Length - 1;i++)
            {
                ComparePdfA(points[i].PdfA, lightPdfA);

                if (points[i].LocationType == MediaPointLocationType.Surface)
                {
                    var surfacePoint = points[i].SurfacePoint;

                    float continuationPdf = surfacePoint.IsLocatedOnLightSource ? 1 : Math.Min(1, Math.Max(MagicNumbers.MinSurfaceContinuationPdf, points[i].BrdfPoint.ContinuationPdf));

                    Vector3D directionToNextPoint = Vector3D.Normalize(points[i + 1].Position - points[i].Position);
                    float diffusePdfW = Math.Max(MagicNumbers.MinAllowedPdfW, Math.Max(0, surfacePoint.OrientedFlatNormal * directionToNextPoint) / (float)Math.PI);
                    double pdfAOnNextPoint = PdfHelper.PdfWToPdfAOrV(diffusePdfW, points[i], points[i + 1]);

                    float pdfL = useDistanceSampling ? DistaneSamplingPdfHomogeonMedia(points[i], points[i + 1], testSzene.ScatteringFromMedia, testSzene.AbsorbationFromMedia, testSzene.SceneHasMedia) : 1;

                    lightPdfA *= continuationPdf * pdfAOnNextPoint * pdfL;
                }
                else if (points[i].LocationType == MediaPointLocationType.MediaParticle)
                {
                    float continuationPdf = Math.Max(MagicNumbers.MediaMinContinuationPdf, testSzene.ScatteringFromMedia / (testSzene.ScatteringFromMedia + testSzene.AbsorbationFromMedia));

                    Vector3D directionToNextPoint = Vector3D.Normalize(points[i + 1].Position - points[i].Position);
                    float isotrophicPdfW = 1.0f / (4 * (float)Math.PI);
                    double pdfAOnNextPoint = PdfHelper.PdfWToPdfAOrV(isotrophicPdfW, points[i], points[i + 1]);

                    float pdfL = useDistanceSampling ? DistaneSamplingPdfHomogeonMedia(points[i], points[i + 1], testSzene.ScatteringFromMedia, testSzene.AbsorbationFromMedia, testSzene.SceneHasMedia) : 1;

                    lightPdfA *= continuationPdf * pdfAOnNextPoint * pdfL;
                }
                else
                if (points[i].LocationType == MediaPointLocationType.MediaBorder)
                {
                    var surfacePoint = points[i].SurfacePoint;

                    float continuationPdf = 1;

                    Vector3D directionToNextPoint = Vector3D.Normalize(points[i + 1].Position - points[i].Position);
                    float diffusePdfW = 1;
                    double pdfAOnNextPoint = PdfHelper.PdfWToPdfAOrV(diffusePdfW, points[i], points[i + 1]);

                    float pdfL = useDistanceSampling ? DistaneSamplingPdfHomogeonMedia(points[i], points[i + 1], testSzene.ScatteringFromMedia, testSzene.AbsorbationFromMedia, testSzene.SceneHasMedia) : 1;

                    lightPdfA *= continuationPdf * pdfAOnNextPoint * pdfL;
                }else
                {
                    throw new Exception("FullPaths dürfen momentan nur auf Surface, Camera und MediaParticeln liegen");
                }
            }

            //Assert.IsTrue(Math.Abs(path.Points[0].LightPdfA - lightPdfA) < maxError);
        }

        private static float DistaneSamplingPdfHomogeonMedia(PathPoint p1, PathPoint p2, float scatteringFromMedia, float absorbationFromMedia, bool hasSceneMedia)
        {
            if (hasSceneMedia == false) return 1;

            MediaLine mediaLine = null;
            if (p1.LineToNextPoint != null && (p1.LineToNextPoint.EndPoint.Position - p2.Position).SquareLength() < 0.00001f)
            {
                mediaLine = p1.LineToNextPoint;
                Assert.AreEqual(p1.LocationType, mediaLine.StartPoint.Location);
                Assert.AreEqual(p2.LocationType, mediaLine.EndPoint.Location);
                Assert.IsTrue((p1.Position - mediaLine.StartPoint.Position).SquareLength() < 0.00001f);
            }
            else
            if (p2.LineToNextPoint != null && (p2.LineToNextPoint.EndPoint.Position - p1.Position).SquareLength() < 0.00001f)
            {
                mediaLine = p2.LineToNextPoint;
                Assert.AreEqual(p2.LocationType, mediaLine.StartPoint.Location);
                Assert.AreEqual(p1.LocationType, mediaLine.EndPoint.Location);
                Assert.IsTrue((p2.Position - mediaLine.StartPoint.Position).SquareLength() < 0.00001f);
            }

            float pdfL = 1;
            foreach (var s in mediaLine.Segments)
            {
                if (s.Media.HasScatteringSomeWhereInMedium())
                {
                    pdfL *= (float)Math.Exp(-(scatteringFromMedia + absorbationFromMedia) * (s.RayMax - s.RayMin));
                    if (mediaLine.EndPointLocation == MediaPointLocationType.MediaParticle)
                    {
                        pdfL *= (scatteringFromMedia + absorbationFromMedia);
                    }
                }
            }

            return pdfL;
        }
    }    
}
