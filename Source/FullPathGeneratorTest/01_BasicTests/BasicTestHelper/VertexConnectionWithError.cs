using FullPathGenerator;
using GraphicMinimal;
using RaytracingLightSource;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullPathGeneratorTest._01_BasicTests.BasicTestHelper
{
    //Hiermit teste ich den PdfATester, da PdfA-Fehler extrem schwer zu testen sind
    class VertexConnectionWithError : VertexConnection
    {
        public VertexConnectionWithError(int maxPathLength, PointToPointConnector pointToPointConnector, PathSamplingType usedSubPathSamplingType)
        :base(maxPathLength, pointToPointConnector, usedSubPathSamplingType)
        {
        }

        protected override FullPath CreatePath(PathPoint eyePoint, PathPoint lightPoint, EyePoint2LightPointConnectionData connectData)
        {
            SubPath eyePath = eyePoint.AssociatedPath as SubPath;
            SubPath lightPath = lightPoint.AssociatedPath as SubPath;

            double pathPdfA = eyePoint.PdfA * lightPoint.PdfA;
            Vector3D pathContribution = Vector3D.Mult(Vector3D.Mult(eyePoint.PathWeight, lightPoint.PathWeight), Vector3D.Mult(connectData.EyeBrdf.Brdf, connectData.LightBrdf.Brdf)) * connectData.GeometryTerm;
            pathContribution = Vector3D.Mult(pathContribution, connectData.AttenuationTerm);

            var points = new FullPathPoint[eyePoint.Index + lightPoint.Index + 2];

            //Ich habe 9 Wochen gebraucht, um den Fehler in den nächsten 2 Zeilen zu finden. Der RandMock-Ansatz hat mir dann geholfen.
            float eyePredecessorPdfAReverse = eyePoint == eyePath.Points.Last() ? PdfHelper.PdfWToPdfAOrV(connectData.EyeBrdf.PdfWReverse, eyePoint, eyePoint.Predecessor) : (float)eyePoint.Predecessor.PdfAReverse;
            float lightPredecessorPdfAReverse = lightPoint == lightPath.Points.Last() ? PdfHelper.PdfWToPdfAOrV(connectData.LightBrdf.PdfWReverse, lightPoint, lightPoint.Predecessor) : (float)lightPoint.Predecessor.PdfAReverse;
            //float eyePredecessorPdfAReverse = PdfMisHelper.PdfWToPdfAOrV(connectData.EyeBrdf.PdfWReverse, eyePoint, eyePoint.Predecessor);
            //float lightPredecessorPdfAReverse = PdfMisHelper.PdfWToPdfAOrV(connectData.LightBrdf.PdfWReverse, lightPoint, lightPoint.Predecessor);

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
    }
}
