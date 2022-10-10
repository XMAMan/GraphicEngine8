using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RayObjects;
using RayObjects.RayObjects;
using System.Collections.Generic;
using System.Linq;
using TriangleObjectGeneration;

//Tests die früher mal wegen Fehlern im KDBaum oder IntersectableTriangle umgefallen sind und nun gefixt sind
//-GraphicPanelsTest.PhotonmapDirectPixelTest.DirectLightPhotons
//-GraphicPanelsTest.RasterizerImageTest.TextureMapping
//-MediaPathtracingTest.C_PathContributionSumForEachPathLengthCheck
//-IntersectionTests.MediaIntersectionFinder.CreateAirLine_TwoAirCubesToParticleInSecondCube_ParticleIsVisible
//-IntersectionTests.MediaIntersectionFinder.Group_CreateAirLine_ReturnsMediaParticleFromAirCube
//-GraphicPanels.PhotonmapDirectPixel.DirectLightPhotons
//-GraphicPanels.PhotonmapDirectPixel.ParticlePhotons
//-GraphicPanels.PhotonmapDirectPixel.ShowGodRays
//-GraphicPanels.PixelConvergence.CheckPixelColor_BidirectionalPathTracing_Cornellbox_GlassSphereLightFlackOverRectangle
//-GraphicPanels.PixelConvergence.CheckPixelColor_BidirectionalPathTracing_NoWindowRoom_Cupboard
//-GraphicPanels.PixelConvergence.CheckPixelColor_PathTracer_NoWindowRoom_Cupboard
//-GraphicPanels.PixelConvergence.CheckPixelColor_PathTracer_NoWindowRoom_GreenSphere
//-ToolsTest.CreateSceneBatFilesTest.08_WindowRoom
//-RasterizerImageTest.MirrorSphere

//Tests die aktuell umfallen wenn ich den KDTree gegen den BIH im IntersectionFinder vergleiche ohne das ich BoundingBox.IsPointInside bei IntersectableTriangel nutze
//(Es müsste IntersectableTriangleTest.GetSimpleIntersectionPoint_CalledForXLeftSideRay_NoIntersectionFound gefixt werden, damit GetIntersectionPoint_CalledForTriangleAndRaySphere_VertexPropertysAreEqual geht was aber vermutlich nicht ohne BoundingBox-Test möglich ist)
//-IntersectionTest.IntersectionFinderTest.GetIntersectionPoint_CalledForTriangleAndRaySphere_VertexPropertysAreEqual -> Dieser Test um, da IntersectableTriangle auch dann noch Schnittpunkte findet, wenn dieser Außerhalb seiner Boundingbox liegen. Man kann den Test grün machen, wenn man ein BoundingBox-Test mit im IntersectableTriangel macht
//-GraphicPanels.PixelConvergence.CheckPixelColor_Mirrorballs -> Dieser Test wird selbst dann noch rot, wenn ich die BoundingBox-Test-Erweiterung im IntersectableTriangle mache. Das gilt für den Flipcode- als auch 4-Planes-Test
//-GraphicPanels.PixelConvergence.CheckPixelColor_BidirectionalPathTracing_Cornellbox_GlassSphereLightFlackOverRectangle
//-RaytracingImageTest.RenderAllScenes
//-RaytracingImageTest.MasterTestImage
//-CreateScenesBatFilesTest.16_Graphic6Memories
//-CreateScenesBatFilesTest.05_WaterCornellbox
//-CreateScenesBatFilesTest.02NoWindowRoom_Radiosity
//-CreateScenesBatFilesTest.11_PillarsOffice
//-CreateScenesBatFilesTest.20_Mirrorballs

//Hinweis warum ohne BoundingBox-Test im Dreieck KD-Baum was anderes zeigt als BIH:
//Der Barycentric-Test/4-Planes-Test findet eher ein Schnittpunkt als der Ray-BoundingBox-Test
//Beim KD-Baum nutze ich indirekt den BoundingBox-Test und beim LinearSearch mache ich das nicht. BIH nutzt ein ungenauen BBOX-Test.
//Wenn ein Strahl ein Dreieck am Rand trifft, der leicht über das Dreieck hinaus ragt, dann wird der 
//Barycentric-Test true liefern und der BoundingBox-Test false. Deswegen findet der KD-Baum-Test scheinbar
//nicht alle Schnittpunkte obwohl er eigentlich perfekter arbeitet.
//Wenn man das Ausgabebild von IntersectionFinderTest.GetIntersectionPoint_CalledForTriangleAndRaySphere_VertexPropertysAreEqual
//von der LinearSearch gegen den KD-Baum vergleicht, dann sieht man für einzelne wenige Pixel ein Unterschied. Diese Pixel
//leiden unter den numberischen Ungenauigkeitsproblem des Barycentric-Test/4-Planes-Test
//Achtung: BoundingBox.IsPointInside-Test innerhalb von IntersectableTriangel führt zu Streuselfehler bei WindowRoom!


//Tests die aktuell umfallen wenn ich den KDTree gegen den BIH im IntersectionFinder vergleiche und dabei BoundingBox.IsPointInside bei IntersectableTriangel nutze
//Achtung: Der BoundingBox-Test beim Dreieck führt zu Streuselfehlern (Siehe Radiosity-WindowRoom). Das ist also kein praktikable Lösung
//-CreateScenesBatFilesTest.02NoWindowRoom_Radiosity
//-CreateScenesBatFilesTest.08_WindowRoom
//-GraphicPanels.PixelConvergence.CheckPixelColor_Mirrorballs 
//-RaytracingImageTest.RenderAllScenes
//-RaytracingImageTest.MasterTestImage

//-BIH ist nur scheinbar besser als KD-Baum beim finden von Schnittpunkten, da es die BoundingBox nicht so eng macht. 
// Eigentlich ist der KD-Baum also besser und der eigentlich Fehler liegt in Ungenauigkeitsproblemen in der IntersectableTriangle-Klasse

namespace IntersectionTestsTest
{
    [TestClass]
    public class KDSahTreeTest
    {
        //private static string DataDirectory = UnitTestHelper.FilePaths.DataDirectory;

        //Achtung: Bei diesen Test ist mir aufgefallen, das BIH-Blattknoten leere RayObjekteListe enthalten. Solche Blattknoten können weg
        [TestMethod]
        public void GetIntersectionPoint_CalledForSpezialRay1_ReturnsIntersectionPoint()
        {
            TriangleObject data = TriangleObjectGenerator.CreateSquareXY(10, 10, 2);
            var quad = new DrawingObject(data, new ObjectPropertys() { Orientation = new Vector3D(-90, 0, 180), Size = 8, ShowFromTwoSides = true });
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { quad });

            KDSahTree sut = new KDSahTree(rayObjects.Cast<IIntersecableObject>().ToList(), (s, f) => { }); 
            BoundingIntervallHierarchy bih = new BoundingIntervallHierarchy(rayObjects.Cast<IIntersecableObject>().ToList(), (s, f) => { });

            var sutPoint = sut.GetIntersectionPoint(new Ray(new Vector3D(0, 70, 100), new Vector3D(-0.215718955f, -0.413377285f, -0.884638071f)), null, float.MaxValue, 0);
            var bihPoint = bih.GetIntersectionPoint(new Ray(new Vector3D(0, 70, 100), new Vector3D(-0.215718955f, -0.413377285f, -0.884638071f)), null, float.MaxValue, 0);
            Assert.IsNotNull(bihPoint);
            Assert.IsNotNull(sutPoint, "Kd-Baum zeigt Schnittpunkt nicht");
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForSpezialRay2_ReturnsIntersectionPoint()
        {
            IIntersectableRayDrawingObject rayHigh = new RayDrawingObject(new ObjectPropertys() { Name = "Mario", Position = new Vector3D(+30, 13, 3), Orientation = new Vector3D(0, 70 + 90, 0), Size = 0.5f, ShowFromTwoSides = false, }, null, null);

            List<RayTriangle> list = new List<RayTriangle>();
            #region AddToList
            list.Add(new RayTriangle(new Triangle(new Vector3D(-19f, -16f, -2f), new Vector3D(-19f, -3f, -2f), new Vector3D(19f, -3f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, -3f, -2f), new Vector3D(19f, -16f, -2f), new Vector3D(-19f, -16f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, -16f, 2f), new Vector3D(19f, -3f, 2f), new Vector3D(-19f, -3f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-19f, -3f, 2f), new Vector3D(-19f, -16f, 2f), new Vector3D(19f, -16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, -16f, -2f), new Vector3D(19f, -3f, -2f), new Vector3D(19f, -3f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, -3f, 2f), new Vector3D(19f, -16f, 2f), new Vector3D(19f, -16f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-19f, -16f, 2f), new Vector3D(-19f, -3f, 2f), new Vector3D(-19f, -3f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-19f, -3f, -2f), new Vector3D(-19f, -16f, -2f), new Vector3D(-19f, -16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-19f, -16f, 2f), new Vector3D(-19f, -16f, -2f), new Vector3D(-13f, -16f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, -16f, -2f), new Vector3D(-13f, -16f, 2f), new Vector3D(-19f, -16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-3f, -16f, 2f), new Vector3D(-3f, -16f, -2f), new Vector3D(3f, -16f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(3f, -16f, -2f), new Vector3D(3f, -16f, 2f), new Vector3D(-3f, -16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(13f, -16f, 2f), new Vector3D(13f, -16f, -2f), new Vector3D(19f, -16f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, -16f, -2f), new Vector3D(19f, -16f, 2f), new Vector3D(13f, -16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-19f, -3f, -2f), new Vector3D(-19f, -3f, 2f), new Vector3D(-16f, -3f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, -3f, 2f), new Vector3D(-16f, -3f, -2f), new Vector3D(-19f, -3f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, -3f, -2f), new Vector3D(16f, -3f, 2f), new Vector3D(19f, -3f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, -3f, 2f), new Vector3D(19f, -3f, -2f), new Vector3D(16f, -3f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 18f, -2f), new Vector3D(-13f, 21f, -2f), new Vector3D(16f, 21f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 21f, -2f), new Vector3D(16f, 18f, -2f), new Vector3D(-13f, 18f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 18f, 2f), new Vector3D(16f, 21f, 2f), new Vector3D(-13f, 21f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 21f, 2f), new Vector3D(-13f, 18f, 2f), new Vector3D(16f, 18f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 18f, -2f), new Vector3D(16f, 21f, -2f), new Vector3D(16f, 21f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 21f, 2f), new Vector3D(16f, 18f, 2f), new Vector3D(16f, 18f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 18f, 2f), new Vector3D(-13f, 21f, 2f), new Vector3D(-13f, 21f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 21f, -2f), new Vector3D(-13f, 18f, -2f), new Vector3D(-13f, 18f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(10f, 18f, 2f), new Vector3D(10f, 18f, -2f), new Vector3D(16f, 18f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 18f, -2f), new Vector3D(16f, 18f, 2f), new Vector3D(10f, 18f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 21f, -2f), new Vector3D(-13f, 21f, 2f), new Vector3D(-12f, 21f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-12f, 21f, 2f), new Vector3D(-12f, 21f, -2f), new Vector3D(-13f, 21f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 9f, -2f), new Vector3D(-16f, 13f, -2f), new Vector3D(19f, 13f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, 13f, -2f), new Vector3D(19f, 9f, -2f), new Vector3D(-16f, 9f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, 9f, 2f), new Vector3D(19f, 13f, 2f), new Vector3D(-16f, 13f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 13f, 2f), new Vector3D(-16f, 9f, 2f), new Vector3D(19f, 9f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, 9f, -2f), new Vector3D(19f, 13f, -2f), new Vector3D(19f, 13f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, 13f, 2f), new Vector3D(19f, 9f, 2f), new Vector3D(19f, 9f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 9f, 2f), new Vector3D(-16f, 13f, 2f), new Vector3D(-16f, 13f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 13f, -2f), new Vector3D(-16f, 9f, -2f), new Vector3D(-16f, 9f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 9f, 2f), new Vector3D(16f, 9f, -2f), new Vector3D(19f, 9f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, 9f, -2f), new Vector3D(19f, 9f, 2f), new Vector3D(16f, 9f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 13f, -2f), new Vector3D(16f, 13f, 2f), new Vector3D(19f, 13f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, 13f, 2f), new Vector3D(19f, 13f, -2f), new Vector3D(16f, 13f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 6f, -2f), new Vector3D(-16f, 9f, -2f), new Vector3D(16f, 9f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 9f, -2f), new Vector3D(16f, 6f, -2f), new Vector3D(-16f, 6f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 6f, 2f), new Vector3D(16f, 9f, 2f), new Vector3D(-16f, 9f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 9f, 2f), new Vector3D(-16f, 6f, 2f), new Vector3D(16f, 6f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 6f, -2f), new Vector3D(16f, 9f, -2f), new Vector3D(16f, 9f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 9f, 2f), new Vector3D(16f, 6f, 2f), new Vector3D(16f, 6f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 6f, 2f), new Vector3D(-16f, 9f, 2f), new Vector3D(-16f, 9f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 9f, -2f), new Vector3D(-16f, 6f, -2f), new Vector3D(-16f, 6f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 6f, 2f), new Vector3D(-16f, 6f, -2f), new Vector3D(-10f, 6f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-10f, 6f, -2f), new Vector3D(-10f, 6f, 2f), new Vector3D(-16f, 6f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(13f, 6f, 2f), new Vector3D(13f, 6f, -2f), new Vector3D(16f, 6f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 6f, -2f), new Vector3D(16f, 6f, 2f), new Vector3D(13f, 6f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-10f, 3f, -2f), new Vector3D(-10f, 6f, -2f), new Vector3D(13f, 6f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(13f, 6f, -2f), new Vector3D(13f, 3f, -2f), new Vector3D(-10f, 3f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(13f, 3f, 2f), new Vector3D(13f, 6f, 2f), new Vector3D(-10f, 6f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-10f, 6f, 2f), new Vector3D(-10f, 3f, 2f), new Vector3D(13f, 3f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(13f, 3f, -2f), new Vector3D(13f, 6f, -2f), new Vector3D(13f, 6f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(13f, 6f, 2f), new Vector3D(13f, 3f, 2f), new Vector3D(13f, 3f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-10f, 3f, 2f), new Vector3D(-10f, 6f, 2f), new Vector3D(-10f, 6f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-10f, 6f, -2f), new Vector3D(-10f, 3f, -2f), new Vector3D(-10f, 3f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 3f, 2f), new Vector3D(6f, 3f, -2f), new Vector3D(13f, 3f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(13f, 3f, -2f), new Vector3D(13f, 3f, 2f), new Vector3D(6f, 3f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 0f, -2f), new Vector3D(-13f, 3f, -2f), new Vector3D(6f, 3f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 3f, -2f), new Vector3D(6f, 0f, -2f), new Vector3D(-13f, 0f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 0f, 2f), new Vector3D(6f, 3f, 2f), new Vector3D(-13f, 3f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 3f, 2f), new Vector3D(-13f, 0f, 2f), new Vector3D(6f, 0f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 0f, -2f), new Vector3D(6f, 3f, -2f), new Vector3D(6f, 3f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 3f, 2f), new Vector3D(6f, 0f, 2f), new Vector3D(6f, 0f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 0f, 2f), new Vector3D(-13f, 3f, 2f), new Vector3D(-13f, 3f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 3f, -2f), new Vector3D(-13f, 0f, -2f), new Vector3D(-13f, 0f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 3f, -2f), new Vector3D(-13f, 3f, 2f), new Vector3D(-10f, 3f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-10f, 3f, 2f), new Vector3D(-10f, 3f, -2f), new Vector3D(-13f, 3f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, -3f, -2f), new Vector3D(-16f, 0f, -2f), new Vector3D(16f, 0f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 0f, -2f), new Vector3D(16f, -3f, -2f), new Vector3D(-16f, -3f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, -3f, 2f), new Vector3D(16f, 0f, 2f), new Vector3D(-16f, 0f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 0f, 2f), new Vector3D(-16f, -3f, 2f), new Vector3D(16f, -3f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, -3f, -2f), new Vector3D(16f, 0f, -2f), new Vector3D(16f, 0f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 0f, 2f), new Vector3D(16f, -3f, 2f), new Vector3D(16f, -3f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, -3f, 2f), new Vector3D(-16f, 0f, 2f), new Vector3D(-16f, 0f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 0f, -2f), new Vector3D(-16f, -3f, -2f), new Vector3D(-16f, -3f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 0f, -2f), new Vector3D(-16f, 0f, 2f), new Vector3D(-13f, 0f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 0f, 2f), new Vector3D(-13f, 0f, -2f), new Vector3D(-16f, 0f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 0f, -2f), new Vector3D(6f, 0f, 2f), new Vector3D(16f, 0f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 0f, 2f), new Vector3D(16f, 0f, -2f), new Vector3D(6f, 0f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, -19f, -2f), new Vector3D(-13f, -16f, -2f), new Vector3D(-3f, -16f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-3f, -16f, -2f), new Vector3D(-3f, -19f, -2f), new Vector3D(-13f, -19f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-3f, -19f, 2f), new Vector3D(-3f, -16f, 2f), new Vector3D(-13f, -16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, -16f, 2f), new Vector3D(-13f, -19f, 2f), new Vector3D(-3f, -19f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-3f, -19f, -2f), new Vector3D(-3f, -16f, -2f), new Vector3D(-3f, -16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-3f, -16f, 2f), new Vector3D(-3f, -19f, 2f), new Vector3D(-3f, -19f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, -19f, 2f), new Vector3D(-13f, -16f, 2f), new Vector3D(-13f, -16f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, -16f, -2f), new Vector3D(-13f, -19f, -2f), new Vector3D(-13f, -19f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-6f, -19f, 2f), new Vector3D(-6f, -19f, -2f), new Vector3D(-3f, -19f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-3f, -19f, -2f), new Vector3D(-3f, -19f, 2f), new Vector3D(-6f, -19f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(3f, -19f, -2f), new Vector3D(3f, -16f, -2f), new Vector3D(13f, -16f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(13f, -16f, -2f), new Vector3D(13f, -19f, -2f), new Vector3D(3f, -19f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(13f, -19f, 2f), new Vector3D(13f, -16f, 2f), new Vector3D(3f, -16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(3f, -16f, 2f), new Vector3D(3f, -19f, 2f), new Vector3D(13f, -19f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(13f, -19f, -2f), new Vector3D(13f, -16f, -2f), new Vector3D(13f, -16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(13f, -16f, 2f), new Vector3D(13f, -19f, 2f), new Vector3D(13f, -19f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(3f, -19f, 2f), new Vector3D(3f, -16f, 2f), new Vector3D(3f, -16f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(3f, -16f, -2f), new Vector3D(3f, -19f, -2f), new Vector3D(3f, -19f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(3f, -19f, 2f), new Vector3D(3f, -19f, -2f), new Vector3D(6f, -19f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, -19f, -2f), new Vector3D(6f, -19f, 2f), new Vector3D(3f, -19f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-15f, -22f, -2f), new Vector3D(-15f, -19f, -2f), new Vector3D(-6f, -19f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-6f, -19f, -2f), new Vector3D(-6f, -22f, -2f), new Vector3D(-15f, -22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-6f, -22f, 2f), new Vector3D(-6f, -19f, 2f), new Vector3D(-15f, -19f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-15f, -19f, 2f), new Vector3D(-15f, -22f, 2f), new Vector3D(-6f, -22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-6f, -22f, -2f), new Vector3D(-6f, -19f, -2f), new Vector3D(-6f, -19f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-6f, -19f, 2f), new Vector3D(-6f, -22f, 2f), new Vector3D(-6f, -22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-15f, -22f, 2f), new Vector3D(-15f, -19f, 2f), new Vector3D(-15f, -19f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-15f, -19f, -2f), new Vector3D(-15f, -22f, -2f), new Vector3D(-15f, -22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-15f, -19f, -2f), new Vector3D(-15f, -19f, 2f), new Vector3D(-13f, -19f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, -19f, 2f), new Vector3D(-13f, -19f, -2f), new Vector3D(-15f, -19f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, -22f, -2f), new Vector3D(6f, -19f, -2f), new Vector3D(16f, -19f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, -19f, -2f), new Vector3D(16f, -22f, -2f), new Vector3D(6f, -22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, -22f, 2f), new Vector3D(16f, -19f, 2f), new Vector3D(6f, -19f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, -19f, 2f), new Vector3D(6f, -22f, 2f), new Vector3D(16f, -22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, -22f, -2f), new Vector3D(16f, -19f, -2f), new Vector3D(16f, -19f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, -19f, 2f), new Vector3D(16f, -22f, 2f), new Vector3D(16f, -22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, -22f, 2f), new Vector3D(6f, -19f, 2f), new Vector3D(6f, -19f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, -19f, -2f), new Vector3D(6f, -22f, -2f), new Vector3D(6f, -22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(13f, -19f, -2f), new Vector3D(13f, -19f, 2f), new Vector3D(16f, -19f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, -19f, 2f), new Vector3D(16f, -19f, -2f), new Vector3D(13f, -19f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-19f, -25f, -2f), new Vector3D(-19f, -22f, -2f), new Vector3D(-6f, -22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-6f, -22f, -2f), new Vector3D(-6f, -25f, -2f), new Vector3D(-19f, -25f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-6f, -25f, 2f), new Vector3D(-6f, -22f, 2f), new Vector3D(-19f, -22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-19f, -22f, 2f), new Vector3D(-19f, -25f, 2f), new Vector3D(-6f, -25f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-6f, -25f, -2f), new Vector3D(-6f, -22f, -2f), new Vector3D(-6f, -22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-6f, -22f, 2f), new Vector3D(-6f, -25f, 2f), new Vector3D(-6f, -25f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-19f, -25f, 2f), new Vector3D(-19f, -22f, 2f), new Vector3D(-19f, -22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-19f, -22f, -2f), new Vector3D(-19f, -25f, -2f), new Vector3D(-19f, -25f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-19f, -25f, 2f), new Vector3D(-19f, -25f, -2f), new Vector3D(-6f, -25f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-6f, -25f, -2f), new Vector3D(-6f, -25f, 2f), new Vector3D(-19f, -25f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-19f, -22f, -2f), new Vector3D(-19f, -22f, 2f), new Vector3D(-15f, -22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-15f, -22f, 2f), new Vector3D(-15f, -22f, -2f), new Vector3D(-19f, -22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, -25f, -2f), new Vector3D(6f, -22f, -2f), new Vector3D(19f, -22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, -22f, -2f), new Vector3D(19f, -25f, -2f), new Vector3D(6f, -25f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, -25f, 2f), new Vector3D(19f, -22f, 2f), new Vector3D(6f, -22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, -22f, 2f), new Vector3D(6f, -25f, 2f), new Vector3D(19f, -25f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, -25f, -2f), new Vector3D(19f, -22f, -2f), new Vector3D(19f, -22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, -22f, 2f), new Vector3D(19f, -25f, 2f), new Vector3D(19f, -25f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, -25f, 2f), new Vector3D(6f, -22f, 2f), new Vector3D(6f, -22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, -22f, -2f), new Vector3D(6f, -25f, -2f), new Vector3D(6f, -25f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, -25f, 2f), new Vector3D(6f, -25f, -2f), new Vector3D(19f, -25f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, -25f, -2f), new Vector3D(19f, -25f, 2f), new Vector3D(6f, -25f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, -22f, -2f), new Vector3D(16f, -22f, 2f), new Vector3D(19f, -22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(19f, -22f, 2f), new Vector3D(19f, -22f, -2f), new Vector3D(16f, -22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-10f, 22f, -2f), new Vector3D(-10f, 24f, -2f), new Vector3D(6f, 24f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 24f, -2f), new Vector3D(6f, 22f, -2f), new Vector3D(-10f, 22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 22f, 2f), new Vector3D(6f, 24f, 2f), new Vector3D(-10f, 24f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-10f, 24f, 2f), new Vector3D(-10f, 22f, 2f), new Vector3D(6f, 22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 22f, -2f), new Vector3D(6f, 24f, -2f), new Vector3D(6f, 24f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 24f, 2f), new Vector3D(6f, 22f, 2f), new Vector3D(6f, 22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-10f, 22f, 2f), new Vector3D(-10f, 24f, 2f), new Vector3D(-10f, 24f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-10f, 24f, -2f), new Vector3D(-10f, 22f, -2f), new Vector3D(-10f, 22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-10f, 24f, -2f), new Vector3D(-10f, 24f, 2f), new Vector3D(-9f, 24f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-9f, 24f, 2f), new Vector3D(-9f, 24f, -2f), new Vector3D(-10f, 24f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 16f, -2f), new Vector3D(-13f, 18f, -2f), new Vector3D(10f, 18f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(10f, 18f, -2f), new Vector3D(10f, 16f, -2f), new Vector3D(-13f, 16f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(10f, 16f, 2f), new Vector3D(10f, 18f, 2f), new Vector3D(-13f, 18f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 18f, 2f), new Vector3D(-13f, 16f, 2f), new Vector3D(10f, 16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(10f, 16f, -2f), new Vector3D(10f, 18f, -2f), new Vector3D(10f, 18f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(10f, 18f, 2f), new Vector3D(10f, 16f, 2f), new Vector3D(10f, 16f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 16f, 2f), new Vector3D(-13f, 18f, 2f), new Vector3D(-13f, 18f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 18f, -2f), new Vector3D(-13f, 16f, -2f), new Vector3D(-13f, 16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 13f, -2f), new Vector3D(-16f, 15f, -2f), new Vector3D(16f, 15f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 15f, -2f), new Vector3D(16f, 13f, -2f), new Vector3D(-16f, 13f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 13f, 2f), new Vector3D(16f, 15f, 2f), new Vector3D(-16f, 15f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 15f, 2f), new Vector3D(-16f, 13f, 2f), new Vector3D(16f, 13f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 13f, -2f), new Vector3D(16f, 15f, -2f), new Vector3D(16f, 15f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 15f, 2f), new Vector3D(16f, 13f, 2f), new Vector3D(16f, 13f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 13f, 2f), new Vector3D(-16f, 15f, 2f), new Vector3D(-16f, 15f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 15f, -2f), new Vector3D(-16f, 13f, -2f), new Vector3D(-16f, 13f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-16f, 15f, -2f), new Vector3D(-16f, 15f, 2f), new Vector3D(-13f, 15f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 15f, 2f), new Vector3D(-13f, 15f, -2f), new Vector3D(-16f, 15f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-9f, 24f, -2f), new Vector3D(-9f, 25f, -2f), new Vector3D(6f, 25f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 25f, -2f), new Vector3D(6f, 24f, -2f), new Vector3D(-9f, 24f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 24f, 2f), new Vector3D(6f, 25f, 2f), new Vector3D(-9f, 25f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-9f, 25f, 2f), new Vector3D(-9f, 24f, 2f), new Vector3D(6f, 24f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 24f, -2f), new Vector3D(6f, 25f, -2f), new Vector3D(6f, 25f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 25f, 2f), new Vector3D(6f, 24f, 2f), new Vector3D(6f, 24f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-9f, 24f, 2f), new Vector3D(-9f, 25f, 2f), new Vector3D(-9f, 25f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-9f, 25f, -2f), new Vector3D(-9f, 24f, -2f), new Vector3D(-9f, 24f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-9f, 25f, -2f), new Vector3D(-9f, 25f, 2f), new Vector3D(6f, 25f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 25f, 2f), new Vector3D(6f, 25f, -2f), new Vector3D(-9f, 25f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-12f, 21f, -2f), new Vector3D(-12f, 22f, -2f), new Vector3D(16f, 22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 22f, -2f), new Vector3D(16f, 21f, -2f), new Vector3D(-12f, 21f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 21f, 2f), new Vector3D(16f, 22f, 2f), new Vector3D(-12f, 22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-12f, 22f, 2f), new Vector3D(-12f, 21f, 2f), new Vector3D(16f, 21f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 21f, -2f), new Vector3D(16f, 22f, -2f), new Vector3D(16f, 22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 22f, 2f), new Vector3D(16f, 21f, 2f), new Vector3D(16f, 21f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-12f, 21f, 2f), new Vector3D(-12f, 22f, 2f), new Vector3D(-12f, 22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-12f, 22f, -2f), new Vector3D(-12f, 21f, -2f), new Vector3D(-12f, 21f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-12f, 22f, -2f), new Vector3D(-12f, 22f, 2f), new Vector3D(-10f, 22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-10f, 22f, 2f), new Vector3D(-10f, 22f, -2f), new Vector3D(-12f, 22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(6f, 22f, -2f), new Vector3D(6f, 22f, 2f), new Vector3D(16f, 22f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 22f, 2f), new Vector3D(16f, 22f, -2f), new Vector3D(6f, 22f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 15f, -2f), new Vector3D(-13f, 16f, -2f), new Vector3D(16f, 16f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 16f, -2f), new Vector3D(16f, 15f, -2f), new Vector3D(-13f, 15f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 15f, 2f), new Vector3D(16f, 16f, 2f), new Vector3D(-13f, 16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 16f, 2f), new Vector3D(-13f, 15f, 2f), new Vector3D(16f, 15f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 15f, -2f), new Vector3D(16f, 16f, -2f), new Vector3D(16f, 16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 16f, 2f), new Vector3D(16f, 15f, 2f), new Vector3D(16f, 15f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 15f, 2f), new Vector3D(-13f, 16f, 2f), new Vector3D(-13f, 16f, -2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(-13f, 16f, -2f), new Vector3D(-13f, 15f, -2f), new Vector3D(-13f, 15f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(10f, 16f, -2f), new Vector3D(10f, 16f, 2f), new Vector3D(16f, 16f, 2f)), rayHigh));
            list.Add(new RayTriangle(new Triangle(new Vector3D(16f, 16f, 2f), new Vector3D(16f, 16f, -2f), new Vector3D(10f, 16f, -2f)), rayHigh));
            #endregion

            //Wenn diese Zeilen drin sind, dann hat man direkt nur das Dreieck, was getroffen werden soll
            //RayTriangle triangle = list[55];
            //list = new List<RayTriangle>() { triangle };

            KDSahTree sut = new KDSahTree(list.Cast<IIntersecableObject>().ToList(), (s, f) => { });
            BoundingIntervallHierarchy bih = new BoundingIntervallHierarchy(list.Cast<IIntersecableObject>().ToList(), (s, f) => { });

            var sutPoint = sut.GetIntersectionPoint(new Ray(new Vector3D(-0.573426247f, 114f, -206.241776f), new Vector3D(0.0301454682f, -0.472031146f, 0.881066322f)), null, float.MaxValue, 0);
            var bihPoint = bih.GetIntersectionPoint(new Ray(new Vector3D(-0.573426247f, 114f, -206.241776f), new Vector3D(0.0301454682f, -0.472031146f, 0.881066322f)), null, float.MaxValue, 0);
            Assert.IsNotNull(bihPoint);
            Assert.IsNotNull(sutPoint, "Kd-Baum zeigt Schnittpunkt nicht");
        }

        

        private void Linear_KD_Compare(List<IIntersecableObject> list, Ray ray, IIntersecableObject excludedObject = null)
        {
            KDSahTree sut = new KDSahTree(list, (s, f) => { });
            //BoundingIntervallHierarchy bih = new BoundingIntervallHierarchy(list, (s, f) => { });
            LinearSearchIntersector lin = new LinearSearchIntersector(list);

            var linPoint = lin.GetIntersectionPoint(ray, excludedObject, float.MaxValue, 0);
            var sutPoint = sut.GetIntersectionPoint(ray, excludedObject, float.MaxValue, 0);

            //var pNext = linPoint.IntersectedObject.GetSimpleIntersectionPoint(ray, 0);

            if (linPoint == null && sutPoint == null) return;

            var kdItem = string.Join(", ", sut.VisitEachLeafeItem().Where(x => x.Obj == linPoint.IntersectedObject).Select(x => x.Location));

            Assert.IsNotNull(linPoint);
            Assert.IsNotNull(sutPoint, $"Kd-Baum zeigt Schnittpunkt nicht. Es wurde {kdItem} nicht angesprungen");
            Assert.AreEqual(linPoint.DistanceToRayStart, sutPoint.DistanceToRayStart);
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForRayInFullPathBoxScene1()
        {
            Linear_KD_Compare(IntersectableObjectsData.box, new Ray(new Vector3D(-24.4703178f, 90f, -62.0122452f), new Vector3D(-0.0272170603f, -0.994390488f, -0.102209724f)));
        }

        //Kd-Baum zeigt Schnittpunkt nicht. Es wurde root left right right Index=0 nicht angesprungen
        [TestMethod]
        //[Ignore] //Ray-Start liegt genau auf der Split-Ebene von ein KDBaum-Knoten. In diesen Fall entscheidet sich der KD-Baum für eine von beiden Splitseiten anstatt beide zu nehmen
        public void GetIntersectionPoint_CalledForRayInFullPathBoxScene2()
        {
            //IntersectedObject = {Test [new Vector3D(-90f, -90f, 90f)|new Vector3D(-90f, -90f, -90f)|new Vector3D(90f, -90f, -90f)|new Vector3D(90f, -90f, 90f)] IsLightSource=false}
            //root left right right -> Hier würde das Objekt liegen was gesucht ist
            //root left right left -> So sucht der Baum und findet es nicht
            Linear_KD_Compare(IntersectableObjectsData.box, new Ray(new Vector3D(52.0540924f, -19.229372f, -90f), new Vector3D(0.0972473249f, -0.775696576f, 0.623568654f)));
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForRayInFullPathBoxScene3()
        {
            Linear_KD_Compare(IntersectableObjectsData.box, new Ray(new Vector3D(0f, -190f, 0f), new Vector3D(0.232311338f, 0.936068952f, 0.264209032f)));
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForRayInFullPathBoxScene4()
        {
            Linear_KD_Compare(IntersectableObjectsData.box, new Ray(new Vector3D(-47.2172699f, 90f, 89.9997253f), new Vector3D(0.796966851f, -0.46732673f, 0.38268736f)));
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForRayInCPLScene1()
        {
            Linear_KD_Compare(IntersectableObjectsData.cplScene, new Ray(new Vector3D(0f, 0f, -0.5f), new Vector3D(0f, 0f, 1f)));
        }

        //Kd-Baum zeigt Schnittpunkt nicht. Es wurde root left left right left Index=1 nicht angesprungen
        [TestMethod]
        public void GetIntersectionPoint_CalledForRayInSubpathSamplerScene1()
        {
            Linear_KD_Compare(IntersectableObjectsData.subpathSamplerScene, new Ray(new Vector3D(6.5999999f, 1.69203949f, -0.642457247f), new Vector3D(-0.963941693f, -0.13268815f, 0.230673462f)));
        }

        //Kd-Baum zeigt Schnittpunkt nicht. Es wurde root left left right left Index=1 nicht angesprungen
        [TestMethod]
        public void GetIntersectionPoint_CalledForRayInSubpathSamplerScene2()
        {
            Linear_KD_Compare(IntersectableObjectsData.subpathSamplerScene, new Ray(new Vector3D(6.5999999f, 0.184085086f, -0.811365008f), new Vector3D(-0.988465667f, 0.151159406f, -0.00930232555f)));
        }

        //Kd-Baum zeigt Schnittpunkt nicht. Es wurde root right right left left right left right right Index=1 nicht angesprungen
        [TestMethod]
        public void GetIntersectionPoint_CalledForToyBod_TextureMapping1()
        {
            Linear_KD_Compare(IntersectableObjectsData.toyBox, new Ray(new Vector3D(0f, 0f, 0f), new Vector3D(0.395456642f, -0.0173954535f, -0.918319941f)));
        }

        //Kd-Baum zeigt Schnittpunkt nicht. Es wurde root right right left left right right right Index=5 nicht angesprungen
        [TestMethod]
        public void GetIntersectionPoint_CalledForToyBod_TextureMapping2()
        {
            Linear_KD_Compare(IntersectableObjectsData.toyBox, new Ray(new Vector3D(0f, 0f, 0f), new Vector3D(0.431827515f, 0.0895390436f, -0.897500873f)));
        }

        //Kd-Baum zeigt Schnittpunkt nicht. Es wurde root right left right left left left right left right left left left Index=1, root right left right left left left right left right left left right Index=4, root right left right left left left right left right left right left Index=3, root right left right left left left right left right left right right Index=6, root right left right left left left right left right right left Index=6, root right left right left left left right left right right right Index=7, root right left right left left right right left left left left left left Index=3, root right left right left left right right left left left left right left Index=3, root right left right left left right right left right left left left left Index=3, root right left right left left right right left right left left right left Index=5 nicht angesprungen
        [TestMethod]
        public void GetIntersectionPoint_CalledForWindowRoom1()
        {
            Linear_KD_Compare(IntersectableObjectsData.windowRoom, new Ray(new Vector3D(15.75f, 0f, -8.875f), new Vector3D(-0.634727657f, 0.361553669f, -0.682934701f)));
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForWindowRoom2()
        {
            Linear_KD_Compare(IntersectableObjectsData.windowRoom, new Ray(new Vector3D(12.625f, 0f, -1.625f), new Vector3D(-0.670896471f, 0.425966024f, -0.607001543f)));
        }

        //Kd-Baum zeigt Schnittpunkt nicht. Es wurde root right left right left left left right left right left left left Index=1, root right left right left left left right left right left left right Index=4, root right left right left left left right left right left right left Index=3, root right left right left left left right left right left right right Index=6, root right left right left left left right left right right left Index=6, root right left right left left left right left right right right Index=7, root right left right left left right right left left left left left left Index=3, root right left right left left right right left left left left right left Index=3, root right left right left left right right left right left left left left Index=3, root right left right left left right right left right left left right left Index=5 nicht angesprungen
        [TestMethod]
        public void GetIntersectionPoint_CreateAirLine_TwoAirCubesToParticleInSecondCube_ParticleIsVisible()
        {
            Linear_KD_Compare(IntersectableObjectsData.mediaIntersectionFinder, new Ray(new Vector3D(2f, 0.5f, 0f), new Vector3D(1f, 0f, 0f)), IntersectableObjectsData.mediaIntersectionFinder[23]);
        }

        [TestMethod]
        public void GetIntersectionPoint_CheckPixelColor_BidirectionalPathTracing_Cornellbox_GlassSphereLightFlackOverRectangle1()
        {
            Linear_KD_Compare(IntersectableObjectsData.cornellBox, new Ray(new Vector3D(0.147045732f, 0.213771492f, -0.559000015f), new Vector3D(-0.97953552f, -0.201271445f, 0f)));
        }

        [TestMethod]
        public void GetIntersectionPoint_CheckPixelColor_BidirectionalPathTracing_Cornellbox_GlassSphereLightFlackOverRectangle2()
        {
            Linear_KD_Compare(IntersectableObjectsData.cornellBox, new Ray(new Vector3D(0.0497084036f, -7.4505806E-09f, -0.240201071f), new Vector3D(0.74241364f, 0f, -0.669941783f)));
        }

        [TestMethod] //Wenn ich in bei IntersectableTriangle if (1 - beta - gamma < 0) return null; schreibe, wird der Test rot
        [Ignore] //Ignore, da es anscheinend nicht möglich ist die Ray-Triangle-Schnittpunktsabfrage so zu machen, dass der BoundingBox-Test eher ein Schnittpunkt findet, wie der Triangle-Schnittpunktstest (Ich müsste die BoundingBox des Dreiecks um ein Epsilon-Wert größer machen)
        public void GetIntersectionPoint_CalledForTriangleAndRaySphere_VertexPropertysAreEqual1()
        {
            TriangleObject data = TriangleObjectGenerator.CreateSphere(1, 20, 20);
            var triData = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", Orientation = new Vector3D(0, 0, 0) });

            var triangles = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { triData }, true);

            Linear_KD_Compare(triangles.Cast<IIntersecableObject>().ToList(), new Ray(new Vector3D(0f, 0f, 0f), new Vector3D(-2.25403822E-17f, 0.368124545f, 0.92977649f)));
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForTriangleAndRaySphere_VertexPropertysAreEqual2()
        {
            TriangleObject data = TriangleObjectGenerator.CreateSphere(1, 20, 20);
            var triData = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", Orientation = new Vector3D(0, 0, 0) });

            var triangles = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { triData }, true);

            Linear_KD_Compare(triangles.Cast<IIntersecableObject>().ToList(), new Ray(new Vector3D(0f, 0f, 0f), new Vector3D(-9.57853146E-18f, 0.156434461f, 0.987688363f)));
        }

        
        //Wenn ich kein IntersectionHelper.CanRayClippedAwayWithBoundingBox-Test in der IntersectableTriangle-Klasse mache, dann wird der Test hier rot
        [TestMethod]
        [Ignore] //Ignore, da es anscheinend nicht möglich ist die Ray-Triangle-Schnittpunktsabfrage so zu machen, dass der BoundingBox-Test eher ein Schnittpunkt findet, wie der Triangle-Schnittpunktstest (Ich müsste die BoundingBox des Dreiecks um ein Epsilon-Wert größer machen)
        public void GetIntersectionPoint_CalledForTriangleAndRaySphere_VertexPropertysAreEqual3()
        {
            TriangleObject data = TriangleObjectGenerator.CreateSphere(1, 20, 20);
            var triData = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", Orientation = new Vector3D(0, 0, 0) });

            var triangles = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { triData }, true);

            Linear_KD_Compare(triangles.Cast<IIntersecableObject>().ToList(), new Ray(new Vector3D(0f, 0f, 0f), new Vector3D(-2.25403822E-17f, 0.368124545f, 0.92977649f)));
        }

        [TestMethod]
        //[Ignore]
        public void GetIntersectionPoint_CalledForTriangleAndRaySphere_VertexPropertysAreEqual4()
        {
            TriangleObject data = TriangleObjectGenerator.CreateSphere(1, 20, 20);
            var triData = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", Orientation = new Vector3D(0, 0, 0) });

            var triangles = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { triData }, true);

            Linear_KD_Compare(triangles.Cast<IIntersecableObject>().ToList(), new Ray(new Vector3D(0f, 0f, 0f), new Vector3D(-0.187381312f, -0.982287228f, 6.12303177E-17f)));
        }
    }
}
