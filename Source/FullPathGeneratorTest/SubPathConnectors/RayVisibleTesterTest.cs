using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using RaytracingLightSource;
using SubpathGenerator;

//Testfälle für den RayVisibleTester:
//Hinweis: Nicht Intersectable bedeutet die Lichtquelle besitzt zwar schon ein RayTriangle aber das ist kein Element des IntersectionFinders
//RaytracerSimple: Zwischen IntersectionPoint (Surface) und IIntersectableRayDrawingObject (Surface)
//	Testcase 1: Parallax-Schatten beim IntersectionPoint
//	Testcase 2: BlockingObject
//	Testcase 3: Strahl trift Dreieck der Lichtquelle
//DirectLighting (NoMedia): Zwischen IntersectionPoint und DirectLightingSampleResult
//	Testcase 4: Parallax-Schatten beim IntersectionPoint
//	Testcase 5: Lichtquelle ist Intersectable; BlockingObject
//	Testcase 6: Lichtquelle ist Intersectable; Lichtquelle wird getroffen 
//	Testcase 7: Lichtquelle ist nicht Intersectable; BlockingObject
//	Testcase 8: Lichtquelle ist nicht Intersectable; Weg zur Lichtquelle ist frei
//DirectLighting (WithMedia): Zwischen IntersectionPoint und DirectLightingSampleResult
//	Testcase 9: Parallax-Schatten beim IntersectionPoint
//	Testcase 10: Lichtquelle ist Intersectable; Zwischen Surface und Lichtquelle befindet sich NullMediaObjekt (Wolke)
//	Testcase 11: Lichtquelle ist Intersectable; Zwischen Surface und Lichtquelle befindet sich MediaObjekt (Glas)
//	Testcase 12: Lichtquelle ist nicht Intersectable(EnvironmentLight); Mit Global Media; Air-BlockingObject
//	Testcase 13: Lichtquelle ist nicht Intersectable(EnvironmentLight); Ohne Global Media; Air-BlockingObject
//	Testcase 14: Lichtquelle ist nicht Intersectable(EnvironmentLight); Ohne Global Media; kein BlockingObject
//	Testcase 15: Lichtquelle ist nicht Intersectable(EnvironmentLight); Mit Global Media; Kein BlockingObject
//	Testcase 16: Lichtquelle ist nicht Intersectable(FarAwayLight); Ohne Global Media; Zwischen Surface und Lichtquelle befindet sich Atmospährenkugel
//	Testcase 17: Lichtquelle ist nicht Intersectable(FarAwayLight); Ohne Global Media; Zwischen Surface und Lichtquelle befindet sich MediaObjekt (Glas)
//	Testcase 18: Lichtquelle ist nicht Intersectable(FarAwayLight); Ohne Global Media; kein BlockingObject
//	Testcase 19: Lichtquelle ist nicht Intersectable(FarAwayLight); Mit Global Media; kein BlockingObject
//LightTracing (NoMedia): Zwischen IntersectionPoint (MUSS Punkt der Szene sein) und CameraPosition
//	Testcase 20: Parallax-Schatten beim IntersectionPoint
//	Testcase 21: Zwischen IntersectionPoint und CameraPosition ist BlockingObject
//	Testcase 22: Zwischen IntersectionPoint und CameraPosition ist kein BlockingObject
//LightTracing (With Media): Zwischen Camera-MediaPoint und Light-PathPoint(Surface/Partikel)
//	Testcase 23: Parallax-Schatten beim IntersectionPoint
//	Testcase 24: Zwischen Camera-MediaPoint und Light-SurfaccePunkt ist MediaObjekt (Glas)
//	Testcase 25: Zwischen Camera-MediaPoint und Light-SurfaccePunkt ist Null-MediaObjekt (Wolke)
//	Testcase 26: Zwischen Camera-MediaPoint und Light-PartikelPunkt ist Null-MediaObjekt (Wolke)
//VertexConnection (NoMedia): Zwischen Eye-IntersectionPoint und Light-IntersectionPoint
//	Testcase 27: Parallax-Schatten beim Eye-IntersectionPoint
//	Testcase 28: Parallax-Schatten beim Light-IntersectionPoint
//	Testcase 29: Zwischen Eye-IntersectionPoint und Light-IntersectionPoint ist BlockingObject
//	Testcase 30: Zwischen Eye-IntersectionPoint und Light-IntersectionPoint ist kein BlockingObject
//VertexConnection (With Media)
//	Testcase 31: Parallax-Schatten beim Eye-SurfacePunkt
//	Testcase 32: Parallax-Schatten beim Light-SurfacePunkt
//	Testcase 33: Zwischen Eye-SurfacePunkt und Light-SurfacePunkt ist MediaObjekt (Glas)
//	Testcase 34: Zwischen Eye-SurfacePunkt und Light-SurfacePunkt ist Null-MediaObjekt (Wolke)
//	Testcase 35: Zwischen Eye-SurfacePunkt und Light-ParticlePunkt ist Null-MediaObjekt (Wolke)

//Für die ParallaxTests verwende ich eine 3x3 große Bumpmap wo alle Pixel die Höhe 1 haben außer der in der Mitte hat die Höhe 0

namespace FullPathGeneratorTest.SubPathConnectors
{
    [TestClass]
    public class RayVisibleTesterTest
    {
        [TestMethod]
        public void RaytracerSimple_ParallaxShadow() //Testcase 1
        {
            TestData d = new TestData(new TestInput() {GroundYRotation = 45 });
            var lightPoint = d.RayVisibleTester.GetPointOnIntersectableLight(d.EyePoint, 0, d.LightPoint.Position, d.LightPoint.IntersectedRayHeigh);
            Assert.IsNull(lightPoint);
        }

        [TestMethod]
        public void DirectLighting_NoMedia_BlockingObject() //Testcase 2
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Solid });
            var lightPoint = d.RayVisibleTester.GetPointOnIntersectableLight(d.EyePoint, 0, d.LightPoint.Position, d.LightPoint.IntersectedRayHeigh);
            Assert.IsNull(lightPoint);
        }

        [TestMethod]
        public void RaytracerSimple_NoBlockingObject() //Testcase 3
        {
            TestData d = new TestData(new TestInput());
            var lightPoint = d.RayVisibleTester.GetPointOnIntersectableLight(d.EyePoint, 0, d.LightPoint.Position, d.LightPoint.IntersectedRayHeigh);
            Assert.AreEqual(10, lightPoint.Position.Y);
            Assert.AreEqual(d.LightPoint.IntersectedRayHeigh, lightPoint.IntersectedRayHeigh);
        }

        [TestMethod]
        public void DirectLightingNoMedia_ParallaxShadow() //Testcase 4
        {
            TestData d = new TestData(new TestInput() { GroundYRotation = 45, IsLightIntersectable = true });
            var lightPoint = d.RayVisibleTester.GetPointOnLightsource(d.EyePoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0,1,0),
                IsLightIntersectable = true,
                LightSource = d.LightPoint.IntersectedRayHeigh
            });
            Assert.IsNull(lightPoint);
        }

        [TestMethod]
        public void DirectLightingNoMedia_BlockingObject() //Testcase 5
        {
            TestData d = new TestData(new TestInput() {Blocking = TestInput.Block.Solid, IsLightIntersectable = true });
            var lightPoint = d.RayVisibleTester.GetPointOnLightsource(d.EyePoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = true,
                LightSource = d.LightPoint.IntersectedRayHeigh
            });
            Assert.IsNull(lightPoint);
        }

        [TestMethod]
        public void DirectLightingNoMedia_NoBlockingObject() //Testcase 6
        {
            TestData d = new TestData(new TestInput() { IsLightIntersectable = true });
            var lightPoint = d.RayVisibleTester.GetPointOnLightsource(d.EyePoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = true,
                LightSource = d.LightPoint.IntersectedRayHeigh
            });
            Assert.AreEqual(10, lightPoint.Position.Y);
            Assert.AreEqual(d.LightPoint.IntersectedRayHeigh, lightPoint.IntersectedRayHeigh);
        }

        [TestMethod]
        public void DirectLightingNoMedia_VirtualLight_BlockingObject() //Testcase 7
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Solid, IsLightIntersectable = false });
            var lightPoint = d.RayVisibleTester.GetPointOnLightsource(d.EyePoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = false,
                LightSource = d.LightPoint.IntersectedRayHeigh,
                LightPointIfNotIntersectable = d.LightPoint
            });
            Assert.IsNull(lightPoint);
        }

        [TestMethod]
        public void DirectLightingNoMedia_VirtualLight_NoBlockingObject() //Testcase 8
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.No, IsLightIntersectable = false });
            var lightPoint = d.RayVisibleTester.GetPointOnLightsource(d.EyePoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = false,
                LightSource = d.LightPoint.IntersectedRayHeigh,
                LightPointIfNotIntersectable = d.LightPoint
            });
            Assert.AreEqual(10, lightPoint.Position.Y);
            Assert.AreEqual(d.LightPoint.IntersectedRayHeigh, lightPoint.IntersectedRayHeigh);
        }

        [TestMethod]
        public void DirectLightingWithMedia_ParallaxShadow() //Testcase 9
        {
            TestData d = new TestData(new TestInput() { GroundYRotation = 45, IsLightIntersectable = true, CreateMediaIntersectionFinder = true });
            var toLightMediaLine = d.RayVisibleTester.GetLineToLightSource(d.EyeMediaPoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = true,
                LightSource = d.LightPoint.IntersectedRayHeigh
            });
            Assert.IsNull(toLightMediaLine);
        }

        [TestMethod]
        public void DirectLightingWitMedia_AirBlockingObject() //Testcase 10
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Air, IsLightIntersectable = true, CreateMediaIntersectionFinder = true });
            var toLightMediaLine = d.RayVisibleTester.GetLineToLightSource(d.EyeMediaPoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = true,
                LightSource = d.LightPoint.IntersectedRayHeigh,
            });
            
            Assert.AreEqual(4.5f, toLightMediaLine.Segments[0].SegmentLength);  //Vacuum
            Assert.AreEqual(1, toLightMediaLine.Segments[1].SegmentLength);     //Luft
            Assert.AreEqual(4.5f, toLightMediaLine.Segments[2].SegmentLength);  //Vacuum
            Assert.AreEqual(10, toLightMediaLine.EndPoint.Position.Y);
            Assert.AreEqual(d.LightPoint.IntersectedRayHeigh, toLightMediaLine.EndPoint.SurfacePoint.IntersectedRayHeigh);
        }

        [TestMethod]
        public void DirectLightingWitMedia_GlasBlockingObject() //Testcase 11
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Glas, IsLightIntersectable = true, CreateMediaIntersectionFinder = true });
            var toLightMediaLine = d.RayVisibleTester.GetLineToLightSource(d.EyeMediaPoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = true,
                LightSource = d.LightPoint.IntersectedRayHeigh,
            });
            Assert.IsNull(toLightMediaLine);
        }

        [TestMethod]
        public void DirectLightingWitMedia_EnvironmentLight_GlobalMedia_AirBlockingObject() //Testcase 12
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Air, IsLightIntersectable = false, CreateMediaIntersectionFinder = true, HasGlobalMedia = true });
            var toLightMediaLine = d.RayVisibleTester.GetLineToLightSource(d.EyeMediaPoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = false,
                LightSource = d.LightPoint.IntersectedRayHeigh,
                LightPointIfNotIntersectable = d.LightPoint,
                LightSourceIsInfinityAway = false
            });

            Assert.AreEqual(4.5f, toLightMediaLine.Segments[0].SegmentLength);  //GlobalMedia
            Assert.AreEqual(1, toLightMediaLine.Segments[1].SegmentLength);     //Luft
            Assert.AreEqual(4.5f, toLightMediaLine.Segments[2].SegmentLength);  //GlobalMedia
            Assert.AreEqual(10, toLightMediaLine.EndPoint.Position.Y);
            Assert.AreEqual(d.LightPoint.IntersectedRayHeigh, toLightMediaLine.EndPoint.SurfacePoint.IntersectedRayHeigh);
        }

        [TestMethod]
        public void DirectLightingWitMedia_EnvironmentLight_NoGlobalMedia_AirBlockingObject() //Testcase 13
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Air, IsLightIntersectable = false, CreateMediaIntersectionFinder = true, HasGlobalMedia = false });
            var toLightMediaLine = d.RayVisibleTester.GetLineToLightSource(d.EyeMediaPoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = false,
                LightSource = d.LightPoint.IntersectedRayHeigh,
                LightPointIfNotIntersectable = d.LightPoint,
                LightSourceIsInfinityAway = false
            });

            Assert.AreEqual(4.5f, toLightMediaLine.Segments[0].SegmentLength);  //Vacuum
            Assert.AreEqual(1, toLightMediaLine.Segments[1].SegmentLength);     //Luft
            Assert.AreEqual(4.5f, toLightMediaLine.Segments[2].SegmentLength);  //Vacuum
            Assert.AreEqual(10, toLightMediaLine.EndPoint.Position.Y);
            Assert.AreEqual(d.LightPoint.IntersectedRayHeigh, toLightMediaLine.EndPoint.SurfacePoint.IntersectedRayHeigh);
        }

        [TestMethod]
        public void DirectLightingWitMedia_EnvironmentLight_NoGlobalMedia_NoBlockingObject() //Testcase 14
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.No, IsLightIntersectable = false, CreateMediaIntersectionFinder = true, HasGlobalMedia = false });
            var toLightMediaLine = d.RayVisibleTester.GetLineToLightSource(d.EyeMediaPoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = false,
                LightSource = d.LightPoint.IntersectedRayHeigh,
                LightPointIfNotIntersectable = d.LightPoint,
                LightSourceIsInfinityAway = false
            });

            Assert.AreEqual(10, toLightMediaLine.Segments[0].SegmentLength);  //Vacuum
            Assert.AreEqual(10, toLightMediaLine.EndPoint.Position.Y);
            Assert.AreEqual(d.LightPoint.IntersectedRayHeigh, toLightMediaLine.EndPoint.SurfacePoint.IntersectedRayHeigh);
        }

        [TestMethod]
        public void DirectLightingWitMedia_EnvironmentLight_WithGlobalMedia_NoBlockingObject() //Testcase 15
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.No, IsLightIntersectable = false, CreateMediaIntersectionFinder = true, HasGlobalMedia = true });
            var toLightMediaLine = d.RayVisibleTester.GetLineToLightSource(d.EyeMediaPoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = false,
                LightSource = d.LightPoint.IntersectedRayHeigh,
                LightPointIfNotIntersectable = d.LightPoint,
                LightSourceIsInfinityAway = false
            });

            Assert.AreEqual(10, toLightMediaLine.Segments[0].SegmentLength);  //GlobalMedia
            Assert.AreEqual(10, toLightMediaLine.EndPoint.Position.Y);
            Assert.AreEqual(d.LightPoint.IntersectedRayHeigh, toLightMediaLine.EndPoint.SurfacePoint.IntersectedRayHeigh);
        }

        [TestMethod]
        public void DirectLightingWitMedia_SunLight_NoGlobalMedia_AirBlockingObject() //Testcase 16
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Air, IsLightIntersectable = false, CreateMediaIntersectionFinder = true, HasAtmosphereSpere = true });
            var toLightMediaLine = d.RayVisibleTester.GetLineToLightSource(d.EyeMediaPoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = false,
                LightSource = d.LightPoint.IntersectedRayHeigh,
                LightPointIfNotIntersectable = d.LightPoint,
                LightSourceIsInfinityAway = true
            });

            Assert.AreEqual(4.5f, toLightMediaLine.Segments[0].SegmentLength);  //Luft
            Assert.AreEqual(1, toLightMediaLine.Segments[1].SegmentLength);     //Wolke
            Assert.AreEqual(1.5f, toLightMediaLine.Segments[2].SegmentLength);  //Luft
            Assert.AreEqual(3, toLightMediaLine.Segments[3].SegmentLength);     //Vacuum
            Assert.AreEqual(10, toLightMediaLine.EndPoint.Position.Y);
            Assert.AreEqual(d.LightPoint.IntersectedRayHeigh, toLightMediaLine.EndPoint.SurfacePoint.IntersectedRayHeigh);
        }

        [TestMethod]
        public void DirectLightingWitMedia_SunLight_NoGlobalMedia_BlockingObject() //Testcase 17
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Glas, IsLightIntersectable = false, CreateMediaIntersectionFinder = true, HasAtmosphereSpere = true });
            var toLightMediaLine = d.RayVisibleTester.GetLineToLightSource(d.EyeMediaPoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = false,
                LightSource = d.LightPoint.IntersectedRayHeigh,
                LightPointIfNotIntersectable = d.LightPoint,
                LightSourceIsInfinityAway = true
            });

            Assert.IsNull(toLightMediaLine);
        }

        [TestMethod]
        public void DirectLightingWitMedia_SunLight_NoGlobalMedia_NoBlockingObject() //Testcase 18
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.No, IsLightIntersectable = false, CreateMediaIntersectionFinder = true});
            var toLightMediaLine = d.RayVisibleTester.GetLineToLightSource(d.EyeMediaPoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = false,
                LightSource = d.LightPoint.IntersectedRayHeigh,
                LightPointIfNotIntersectable = d.LightPoint,
                LightSourceIsInfinityAway = true
            });

            Assert.AreEqual(10, toLightMediaLine.Segments[0].SegmentLength);  //Vacuum
            Assert.AreEqual(10, toLightMediaLine.EndPoint.Position.Y);
            Assert.AreEqual(d.LightPoint.IntersectedRayHeigh, toLightMediaLine.EndPoint.SurfacePoint.IntersectedRayHeigh);

        }

        [TestMethod]
        public void DirectLightingWitMedia_SunLight_WithGlobalMedia_NoBlockingObject() //Testcase 19
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.No, IsLightIntersectable = false, CreateMediaIntersectionFinder = true, HasGlobalMedia = true, HasAtmosphereSpere = true });
            var toLightMediaLine = d.RayVisibleTester.GetLineToLightSource(d.EyeMediaPoint, 0, new DirectLightingSampleResult()
            {
                DirectionToLightPoint = new Vector3D(0, 1, 0),
                IsLightIntersectable = false,
                LightSource = d.LightPoint.IntersectedRayHeigh,
                LightPointIfNotIntersectable = d.LightPoint,
                LightSourceIsInfinityAway = true
            });

            Assert.IsNull(toLightMediaLine);
        }

        [TestMethod]
        public void LightTracingNoMedia_ParallaxShadow() //Testcase 20
        {
            TestData d = new TestData(new TestInput() {  LightYRotation = 45 });
            bool isVisible = d.RayVisibleTester.IsCameraVisible(d.LightPoint, 0, d.CameraPosition);
            Assert.IsFalse(isVisible);
        }

        [TestMethod]
        public void LightTracingNoMedia_WithBlockingObject() //Testcase 21
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Solid});
            bool isVisible = d.RayVisibleTester.IsCameraVisible(d.LightPoint, 0, d.CameraPosition);
            Assert.IsFalse(isVisible);
        }

        [TestMethod]
        public void LightTracingNoMedia_NoBlockingObject() //Testcase 22
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.No });
            bool isVisible = d.RayVisibleTester.IsCameraVisible(d.LightPoint, 0, d.CameraPosition);
            Assert.IsTrue(isVisible);
        }

        [TestMethod]
        public void LightTracingWithMedia_ParallaxShadow() //Testcase 23
        {
            TestData d = new TestData(new TestInput() { LightYRotation = 45, CreateMediaIntersectionFinder = true });
            var toLightMediaLine = d.RayVisibleTester.GetLineFromCameraToLightPoint(
                d.CameraMediaPoint, 
                Vector3D.Normalize(d.LightPoint.Position - d.CameraPosition), 
                (d.LightPoint.Position - d.CameraPosition).Length(),
                PathPoint.CreateLightsourcePointWithSurroundingMedia(d.LightPoint, null, d.LightMediaPoint, false)
                );
            Assert.IsNull(toLightMediaLine);
        }

        [TestMethod]
        public void LightTracingWithMedia_ToSurfaceLightPoint_GlasBlocking() //Testcase 24
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Glas, CreateMediaIntersectionFinder = true });

            PathPoint surfaceLightPoint = PathPoint.CreateLightsourcePointWithSurroundingMedia(d.LightPoint, null, d.LightMediaPoint, false);
            surfaceLightPoint.AssociatedPath = new SubPath(null, 0);

            var toLightMediaLine = d.RayVisibleTester.GetLineFromCameraToLightPoint(
                d.CameraMediaPoint,
                Vector3D.Normalize(d.LightPoint.Position - d.CameraPosition),
                (d.LightPoint.Position - d.CameraPosition).Length(),
                surfaceLightPoint
                );
            Assert.IsNull(toLightMediaLine);
        }

        [TestMethod]
        public void LightTracingWithMedia_ToSurfaceLightPoint_AirBlocking() //Testcase 25
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Air, CreateMediaIntersectionFinder = true });

            PathPoint surfaceLightPoint = PathPoint.CreateLightsourcePointWithSurroundingMedia(d.LightPoint, null, d.LightMediaPoint, false);
            surfaceLightPoint.AssociatedPath = new SubPath(null, 0);

            var toLightMediaLine = d.RayVisibleTester.GetLineFromCameraToLightPoint(
                d.CameraMediaPoint,
                Vector3D.Normalize(d.LightPoint.Position - d.CameraPosition),
                (d.LightPoint.Position - d.CameraPosition).Length(),
                surfaceLightPoint
                );

            Assert.AreEqual(3.5f, toLightMediaLine.Segments[0].SegmentLength);  //Vacuum
            Assert.AreEqual(1, toLightMediaLine.Segments[1].SegmentLength);     //Wolke
            Assert.AreEqual(4.5f, toLightMediaLine.Segments[2].SegmentLength);  //Vacuum
            Assert.AreEqual(10, toLightMediaLine.EndPoint.Position.Y);
            Assert.AreEqual(d.LightPoint.IntersectedRayHeigh, toLightMediaLine.EndPoint.SurfacePoint.IntersectedRayHeigh);
        }

        [TestMethod]
        public void LightTracingWithMedia_ToParticleLightPoint_AirBlocking() //Testcase 26
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Air, CreateMediaIntersectionFinder = true, HasGlobalMedia = true });

            PathPoint particleLightPoint = PathPoint.CreateMediaParticlePoint(MediaIntersectionPoint.CreateMediaPoint(d.CameraMediaPoint, new Vector3D(0,9,0), MediaPointLocationType.MediaParticle), null);
            particleLightPoint.AssociatedPath = new SubPath(null, 0);

            var toLightMediaLine = d.RayVisibleTester.GetLineFromCameraToLightPoint(
                d.CameraMediaPoint,
                Vector3D.Normalize(particleLightPoint.Position - d.CameraPosition),
                (particleLightPoint.Position - d.CameraPosition).Length(),
                particleLightPoint
                );

            Assert.AreEqual(3.5f, toLightMediaLine.Segments[0].SegmentLength);  //GlobalMedia
            Assert.AreEqual(1, toLightMediaLine.Segments[1].SegmentLength);     //Wolke
            Assert.AreEqual(3.5f, toLightMediaLine.Segments[2].SegmentLength);  //GlobalMedia
            Assert.AreEqual(9, toLightMediaLine.EndPoint.Position.Y);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, toLightMediaLine.EndPoint.Location);
        }

        [TestMethod]
        public void VertexConnectionNoMedia_ParallaxShadowOnEyePoint() //Testcase 27
        {
            TestData d = new TestData(new TestInput() { GroundYRotation = 45 });
            bool isVisible = d.RayVisibleTester.IsVisibleFromSurfaceToSurface(d.EyePoint, 0, Vector3D.Normalize(d.LightPoint.Position - d.EyePoint.Position), d.LightPoint);
            Assert.IsFalse(isVisible);
        }

        [TestMethod]
        public void VertexConnectionNoMedia_ParallaxShadowOnLightPoint() //Testcase 28
        {
            TestData d = new TestData(new TestInput() { LightYRotation = 45 });
            bool isVisible = d.RayVisibleTester.IsVisibleFromSurfaceToSurface(d.EyePoint, 0, Vector3D.Normalize(d.LightPoint.Position - d.EyePoint.Position), d.LightPoint);
            Assert.IsFalse(isVisible);
        }

        [TestMethod]
        public void VertexConnectionNoMedia_WithBlockingObject() //Testcase 29
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Solid });
            bool isVisible = d.RayVisibleTester.IsVisibleFromSurfaceToSurface(d.EyePoint, 0, Vector3D.Normalize(d.LightPoint.Position - d.EyePoint.Position), d.LightPoint);
            Assert.IsFalse(isVisible);
        }

        [TestMethod]
        public void VertexConnectionNoMedia_NoBlockingObject() //Testcase 30
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.No });
            bool isVisible = d.RayVisibleTester.IsVisibleFromSurfaceToSurface(d.EyePoint, 0, Vector3D.Normalize(d.LightPoint.Position - d.EyePoint.Position), d.LightPoint);
            Assert.IsTrue(isVisible);
        }

        [TestMethod]
        public void VertexConnectionWithMedia_ParallaxShadowOnEyePoint() //Testcase 31
        {
            TestData d = new TestData(new TestInput() { GroundYRotation = 45, CreateMediaIntersectionFinder = true });

            PathPoint surfaceEyePoint = PathPoint.CreateLightsourcePointWithSurroundingMedia(d.EyePoint, null, d.EyeMediaPoint, false);
            surfaceEyePoint.AssociatedPath = new SubPath(null, 0);

            PathPoint surfaceLightPoint = PathPoint.CreateLightsourcePointWithSurroundingMedia(d.LightPoint, null, d.LightMediaPoint, false);
            surfaceLightPoint.AssociatedPath = new SubPath(null, 0);

            var lintFromEyeToLight = d.RayVisibleTester.GetLineFromP1ToP2(surfaceEyePoint, Vector3D.Normalize(d.LightPoint.Position - d.EyePoint.Position), (d.LightPoint.Position - d.EyePoint.Position).Length(), surfaceLightPoint);
            Assert.IsNull(lintFromEyeToLight);
        }

        [TestMethod]
        public void VertexConnectionWithMedia_ParallaxShadowOnLightPoint() //Testcase 32
        {
            TestData d = new TestData(new TestInput() { LightYRotation = 45, CreateMediaIntersectionFinder = true });

            PathPoint surfaceEyePoint = PathPoint.CreateLightsourcePointWithSurroundingMedia(d.EyePoint, null, d.EyeMediaPoint, false);
            surfaceEyePoint.AssociatedPath = new SubPath(null, 0);

            PathPoint surfaceLightPoint = PathPoint.CreateLightsourcePointWithSurroundingMedia(d.LightPoint, null, d.LightMediaPoint, false);
            surfaceLightPoint.AssociatedPath = new SubPath(null, 0);

            var lintFromEyeToLight = d.RayVisibleTester.GetLineFromP1ToP2(surfaceEyePoint, Vector3D.Normalize(d.LightPoint.Position - d.EyePoint.Position), (d.LightPoint.Position - d.EyePoint.Position).Length(), surfaceLightPoint);
            Assert.IsNull(lintFromEyeToLight);
        }

        [TestMethod]
        public void VertexConnectionWithMedia_SurfaceToSurface_GlasBlockingObject() //Testcase 33
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Glas, CreateMediaIntersectionFinder = true });

            PathPoint surfaceEyePoint = PathPoint.CreateLightsourcePointWithSurroundingMedia(d.EyePoint, null, d.EyeMediaPoint, false);
            surfaceEyePoint.AssociatedPath = new SubPath(null, 0);

            PathPoint surfaceLightPoint = PathPoint.CreateLightsourcePointWithSurroundingMedia(d.LightPoint, null, d.LightMediaPoint, false);
            surfaceLightPoint.AssociatedPath = new SubPath(null, 0);

            var lintFromEyeToLight = d.RayVisibleTester.GetLineFromP1ToP2(surfaceEyePoint, Vector3D.Normalize(d.LightPoint.Position - d.EyePoint.Position), (d.LightPoint.Position - d.EyePoint.Position).Length(), surfaceLightPoint);
            Assert.IsNull(lintFromEyeToLight);
        }

        [TestMethod]
        public void VertexConnectionWithMedia_SurfaceToSurface_AirBlockingObject() //Testcase 34
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Air, CreateMediaIntersectionFinder = true });

            PathPoint surfaceEyePoint = PathPoint.CreateLightsourcePointWithSurroundingMedia(d.EyePoint, null, d.EyeMediaPoint, false);
            surfaceEyePoint.AssociatedPath = new SubPath(null, 0);

            PathPoint surfaceLightPoint = PathPoint.CreateLightsourcePointWithSurroundingMedia(d.LightPoint, null, d.LightMediaPoint, false);
            surfaceLightPoint.AssociatedPath = new SubPath(null, 0);

            var lintFromEyeToLight = d.RayVisibleTester.GetLineFromP1ToP2(surfaceEyePoint, Vector3D.Normalize(d.LightPoint.Position - d.EyePoint.Position), (d.LightPoint.Position - d.EyePoint.Position).Length(), surfaceLightPoint);

            Assert.AreEqual(4.5f, lintFromEyeToLight.Segments[0].SegmentLength);  //Vacuum
            Assert.AreEqual(1, lintFromEyeToLight.Segments[1].SegmentLength);     //Wolke
            Assert.AreEqual(4.5f, lintFromEyeToLight.Segments[2].SegmentLength);  //Vacuum
            Assert.AreEqual(10, lintFromEyeToLight.EndPoint.Position.Y);
            Assert.AreEqual(d.LightPoint.IntersectedRayHeigh, lintFromEyeToLight.EndPoint.SurfacePoint.IntersectedRayHeigh);
        }

        [TestMethod]
        public void VertexConnectionWithMedia_SurfaceToParticle_AirBlockingObject() //Testcase 35
        {
            TestData d = new TestData(new TestInput() { Blocking = TestInput.Block.Air, CreateMediaIntersectionFinder = true, HasGlobalMedia = true });

            PathPoint surfaceEyePoint = PathPoint.CreateLightsourcePointWithSurroundingMedia(d.EyePoint, null, d.EyeMediaPoint, false);
            surfaceEyePoint.AssociatedPath = new SubPath(null, 0);

            PathPoint particleLightPoint = PathPoint.CreateMediaParticlePoint(MediaIntersectionPoint.CreateMediaPoint(d.CameraMediaPoint, new Vector3D(0, 9, 0), MediaPointLocationType.MediaParticle), null);
            particleLightPoint.AssociatedPath = new SubPath(null, 0);

            var lintFromEyeToLight = d.RayVisibleTester.GetLineFromP1ToP2(surfaceEyePoint, Vector3D.Normalize(particleLightPoint.Position - d.EyePoint.Position), (particleLightPoint.Position - d.EyePoint.Position).Length(), particleLightPoint);

            Assert.AreEqual(4.5f, lintFromEyeToLight.Segments[0].SegmentLength);  //Vacuum
            Assert.AreEqual(1, lintFromEyeToLight.Segments[1].SegmentLength);     //Wolke
            Assert.AreEqual(3.5f, lintFromEyeToLight.Segments[2].SegmentLength);  //Vacuum
            Assert.AreEqual(9, lintFromEyeToLight.EndPoint.Position.Y);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, lintFromEyeToLight.EndPoint.Location);
        }
    }
}
