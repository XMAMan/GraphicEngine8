using RaytracingColorEstimator;
using FullPathGenerator;
using GraphicGlobal;
using SubpathGenerator;

namespace RaytracingMethods
{
    //Idee hinter Photonmapping: das Path-Integral wird in 4 bereiche unterteilt, welche disjunkt sind und somit ein MIS-Faktor von 1 ermöglichen
    //Bereich 1: Erster Eye-Point Caustic-Map
    //Bereich 2: Erster Eye-Point - MultipleDirectLighting
    //Bereich 3: Specular-Pfad E-S*-D (Alle S-Punkte)
    //Bereich 4: GlobalPhotonmap ab 2. Eye-Point
    public class Photonmapping : IPixelEstimator
    {
        private PixelRadianceCalculator pixelRadianceCalculator;

        public bool CreatesLigthPaths { get; } = false;

        public void BuildUp(RaytracingFrame3DData data)
        {
            this.pixelRadianceCalculator = PixelRadianceCreationHelper.CreatePixelRadianceCalculator(data,
                new SubPathSettings()
                {
                    EyePathType = PathSamplingType.NoMedia,
                    LightPathType = PathSamplingType.NoMedia
                },
                new FullPathSettings()
                {
                    //UsePathTracing = true, 
                    UseSpecularPathTracing = true,
                    //UseDirectLighting = true,
                    UseMultipleDirectLighting = true,
                    UseVertexMerging = true,
                    MaximumDirectLightingEyeIndex = 1,
                    //WithoutMis = true, 
                });

            this.pixelRadianceCalculator.FrameData.PhotonMaps = this.pixelRadianceCalculator.CreateSurfaceAndCausticMapWithMultipleThreads();
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            return this.pixelRadianceCalculator.SampleSingleEyePath(x, y, rand);
        }
    }
}
