namespace GraphicGlobal
{
    public interface IDivideable
    {
        float SurfaceArea { get; }
        IDivideable[] Divide();
    }
}
