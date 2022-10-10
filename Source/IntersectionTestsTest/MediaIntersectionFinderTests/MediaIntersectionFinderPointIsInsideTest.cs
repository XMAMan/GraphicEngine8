using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMediaTest.MediaMocks;
using System.Collections.Generic;

namespace IntersectionTestsTest.MediaIntersectionFinderTests
{
    //Kamera steht 1 m über der Erde. Somit erwarte ich, dass sie in der Luft liegt und Media != null
    //Kamera steht außerhalb der Atmosphärenkugel. Somit erwarte ich, dass sie im Weltraum/Vacuum liegt
    //Kamera steht innerhalb eines Media-Würfels. Erwartung: Media-Würfel-Objekt wird zurück gegeben
    //Kamera steht außerhalb von Media-Würfeln. Erwartung: Es wird null zurück gegeben

    [TestClass]
    public class MediaIntersectionFinderPointIsInsideTest
    {
        [TestMethod]
        public void GetMediaObjectPointIsInside_CalledInsideFromMediaSphere_MediaSphereIsReturned() //Kamera steht 1 m über der Erde. Somit erwarte ich, dass sie in der Luft liegt und Media != null
        {
            var intersectionFinder = MediaIntersectionFinderTestHelper.CreateSphereRowOnXAxis(new List<Vector3D>() {
                new Vector3D(0, 1, 0),        //Erde mit X-Position = 0; Radius = 1; Ohne Media
                new Vector3D(0, 10, 0.1f)     //Himmel mit X-Position = 0; Radius = 10; Media mit ScatterCoeffizient = 0.1
            }, 0).MediaIntersectionFinder;

            Vector3D startPoint = new Vector3D(1.1f, 0, 0);
            var rayHeighSphere = intersectionFinder.GetMediaObjectPointIsInside(startPoint);

            Assert.IsNotNull(rayHeighSphere);
            Assert.IsNotNull(rayHeighSphere.Media);
        }

        [TestMethod]
        public void GetMediaObjectPointIsInside_CalledOutsideFromMediaSphere_NoMediaObjectIsReturned() //Kamera steht außerhalb der Atmosphärenkugel. Somit erwarte ich, dass sie im Weltraum/Vacuum liegt
        {
            var intersectionFinder = MediaIntersectionFinderTestHelper.CreateSphereRowOnXAxis(new List<Vector3D>() {
                new Vector3D(0, 1, 0),        //Erde mit X-Position = 0; Radius = 1; Ohne Media
                new Vector3D(0, 10, 0.1f)     //Himmel mit X-Position = 0; Radius = 10; Media mit ScatterCoeffizient = 0.1
            }, 0).MediaIntersectionFinder;

            Vector3D startPoint = new Vector3D(11, 0, 0);
            var rayHeighSphere = intersectionFinder.GetMediaObjectPointIsInside(startPoint);

            Assert.IsNull(rayHeighSphere);
        }

        [TestMethod]
        public void GetMediaObjectPointIsInside_CalledInsideFromMediaCube_MediaCubeIsReturned() //Kamera steht innerhalb eines Media-Würfels. Erwartung: Media-Würfel-Objekt wird zurück gegeben
        {
            var intersectionFinder = MediaIntersectionFinderTestHelper.CreateCubeRowOnXAxis(new List<Vector3D>() {
                new Vector3D(0, 1, 0.1f),        //Media-Würfel mit X-Position = 0; Radius = 1; Mit Media
                new Vector3D(3, 1, 0.1f),        //Media-Würfel mit X-Position = 3; Radius = 1; Mit Media
            }, 0).MediaIntersectionFinder;

            Vector3D startPoint = new Vector3D(2.1f, 0, 0);
            var rayHeighCube = intersectionFinder.GetMediaObjectPointIsInside(startPoint);

            Assert.IsNotNull(rayHeighCube);
            Assert.IsNotNull(rayHeighCube.Media);
        }

        [TestMethod]
        public void GetMediaObjectPointIsInside_CalledOutsideFromMediaCubes_NoMediaCubeIsReturned() //Kamera steht außerhalb von Media-Würfeln. Erwartung: Es wird null zurück gegeben
        {
            var intersectionFinder = MediaIntersectionFinderTestHelper.CreateCubeRowOnXAxis(new List<Vector3D>() {
                new Vector3D(0, 1, 0.1f),        //Media-Würfel mit X-Position = 0; Radius = 1; Mit Media
                new Vector3D(3, 1, 0.1f),        //Media-Würfel mit X-Position = 3; Radius = 1; Mit Media
            }, 0).MediaIntersectionFinder;

            Vector3D startPoint = new Vector3D(1.1f, 0, 0);
            var rayHeighCube = intersectionFinder.GetMediaObjectPointIsInside(startPoint);

            Assert.IsNull(rayHeighCube);
        }
    }
}
