using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RayObjects;
using RayObjects.RayObjects;
using RayTracerGlobal;
using TriangleObjectGeneration;

namespace IntersectionTestsTest
{
    [TestClass]
    public class IntersectionFinderTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void GetIntersectionPoint_CalledMultipetimes_NoRayCanLeaveTheTriangleQuader()
        {
            TriangleObject data = TriangleObjectGenerator.CreateCube(1, 1, 1);
            var cube = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF" });
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { cube });

            Assert.AreEqual(12, rayObjects.Count);
            TryToExcapeFromObject(rayObjects);
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledMultipetimes_NoRayCanLeaveTheQuadQuader()
        {
            TriangleObject data = TriangleObjectGenerator.CreateCube(1, 1, 1);
            var cube = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF" });
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { cube }, true);

            Assert.AreEqual(6, rayObjects.Count);
            TryToExcapeFromObject(rayObjects);
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledMultipetimes_NoRayCanLeaveTheTriangleSphere()
        {
            TriangleObject data = TriangleObjectGenerator.CreateSphere(1, 10, 10);
            var cube = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF" });
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { cube }, false);

            Assert.IsTrue(rayObjects.Count > 50);
            TryToExcapeFromObject(rayObjects);
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledMultipetimes_NoRayCanLeaveTheRaySphere()
        {
            TriangleObject data = TriangleObjectGenerator.CreateSphere(1, 10, 10);
            var cube = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF" });
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { cube });

            Assert.AreEqual(1, rayObjects.Count);
            TryToExcapeFromObject(rayObjects);
        }

        private void TryToExcapeFromObject(List<IRayObject> rayObjects)
        {            
            var intersectionFinder = new IntersectionFinder(rayObjects.Cast<IIntersecableObject>().ToList(), null);

            int sampleCount = 10000;
            Random rand = new Random(0);
            for (int i = 0; i < sampleCount; i++)
            {
                Vector3D direction = Vector3D.Normalize(new Vector3D((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f));
                var point = intersectionFinder.GetIntersectionPoint(new Ray(new Vector3D(0, 0, 0), direction), 0);
                Assert.IsNotNull(point);
            }
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForTriangleAndQuadCube_VertexPropertysAreEqual()
        {
            TriangleObject data = TriangleObjectGenerator.CreateCube(1, 1, 1);
            var cube = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat });

            var triangles = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { cube });
            var quads = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { cube }, true);

            var intersectionFinder1 = new IntersectionFinder(triangles.Cast<IIntersecableObject>().ToList(), null);
            var intersectionFinder2 = new IntersectionFinder(quads.Cast<IIntersecableObject>().ToList(), null);

            CompareVertexDataFromIntersectionPoint(intersectionFinder1, intersectionFinder2);
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForTriangleAndSubdiviedQuadCube_VertexPropertysAreEqual()
        {
            TriangleObject data = TriangleObjectGenerator.CreateCube(1, 1, 1);
            var cube = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat });

            var triangles = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { cube });
            var quads = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { cube }, true, (source, obj) => { return obj.SurfaceArea < 0.2f; });

            var intersectionFinder1 = new IntersectionFinder(triangles.Cast<IIntersecableObject>().ToList(), null);
            var intersectionFinder2 = new IntersectionFinder(quads.Cast<IIntersecableObject>().ToList(), null);

            CompareVertexDataFromIntersectionPoint(intersectionFinder1, intersectionFinder2);
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForTriangleAndSubdiviedTrianglesCube_VertexPropertysAreEqual()
        {
            TriangleObject data = TriangleObjectGenerator.CreateCube(1, 1, 1);
            var cube = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat });

            var triangles = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { cube });
            var subTriangles = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { cube }, false, (source, obj) => { return obj.SurfaceArea < 0.2f; });

            var intersectionFinder1 = new IntersectionFinder(triangles.Cast<IIntersecableObject>().ToList(), null);
            var intersectionFinder2 = new IntersectionFinder(subTriangles.Cast<IIntersecableObject>().ToList(), null);

            CompareVertexDataFromIntersectionPoint(intersectionFinder1, intersectionFinder2);
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForTriangleAndRaySphere_VertexPropertysAreEqual()
        {
            TriangleObject data = TriangleObjectGenerator.CreateSphere(1, 20, 20);
            var triData = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", Orientation = new Vector3D(0, 0, 0) });

            var sphere = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { triData });
            var triangles = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { triData }, true);

            var intersectionFinder1 = new IntersectionFinder(sphere.Cast<IIntersecableObject>().ToList(), null);
            var intersectionFinder2 = new IntersectionFinder(triangles.Cast<IIntersecableObject>().ToList(), null);

            BitmapHelp.TransformBitmapListToRow(new List<Bitmap>() {
                BitmapHelp.WriteToBitmap(GetTextureImageFromSphere(100, intersectionFinder1), "Sphere", Color.Black),
                BitmapHelp.WriteToBitmap(GetTextureImageFromSphere(100, intersectionFinder2), "Triangles", Color.Black),
            }).Save(WorkingDirectory + "SphereTextures.bmp");            

            CompareVertexDataFromIntersectionPoint(intersectionFinder1, intersectionFinder2, 0.04f);
        }

        private Bitmap GetTextureImageFromSphere(int size, IntersectionFinder intersectionFinder)
        {
            SphericalCoordinateConverter converter = new SphericalCoordinateConverter();
            Bitmap image = new Bitmap(100, 100);
            double maxPhi = 2 * Math.PI;
            double maxTheta = Math.PI;
            for (int phi = 0; phi < image.Width; phi++)
                for (int theta = 0; theta < image.Height; theta++)
                {
                    Vector3D direction = converter.ToWorldDirection(new SphericalCoordinate(phi * maxPhi / image.Width, theta * maxTheta / image.Height));
                    var point = intersectionFinder.GetIntersectionPoint(new Ray(new Vector3D(0, 0, 0), direction), 0);
                    if (point != null)
                    {
                        image.SetPixel(phi, theta, PixelHelper.VectorToColor(new Vector3D(point.VertexPoint.TextcoordVector, 1))); //U und V
                        //image.SetPixel(phi, theta, PixelHelper.VectorToColor(new Vector3D(point.VertexPoint.TextcoordVector.X, 0, 0))); //U
                        //image.SetPixel(phi, theta, PixelHelper.VectorToColor(new Vector3D(0, point.VertexPoint.TextcoordVector.Y, 0))); //V

                        //image.SetPixel(phi, theta, PixelHelper.VectorToColor(point.VertexPoint.Tangent)); //Tangente
                        //image.SetPixel(phi, theta, PixelHelper.VectorToColor(new Vector3D(point.VertexPoint.Tangent.X, 0, 0))); //Tangente.X
                        //image.SetPixel(phi, theta, PixelHelper.VectorToColor(new Vector3D(0, point.VertexPoint.Tangent.Y, 0))); //Tangente.Y
                    }
                        
                }
            return image;
        }

        private void CompareVertexDataFromIntersectionPoint(IntersectionFinder intersectionFinder1, IntersectionFinder intersectionFinder2, float maxError = 0.001f)
        {
            int sampleCount = 1000;
            Random rand = new Random(0);
            for (int i = 0; i < sampleCount; i++)
            {
                Vector3D direction = Vector3D.Normalize(new Vector3D((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f));
                var point1 = intersectionFinder1.GetIntersectionPoint(new Ray(new Vector3D(0, 0, 0), direction), 0);
                var point2 = intersectionFinder2.GetIntersectionPoint(new Ray(new Vector3D(0, 0, 0), direction), 0);

                Assert.IsTrue((point1.Position - point2.Position).Length() < maxError, "Position");
                Assert.IsTrue(point1.OrientedFlatNormal * point2.OrientedFlatNormal > 1 - maxError, "Normal");
                Assert.IsTrue(point1.ShadedNormal * point2.ShadedNormal > 1 - maxError, "Normal");
                Assert.IsTrue(point1.Tangent * point2.Tangent > 1 - maxError, "Tangent " + point1.Tangent.ToShortString() + " <-> " + point2.Tangent.ToShortString());
                Assert.IsTrue((point1.VertexPoint.TextcoordVector - point2.VertexPoint.TextcoordVector).Length() < maxError, "TextcoordVector-Error=" + (point1.VertexPoint.TextcoordVector - point2.VertexPoint.TextcoordVector).Length());
            }
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForSphere_NormalIsOutside()
        {
            CheckNormalIsOutside(TriangleObjectGenerator.CreateSphere(1, 7, 7));            
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForRing_NormalIsOutside()
        {
            CheckNormalIsOutside(TriangleObjectGenerator.CreateRing(0.3f, 2, 5, 5));
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForFlasche_NormalIsOutside()
        {
            CheckNormalIsOutside(TriangleObjectGenerator.CreateBottle(1, 2, 6));
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForFackel_NormalIsOutside()
        {
            CheckNormalIsOutside(TriangleObjectGenerator.CreateTorch(0.2f, 5));
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForZylinder_NormalIsOutside()
        {
            CheckNormalIsOutside(TriangleObjectGenerator.CreateCylinder(5, 1, 2, false, 4));
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForSchwert_NormalIsOutside()
        {
            CheckNormalIsOutside(TriangleObjectGenerator.CreateSword(4,5));
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForSpieser_NormalIsOutside()
        {
            CheckNormalIsOutside(TriangleObjectGenerator.CreateSkewer(4, 3));
        }

        //Schieße von Außen Strahl gegen Objekt und schaue, ob Normale auch zu mir hin nach außen zeigt
        private void CheckNormalIsOutside(TriangleObject data)
        {
            var triData = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", Orientation = new Vector3D(90, 20, 0) });

            var triangles = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { triData });
            if (triangles.Count == 1 && triangles[0] is RaySphere)
            {
                triangles = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(new List<DrawingObject>() { triData }, true);
            }

            var intersectionFinder = new IntersectionFinder(triangles.Cast<IIntersecableObject>().ToList(), null);

            var p = intersectionFinder.GetIntersectionPoint(new Ray(new Vector3D(-100, 0, 0), new Vector3D(1, 0, 0)), 0);
            Assert.IsNotNull(p, "Objekt nicht getroffen");
            Assert.IsTrue(p.FlatNormal * new Vector3D(-1, 0, 0) > 0, "Normale zeigt nicht nach außen");
        }

        [TestMethod]
        public void GetAllIntersectionPoints_CalledForOneSphere_OneIntersectionPointFound()
        {
            var points = GetAllIntersectionPointsFromSphere(new Ray(new Vector3D(0.453478456f, 6360f, 0.109414801f), new Vector3D(-0.781651616f, 0.249662444f, 0.571567476f)), 6420);
            Assert.AreEqual(1, points.Count);            
        }

        [TestMethod]
        public void GetAllIntersectionPoints_CalledForOneSphere_TwoIntersectionPointsFound1()
        {
            var points = GetAllIntersectionPointsFromSphere(new Ray(new Vector3D(-4323.91553f, -3297.19458f, -3413.22559f), new Vector3D(0.707106769f, -0.707106769f, 0f)), 6420);
            Assert.AreEqual(2, points.Count);
        }

        [TestMethod]
        public void GetAllIntersectionPoints_CalledForOneSphere_TwoIntersectionPointsFound2()
        {
            var points = GetAllIntersectionPointsFromSphere(new Ray(new Vector3D(431.313324f, 434.982422f, 192.164886f), new Vector3D(0.707106769f, -0.707106769f, 0f)), 642);
            Assert.AreEqual(2, points.Count);
        }

        [TestMethod]
        public void GetAllIntersectionPoints_CalledForCubeEdge_TwoIntersectionPointsFound()
        {
            TriangleObject data = TriangleObjectGenerator.CreateCube(1,1,1);
            var triData1 = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", Orientation = new Vector3D(0, 0, 0), Size = 1 });

            var cube = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { triData1 });
            
            var intersectionFinder = new IntersectionFinder(cube.Cast<IIntersecableObject>().ToList(), null);

            Vector3D direction = Vector3D.Normalize(new Vector3D(1, -1, 0));
            float d = MagicNumbers.MinAllowedPathPointDistance / 2 / 10;
            Vector3D nearEdge = new Vector3D(1,1,0) + Vector3D.Normalize(new Vector3D(-1, -1, 0)) * d;
            Vector3D start = nearEdge - direction * 10;

            var points = intersectionFinder.GetAllIntersectionPoints(new Ray(start, direction), float.MaxValue, 0);
            Assert.AreEqual(2, points.Count);
        }

        private List<IntersectionPoint> GetAllIntersectionPointsFromSphere(Ray ray, float radius)
        {
            TriangleObject data = TriangleObjectGenerator.CreateSphere(1, 20, 20);
            var triData1 = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", Orientation = new Vector3D(0, 0, 0), Size = radius });

            var sphere = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { triData1 });
            //var sphere = new RayObjectCreationHelper().CreatePlanarObjects(new List<DrawingObject>() { triData1, triData2 }, true);

            var intersectionFinder = new IntersectionFinder(sphere.Cast<IIntersecableObject>().ToList(), null);

            var points = intersectionFinder.GetAllIntersectionPoints(ray, float.MaxValue, 0);
            return points;
        }

        [TestMethod]
        public void GetAllIntersectionPoints_CalledForTwoSpheres_ForHitpointsFound1()
        {
            GetAllIntersectionPoints_CalledForTwoSpheres_ForHitpointsFound((l) => { return new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(l); });
        }

        [TestMethod]
        public void GetAllIntersectionPoints_CalledForTwoSpheres_ForHitpointsFound2()
        {
            GetAllIntersectionPoints_CalledForTwoSpheres_ForHitpointsFound((l) => { return new RayObjectCreationHelper(new GlobalObjectPropertys()).CreatePlanarObjects(l, true); });
        }

        private void GetAllIntersectionPoints_CalledForTwoSpheres_ForHitpointsFound(Func<List<DrawingObject>, List<IRayObject>> converter)
        {
            TriangleObject data = TriangleObjectGenerator.CreateSphere(1, 20, 20);
            var triData1 = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", Orientation = new Vector3D(0, 0, 0), Size = 1 });
            var triData2 = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", Orientation = new Vector3D(0, 0, 0), Size = 2 });

            var sphereList = converter(new List<DrawingObject>() { triData1, triData2 });

            var intersectionFinder = new IntersectionFinder(sphereList.Cast<IIntersecableObject>().ToList(), null);

            var points = intersectionFinder.GetAllIntersectionPoints(new Ray(new Vector3D(-10, 0, 0), new Vector3D(1, 0, 0)), float.MaxValue, 0);
            Assert.AreEqual(4, points.Count);
            Assert.AreEqual(-2, points[0].Position.X);
            Assert.AreEqual(-1, points[1].Position.X);
            Assert.AreEqual(+1, points[2].Position.X);
            Assert.AreEqual(+2, points[3].Position.X); 
        }

        [TestMethod]
        public void GetIntersectionPoint_CalledForSphere_TangentThrowsNoException()
        {
            var sphere = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { new DrawingObject(TriangleObjectGenerator.CreateSphere(1, 20, 20), new ObjectPropertys() { TextureFile = "#FFFFFF" }) });
            var intersectionFinder = new IntersectionFinder(sphere.Cast<IIntersecableObject>().ToList(), null);
            var point = intersectionFinder.GetIntersectionPoint(new Ray(new Vector3D(0, 0, 0), new Vector3D(0, 1, 0)), 0);
            Assert.IsNotNull(point.Tangent);
        }


        //Dieser Test zeigt, dass es nur ein Schnittpunkt zwischen einen Strahl und ein Würfel gibt, wenn im IntersectableTriangle.cs die Gamma+Beta+Summe mit 1f verglichen wird.
        //Mein aktueller Fix dafür ist if (sum > 1.0001f) return null;
        [TestMethod]
        [Ignore]    //Ich ignoriere den Test erstmal, da mir der Fix mit sum > 1.0001f doch zu heiß ist und er alleine nicht ausreicht, damit zu jeder Zeit der Schnittpunkt zwischen ein Wolkenwürfel und ein Strahl gefunden wird
        public void GetIntersectionPoint_CalledForRayFromOutside_TwoIntersectionspointsAreFound1()
        {
            TriangleObject data = TriangleObjectGenerator.CreateCube(258.057251f, 129.689636f, 479.934723f);
            var cube = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", Position = new Vector3D(325.197998f, 6361097.5f, -119.482574f), Size = 1.8f });
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { cube });

            var intersectionFinder = new IntersectionFinder(rayObjects.Cast<IIntersecableObject>().ToList(), null);

            var points = intersectionFinder.GetAllIntersectionPoints(new Ray(new Vector3D(1.0206619E-06f, 6360100f, 4.19625076E-06f), new Vector3D(-0.102066189f, 0.901940823f, -0.419625103f)), float.MaxValue, 0);

            Assert.AreEqual(2, points.Count);
        }

        [TestMethod]
        [Ignore] //Bei diesen Test muss man sogar die gamma/beta < 0-Abfrage manipulieren, damit es geht
        public void GetIntersectionPoint_CalledForRayFromOutside_TwoIntersectionspointsAreFound2()
        {
            float s = 1;
            TriangleObject data = TriangleObjectGenerator.CreateCube(498.3865f * s, 181.7888f * s, 493.6584f * s);
            var cube = new DrawingObject(data, new ObjectPropertys() { TextureFile = "#FFFFFF", Position = new Vector3D(3156.44751f, 6361285.5f, 1992.78235f) * s, Size = 1.8f });
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { cube });

            var intersectionFinder = new IntersectionFinder(rayObjects.Cast<IIntersecableObject>().ToList(), null);

            var points = intersectionFinder.GetAllIntersectionPoints(new Ray(new Vector3D(5856.70898f , 6361607.5f, 5002.20313f) * s, new Vector3D(-0.581716061f, 0.00170598924f, -0.813390195f)), float.MaxValue, 0);

            Assert.IsFalse(cube.GetBoundingBoxFromObject().IsPointInside(new Vector3D(5856.70898f, 6361607.5f, 5002.20313f)));

            Assert.AreEqual(2, points.Count);
        }

        //Erzeugt eine 3x3-Große Bumpmap, wo alle Normalen (0,0,1) sind und wo der Höhenwert bei allen Pixeln 1 
        //ist außer bei den in der Mitte. Dort ist er 0 (Tiefe Mulde in der Mitte)
        [TestMethod]
        [Ignore] //Wird nur ausgeführt, wenn eine neue ParallaxBumpmap erstellt werden soll
        public void CreateParallaxTexture()
        {
            Bitmap bumpmap = new Bitmap(3, 3);
            for (int x=0;x<3;x++)
                for (int y=0;y<3;y++)
                {
                    bumpmap.SetPixel(x, y, Color.Blue);
                }
            bumpmap.SetPixel(1, 1, Color.FromArgb(0, 0, 0, 255));
            bumpmap.Save(WorkingDirectory + "ExpectedValues\\ParallaxBumpmap.bmp");
        }

        //Parallaxmappingtest 1: Ich starte über der Mulde von einer Parallaxmap und schaue in die Mulde rein
        //Erwartung: Der zurück gegebene Parallaxpunkt liegt bei Höhe 0
        [TestMethod]
        public void GetIntersectionPoint_CalledForParallaxQuadWithHeighScale1_ReturnsParallaxPointWithHeight0()
        {
            //Die Kamera schaut frontal auf die Mulde
            var point = GetIntersectionPointWithParallaxQuad(new Ray(new Vector3D(0, 0, 5), new Vector3D(0, 0, -1))).ParallaxPoint;

            Assert.AreEqual(new Vector3D(0.5f, 0.5f, 0), point.TexturSpacePoint);
            Assert.AreEqual(new Vector2D(0.5f, 0.5f), point.TexureCoords); //Da meine TexutureMatrix die Einheitsmatrix ist, erwarte ich, dass TexureCoords == TexturSpacePoint.XY
            Assert.AreEqual(new Vector3D(0, 0, 0), point.EntryWorldPoint.Position); 
            Assert.AreEqual(new Vector3D(0, 0, -1), point.WorldSpacePoint);
            Assert.AreEqual(false, point.PointIsOnTopHeight);
        }

        //Parallaxmappingtest 2: Ich starte rechts über der Mulde von einer Parallaxmap und schaue auf den Rand oben
        //Erwartung: Der zurück gegebene Parallaxpunkt liegt bei Höhe 1
        [TestMethod]
        public void GetIntersectionPoint_CalledForParallaxQuadWithHeighScale1_ReturnsParallaxPointWithHeight1()
        {
            //Die Kamera schaut frontal auf den Pixel rechts daneben
            var point = GetIntersectionPointWithParallaxQuad(new Ray(new Vector3D(1, 0, 5), new Vector3D(0, 0, -1))).ParallaxPoint;

            Assert.AreEqual(new Vector3D(0.5f + 1/3f, 0.5f, 1), point.TexturSpacePoint);
            Assert.AreEqual(new Vector2D(0.5f + 1 / 3f, 0.5f), point.TexureCoords); //Da meine TexutureMatrix die Einheitsmatrix ist, erwarte ich, dass TexureCoords == TexturSpacePoint.XY
            Assert.AreEqual(new Vector3D(1, 0, 0), point.EntryWorldPoint.Position);
            Assert.AreEqual(new Vector3D(1, 0, 0), point.WorldSpacePoint);
            Assert.AreEqual(true, point.PointIsOnTopHeight);
        }

        private IntersectionPoint GetIntersectionPointWithParallaxQuad(Ray ray)
        {
            //Viereck mit Kantenlänge 3. Jeder Texturpixel darauf hat dann Kantenlänge von 1
            TriangleObject data = TriangleObjectGenerator.CreateSquareXY(1.5f, 1.5f, 1);
            var quad = new DrawingObject(data, new ObjectPropertys() { NormalSource = new NormalFromParallax() { ParallaxMap = WorkingDirectory + "ExpectedValues\\ParallaxBumpmap.bmp", TexturHeightFactor = 1 } });
            var rayObjects = new RayObjectCreationHelper(new GlobalObjectPropertys()).CreateRayObjects(new List<DrawingObject>() { quad });
            var intersectionFinder = new IntersectionFinder(rayObjects.Cast<IIntersecableObject>().ToList(), null);

            return intersectionFinder.GetIntersectionPoint(ray, 0);
        }

        //Wenn ich ein Schattenstrahltest zwischen zwei Surfacepunkten mache, dann erwarte ich, dass er in beide Richtungen das gleiche anzeigt 
        //Diese Anforderung ist bei Radiostiy-SolidAngle entstanden. Diese Erwartung funktioniert aber nicht, wenn zwei Objekte eine gemeinsame
        //Schnittkante haben (Würfel steht auf Boden; Die Unterkante vom Würfel berührt den Boden und bildet gemeinsame Schnittkante).
        //Wenn ich nun ein Punkt auf der gemeinsamen Schnittkante per Schattenstrahl ansteuere, dann hängt es von der Reihenfolge der Objekte ab,
        //wie sie abgefragt werden, welches zuerst getroffen wird und was somit den Schnittpunkt erzeugt.
        [TestMethod]
        public void GetIntersectionPoint_ShadowRayTestInBothDirections()
        {
            IIntersectableRayDrawingObject rayHeigh = new RayDrawingObject(new ObjectPropertys() { Name = "Test" }, null, null);

            List<IIntersecableObject> list = new List<IIntersecableObject>()
            {
                new RayTriangle(new Triangle(new Vertex(new Vector3D(0.474000007f, 0f, -0.224999994f),new Vector3D(0.665146589f, 0.407932371f, 0.625436783f),new Vector3D(-0.958491504f, 0f, -0.285121024f),0f,0f),new Vertex(new Vector3D(0.316000015f, 0f, -0.272000015f),new Vector3D(-0.509850383f, 0.669252157f, 0.5405128f),new Vector3D(-0.958491504f, 0f, -0.285121024f),0f,0f),new Vertex(new Vector3D(0.425999999f, 0f, -0.0649999976f),new Vector3D(0.124216333f, 0.667516351f, 0.73416096f),new Vector3D(-0.958491504f, 0f, -0.285121024f),0f,0f)), rayHeigh),
                new RayQuad(new Quad(new Vertex(new Vector3D(0.474000007f, 0f, -0.224999994f),new Vector3D(0.665146589f, 0.407932371f, 0.625436783f),new Vector3D(-0.677431822f, -0.707444668f, -0.201514632f),0f,0f),new Vertex(new Vector3D(0.474000007f, 0.165000007f, -0.224999994f),new Vector3D(0.158108145f, 0.407932371f, 0.899217963f),new Vector3D(-0.677431822f, -0.707444668f, -0.201514632f),0f,0f),new Vertex(new Vector3D(0.316000015f, 0.165000007f, -0.272000015f),new Vector3D(-0.733474016f, 0.669252157f, 0.118816897f),new Vector3D(-0.958491504f, 0f, -0.285121024f),0f,0f),new Vertex(new Vector3D(0.316000015f, 0f, -0.272000015f),new Vector3D(-0.509850383f, 0.669252157f, 0.5405128f),new Vector3D(-0.958491504f, 0f, -0.285121024f),0f,0f)),rayHeigh),
                new RayTriangle(new Triangle(new Vertex(new Vector3D(0.418500006f, 0f, -0.279500008f),new Vector3D(0f, 1f, 0f),new Vector3D(1f, 0f, 0f),0.75f,0.5f),new Vertex(new Vector3D(0.281000018f, 0f, -0.279500008f),new Vector3D(0f, 1f, 0f),new Vector3D(1f, 0f, 0f),0.5f,0.5f),new Vertex(new Vector3D(0.418500006f, 0f, -0.139750004f),new Vector3D(0f, 1f, 0f),new Vector3D(1f, 0f, 0f),0.75f,0.25f)),rayHeigh)
            };
            var start1 = list[0];
            var start2 = list[1];
            //list.Reverse(); //Wenn diese Zeile drin ist, wird der Test rot da die Schnittpunkte von den beiden RayTriangles den gleichen Abstand zu ray2.Start haben und somit die Reihenfolge entscheidend ist

            var ray1 = new Ray(new Vector3D(0.40533337f, 0f, -0.187333345f), new Vector3D(-0.100109957f, 0.799263f, -0.592584789f));
            var ray2 = new Ray(new Vector3D(0.395000011f, 0.0825000033f, -0.248500004f), new Vector3D(0.100109957f, -0.799263f, 0.592584789f));

            LinearSearchIntersector lin = new LinearSearchIntersector(list);

            //Erst laufe ich von ray1.Start nach point1/ray2.start
            //Dann laufe ich von point1/ray2.start zurück nach ray1.Start/point2
            var point1 = lin.GetIntersectionPoint(ray1, start1, float.MaxValue, 0);
            var point2 = lin.GetIntersectionPoint(ray2, start2, float.MaxValue, 0);

            Assert.IsTrue((point1.Position - ray2.Start).Length() < 0.0001f);
            Assert.IsTrue((point2.Position - ray1.Start).Length() < 0.0001f);

            Assert.AreEqual(point1.IntersectedObject, start2);
            Assert.AreEqual(point2.IntersectedObject, start1);
        }
    }
}
