namespace GraphicPanels
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
    }

    public enum Mode2D
    {
        OpenGL_Version_1_0 = 0,
        OpenGL_Version_3_0 = 1,
        Direct3D_11 = 2,
        CPU = 3,
    }
}
