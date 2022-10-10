using System.Linq;
using System.Xml.Serialization;

namespace GraphicMinimal
{
    public class GlobalObjectPropertys : ICommonGlobalDrawingProps, IRasterizerGlobalDrawingProps, IRaytracerGlobalDrawingProps
    {
        //OpenGL + Raytracer
        public Camera Camera { get; set; } = new Camera(new Vector3D(0, 0, 0), new Vector3D(0, 0, -1), 45);
        public string BackgroundImage { get; set; } = "#000000";
        public float BackgroundColorFactor { get; set; } = 1; //Braucht man dann, wenn Media(Luft) ein Teil der Eye-Pfade, die ins Unendliche fliegen wollen, an Partikeln hängen bleiben
        public float ExplosionRadius { get; set; } = 1.0f;
        public int Time { get; set; } = 0; //Aktuelle Zeit; Wird beim Explosionseffekt genutzt

        //OpenGL
        public RasterizerShadowMode ShadowsForRasterizer { get; set; } = RasterizerShadowMode.Stencil;
        public bool UseFrustumCulling { get; set; } = true; //Objekte außerhalb des Sichtbereiches nicht zeichnen (Ragt der Stencilschatten in den Sichbereich und das Objekt ist nicht zu sehen, dann fehlt auch der Schatten wenn FrustumCulling aktiv ist)

        //Raytracer
        public float DistanceDephtOfFieldPlane { get; set; } = 100.0f;
        public float WidthDephtOfField { get; set; } = 2.0f;
        public bool DepthOfFieldIsEnabled { get; set; } = false;
        public bool UseCosAtCamera { get; set; } = true;
        public PixelSamplingMode CameraSamplingMode {get;set;} = PixelSamplingMode.Tent;
        public string SaveFolder  { get; set; } = "";
        public RaytracerAutoSaveMode AutoSaveMode { get; set; } = RaytracerAutoSaveMode.Disabled;
        public int SamplingCount { get; set; } = 10;
        public int RecursionDepth { get; set; } = 10;
        public int ThreadCount { get; set; } = (new System.Management.ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get()).Cast<System.Management.ManagementBaseObject>().ToList().Sum(x => int.Parse(x["NumberOfLogicalProcessors"].ToString())) - 1;
        public int MaxRenderTimeInSeconds { get; set; } = int.MaxValue;
        public RaytracerRenderMode RaytracerRenderMode { get; set; } = RaytracerRenderMode.SmallBoxes;
        public TonemappingMethod Tonemapping { get; set; } = TonemappingMethod.None;
        public float BrightnessFactor { get; set; } = 1; //Mit diesen Faktor werden alle Farbwerte am Ende noch multipliziert (Ist quasi Tonemapping für Arme)
        public int PhotonCount { get; set; } = 60000;
        public float PhotonmapSearchRadiusFactor { get; set; } = 1; //Faktor für die Media-Photonmapping-Verfahren. Suchradius ist größe eines Pixelfootprints * PhotonmapSearchRadiusFactor
        public float BeamDataLineQueryReductionFactor { get; set; } = 0.1f; //Nur so viel Prozent aller LightSubpahts bekommt bei UPBP die Beam2Beam-Map gegenüber den anderen Photonmaps
        public float SearchRadiusForMediaBeamTracer { get; set; } = 0.005f; //Suchradius für die Beam2Beam-Abfrage beim MediaBeamTracer
        public PhotonmapDirectPixelSetting PhotonmapPixelSettings { get; set; } = PhotonmapDirectPixelSetting.ShowPixelPhotons;
        [XmlIgnore()] public IParticipatingMediaDescription GlobalParticipatingMedia { get; set; } = null; //[XmlIgnore()] Wenn ich diese Property mal serialisieren will siehe hier: https://stackoverflow.com/questions/1333864/xml-serialization-of-interface-property
        public RadiositySettings RadiositySettings { get; set; } = new RadiositySettings();
        public int LightPickStepSize { get; set; } = 0; //0=LightPickProp laut Emission; 4 = Wenn man kleine schwache Lampe nah an Kamera hat(Säulenbüro); 2 = Wenn ich das bei der Stilllife-Scene einstelle, dann verwende ich die gleiche LightPickProp, wie es SmallUPBP macht; 
    }

    public class RadiositySettings
    {
        public RadiosityColorMode RadiosityColorMode = RadiosityColorMode.WithColorInterpolation;
        public float MaxAreaPerPatch = 0.01f;//0.005f; //Bei 0.003f; bekomme ich eine OutOfMemmory-Exception in RGB
        public int HemicubeResolution = 30;
        public int IlluminationStepCount = 10; //2 = Nur Direktes Licht (1 wäre nur Lichtquelle sichbar)
        public bool GenerateQuads = true;
        public int SampleCountForPatchDividerShadowTest = 40;
        public bool UseShadowRaysForVisibleTest = true; //Bei der einfachen Radiostiy-Box-Testscene kann man sich die Sichtbarkeitsprüfung sparen
        public string VisibleMatrixFileName = null; //Wenn hier ein Dateiname steht, dann wird hier die Matrix geladen wenn vorhanden und auch gespeichert, wenn noch nicht vorhanden
    }

    public interface ICommonGlobalDrawingProps
    {
        Camera Camera { get; set; }
        string BackgroundImage { get; set; }
        float BackgroundColorFactor { get; set; }
        float ExplosionRadius { get; set; }
        int Time { get; set; }
    }

    public interface IRasterizerGlobalDrawingProps : ICommonGlobalDrawingProps
    {
        RasterizerShadowMode ShadowsForRasterizer { get; set; }
        bool UseFrustumCulling { get; set; }
    }

    public interface IRaytracerGlobalDrawingProps : ICommonGlobalDrawingProps
    {
        float DistanceDephtOfFieldPlane { get; set; }
        float WidthDephtOfField { get; set; }
        bool DepthOfFieldIsEnabled { get; set; }
        bool UseCosAtCamera { get; set; }
        string SaveFolder { get; set; }
        RaytracerAutoSaveMode AutoSaveMode { get; set; }
        int SamplingCount { get; set; }
        int RecursionDepth { get; set; }
        int ThreadCount { get; set; }
        int MaxRenderTimeInSeconds { get; set; }
        RaytracerRenderMode RaytracerRenderMode { get; set; }
        TonemappingMethod Tonemapping { get; set; }
        float BrightnessFactor { get; set; }
        int PhotonCount { get; set; }
        float BeamDataLineQueryReductionFactor { get; set; }
        float SearchRadiusForMediaBeamTracer { get; set; }
        IParticipatingMediaDescription GlobalParticipatingMedia { get; set; }
        RadiositySettings RadiositySettings { get; set; }
    }
}
