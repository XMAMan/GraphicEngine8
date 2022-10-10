namespace GraphicMinimal
{
    public class ObjectPropertys : ICommonDrawingProps, IRasterizerDrawingProps, IRaytracerDrawingProps
    {
        // Gemeinsame Eigenschaften zwischen OpenGL und Raytracer
        public string Name { get; set; } = "";
        public int Id { get; set; } = -1;
        public Vector3D Position { get; set; } = new Vector3D(0, 0, 0);
        public Vector3D Orientation { get; set; } = new Vector3D(0, 0, 0);
        public float SpecularHighlightPowExponent { get; set; } = 0;//20.0f; //0 = Kein Glanzpunkt (Pow-Exponent) 
        public float SpecularHighlightCutoff1 { get; set; } = 1; //Glanzpunkt mal diesen Faktor (Um so größer, um so kleiner wird die Glanzpunktfläche aber sie wird dafür heller)
        public float SpecularHighlightCutoff2 { get; set; } = 2; //Durch diesen Faktor (Um so größer um so dunkler wird der Glanzpunkt)
        public float Size { get; set; } = 1.0f;
        public IColorSource Color { get; set; } = new ColorFromRgb(); //ColorFromRgb, ColorFromTexture, ColorFromProcedure
        public INormalSource NormalSource { get; set; } = new NormalFromObjectData(); //NormalFromObjectData, NormalFromMap, NormalFromParallax, NormalFromMicrofacet, NormalFromProcedure
        public InterpolationMode NormalInterpolation { get; set; } = InterpolationMode.Flat; //Wie soll die Normale, mit der die TBN-Matrix erstellt wird, berechnet werden? Über Interpolation oder über die Dreiecksnormale?
        public bool HasBillboardEffect { get; set; } = false;
        public bool HasStencilShadow { get; set; } = false;
        public bool BlackIsTransparent { get; set; } = false;
        public string TextureFile //Wenn MediaDescription != null ist, dann wird implizit davon ausgegangen, dass DiffuseColor=(0,0,0) ist und das hier die SpecularColor ist, welche angibt, wie stark der MediaBorder wie ein Glas aussieht. Bei Wasser würde man hier (#FFFFFF) eintragen und bei Luft (#000000). Kerze bekommt Wert zwischen 0 und 1
        {
            set
            {
                if (value.StartsWith("#"))
                    this.Color = new ColorFromRgb() { Rgb = Vector3D.FromColorString(value) };
                else
                    this.Color = new ColorFromTexture() { TextureFile = value };
            }
        }
        public DisplacementData DisplacementData { get; set; } = new DisplacementData();
        public bool HasExplosionEffect { get; set; } = false;

        //Nur bei OpenGL/DirectX
        public RasterizerLightSourceDescription RasterizerLightSource { get; set; } = null;
        public bool CanReceiveLight { get; set; } = true;
        public bool ShowFromTwoSides { get; set; } = false;
        public bool HasSilhouette { get; set; } = false;
        public float Opacity { get; set; } = 0f;
        public bool UseCubemap { get; set; } = false;
        public bool IsMirrorPlane { get; set; } = false;
        public bool IsWireFrame { get; set; } = false;

        // Eigenschaften, die nur im Raytracer vorkommen
        public ITextureCoordSource TextureCoordSource { get; set; } = new ObjectDataTextureCoordSource();
        public IRaytracingLightSource RaytracingLightSource { get; set; } = null;
        public BlobPropertys BlobPropertys { get; set; } = null;
        public IParticipatingMediaDescription MediaDescription { get; set; } = null; //Mit diesen Medium ist das Objekt gefüllt (Es muss ein geschlossener Körper sein)
        public MotionBlurMovementDescription MotionBlurMovment { get; set; } = null;
        public bool CreateQuads { get; set; } = false; //Sollen Dreiecke, die ein Rechteck bilden, als ein Quad erzegt werden?
        public BrdfModel BrdfModel { get; set; } = BrdfModel.Diffus;       
        public float RefractionIndex { get; set; } = float.NaN; //Brechungsindex (Surface hat Index von NaN; Wolke/Dünne Luft 1; Wasser=1.33;Glas=1.5). Anhand des Brechungsindex erkennt die Brdf-Abfrage ob das ein Surfacepunkt(NaN) oder Glas ist. Bei Surfacepunkt darf In- und Out-Licht nicht auf unterschiedlichen Seiten liegen. Bei Microfacet schon
        public float Albedo { get; set; } = 0.2f; //Color-Wichtungsfaktor für die Diffuse Brdf (Bei Diffusen Lichtquellen sollte diese Zahl tendenziell bei 0.8 liegen und bei Wolkenlosen Environmentlight eher bei 0.1)
        public float SpecularAlbedo { get; set; } = 1.0f; //Wichtungsfaktor für Glas/Mirror/RoughMetall (Sollte immmer so nahe 1 liegen)
        public Vector3D MirrorColor { get; set; } = new Vector3D(1, 1, 1);
        public Vector3D GlossyColor { get; set; } = new Vector3D(1, 1, 1); //Legt für die Glossy-Brdf, welche ein Teil vom Phong-Material ist, die Farbe fest
        public float GlossyPowExponent { get; set; } = 200.0f; //Pow-Exponent von der Glossy-Brdf (Teil der Phong-Brdf)   
        public bool GlasIsSingleLayer { get; set; } = false; //Wenn das true ist, wird RayWasRefracted IMMER auf false gesetzt. Simmuliert quasi das Ein- und Austreten eines Strahles durch eine unendliche dünne Glasscheibe
        public float TileDiffuseFactor { get; set; } = 0.2f; //Damit kann der Diffuseanteil von all den Materialien eingestellt werden, welche ein diffusen Anteil haben

        public ObjectPropertys() 
        {
        }

        //Kopierkonstruktor
        public ObjectPropertys(ObjectPropertys copy)
        {
            this.Name = copy.Name;
            this.Id = copy.Id;
            this.Position = new Vector3D(copy.Position);
            this.Orientation = new Vector3D(copy.Orientation);            
            this.SpecularHighlightPowExponent = copy.SpecularHighlightPowExponent;
            this.SpecularHighlightCutoff1 = copy.SpecularHighlightCutoff1;
            this.SpecularHighlightCutoff2 = copy.SpecularHighlightCutoff2;
            this.Size = copy.Size;
            this.Color = copy.Color;
            this.NormalSource = copy.NormalSource;            
            this.NormalInterpolation = copy.NormalInterpolation;
            this.HasBillboardEffect = copy.HasBillboardEffect;
            this.HasStencilShadow = copy.HasStencilShadow;
            this.BlackIsTransparent = copy.BlackIsTransparent;
            this.DisplacementData = copy.DisplacementData;            
            this.HasExplosionEffect = copy.HasExplosionEffect;
            this.RasterizerLightSource = copy.RasterizerLightSource?.Clone();
            this.CanReceiveLight = copy.CanReceiveLight;
            this.ShowFromTwoSides = copy.ShowFromTwoSides;
            this.HasSilhouette = copy.HasSilhouette;
            this.Opacity = copy.Opacity;            
            this.UseCubemap = copy.UseCubemap;
            this.IsMirrorPlane = copy.IsMirrorPlane;
            this.IsWireFrame = copy.IsWireFrame;
            this.TextureCoordSource = copy.TextureCoordSource;
            this.RaytracingLightSource = copy.RaytracingLightSource?.Clone();
            this.BlobPropertys = copy.BlobPropertys;
            this.MediaDescription = copy.MediaDescription?.Clone();
            this.MotionBlurMovment = copy.MotionBlurMovment;
            this.CreateQuads = copy.CreateQuads;
            this.BrdfModel = copy.BrdfModel;                     
            this.RefractionIndex = copy.RefractionIndex;            
            this.Albedo = copy.Albedo;
            this.SpecularAlbedo = copy.SpecularAlbedo;
            this.MirrorColor = copy.MirrorColor;
            this.GlossyColor = copy.GlossyColor;
            this.GlossyPowExponent = copy.GlossyPowExponent;
            this.GlasIsSingleLayer = copy.GlasIsSingleLayer;
            this.TileDiffuseFactor = copy.TileDiffuseFactor;            
        }
    }

    public interface ICommonDrawingProps
    {
        string Name { get; set; }
        int Id { get; set; }
        Vector3D Position { get; set; }
        Vector3D Orientation { get; set; }             
        float SpecularHighlightPowExponent { get; set; }
        float SpecularHighlightCutoff1 { get; set; }
        float SpecularHighlightCutoff2 { get; set; }
        float Size { get; set; }
        IColorSource Color { get; set; }
        INormalSource NormalSource { get; set; }        
        InterpolationMode NormalInterpolation { get; set; }
        bool HasBillboardEffect { get; set; }
        bool HasStencilShadow { get; set; }
        bool BlackIsTransparent { get; set; }
        DisplacementData DisplacementData { get; set; }
        bool HasExplosionEffect { get; set; }
    }

    public interface IRasterizerDrawingProps : ICommonDrawingProps
    {
        RasterizerLightSourceDescription RasterizerLightSource { get; set; }
        bool CanReceiveLight { get; set; }
        bool ShowFromTwoSides { get; set; }
        bool HasSilhouette { get; set; }
        float Opacity { get; set; }        
        bool UseCubemap { get; set; }
        bool IsMirrorPlane { get; set; }
        bool IsWireFrame { get; set; }
    }

    public interface IRaytracerDrawingProps : ICommonDrawingProps
    {
        ITextureCoordSource TextureCoordSource { get; set; }
        IRaytracingLightSource RaytracingLightSource { get; set; }
        BlobPropertys BlobPropertys { get; set; }
        IParticipatingMediaDescription MediaDescription { get; set; }
        MotionBlurMovementDescription MotionBlurMovment { get; set; }
        bool CreateQuads { get; set; }
        BrdfModel BrdfModel { get; set; }        
        float RefractionIndex { get; set; }        
        float Albedo { get; set; }
        float SpecularAlbedo { get; set; }
        Vector3D MirrorColor { get; set; }
        Vector3D GlossyColor { get; set; }
        float GlossyPowExponent { get; set; }
        bool GlasIsSingleLayer { get; set; }
        float TileDiffuseFactor { get; set; }        
    }
    
}
