namespace GraphicMinimal
{
    //Hier raus kommt die Objektfarbe
    public enum ColorSource
    {
        Texture,                 //Farbe kommt aus Textur
        ColorString,             //Einfarbiges Objekt. Angabe über #FF00FF
        Procedural               //Farbe kommt aus Prozedur laut FarbProzeduralFunktion-Property
    }

    //Hier kommt die Normale her
    public enum NormalSource
    {
        ObjectData, //Normale kommt aus Dreiecksnormale und wird laut NormalInterpolation-Property entweder interpoliert oder nicht
        Normalmap,  //Normale kommt aus BumpmapFile
        Parallax,   //Normale kommt aus BumpmapFile; Höhenwert auch
        Microfacet, //Normale wird über Microfacet-Brdf erzeugt und Roughnessmap sagt dann, an welchen Stellen die Fläche 100% glatt ist und wo Microfacet-Struktur
        Procedural  //Normale kommt aus Prozedur laut NormalProzeduralFunktion-Property
    }

    public enum TexturCoordSource
    {
        ObjectData, //Texturkoordinaten kommen aus den Objektaten und werden über die Texturmatrix noch modifiziert
        Procedural, //Texturkoordinaten werden laut TexturCoordsProzeduralFunktion erzeugt
    }

    public enum TextureFilter
    {
        Point = 0,
        Linear = 1,
        Anisotroph = 2
    }

    public enum TextureMode
    {
        Clamp,       // Begrenze UV-Werte auf 0..1
        Repeat      // UV-Werte gehen Modulo
    }

    public enum InterpolationMode
    {
        Smooth,     //Im Vertex-Shader wird die Normale interpoliert (Ohne Z-Division)
        Flat        //Es erfolgt keine Interpolation der Normale
    }

    //Was soll die Lichtquelle bevorzugt anstrahlen?
    public enum LightImportanceSamplingMode
    {
        Specular,
        IsVisibleFromCamera,
        SpecularOrVisibleFromCamera
    }

    //Raytracermaterialien
    public enum BrdfModel
    {
        Diffus,
        Mirror,
        MirrorWithRust, // Spiegel mit Rostflecken (Die Rosttextur kommt über TextureFile, die Spiegelfarbe über MirrorColor)
        TextureGlass,   // Beim brechen/reflektieren wird bei der Brdf-Funktion die Farbe aus der Textur genommen
        MirrorGlass,    // Beim brechen ist die Brdf 1, beim Reflektieren MirrorColor
        Phong,          // Spiegel mit Streuung der zu 10% diffuse ist -> (Glossy = 100% Microfacet-Spiegel; Phong = Teilweise diffuse; teilweise Glossy)
        Tile,           // Verbundmaterial aus Diffuse + Spiegel mit konstanten Diffusefaktor (Das ist NICHT der Tisch von Stilllife-SmallUPBP)
        FresnelTile,    // Verbundmaterial aus Diffuse + Spiegel mit Diffusfaktor, der sich aus dem Fresnelterm ergibt
        DiffuseAndMirror,//Verbundmaterial aus Diffuse + Spiegel wo die Brdfs nicht gemischt werden sondern addiert (Tisch von Stilllife-SmallUPBP)
        PlasticDiffuse, // Verbundmaterial aus Diffuse + Glanzpunkt ohne SampleBrdf
        PlasticMirror,  // Verbundmaterial aus Diffuse + Glanzpunkt ohne SampleBrdf + Spiegel
        PlasticMetal,   // Verbundmaterial aus Diffuse + Glanzpunkt mit SampleBrdf
        WalterGlass,    // Microfacet
        WalterMetal,    // Microfacet
        HeizGlass,      // Microfacet
        HeizMetal,      // Microfacet
        MicrofacetTile, // Verbundmaterial aus Diffuse + HeizMetall
        DiffusePhongGlassOrMirrorSum //Material aus SmallUPBP
    }

    //Über dieses Verfahren wird der Farbwert berechnet
    public enum ColorProceduralFunction
    {
        Tile,
        Wood,
        ToonShader,
        Hatch,
    }

    //Verfahren, wie die Normale prozedural berechnet werden kann
    public enum NormalProceduralFunction
    {
        PerlinNoise,
        SinForU,
        SinUCosV,
        Tent,
    }

    //Schatten (momentan Globale Einstellung nur beim Raytracer)
    public enum RaytracerShadowMode
    {
        NoShadows,
        Hard,
        Soft,
        SoftOneSample
    };

    public enum RasterizerShadowMode
    {
        Stencil,
        Shadowmap
    }

    public enum RaytracerRenderMode
    {
        SmallBoxes, //Wenn ich ein Bild mit nur ein Sample pro Pixel ausgeben will, dann ist das hier schneller bei Verfahren, die keine Lightracing-Pfade erzeugen
        Frame
    }

    public enum RaytracerAutoSaveMode
    {
        Disabled,
        FullScreen,
        SaveAreas
    }

    public enum TonemappingMethod
    { 
        None,       //Kein Tonemapping und keine Gammakorrektur
        GammaOnly,  //Kein Tonemapping nur Gammakorrektur
        Reinhard,
        Ward,
        HaarmPeterDuikersCurve,
        JimHejlAndRichardBurgessDawson,
        Uncharted2Tonemap,
        ACESFilmicToneMappingCurve //Filmic-Tonemapping + Gammakorrektur danach
    }

    public enum RadiosityColorMode
    {
        WithoutColorInterpolation, 
        WithColorInterpolation,
        RandomColors
    }

    public enum PixelSamplingMode
    {
        None,
        Equal,
        Tent,         
    }

    public enum PhotonmapDirectPixelSetting
    {
        CountHowManyPhotonsAreInFieldOfView, //Wenn ich das ImportanceLight untersuchen will
        ShowMediaLongBeams,                  //Wenn ich bei Stilllife schauen will ob die Media-Objekte mit Beams gefüllt sind
        ShowMediaShortBeams,                 //Wenn ich Media-Pfade 
        ShowGodRays,                         //Wenn ich Godrays untersuchen will
        ShowNoMediaCaustics,                 //Wenn ich NoMedia-Causticen untersuchen will
        ShowPixelPhotons,                    //Wenn ich alle Photonen (Surface/Partikel) sehen will
        ShowParticlePhotons,                 //Wenn ich Volumetrische Causticen untersuchen will
        ShowDirectLightPhotons
    }
}
