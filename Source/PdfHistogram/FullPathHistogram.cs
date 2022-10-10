using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using BitmapHelper;
using FullPathGenerator;
using GraphicMinimal;
using ParticipatingMedia;
using RayObjects.RayObjects;
using RaytracingRandom;
using SubpathGenerator;

namespace PdfHistogram
{
    public class FullPathHistogram
    {
        class PathData
        {
            public List<double> PathPdfAs = new List<double>(); //Das ist die PdfA vom gesampelten Pfad laut Property (FullPath.PathPdfA)
            public string PathCode { get; private set; }
            public int PathLength { get; private set; }
            public double DifferentialArea { get; private set; } //Geometrische Summe alle dA-Flächen von sein zugehörigen Pfad-Punkten

            //Hier werden dann nach dem Histogram-Erstellen die Ergebnisse gespeichert
            //public double PathPdfA = double.NaN;   //PdfA laut FullPath.PathPdfA
            //public double SamplePdfA = double.NaN;  //PdfA laut Histogram
            //public double Difference = double.NaN;

            public PathData(string pathCode, int pathLength, double differentialArea)
            {
                this.PathCode = pathCode;
                this.PathLength = pathLength;
                this.DifferentialArea = differentialArea;
            }
        }

        class DoNothing { }
        private QuadListChunkTable<DoNothing> surfacePointToIndexConverter; //Konvertiert ein 3D-Punkt in eine dA-Fläche (Fläche hat Index + Flächeninhalt)
        private AxialCubeChunkTable<DoNothing> volumePointToIndexConverter = null; //Konvertiert ein 3D-Volumen-Punkt in ein dV-Volumenelement mit zugehörigen Index
        private Dictionary<string, PathData> pathHistogram = new Dictionary<string, PathData>();
        private int sampleCounter = 0;

        public FullPathHistogram(List<RayQuad> quads, BoundingBox mediaBox, int histogramSize, int sampleCount)
        {
            this.sampleCounter = sampleCount;
            this.surfacePointToIndexConverter = new QuadListChunkTable<DoNothing>(quads, histogramSize);

            if (mediaBox != null)
                this.volumePointToIndexConverter = new AxialCubeChunkTable<DoNothing>(mediaBox, histogramSize);
        }

        public void AddPathToHistogram(FullPath path, int startIndex)
        {
            //this.sampleCounter++;

            StringBuilder pathString = new StringBuilder(startIndex == 1 ? "c;" : ""); //c == Camera-Punkt    
            double pathDifferentialArea = 1;
            int pointCounter = 0;
            for (int i = startIndex; i < path.Points.Length; i++)
            {
                if (path.Points[i].LocationType == MediaPointLocationType.MediaBorder) continue;  //Ich verwende momentan kein Glas         

                string indexString = null;
                double differential = double.NaN;
                switch (path.Points[i].LocationType)
                {
                    case MediaPointLocationType.Surface:
                        var field1 = this.surfacePointToIndexConverter[path.Points[i].Point.SurfacePoint];
                        indexString = "S" + field1.Index;
                        differential = field1.DifferentialArea;
                        break;
                    case MediaPointLocationType.MediaParticle:                    
                        var field2 = this.volumePointToIndexConverter[path.Points[i].Position];
                        indexString = "V" + field2.Index;
                        differential = this.volumePointToIndexConverter.DifferentialVolume;
                        break;
                    case MediaPointLocationType.MediaBorder:
                        var field3 = this.surfacePointToIndexConverter[path.Points[i].Point.SurfacePoint]; //Es ist die 2D-Oberfläche von den Glas-Punkt gesucht
                        indexString = "B" + field3.Index;
                        differential = 1;
                        break;
                    default:
                        throw new Exception("Unknown enum" + path.Points[i].LocationType);
                }

                pathString.Append(indexString);
                pointCounter++;
                //pathDifferentialArea *= field.DifferentialArea;
                pathDifferentialArea *= differential;
                pathString.Append(";");
            }

            string pathCode = pathString.ToString();
            int pathLength = pointCounter;// path.Points.Length;
            if (this.pathHistogram.ContainsKey(pathCode) == false) this.pathHistogram.Add(pathCode, new PathData(path.GetPathSpaceString().Replace(" B",""), pathLength, pathDifferentialArea));
            this.pathHistogram[pathCode].PathPdfAs.Add(path.PathPdfA);
        }

        class PdfAData
        {
            public double HistogramPdfA;
            public double PropertyPdfA;
        }

        private PdfAData[] GetDataFromSinglePathCode(string pathCode)
        {
            return this.pathHistogram.Where(x => x.Value.PathCode == pathCode).
                //Where(x => x.Value.PointPdfAs.Count > 20).
                Select(x => new PdfAData()
                {
                    HistogramPdfA = x.Value.PathPdfAs.Count / (double)this.sampleCounter / x.Value.DifferentialArea,
                    PropertyPdfA = x.Value.PathPdfAs.Average()
                }).OrderBy(x => x.PropertyPdfA).ToArray();
        }

        private SimpleFunction GetPdfFunctionFromHistogramForSinglePathCode(string pathCode)
        {
            var histo = GetDataFromSinglePathCode(pathCode);

            return new SimpleFunction((x) =>
            {
                if (x < 0 || x > 1 || histo.Length == 0) return 0;

                int index = Math.Min((int)(x * (histo.Length)), histo.Length - 1);

                return histo[index].HistogramPdfA;
            });
        }

        private SimpleFunction GetPdfFunctionFromPdfPropertyForSinglePathCode(string pathCode)
        {
            var histo = GetDataFromSinglePathCode(pathCode);

            return new SimpleFunction((x) =>
            {
                if (x < 0 || x > 1 || histo.Length == 0) return 0;

                int index = Math.Min((int)(x * (histo.Length)), histo.Length - 1);

                return histo[index].PropertyPdfA;
            });
        }

        private Bitmap GetPlotterImageForSinglePathLength(int width, int height, string pathCode, out int error)
        {
            List<FunctionToPlot> functions = new List<FunctionToPlot>();
            functions.Add(new FunctionToPlot() { Function = GetPdfFunctionFromHistogramForSinglePathCode(pathCode), Color = Color.Blue, Text = "Histogram" });
            functions.Add(new FunctionToPlot() { Function = GetPdfFunctionFromPdfPropertyForSinglePathCode(pathCode), Color = Color.Red, Text = "PdfA" });
            FunctionPlotter plotter = new FunctionPlotter(new SizeF(1, (float)1.08028575E-09), new SizeF(1,0.4f), 0, 1, new Size(width, height));
            //FunctionPlotter plotter = new FunctionPlotter(new SizeF(1, 0.04f), 0, 1, new Size(width, height));
            error = (int)FunctionComparer.GetMaxDifferenceAreaBetweenFunctions(0, 1, 0.05f, GetPdfFunctionFromHistogramForSinglePathCode(pathCode), GetPdfFunctionFromPdfPropertyForSinglePathCode(pathCode));
            int sampleCountFromGivenPathLength = this.pathHistogram.Where(x => x.Value.PathCode == pathCode).Sum(x => x.Value.PathPdfAs.Count);
            string customText = "[" + pathCode + "] Error=" +
                error +
                " (" + sampleCountFromGivenPathLength + ")";
            return plotter.PlotFunctions(functions, customText);
        }

        public TestResult GetTestResult(int width, int height, int maxPathLength)
        {
            string[] pathCodes = this.pathHistogram.Where(x => x.Value.PathLength <= maxPathLength).GroupBy(x => x.Value.PathCode).Select(x => x.Key).OrderBy(x => x.Length).ToArray();

            //int[] errorValues = new int[pathLengths.Max() + 1];
            Dictionary<string, int> errorValues = new Dictionary<string, int>();

            List<Bitmap> images = new List<Bitmap>();
            int error = int.MaxValue;
            foreach (var pathCode in pathCodes)
            {
                Bitmap image = GetPlotterImageForSinglePathLength(width, height, pathCode, out error);
                images.Add(image);
                errorValues[pathCode] = error;
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
            public Dictionary<string, int> Error;

            public string Text=""; //Kann weg sobalt Compiler es zuläßt

            public int MaxErrorUptoGivenPathLength(int pathLength)
            {
                return this.Error.Where(x => x.Key.Split(' ').Count() <= pathLength).Max(x => x.Value);
            }
        }
    }
}
