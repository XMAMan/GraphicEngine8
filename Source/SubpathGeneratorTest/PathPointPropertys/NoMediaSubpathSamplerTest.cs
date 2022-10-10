using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using ParticipatingMedia.PhaseFunctions;
using ParticipatingMediaTest.MediaMocks;
using RayCameraNamespace;
using RayTracerGlobal;
using RaytracingBrdf;
using RaytracingLightSource;
using SubpathGenerator;
using SubpathGenerator.SubPathSampler;
using SubpathGeneratorTest.PathPointPropertys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RayCameraNamespace.RayCamera;

namespace SubpathGeneratorTest.PathPointPropertys
{
    //1 Ohne Media. Kamera befindet sich in Würfel und schaut gegen Wand, wo Strahl erst reflektiert und dann absorbiert wird
    //2 Ohne Media. Kamera schaut auf Solid-Würfel. Strahl wird daran reflektiert und fliegt ins Leere
    //3 Strahl trift Glas-Würfel und durchläuft ihn (Zwei mal gebrochen)
    //4 Strahl trift Glas-Kugel und durchläuft sie (Zwei mal gebrochen)
    //5 Strahl befindet sich in Glas-Kugel und schaut auf Rand, wo Strahl gebrochen wird
    //6 Ohne Media. Kamera befindet sich in Diffuse-Kugel und schaut gegen Wand, wo Strahl diffuse flach reflektiert wird, was dazu führt, dass Strahl Kugel verläßt (Hintergrund des Tests: Bei der Wolkenszene ist Strahl von Kamera auf Baumstamm gelandet und war damit innerhalb der Kugel. Ab den Momenten darf der IntersectionFinder nicht denken, dass Strahl nun gebrochen wurde und er anderen Schnittpunkt finden muss
    //7 Ohne Media. Kamera befindet sich in Diffuse-Kugel und schaut gegen Wand, wo Strahl diffuse richtung Normale reflektiert wird und andere Kugelwand trifft

    [TestClass]
    public class NoMediaSubpathSamplerTest
    {
        [TestMethod]
        public void SampleCameraPoints_HitWillAndReflect_CDD() //1 Ohne Media. Kamera befindet sich in Würfel und schaut gegen Wand, wo Strahl erst reflektiert und dann absorbiert wird
        {
            Vector3D startPoint = new Vector3D(0, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataNoMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0 },//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                }), 
                SubpathGenerator.PathSamplingType.NoMedia,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(1, 0, 0) , ReturnValueForDirectionSampling = new Vector3D(-1,0,0) },
                    new BrdfMockData(){ExpectedLocation = new Vector3D(-1, 0, 0) , ReturnValueForDirectionSampling = null },
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(3, points.Length);
            SubpathTestHelper.CheckCameraNoMediaPoint(points[0], startPoint);
            SubpathTestHelper.CheckSurfacePointWithoutMedia(points[1], new Vector3D(+1, 0, 0), new Vector3D(-1, 0, 0), 1);
            SubpathTestHelper.CheckSurfacePointWithoutMedia(points[2], new Vector3D(-1, 0, 0), new Vector3D(+1, 0, 0), 2);
        }

        [TestMethod]
        public void SampleCameraPoints_HitWillAndReflect_CD() //2  Ohne Media. Kamera schaut auf Solid-Würfel. Strahl wird daran reflektiert und fliegt ins Leere
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataNoMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0 },//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                }),
                SubpathGenerator.PathSamplingType.NoMedia,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(-1, 0, 0) , ReturnValueForDirectionSampling = new Vector3D(-1,0,0) },
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraNoMediaPoint(points[0], startPoint);
            SubpathTestHelper.CheckSurfacePointWithoutMedia(points[1], new Vector3D(-1, 0, 0), new Vector3D(-1, 0, 0), 1);
        }

        [TestMethod]
        public void SampleCameraPoints_GoThroughCenterFromGlasCube_ReturnCSS() //3 Strahl trift Glas-Würfel und durchläuft ihn (Zwei mal gebrochen)
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataNoMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.0f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.5f}//Glas-Kugel
                }),
                SubpathGenerator.PathSamplingType.NoMedia,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(-1, 0, 0) , ReturnValueForDirectionSampling = new Vector3D(+1,0,0), ExpectedRefractionIndexRaysComesFrom = 1, ExpectedRefrationIndexRaysGoesInto = 1.5f },
                    new BrdfMockData(){ExpectedLocation = new Vector3D(+1, 0, 0) , ReturnValueForDirectionSampling = new Vector3D(+1,0,0), ExpectedRefractionIndexRaysComesFrom = 1.5f, ExpectedRefrationIndexRaysGoesInto = 1.0f },
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(3, points.Length);
            SubpathTestHelper.CheckCameraNoMediaPoint(points[0], startPoint);
            SubpathTestHelper.CheckSpecurlarPointWithoutMedia(points[1], new Vector3D(-1, 0, 0), new Vector3D(-1, 0, 0), 1);
            SubpathTestHelper.CheckSpecurlarPointWithoutMedia(points[2], new Vector3D(+1, 0, 0), new Vector3D(-1, 0, 0), 2);
        }

        [TestMethod]
        public void SampleCameraPoints_GoThroughCenterFromGlasSphere_ReturnCSS() //4 Strahl trift Glas-Kugel und durchläuft sie (Zwei mal gebrochen)
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataNoMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Sphere, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.0f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.5f}//Glas-Kugel
                }),
                SubpathGenerator.PathSamplingType.NoMedia,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(-1, 0, 0) , ReturnValueForDirectionSampling = new Vector3D(+1,0,0), ExpectedRefractionIndexRaysComesFrom = 1, ExpectedRefrationIndexRaysGoesInto = 1.5f },
                    new BrdfMockData(){ExpectedLocation = new Vector3D(+1, 0, 0) , ReturnValueForDirectionSampling = new Vector3D(+1,0,0), ExpectedRefractionIndexRaysComesFrom = 1.5f, ExpectedRefrationIndexRaysGoesInto = 1.0f },
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(3, points.Length);
            SubpathTestHelper.CheckCameraNoMediaPoint(points[0], startPoint);
            SubpathTestHelper.CheckSpecurlarPointWithoutMedia(points[1], new Vector3D(-1, 0, 0), new Vector3D(-1, 0, 0), 1);
            SubpathTestHelper.CheckSpecurlarPointWithoutMedia(points[2], new Vector3D(+1, 0, 0), new Vector3D(-1, 0, 0), 2);
        }

        [TestMethod]
        public void SampleCameraPoints_LeafeGlasSphere_ReturnCS() //5 Strahl befindet sich in Glas-Kugel und schaut auf Rand, wo Strahl gebrochen wird
        {
            Vector3D startPoint = new Vector3D(0, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataNoMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Sphere, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.0f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.5f}//Glas-Kugel
                }),
                SubpathGenerator.PathSamplingType.NoMedia,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(+1, 0, 0) , ReturnValueForDirectionSampling = new Vector3D(+1,0,0), ExpectedRefractionIndexRaysComesFrom = 1, ExpectedRefrationIndexRaysGoesInto = 1.5f },
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraNoMediaPoint(points[0], startPoint);
            SubpathTestHelper.CheckSpecurlarPointWithoutMedia(points[1], new Vector3D(+1, 0, 0), new Vector3D(-1, 0, 0), 1);
        }

        [TestMethod]
        public void SampleCameraPoints_StartInsideFromDiffuseSphereAndLeafeIt_CD() //5 Ohne Media. Kamera befindet sich in Kugel und schaut gegen Wand, wo Strahl diffuse flach reflektiert wird, was dazu führt, dass Strahl Kugel verläßt (Hintergrund des Tests: Bei der Wolkenszene ist Strahl von Kamera auf Baumstamm gelandet und war damit innerhalb der Kugel. Ab den Momenten darf der IntersectionFinder nicht denken, dass Strahl nun gebrochen wurde und er anderen Schnittpunkt finden muss
        {
            Vector3D startPoint = new Vector3D(0, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataNoMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Sphere, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0 },//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                }),
                SubpathGenerator.PathSamplingType.NoMedia,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(1, 0, 0) , ReturnValueForDirectionSampling = new Vector3D(0,1,0) },
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraNoMediaPoint(points[0], startPoint);
            SubpathTestHelper.CheckSurfacePointWithoutMedia(points[1], new Vector3D(+1, 0, 0), new Vector3D(-1, 0, 0), 1);
        }

        [TestMethod]
        public void SampleCameraPoints_StartInsideFromDiffuseSphereAndReflect_CDD() //6 Ohne Media. Kamera befindet sich in Kugel und schaut gegen Wand, wo Strahl diffuse richtung Normale reflektiert wird und andere Kugelwand trifft (Hintergrund des Tests: Bei der Wolkenszene ist Strahl von Kamera auf Baumstamm gelandet und war damit innerhalb der Kugel. Ab den Momenten darf der IntersectionFinder nicht denken, dass Strahl nun gebrochen wurde und er anderen Schnittpunkt finden muss
        {
            Vector3D startPoint = new Vector3D(0, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataNoMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Sphere, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0 },//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                }),
                SubpathGenerator.PathSamplingType.NoMedia,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(1, 0, 0) , ReturnValueForDirectionSampling = new Vector3D(-1,0,0) },
                    new BrdfMockData(){ExpectedLocation = new Vector3D(-1, 0, 0) , ReturnValueForDirectionSampling = null },
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(3, points.Length);
            SubpathTestHelper.CheckCameraNoMediaPoint(points[0], startPoint);
            SubpathTestHelper.CheckSurfacePointWithoutMedia(points[1], new Vector3D(+1, 0, 0), new Vector3D(-1, 0, 0), 1);
            SubpathTestHelper.CheckSurfacePointWithoutMedia(points[2], new Vector3D(-1, 0, 0), new Vector3D(+1, 0, 0), 2);
        }
    }
}
