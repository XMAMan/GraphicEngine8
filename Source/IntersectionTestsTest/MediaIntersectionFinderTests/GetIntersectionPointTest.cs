using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using ParticipatingMediaTest.MediaMocks;
using RayTracerGlobal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IntersectionTestsTest.MediaIntersectionFinderTests
{
    //1 Ohne Distancesampling; Ohne GlobalMedia; Es wurde kein Surface- oder Media-Objekte getroffen	-> Es wird null zurück gegeben
    //2 Mit Distancesampling-Shortray;Mit GlobalMedia; Es wurde kein Surface- oder Media-Objekte getroffen; Distanzsmpler findet im Bereich des Strahls keine Partikel ung gibt Float.Max zurück -> Es wird null zurück gegeben
    //3 Mit Distancesampling-Shortray;Mit GlobalMedia; Es wurde kein Surface- oder Media-Objekte getroffen; Distanzsmpler findet Partikel -> Es wird MediaPartikel und ein Segment zurück gegeben
    //4 Mit Distancesampling-LongRay;Mit GlobalMedia; Es wurde kein Surface- oder Media-Objekte getroffen; Distanzsmpler findet Partikel -> Es wird MediaPartikel und ein Segment zurück gegeben
    //5 Ohne Distancesampling;Mit GlobalMedia;MaxDistance == Max;Es wurde kein Surface- oder Media-Objekte getroffen; -> Es wird Infinity-Punkt und ein Segment zurück gegeben
    //6 Ohne Distancesampling;Mit GlobalMedia;MaxDistance < Max;Es wurde kein Surface- oder Media-Objekte getroffen; -> Es wird Partikel-Punkt und ein Segment zurück gegeben
    //7 Mit Distancesampling-Shortray;Innerhalb vom Medium;Distanzsampler bleibt vor MediaGrenze stecken -> Es wird MediaPartikel und ein Segment zurück gegeben
    //8 Mit Distancesampling-Shortray;Innerhalb vom Medium;Distanzsampler bleibt vor MediaGrenze stecken und kurz nach Start -> Es wird MediaPartikel und kein Segment (Segmentlänge zu kurz) zurück gegeben
    //9 Mit Distancesampling-LongRay;Innerhalb vom Medium;Distanzsampler bleibt vor MediaGrenze stecken -> Es wird MediaPartikel und zwei Segmente zurück gegeben
    //10 Mit Distancesampling-LongRay;Innerhalb vom Medium;Distanzsampler bleibt kurz vor MediaGrenze stecken -> Es wird MediaPartikel und ein Segmente zurück gegeben (Das zweite Segment ist zu kurz)
    //11 Ohne Distancesampling; Ohne GlobalMedia;Es wurde Surface aus Medium herraus getroffen -> Es wird SurfacePunkt und ein Segment zurück gegeben 
    //12 Ohne Distancesampling; Mit GlobalMedia;Es wurde Surface aus GlobalMedia herraus getroffen -> Es wird SurfacePunkt und ein Segment zurück gegeben 
    //13 Ohne Distancesampling; Ohne GlobalMedia;Es wurde MediaBorder aus Medium herraus getroffen -> Es wird MediaBorder und ein Segment zurück gegeben
    //14 Ohne Distancesampling; Ohne GlobalMedia;Es wurde MediaBorder aus Vacuum herraus getroffen -> Es wird MediaBorder und ein Segment zurück gegeben

    //Border-Border = Ohne Distancesampling; Ohne GlobalMedia;Strahl startet in Wasserwürfel, welcher direkt an Glaswürfel angrenzt und trifft WasserBorder vom Würfel 1. 
    //Border-Border-Fall 1: Reflektion an WasserWürfel	-> Linker WasserBorder wird getroffen 
    //Border-Border-Fall 2: Brechung an WasserWürfel; Reflektion an GlasWürfel; Brechung an Wasserwürfel -> Linker WasserBorder wird getroffen 
    //Border-Border-Fall 3: Brechung an WasserWürfel; Brechung an GlasWürfel -> Rechter GlasBorder wird getroffen 

    [TestClass]
    public class GetIntersectionPointTest
    {
        [TestMethod]
        public void GetIntersectionPoint_RayPastMediaCubeAndHitNothing_NullIsReturned() //1 Ohne Distancesampling; Ohne GlobalMedia; Es wurde kein Surface- oder Media-Objekte getroffen	-> Es wird null zurück gegeben
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.NoDistanceSampling;
            var intersectionFinder = MediaIntersectionFinderTestHelper.CreateCubeRowOnXAxis(new List<Vector3D>() {
                new Vector3D(0, 1, 0.1f),        //Media-Würfel mit X-Position = 0; Radius = 1; Mit Media
            }, 0).MediaIntersectionFinder;

            Vector3D startPoint = new Vector3D(-2, 3, 0);
            var result = intersectionFinder.GetIntersectionPoint(intersectionFinder.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(1, 0, 0)), null, new Rand(), intersectionMode, 0, float.MaxValue);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetIntersectionPoint_DistanceSamplingCreatesFloatMax_NullIsReturned() //2 Mit Distancesampling-Shortray;Mit GlobalMedia; Es wurde kein Surface- oder Media-Objekte getroffen; Distanzsmpler findet im Bereich des Strahls keine Partikel ung gibt Float.Max zurück -> Es wird null zurück gegeben
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.ShortRayWithDistanceSampling;
            Vector3D startPoint = new Vector3D(-2, 3, 0);

            var sut = MediaIntersectionFinderTestHelper.CreateCubeRowOnXAxis(new List<Vector3D>() {
                     new Vector3D(0, 1, 0.0f),        //Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                    }, 0.1f,
                    new ParticipatingMediaMockData()
                    {
                        ReturnValuesForDistanceSampling = new List<float>() { float.MaxValue }, //SampleDistance = Max; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                        ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { startPoint },
                        ScatteringCoeffizient = 0.1f
                    }).MediaIntersectionFinder;

            //Strahl von Kamera zum MediaBorder
            var result = sut.GetIntersectionPoint(sut.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(1, 0, 0)), null, new Rand(), intersectionMode, 0, float.MaxValue);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetIntersectionPoint_DistanceSamplingShortRayCreatesParticle_ParticleIsReturned() //3 Mit Distancesampling-Shortray;Mit GlobalMedia; Es wurde kein Surface- oder Media-Objekte getroffen; Distanzsmpler findet Partikel -> Es wird MediaPartikel und ein Segment zurück gegeben
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.ShortRayWithDistanceSampling;
            Vector3D startPoint = new Vector3D(-2, 3, 0);

            var sut = MediaIntersectionFinderTestHelper.CreateCubeRowOnXAxis(new List<Vector3D>() {
                     new Vector3D(0, 1, 0.0f),        //Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                    }, 0.1f,
                    new ParticipatingMediaMockData()
                    {
                        ReturnValuesForDistanceSampling = new List<float>() { 1 }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                        ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { startPoint },
                        ScatteringCoeffizient = 0.1f
                    }).MediaIntersectionFinder;

            //Strahl von Kamera zum MediaBorder
            var result = sut.GetIntersectionPoint(sut.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(1, 0, 0)), null, new Rand(), intersectionMode, 0, float.MaxValue);
            Assert.AreEqual(1, result.Segments.Count);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, result.EndPoint.Location);
            Assert.IsTrue(result.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Endpunkt liegt in Medium am Rand
            Assert.AreEqual(new Vector3D(-1, 3, 0), result.EndPoint.Position);
            Assert.IsNull(result.EndPoint.SurfacePoint);

            //Segent von Kamera bis Partikel      
            Assert.AreEqual(0, result.Segments[0].RayMin);
            Assert.AreEqual(1, result.Segments[0].RayMax);
            Assert.IsTrue(result.Segments[0].Media.HasScatteringSomeWhereInMedium());
        }

        [TestMethod]
        public void GetIntersectionPoint_NoDistanceSamplingInGlobalMedia_InfinityIsReturned() //4 Mit Distancesampling-LongRay;Mit GlobalMedia; Es wurde kein Surface- oder Media-Objekte getroffen; Distanzsmpler findet Partikel -> Es wird MediaPartikel und ein Segment zurück gegeben
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.LongRayWithDistanceSampling;
            Vector3D startPoint = new Vector3D(-2, 3, 0);

            var sut = MediaIntersectionFinderTestHelper.CreateCubeRowOnXAxis(new List<Vector3D>() {
                     new Vector3D(0, 1, 0.0f),        //Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                    }, 0.1f,
                    new ParticipatingMediaMockData()
                    {
                        ReturnValuesForDistanceSampling = new List<float>() { 1 }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                        ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { startPoint },
                        ScatteringCoeffizient = 0.1f
                    }).MediaIntersectionFinder;

            //Strahl von Kamera zum MediaBorder
            var result = sut.GetIntersectionPoint(sut.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(1, 0, 0)), null, new Rand(), intersectionMode, 0, float.MaxValue);
            Assert.AreEqual(2, result.Segments.Count);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, result.EndPoint.Location);
            Assert.IsTrue(result.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Endpunkt liegt in Medium am Rand
            Assert.AreEqual(new Vector3D(-1, 3, 0), result.EndPoint.Position);
            Assert.IsNull(result.EndPoint.SurfacePoint);

            //Segent von Kamera bis Partikel      
            Assert.AreEqual(0, result.Segments[0].RayMin);
            Assert.AreEqual(1, result.Segments[0].RayMax);
            Assert.IsTrue(result.Segments[0].Media.HasScatteringSomeWhereInMedium());

            //Segent von Partikel bis Infinity      
            Assert.AreEqual(1, result.Segments[1].RayMin);
            Assert.IsTrue(result.Segments[1].RayMax > 1000);
            Assert.IsTrue(result.Segments[1].Media.HasScatteringSomeWhereInMedium());
        }

        [TestMethod]
        public void GetIntersectionPoint_NoDistanceSamplingGlobalMediaNoBlockingDistanceMax_ParticleIsReturned() //5 Ohne Distancesampling;Mit GlobalMedia;Es wurde kein Surface- oder Media-Objekte getroffen; -> Es wird Infinity-Punkt und ein Segment zurück gegeben
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.NoDistanceSampling;
            Vector3D startPoint = new Vector3D(-2, 3, 0);

            var sut = MediaIntersectionFinderTestHelper.CreateCubeRowOnXAxis(new List<Vector3D>() {
                     new Vector3D(0, 1, 0.0f),        //Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                    }, 0.1f,
                    new ParticipatingMediaMockData()
                    {
                        ReturnValuesForDistanceSampling = new List<float>() { 1 }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                        ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { startPoint },
                        ScatteringCoeffizient = 0.1f
                    }).MediaIntersectionFinder;

            //Strahl von Kamera zum MediaBorder
            var result = sut.GetIntersectionPoint(sut.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(1, 0, 0)), null, new Rand(), intersectionMode, 0, float.MaxValue);
            Assert.AreEqual(1, result.Segments.Count);
            Assert.AreEqual(MediaPointLocationType.MediaInfinity, result.EndPoint.Location);
            Assert.IsTrue(result.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Endpunkt liegt in Medium am Rand
            Assert.IsTrue(result.EndPoint.Position.X > 1000);
            Assert.IsNull(result.EndPoint.SurfacePoint);

            //Segent von Kamera bis Infinity      
            Assert.AreEqual(0, result.Segments[0].RayMin);
            Assert.IsTrue(result.Segments[0].RayMax > 1000);
            Assert.IsTrue(result.Segments[0].Media.HasScatteringSomeWhereInMedium());
        }

        [TestMethod]
        public void GetIntersectionPoint_NoDistanceSamplingGlobalMediaNoBlockingDistanceNotMax_ParticleIsReturned() //6 Ohne Distancesampling;Mit GlobalMedia;MaxDistance < Max;Es wurde kein Surface- oder Media-Objekte getroffen; -> Es wird Partikel-Punkt und ein Segment zurück gegeben
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.NoDistanceSampling;
            Vector3D startPoint = new Vector3D(-2, 3, 0);

            var sut = MediaIntersectionFinderTestHelper.CreateCubeRowOnXAxis(new List<Vector3D>() {
                     new Vector3D(0, 1, 0.0f),        //Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                    }, 0.1f,
                    new ParticipatingMediaMockData()
                    {
                        ReturnValuesForDistanceSampling = new List<float>() { 1 }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                        ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { startPoint },
                        ScatteringCoeffizient = 0.1f
                    }).MediaIntersectionFinder;

            //Strahl von Kamera zum MediaBorder
            var result = sut.GetIntersectionPoint(sut.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(1, 0, 0)), null, new Rand(), intersectionMode, 0, 5);
            Assert.AreEqual(1, result.Segments.Count);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, result.EndPoint.Location);
            Assert.IsTrue(result.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Endpunkt liegt in Medium am Rand
            Assert.AreEqual(-2 + 5, result.EndPoint.Position.X);
            Assert.IsNull(result.EndPoint.SurfacePoint);

            //Segent von Kamera bis Partikel      
            Assert.AreEqual(0, result.Segments[0].RayMin);
            Assert.AreEqual(5, result.Segments[0].RayMax);
            Assert.IsTrue(result.Segments[0].Media.HasScatteringSomeWhereInMedium());
        }

        [TestMethod]
        public void GetIntersectionPoint_DistanceSamplingShortRayCreatesParticle1_ParticleIsReturned() //7 Mit Distancesampling-Shortray;Innerhalb vom Medium;Distanzsampler bleibt vor MediaGrenze stecken -> Es wird MediaPartikel und ein Segment zurück gegeben
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.ShortRayWithDistanceSampling;
            Vector3D startPoint = new Vector3D(-3, 0, 0);

            var sut = MediaIntersectionFinderTestHelper.CreateCubeRowOnXAxis(new List<Vector3D>() {
                     new Vector3D(0, 1, 0.0f),        //Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                    }, 0.1f,
                    new ParticipatingMediaMockData()
                    {
                        ReturnValuesForDistanceSampling = new List<float>() { 1 }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                        ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { startPoint },
                        ScatteringCoeffizient = 0.1f
                    }).MediaIntersectionFinder;

            var result = sut.GetIntersectionPoint(sut.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(1, 0, 0)), null, new Rand(), intersectionMode, 0, float.MaxValue);
            Assert.AreEqual(1, result.Segments.Count);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, result.EndPoint.Location);
            Assert.IsTrue(result.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Endpunkt liegt in Medium am Rand
            Assert.AreEqual(new Vector3D(-2, 0, 0), result.EndPoint.Position);
            Assert.IsNull(result.EndPoint.SurfacePoint);

            //Segent von Kamera bis Partikel      
            Assert.AreEqual(0, result.Segments[0].RayMin);
            Assert.AreEqual(1, result.Segments[0].RayMax);
            Assert.IsTrue(result.Segments[0].Media.HasScatteringSomeWhereInMedium());
        }

        [TestMethod]
        public void GetIntersectionPoint_DistanceSamplingShortRayVeryShortDistanceSampling_ParticleIsReturned() //8 Mit Distancesampling-Shortray;Innerhalb vom Medium;Distanzsampler bleibt vor MediaGrenze stecken und kurz nach Start -> Es wird MediaPartikel und kein Segment (Segmentlänge zu kurz) zurück gegeben
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.ShortRayWithDistanceSampling;
            Vector3D startPoint = new Vector3D(-3, 0, 0);

            var sut = MediaIntersectionFinderTestHelper.CreateCubeRowOnXAxis(new List<Vector3D>() {
                     new Vector3D(0, 1, 0.0f),        //Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                    }, 0.1f,
                    new ParticipatingMediaMockData()
                    {
                        ReturnValuesForDistanceSampling = new List<float>() { MagicNumbers.MinAllowedPathPointDistance / 2 }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                        ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { startPoint },
                        ScatteringCoeffizient = 0.1f
                    }).MediaIntersectionFinder;

            var result = sut.GetIntersectionPoint(sut.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(1, 0, 0)), null, new Rand(), intersectionMode, 0, float.MaxValue);
            Assert.AreEqual(0, result.Segments.Count);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, result.EndPoint.Location);
            Assert.IsTrue(result.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Endpunkt liegt in Medium am Rand
            Assert.AreEqual(new Vector3D(-3 + MagicNumbers.MinAllowedPathPointDistance / 2, 0, 0), result.EndPoint.Position);
            Assert.IsNull(result.EndPoint.SurfacePoint);
        }

        [TestMethod]
        public void GetIntersectionPoint_DistanceSamplingLongRayCreatesParticle1_ParticleIsReturned() //9 Mit Distancesampling-LongRay;Innerhalb vom Medium;Distanzsampler bleibt vor MediaGrenze stecken -> Es wird MediaPartikel und zwei Segmente zurück gegeben
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.LongRayWithDistanceSampling;
            Vector3D startPoint = new Vector3D(-3, 0, 0);

            var sut = MediaIntersectionFinderTestHelper.CreateCubeRowOnXAxis(new List<Vector3D>() {
                     new Vector3D(0, 1, 0.0f),        //Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                    }, 0.1f,
                    new ParticipatingMediaMockData()
                    {
                        ReturnValuesForDistanceSampling = new List<float>() { 1 }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                        ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { startPoint },
                        ScatteringCoeffizient = 0.1f
                    }).MediaIntersectionFinder;

            var result = sut.GetIntersectionPoint(sut.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(1, 0, 0)), null, new Rand(), intersectionMode, 0, float.MaxValue);
            Assert.AreEqual(2, result.Segments.Count);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, result.EndPoint.Location);
            Assert.IsTrue(result.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Endpunkt liegt in Medium am Rand
            Assert.AreEqual(new Vector3D(-2, 0, 0), result.EndPoint.Position);
            Assert.IsNull(result.EndPoint.SurfacePoint);

            //Segent von Kamera bis Partikel      
            Assert.AreEqual(0, result.Segments[0].RayMin);
            Assert.AreEqual(1, result.Segments[0].RayMax);
            Assert.IsTrue(result.Segments[0].Media.HasScatteringSomeWhereInMedium());

            //Segent von Partikel bis SolidWürfel      
            Assert.AreEqual(1, result.Segments[1].RayMin);
            Assert.AreEqual(2, result.Segments[1].RayMax);
            Assert.IsTrue(result.Segments[1].Media.HasScatteringSomeWhereInMedium());
        }

        [TestMethod]
        public void GetIntersectionPoint_DistanceSamplingLongRayCreatesParticleToShortForNextBorder_ParticleIsReturned() //10 Mit Distancesampling-LongRay;Innerhalb vom Medium;Distanzsampler bleibt kurz vor MediaGrenze stecken -> Es wird MediaPartikel und ein Segmente zurück gegeben (Das zweite Segment ist zu kurz)
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.LongRayWithDistanceSampling;
            Vector3D startPoint = new Vector3D(-3, 0, 0);

            var sut = MediaIntersectionFinderTestHelper.CreateCubeRowOnXAxis(new List<Vector3D>() {
                     new Vector3D(0, 1, 0.0f),        //Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                    }, 0.1f,
                    new ParticipatingMediaMockData()
                    {
                        ReturnValuesForDistanceSampling = new List<float>() { 2 - MagicNumbers.MinAllowedPathPointDistance / 2 }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                        ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { startPoint },
                        ScatteringCoeffizient = 0.1f
                    }).MediaIntersectionFinder;

            var result = sut.GetIntersectionPoint(sut.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(1, 0, 0)), null, new Rand(), intersectionMode, 0, float.MaxValue);
            Assert.AreEqual(1, result.Segments.Count);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, result.EndPoint.Location);
            Assert.IsTrue(result.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Endpunkt liegt in Medium am Rand
            Assert.AreEqual(new Vector3D(-3 + (2 - MagicNumbers.MinAllowedPathPointDistance / 2), 0, 0), result.EndPoint.Position);
            Assert.IsNull(result.EndPoint.SurfacePoint);

            //Segent von Kamera bis Partikel      
            Assert.AreEqual(0, result.Segments[0].RayMin);
            Assert.AreEqual(2 - MagicNumbers.MinAllowedPathPointDistance / 2, result.Segments[0].RayMax);
            Assert.IsTrue(result.Segments[0].Media.HasScatteringSomeWhereInMedium());
        }

        [TestMethod]
        public void GetIntersectionPoint_WatchOntoEarthFromAirSphere_OneSegmentIsReturned() //11 Ohne Distancesampling; Ohne GlobalMedia;Es wurde Surface aus Medium herraus getroffen -> Es wird SurfacePunkt und ein Segment zurück gegeben 
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.NoDistanceSampling;
            var intersectionFinder = MediaIntersectionFinderTestHelper.CreateSphereRowOnXAxis(new List<Vector3D>() {
                new Vector3D(0, 1, 0),        //Erde mit X-Position = 0; Radius = 1; Ohne Media
                new Vector3D(0, 10, 0.1f)     //Himmel mit X-Position = 0; Radius = 10; Media mit ScatterCoeffizient = 0.1
            }, 0).MediaIntersectionFinder;

            Vector3D startPoint = new Vector3D(1.1f, 0, 0);
            var result = intersectionFinder.GetIntersectionPoint(intersectionFinder.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(-1, 0, 0)), null, new Rand(), intersectionMode, 0, float.MaxValue);

            Assert.AreEqual(1, result.Segments.Count);
            Assert.AreEqual(MediaPointLocationType.Surface, result.EndPoint.Location);
            Assert.IsTrue(result.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Endpunkt liegt in Medium am Rand
            Assert.AreEqual(new Vector3D(1, 0, 0), result.EndPoint.Position);
            Assert.AreEqual(result.EndPoint.SurfacePoint.Position, result.EndPoint.Position);

            //Segent von Kamera zur Erde   
            Assert.AreEqual(0, result.Segments[0].RayMin);
            Assert.AreEqual(1.1f - 1, result.Segments[0].RayMax);
            Assert.IsTrue(result.Segments[0].Media.HasScatteringSomeWhereInMedium());
        }

        [TestMethod]
        public void GetIntersectionPoint_WatchOntoEarthFromGlobalMedia_OneSegmentIsReturned() //12 Ohne Distancesampling; Mit GlobalMedia;Es wurde Surface aus GlobalMedia herraus getroffen -> Es wird SurfacePunkt und ein Segment zurück gegeben 
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.NoDistanceSampling;
            var intersectionFinder = MediaIntersectionFinderTestHelper.CreateSphereRowOnXAxis(new List<Vector3D>() {
                new Vector3D(0, 1, 0),        //Erde mit X-Position = 0; Radius = 1; Ohne Media
            }, 0.2f).MediaIntersectionFinder;

            Vector3D startPoint = new Vector3D(1.1f, 0, 0);
            var result = intersectionFinder.GetIntersectionPoint(intersectionFinder.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(-1, 0, 0)), null, new Rand(), intersectionMode, 0, float.MaxValue);

            Assert.AreEqual(1, result.Segments.Count);
            Assert.AreEqual(MediaPointLocationType.Surface, result.EndPoint.Location);
            Assert.IsTrue(result.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Endpunkt liegt in Medium am Rand
            Assert.AreEqual(new Vector3D(1, 0, 0), result.EndPoint.Position);
            Assert.AreEqual(result.EndPoint.SurfacePoint.Position, result.EndPoint.Position);

            //Segent von Kamera zur Erde   
            Assert.AreEqual(0, result.Segments[0].RayMin);
            Assert.AreEqual(1.1f - 1, result.Segments[0].RayMax);
            Assert.IsTrue(result.Segments[0].Media.HasScatteringSomeWhereInMedium());
        }

        [TestMethod]
        public void GetIntersectionPoint_WatchIntoSkyFromEarth_OneSegmentIsReturned() //13 Ohne Distancesampling; Ohne GlobalMedia;Es wurde MediaBorder aus Medium herraus getroffen -> Es wird MediaBorder und ein Segment zurück gegeben
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.NoDistanceSampling;
            var intersectionFinder = MediaIntersectionFinderTestHelper.CreateSphereRowOnXAxis(new List<Vector3D>() {
                new Vector3D(0, 1, 0),        //Erde mit X-Position = 0; Radius = 1; Ohne Media
                new Vector3D(0, 10, 0.1f)     //Himmel mit X-Position = 0; Radius = 10; Media mit ScatterCoeffizient = 0.1
            }, 0).MediaIntersectionFinder;

            Vector3D startPoint = new Vector3D(1.1f, 0, 0);
            var result = intersectionFinder.GetIntersectionPoint(intersectionFinder.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(1, 0, 0)), null, new Rand(), intersectionMode, 0, float.MaxValue);

            Assert.AreEqual(1, result.Segments.Count);
            Assert.AreEqual(MediaPointLocationType.NullMediaBorder, result.EndPoint.Location);
            Assert.IsTrue(result.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Endpunkt liegt in Medium am Rand
            Assert.AreEqual(new Vector3D(10, 0, 0), result.EndPoint.Position);
            Assert.AreEqual(result.EndPoint.SurfacePoint.Position, result.EndPoint.Position);

            //Segent von Kamera bis Atmosphären-Kugel            
            Assert.AreEqual(0, result.Segments[0].RayMin);
            Assert.AreEqual(10 - 1.1f, result.Segments[0].RayMax);
            Assert.IsTrue(result.Segments[0].Media.HasScatteringSomeWhereInMedium());
        }

        
        [TestMethod]
        public void GetIntersectionPoint_WatchIntoSkyFromSpace_OneSegmentIsReturned() //14 Ohne Distancesampling; Ohne GlobalMedia;Es wurde MediaBorder aus Vacuum herraus getroffen -> Es wird MediaBorder und ein Segment zurück gegeben
        {
            var intersectionMode = MediaIntersectionFinder.IntersectionMode.NoDistanceSampling;
            var intersectionFinder = MediaIntersectionFinderTestHelper.CreateSphereRowOnXAxis(new List<Vector3D>() {
                new Vector3D(0, 1, 0),        //Erde mit X-Position = 0; Radius = 1; Ohne Media
                new Vector3D(0, 10, 0.1f)     //Himmel mit X-Position = 0; Radius = 10; Media mit ScatterCoeffizient = 0.1
            }, 0).MediaIntersectionFinder;

            Vector3D startPoint = new Vector3D(-11, 0, 0);
            var result = intersectionFinder.GetIntersectionPoint(intersectionFinder.CreateCameraMediaStartPoint(startPoint), new Ray(startPoint, new Vector3D(1, 0, 0)), null, new Rand(), intersectionMode, 0, float.MaxValue);

            Assert.AreEqual(1, result.Segments.Count);
            Assert.AreEqual(MediaPointLocationType.NullMediaBorder, result.EndPoint.Location);
            Assert.IsFalse(result.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Endpunkt liegt in Vacuum am Rand
            Assert.AreEqual(new Vector3D(-10, 0, 0), result.EndPoint.Position);
            Assert.AreEqual(result.EndPoint.SurfacePoint.Position, result.EndPoint.Position);

            //Segent von Kamera bis Atmosphären-Kugel            
            Assert.AreEqual(0, result.Segments[0].RayMin);
            Assert.AreEqual(1, result.Segments[0].RayMax);
            Assert.IsFalse(result.Segments[0].Media.HasScatteringSomeWhereInMedium());
        }
    }
}
