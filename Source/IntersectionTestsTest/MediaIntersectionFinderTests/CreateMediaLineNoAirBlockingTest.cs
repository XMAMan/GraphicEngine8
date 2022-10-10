using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using ParticipatingMediaTest.MediaMocks;
using RayObjects;
using RayTracerGlobal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IntersectionTestsTest.MediaIntersectionFinderTests
{
    //Testfälle für die MediaIntersectionFinder.CreateMediaLineNoAirBlocking-Methode:
    //0. Treffe garnichts
    //1. Treffe Umgebungslicht direkt von der Kamera aus
    //2. Treffe Umgebungslicht nachdem Strahl durch AirCube geflogen ist
    //3. Verlasse die Scene ins Vacuum nachdem zwei Air-Cubes durchlaufen wurden
    //4. Gehe ohne Distanzsampling mit GlobalMedia durch zwei AirCubes und lande auf InfinityPoint
    //5. Lande auf Partikel im Air-Cube und erzeuge LongRay
    //6. Lande auf Glas-Rand
    //7. Lande auf Diffuse-Surface
    //8. Visible-Test zwischen Kamera und Partikel (MaxDistanz < Float.Max)
    //9. Randfall-Segment zu kurz 1: Sample Distanz bis kurz vor MediaRand (Long-Ray-Segment geht verloren)
    //10. Randfall-Segment zu kurz 2: Starte kurz vor MediaRand (Erstes Segment geht verloren)
    //11. Randfall-Segment zu kurz 3: Durchlaufe Air-Objekt am Rand (Mittleres Segment geht verloren)

    [TestClass]
    public class CreateMediaLineNoAirBlockingTest
    {
        [TestMethod]
        public void CreateMediaLineNoAirBlocking_NoPartikelOrSurfaceHit_null()//0. Treffe garnichts
        {
            MediaLine actual = CreateMediaAirLine(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.Nothing, BlockingObject.Nothing, GlobalMedia.NoGlobalMedia, MediaIntersectionFinder.IntersectionMode.NoDistanceSampling, EnvironmentLight.No, DistanceValues.Emtpy);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void CreateMediaLineNoAirBlocking_HitEnvironmentLight_LightPoint() //1. Treffe Umgebungslicht direkt von der Kamera aus
        {
            MediaLine actual = CreateMediaAirLine(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.Nothing, BlockingObject.Nothing, GlobalMedia.NoGlobalMedia, MediaIntersectionFinder.IntersectionMode.NoDistanceSampling, EnvironmentLight.Yes, DistanceValues.Emtpy);
            var expected = new ExpectedMediaLine(new S[] { new S(9, 0) }, new P(10, 0, MediaPointLocationType.Surface, true));
            ExpectedMediaLine.CheckMediaLine(expected, actual);
        }

        [TestMethod]
        public void CreateMediaLineNoAirBlocking_HitEnvironmentLightAfterAirCubes_LightPoint() //2. Treffe Umgebungslicht nachdem Strahl durch AirCube geflogen ist
        {
            MediaLine actual = CreateMediaAirLine(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.AirCube, BlockingObject.AirCube, GlobalMedia.NoGlobalMedia, MediaIntersectionFinder.IntersectionMode.NoDistanceSampling, EnvironmentLight.Yes, DistanceValues.Emtpy);
            var expected = new ExpectedMediaLine(new S[] { new S(1, 0), new S(2, 0.1f), new S(1, 0), new S(2, 0.1f), new S(3, 0) }, new P(10, 0, MediaPointLocationType.Surface, true));
            ExpectedMediaLine.CheckMediaLine(expected, actual);
        }

        [TestMethod]
        public void CreateMediaLineNoAirBlocking_HitNothingLightAfterAirCubes_NullBorder()//3. Verlasse die Scene ins Vacuum nachdem zwei Air-Cubes durchlaufen wurden
        {
            MediaLine actual = CreateMediaAirLine(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.AirCube, BlockingObject.AirCube, GlobalMedia.NoGlobalMedia, MediaIntersectionFinder.IntersectionMode.NoDistanceSampling, EnvironmentLight.No, DistanceValues.Emtpy);
            var expected = new ExpectedMediaLine(new S[] { new S(1, 0), new S(2, 0.1f), new S(1, 0), new S(2, 0.1f) }, new P(7, 0, MediaPointLocationType.NullMediaBorder, false));
            ExpectedMediaLine.CheckMediaLine(expected, actual);
        }

        [TestMethod]
        public void CreateMediaLineNoAirBlocking_HitNothingLightAfterAirCubes_Infinity()//4. Gehe ohne Distanzsampling mit GlobalMedia durch zwei AirCubes und lande auf InfinityPoint
        {
            MediaLine actual = CreateMediaAirLine(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.AirCube, BlockingObject.AirCube, GlobalMedia.WithGlobalMedia, MediaIntersectionFinder.IntersectionMode.NoDistanceSampling, EnvironmentLight.No, DistanceValues.Emtpy);
            var expected = new ExpectedMediaLine(new S[] { new S(1, 0.2f), new S(2, 0.1f), new S(1, 0.2f), new S(2, 0.1f), new S(100000, 0.2f) }, new P(100007, 0.2f, MediaPointLocationType.MediaInfinity, false));
            ExpectedMediaLine.CheckMediaLine(expected, actual);
        }

        [TestMethod]
        public void CreateMediaLineNoAirBlocking_HitPartikelInSecondCube_Partikel()//5. Lande auf Partikel im Air-Cube und erzeuge LongRay
        {
            MediaLine actual = CreateMediaAirLine(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.AirCube, BlockingObject.AirCube, GlobalMedia.NoGlobalMedia, MediaIntersectionFinder.IntersectionMode.LongRayWithDistanceSampling, EnvironmentLight.Yes, DistanceValues.TwoOne);
            var expected = new ExpectedMediaLine(new S[] { new S(1, 0), new S(2, 0.1f), new S(1, 0), new S(1, 0.1f), new S(1, 0.1f) }, new P(6, 0.1f, MediaPointLocationType.MediaParticle, false));
            ExpectedMediaLine.CheckMediaLine(expected, actual);
        }

        [TestMethod]
        public void CreateMediaLineNoAirBlocking_HitMediaBorderFromSecondCube_MediaBorder()//6. Lande auf Glas-Rand
        {
            MediaLine actual = CreateMediaAirLine(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.AirCube, BlockingObject.GlasWithMedia, GlobalMedia.NoGlobalMedia, MediaIntersectionFinder.IntersectionMode.LongRayWithDistanceSampling, EnvironmentLight.Yes, DistanceValues.TwoOne);
            var expected = new ExpectedMediaLine(new S[] { new S(1, 0), new S(2, 0.1f), new S(1, 0) }, new P(5, 0, MediaPointLocationType.MediaBorder, false));
            ExpectedMediaLine.CheckMediaLine(expected, actual);
        }

        [TestMethod]
        public void CreateMediaLineNoAirBlocking_HitSurfaceFromSecondCube_Surface()//7. Lande auf Diffuse-Surface
        {
            MediaLine actual = CreateMediaAirLine(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.AirCube, BlockingObject.Surface, GlobalMedia.NoGlobalMedia, MediaIntersectionFinder.IntersectionMode.LongRayWithDistanceSampling, EnvironmentLight.Yes, DistanceValues.TwoOne);
            var expected = new ExpectedMediaLine(new S[] { new S(1, 0), new S(2, 0.1f), new S(1, 0) }, new P(5, 0, MediaPointLocationType.Surface, false));
            ExpectedMediaLine.CheckMediaLine(expected, actual);
        }

        [TestMethod]
        public void CreateMediaLineNoAirBlocking_VisibleTestBetweenCameraAndParticle_Particle() //8. Visible-Test zwischen Kamera und Partikel (MaxDistanz < Float.Max)
        {
            MediaLine actual = CreateMediaAirLine(StartLocation.Camera, EndDistance.UpToParticle, BlockingObject.AirCube, BlockingObject.Nothing, GlobalMedia.NoGlobalMedia, MediaIntersectionFinder.IntersectionMode.NoDistanceSampling, EnvironmentLight.Yes, DistanceValues.Emtpy);
            var expected = new ExpectedMediaLine(new S[] { new S(1, 0), new S(1, 0.1f) }, new P(3, 0.1f, MediaPointLocationType.MediaParticle, false));
            ExpectedMediaLine.CheckMediaLine(expected, actual);
        }


        //Ich starte auf Partikel und sample eine Distanz, die ganz nah an den MediaBorder ranreicht. Dadurch kann das LongRay-Segment-Stück, was nach dem Partikel kommt nicht erstellt werden
        [TestMethod]
        public void CreateMediaLineNoAirBlocking_StartOnParticleAndSampleDistanceNearBorder_LastSegmentFromLongRayIsMissing()//9. Randfall-Segment zu kurz 1: Sample Distanz bis kurz vor MediaRand (Long-Ray-Segment geht verloren)
        {
            MediaLine actual = CreateMediaAirLine(StartLocation.MediaParticle, EndDistance.FloatMax, BlockingObject.Nothing, BlockingObject.Nothing, GlobalMedia.NoGlobalMedia, MediaIntersectionFinder.IntersectionMode.LongRayWithDistanceSampling, EnvironmentLight.No, DistanceValues.NearOne);
            var expected = new ExpectedMediaLine(new S[] { new S(0.9995f, 0.1f) }, new P(0.9995f, 0.1f, MediaPointLocationType.MediaParticle, false));
            ExpectedMediaLine.CheckMediaLine(expected, actual);
        }

        //Ich start auf Partikel, der ganz nah am MediaBorder liegt und danach kommt das Umgebungslicht. Dadurch kann das erste Segment vom Partikel bis zum Rand nicht erstellt werden.
        //Ich springe also vom Partikel bis zum NullMediaBorder das kurze Stück ohne Segmenterstellung und von da treffe ich dann auf 
        //das Umgebungslicht. Meine MediaLine enthält also nur ein Segment (Vom Air-Border zur Lichtquelle). Hier muss dann gelten: LongRaySegmentCount==ShortRaySegmentCount
        [TestMethod]
        public void CreateMediaLineNoAirBlocking_StartOnParticleNearBorder_ReturnNull() //10. Randfall-Segment zu kurz 2: Starte kurz vor MediaRand (Erstes Segment geht verloren)
        {
            MediaLine actual = CreateMediaAirLine(StartLocation.MediaParticleNearBorder, EndDistance.FloatMax, BlockingObject.Nothing, BlockingObject.Nothing, GlobalMedia.NoGlobalMedia, MediaIntersectionFinder.IntersectionMode.LongRayWithDistanceSampling, EnvironmentLight.Yes, DistanceValues.NearOneFromNearBorder);
            
            //Anstatt zwei habe ich nur ein Segment, da das erste Segment vom Partikel bis zum MediaBorder fehlt da es zu kurz ist
            //Deswegen fängt Segment[0].RayMin auch nicht bei 0 sondern bei einer kleinen Zahl größer 0 an.
            Assert.AreEqual(1, actual.Segments.Count);
            Assert.AreEqual(1, actual.ShortRaySegmentCount);
            Assert.AreEqual(0.000500023365f, actual.Segments[0].RayMin);
            Assert.AreEqual(9.0005f, actual.Segments[0].RayMax);
            Assert.AreEqual(10, actual.EndPoint.Position.X);
        }

        [TestMethod]
        public void CreateMediaLineNoAirBlocking_GoThrougAirEdge_EdgeSegmentIsMissing() //11. Randfall-Segment zu kurz 3: Durchlaufe Air-Objekt am Rand (Mittleres Segment geht verloren)
        {
            MediaLine actual = CreateMediaAirLine(StartLocation.CameraOnYNearOne, EndDistance.FloatMax, BlockingObject.AirSphere, BlockingObject.Surface, GlobalMedia.NoGlobalMedia, MediaIntersectionFinder.IntersectionMode.NoDistanceSampling, EnvironmentLight.Yes, DistanceValues.Emtpy);

            //Eigentlich hätte ich 3 Segmente: [Camera-AirKugel] [AirKugel-AirKugel] [AirKugel - Surface]
            //Das mittlere Segment ist aber zu kurz. Deswegen fehlt es und RayMax von Segment0 ist kleiner als RayMin von Segment1

            Assert.AreEqual(2, actual.Segments.Count);
            Assert.AreEqual(2, actual.ShortRaySegmentCount);
            Assert.AreEqual(0, actual.Segments[0].RayMin);
            Assert.AreEqual(1.99951172f, actual.Segments[0].RayMax);
            Assert.AreEqual(1.99951172f, actual.Segments[1].RayMin);
            Assert.AreEqual(3.99902344f, actual.Segments[1].RayMax);
            Assert.AreEqual(5, actual.EndPoint.Position.X);
        }

        private MediaLine CreateMediaAirLine(StartLocation start, EndDistance distance, BlockingObject blockingObject1, BlockingObject blockingObject2, GlobalMedia globalMedia, MediaIntersectionFinder.IntersectionMode intersectionMode, EnvironmentLight environmentLight, DistanceValues distanceValues)
        {
            var data = TestData.CreateTestData(start, blockingObject1, blockingObject2, globalMedia, environmentLight, distanceValues);
            return data.MediaIntersectionFinder.CreateMediaLineNoAirBlocking(data.StartPoint, new Vector3D(1, 0, 0), null, intersectionMode, 0, distance == EndDistance.FloatMax ? float.MaxValue : 2, data.EnvironmentLight);
        }

        //Prüft ob die MediaLine so aussieht wie erwartet
        public class ExpectedMediaLine
        {
            private readonly S[] segments;
            private readonly P endPoint;
            public ExpectedMediaLine(S[] segments, P endPoint)
            {
                this.segments = segments;
                this.endPoint = endPoint;
            }

            public static void CheckMediaLine(ExpectedMediaLine expected, MediaLine mediaLine)
            {
                if (expected == null)
                {
                    Assert.IsNull(mediaLine);
                    return;
                }

                S.CheckSegments(expected.segments, mediaLine.Segments);
                expected.endPoint.CheckPoint(mediaLine.EndPoint);
            }
        }

        //Expected MediaIntersectionPoint
        public class P
        {
            private readonly float posX;
            private readonly float scatteringCoeffizient;
            private readonly MediaPointLocationType location;
            private readonly bool isLocatedOnLightSource;

            public P(float posX, float scatteringCoeffizient, MediaPointLocationType location, bool isLocatedOnLightSource)
            {
                this.posX = posX;
                this.scatteringCoeffizient = scatteringCoeffizient;
                this.location = location;
                this.isLocatedOnLightSource = isLocatedOnLightSource;
            }

            public void CheckPoint(MediaIntersectionPoint actual)
            {
                Assert.AreEqual(posX, actual.Position.X);
                Assert.AreEqual(scatteringCoeffizient, actual.CurrentMedium.GetScatteringCoeffizient(null).X);
                Assert.AreEqual(location, actual.Location);

                if (isLocatedOnLightSource || actual.SurfacePoint != null)
                    Assert.AreEqual(isLocatedOnLightSource, actual.SurfacePoint.IsLocatedOnLightSource);                
            }
        }

        //ExpectedSegment
        public class S
        {
            private readonly float length;
            private readonly float scatteringCoeffizient;
            public S(float length, float scatteringCoeffizient)
            {
                this.length = length;
                this.scatteringCoeffizient = scatteringCoeffizient;
            }

            public static void CheckSegments(S[] expected, List<VolumeSegment> actual)
            {
                Assert.AreEqual(expected.Length, actual.Count);
                float rayMin = 0;
                float rayMax = expected[0].length;
                for (int i=0;i<expected.Length;i++)
                {
                    Assert.AreEqual(rayMin, actual[i].RayMin);
                    Assert.AreEqual(rayMax, actual[i].RayMax);
                    Assert.AreEqual(expected[i].scatteringCoeffizient, actual[i].Media.GetScatteringCoeffizient(null).X);

                    if (i < expected.Length - 1)
                    {
                        rayMin = rayMax;
                        rayMax = rayMin + expected[i + 1].length;
                    }                    
                }
            }
        }

        //(Start von Partikel/Camera|Start von Surface)*(Bis Partikel | Bis Float-Max) * (Treffe Nix|Treffe Glas ohne Media|Treffe Glas mit Media|Treffe Luft-Würfel)*(Ohne GlobalMedia|Mit GlobalMedia) = 32 Kombinationen
        //Startpunkt (1,0,0) -> Rechte Kante von Würfel 1 (Wenn vorhanden)
        //Würfel 1 liegt bei X-Pos 0 (Radius 1) (Lichtquelle)      -1..1
        //Würfel 2 liegt bei X-Pos 3 (Radius 1) (BlockingObjekt1)   2..4
        //Würfel 3 liegt bei X-Pos 6 (Radius 1) (BlockingObjekt1)   5..7
        public enum StartLocation { Camera, CameraOnYNearOne, Surface, MediaParticle, MediaParticleNearBorder }
        public enum EndDistance { UpToParticle, FloatMax }
        public enum BlockingObject { Nothing, Surface, GlasWithMedia, AirCube, AirSphere }
        public enum GlobalMedia { NoGlobalMedia, WithGlobalMedia }
        //MediaIntersectionFinder.IntersectionMode
        public enum EnvironmentLight { Yes, No}
        public enum DistanceValues { Emtpy, One, TwoOne, NearOne, NearOneFromNearBorder }

        class LightsourcesWithoutEnvironmentLight : IIntersectableEnvironmentLight
        {
            public bool ContainsEnvironmentLight => false;

            public IntersectionPoint GetIntersectionPointWithEnvironmentLight(Ray ray)
            {
                throw new NotImplementedException();
            }
        }

        class LightsourcesWithEnvironmentLight : IIntersectableEnvironmentLight
        {
            public bool ContainsEnvironmentLight => true;

            public IntersectionPoint GetIntersectionPointWithEnvironmentLight(Ray ray)
            {
                return new IntersectionPoint(new Vertex(10, 0, 0), null, null, new Vector3D(-1,0,0), new Vector3D(-1, 0, 0), null, null, new RayDrawingObject(new ObjectPropertys() { RaytracingLightSource = new EnvironmentLightDescription() }, null, null));
            }
        }

        class TestData
        {
            public MediaIntersectionFinder MediaIntersectionFinder;
            public IIntersecableObject ExcludedObjektOnStartPoint;
            public MediaIntersectionPoint StartPoint;
            public IIntersectableEnvironmentLight EnvironmentLight;

            public static TestData CreateTestData(StartLocation start, BlockingObject blockingObject1, BlockingObject blockingObject2, GlobalMedia globalMedia, EnvironmentLight environmentLight, DistanceValues distanceValues)
            {
                List<IntersectableTestObject> testObjects = new List<IntersectableTestObject>();
                if (start == StartLocation.Surface) testObjects.Add(new IntersectableTestObject() { Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0, Material = BrdfModel.Diffus });//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                if (start == StartLocation.MediaParticle || start == StartLocation.MediaParticleNearBorder) testObjects.Add(new IntersectableTestObject() { Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.Diffus, RefractionIndex = 1.0f });//Air-Würfel mit X-Position = 0; Radius = 1; Mit Media
                testObjects.Add(CreateTestObject(blockingObject1, 3));
                testObjects.Add(CreateTestObject(blockingObject2, 6));
                testObjects = testObjects.Where(x => x != null).ToList(); //BlockingObject==Nothing is translated into null; Remove Null-Objects to avoid a NullException durring RayObject-Creation

                ParticipatingMediaMockData distanceSamplingData = null;
                switch (distanceValues)
                {
                    case DistanceValues.One:
                        distanceSamplingData = new ParticipatingMediaMockData()
                        {
                            ReturnValuesForDistanceSampling = new List<float>() { 1 }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                            ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { new Vector3D(2, 0, 0) },
                            ScatteringCoeffizient = 0.1f
                        };
                        break;

                    case DistanceValues.TwoOne:
                        distanceSamplingData = new ParticipatingMediaMockData()
                        {
                            ReturnValuesForDistanceSampling = new List<float>() { 2, 1 }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                            ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { new Vector3D(2, 0, 0), new Vector3D(5, 0, 0) },
                            ScatteringCoeffizient = 0.1f
                        };
                        break;

                    case DistanceValues.NearOneFromNearBorder:
                        distanceSamplingData = new ParticipatingMediaMockData()
                        {
                            ReturnValuesForDistanceSampling = new List<float>() { 0.000500023365f }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                            ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { new Vector3D(1 - MagicNumbers.MinAllowedPathPointDistance / 2, 0, 0), new Vector3D(5, 0, 0) },
                            ScatteringCoeffizient = 0.1f
                        };
                        break;

                    case DistanceValues.NearOne:
                        distanceSamplingData = new ParticipatingMediaMockData()
                        {
                            ReturnValuesForDistanceSampling = new List<float>() { 1 - MagicNumbers.MinAllowedPathPointDistance / 2 }, //SampleDistance = 0.999; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                            ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { new Vector3D(0, 0, 0) },
                            ScatteringCoeffizient = 0.1f
                        };
                        break;
                }


                var data = MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(testObjects, globalMedia == GlobalMedia.NoGlobalMedia ? 0 : 0.2f, distanceSamplingData);

                IIntersecableObject excludedObjektOnStartPoint = null;
                MediaIntersectionPoint mediaStartPoint = null;
                if (start == StartLocation.Surface)
                {
                    var startPoint = data.NoMediaIntersectionFinder.GetIntersectionPoint(new Ray(new Vector3D(0, 0, 0), new Vector3D(1, 0, 0)), 0);
                    excludedObjektOnStartPoint = startPoint.IntersectedObject;
                    mediaStartPoint = MediaIntersectionPoint.CreatePointOnLight(startPoint, data.MediaIntersectionFinder.GlobalParticipatingMediaFromScene);
                } if (start == StartLocation.MediaParticle)
                {
                    var mediaObjectCameraIsInside = data.MediaIntersectionFinder.GetMediaObjectPointIsInside(new Vector3D(0, 0, 0));
                    mediaStartPoint = MediaIntersectionPoint.CreateCameraPoint(new Vector3D(0, 0, 0), data.MediaIntersectionFinder.GlobalParticipatingMediaFromScene, mediaObjectCameraIsInside);
                }
                else if (start == StartLocation.MediaParticleNearBorder)
                {
                    var mediaObjectCameraIsInside = data.MediaIntersectionFinder.GetMediaObjectPointIsInside(new Vector3D(1 - MagicNumbers.MinAllowedPathPointDistance / 2, 0, 0));
                    mediaStartPoint = MediaIntersectionPoint.CreateCameraPoint(new Vector3D(1 - MagicNumbers.MinAllowedPathPointDistance / 2, 0, 0), data.MediaIntersectionFinder.GlobalParticipatingMediaFromScene, mediaObjectCameraIsInside);
                }
                else if (start == StartLocation.CameraOnYNearOne)
                {
                    mediaStartPoint = MediaIntersectionPoint.CreateCameraPoint(new Vector3D(1, 1 - MagicNumbers.MinAllowedPathPointDistance / 10000, 0), data.MediaIntersectionFinder.GlobalParticipatingMediaFromScene);
                }
                else
                {
                    mediaStartPoint = MediaIntersectionPoint.CreateCameraPoint(new Vector3D(1, 0, 0), data.MediaIntersectionFinder.GlobalParticipatingMediaFromScene);
                }


                return new TestData()
                {
                    MediaIntersectionFinder = data.MediaIntersectionFinder,
                    ExcludedObjektOnStartPoint = excludedObjektOnStartPoint,
                    StartPoint = mediaStartPoint,
                    EnvironmentLight = (environmentLight == CreateMediaLineNoAirBlockingTest.EnvironmentLight.No ? (IIntersectableEnvironmentLight)new LightsourcesWithoutEnvironmentLight() : new LightsourcesWithEnvironmentLight())
                };
            }

            private static IntersectableTestObject CreateTestObject(BlockingObject blocking, float xPos)
            {
                switch (blocking)
                {
                    case BlockingObject.Surface:
                        return new IntersectableTestObject() { Type = IntersectableTestObject.ObjectType.Cube, XPosition = xPos, Radius = 1, ScatteringCoeffizient = 0, Material = BrdfModel.Diffus };
                    case BlockingObject.GlasWithMedia:
                        return new IntersectableTestObject() { Type = IntersectableTestObject.ObjectType.Cube, XPosition = xPos, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.5f };
                    case BlockingObject.AirCube:
                        return new IntersectableTestObject() { Type = IntersectableTestObject.ObjectType.Cube, XPosition = xPos, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f };
                    case BlockingObject.AirSphere:
                        return new IntersectableTestObject() { Type = IntersectableTestObject.ObjectType.Sphere, XPosition = xPos, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f };
                    case BlockingObject.Nothing:
                        break;
                }
                return null;
            }
        }
    }
}
