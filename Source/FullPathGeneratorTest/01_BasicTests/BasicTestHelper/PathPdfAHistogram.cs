using FullPathGenerator;
using GraphicGlobal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfHistogram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullPathGeneratorTest.BasicTests.BasicTestHelper
{
    static class PathPdfAHistogram
    {
        private static readonly double maxFunctionPdfAError = 0.001;

        public static FullPathHistogram.TestResult ComparePathPdfAWithFunctionPdfAAndHistogram(Sampler sampler, BoxTestScene testSzene, int sampleCount)
        {
            int histogramSize = 7; //Anzahl der Gitterzellen vom 3D-Grid pro Dimmension

            FullPathHistogram histogram = new FullPathHistogram(testSzene.Quads, testSzene.MediaBox, histogramSize, sampleCount);

            var frameData = PathContributionCalculator.CreateFrameDataWithPhotonmap(testSzene.LightPathSampler, testSzene.PhotonenCount, testSzene.rand, testSzene.SizeFactor, sampler.PhotonSettings);

            string randString = "AAEAAAD/////AQAAAAAAAAAEAQAAAA1TeXN0ZW0uUmFuZG9tAwAAAAVpbmV4dAZpbmV4dHAJU2VlZEFycmF5AAAHCAgIDAAAACEAAAAJAgAAAA8CAAAAOAAAAAgAAAAA+uhZJXDZ1UvgyYphR+LTUVOLGxis2553EtNUCzuj6BeK161+HJHgVLpL2hZN61MmtBAYMa9ciHKPre5GZ7IuWa9x1mOSg01CTb2ba+uKGCz3t41j98ygawuf+n5jIwNFPypKIXqC2GRYUskyNmnSB4TDLT6extg8mTFWDSm522sa9b0UaOFgHBBzH37vQ3ZodO9uLzhIkWCct/VpBmh6ep2LKDKDgxlCXN69J9q02VSbW2EJK+b4QRTYjVzJ6BklmFZ/ASbpfW4LgcYt6QEZbxFTw1IUB4cc6eRSegs=";
            testSzene.rand = new Rand(randString);

            for (int i = 0; i < sampleCount; i++)
            {
                string randomObjectBase64Coded = testSzene.rand.ToBase64String();

                var eyePath = sampler.CreateEyePath ? testSzene.EyePathSampler.SamplePathFromCamera(testSzene.PixX, testSzene.PixY, testSzene.rand) : null;
                var lightPath = sampler.CreateLightPath ? testSzene.LightPathSampler.SamplePathFromLighsource(testSzene.rand) : null;
                //float kernelFunction = frameData != null ? frameData.PhotonMaps.GlobalSurfacePhotonmap.KernelFunction(0, frameData.PhotonMaps.GlobalSurfacePhotonmap.SearchRadius) : 1;
                List<FullPath> paths = sampler.SamplerMethod.SampleFullPaths(eyePath, lightPath, frameData, testSzene.rand);
                foreach (var path in paths)
                {
                    if (sampler.SamplerMethod.SampleCountForGivenPath(path) == 1)
                    {
                        double functionPdfA = sampler.SamplerMethod.GetPathPdfAForAGivenPath(path, frameData);// * sampler.SamplerMethod.SampleCountForGivenPathlength(path.Points.Length);
                        Assert.IsTrue(Math.Abs(functionPdfA - path.PathPdfA) < maxFunctionPdfAError, "FunctionPdfA=" + functionPdfA + " PathPdfA=" + path.PathPdfA);
                    }                    

                    histogram.AddPathToHistogram(path, 1);
                }
            }

            int maxPathLengthForVisualisation = 4;
            return histogram.GetTestResult(400, 300, maxPathLengthForVisualisation);
        }
    }
}
