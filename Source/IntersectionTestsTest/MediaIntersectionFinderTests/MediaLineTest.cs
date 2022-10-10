using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using IntersectionTests.Ray_3D_Object;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia;
using ParticipatingMediaTest.MediaMocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntersectionTestsTest.MediaIntersectionFinderTests
{
    [TestClass]
    public class MediaLineTest
    {
        [TestMethod]
        public void CreateShortMediaSubLine_CalledForLineGoesThroughMediaCubeW_TwoSegmentsReturned()
        {
            Vector3D startPoint = new Vector3D(-2, 0.5f, 0);

            //Erzeuge MediaLine, welche aus Luft|Media|Luft-Segment besteht und welche auf Wand endet
            var data = MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, RefractionIndex = 1.0f},//Media-Würfel mit X-Position = 0; Radius = 1; Mit Media
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 3, Radius = 1} //Wand mit X-Position = 3; Radius = 1; Kein Media
                }, 0);
            MediaLine sut = data.MediaIntersectionFinder.CreateMediaShortLineNoAirBlocking(data.MediaIntersectionFinder.CreateCameraMediaStartPoint(startPoint), new Vector3D(1, 0, 0), float.MaxValue, 0);

            var subLine = sut.CreateShortMediaSubLine(2);
            Assert.AreEqual(2, subLine.Segments.Count);

            //Luft-Segment
            Assert.AreEqual(0, subLine.Segments[0].RayMin);
            Assert.AreEqual(1, subLine.Segments[0].RayMax);
            Assert.IsFalse(subLine.Segments[0].Media.HasScatteringSomeWhereInMedium());

            //Media-Segment
            Assert.AreEqual(1, subLine.Segments[1].RayMin);
            Assert.AreEqual(2, subLine.Segments[1].RayMax);
            Assert.IsTrue(subLine.Segments[1].Media.HasScatteringSomeWhereInMedium());

            //Line-StartPoint
            Assert.AreEqual(MediaPointLocationType.Camera, subLine.StartPoint.Location);
            Assert.AreEqual(-2, subLine.StartPoint.Position.X);
            Assert.IsFalse(subLine.StartPoint.CurrentMedium.HasScatteringSomeWhereInMedium());

            //Line-EndPoint
            Assert.AreEqual(0, subLine.EndPoint.Position.X);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, subLine.EndPointLocation);
            Assert.IsTrue(subLine.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium());

            //Line-Pdf
            Assert.AreEqual(GetPdfLForStopInMedia(1), subLine.SampledPdfL().PdfL);
            Assert.AreEqual(GetPdfLForGoThroughMedia(1), subLine.SampledPdfL().ReversePdfL);
        }

        [TestMethod]
        public void CreateLongMediaSubLine_CalledForLineGoesThroughMediaCubeW_FoarSegmentsReturned()
        {
            Vector3D startPoint = new Vector3D(-2, 0.5f, 0);

            //Erzeuge MediaLine, welche aus Luft|Media|Luft-Segment besteht und welche auf Wand endet
            var data = MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 0, Radius = 1, ScatteringCoeffizient = 0.1f, RefractionIndex = 1.0f},//Air-Media-Würfel mit X-Position = 0; Radius = 1; Mit Media
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Cube, XPosition = 3, Radius = 1 } //Wand mit X-Position = 3; Radius = 1; Kein Media
                }, 0);
            MediaLine sut = data.MediaIntersectionFinder.CreateMediaShortLineNoAirBlocking(data.MediaIntersectionFinder.CreateCameraMediaStartPoint(startPoint), new Vector3D(1, 0, 0), float.MaxValue, 0);

            var subLine = sut.CreateLongMediaSubLine(2);
            Assert.AreEqual(4, subLine.Segments.Count);

            //Luft-Segment
            Assert.AreEqual(0, subLine.Segments[0].RayMin);
            Assert.AreEqual(1, subLine.Segments[0].RayMax);
            Assert.IsFalse(subLine.Segments[0].Media.HasScatteringSomeWhereInMedium());

            //Media-Segment bis zur Mitte
            Assert.AreEqual(1, subLine.Segments[1].RayMin);
            Assert.AreEqual(2, subLine.Segments[1].RayMax);
            Assert.IsTrue(subLine.Segments[1].Media.HasScatteringSomeWhereInMedium());

            //Media-Segment ab der Mitte
            Assert.AreEqual(2, subLine.Segments[2].RayMin);
            Assert.AreEqual(3, subLine.Segments[2].RayMax);
            Assert.IsTrue(subLine.Segments[2].Media.HasScatteringSomeWhereInMedium());

            //Luft-Segment
            Assert.AreEqual(3, subLine.Segments[3].RayMin);
            Assert.AreEqual(4, subLine.Segments[3].RayMax);
            Assert.IsFalse(subLine.Segments[3].Media.HasScatteringSomeWhereInMedium());

            //Line-StartPoint
            Assert.AreEqual(MediaPointLocationType.Camera, subLine.StartPoint.Location);
            Assert.AreEqual(-2, subLine.StartPoint.Position.X);
            Assert.IsFalse(subLine.StartPoint.CurrentMedium.HasScatteringSomeWhereInMedium());

            //Line-EndPoint
            Assert.AreEqual(0, subLine.EndPoint.Position.X);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, subLine.EndPointLocation);
            Assert.IsTrue(subLine.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium());

            //Line-Pdf
            Assert.AreEqual(GetPdfLForStopInMedia(1), subLine.SampledPdfL().PdfL);
            Assert.AreEqual(GetPdfLForGoThroughMedia(1), subLine.SampledPdfL().ReversePdfL);
        }

        [TestMethod]
        public void CreateShortMediaSubLine_CalledForLineWatchIntoSky_OneSegmentsReturned()
        {
            Vector3D startPoint = new Vector3D(1.1f, 0, 0);

            var data = MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Sphere, XPosition = 0, Radius = 1},//Erde mit X-Position = 0; Radius = 1; Ohne Media
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Sphere, XPosition = 0, Radius = 10, ScatteringCoeffizient = 0.1f, RefractionIndex = 1.0f} //Himmel mit X-Position = 0; Radius = 10; Media mit ScatterCoeffizient = 0.1
                }, 0);
            MediaLine sut = data.MediaIntersectionFinder.CreateMediaShortLineNoAirBlocking(data.MediaIntersectionFinder.CreateCameraMediaStartPoint(startPoint), new Vector3D(1, 0, 0), float.MaxValue, 0);

            var subLine = sut.CreateShortMediaSubLine(2);
            Assert.AreEqual(1, subLine.Segments.Count);

            //Media-Segment
            Assert.AreEqual(0, subLine.Segments[0].RayMin);
            Assert.AreEqual(2, subLine.Segments[0].RayMax);
            Assert.IsTrue(subLine.Segments[0].Media.HasScatteringSomeWhereInMedium());

            //Line-StartPoint
            Assert.AreEqual(MediaPointLocationType.Camera, subLine.StartPoint.Location);
            Assert.AreEqual(1.1f, subLine.StartPoint.Position.X);
            Assert.IsTrue(subLine.StartPoint.CurrentMedium.HasScatteringSomeWhereInMedium());

            //Line-EndPoint
            Assert.AreEqual(3.1f, subLine.EndPoint.Position.X);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, subLine.EndPointLocation);
            Assert.IsTrue(subLine.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium());

            //Line-Pdf
            Assert.IsTrue(Math.Abs(GetPdfLForStopInMedia(2) - subLine.SampledPdfL().PdfL) < 0.0001f, "ExpectedPdfL=" + GetPdfLForStopInMedia(2) + ", Line-PdfL=" + subLine.SampledPdfL().PdfL);
            Assert.IsTrue(Math.Abs(GetPdfLForGoThroughMedia(2) - subLine.SampledPdfL().ReversePdfL) < 0.0001f, "ExpectedPdfLReverse=" + GetPdfLForStopInMedia(2) + ", Line-ReversePdfL=" + subLine.SampledPdfL().ReversePdfL);
        }

        [TestMethod]
        public void CreateLongMediaSubLine_CalledForLineWatchIntoSky_TwoSegmentsReturned()
        {
            Vector3D startPoint = new Vector3D(1.1f, 0, 0);
            var data = MediaIntersectionFinderTestHelper.CreateIntersectionDataWithMedia(new List<IntersectableTestObject>() {
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Sphere, XPosition = 0, Radius = 1 },//Erde mit X-Position = 0; Radius = 1; Ohne Media
                    new IntersectableTestObject(){ Type = IntersectableTestObject.ObjectType.Sphere, XPosition = 0, Radius = 10, ScatteringCoeffizient = 0.1f, RefractionIndex = 1.0f} //Himmel mit X-Position = 0; Radius = 10; Media mit ScatterCoeffizient = 0.1
                }, 0);
            MediaLine sut = data.MediaIntersectionFinder.CreateMediaShortLineNoAirBlocking(data.MediaIntersectionFinder.CreateCameraMediaStartPoint(startPoint), new Vector3D(1, 0, 0), float.MaxValue, 0);

            var subLine = sut.CreateLongMediaSubLine(2);
            Assert.AreEqual(2, subLine.Segments.Count);

            //Media-Segment bis zum EndPoint
            Assert.AreEqual(0, subLine.Segments[0].RayMin);
            Assert.AreEqual(2, subLine.Segments[0].RayMax);
            Assert.IsTrue(subLine.Segments[0].Media.HasScatteringSomeWhereInMedium());

            //Media-Segment bis zum EndPoint
            Assert.AreEqual(2, subLine.Segments[1].RayMin);
            Assert.AreEqual(8.9f, subLine.Segments[1].RayMax);
            Assert.IsTrue(subLine.Segments[1].Media.HasScatteringSomeWhereInMedium());

            //Line-StartPoint
            Assert.AreEqual(MediaPointLocationType.Camera, subLine.StartPoint.Location);
            Assert.AreEqual(1.1f, subLine.StartPoint.Position.X);
            Assert.IsTrue(subLine.StartPoint.CurrentMedium.HasScatteringSomeWhereInMedium());

            //Line-EndPoint
            Assert.AreEqual(3.1f, subLine.EndPoint.Position.X);
            Assert.AreEqual(MediaPointLocationType.MediaParticle, subLine.EndPointLocation);
            Assert.IsTrue(subLine.EndPoint.CurrentMedium.HasScatteringSomeWhereInMedium());

            //Line-Pdf
            Assert.IsTrue(Math.Abs(GetPdfLForStopInMedia(2) - subLine.SampledPdfL().PdfL) < 0.0001f, "ExpectedPdfL=" + GetPdfLForStopInMedia(2) + ", Line-PdfL=" + subLine.SampledPdfL().PdfL);
            Assert.IsTrue(Math.Abs(GetPdfLForGoThroughMedia(2) - subLine.SampledPdfL().ReversePdfL) < 0.0001f, "ExpectedPdfLReverse=" + GetPdfLForStopInMedia(2) + ", Line-ReversePdfL=" + subLine.SampledPdfL().ReversePdfL);
        }

        private float GetPdfLForStopInMedia(float distance)
        {
            float attenuationCoeffizent = 0.1f;
            float goThroughMediaPdf = Math.Max((float)Math.Exp(-attenuationCoeffizent * distance), 1e-35f);
            float stopInMediaPdf = goThroughMediaPdf * attenuationCoeffizent;
            return stopInMediaPdf;
        }

        private float GetPdfLForGoThroughMedia(float distance)
        {
            float attenuationCoeffizent = 0.1f;
            float goThroughMediaPdf = Math.Max((float)Math.Exp(-attenuationCoeffizent * distance), 1e-35f);
            //float stopInMediaPdf = goThroughMediaPdf * attenuationCoeffizent;
            return goThroughMediaPdf;
        }
    }
}
