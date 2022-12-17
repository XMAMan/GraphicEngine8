using System;
using System.Collections.Generic;
using RaytracingMethods;
using GraphicGlobal;
using RaytracingMethods.MMLT;

namespace GraphicPanels
{
    class DrawingPanelFactory
    {
        private readonly GraphicPipelineCache pipelineCache = new GraphicPipelineCache();

        public IDrawingPanel CreateDrawingPanel(Mode3D modus)
        {
            switch (modus)
            {
                case Mode3D.CPU:
                    return new Rasterizer.Rasterizer(pipelineCache.GetEntry(Mode3D.CPU));
                case Mode3D.OpenGL_Version_1_0:
                    return new Rasterizer.Rasterizer(pipelineCache.GetEntry(Mode3D.OpenGL_Version_1_0));
                case Mode3D.OpenGL_Version_1_0_OldShaders:
                    return new Rasterizer.Rasterizer(pipelineCache.GetEntry(Mode3D.OpenGL_Version_1_0_OldShaders));
                case Mode3D.Direct3D_11:
                    return new Rasterizer.Rasterizer(pipelineCache.GetEntry(Mode3D.Direct3D_11));
                case Mode3D.OpenGL_Version_3_0:
                    return new Rasterizer.Rasterizer(pipelineCache.GetEntry(Mode3D.OpenGL_Version_3_0));
                case Mode3D.DepthOfField:
                    return new RaytracerMain.DepthOfFieldDrawer(pipelineCache.GetEntry(Mode3D.OpenGL_Version_1_0));
                case Mode3D.Raytracer:
                    return new RaytracerMain.RaytracerMain(new RaytracerSimple());
                case Mode3D.RaytracerWithPointLights:
                    return new RaytracerMain.RaytracerMain(new RaytracerWithPointLights());
                case Mode3D.PathTracer:
                    return new RaytracerMain.RaytracerMain(new PathTracer());
                case Mode3D.BidirectionalPathTracing:
                    return new RaytracerMain.RaytracerMain(new BidirectionalPathTracing());
                case Mode3D.FullBidirectionalPathTracing:
                    return new RaytracerMain.RaytracerMain(new FullBidirectionalPathTracing());
                case Mode3D.Photonmapping:
                    return new RaytracerMain.RaytracerMain(new Photonmapping());
                case Mode3D.Photonmap:
                    return new RaytracerMain.RaytracerMain(new PhotonmapDirect());
                case Mode3D.PhotonmapPixel:
                    return new RaytracerMain.RaytracerMain(new PhotonmapDirectPixel());
                case Mode3D.ProgressivePhotonmapping:
                    return new RaytracerMain.RaytracerMain(new ProgressivePhotonmapping());
                case Mode3D.VertexConnectionMerging:
                    return new RaytracerMain.RaytracerMain(new VCM(false));
                case Mode3D.MediaVCM:
                    return new RaytracerMain.RaytracerMain(new VCM(true));
                case Mode3D.RadiositySolidAngle:
                    return new RaytracerMain.RaytracerMain(new Radiosity.Radiosity(Radiosity.Radiosity.Mode.SolidAngle));
                case Mode3D.RadiosityHemicube:
                    return new RaytracerMain.RaytracerMain(new Radiosity.Radiosity(Radiosity.Radiosity.Mode.Hemicube));
                case Mode3D.MediaBidirectionalPathTracing:
                    return new RaytracerMain.RaytracerMain(new MediaBidirectionalPathTracing());
                case Mode3D.MediaPathTracer:
                    return new RaytracerMain.RaytracerMain(new MediaPathTracer());
                case Mode3D.MediaFullBidirectionalPathTracing:
                    return new RaytracerMain.RaytracerMain(new MediaFullBidirectionalPathTracing());
                case Mode3D.MediaEdgeSampler:
                    return new RaytracerMain.RaytracerMain(new MediaEdgeSampler());
                case Mode3D.UPBP:
                    return new RaytracerMain.RaytracerMain(new UPBP());
                case Mode3D.MediaBeamTracer:
                    return new RaytracerMain.RaytracerMain(new MediaBeamTracer());
                case Mode3D.ThinMediaSingleScattering:
                    return new RaytracerMain.RaytracerMain(new ThinMediaTracer(true, true));
                case Mode3D.ThinMediaSingleScatteringBiased:
                    return new RaytracerMain.RaytracerMain(new ThinMediaTracer(false, true));
                case Mode3D.ThinMediaMultipleScattering:
                    return new RaytracerMain.RaytracerMain(new ThinMediaTracer(true, false));
                case Mode3D.SinglePathBPT:
                    return new RaytracerMain.RaytracerMain(new SingleFullPathBPT(false));
                case Mode3D.SinglePathBPT_WithMedia:
                    return new RaytracerMain.RaytracerMain(new SingleFullPathBPT(true));
                case Mode3D.MMLT:
                    return new RaytracerMain.RaytracerMain(new MultiplexedMetropolisLightTransport(false));                    
                case Mode3D.MMLT_WithMedia:
                    return new RaytracerMain.RaytracerMain(new MultiplexedMetropolisLightTransport(true));
            }

            throw new Exception("Unknown modus:" + modus.ToString());
        }
    }

    class GraphicPipelineCache
    {
        private readonly Dictionary<Mode3D, IGraphicPipeline> cache = new Dictionary<Mode3D, IGraphicPipeline>();
        public IGraphicPipeline GetEntry(Mode3D mode)
        {
            if (this.cache.ContainsKey(mode) == false) this.cache.Add(mode, CreateGraphicPipeline(mode));
            return this.cache[mode];
        }

        private IGraphicPipeline CreateGraphicPipeline(Mode3D mode)
        {
            switch (mode)
            {
                case Mode3D.CPU: return new GraphicPipelineCPU.GraphicPipelineCPU();
                case Mode3D.OpenGL_Version_1_0: return new GraphicPipelineOpenGLv1_0.GraphicPipelineOpenGLv1_0(false);
                case Mode3D.OpenGL_Version_1_0_OldShaders: return new GraphicPipelineOpenGLv1_0.GraphicPipelineOpenGLv1_0(true);
                case Mode3D.Direct3D_11: return new GraphicPipelineDirect3D11.GraphicPipelineDirect3D11();
                case Mode3D.OpenGL_Version_3_0: return new GraphicPipelineOpenGLv3_0.GraphicPipelineOpenGLv3_0();
            }
            throw new Exception("Unknown Mode: " + mode);
        }
    }
}
