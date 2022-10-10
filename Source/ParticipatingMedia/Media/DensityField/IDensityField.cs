using GraphicMinimal;

namespace ParticipatingMedia.Media.DensityField
{
    //Dichtefeld, was für ein 3D-Punkt ein skalaren Dichte-Wert zurück gibt
    interface IDensityField
    {
        float MaxDensity { get; }
        float GetDensity(Vector3D point);
    }
}
