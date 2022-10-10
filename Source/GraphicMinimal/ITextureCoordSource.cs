namespace GraphicMinimal
{
    //UV-Bestimmung über die Texturmatrix und Objektdata oder über Prozedur 
    //Man braucht die UV-Daten für die Farb- und Normalmap
    public interface ITextureCoordSource
    {
        TexturCoordSource Type { get; }
    }
    public class ObjectDataTextureCoordSource : ITextureCoordSource
    {
        public TexturCoordSource Type { get => TexturCoordSource.ObjectData; }
    }
    public class ProceduralTextureCoordSource : ITextureCoordSource
    {
        public TexturCoordSource Type { get => TexturCoordSource.Procedural; }
        public ITextureMapping TextureCoordsProceduralFunction { get; set; } = null;
    }
}
