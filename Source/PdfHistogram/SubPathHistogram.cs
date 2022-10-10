using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using BitmapHelper;
using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia;
using RayObjects.RayObjects;
using RaytracingRandom;
using SubpathGenerator;

namespace PdfHistogram
{
    public class SubPathHistogram
    {
        class PathData
        {
            public List<double> PointPdfAs = new List<double>(); //Das ist die PdfA vom letzten PathPoint vom jeweiligen Subpath
            public string PathCode { get; private set; }
            public int PathLength { get; private set; }
            public double DifferentialArea { get; private set; } //Geometrische Summe alle dA-Flächen von sein zugehörigen Pfad-Punkten

            public PathData(string pathCode, int pathLength, double differentialArea)
            {
                this.PathCode = pathCode;
                this.PathLength = pathLength;
                this.DifferentialArea = differentialArea;
            }
        }

        class DoNothing { }
        private QuadListChunkTable<DoNothing> surfacePointToIndexConverter; //Konvertiert ein 3D-Oberflächen-Punkt in eine dA-Fläche (Fläche hat Index + Flächeninhalt)
        private AxialCubeChunkTable<DoNothing> volumePointToIndexConverter = null; //Konvertiert ein 3D-Volumen-Punkt in ein dV-Volumenelement mit zugehörigen Index
        private Dictionary<string, PathData> pathHistogram = new Dictionary<string, PathData>();
        private int sampleCounter = 0; 

        public SubPathHistogram(List<RayQuad> quads, BoundingBox mediaBox, int histogramSize)
        {
            this.surfacePointToIndexConverter = new QuadListChunkTable<DoNothing>(quads, histogramSize);

            if (mediaBox != null)
                this.volumePointToIndexConverter = new AxialCubeChunkTable<DoNothing>(mediaBox, histogramSize);
        }

        public void AddPathToHistogram(PathPoint[] points, int startIndex)
        {
            if (points.Last().LocationType == MediaPointLocationType.MediaInfinity) return;
            //if (points.Any(x => x.LocationType == MediaPointLocationType.MediaBorder)) return;

            this.sampleCounter++;            
            StringBuilder pathString = new StringBuilder(startIndex == 1 ? "c;" : ""); //c == Camera-Punkt;S = Surface-Punkt; V = MediaParticle; B=MediaBorder
            double pathDifferentialArea = 1;
            for (int i = startIndex; i < points.Length; i++)
            {
                string indexString = null;
                double differential = double.NaN;
                switch (points[i].LocationType)
                {
                    case MediaPointLocationType.Surface:
                        var field1 = this.surfacePointToIndexConverter[points[i].SurfacePoint];
                        indexString = "S" + field1.Index;
                        differential = field1.DifferentialArea;
                        break;
                    case MediaPointLocationType.MediaParticle:
                        var field2 = this.volumePointToIndexConverter[points[i].Position];
                        indexString = "V" + field2.Index;
                        differential = this.volumePointToIndexConverter.DifferentialVolume;
                        break;
                    case MediaPointLocationType.NullMediaBorder:
                        var field3 = this.volumePointToIndexConverter[points[i].Position];
                        indexString = "B" + field3.Index;
                        differential = 1;
                        break;
                    default:
                        throw new Exception("Unknown enum" + points[i].LocationType);
                }

                pathString.Append(indexString);
                string pathCode = pathString.ToString();
                int pathLength = i + 1;
                pathDifferentialArea *= differential;
                if (this.pathHistogram.ContainsKey(pathCode) == false) this.pathHistogram.Add(pathCode, new PathData(pathCode, pathLength, pathDifferentialArea));
                this.pathHistogram[pathCode].PointPdfAs.Add(points[i].PdfA);
                pathString.Append(";");
            }
        }

        class PdfAData
        {
            public double HistogramPdfA;
            public double PropertyPdfA;
        }

        private PdfAData[] GetDataFromSinglePathLength(int pathLength)
        {
            return this.pathHistogram.Where(x => x.Value.PathLength == pathLength).
                //Where(x => x.Value.PointPdfAs.Count > 20).
                Select(x => new PdfAData()
                    {
                        HistogramPdfA = x.Value.PointPdfAs.Count / (double)this.sampleCounter / x.Value.DifferentialArea,
                        PropertyPdfA = x.Value.PointPdfAs.Average()
                    }).OrderBy(x => x.PropertyPdfA).ToArray();
        }

        private SimpleFunction GetPdfFunctionFromHistogramForSinglePathLength(int pathLength)
        {
            var histo = GetDataFromSinglePathLength(pathLength);

            return new SimpleFunction((x) =>
            {
                if (x < 0 || x > 1 || histo.Length == 0) return 0;

                int index = Math.Min((int)(x * (histo.Length)), histo.Length - 1);

                return histo[index].HistogramPdfA;
            });
        }

        private SimpleFunction GetPdfFunctionFromPdfPropertyForSinglePathLength(int pathLength)
        {
            var histo = GetDataFromSinglePathLength(pathLength);

            return new SimpleFunction((x) =>
            {
                if (x < 0 || x > 1 || histo.Length == 0) return 0;

                int index = Math.Min((int)(x * (histo.Length)), histo.Length - 1);

                return histo[index].PropertyPdfA;
            });
        }

        private Bitmap GetPlotterImageForSinglePathLength(int width, int height, int pathLength, out int error)
        {
            List<FunctionToPlot> functions = new List<FunctionToPlot>();
            functions.Add(new FunctionToPlot() { Function = GetPdfFunctionFromHistogramForSinglePathLength(pathLength), Color = Color.Blue, Text = "Histogram" });
            functions.Add(new FunctionToPlot() { Function = GetPdfFunctionFromPdfPropertyForSinglePathLength(pathLength), Color = Color.Red, Text = "PdfA" });
            //FunctionPlotter plotter = new FunctionPlotter(new SizeF(1,0.04f), new SizeF(1,0.04f), 0, 1, new Size(width, height));
            FunctionPlotter plotter = new FunctionPlotter(new SizeF(1, 0.04f), 0, 1, new Size(width, height));
            error = (int)FunctionComparer.GetMaxDifferenceAreaBetweenFunctions(0, 1, 0.05f, GetPdfFunctionFromHistogramForSinglePathLength(pathLength), GetPdfFunctionFromPdfPropertyForSinglePathLength(pathLength));
            int sampleCountFromGivenPathLength = this.pathHistogram.Where(x => x.Value.PathLength == pathLength).Sum(x => x.Value.PointPdfAs.Count);
            string customText = "[" + pathLength + "] Error=" +
                error +
                " (" + sampleCountFromGivenPathLength + ")";
            return plotter.PlotFunctions(functions, customText);
        }

        public TestResult GetTestResult(int width, int height)
        {
            int[] pathLengths = this.pathHistogram.GroupBy(x => x.Value.PathLength).Select(x => x.Key).OrderBy(x => x).ToArray();

            int[] errorValues = new int[pathLengths.Max() + 1];

            List<Bitmap> images = new List<Bitmap>();
            int error = int.MaxValue;
            foreach (var pathLength in pathLengths)
            {
                Bitmap image = GetPlotterImageForSinglePathLength(width, height, pathLength, out error);
                images.Add(image);
                errorValues[pathLength] = error;
            }

            return new TestResult()
            {
                Image = BitmapHelp.TransformBitmapListToRow(images),
                Error = errorValues,
            };
        }

        public class TestResult
        {
            public Bitmap Image;
            public int[] Error;

            public int MaxErrorUptoGivenPathLength(int pathLength)
            {
                int error = 0;
                for (int i = 0; i <= pathLength; i++)
                    if (this.Error[i] > error) error = this.Error[i];
                return error;
            }
        }
    }
}
