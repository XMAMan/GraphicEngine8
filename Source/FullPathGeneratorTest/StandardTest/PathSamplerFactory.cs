using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FullPathGenerator;
using FullPathGeneratorTest._01_BasicTests.BasicTestHelper;

namespace FullPathGeneratorTest
{
    class Sampler
    {
        public IFullPathSamplingMethod SamplerMethod;
        public bool CreateEyePath = false;
        public bool CreateLightPath = false;
        public PhotonmapSettings PhotonSettings = null;
    }

    class PhotonmapSettings
    {
        public bool CreateSurfaceMap = false;
        public bool CreatePointDataPointQueryMap = false;
        public bool CreatePointDataBeamQueryMap = false;
        public bool CreateBeamDataLineQueryMap = false;
        public float BeamDataLineQueryReductionFactor = float.NaN;//Wenn hier 0.1 steht dann heißt das, dass 10% aller ausgesendeten LightSubpahts für die BeamDataLineQuery-Map verwendet werden
    }

    enum SamplerEnum { Pathtracing, Lighttracing, DirectLighting, MultipeDirectLighting, VertexConnection, VertexConnectionWithError, VertexMerging, DirectLightingOnEdgeWithSegmentSampling, DirectLightingOnEdgeWithoutSegmentSampling, LightTracingOnEdge, PointDataPointQuery, PointDataBeamQuery, BeamDataLineQuery }

    class PathSamplerFactory
    {
        private readonly BoxTestScene testSzene;

        public PathSamplerFactory(BoxTestScene testSzene)
        {
            this.testSzene = testSzene;
        }

        public Sampler Create(SamplerEnum samplerEnum)
        {
            switch(samplerEnum)
            {
                case SamplerEnum.Pathtracing:
                    return new Sampler()
                    {
                        SamplerMethod = new PathTracing(testSzene.LightSourceSampler, testSzene.EyePathSamplingType),
                        CreateEyePath = true,
                    };
                case SamplerEnum.Lighttracing:
                    return new Sampler()
                    {
                        SamplerMethod = new LightTracing(testSzene.Camera, testSzene.PointToPointConnector, testSzene.EyePathSamplingType),
                        CreateLightPath = true
                    };                
                case SamplerEnum.DirectLighting:
                    return new Sampler()
                    {
                        SamplerMethod = new DirectLighting(testSzene.LightSourceSampler, testSzene.MaxPathLength - 2, testSzene.PointToPointConnector, testSzene.EyePathSamplingType),
                        CreateEyePath = true,
                    };
                case SamplerEnum.MultipeDirectLighting:
                    return new Sampler()
                    {
                        SamplerMethod = new MultipleDirectLighting(testSzene.LightSourceSampler, testSzene.MaxPathLength - 2, testSzene.PointToPointConnector, testSzene.EyePathSamplingType),
                        CreateEyePath = true,
                    };
                case SamplerEnum.VertexConnection:
                    return new Sampler()
                    {
                        SamplerMethod = new VertexConnection(testSzene.MaxPathLength, testSzene.PointToPointConnector, testSzene.EyePathSamplingType),
                        CreateEyePath = true,
                        CreateLightPath = true
                    };
                case SamplerEnum.VertexConnectionWithError:
                    return new Sampler()
                    {
                        SamplerMethod = new VertexConnectionWithError(testSzene.MaxPathLength, testSzene.PointToPointConnector, testSzene.EyePathSamplingType),
                        CreateEyePath = true,
                        CreateLightPath = true
                    };
                case SamplerEnum.VertexMerging:
                    return new Sampler()
                    {
                        SamplerMethod = new VertexMerging(testSzene.MaxPathLength, testSzene.EyePathSamplingType),
                        CreateEyePath = true,
                        PhotonSettings = new PhotonmapSettings() { CreateSurfaceMap = true}
                    };
                case SamplerEnum.DirectLightingOnEdgeWithSegmentSampling:
                    return new Sampler()
                    {
                        SamplerMethod = new DirectLightingOnEdge(testSzene.LightSourceSampler, testSzene.PointToPointConnector, testSzene.EyePathSamplingType, testSzene.MaxPathLength, true),
                        CreateEyePath = true,
                    };
                case SamplerEnum.DirectLightingOnEdgeWithoutSegmentSampling:
                    return new Sampler()
                    {
                        SamplerMethod = new DirectLightingOnEdge(testSzene.LightSourceSampler, testSzene.PointToPointConnector, testSzene.EyePathSamplingType, 3, false),
                        CreateEyePath = true,
                    };
                case SamplerEnum.LightTracingOnEdge:
                    return new Sampler()
                    {
                        SamplerMethod = new LightTracingOnEdge(testSzene.Camera, testSzene.PointToPointConnector, testSzene.EyePathSamplingType),
                        CreateLightPath = true
                    };
                case SamplerEnum.PointDataPointQuery:
                    return new Sampler()
                    {
                        SamplerMethod = new PointDataPointQuery(testSzene.MaxPathLength),
                        CreateEyePath = true,
                        PhotonSettings = new PhotonmapSettings() { CreatePointDataPointQueryMap = true }
                    };
                case SamplerEnum.PointDataBeamQuery:
                    return new Sampler()
                    {
                        SamplerMethod = new PointDataBeamQuery(testSzene.EyePathSamplingType, testSzene.MaxPathLength),
                        CreateEyePath = true,
                        PhotonSettings = new PhotonmapSettings() { CreatePointDataBeamQueryMap = true }
                    };
                case SamplerEnum.BeamDataLineQuery:
                    return new Sampler()
                    {
                        SamplerMethod = new BeamDataLineQuery(testSzene.EyePathSamplingType, testSzene.MaxPathLength),
                        CreateEyePath = true,
                        PhotonSettings = new PhotonmapSettings() { CreateBeamDataLineQueryMap = true, BeamDataLineQueryReductionFactor = 1 }
                    };                    

            }
            throw new Exception("Not Implemented");
        }
    }
}
