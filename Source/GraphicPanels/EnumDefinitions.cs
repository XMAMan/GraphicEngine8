﻿namespace GraphicPanels
{
    public enum Mode3D
    {
        OpenGL_Version_1_0,
        OpenGL_Version_1_0_OldShaders,
        OpenGL_Version_3_0,
        Direct3D_11,
        CPU,
        DepthOfField, //Zur Ausgabe des DepthOfField-Grids
        Raytracer,     //Raytracer, der als Beleuchtungsformel das gleiche Verfahren wie CPU,Direct3D_11, OpenGL_Version_3_0 verwendet
        RaytracerWithPointLights,
        PathTracer,
        BidirectionalPathTracing,
        FullBidirectionalPathTracing,
        Photonmapping,
        Photonmap,
        PhotonmapPixel,
        ProgressivePhotonmapping,
        VertexConnectionMerging,
        MediaVCM,
        RadiositySolidAngle,
        RadiosityHemicube,
        MediaPathTracer,                    //Mit Distanzsampling: Pathtracing
        MediaBidirectionalPathTracing,      //Mit Distanzsampling: Pathtracing, DirectLighting, VertexConnection 
        MediaFullBidirectionalPathTracing,  //Mit Distanzsampling: Pathtracing, DirectLighting, VertexConnection, Lighttracing
        MediaEdgeSampler,                   //Ohne Distanzsampling; Pathtracing, DirectLighting, VertexConnection, Lighttracing; DirectLightingOnEdge, LightTracingOnEdge
        UPBP,                               //Mit Distanzsampling: Pathtracing, DirectLighting, VertexConnection, Lighttracing, SurfaceVertexMerging
        MediaBeamTracer,                    //Single-Scattering mit Beam2Beam
        ThinMediaSingleScattering,          //Ohne Distanzsampling beim SubPatherzeugen; Mit Distanzsampling beim erzeugen eines Segmentpunktes: DirectLighting, DirectLightingOnEdge
        ThinMediaSingleScatteringBiased,    //Ohne Distanzsampling beim SubPatherzeugen; Ohne Distanzsampling beim Segmentpunkterstellen: DirectLighting, DirectLightingOnEdge
        ThinMediaMultipleScattering,        //Mit Distanzsampling: DirectLighting, DirectLightingOnEdge
        MMLT,                               //Multiplexed Metropolis Light Transport (Ohne Media)
        MMLT_WithMedia,                     //Multiplexed Metropolis Light Transport (Mit Media)
        SinglePathBPT,                      //BPT wo pro Sampleschritt nur ein einzelner Fullpfad erzeugt wird, der durch ein zufälliges Pixel geht
        SinglePathBPT_WithMedia,
        McVcm,                              //Markov Chain VCM (Ohne Media)
        McVcm_WithMedia,                    //Markov Chain VCM (Mit Media)
    }

    public enum Mode2D
    {
        OpenGL_Version_1_0 = 0,
        OpenGL_Version_3_0 = 1,
        Direct3D_11 = 2,
        CPU = 3,
    }
}
