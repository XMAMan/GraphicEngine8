﻿using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using SubpathGenerator;
using GraphicGlobal;
using RayCameraNamespace;
using ParticipatingMedia.DistanceSampling;
using RayTracerGlobal;
using FullPathGenerator.FullpathSampling_Methods;

namespace FullPathGenerator
{
    //Jeder Fullpath, der vom LightTracing erzeugt wird, wird gedanklich mit allen Pixeln der Bildebene verbunden. Aber
    //beim Equal-Sampling läßt der PixelFilter nur ein Pixel, und beim Tent-Sampling 9 Pixel durch. Aus dem Grund 
    //kommt in der Pfad-PdfA lediglich die Anzahl der erzeugten LightFullPaths(==PixelCount) vor aber keine Unterscheidung
    //zwischen Equal- und Tent-Sampling. Außerdem ist die CameraPdfW-Multiplikation beim Pfadgewicht der PixelFilter.
    //Mit dem eyeSubPathUseRandomPixelSelect-Schalter kann ich festelegen, ob ich diese Klasse über das
    //IFullPathSamplingMethod-Interface (eyeSubPathUseRandomPixelSelect==False) oder über das ISingleFullPathSampler (True) nutzen werde
    public class LightTracing : IFullPathSamplingMethod, ISingleFullPathSampler
    {
        private readonly IRayCamera rayCamera;
        private readonly PointToPointConnector pointToPointConnector;
        private readonly int sampleCountForGivenPathLength;
        private readonly bool checkThatEachPointIsASurfacePoint;
        private readonly bool noDistanceSampling;

        //Wenn ich Pixel-Tentsampling nutze, dann sind das die 9 Pixel, die um den getroffenen Pixel noch mit erzeugt werden
        private readonly Vector2D[] pixelShifts = new Vector2D[] { new Vector2D(+0, +0), new Vector2D(-1, -1), new Vector2D(-1, +1), new Vector2D(+1, -1), new Vector2D(+1, +1), new Vector2D(+0, -1), new Vector2D(+0, +1), new Vector2D(-1, 0), new Vector2D(+1, 0) };

        //So viel Samples werden pro Pfadlänge pro Frame erzeugt. Im Regelfall werden pro Frame so viele LightTracing-Samples erzeugt,
        //wie es Pixel gibt. Wähle ich aber pro Frame zufällig ein Pixel aus, dann kommt auf ein Pathtracing-Pfad ein Lighttracing-Pfad.
        //In diesen Falle ist perFrameSampleCount 1. 
        private readonly int perFrameSampleCount;
        private readonly float pixelSelectionPdf; //Mit der Wahrscheinlichkeit wählt der Subpfadsampler ein bestimmtes Pixel aus. Ist im Regefall 1 außer bei MMLT und SingelPathBPT
        private readonly float pixelShiftPdf; //Mit der Wahrscheinlichkeit wird bei Verwendung des Tent-Filters ein einzelner Tent-Pixel-Shift ausgewählt

        public LightTracing(IRayCamera rayCamera, PointToPointConnector pointToPointConnector, PathSamplingType usedLightSubPathType, bool eyeSubPathUseRandomPixelSelect)
        {
            this.noDistanceSampling = usedLightSubPathType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling;
            this.checkThatEachPointIsASurfacePoint = usedLightSubPathType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling;
            this.rayCamera = rayCamera;
            this.pointToPointConnector = pointToPointConnector;
            //this.sampleCountForGivenPathLength = rayCamera.SamplingMode == PixelSamplingMode.Tent ? 9 : 1; //Wenn ich das so mache, dann ist das Bild im MIS-Verbund mit anderen Verfahren zu dunkel. Die 9 Samples sind nicht unabhängig über die gesamte Bildebene sondern konzentrieren sich immer nur um den einen Light-Subpath. Somit erzeuge ich quasi immer nur 1 Sample pro Light-Subpath-Diffuse-Punkt egal ob Tent oder Equal
            this.sampleCountForGivenPathLength = 1;

            //Wenn eyeSubPathUseRandomPixelSelect==true ist, dann wird pro Frame zufällig festgelegt, durch welches Pixel der Eye-Subpfad geht
            //Das wird bei MMLT und SinglePathBPT gemacht. In diesen Fall kommt auf ein Pathtracing-Sample ein Lighttracing-Sample und somit
            //darf die Lighttracing-Contribution nicht durch die PixelAnzahl dividiert werden und die Lightracing-PdfA nicht mit der PixelAnzahl multipliziert werden
            if (eyeSubPathUseRandomPixelSelect)
            {
                this.perFrameSampleCount = 1;
                this.pixelSelectionPdf = 1f / rayCamera.PixelCountFromScreen;
            }else
            {
                this.perFrameSampleCount = rayCamera.PixelCountFromScreen;
                this.pixelSelectionPdf = 1;
            }

            //Wenn ich Lighttracing bei MMLT oder SinglePathBPT nutzen will, dann wird eyeSubPathUseRandomPixelSelect true sein und
            //Ich werde pro Sampleschritt immer nur ein Pfad erzeugen dürfen. Deswegen erzeuge ich bei CameraTent nicht 9 sondern 
            //ein zufälligen Tent-Geshifteten Pfad
            if (eyeSubPathUseRandomPixelSelect && rayCamera.SamplingMode == PixelSamplingMode.Tent)
                this.pixelShiftPdf = 1f / pixelShifts.Length;
            else
                this.pixelShiftPdf = 1f;
        }

        public SamplingMethod Name => SamplingMethod.LightTracing;

        public int SampleCountForGivenPath(FullPath path)
        {
            if (path.PathLength > 2 && path.Points[1].IsDiffusePoint) return 1;
            return 0;
        }

        public List<FullPath> SampleFullPaths(SubPath eyePath, SubPath lightPath, FullPathFrameData frameData, IRandom rand)
        {
            return TryToCreatePaths(lightPath);
        }

        private List<FullPath> TryToCreatePaths(SubPath lightPath)
        {
            List<FullPath> paths = new List<FullPath>();
            if (lightPath == null) return paths;            

            for (int i = 1; i < lightPath.Points.Length; i++) //Index 0 ist die Lichtquelle
            {
                var lightPoint = lightPath.Points[i];
                if (lightPoint.IsDiffusePoint && lightPoint.IsLocatedOnLightSource == false)
                {
                    var connectData = this.pointToPointConnector.TryToConnectToCamera(lightPoint);

                    if (connectData != null)
                    {
                        var cameraPoint = connectData.CameraPoint;
                        cameraPoint.AssociatedPath = lightPath.Points[0].AssociatedPath;
                        cameraPoint.LineToNextPoint = connectData.LineFromCameraToLightPoint;

                        if (this.noDistanceSampling) connectData.PdfLForCameraToLightPoint = new DistancePdf() { PdfL = 1, ReversePdfL = 1 };

                        if (this.rayCamera.SamplingMode == PixelSamplingMode.Tent)
                            paths.AddRange(CreatePathForEachNeighborPixel(cameraPoint, lightPoint, connectData));
                        else
                            paths.Add(CreatePath(cameraPoint, lightPoint, connectData.PixelPosition, connectData));
                    }
                }
            }

            return paths;
        }

        private List<FullPath> CreatePathForEachNeighborPixel(PathPoint cameraPoint, PathPoint lightPoint, LightPoint2CameraConnectionData connectData)
        {
            return this.pixelShifts
                .Select(shift => CreatePath(cameraPoint, lightPoint, connectData.PixelPosition + shift, connectData))
                .Where(x => x != null)
                .ToList();
        }

        private FullPath CreatePath(PathPoint cameraPoint, PathPoint lightPoint, Vector2D pixelPosition, LightPoint2CameraConnectionData connectData)
        {
            SubPath lightPath = lightPoint.AssociatedPath;

            float cameraPdfW = this.rayCamera.GetPixelPdfW((int)pixelPosition.X, (int)pixelPosition.Y, connectData.CameraToLightPointDirection);
            if (cameraPdfW <= MagicNumbers.MinAllowedPdfW) return null;

            float pixelFilter = cameraPdfW;

            //Da beim Eye-Subpatherstellen die Division mit der Camera-PdfW fehlt, führt das implizit zur Muliplikattion mit der cameraPdfW beim Sampeln
            //Dieses Camera-PdfW-Pfadgewicht nennt man PixelFiler. D.h. ich multipliziere hier das Pfadgewicht eigentlich nicht mit der CameraPdfW
            //sondern mit dem PixelFilter. Ohne diesen PixelFilter wird das Bild um so dunkler, je kleiner der Kameraöffnungswinkel ist,
            //da der Bereich, von dem Photonen aufgesammelt werden, kleiner wird.
            //Dieser Verdunkelungseffekt passiert auch mit meiner Casio-Kamera. Wenn ich dieses physikalisch korrekten Verhalten nicht will, dann
            //muss ich die PixelFilter-Multiplikation weglassen
            Vector3D pathContribution = Vector3D.Mult(lightPoint.PathWeight, connectData.LightBrdf.Brdf) * connectData.GeometryTerm / (this.sampleCountForGivenPathLength * this.perFrameSampleCount) * pixelFilter / pixelShiftPdf;
            pathContribution = Vector3D.Mult(pathContribution, connectData.AttenuationTerm);
            double pathPdfA = lightPoint.PdfA * this.perFrameSampleCount * this.sampleCountForGivenPathLength * pixelShiftPdf;

            var points = new FullPathPoint[lightPoint.Index + 2];
            double eyePdfA = this.pixelSelectionPdf;

            points[0] = new FullPathPoint(cameraPoint, cameraPoint.LineToNextPoint, null, cameraPdfW, float.NaN, BrdfCreator.BrdfSampling)
            {
                EyePdfA = 1,
                LightPdfA = lightPoint.PdfA * PdfHelper.PdfWToPdfAOrV(connectData.LightBrdf.PdfW, lightPoint, cameraPoint) * connectData.PdfLForCameraToLightPoint.ReversePdfL
            };

            eyePdfA *= PdfHelper.PdfWToPdfAOrV(cameraPdfW, cameraPoint, lightPoint) * connectData.PdfLForCameraToLightPoint.PdfL;
            points[1] = new FullPathPoint(lightPoint, null, null, connectData.LightBrdf.PdfWReverse, connectData.LightBrdf.PdfW, BrdfCreator.BrdfEvaluation)
            {
                EyePdfA = eyePdfA,
                LightPdfA = lightPoint.PdfA
            };

            double pdfAReverseIndex2 = PdfHelper.PdfWToPdfAOrV(connectData.LightBrdf.PdfWReverse, lightPoint, lightPoint.Predecessor);
            eyePdfA = eyePdfA * pdfAReverseIndex2 * lightPoint.Predecessor.PdfLFromNextPointToThis;
            points[2] = new FullPathPoint(lightPoint.Predecessor, null, lightPoint.Predecessor.LineToNextPoint, lightPoint.Predecessor.BrdfSampleEventOnThisPoint.PdfWReverse, lightPoint.Predecessor.BrdfSampleEventOnThisPoint.PdfW, BrdfCreator.BrdfSampling)
            {
                EyePdfA = eyePdfA,
                LightPdfA = lightPoint.Predecessor.PdfA
            };

            for (int i = 3, j = lightPoint.Index - 2; i < points.Length; i++, j--)
            {
                eyePdfA = eyePdfA * lightPath.Points[j].PdfAReverse * lightPath.Points[j].PdfLFromNextPointToThis;
                points[i] = new FullPathPoint(lightPath.Points[j], null, lightPath.Points[j].LineToNextPoint, lightPath.Points[j].BrdfSampleEventOnThisPoint.PdfWReverse, lightPath.Points[j].BrdfSampleEventOnThisPoint.PdfW, BrdfCreator.BrdfSampling)
                {
                    EyePdfA = eyePdfA,
                    LightPdfA = lightPath.Points[j].PdfA
                };
            }

            FullPath path = new FullPath(pathContribution, pathPdfA, points, this)
            {
                PixelPosition = pixelPosition
            };
            return path;
        }

        public double GetPathPdfAForAGivenPath(FullPath path, FullPathFrameData frameData)
        {
            if (this.checkThatEachPointIsASurfacePoint && path.IsSurfacePathOnly() == false) return 0;
            if (path.Points.Length > 2 && path.Points[1].IsDiffusePoint && path.Points[1].IsLocatedOnLightSource == false) //Index 1 ist der Punkt, den die Kamera ansieht
            {
                return path.Points[1].LightPdfA * this.perFrameSampleCount * this.sampleCountForGivenPathLength * pixelShiftPdf;
            }
            return 0;
        }

        #region ISingleFullPathSampler
        public FullPathSamplingStrategy[] GetAvailableStrategiesForFullPathLength(int fullPathLength)
        {
            if (fullPathLength <= 2) return new FullPathSamplingStrategy[0];

            return new FullPathSamplingStrategy[]
            {
                new FullPathSamplingStrategy()
                {
                    NeededEyePathLength = 0,
                    NeededLightPathLength = fullPathLength - 1,
                    StrategyIndex = 0
                }
            };
        }

        
        public FullPath SampleFullPathFromSingleStrategy(SubPath eyePath, SubPath lightPath, int fullPathLength, int strategyIndex, IRandom rand)
        {
            var lightPoint = lightPath.Points[fullPathLength - 2];
            if (lightPoint.IsDiffusePoint == false || lightPoint.IsLocatedOnLightSource) return null;

            var connectData = this.pointToPointConnector.TryToConnectToCamera(lightPoint);

            if (connectData != null)
            {
                var cameraPoint = connectData.CameraPoint;
                cameraPoint.AssociatedPath = lightPath.Points[0].AssociatedPath;
                cameraPoint.LineToNextPoint = connectData.LineFromCameraToLightPoint;

                if (this.noDistanceSampling) connectData.PdfLForCameraToLightPoint = new DistancePdf() { PdfL = 1, ReversePdfL = 1 };

                if (this.rayCamera.SamplingMode == PixelSamplingMode.Tent)
                {
                    int shiftIndex = rand.Next(this.pixelShifts.Length);
                    return CreatePath(cameraPoint, lightPoint, connectData.PixelPosition + this.pixelShifts[shiftIndex], connectData);
                }

                return CreatePath(cameraPoint, lightPoint, connectData.PixelPosition, connectData);
            }

            return null;
        }
        #endregion 
    }
}
