namespace GraphicMinimal
{
    public interface IColorSource 
    {
        ColorSource Type { get; }
        T As<T>() where T : IColorSource;
    }

    public class ColorFromTexture : TextureData, IColorSource
    {
        public ColorSource Type { get => ColorSource.Texture; }
        public T As<T>() where T : IColorSource => (T)(IColorSource)this;

        public string TextureFile { get; set; } = "";
        public override string ToString()
        {
            return this.TextureFile;
        }
    }


    public class TextureData
    {
        public Matrix3x3 TextureMatrix { get; set; } = Matrix3x3.Ident(); //Mit dieser 3x3-Matrix werden die Texturkoordinaten im Vertex-Shader multipliziert. Das hat Einfluß auf die Textur für die Farbe als auch auf die Normalmap/Parallaxmap
        public TextureFilter TextureFilter { get; set; } = TextureFilter.Point;
        public TextureMode TextureMode { get; set; } = TextureMode.Repeat; //Wird in der Farbtextur als auch Normalmap verwendet
    }

    public class ColorFromRgb : IColorSource
    {
        public ColorSource Type { get => ColorSource.ColorString; }
        public T As<T>() where T : IColorSource => (T)(IColorSource)this;

        public Vector3D Rgb { get; set; }
        public override string ToString()
        {
            return this.Rgb.ToShortString();
        }
    }

    public class ColorFromProcedure : IColorSource
    {
        public ColorSource Type { get => ColorSource.Procedural; }
        public T As<T>() where T : IColorSource => (T)(IColorSource)this;

        public ColorProceduralFunction ColorProceduralFunction { get; set; } = ColorProceduralFunction.Tile;
        public string ColorString { get; set; } = "#FFFFFF";

        public override string ToString()
        {
            return this.ColorString + "-" + ColorProceduralFunction.ToString();
        }
    }

    //####################################################################################################
    //####################################################################################################
    //####################################################################################################

    public interface INormalSource 
    {
        NormalSource Type { get; }
        T As<T>() where T : INormalSource;
    }
    public class NormalFromObjectData : INormalSource
    {
        public NormalSource Type { get => NormalSource.ObjectData; }
        public T As<T>() where T : INormalSource => (T)(INormalSource)this;        
    }

    public abstract class NormalMapFromFile : TextureData, INormalSource
    {
        public abstract NormalSource Type { get; }
        public T As<T>() where T : INormalSource => (T)(INormalSource)this;

        public bool ConvertNormalMapFromColor = false; //true = BumpmapFile wird als Farbtextur interpretiert, welche zuerst in Grauwerte und dann in Normalmap umgewandelt wird; false = BumpmapFile wird als Normalmap interprettiert
        public abstract string FileName { get; } //Name auf die Normalmap(RGB=Normale; Kein Alpha) / Parallaxmap(RGB=Normale; A=Höhenwert); RoughnessMap (Schwarzweißbild)
    }

    public class NormalFromMap : NormalMapFromFile, INormalSource
    {
        public override NormalSource Type { get => NormalSource.Normalmap; }
        public override string FileName { get => this.NormalMap; }
        public string NormalMap { get; set; } = ""; //RGB=Normale; Alpha wird nicht beachtet
    }

    public class NormalFromParallax : NormalMapFromFile, INormalSource
    {
        public override NormalSource Type { get => NormalSource.Parallax; }
        public override string FileName { get => this.ParallaxMap; }
        public string ParallaxMap { get; set; } = ""; //RGB=Normale; Alpha=Höhenwerte

        public float TexturHeightFactor { get; set; } = 1.0f; //Tiefe der Textur beim Parallax- und Displacement-Mapping
        public bool IsParallaxEdgeCutoffEnabled { get; set; } = false; //Nur auf true setzen, wenn man ein Flaches Objekt hat, dessen Vorderkante man
    }

    public class NormalFromMicrofacet : NormalMapFromFile, INormalSource
    {
        public override NormalSource Type { get => NormalSource.Microfacet; }
        public override string FileName { get => this.RoughnessMap; }

        public Vector2D MicrofacetRoughness { get; set; } = new Vector2D(0.01f, 0.01f); //Microfacet-Roughness in u- und v-Richtung
        public string RoughnessMap { get; set; } //Wenn man eine Microfacet-Brdf nutzt, dann wird die RoughnessMap als 0/1-Bild interpretiert, um zu sagen, wo Normale=Microfacet; Sonst gilt Normale=ObjectData-Normale
    }

    public class NormalFromProcedure : INormalSource
    {
        public NormalSource Type { get => NormalSource.Procedural; }

        public T As<T>() where T : INormalSource => (T)(INormalSource)this;

        public Matrix3x3 TextureMatrix { get; set; } = Matrix3x3.Ident(); //Ich habe hier zwar keine Textur die ich auslesen will aber ich verwende die Objekt-Texturkoordinaten neben der 3D-Position als Input für die Procedurale Funktion
        public INormalProceduralFunction Function { get; set; } = null;
    }
    public interface INormalProceduralFunction 
    {
        NormalProceduralFunction NormalProceduralFunction { get; }
    }
    public class NormalProceduralFunctionPerlinNoise : INormalProceduralFunction
    {
        public NormalProceduralFunction NormalProceduralFunction { get => NormalProceduralFunction.PerlinNoise; }
        public float NormalNoiseFactor { get; set; } = 0.1f;
    }
    public class NormalProceduralFunctionSinForU : INormalProceduralFunction
    {
        public NormalProceduralFunction NormalProceduralFunction { get => NormalProceduralFunction.SinForU; }
    }
    public class NormalProceduralFunctionSinUCosV : INormalProceduralFunction
    {
        public NormalProceduralFunction NormalProceduralFunction { get => NormalProceduralFunction.SinUCosV; }
    }
    public class NormalProceduralFunctionTent : INormalProceduralFunction
    {
        public NormalProceduralFunction NormalProceduralFunction { get => NormalProceduralFunction.Tent; }
    }
}
