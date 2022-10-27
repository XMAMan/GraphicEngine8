using GraphicGlobal;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using ParticipatingMediaTest.MediaMocks;
using RayTracerGlobal;
using SubpathGenerator;
using System;
using System.Collections.Generic;

namespace SubpathGeneratorTest.PathPointPropertys
{
    //1 Strahl fliegt ins Leere und es gibt Umgebungslicht -> Gebe Punkt mit Umgebungslicht zurück (Bei Environmentlight hat man immer kein GlobalMedia, da die Attenuation immer 0 wäre)
    //2 Strahl fliegt ins Leere und es gibt kein Umgebungslicht und GlobalMedia -> Gebe nur Kamerapunkt zurück
    //3 Strahl fliegt ins Leere und es gibt GlobalMedia -> Gebe Infinity-Punkt zurück
    //4 Strahl trift Diffuse-Lichtquelle -> Gebe Punkt auf Lichtquelle zurück
    //5 Strahl trift Diffuse-Surface und wird Reflektiert und trift Lichtquelle -> C D L Letzter Punkt liegt im GlobalMedia
    //6 Strahl trift Surface(Glas ohne Media) und wird Reflektiert und trift Lichtquelle -> C S L Letzter Punkt liegt im GlobalMedia
    //7 Strahl trift Surface(Glas ohne Media) und wird gebrochen (Brechungsindex > 1) und trift andere Glaswand -> C S S Letzter Punkt liegt im Glasmedium 
    //8 Strahl trift Glas-Würfel ohne Media, welcher in ein Luftwürfel liegt. Strahl wird gebrochen (Brechungsindex > 1) und trift andere Glaswand -> C S S Letzter Punkt liegt im Glasmedium 
    //9 Strahl trift MediaBorder(Glas mit Media) und wird Reflektiert und trift Lichtquelle -> C B L Letzter Punkt liegt im GlobalMedia
    //10 Strahl trift MediaBorder(Glas mit Media) und wird gebrochen (Brechungsindex > 1) und trift andere Glaswand -> C B B Letzter Punkt liegt im Glasmedium 
    //11 Shortray: Strahl trift Partikel in Wolke(MediaBroder mit Brechungsindex 1) und wird absorbiert -> CP
    //12 Longray: Strahl trift Partikel in Wolke(MediaBroder mit Brechungsindex 1) und wird absorbiert -> CP
    //13 Strahl trift Partikel in GlobalMedia und wird absorbiert -> C P 
    //14 Distanzsampling liefert Wert nahe 0 -> Es wird nur Kamerapunkt zurück gegeben
    //15 Kein Distanzsampling; Strahl trifft Media-Luft-Kugel in Mitte und durchläuft sie -> CB mit 2 Segmenten

    //Extratestfälle, welche ich nicht aus den zu testenden Quelltext extrahiert habe sondern aus den Ansatz2-UnitTests
    //Tests mit Air-Würfel (Media-Würfel mit Brechungsindex von 1)
    //16 Longray; Strahl trifft Media-Luft-Kugel ganz oben und trifft nur einmal obwohl Kugel durchlaufen wird -> CB mit 1 Segment
    //17 Ohne Global Media; Ohne Distanzsampling; Strahl durchfliegt Kugel ganz oben, so das KugelSegment < MinAllowedPathPointDistanz ist
    //18 Ohne Global Media; Ohne Distanzsampling; Strahl durchfliegt zwei Media-Luft-Würfel wo Vacuum dahinter ist
    //19 Ohne Global Media; Ohne Distanzsampling; Strahl durchfliegt Media-Luft-Würfel wo Wand mit Abstand dahinter ist
    //20 Ohne Global Media; Ohne Distanzsampling;Kamera schaut auf Media-Würfel, der Wand überschneidet 
    //21 Ohne Global Media; Ohne Distanzsampling;Kamera schaut auf Media-Würfel, der Wand überschneidet. Lichtstrahl wird an Wand reflektiert
    //22 Mit Global Media; Ohne Distanzsampling;Kamera befindet sich im GlobalMedia und schaut auf Media-Air-Würfel
    //23 Strahl durchläuft Wasser-Glas, wo das Glas auch ein Medium enthält und das Wasser eine niedrigere Prio als das Glas hat
    //24 Ohne Global Media; Ohne Distanzsampling; Strahl durchfliegt Media-Luft-Würfel an der Ecke, so das Abstand zwischen Ein- und Ausrittspunkt kleiner MinAllowedDistance ist
    //25 Ohne Global Media; Ohne Distanzsampling; Strahl durchläuft Media-Würfel und trifft danach auf Umgebungslicht -> Gebe Punkt mit Umgebungslicht zurück (Bei Environmentlight hat man immer kein GlobalMedia, da die Attenuation immer 0 wäre)

    //Tests, die ich absichtlich ignoriere
    //Kamera schaut auf Media-Würfel, der kein Abstand zur Wand dahinter hat. Lichtstrahl wird an Wand reflektiert (Kamera-Y-Starthöhe ist 0.5)
    //Ohne Global Media; Ohne Distanzsampling; Strahl durchfliegt Media-Luft-Würfel wo Wand ohne Abstand dahinter ist [Ignore]

    [TestClass]
    public class MediaSubpathSamplerTest
    {

        [TestMethod]
        public void SampleCameraPoints_GoThroughVacuumAndHitEnvironmentLight() //1 Strahl fliegt ins Leere und es gibt Umgebungslicht -> Gebe Punkt mit Umgebungslicht zurück (Bei Environmentlight hat man immer kein GlobalMedia, da die Attenuation immer 0 wäre)
        {
            float cameraY = 0;
            Vector3D startPoint = new Vector3D(-2, cameraY, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 3, Radius = 1, ScatteringCoeffizient = 0.0f, Material = BrdfModel.Diffus, LightsourceData = new  EnvironmentLightDescription() },//Umgebungslicht
                },
                0), //Ohne GlobalMedia
                SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(2, cameraY, 0) , ReturnValueForDirectionSampling = null },
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckEnvironmentLightPoint(points[1], 1);

            var line = points[0].LineToNextPoint;
            Assert.AreEqual(MediaPointLocationType.Camera, line.StartPoint.Location);
            Assert.AreEqual(startPoint, line.StartPoint.Position);
            Assert.AreEqual(MediaPointLocationType.Surface, line.EndPoint.Location);
            Assert.AreEqual(1, line.Segments.Count);

            SubpathTestHelper.CheckSegment(line.Segments[0], 0, line.ShortRayLength, 0);//Global-Media-Segment
        }

        [TestMethod]
        public void SampleCameraPoints_HitNoObjectWithoutGlobalMedia_ReturnC() //2 Strahl fliegt ins Leere und es gibt kein Umgebungslicht und GlobalMedia -> Gebe nur Kamerapunkt zurück
        {
            Vector3D startPoint = new Vector3D(-2, 3, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0}//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                }, 
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                null,
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(1, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);

            Assert.IsNull(points[0].LineToNextPoint);
        }

        [TestMethod]
        public void SampleCameraPoints_HitNoObjectWithGlobalMedia_ReturnCI() //3 Strahl fliegt ins Leere und es gibt GlobalMedia -> Gebe Infinity-Punkt zurück
        {
            Vector3D startPoint = new Vector3D(-2, 3, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0}//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                }, 
                0.2f), //GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                null,
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0.2f);
            SubpathTestHelper.CheckMediaInfinityPoint(points[1], 0.2f, 1);

            Assert.IsTrue(points[0].LineToNextPoint.ShortRayLength > 1000);
            Assert.AreEqual(points[0].LineToNextPoint.LongRayLength, points[0].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[0], 0, points[0].LineToNextPoint.ShortRayLength, 0.2f);
        }

        [TestMethod]
        public void SampleCameraPoints_HitDiffuseLightSource_ReturnCL() //4 Strahl trift Diffuse-Lichtquelle -> Gebe Punkt auf Lichtquelle zurück
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0, LightsourceData = new DiffuseSurfaceLightDescription(){ Emission = 1 } }//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                null,
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckDiffuseLightSourcePointWithmedia(points[1], new Vector3D(-1, 0, 0), new Vector3D(-1, 0, 0), 0, 1);

            Assert.AreEqual(1, points[0].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[0], 0, 1, 0.0f);
        }

        [TestMethod]
        public void SampleCameraPoints_HitDiffuseLightSourceAfterDiffuseReflection_ReturnCDL() //5 Strahl trift Diffuse-Surface und wird Reflektiert und trift Lichtquelle -> C D L Letzter Punkt liegt im GlobalMedia
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0 },//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = -10, Radius = 1, ScatteringCoeffizient = 0, LightsourceData = new DiffuseSurfaceLightDescription(){ Emission = 1 } }//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(-1, 0, 0), ReturnValueForDirectionSampling =new Vector3D(-1, 0, 0) }
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(3, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);            
            SubpathTestHelper.CheckSurfacePointWithMedia(points[1], new Vector3D(-1, 0, 0), new Vector3D(-1, 0, 0), 0, 1);
            SubpathTestHelper.CheckDiffuseLightSourcePointWithmedia(points[2], new Vector3D(-9, 0, 0), new Vector3D(+1, 0, 0), 0, 2);

            Assert.AreEqual(1, points[0].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[0], 0, 1, 0.0f);

            Assert.AreEqual(8, points[1].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(8, points[1].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[1].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[1].LineToNextPoint.Segments[0], 0, 8, 0.0f);
        }

        [TestMethod]
        public void SampleCameraPoints_HitDiffuseLightSourceAfterSpecularReflection_ReturnCSL() //6 Strahl trift Surface(Glas ohne Media) und wird Reflektiert und trift Lichtquelle -> C S L Letzter Punkt liegt im GlobalMedia
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0, Material = BrdfModel.TextureGlass, RefractionIndex = 1.5f },//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = -10, Radius = 1, ScatteringCoeffizient = 0, LightsourceData = new DiffuseSurfaceLightDescription(){ Emission = 1 } }//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(-1, 0, 0), ReturnValueForDirectionSampling =new Vector3D(-1, 0, 0), ExpectedRefractionIndexRaysComesFrom = 1, ExpectedRefrationIndexRaysGoesInto = 1.5f }
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(3, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckSpecularBorderPoint(points[1], new Vector3D(-1, 0, 0), new Vector3D(-1, 0, 0), 1.5f, 1);
            SubpathTestHelper.CheckDiffuseLightSourcePointWithmedia(points[2], new Vector3D(-9, 0, 0), new Vector3D(+1, 0, 0), 0, 2);

            Assert.AreEqual(1, points[0].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[0], 0, 1, 0.0f);

            Assert.AreEqual(8, points[1].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(8, points[1].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[1].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[1].LineToNextPoint.Segments[0], 0, 8, 0.0f);
        }

        [TestMethod]
        public void SampleCameraPoints_HitSpecularPointAfterRefractionNoMedia_ReturnCSS() //7 Strahl trift Surface(Glas ohne Media) und wird gebrochen (Brechungsindex > 1) und trift andere Glaswand -> C S S Letzter Punkt liegt im Glasmedium 
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0, Material = BrdfModel.TextureGlass, RefractionIndex = 1.5f },//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = -10, Radius = 1, ScatteringCoeffizient = 0, LightsourceData = new DiffuseSurfaceLightDescription(){ Emission = 1 } }//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(-1, 0, 0), ReturnValueForDirectionSampling =new Vector3D(+1, 0, 0), ExpectedRefractionIndexRaysComesFrom = 1, ExpectedRefrationIndexRaysGoesInto = 1.5f },
                    new BrdfMockData(){ExpectedLocation = new Vector3D(+1, 0, 0), ReturnValueForDirectionSampling =null, ExpectedRefractionIndexRaysComesFrom = 1.5f, ExpectedRefrationIndexRaysGoesInto = 1 }
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(3, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckSpecularBorderPoint(points[1], new Vector3D(-1, 0, 0), new Vector3D(-1, 0, 0), 1.5f, 1);
            SubpathTestHelper.CheckSpecularBorderPoint(points[2], new Vector3D(+1, 0, 0), new Vector3D(-1, 0, 0), 1.5f, 2);

            Assert.AreEqual(1, points[0].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[0], 0, 1, 0.0f);

            Assert.AreEqual(2, points[1].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(2, points[1].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[1].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[1].LineToNextPoint.Segments[0], 0, 2, 0.0f);
        }

        [TestMethod]
        public void SampleCameraPoints_HitSpecularPointAfterRefractionNoMedia1_ReturnCSS() //8 Strahl trift Glas-Würfel ohne Media, welcher in ein Luftwürfel liegt. Strahl wird gebrochen (Brechungsindex > 1) und trift andere Glaswand -> C S S Letzter Punkt liegt im Glasmedium 
        {
            Vector3D startPoint = new Vector3D(-3, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 2, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f },//Luft-Würfel mit Brechungsindex 1
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0, Material = BrdfModel.TextureGlass, RefractionIndex = 1.5f },//Glas-Würfel ohne Media                     
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(-1, 0, 0), ReturnValueForDirectionSampling =new Vector3D(+1, 0, 0), ExpectedRefractionIndexRaysComesFrom = 1, ExpectedRefrationIndexRaysGoesInto = 1.5f },
                    new BrdfMockData(){ExpectedLocation = new Vector3D(+1, 0, 0), ReturnValueForDirectionSampling =null, ExpectedRefractionIndexRaysComesFrom = 1.5f, ExpectedRefrationIndexRaysGoesInto = 1 }
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(3, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckSpecularBorderPoint(points[1], new Vector3D(-1, 0, 0), new Vector3D(-1, 0, 0), 1.5f, 1);
            SubpathTestHelper.CheckSpecularBorderPoint(points[2], new Vector3D(+1, 0, 0), new Vector3D(-1, 0, 0), 1.5f, 2);

            Assert.AreEqual(2, points[0].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(2, points[0].LineToNextPoint.LongRayLength);
            Assert.AreEqual(2, points[0].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[0], 0, 1, 0.0f);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[1], 1, 2, 0.1f);

            Assert.AreEqual(2, points[1].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(2, points[1].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[1].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[1].LineToNextPoint.Segments[0], 0, 2, 0.0f);
        }

        [TestMethod]
        public void SampleCameraPoints_ReflectOnGlasMediaBorder_ReturnCSL() //9 Strahl trift MediaBorder(Glas mit Media) und wird Reflektiert und trift Lichtquelle -> C B L Letzter Punkt liegt im GlobalMedia
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.5f },//Media-Glas-Würfel mit X-Position = 0; Radius = 1
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = -10, Radius = 1, ScatteringCoeffizient = 0, LightsourceData = new DiffuseSurfaceLightDescription(){ Emission = 1 } }//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(-1, 0, 0), ReturnValueForDirectionSampling =new Vector3D(-1, 0, 0), ExpectedRefractionIndexRaysComesFrom = 1, ExpectedRefrationIndexRaysGoesInto = 1.5f }
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(3, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckSpecularBorderPoint(points[1], new Vector3D(-1, 0, 0), new Vector3D(-1, 0, 0), 1.5f, 1); //Obwohl Borderpunkt auf Medium liegt, hat er doch kein Scattering, da Strahl daran reflektiert wurde
            SubpathTestHelper.CheckDiffuseLightSourcePointWithmedia(points[2], new Vector3D(-9, 0, 0), new Vector3D(+1, 0, 0), 0, 2);

            Assert.AreEqual(1, points[0].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[0], 0, 1, 0.0f);

            Assert.AreEqual(8, points[1].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(8, points[1].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[1].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[1].LineToNextPoint.Segments[0], 0, 8, 0.0f);
        }

        [TestMethod]
        public void SampleCameraPoints_RefractOnGlasMediaBorder_ReturnCSS() //10 Strahl trift MediaBorder(Glas mit Media) und wird gebrochen (Brechungsindex > 1) und trift andere Glaswand -> C B B Letzter Punkt liegt im Glasmedium 
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.5f },//Media-Glas-Würfel mit X-Position = 0; Radius = 1
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = -10, Radius = 1, ScatteringCoeffizient = 0, LightsourceData = new DiffuseSurfaceLightDescription(){ Emission = 1 } }//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(-1, 0, 0), ReturnValueForDirectionSampling =new Vector3D(+1, 0, 0), ExpectedRefractionIndexRaysComesFrom = 1, ExpectedRefrationIndexRaysGoesInto = 1.5f },
                    new BrdfMockData(){ExpectedLocation = new Vector3D(+1, 0, 0), ReturnValueForDirectionSampling =null, ExpectedRefractionIndexRaysComesFrom = 1.5f, ExpectedRefrationIndexRaysGoesInto = 1 }
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(3, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckSpecularBorderPoint(points[1], new Vector3D(-1, 0, 0), new Vector3D(-1, 0, 0), 1.5f, 1); //Obwohl Borderpunkt auf Medium liegt, hat er doch kein Scattering, da Strahl daran reflektiert wurde
            SubpathTestHelper.CheckSpecularBorderPoint(points[2], new Vector3D(+1, 0, 0), new Vector3D(-1, 0, 0), 1.5f, 2); //Borderpunkt liegt nun innen im Medium

            //LuftSegment
            Assert.AreEqual(1, points[0].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[0], 0, 1, 0.0f);

            //Media-Segment
            Assert.AreEqual(2, points[1].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(2, points[1].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[1].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[1].LineToNextPoint.Segments[0], 0, 2, 0.1f);
        }

        [TestMethod]
        public void SampleCameraPoints_RefractOnAirMediaBorder_ReturnCP() //11 Shortray: Strahl trift Partikel in Wolke(MediaBroder mit Brechungsindex 1) und wird absorbiert -> CP
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f },//Media-Luft-Würfel mit X-Position = 0; Radius = 1
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = -10, Radius = 1, ScatteringCoeffizient = 0, LightsourceData = new DiffuseSurfaceLightDescription(){ Emission = 1 } }//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                },
                0,//Kein GlobalMedia vorhanden
                new ParticipatingMediaMockData()
                {
                    ReturnValuesForDistanceSampling = new List<float>() { 1 }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                    ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { new Vector3D(-1, 0, 0) },
                    ScatteringCoeffizient = 0.1f
                }),
                SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                null,
                new DirectionSamplingMockData()
                {
                    ReturnValuesForDirectionSampling = new List<Vector3D>() { null },
                    ExpectedPointLocationForDirectionSampling = new List<Vector3D>() { new Vector3D(0, 0, 0) }
                });


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckMediaParticlePoint(points[1], new Vector3D(0, 0, 0), 0.1f, 1);

            
            Assert.AreEqual(2, points[0].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(2, points[0].LineToNextPoint.LongRayLength);
            Assert.AreEqual(2, points[0].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[0], 0, 1, 0.0f); //LuftSegment
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[1], 1, 2, 0.1f); //Media-Segment
        }

        [TestMethod]
        public void SampleCameraPoints_RefractOnAirMediaBorder_ReturnCPI() //12 Longray: Strahl trift Partikel in Wolke(MediaBroder mit Brechungsindex 1) und wird absorbiert -> CP
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f },//Media-Luft-Würfel mit X-Position = 0; Radius = 1
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = -10, Radius = 1, ScatteringCoeffizient = 0, LightsourceData = new DiffuseSurfaceLightDescription(){ Emission = 1 } }//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                },
                0,//Kein GlobalMedia vorhanden
                new ParticipatingMediaMockData()
                {
                    ReturnValuesForDistanceSampling = new List<float>() { 1 }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                    ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { new Vector3D(-1, 0, 0) },
                    ScatteringCoeffizient = 0.1f
                }),
                SubpathGenerator.PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling,
                null,
                new DirectionSamplingMockData()
                {
                    ReturnValuesForDirectionSampling = new List<Vector3D>() { null },
                    ExpectedPointLocationForDirectionSampling = new List<Vector3D>() { new Vector3D(0, 0, 0) }
                });


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckMediaParticlePoint(points[1], new Vector3D(0, 0, 0), 0.1f, 1);

            Assert.AreEqual(2, points[0].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(3, points[0].LineToNextPoint.LongRayLength);
            Assert.AreEqual(3, points[0].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[0], 0, 1, 0.0f); //LuftSegment
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[1], 1, 2, 0.1f); //Media-Segment (ShortRay)
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[2], 2, 3, 0.1f); //Media-Segment (LongRay)
            Assert.IsFalse(points[0].LineToNextPoint.StartPoint.CurrentMedium.HasScatteringSomeWhereInMedium());
            Assert.IsTrue(points[0].LineToNextPoint.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium());
        }

        [TestMethod]
        public void SampleCameraPoints_HitParticle_ReturnCP() //13 Strahl trift Partikel und wird absorbiert -> C P 
        {
            Vector3D startPoint = new Vector3D(-2, 3, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0}//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                },
                0.1f,
                new ParticipatingMediaMockData()
                {
                    ReturnValuesForDistanceSampling = new List<float>() { 1 }, //SampleDistance = 1; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                    ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { startPoint },
                    ScatteringCoeffizient = 0.1f
                }), //GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                null,
                new DirectionSamplingMockData()
                {
                    ReturnValuesForDirectionSampling = new List<Vector3D>() { null },
                    ExpectedPointLocationForDirectionSampling = new List<Vector3D>() { new Vector3D(-1, 3, 0) }
                });


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0.1f);
            SubpathTestHelper.CheckMediaParticlePoint(points[1], new Vector3D(-1, 3, 0), 0.1f, 1);

            //Media-Segment
            Assert.AreEqual(1, points[0].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[0], 0, 1, 0.1f);
        }

        [TestMethod]
        public void SampleCameraPoints_SampleVeryShortDistance_ReturnC() //14 Distanzsampling liefert Wert nahe 0 -> Es wird nur Kamerapunkt zurück gegeben 
        {
            Vector3D startPoint = new Vector3D(-2, 3, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0}//Solid-Würfel mit X-Position = 0; Radius = 1; Ohne Media
                },
                0.1f,
                new ParticipatingMediaMockData()
                {
                    ReturnValuesForDistanceSampling = new List<float>() { MagicNumbers.MinAllowedPathPointDistance / 2 }, //SampleDistance = Nahe 0; Ich erwarte, das beim SampleDistanz-Aufruf die aktuelle Position der Kamera-Startpunkt ist
                    ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { startPoint },
                    ScatteringCoeffizient = 0.1f
                }), //GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                null,
                new DirectionSamplingMockData()
                {
                    ReturnValuesForDirectionSampling = new List<Vector3D>() { null },
                    ExpectedPointLocationForDirectionSampling = new List<Vector3D>() { new Vector3D(-2 + MagicNumbers.MinAllowedPathPointDistance / 2, 3, 0) }
                });


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(1, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0.1f);
            Assert.IsNull(points[0].LineToNextPoint);            
        }

        
        [TestMethod]
        public void SampleCameraPoints_GoThroughCenterFromAirSphere_ReturnCB() //15 Kein Distanzsampling; Strahl trifft Media-Luft-Kugel in Mitte und durchläuft sie -> CB mit 2 Segmenten
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Sphere, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1}//Luft-Kugel
                },
                0.0f //Kein GlobalMedia
                ),
                SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                null,
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0.0f);
            SubpathTestHelper.CheckAirBorderPoint(points[1], new Vector3D(+1, 0, 0), new Vector3D(-1, 0, 0), 1); //Borderpunkt
            
            var line = points[0].LineToNextPoint;
            Assert.AreEqual(MediaPointLocationType.Camera, line.StartPoint.Location);
            Assert.AreEqual(startPoint, line.StartPoint.Position);
            Assert.AreEqual(MediaPointLocationType.NullMediaBorder, line.EndPoint.Location);
            Assert.AreEqual(new Vector3D(1, 0, 0), line.EndPoint.Position);
            Assert.AreEqual(3, line.ShortRayLength);
            Assert.AreEqual(3, line.LongRayLength);
            Assert.AreEqual(2, line.Segments.Count);

            SubpathTestHelper.CheckSegment(line.Segments[0], 0, 1, 0.0f);//LuftSegment
            SubpathTestHelper.CheckSegment(line.Segments[1], 1, 3, 0.1f);//Media-Segment Würfel 1
        }

        [TestMethod]
        public void SampleCameraPoints_GoThroughUpperBorderFromAirSphere_ReturnCB() //16 Longray; Strahl trifft Media-Luft-Kugel ganz oben und trifft nur einmal obwohl Kugel durchlaufen wird -> CB mit 1 Segment
        {
            Vector3D startPoint = new Vector3D(-2, 1, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Sphere, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1}//Luft-Kugel
                },
                0.0f, //Kein GlobalMedia
                new ParticipatingMediaMockData() //Mockdate für die Air-Kugel
                {
                    ReturnValuesForDistanceSampling = new List<float>() { 1 }, //SampleDistance = 1 
                    ExpectedMediaPointsForDistanceSampling = new List<Vector3D>() { new Vector3D(0, 1, 0) },
                    ScatteringCoeffizient = 0.1f
                }
                ),
                SubpathGenerator.PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling,
                null,
                new DirectionSamplingMockData()
                {
                    ReturnValuesForDirectionSampling = new List<Vector3D>() { null },
                    ExpectedPointLocationForDirectionSampling = new List<Vector3D>() { new Vector3D(1, 1, 0) }//Achtung: Der Punkt (1, 1, 0) liegt außerhalb der Kugel. Ich habe ihn hier nur mit drin, damit der Test dann beim Point-Position-Assert failed und nicht schon 
                });


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0.0f);
            //Assert.AreEqual(MediaPointLocationType.MediaInfinity, points[0].LineToNextPoint.EndPoint.Location);
            //Assert.AreEqual(MediaPointLocationType.MediaInfinity, points[1].LocationType);
            
            //Es gibt nur ein Punkt, da die PdfA bei der Kugel 0 ist, da die Normale nach oben zeigt und der Strahl von Links nach Rechts. Somit ist die Fläche von der Seite unsichtbar/PdfWToPdfA ergibt somit 0
            SubpathTestHelper.CheckAirBorderPoint(points[1], new Vector3D(0, 1, 0), new Vector3D(0, 1, 0), 1); //Bordereintrittspunkt ganz oben

            var line = points[0].LineToNextPoint;
            Assert.AreEqual(MediaPointLocationType.Camera, line.StartPoint.Location);
            Assert.AreEqual(startPoint, line.StartPoint.Position);
            Assert.AreEqual(MediaPointLocationType.NullMediaBorder, line.EndPoint.Location);
            Assert.AreEqual(new Vector3D(0, 1, 0), line.EndPoint.Position);
            Assert.AreEqual(2, line.ShortRayLength);
            Assert.AreEqual(2, line.LongRayLength);
            Assert.AreEqual(1, line.Segments.Count);

            SubpathTestHelper.CheckSegment(line.Segments[0], 0, 2, 0.0f);//LuftSegment
        }

        [DataRow(50)] //So ist das Segment so lang, das beide Kugelschnittpunkte gefunden werden
        [DataRow(MagicNumbers.MinAllowedPathPointDistance * 0.1f)] //Das Segment ist so kurz, dass nur der Eintrittspunkt gefunden wird
        [DataTestMethod]
        public void SampleCameraPoints_GoThroudMinEdgeFromAirSphere_ReturnCB(float segmentLength) //17 Ohne Global Media; Ohne Distanzsampling; Strahl durchfliegt Kugel ganz oben, so das KugelSegment < MinAllowedPathPointDistanz ist
        {
            //https://de.wikipedia.org/wiki/Kreissegment
            //Gegeben: Radius r und Kreissehne s; Gesucht: Segmenthöhe h
            //Mittelpunktswinkel alpha = 2 * arcsin(s / (2*r))
            //Segmenthöhe h = r * (1 - cos(alpha / 2))

            //Strahl kommt mit y = radius - h an
            float radius = 100;
            //float s = 50; //So ist das Segment so lang, das es keine Kunst ist, beide Kugelschnittpunkte zu treffen
            //float s = MagicNumbers.MinAllowedPathPointDistance * 0.1f;
            float s = segmentLength;
            double alpha = 2 * Math.Asin(s / (2 * radius));
            float h = (float)(radius * (1 - Math.Cos(alpha / 2)));

           

            Vector3D startPoint = new Vector3D(-100, radius - h, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Sphere, XPosition = 0, Radius = radius, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f },//Media-Luft-Kugel mit X-Position = 0; Radius = 1
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                null,
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;


            Vector3D border1 = new Vector3D(0 - s / 2, startPoint.Y, 0); //Border-Eintrittspunkt (Da es Luft-Kugel ist, wird er verschluckt und findet sich in MediaLine wieder)
            Vector3D border2 = new Vector3D(0 + s / 2, startPoint.Y, 0); //Border-Austrittspunkt

            float l1 = (border1 - startPoint).Length();
            float l2 = s;
            float l = l1 + l2;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            

            var line = points[0].LineToNextPoint;
            Assert.AreEqual(MediaPointLocationType.Camera, line.StartPoint.Location);
            Assert.AreEqual(startPoint, line.StartPoint.Position);
            


            if (l2 > MagicNumbers.MinAllowedPathPointDistance) //Bei großen Kreissegmentn landet Punkt auf Border2 = NullMediaBorder
            {
                SubpathTestHelper.CheckAirBorderPoint(points[1], border2, Vector3D.Normalize(-border2), 1); //Borderpunkt
                Assert.AreEqual(MediaPointLocationType.NullMediaBorder, line.EndPoint.Location);
                Assert.IsTrue((border2 - line.EndPoint.Position).Length() < 0.0001f);
                Assert.IsTrue(Math.Abs(l - line.ShortRayLength) < 0.00001f);

                Assert.IsTrue(Math.Abs(l - line.LongRayLength) < 0.00001f);

                Assert.AreEqual(2, line.Segments.Count);

                SubpathTestHelper.CheckSegment(line.Segments[0], 0, l1, 0.0f);//LuftSegment
                SubpathTestHelper.CheckSegment(line.Segments[1], l1, l, 0.1f);//Media-Segment Würfel 1
            }
            else //Bei kleinen Kreissegment wird zweiter Kugelschnittpunkt nicht gefunden und Strahl geht von Border1 aus ins Infinity
            {
                //Assert.AreEqual(MediaPointLocationType.MediaInfinity, line.EndPoint.Location);

                SubpathTestHelper.CheckAirBorderPoint(points[1], border1, new Vector3D(0,1,0), 1); //Borderpunkt

                l1 = 100;
                Assert.IsTrue(Math.Abs(l1 - line.LongRayLength) < 0.00001f);

                Assert.AreEqual(1, line.Segments.Count);

                SubpathTestHelper.CheckSegment(line.Segments[0], 0, l1, 0.0f);//LuftSegment
            }

        }

        [TestMethod]
        public void SampleCameraPoints_GoThroudTwoAirCubes_ReturnCB() //18 Ohne Global Media; Ohne Distanzsampling; Strahl durchfliegt zwei Media-Luft-Würfel wo Vacuum dahinter ist
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f },//Media-Luft-Würfel mit X-Position = 0; Radius = 1
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 3, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f },//Media-Luft-Würfel mit X-Position = 3; Radius = 1
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                null,
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckAirBorderPoint(points[1], new Vector3D(+4, 0, 0), new Vector3D(-1, 0, 0), 1); //Borderpunkt

            var line = points[0].LineToNextPoint;
            Assert.AreEqual(MediaPointLocationType.Camera, line.StartPoint.Location);
            Assert.AreEqual(startPoint, line.StartPoint.Position);
            Assert.AreEqual(MediaPointLocationType.NullMediaBorder, line.EndPoint.Location);
            Assert.AreEqual(new Vector3D(4,0,0), line.EndPoint.Position);
            Assert.AreEqual(6, line.ShortRayLength);
            Assert.AreEqual(6, line.LongRayLength);
            Assert.AreEqual(4, line.Segments.Count);

            SubpathTestHelper.CheckSegment(line.Segments[0], 0, 1, 0.0f);//LuftSegment
            SubpathTestHelper.CheckSegment(line.Segments[1], 1, 3, 0.1f);//Media-Segment Würfel 1
            SubpathTestHelper.CheckSegment(line.Segments[2], 3, 4, 0.0f);//LuftSegment zwischen den Würfeln
            SubpathTestHelper.CheckSegment(line.Segments[3], 4, 6, 0.1f);//Media-Segment Würfel 2
        }

        
        [TestMethod]
        public void SampleCameraPoints_GoThroudAirCubeAndEndsOnWall1_ReturnCBBBBS() 
        {
            SampleCameraPoints_GoThroudAirCubeAndEndsOnWall_ReturnCBS(0.5f);
        }

        [TestMethod]
        public void SampleCameraPoints_GoThroudAirCubeAndEndsOnWall2_ReturnCBBBBS() 
        {
            SampleCameraPoints_GoThroudAirCubeAndEndsOnWall_ReturnCBS(0.0f);
        }

        private void SampleCameraPoints_GoThroudAirCubeAndEndsOnWall_ReturnCBS(float cameraY) //19 Ohne Global Media; Ohne Distanzsampling; Strahl durchfliegt Media-Luft-Würfel wo Wand mit Abstand dahinter ist
        {
            Vector3D startPoint = new Vector3D(-2, cameraY, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f },//Media-Luft-Würfel mit X-Position = 0; Radius = 1
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 3, Radius = 1, ScatteringCoeffizient = 0.0f, Material = BrdfModel.Diffus },//Solid-Würfel mit X-Position = 3; Radius = 1
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(2, cameraY, 0) , ReturnValueForDirectionSampling = null },
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckSurfacePointWithMedia(points[1], new Vector3D(2, cameraY, 0), new Vector3D(-1, 0, 0), 0, 1);

            var line = points[0].LineToNextPoint;
            Assert.AreEqual(MediaPointLocationType.Camera, line.StartPoint.Location);
            Assert.AreEqual(startPoint, line.StartPoint.Position);
            Assert.AreEqual(MediaPointLocationType.Surface, line.EndPoint.Location);
            Assert.AreEqual(new Vector3D(2, cameraY, 0), line.EndPoint.Position);
            Assert.AreEqual(4, line.ShortRayLength);
            Assert.AreEqual(4, line.LongRayLength);
            Assert.AreEqual(3, line.Segments.Count);

            SubpathTestHelper.CheckSegment(line.Segments[0], 0, 1, 0.0f);//LuftSegment
            SubpathTestHelper.CheckSegment(line.Segments[1], 1, 3, 0.1f);//Media-Segment Würfel 1
            SubpathTestHelper.CheckSegment(line.Segments[2], 3, 4, 0.0f);//LuftSegment zwischen den Würfeln
        }

        [TestMethod]
        public void SampleCameraPoints_GoIntoAirCubeWhoIsOverlappingWall_ReturnCBS() //20 Kamera schaut auf Media-Würfel, der Wand überschneidet
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0.5f, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f },//Media-Luft-Würfel mit X-Position = 0; Radius = 1
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 2, Radius = 1, ScatteringCoeffizient = 0.0f, Material = BrdfModel.Diffus },//Solid-Würfel mit X-Position = 3; Radius = 1
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(1, 0, 0) , ReturnValueForDirectionSampling = null },
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);            
            SubpathTestHelper.CheckSurfacePointWithMedia(points[1], new Vector3D(1, 0, 0), new Vector3D(-1, 0, 0), 0.1f, 1);

            var line = points[0].LineToNextPoint;
            Assert.AreEqual(MediaPointLocationType.Camera, line.StartPoint.Location);
            Assert.AreEqual(startPoint, line.StartPoint.Position);
            Assert.AreEqual(MediaPointLocationType.Surface, line.EndPoint.Location);
            Assert.AreEqual(new Vector3D(1, 0, 0), line.EndPoint.Position);
            Assert.AreEqual(3, line.ShortRayLength);
            Assert.AreEqual(3, line.LongRayLength);
            Assert.AreEqual(2, line.Segments.Count);

            SubpathTestHelper.CheckSegment(line.Segments[0], 0, 1.5f, 0.0f);//LuftSegment
            SubpathTestHelper.CheckSegment(line.Segments[1], 1.5f, 3, 0.1f);//Media-Segment Würfel 1
        }

        
        [TestMethod]
        public void SampleCameraPoints_GoIntoAirCubeWhoIsOverlappingWallAndReflect_ReturnCBSB() //21 Kamera schaut auf Media-Würfel, der Wand überschneidet. Lichtstrahl wird an Wand reflektiert
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0.5f, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f },//Media-Luft-Würfel mit X-Position = 0; Radius = 1
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 2, Radius = 1, ScatteringCoeffizient = 0.0f, Material = BrdfModel.Diffus },//Solid-Würfel mit X-Position = 3; Radius = 1
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(1, 0, 0) , ReturnValueForDirectionSampling = new Vector3D(-1,0,0) },
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(3, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);            
            SubpathTestHelper.CheckSurfacePointWithMedia(points[1], new Vector3D(1, 0, 0), new Vector3D(-1, 0, 0), 0.1f, 1);
            SubpathTestHelper.CheckAirBorderPoint(points[2], new Vector3D(-0.5f, 0, 0), new Vector3D(+1, 0, 0), 2);

            //LuftSegment
            Assert.AreEqual(3, points[0].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(3, points[0].LineToNextPoint.LongRayLength);
            Assert.AreEqual(2, points[0].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[0], 0, 1.5f, 0.0f);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[1], 1.5f, 3, 0.1f);

            //Media-Segment Würfel Rechts-Nach-Links
            Assert.AreEqual(1.5f, points[1].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1.5f, points[1].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[1].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[1].LineToNextPoint.Segments[0], 0, 1.5f, 0.1f);
        }

        [TestMethod]
        public void SampleCameraPoints_GoThrougAirCubeWithGlobalMedia_ReturnCBI() //22 Mit Global Media; Ohne Distanzsampling;Kamera befindet sich im GlobalMedia und schaut auf Media-Air-Würfel
        {
            Vector3D startPoint = new Vector3D(-2, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f },//Media-Luft-Würfel mit X-Position = 0; Radius = 1
                },
                0.2f), //Mit GlobalMedia
                SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                null,
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0.2f);
            SubpathTestHelper.CheckMediaInfinityPoint(points[1], 0.2f, 1);

            var line = points[0].LineToNextPoint;
            Assert.AreEqual(MediaPointLocationType.Camera, line.StartPoint.Location);
            Assert.AreEqual(startPoint, line.StartPoint.Position);
            Assert.AreEqual(MediaPointLocationType.MediaInfinity, line.EndPoint.Location);
            Assert.AreEqual(points[1].Position, line.EndPoint.Position);
            Assert.IsTrue(line.ShortRayLength > 1000);
            Assert.AreEqual(line.LongRayLength, line.ShortRayLength);
            Assert.AreEqual(3, line.Segments.Count);

            SubpathTestHelper.CheckSegment(line.Segments[0], 0, 1, 0.2f);//GlobalMedia bis Würfel
            SubpathTestHelper.CheckSegment(line.Segments[1], 1, 3, 0.1f);//Media-Segment Würfel
            SubpathTestHelper.CheckSegment(line.Segments[2], 3, line.ShortRayLength, 0.2f);//GlobalMedia bis Infinity
        }


        [TestMethod]
        public void SampleCameraPoints_GoThrougWaterGlas_ReturnCSL() //23 Strahl durchläuft Wasser-Glas, wo das Glas auch ein Medium enthält und das Wasser eine niedrigere Prio als das Glas hat
        {
            Vector3D startPoint = new Vector3D(-4, 0, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 2, ScatteringCoeffizient = 0.2f, Material = BrdfModel.TextureGlass, RefractionIndex = 2.0f },//Wasser-Würfel
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = -2, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.5f },//Linke Glas-Wand (Wasser ragt in Glaswand rein)
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = +2, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.5f },//Linke Glas-Wand (Wasser ragt in Glaswand rein)
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(-3, 0, 0), ReturnValueForDirectionSampling =new Vector3D(+1, 0, 0), ExpectedRefractionIndexRaysComesFrom = 1, ExpectedRefrationIndexRaysGoesInto = 1.5f },   //Linker Rand linker Glaswand
                    //new BrdfMockData(){ExpectedLocation = new Vector3D(-2, 0, 0), ReturnValueForDirectionSampling =new Vector3D(+1, 0, 0), ExpectedRefractionIndexRaysComesFrom = 1.5f, ExpectedRefrationIndexRaysGoesInto = 1.5f },//Linker Rand Wasser          Es darf kein Sampling auf ein MediaBorder mit niedrigerer Prio erfolgen
                    new BrdfMockData(){ExpectedLocation = new Vector3D(-1, 0, 0), ReturnValueForDirectionSampling =new Vector3D(+1, 0, 0), ExpectedRefractionIndexRaysComesFrom = 1.5f, ExpectedRefrationIndexRaysGoesInto = 2.0f },   //Rechter Rand linker Glaswand
                    new BrdfMockData(){ExpectedLocation = new Vector3D(+1, 0, 0), ReturnValueForDirectionSampling =new Vector3D(+1, 0, 0), ExpectedRefractionIndexRaysComesFrom = 2.0f, ExpectedRefrationIndexRaysGoesInto = 1.5f },   //Linker Rand rechte Glaswand
                    //new BrdfMockData(){ExpectedLocation = new Vector3D(+2, 0, 0), ReturnValueForDirectionSampling =new Vector3D(+1, 0, 0), ExpectedRefractionIndexRaysComesFrom = 1.5f, ExpectedRefrationIndexRaysGoesInto = 1.5f },//Rechter Rand Wasser         Es darf kein Sampling auf ein MediaBorder mit niedrigerer Prio erfolgen
                    new BrdfMockData(){ExpectedLocation = new Vector3D(+3, 0, 0), ReturnValueForDirectionSampling =new Vector3D(+1, 0, 0), ExpectedRefractionIndexRaysComesFrom = 1.5f, ExpectedRefrationIndexRaysGoesInto = 1.0f },   //Rechter Rand rechte Glaswand
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(7, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckSpecularBorderPoint(points[1], new Vector3D(-3, 0, 0), new Vector3D(-1, 0, 0), 1.5f, 1); //Linker Rand linke Glas-Wand
            SubpathTestHelper.CheckSpecularBorderPoint(points[2], new Vector3D(-2, 0, 0), new Vector3D(-1, 0, 0), 2.0f, 2); //Linker Rand Wasser
            SubpathTestHelper.CheckSpecularBorderPoint(points[3], new Vector3D(-1, 0, 0), new Vector3D(-1, 0, 0), 1.5f, 3); //Rechter Rand linke Glas-Wand
            SubpathTestHelper.CheckSpecularBorderPoint(points[4], new Vector3D(+1, 0, 0), new Vector3D(-1, 0, 0), 1.5f, 4); //Linker Rand rechte Glas-Wand
            SubpathTestHelper.CheckSpecularBorderPoint(points[5], new Vector3D(+2, 0, 0), new Vector3D(-1, 0, 0), 2.0f, 5); //Rechter Rand Wasser
            SubpathTestHelper.CheckSpecularBorderPoint(points[6], new Vector3D(+3, 0, 0), new Vector3D(-1, 0, 0), 1.5f, 6);	//Rechter Rand rechte Glas-Wand

            //Luft
            Assert.AreEqual(1, points[0].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[0].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[0].LineToNextPoint.Segments[0], 0, 1, 0.0f);

            //Linkes Glas 1
            Assert.AreEqual(1, points[1].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1, points[1].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[1].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[1].LineToNextPoint.Segments[0], 0, 1, 0.1f);

            //Linkes Glas 2
            Assert.AreEqual(1, points[2].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1, points[2].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[2].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[2].LineToNextPoint.Segments[0], 0, 1, 0.1f);

            //Wasser
            Assert.AreEqual(2, points[3].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(2, points[3].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[3].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[3].LineToNextPoint.Segments[0], 0, 2, 0.2f);

            //Rechtes Glas 1
            Assert.AreEqual(1, points[4].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1, points[4].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[4].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[4].LineToNextPoint.Segments[0], 0, 1, 0.1f);

            //Rechtes Glas 2
            Assert.AreEqual(1, points[5].LineToNextPoint.ShortRayLength);
            Assert.AreEqual(1, points[5].LineToNextPoint.LongRayLength);
            Assert.AreEqual(1, points[5].LineToNextPoint.Segments.Count);
            SubpathTestHelper.CheckSegment(points[5].LineToNextPoint.Segments[0], 0, 1, 0.1f);

            Assert.IsNull(points[6].LineToNextPoint);
        }

        
        [TestMethod]
        public void SampleCameraPoints_GoThroudEdgeFromAirCube_ReturnCB() //24 Ohne Global Media; Ohne Distanzsampling; Strahl durchfliegt Media-Luft-Würfel an der Ecke, so das Abstand zwischen Ein- und Ausrittspunkt kleiner MinAllowedDistance ist
        {
            //Würfel mit Kantenlänge Radius*2 steht auf der Spitze
            //b = Linie von einer Ecke schräg durch den Würfel; b² = (Radius*2)² + (Radius*2)²
            //                                                     = (Radius*2)² * 2
            //                                                     = Radius² * 8
            //                                                  b  = Radius * Sqrt(8)
            //h = Linie von Würfelmitte zu einer der Ecken; h² = (Radius*2)² - (b/2)²
            //                                                 = Radius² * 4 - (Radius² * 8) / 4
            //                                                 = Radius² * 4 * (1 - 1/2)
            //                                              h² = Radius² * 2
            //                                              h  = Radius * Sqrt(2)

            //Strahl kommt mit y = h - c an. Oben an der Würfelspitze gibt es ein Dr
            float radius = 1;
            float h = radius * (float)Math.Sqrt(2);

            float c = MagicNumbers.MinAllowedPathPointDistance * 0.1f;

            Vector3D startPoint = new Vector3D(-2, h - c, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, Rotation = 45, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f },//Media-Luft-Würfel mit X-Position = 0; Radius = 1
                },
                0), //Kein GlobalMedia vorhanden
                SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                null,
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;


            Vector3D border1 = new Vector3D(0 - c, startPoint.Y, 0); //Border-Eintrittspunkt (Da es Luft-Würfel ist, wird er verschluckt und findet sich in MediaLine wieder)
            Vector3D border2 = new Vector3D(0 + c, startPoint.Y, 0); //Border-Austrittspunkt

            float l1 = (border1 - startPoint).Length();
            float l2 = c * 2;
            float l = l1 + l2;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckAirBorderPoint(points[1], border2, Vector3D.Normalize(new Vector3D(-1, -1, 0)), 1); //Borderpunkt

            var line = points[0].LineToNextPoint;
            Assert.AreEqual(MediaPointLocationType.Camera, line.StartPoint.Location);
            Assert.AreEqual(startPoint, line.StartPoint.Position);
            Assert.AreEqual(MediaPointLocationType.NullMediaBorder, line.EndPoint.Location);
            Assert.IsTrue((border2 - line.EndPoint.Position).Length() < 0.0001f);
            Assert.IsTrue(Math.Abs(l - line.ShortRayLength) < 0.00001f);
            

            if (l2 > MagicNumbers.MinAllowedPathPointDistance)
            {
                Assert.IsTrue(Math.Abs(l - line.LongRayLength) < 0.00001f);

                Assert.AreEqual(2, line.Segments.Count);

                SubpathTestHelper.CheckSegment(line.Segments[0], 0, l1, 0.0f);//LuftSegment
                SubpathTestHelper.CheckSegment(line.Segments[1], l1, l, 0.1f);//Media-Segment Würfel 1
            }else
            {
                Assert.IsTrue(Math.Abs(l1 - line.LongRayLength) < 0.00001f);

                Assert.AreEqual(1, line.Segments.Count);

                SubpathTestHelper.CheckSegment(line.Segments[0], 0, l1, 0.0f);//LuftSegment
            }
            
        }

        [TestMethod]
        public void SampleCameraPoints_GoThroughAirCubeAndHitEnvironmentLight_NoGlobalMedia() //25 Ohne Global Media; Ohne Distanzsampling; Strahl durchläuft Media-Würfel und trifft danach auf Umgebungslicht -> Gebe Punkt mit Umgebungslicht zurück (Bei Environmentlight hat man immer kein GlobalMedia, da die Attenuation immer 0 wäre)
        {
            float cameraY = 0;
            Vector3D startPoint = new Vector3D(-2, cameraY, 0);

            var sut = SubpathTestHelper.CreateSubPathSampler(
                startPoint,
                MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, Material = BrdfModel.TextureGlass, RefractionIndex = 1.0f },//Media-Luft-Würfel mit X-Position = 0; Radius = 1
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 3, Radius = 1, ScatteringCoeffizient = 0.0f, Material = BrdfModel.Diffus, LightsourceData = new  EnvironmentLightDescription() },//Umgebungslicht
                },
                0), //Ohne GlobalMedia
                SubpathGenerator.PathSamplingType.ParticipatingMediaWithoutDistanceSampling,
                new List<BrdfMockData>()
                {
                    new BrdfMockData(){ExpectedLocation = new Vector3D(2, cameraY, 0) , ReturnValueForDirectionSampling = null },
                },
                null);


            var points = sut.SamplePathFromCamera(1, 1, new Rand(0)).Points;

            Assert.AreEqual(2, points.Length);
            SubpathTestHelper.CheckCameraInMediaPoint(points[0], startPoint, 0);
            SubpathTestHelper.CheckEnvironmentLightPoint(points[1], 1);

            var line = points[0].LineToNextPoint;
            Assert.AreEqual(MediaPointLocationType.Camera, line.StartPoint.Location);
            Assert.AreEqual(startPoint, line.StartPoint.Position);
            Assert.AreEqual(MediaPointLocationType.Surface, line.EndPoint.Location);

            Assert.AreEqual(3, line.Segments.Count);
            SubpathTestHelper.CheckSegment(line.Segments[0], 0, 1, 0);//LuftSegment
            SubpathTestHelper.CheckSegment(line.Segments[1], 1, 3, 0.1f);//Media-Segment Würfel 1
            SubpathTestHelper.CheckSegment(line.Segments[2], 3, line.ShortRayLength, 0);//LuftSegment zwischen AirWürfel und Environmentlight
        }

        
    }
}
