using System.Collections.Generic;
using SubpathGenerator;
using GraphicMinimal;
using GraphicGlobal;

namespace FullPathGenerator
{
    public class VertexConnection : IFullPathSamplingMethod
    {
        private readonly PointToPointConnector pointToPointConnector;
        private readonly bool checkThatEachPointIsASurfacePoint;
        private readonly int maxPathLength;

        public VertexConnection(int maxPathLength, PointToPointConnector pointToPointConnector, PathSamplingType usedSubPathSamplingType)
        {
            this.checkThatEachPointIsASurfacePoint = usedSubPathSamplingType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling;
            this.maxPathLength = maxPathLength;
            this.pointToPointConnector = pointToPointConnector;
        }
        public SamplingMethod Name => SamplingMethod.VertexConnection;

        public int SampleCountForGivenPath(FullPath path)
        {
            int counter = 0;
            for (int i = 1; i < path.Points.Length - 2; i++) if (path.Points[i].IsDiffusePoint && path.Points[i + 1].IsDiffusePoint) counter++;

            return counter;
        }

        public List<FullPath> SampleFullPaths(SubPath eyePath, SubPath lightPath, FullPathFrameData frameData, IRandom rand)
        {
            List<FullPath> paths = new List<FullPath>();
            if (eyePath == null || lightPath == null) return paths;
            for (int i = 1; i < eyePath.Points.Length; i++)
            {
                paths.AddRange(TryToCreatePaths(eyePath.Points[i], lightPath));
            }
            return paths;
        }

        private List<FullPath> TryToCreatePaths(PathPoint eyePoint, SubPath lightPath)
        {
            List<FullPath> paths = new List<FullPath>();

            if (eyePoint.IsLocatedOnLightSource == false && eyePoint.IsDiffusePoint)
            {
                for (int j = 1; j < lightPath.Points.Length; j++) //Index 0 ist der Punkt auf der Lichtquelle. Mit diesen wird nicht direkt verbunden, da DirectLighting besser die Variance (Abschätzfehler) reduziert
                {
                    var lightPoint = lightPath.Points[j];
                    if (lightPoint.IsDiffusePoint && eyePoint.Index + j + 2 <= this.maxPathLength)
                    {
                        var connectData = this.pointToPointConnector.TryToConnectTwoPoints(eyePoint, lightPoint);
                        if (connectData != null)
                        {
                            var path = CreatePath(eyePoint, lightPoint, connectData);
                            paths.Add(path);
                        }
                    }                       
                }
            }

            return paths;
        }

        protected virtual FullPath CreatePath(PathPoint eyePoint, PathPoint lightPoint, EyePoint2LightPointConnectionData connectData)
        {
            SubPath eyePath = eyePoint.AssociatedPath;
            SubPath lightPath = lightPoint.AssociatedPath;

            double pathPdfA = eyePoint.PdfA * lightPoint.PdfA;
            Vector3D pathContribution = Vector3D.Mult(Vector3D.Mult(eyePoint.PathWeight, lightPoint.PathWeight), Vector3D.Mult(connectData.EyeBrdf.Brdf, connectData.LightBrdf.Brdf)) * connectData.GeometryTerm;
            pathContribution = Vector3D.Mult(pathContribution, connectData.AttenuationTerm);

            var points = new FullPathPoint[eyePoint.Index + lightPoint.Index + 2];

            //Ich habe 9 Wochen gebraucht, um den Fehler in den nächsten 2 Zeilen zu finden. Der RandMock-Ansatz hat mir dann geholfen.
            //float eyePredecessorPdfAReverse = eyePoint == eyePath.Points.Last() ? PdfMisHelper.PdfWToPdfAOrV(connectData.EyeBrdf.PdfWReverse, eyePoint, eyePoint.Predecessor) : (float)eyePoint.Predecessor.PdfAReverse; //So ist die Kerze bei Stillife zu dunkel da hier mit der falschen PdfWReverse gearbeitet wird
            //float lightPredecessorPdfAReverse = lightPoint == lightPath.Points.Last() ? PdfMisHelper.PdfWToPdfAOrV(connectData.LightBrdf.PdfWReverse, lightPoint, lightPoint.Predecessor) : (float)lightPoint.Predecessor.PdfAReverse;
            float eyePredecessorPdfAReverse = PdfHelper.PdfWToPdfAOrV(connectData.EyeBrdf.PdfWReverse, eyePoint, eyePoint.Predecessor);
            float lightPredecessorPdfAReverse = PdfHelper.PdfWToPdfAOrV(connectData.LightBrdf.PdfWReverse, lightPoint, lightPoint.Predecessor);

            points[eyePoint.Index - 1] = new FullPathPoint(eyePoint.Predecessor, eyePoint.Predecessor.LineToNextPoint, null, eyePoint.Predecessor.BrdfSampleEventOnThisPoint.PdfW, eyePoint.Predecessor.BrdfSampleEventOnThisPoint.PdfWReverse, BrdfCreator.BrdfSampling) { EyePdfA = eyePoint.Predecessor.PdfA };
            points[eyePoint.Index] = new FullPathPoint(eyePoint, connectData.LineFromEyeToLightPoint, null, connectData.EyeBrdf.PdfW, connectData.EyeBrdf.PdfWReverse, BrdfCreator.BrdfEvaluation) { EyePdfA = eyePoint.PdfA };
            points[eyePoint.Index + 1] = new FullPathPoint(lightPoint, null, null, connectData.LightBrdf.PdfWReverse, connectData.LightBrdf.PdfW, BrdfCreator.BrdfEvaluation) { LightPdfA = lightPoint.PdfA, EyePdfA = eyePoint.PdfA * PdfHelper.PdfWToPdfAOrV(connectData.EyeBrdf.PdfW, eyePoint, lightPoint) * connectData.PdfLForEyeToLightPoint.PdfL };
            points[eyePoint.Index + 2] = new FullPathPoint(lightPoint.Predecessor, null, lightPoint.Predecessor.LineToNextPoint, lightPoint.Predecessor.BrdfSampleEventOnThisPoint.PdfWReverse, lightPoint.Predecessor.BrdfSampleEventOnThisPoint.PdfW, BrdfCreator.BrdfSampling) { LightPdfA = lightPoint.Predecessor.PdfA, EyePdfA = points[eyePoint.Index + 1].EyePdfA * lightPredecessorPdfAReverse * lightPoint.Predecessor.PdfLFromNextPointToThis };
            points[eyePoint.Index].LightPdfA = lightPoint.PdfA * PdfHelper.PdfWToPdfAOrV(connectData.LightBrdf.PdfW, lightPoint, eyePoint) * connectData.PdfLForEyeToLightPoint.ReversePdfL;
            points[eyePoint.Index - 1].LightPdfA = points[eyePoint.Index].LightPdfA * eyePredecessorPdfAReverse * eyePoint.Predecessor.PdfLFromNextPointToThis;

            double lightPdfA = points[eyePoint.Index - 1].LightPdfA;
            for (int i = eyePoint.Index - 2; i >= 0; i--)
            {
                lightPdfA = lightPdfA * eyePath.Points[i].PdfAReverse * eyePath.Points[i].PdfLFromNextPointToThis;
                points[i] = new FullPathPoint(eyePath.Points[i], eyePath.Points[i].LineToNextPoint, null, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfW, eyePath.Points[i].BrdfSampleEventOnThisPoint.PdfWReverse, BrdfCreator.BrdfSampling) { EyePdfA = eyePath.Points[i].PdfA, LightPdfA = lightPdfA };              
            }

            double eyePdfA = points[eyePoint.Index + 2].EyePdfA;
            for (int i = eyePoint.Index + 3, j = lightPoint.Index - 2; i < points.Length; i++, j--)
            {
                eyePdfA = eyePdfA * lightPath.Points[j].PdfAReverse * lightPath.Points[j].PdfLFromNextPointToThis;
                points[i] = new FullPathPoint(lightPath.Points[j], null, lightPath.Points[j].LineToNextPoint, lightPath.Points[j].BrdfSampleEventOnThisPoint.PdfWReverse, lightPath.Points[j].BrdfSampleEventOnThisPoint.PdfW, BrdfCreator.BrdfSampling) { EyePdfA = eyePdfA, LightPdfA = lightPath.Points[j].PdfA };
            }

            return new FullPath(pathContribution, pathPdfA, points, this);
        }

        public double GetPathPdfAForAGivenPath(FullPath path, FullPathFrameData frameData)
        {
            if (this.checkThatEachPointIsASurfacePoint && path.IsSurfacePathOnly() == false) return 0;
            double sum = 0;

            for (int i = 1; i < path.Points.Length - 2; i++) 
            {
                //Conencte immer i mit i+1
                if (path.Points[i].IsDiffusePoint && path.Points[i + 1].IsDiffusePoint)
                {
                    sum += path.Points[i].EyePdfA * path.Points[i + 1].LightPdfA;
                }
            }
            return sum;
        }
    }
}
