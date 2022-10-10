using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParticipatingMedia.PhaseFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullPathGeneratorTest.PathPointPropertys
{
    //Erzeugt lauter gemockte CPL-Pfade und überprüft die Pfad-Propertys
    [TestClass]
    public class CPLTests
    {
        private readonly float MaxError = 0.0001f;

        [TestMethod]
        public void PathTracing()
        {
            var data = FullPathTestHelper.CreateFullPathConstructorData();
            var e = data.ExpectedValues;
            var fullPathSampler = new PathTracing(data.Full.FullPathKonstruktorData.LightSourceSampler, data.Full.FullPathKonstruktorData.EyePathSamplingType);
            var fullPath = fullPathSampler.SampleFullPaths(data.Full.EyePath, data.Full.LightPath, data.Full.FrameData, new Rand(0))[0];
            var points = fullPath.Points;

            Assert.AreEqual(new Vector3D(0, 0, -2), points[0].Position); //Kamera

            Assert.AreEqual(new Vector3D(0, 0, 0), points[1].Position); //Partikel
            Assert.AreEqual(e.Attenuation / e.PdfLEndInMedia, points[1].Point.PathWeight.X); //Hier wird lediglich eine Distanz gesampelt

            Assert.IsTrue( (new Vector3D(-2f, 0, 0f) - points[2].Position).Max() < MaxError); //Lichtquelle
            //Hier wird eine Richtung gesampelt. PhaseBrdf/PdfW kürzt sich weg. Es bleibt nur der Scatteringkoeffizient und die Attenuation/PdfL zum Verlassen des Mediums
            Assert.AreEqual(e.ScatteringCoeffizient * e.Attenuation / e.PdfLLeaveMedia * points[1].Point.PathWeight.X, points[2].Point.PathWeight.X);
            
            Assert.AreEqual(new Vector3D(10, 10, 10), fullPath.PathContribution);
            Assert.AreEqual(fullPath.PathPdfA, fullPathSampler.GetPathPdfAForAGivenPath(fullPath, data.Full.FrameData));
        }

        [TestMethod]
        public void LightTracing()
        {
            var data = FullPathTestHelper.CreateFullPathConstructorData();
            var e = data.ExpectedValues;
            var fullPathSampler = new LightTracing(data.Full.FullPathKonstruktorData.RayCamera, data.Full.FullPathKonstruktorData.PointToPointConnector,  data.Full.FullPathKonstruktorData.EyePathSamplingType);
            var fullPath = fullPathSampler.SampleFullPaths(data.Full.EyePath, data.Full.LightPath, data.Full.FrameData, new Rand(0))[0];
            var points = fullPath.Points;

            Assert.AreEqual(new Vector3D(0, 0, -2), points[0].Position); //Kamera

            Assert.AreEqual(new Vector3D(0, 0, 0), points[1].Position); //Partikel
            float pdfWOnLight = 1 / (float)Math.PI;
            //Auf der Lichtquelle wird ein Punkt mit PdfA==1 und eine Richtung mit PdfW=1/PI gesampelt. Danach wird eine Distanz gesampelt, wo man im Medium stecken bleibt
            Assert.AreEqual(e.Emission / pdfWOnLight * e.Attenuation / e.PdfLEndInMedia, points[1].Point.PathWeight.X);
            
            Assert.AreEqual(new Vector3D(-2, 0, 0), points[2].Position); //Lichtquelle
            //Um nun vom Partikel zur Kamera zu kommen, muss die Phasenfunktion abgefragt werden.
            //Das heißt dort erhalte ich: data.ScatteringCoeffizient * uniformPhase
            //Dann kommt noch der Geometry-Term und Pixelfilter und Attenuation hinzu

            float pathContribution = points[1].Point.PathWeight.X * e.Attenuation * e.ScatteringCoeffizient * e.PhaseFunction * e.GeometryTerm * e.PixelFilter;
            Assert.IsTrue(Math.Abs(pathContribution - fullPath.PathContribution.X) < MaxError, "pathContribution=" + pathContribution + "; PathContribution=" + fullPath.PathContribution.X); ;
            Assert.AreEqual(fullPath.PathPdfA, fullPathSampler.GetPathPdfAForAGivenPath(fullPath, data.Full.FrameData));
        }

        [TestMethod]
        public void PointDataBeamQuery()
        {
            var data = FullPathTestHelper.CreateFullPathConstructorData();
            var e = data.ExpectedValues;
            var fullPathSampler = new PointDataBeamQuery(data.Full.FullPathKonstruktorData.EyePathSamplingType, data.Full.FullPathKonstruktorData.MaxPathLength);
            var fullPath = fullPathSampler.SampleFullPaths(data.Full.EyePath, data.Full.LightPath, data.Full.FrameData, new Rand(0))[0];
            var points = fullPath.Points;

            Assert.AreEqual(new Vector3D(0, 0, -2), points[0].Position);  //Kamera
            Assert.AreEqual(new Vector3D(0, 0, 0), points[1].Position); //Partikel
            Assert.AreEqual(new Vector3D(-2, 0, 0), points[2].Position); //Lichtquelle

            //Auf der Lichtquelle wird ein Punkt mit PdfA==1 und eine Richtung mit PdfW=1/PI gesampelt. Danach wird eine Distanz gesampelt, wo man im Medium stecken bleibt
            float pdfWOnLight = 1 / (float)Math.PI;
            float particlePathWeightFromLight = e.Emission / pdfWOnLight * e.Attenuation / e.PdfLEndInMedia;

            //PixelFilter / CameraPdfW ergibt 1. Es bleibt nur die Attenuation (Ohne Distanzsampling)
            float particlePathweightFromEye = e.Attenuation;

            //Beim Verbinden noch die Phasen-Funktion und Kernel hinzu
            float kernel2D = 1.0f / (e.PhotonSearchRadius * e.PhotonSearchRadius * (float)Math.PI);
            float pathContribution = particlePathWeightFromLight * particlePathweightFromEye * e.ScatteringCoeffizient * e.PhaseFunction * kernel2D / e.PhotonenCount;

            Assert.IsTrue(Math.Abs( pathContribution - fullPath.PathContribution.X) < MaxError, "pathContribution=" + pathContribution+"; PathContribution=" + fullPath.PathContribution.X); ;
            Assert.AreEqual(fullPath.PathPdfA, fullPathSampler.GetPathPdfAForAGivenPath(fullPath, data.Full.FrameData));
        }

        [TestMethod]
        public void PointDataPointQuery()
        {
            var data = FullPathTestHelper.CreateFullPathConstructorData();
            var e = data.ExpectedValues;
            var fullPathSampler = new PointDataPointQuery(data.Full.FullPathKonstruktorData.MaxPathLength);
            var fullPath = fullPathSampler.SampleFullPaths(data.Full.EyePath, data.Full.LightPath, data.Full.FrameData, new Rand(0))[0];
            var points = fullPath.Points;

            Assert.AreEqual(new Vector3D(0, 0, -2), points[0].Position);  //Kamera
            Assert.AreEqual(new Vector3D(0, 0, 0), points[1].Position); //Partikel
            Assert.AreEqual(new Vector3D(-2, 0, 0), points[2].Position); //Lichtquelle

            //Auf der Lichtquelle wird ein Punkt mit PdfA==1 und eine Richtung mit PdfW=1/PI gesampelt. Danach wird eine Distanz gesampelt, wo man im Medium stecken bleibt
            float pdfWOnLight = 1 / (float)Math.PI;
            float particlePathWeightFromLight = e.Emission / pdfWOnLight * e.Attenuation / e.PdfLEndInMedia;

            //PixelFilter / CameraPdfW ergibt 1. Es wird Distanz von Kamera bis zum Partikelpunkt gesampelt
            float particlePathweightFromEye = e.Attenuation / e.PdfLEndInMedia;

            //Beim Verbinden noch die Phasen-Funktion und Kernel hinzu
            float kernel3D = 1.0f / ((4.0f / 3) * e.PhotonSearchRadius * e.PhotonSearchRadius * e.PhotonSearchRadius * (float)Math.PI);
            float pathWeight = particlePathWeightFromLight * particlePathweightFromEye * e.ScatteringCoeffizient * e.PhaseFunction * kernel3D / e.PhotonenCount;

            Assert.IsTrue(Math.Abs(pathWeight - fullPath.PathContribution.X) < MaxError, "pathWeight=" + pathWeight + "; PathContribution=" + fullPath.PathContribution.X);
            Assert.AreEqual(fullPath.PathPdfA, fullPathSampler.GetPathPdfAForAGivenPath(fullPath, data.Full.FrameData));
        }
    }
}
