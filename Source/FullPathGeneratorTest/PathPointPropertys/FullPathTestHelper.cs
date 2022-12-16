using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;
using Photonusmap;
using RaytracingBrdf;
using RaytracingColorEstimator;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.Threading;
using TriangleObjectGeneration;

namespace FullPathGeneratorTest.PathPointPropertys
{
    //Enthält all die Daten, um ein IFullPathSamplingMethod zu erstellen und dann auch dessen Methoden aufrufen zu können
    class FullPathTestData
    {
        public FullPathKonstruktorData FullPathKonstruktorData;
        public SubPath EyePath;
        public SubPath LightPath;
        public FullPathFrameData FrameData;
    }

    //Mit diesen Daten kann ich dann prüfen, ob die Fullpathsampmler richtig arbeiten
    class ExpectedValues
    {
        public float Emission = 10;
        public float ScatteringCoeffizient = 0.5f;
        public float PhotonSearchRadius = 0.05f;
        public float Attenuation;       //Attenuation für Homogenes Medium der Länge 0.5 and ScatteringCoeffizient von 0.5
        public float PdfLEndInMedia;
        public float PdfLLeaveMedia;
        public float PixelFilter = 1.457107f;
        public float PhaseFunction;
        public float GeometryTerm = 1.0f / (2 * 2);
        public float AnisotropyCoeffizient = 0.8f;
        public int PhotonenCount = 5;
        public ExpectedValues()
        {
            float halfAirCubeSize = 0.5f; //Halbe Kantenlänge des Air-Cubes

            this.Attenuation = (float)Math.Exp(-this.ScatteringCoeffizient * halfAirCubeSize);
            this.PdfLEndInMedia = this.ScatteringCoeffizient * this.Attenuation; //PdfL um halfAirCubeSize-Distanz zu sampeln und dann im Medium stecken zu bleiben
            this.PdfLLeaveMedia = this.Attenuation;

            if (this.AnisotropyCoeffizient == 0)
            {
                this.PhaseFunction = 1.0f / (4 * (float)Math.PI); //Isotrophic Phase-Function
            }else
            {
                float cosTheta = 0;
                float squareMeanCosine = this.AnisotropyCoeffizient * this.AnisotropyCoeffizient;
                float d = 1 + squareMeanCosine - (this.AnisotropyCoeffizient + this.AnisotropyCoeffizient) * cosTheta;

                this.PhaseFunction = d > 0 ? (1.0f / (4 * (float)Math.PI) * (1 - squareMeanCosine) / (d * (float)Math.Sqrt(d))) : 0;
            }            
        }
    }

    class TestData
    {
        public FullPathTestData Full;
        public ExpectedValues ExpectedValues;
    }

    class FullPathTestHelper
    {
        public static TestData CreateFullPathConstructorData()
        {
            var expectedValues = new ExpectedValues();

            var airCube = new DrawingObject(TriangleObjectGenerator.CreateCube(0.5f, 0.5f, 0.5f),
                new ObjectPropertys()
                {
                    TextureFile = "#FFFFFF",
                    Position = new Vector3D(0, 0, 0),
                    MediaDescription = new DescriptionForHomogeneousMedia() 
                    { 
                        ScatteringCoeffizent = new Vector3D(1, 1, 1) * expectedValues.ScatteringCoeffizient,
                        AnisotropyCoeffizient = expectedValues.AnisotropyCoeffizient
                    },
                    RefractionIndex = 1
                });
            var lightPlane = new DrawingObject(TriangleObjectGenerator.CreateSquareXY(0.5f, 0.5f, 1),
                new ObjectPropertys()
                {
                    TextureFile = "#FFFFFF",
                    Position = new Vector3D(-2, 0, 0),
                    Orientation = new Vector3D(0, 90, 0),
                    RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = expectedValues.Emission }
                });

            var data = PixelRadianceCreationHelper.CreatePixelRadianceData(new RaytracingFrame3DData()
            {
                GlobalObjektPropertys = new GlobalObjectPropertys()
                {
                    Camera = new Camera(new Vector3D(0, 0, -2), new Vector3D(0, 0, 1), 45)
                },
                DrawingObjects = new List<DrawingObject>() { airCube, lightPlane },
                ScreenWidth = 1,
                ScreenHeight = 1,
                ProgressChanged = (s, f) => { },
                StopTrigger = new CancellationTokenSource(),
                PixelRange = new ImagePixelRange(0, 0, 1, 1)
            }, new SubPathSettings()
            {
                EyePathType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                LightPathType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling,
                MaxEyePathLength = 5
            },
            null);

            var fullPathData = new FullPathKonstruktorData()
            {
                EyePathSamplingType = data.EyePathSampler.PathSamplingType,
                LightPathSamplingType = data.LightPathSampler.PathSamplingType,
                PointToPointConnector = new PointToPointConnector(new RayVisibleTester(data.IntersectionFinder, data.MediaIntersectionFinder), data.RayCamera, data.EyePathSampler.PathSamplingType, new PhaseFunction()),
                RayCamera = data.RayCamera,
                LightSourceSampler = data.LightSourceSampler,
                MaxPathLength = 5
            };

            var eyePath = data.EyePathSampler.SamplePathFromCamera(0, 0, new RandMock(new List<double>()
            {
                0.5,  //PixelMitte X
                0.5,  //PixelMitte Y
                0,  //PathCreationTime
                0.77880078307140488,// Distanz-Sampling von MediaBorder zur Position (0,0,0)
                //0,    //ContinationPdf PhasenFunktion; Da die ContinationPdf 1 ist, wird die Absorbation nicht gesampelt

                //Wenn ich mit der Isotrophic-Phase eine Richtung (-1,0,0) sampeln will:
                //0.5,  //Phasenfunktion Richtung Phi
                //0.5,  //Phasenfunktion Richtung Theta   (RIchtung (-1,0,0)

                //Wenn ich mit der Anisotrophic-Phase mit anisotropyCoeffizient = 0.8 eine Richtung (-1,0,0) sampeln will:
                0.050695478489474748,
                3.0 / 4,

                0.2,  //Distanz-Sampling z            (Distanz bis MediaBorder)
            }));


            var lightPath = data.LightPathSampler.SamplePathFromLighsource(new RandMock(new List<double>()
            {
                0,  //Lichtquelle 0 auswählen
                0,  //Dreieck 0 auswählen
                1,  //Position auf Dreieck
                0.5,     //Position auf Dreieck (-2,0,0)
                0,  //Phi = 0
                1,  //Cos²Theta = 1 -> Richtung auf Lichtquelle (1,0,0)
                0, //PathCreationTime
                0.77880078307140488,// Distanz-Sampling von MediaBorder zur Position (0,0,0)
                0,    //ContinationPdf PhasenFunktion

                //Wenn ich mit der Isotrophic-Phase eine Richtung (0,1,0) sampeln will:
                //0.25,  //Phasenfunktion Richtung Phi
                //0.5,  //Phasenfunktion Richtung Theta   (RIchtung (0,1,0)

                 //Wenn ich mit der Anisotrophic-Phase mit anisotropyCoeffizient = 0.8 eine Richtung (0,1,0) sampeln will:
                0.050695478489474748,
                0.25,

                0.2,  //Distanz-Sampling z            (Distanz bis MediaBorder)
            }));

            var lightPaths = new List<SubPath>();
            for (int i = 0; i < expectedValues.PhotonenCount; i++) lightPaths.Add(lightPath);
            var frameData = new FullPathFrameData()
            {
                PhotonMaps = new Photonmaps()
                {
                     PointDataPointQueryMap = new PointDataPointQueryMap(lightPaths, expectedValues.PhotonenCount, (s, f) => { }) {  SearchRadius = expectedValues.PhotonSearchRadius },
                     PointDataBeamQueryMap = new PointDataBeamQueryMap(lightPaths, expectedValues.PhotonenCount, (s, f) => { }, expectedValues.PhotonSearchRadius)
                }
            };

            var fullPathTestData = new FullPathTestData()
            {
                FullPathKonstruktorData = fullPathData,
                EyePath = eyePath,
                LightPath = lightPath,
                FrameData = frameData,                
            };

            return new TestData()
            {
                Full = fullPathTestData,
                ExpectedValues = expectedValues
            };
        }
    }
}
