namespace GraphicMinimal
{
    public interface ILightSourceDescription
    {
    }

    public interface IRaytracingLightSource : ILightSourceDescription
    {
        float Emission { get; set; }                              //Radiant flux -> Wie viel Watt hat die Lichtquelle? (Die Radiance berechnet sich dann aus Emission / Oberflächeninhalt)
        bool IsInfinityAway { get; }
        IRaytracingLightSource Clone();
    }

    public interface IRaytracingImportanceLight : IRaytracingLightSource
    {        
        LightImportanceSamplingMode ImportanceSamplingMode { get; set; } // In die Richtung, wo Objekte sehr nah sind, werden weniger Photonen gesendet, als in Objekte, die weit weg sind
    }

    public interface IRaytracingSphereWithSpotLight : IRaytracingLightSource
    {
        float SpotCutoff { get; set; }                            // Der Öffnungswinkel muss zwischen 0 und 90 liegen. Wenn 180, dann ist Richtungslicht deaktiviert
        Vector3D SpotDirection { get; set; }    // Richtung in Weltkoordinaten
    }

    //Das entspricht ein Punktlicht
    public class RasterizerLightSourceDescription : ILightSourceDescription
    {        
        public float SpotCutoff { get; set; } = 180;        // Der Öffnungswinkel muss zwischen 0 und 90 liegen. Wenn 180, dann ist Richtungslicht deaktiviert (Punktlicht in alle Richtungen)
        public Vector3D SpotDirection { get; set; } = new Vector3D(0, -1, -0.3f);    // Richtung in Weltkoordinaten
        public float ConstantAttenuation { get; set; } = 1.0f;
        public float LinearAttenuation { get; set; } = 0.00002f;
        public float QuadraticAttenuation { get; set; } = 0.00004f;
        public float SpotExponent { get; set; } = 20.0f;                         // Um so größer, um so dunkler wird der beleuchtete Bereich, aber der Schnitt zwischen Schatten und Licht wird weicher
        public bool CreateShadows { get; set; } = true;     //Erzeugt diese Lichtquelle Schatten?

        public RasterizerLightSourceDescription() { }

        public RasterizerLightSourceDescription(RasterizerLightSourceDescription copy)
        {
            this.SpotCutoff = copy.SpotCutoff;
            this.SpotDirection = copy.SpotDirection;
            this.ConstantAttenuation = copy.ConstantAttenuation;
            this.LinearAttenuation = copy.LinearAttenuation;
            this.QuadraticAttenuation = copy.QuadraticAttenuation;
            this.SpotExponent = copy.SpotExponent;
            this.CreateShadows = copy.CreateShadows;
        }

        public RasterizerLightSourceDescription Clone()
        {
            return new RasterizerLightSourceDescription(this);
        }
    }

    // Besteht aus 2 Dreiecken und wird so behandelt, als ob die Fläche unendlich groß und weit weg ist und die Lichtstrahlen parallel verlaufen
    public class FarAwayDirectionLightDescription : IRaytracingLightSource
    {
        public float Emission { get; set; } = 1;                              //Radiant flux -> Wie viel Watt hat die Lichtquelle? (Die Radiance berechnet sich dann aus Emission / Oberflächeninhalt)
        public bool IsInfinityAway { get => true; }

        public FarAwayDirectionLightDescription() { }

        public FarAwayDirectionLightDescription(FarAwayDirectionLightDescription copy)
        {
            this.Emission = copy.Emission;
        }

        public IRaytracingLightSource Clone()
        {
            return new FarAwayDirectionLightDescription(this);
        }
    }

    // Kugel, die die gesamte Szene umschließt (Ist nicht unendlich groß)
    // Wenn man eine Textur angeben will, muss man an das Objekt, wo die Lichtquelle dranhängt, die TextureFile-Property setzen
    public class EnvironmentLightDescription : IRaytracingLightSource
    {
        public float Emission { get; set; } = 1;                              //Radiant flux -> Wie viel Watt hat die Lichtquelle? (Die Radiance berechnet sich dann aus Emission / Oberflächeninhalt)
        public bool IsInfinityAway { get => true; }
        public float Rotate { get; set; } = 0; //Zahl zwischen 0 und 1. Rotiert die Umgebungslichtkugel um ihre Vertikal-Achse
        public Vector3D CameraUpVector { get; set; } = null; //SmallUPBP verwendet für die Kamera ein anderen CameraUpVector als für die Phi/Theta-Umrechung im Umgebungslicht. Gibt man hier null an, wird der Vektor von der Kamera genommen

        public EnvironmentLightDescription() { }

        public EnvironmentLightDescription(EnvironmentLightDescription copy)
        {
            this.Emission = copy.Emission;
            this.Rotate = copy.Rotate;
        }

        public IRaytracingLightSource Clone()
        {
            return new EnvironmentLightDescription(this);
        }
    }

    // Besteht aus 2 Dreicken welche in einer Ebene liegen. Strahlt diffuse mit Normale von den Dreieck. Außerdem kann es noch zusätzlich in Richtung SpotDirection leuchten.
    // Wenn SpotMix == 0, dann ist das eine rein Diffuse Lampe. Wenn SpotMix == 1, dann leuchtet es 100% seiner Energie in Richtung SpotDirection
    public class SurfaceWithSpotLightDescription : IRaytracingLightSource
    {
        public float Emission { get; set; } = 1;                              //Radiant flux -> Wie viel Watt hat die Lichtquelle? (Die Radiance berechnet sich dann aus Emission / Oberflächeninhalt)
        public bool IsInfinityAway { get => false; }
        public Vector3D SpotDirection { get; set; } = null; //Wenn null angegeben wird, dann wird SpotDirection implizit auf die Flächennormale von den Viereck gelegt
        public float SpotCutoff { get; set; } = 1;        // Der Öffnungswinkel muss zwischen 1 und 90 liegen
        public float SpotMix { get; set; } = 1; //Zahl zwischen 0 und 1. 0==100%Diffuse;1==100% in SpotDirection
        public bool UseWithoutDiffuseDictionSamplerForLightPathCreation { get; set; } = false; //Wenn true, nutzt er für das Lighttracing/Photonmap nur das enge Sampling in Richtung SpotDirection (Eigentlich unphysikalisch aber sieht schöner aus)

        public SurfaceWithSpotLightDescription() { }

        public SurfaceWithSpotLightDescription(SurfaceWithSpotLightDescription copy)
        {
            this.Emission = copy.Emission;
            this.SpotDirection = copy.SpotDirection;
            this.SpotCutoff = copy.SpotCutoff;
            this.SpotMix = copy.SpotMix;
        }

        public IRaytracingLightSource Clone()
        {
            return new SurfaceWithSpotLightDescription(this);
        }
    }

    // Generiere Lichtstrahl von zufälligen Punkt auf der Oberfläche
    public class DiffuseSurfaceLightDescription : IRaytracingLightSource
    {
        public float Emission { get; set; } = 1;                              //Radiant flux -> Wie viel Watt hat die Lichtquelle? (Die Radiance berechnet sich dann aus Emission / Oberflächeninhalt)
        public bool IsInfinityAway { get => false; }

        public DiffuseSurfaceLightDescription() { }

        public DiffuseSurfaceLightDescription(DiffuseSurfaceLightDescription copy)
        {
            this.Emission = copy.Emission;
        }

        public IRaytracingLightSource Clone()
        {
            return new DiffuseSurfaceLightDescription(this);
        }
    }

    //Oberflächenlicht mit Importance-Cellen
    public class ImportanceSurfaceLightDescription : IRaytracingImportanceLight
    {
        public float Emission { get; set; } = 1;                              //Radiant flux -> Wie viel Watt hat die Lichtquelle? (Die Radiance berechnet sich dann aus Emission / Oberflächeninhalt)
        public bool IsInfinityAway { get => false; }

        public LightImportanceSamplingMode ImportanceSamplingMode { get; set; } = LightImportanceSamplingMode.IsVisibleFromCamera;  // In die Richtung, wo Objekte sehr nah sind, werden weniger Photonen gesendet, als in Objekte, die weit weg sind
        public int CellSurfaceCount { get; set; } = 30; //Jedes Dreieck/Quad/Kugel wird in CellSurfaceCount*CellSurfaceCount Kästchen unterteilt
        public int CellDirectionCount { get; set; } = 15; //Auf jeder SurfaceCelle befinden sich CellDirectionCount*CellDirectionCount Richtungscellen

        public ImportanceSurfaceLightDescription() { }

        public ImportanceSurfaceLightDescription(ImportanceSurfaceLightDescription copy)
        {
            this.Emission = copy.Emission;
            this.ImportanceSamplingMode = copy.ImportanceSamplingMode;
            this.CellSurfaceCount = copy.CellSurfaceCount;
            this.CellDirectionCount = copy.CellDirectionCount;
        }

        public IRaytracingLightSource Clone()
        {
            return new ImportanceSurfaceLightDescription(this);
        }
    }

    //Oberflächenlicht was hauptsächlich nach vorne strahlt mit Importance-Cellen
    public class ImportanceSurfaceWithSpotLightDescription : IRaytracingImportanceLight
    {
        public float Emission { get; set; } = 1;                              //Radiant flux -> Wie viel Watt hat die Lichtquelle? (Die Radiance berechnet sich dann aus Emission / Oberflächeninhalt)
        public bool IsInfinityAway { get => false; }

        public Vector3D SpotDirection { get; set; } = null; //Wenn null angegeben wird, dann wird SpotDirection implizit auf die Flächennormale von den Viereck gelegt
        public float SpotCutoff { get; set; } = 1;        // Der Öffnungswinkel muss zwischen 1 und 90 liegen
        public float SpotMix { get; set; } = 1; //Zahl zwischen 0 und 1. 0==100%Diffuse;1==100% in SpotDirection

        public LightImportanceSamplingMode ImportanceSamplingMode { get; set; } = LightImportanceSamplingMode.IsVisibleFromCamera;  // In die Richtung, wo Objekte sehr nah sind, werden weniger Photonen gesendet, als in Objekte, die weit weg sind
        public int CellSurfaceCount { get; set; } = 50; //Jedes Dreieck/Quad/Kugel wird in CellSurfaceCount*CellSurfaceCount Kästchen unterteilt

        public ImportanceSurfaceWithSpotLightDescription() { }

        public ImportanceSurfaceWithSpotLightDescription(ImportanceSurfaceWithSpotLightDescription copy)
        {
            this.Emission = copy.Emission;

            this.SpotDirection = copy.SpotDirection;
            this.SpotCutoff = copy.SpotCutoff;
            this.SpotMix = copy.SpotMix;

            this.ImportanceSamplingMode = copy.ImportanceSamplingMode;
            this.CellSurfaceCount = copy.CellSurfaceCount;
        }

        public IRaytracingLightSource Clone()
        {
            return new ImportanceSurfaceWithSpotLightDescription(this);
        }
    }

    // Generiere Lichtstrahl von zufälligen Punkt auf der Oberfläche
    public class DiffuseSphereLightDescription : IRaytracingLightSource
    {
        public float Emission { get; set; } = 1;                              //Radiant flux -> Wie viel Watt hat die Lichtquelle? (Die Radiance berechnet sich dann aus Emission / Oberflächeninhalt)
        public bool IsInfinityAway { get => false; }

        public DiffuseSphereLightDescription() { }

        public DiffuseSphereLightDescription(DiffuseSphereLightDescription copy)
        {
            this.Emission = copy.Emission;
        }

        public IRaytracingLightSource Clone()
        {
            return new DiffuseSphereLightDescription(this);
        }
    }

    // Generiere Lichtstrahl der in Spot liegt
    public class SphereWithSpotLightDescription : IRaytracingSphereWithSpotLight
    {
        public float Emission { get; set; } = 1;                              //Radiant flux -> Wie viel Watt hat die Lichtquelle? (Die Radiance berechnet sich dann aus Emission / Oberflächeninhalt)
        public bool IsInfinityAway { get => false; }

        public float SpotCutoff { get; set; }                            // Der Öffnungswinkel muss zwischen 0 und 90 liegen. Wenn 180, dann ist Richtungslicht deaktiviert
        public Vector3D SpotDirection { get; set; }    // Richtung in Weltkoordinaten

        public SphereWithSpotLightDescription() { }

        public SphereWithSpotLightDescription(SphereWithSpotLightDescription copy)
        {
            this.Emission = copy.Emission;
            this.SpotCutoff = copy.SpotCutoff;
            this.SpotDirection = copy.SpotDirection;
        }

        public IRaytracingLightSource Clone()
        {
            return new SphereWithSpotLightDescription(this);
        }
    }
}
