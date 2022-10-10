using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using ParticipatingMediaTest.MediaMocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntersectionTestsTest.MediaIntersectionFinderTests
{
    //Hier wird die statische Methode 'MediaLine.CreateMediaShortLineNoAirBlocking' getestest
    //Ich wollte sie nicht mit in die MediaLineTest-Klasse mit reinnehmen, damit das dort nicht zu viel wird
    [TestClass]
    public class CreateMediaShortLineNoAirBlockingTest
    {
        //(Start von Partikel/Camera|Start von Surface)*(Bis Partikel | Bis Float-Max) * (Treffe Nix|Treffe Glas ohne Media|Treffe Glas mit Media|Treffe Luft-Würfel)*(Ohne GlobalMedia|Mit GlobalMedia) = 32 Kombinationen
        //Startpunkt (1,0,0) -> Rechte Kante von Würfel 1 (Wenn vorhanden)
        //Würfel 1 liegt bei X-Pos 0 (Radius 1)
        //Würfel 2 liegt bei X-Pos 3 (Radius 1) (BlockingObjekt)
        public enum StartLocation { Camera, Surface }
        public enum EndDistance { UpToParticle, FloatMax }
        public enum BlockingObject { Nothing, GlasNoMedia, GlasWithMedia, AirCube }
        public enum GlobalMedia { NoGlobalMedia, WithGlobalMedia }


        [DataRow(StartLocation.Camera, EndDistance.UpToParticle, BlockingObject.Nothing, GlobalMedia.NoGlobalMedia)]
        [DataRow(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.Nothing, GlobalMedia.NoGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.UpToParticle, BlockingObject.Nothing, GlobalMedia.NoGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.FloatMax, BlockingObject.Nothing, GlobalMedia.NoGlobalMedia)]
        [DataTestMethod]
        public void Group_CreateAirLine_ReturnsNull(StartLocation start, EndDistance distance, BlockingObject blockingObject, GlobalMedia globalMedia)
        {
            var p = CreateMediaAirLine(start, distance, blockingObject, globalMedia);
            Assert.IsNull(p);
        }

        [DataRow(StartLocation.Camera, EndDistance.UpToParticle, BlockingObject.Nothing, GlobalMedia.WithGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.UpToParticle, BlockingObject.Nothing, GlobalMedia.WithGlobalMedia)]
        [DataTestMethod]
        public void Group_CreateAirLine_ReturnsParticleInGlobalMedia(StartLocation start, EndDistance distance, BlockingObject blockingObject, GlobalMedia globalMedia)
        {
            var p = CreateMediaAirLine(start, distance, blockingObject, globalMedia);
            Assert.AreEqual(3, p.EndPoint.Position.X);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, p.EndPointLocation);
            Assert.AreEqual(0.2f, p.EndPoint.CurrentMedium.GetScatteringCoeffizient(p.EndPoint.Position).X);
            MediaLineCheck(p);
        }

        [DataRow(StartLocation.Camera, EndDistance.UpToParticle, BlockingObject.GlasNoMedia, GlobalMedia.NoGlobalMedia)]
        [DataRow(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.GlasNoMedia, GlobalMedia.NoGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.UpToParticle, BlockingObject.GlasNoMedia, GlobalMedia.NoGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.FloatMax, BlockingObject.GlasNoMedia, GlobalMedia.NoGlobalMedia)]
        [DataTestMethod]
        public void Group_CreateAirLine_ReturnsNoMediaSurface(StartLocation start, EndDistance distance, BlockingObject blockingObject, GlobalMedia globalMedia)
        {
            var p = CreateMediaAirLine(start, distance, blockingObject, globalMedia);
            Assert.AreEqual(2, p.EndPoint.Position.X);
            Assert.AreEqual(MediaPointLocationType.MediaBorder, p.EndPointLocation);
            Assert.AreEqual(0.0f, p.EndPoint.CurrentMedium.GetScatteringCoeffizient(p.EndPoint.Position).X);
            MediaLineCheck(p);
        }

        [DataRow(StartLocation.Camera, EndDistance.UpToParticle, BlockingObject.GlasNoMedia, GlobalMedia.WithGlobalMedia)]
        [DataRow(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.GlasNoMedia, GlobalMedia.WithGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.UpToParticle, BlockingObject.GlasNoMedia, GlobalMedia.WithGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.FloatMax, BlockingObject.GlasNoMedia, GlobalMedia.WithGlobalMedia)]
        [DataTestMethod]
        public void Group_CreateAirLine_ReturnsSurfaceInGlobalMedia(StartLocation start, EndDistance distance, BlockingObject blockingObject, GlobalMedia globalMedia)
        {
            var p = CreateMediaAirLine(start, distance, blockingObject, globalMedia);
            Assert.AreEqual(2, p.EndPoint.Position.X);
            Assert.AreEqual(MediaPointLocationType.MediaBorder, p.EndPointLocation);
            Assert.AreEqual(0.2f, p.EndPoint.CurrentMedium.GetScatteringCoeffizient(p.EndPoint.Position).X);
            MediaLineCheck(p);
        }

        [DataRow(StartLocation.Camera, EndDistance.UpToParticle, BlockingObject.GlasWithMedia, GlobalMedia.NoGlobalMedia)]
        [DataRow(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.GlasWithMedia, GlobalMedia.NoGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.UpToParticle, BlockingObject.GlasWithMedia, GlobalMedia.NoGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.FloatMax, BlockingObject.GlasWithMedia, GlobalMedia.NoGlobalMedia)]
        [DataTestMethod]
        public void Group_CreateAirLine_ReturnsLeftMediaBorderWithoutGlobalMedia(StartLocation start, EndDistance distance, BlockingObject blockingObject, GlobalMedia globalMedia)
        {
            var p = CreateMediaAirLine(start, distance, blockingObject, globalMedia);
            Assert.AreEqual(2, p.EndPoint.Position.X);
            Assert.AreEqual(MediaPointLocationType.MediaBorder, p.EndPointLocation);
            Assert.AreEqual(0.0f, p.EndPoint.CurrentMedium.GetScatteringCoeffizient(p.EndPoint.Position).X);
            MediaLineCheck(p);
        }

        [DataRow(StartLocation.Camera, EndDistance.UpToParticle, BlockingObject.GlasWithMedia, GlobalMedia.WithGlobalMedia)]
        [DataRow(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.GlasWithMedia, GlobalMedia.WithGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.UpToParticle, BlockingObject.GlasWithMedia, GlobalMedia.WithGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.FloatMax, BlockingObject.GlasWithMedia, GlobalMedia.WithGlobalMedia)]
        [DataTestMethod]
        public void Group_CreateAirLine_ReturnsLeftMediaBorderWithGlobalMedia(StartLocation start, EndDistance distance, BlockingObject blockingObject, GlobalMedia globalMedia)
        {
            var p = CreateMediaAirLine(start, distance, blockingObject, globalMedia);
            Assert.AreEqual(2, p.EndPoint.Position.X);
            Assert.AreEqual(MediaPointLocationType.MediaBorder, p.EndPointLocation);
            Assert.AreEqual(0.2f, p.EndPoint.CurrentMedium.GetScatteringCoeffizient(p.EndPoint.Position).X);
            MediaLineCheck(p);
        }

        [DataRow(StartLocation.Camera, EndDistance.UpToParticle, BlockingObject.AirCube, GlobalMedia.NoGlobalMedia)]
        [DataRow(StartLocation.Camera, EndDistance.UpToParticle, BlockingObject.AirCube, GlobalMedia.WithGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.UpToParticle, BlockingObject.AirCube, GlobalMedia.NoGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.UpToParticle, BlockingObject.AirCube, GlobalMedia.WithGlobalMedia)]
        [DataTestMethod]
        public void Group_CreateAirLine_ReturnsMediaParticleFromAirCube(StartLocation start, EndDistance distance, BlockingObject blockingObject, GlobalMedia globalMedia)
        {
            var p = CreateMediaAirLine(start, distance, blockingObject, globalMedia);
            Assert.AreEqual(3, p.EndPoint.Position.X);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, p.EndPointLocation);
            Assert.AreEqual(0.1f, p.EndPoint.CurrentMedium.GetScatteringCoeffizient(p.EndPoint.Position).X);
            MediaLineCheck(p);
        }

        [DataRow(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.Nothing, GlobalMedia.WithGlobalMedia)]
        [DataRow(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.AirCube, GlobalMedia.WithGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.FloatMax, BlockingObject.Nothing, GlobalMedia.WithGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.FloatMax, BlockingObject.AirCube, GlobalMedia.WithGlobalMedia)]
        [DataTestMethod]
        public void Group_CreateAirLine_ReturnsInfinityPoint(StartLocation start, EndDistance distance, BlockingObject blockingObject, GlobalMedia globalMedia)
        {
            var p = CreateMediaAirLine(start, distance, blockingObject, globalMedia);
            Assert.IsTrue(p.EndPoint.Position.X > 10000);
            Assert.AreEqual(MediaPointLocationType.MediaInfinity, p.EndPointLocation);
            Assert.AreEqual(0.2f, p.EndPoint.CurrentMedium.GetScatteringCoeffizient(p.EndPoint.Position).X);
            MediaLineCheck(p);
        }

        [DataRow(StartLocation.Camera, EndDistance.FloatMax, BlockingObject.AirCube, GlobalMedia.NoGlobalMedia)]
        [DataRow(StartLocation.Surface, EndDistance.FloatMax, BlockingObject.AirCube, GlobalMedia.NoGlobalMedia)]
        [DataTestMethod]
        public void Group_CreateAirLine_ReturnsRightMediaBorder(StartLocation start, EndDistance distance, BlockingObject blockingObject, GlobalMedia globalMedia)
        {
            var p = CreateMediaAirLine(start, distance, blockingObject, globalMedia);
            Assert.AreEqual(4, p.EndPoint.Position.X);
            Assert.AreEqual(MediaPointLocationType.NullMediaBorder, p.EndPointLocation);
            Assert.AreEqual(0.0f, p.EndPoint.CurrentMedium.GetScatteringCoeffizient(p.EndPoint.Position).X);
            MediaLineCheck(p);
        }

        [TestMethod]
        public void CreateAirLine_TwoAirCubesToParticleInSecondCube_ParticleIsVisible() //Visibleteset zwischen Kamera und Air-Würfel-Partikel, wo ein anderer Air-Wüfel dazwischen ist
        {
            var data = MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, RefractionIndex = 1 },//Air-Cube 1
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 3, Radius = 1, ScatteringCoeffizient = 0.1f, RefractionIndex = 1 } //Air-Cube 2
                },
                0); //Kein GlobalMedia

            var startPoint = MediaIntersectionPoint.CreateCameraPoint(new Vector3D(-2, 0.5f, 0), data.MediaIntersectionFinder.GlobalParticipatingMediaFromScene);
            var result = data.MediaIntersectionFinder.CreateMediaShortLineNoAirBlocking(startPoint, new Vector3D(1, 0, 0), 5, 0);

            Assert.AreEqual(MediaPointLocationType.Camera, result.StartPoint.Location);
            Assert.IsFalse(result.StartPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Startpunkt beginnt im Vacuum
            Assert.AreEqual(MediaPointLocationType.MediaParticle, result.EndPointLocation);
            Assert.AreEqual(4, result.Segments.Count);
            Assert.IsTrue(result.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium()); //Endpunkt endet im zweiten Würfel auf Partikel
            Assert.AreEqual(5, result.ShortRayLength);
            Assert.AreEqual(new Vector3D(3, 0.5f, 0), result.EndPoint.Position);
            //Assert.IsTrue(result.GoesAwayIntoInfinity);

            //Luft-Segment
            Assert.AreEqual(0, result.Segments[0].RayMin);
            Assert.AreEqual(1, result.Segments[0].RayMax);
            Assert.IsFalse(result.Segments[0].Media.HasScatteringSomeWhereInMedium());

            //Media-Segment 1
            Assert.AreEqual(1, result.Segments[1].RayMin);
            Assert.AreEqual(3, result.Segments[1].RayMax);
            Assert.IsTrue(result.Segments[1].Media.HasScatteringSomeWhereInMedium());

            //Luft-Segment
            Assert.AreEqual(3, result.Segments[2].RayMin);
            Assert.AreEqual(4, result.Segments[2].RayMax);
            Assert.IsFalse(result.Segments[2].Media.HasScatteringSomeWhereInMedium());

            //Media-Segment 2
            Assert.AreEqual(4, result.Segments[3].RayMin);
            Assert.AreEqual(5, result.Segments[3].RayMax);
            Assert.IsTrue(result.Segments[3].Media.HasScatteringSomeWhereInMedium());
        }

        private void MediaLineCheck(MediaLine line)
        {
            Assert.AreEqual(line.ShortRayLength, line.LongRayLength);
            for (int i = 1; i < line.Segments.Count; i++)
            {
                Assert.AreEqual(line.Segments[i].RayMin, line.Segments[i - 1].RayMax);
                Assert.IsTrue(line.Segments[i].RayMax > line.Segments[i].RayMin);
            }

            foreach (var s in line.Segments)
            {
                Assert.AreEqual(s.PoinOnRayMin.CurrentMedium, s.Media);
            }
        }

        [TestMethod]
        [Ignore] //Erst erstelle ich alle möglichen Testfälle die es gibt und trage per Hand die Assert-Anweisungen ein
        public void CreateAllMediaTestCases()
        {
            StringBuilder str = new StringBuilder();

            foreach (StartLocation start in (StartLocation[])Enum.GetValues(typeof(StartLocation)))
            {
                foreach (EndDistance distance in (EndDistance[])Enum.GetValues(typeof(EndDistance)))
                {
                    foreach (BlockingObject blockingObject in (BlockingObject[])Enum.GetValues(typeof(BlockingObject)))
                    {
                        foreach (GlobalMedia globalMedia in (GlobalMedia[])Enum.GetValues(typeof(GlobalMedia)))
                        {
                            str.AppendLine("[TestMethod]");
                            str.AppendLine($"public void CreateAirLine_{start}{distance}{blockingObject}{globalMedia}_Returns()");
                            str.AppendLine("{");
                            str.AppendLine($"\tvar p = CreateAirLine(StartLocation.{start}, EndDistance.{distance}, BlockingObject.{blockingObject}, GlobalMedia.{globalMedia});");
                            str.AppendLine("}");
                            str.AppendLine();
                        }
                    }
                }
            }

            //string text = str.ToString();
        }

        [TestMethod]
        [Ignore]    //Dann grupiere ich die Tests laut ihren Assert-Anweisungen und erstelle daraus DataRow-Tests
        public void GroupTestCasesByAssert()
        {
            List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
            string[] lines = File.ReadAllLines(@"..\..\..\IntersectionTestsTest\MediaIntersectionFinderTests\CreateMediaShortLineNoAirBlockingTest.cs");
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().StartsWith("public void CreateAirLine"))
                {
                    string key = lines[i + 2].Trim();
                    StringBuilder value = new StringBuilder();
                    for (int j = i + 3; lines[j].Trim() != "}"; j++) value.AppendLine(lines[j]);
                    pairs.Add(new KeyValuePair<string, string>(key, value.ToString()));
                }
            }
            var groups = pairs.GroupBy(x => x.Value);
            string text = string.Join("-----\n", groups.Select(x => string.Join("\n", x.Select(y => y.Key.Replace("var p = CreateAirLine", "[DataRow").Replace(");", ")]"))) + "\n" + x.Key));
        }

        private MediaLine CreateMediaAirLine(StartLocation start, EndDistance distance, BlockingObject blockingObject, GlobalMedia globalMedia)
        {
            var data = CreateTestData(start, blockingObject, globalMedia);
            return data.MediaIntersectionFinder.CreateMediaShortLineNoAirBlocking(data.StartPoint, new Vector3D(1, 0, 0), distance == EndDistance.FloatMax ? float.MaxValue : 2, 0);
        }

        class TestData
        {
            public MediaIntersectionFinder MediaIntersectionFinder;
            public IIntersecableObject ExcludedObjektOnStartPoint;
            public MediaIntersectionPoint StartPoint;
        }

        private TestData CreateTestData(StartLocation start, BlockingObject blockingObject, GlobalMedia globalMedia)
        {
            List<IntersectableTestObject> testObjects = new List<IntersectableTestObject>();
            if (start == StartLocation.Surface) testObjects.Add(new IntersectableTestObject() { Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0, Material = BrdfModel.Diffus });//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
            switch (blockingObject)
            {
                case BlockingObject.GlasNoMedia:
                    testObjects.Add(new IntersectableTestObject() { Type = IntersectableTestObject.ObjectType.Cube, XPosition = 3, Radius = 1, ScatteringCoeffizient = 0, Material = BrdfModel.TextureGlass, RefractionIndex = 1.5f });
                    break;
                case BlockingObject.GlasWithMedia:
                    testObjects.Add(new IntersectableTestObject() { Type = IntersectableTestObject.ObjectType.Cube, XPosition = 3, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.5f });
                    break;
                case BlockingObject.AirCube:
                    testObjects.Add(new IntersectableTestObject() { Type = IntersectableTestObject.ObjectType.Cube, XPosition = 3, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f });
                    break;
                case BlockingObject.Nothing:
                    break;
            }

            //var data = withMedia ? MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(testObjects, globalMedia == GlobalMedia.NoGlobalMedia ? 0 : 0.2f) : MediaIntersectionFinderTestHelper.CreateIntersectionDataNoMedia(testObjects);
            var data = MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(testObjects, globalMedia == GlobalMedia.NoGlobalMedia ? 0 : 0.2f);

            IIntersecableObject excludedObjektOnStartPoint = null;
            MediaIntersectionPoint mediaStartPoint;
            if (start == StartLocation.Surface)
            {
                var startPoint = data.NoMediaIntersectionFinder.GetIntersectionPoint(new Ray(new Vector3D(0, 0, 0), new Vector3D(1, 0, 0)), 0);
                excludedObjektOnStartPoint = startPoint.IntersectedObject;
                mediaStartPoint = MediaIntersectionPoint.CreatePointOnLight(startPoint, data.MediaIntersectionFinder.GlobalParticipatingMediaFromScene);
            }
            else
            {
                mediaStartPoint = MediaIntersectionPoint.CreateCameraPoint(new Vector3D(1, 0, 0), data.MediaIntersectionFinder.GlobalParticipatingMediaFromScene);
            }


            return new TestData()
            {
                MediaIntersectionFinder = data.MediaIntersectionFinder,
                ExcludedObjektOnStartPoint = excludedObjektOnStartPoint,
                StartPoint = mediaStartPoint
            };
        }
    }
}
