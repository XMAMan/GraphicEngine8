namespace GraphicMinimal
{
    //Erzeugt für ein gegebenen 3D-Punkt die zugehörigen UV-Texturkoordinaten
    public interface ITextureMapping
    {
        Vector2D Map(Vector3D pos);
    }
}
