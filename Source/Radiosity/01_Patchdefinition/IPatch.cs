using System.Collections.Generic;
using IntersectionTests;
using GraphicMinimal;
using RayObjects;

namespace Radiosity
{
    interface IPatch : IIntersecableObject, IFlatObject
    {
        //1. ViewFaktors berechnen
        void AddViewFaktor(ViewFactor faktor);
        bool IsLightSource { get; }
        void SetCenterPointFromRayHeigh(Vector3D point);
        void SetLightingArea(float area);
        bool IsInSpotDirection(Vector3D point);

        //2. Beleuchten
        float EmissionPerPatch { get; } //Bei Lichtquellen kommt hier ein Wert. Sonst 0
        Vector3D InputRadiosity { get; set; } //Hier wird die reinkommende Energie aufsummiert
        Vector3D OutputRadiosity { get; set; } //Diese Energie wird auf seine ViewFaktors-Patches verteilt
        List<ViewFactor> ViewFaktors { get; }
        Vector3D ColorOnCenterPoint { get; }

        //3. Farben interpolieren
        Vector3D[] CornerPoints { get; }
        void SetCornerColor(int cornerIndex, Vector3D color);
    }

    //Gibt an, wie gut ich den 'Patch' sehen kann
    class ViewFactor
    {
        public IPatch Patch;
        public float FormFactor; //Gibt an, welche viel Photonen bei einer Empfängerfläche angkommen. Rechnung: EmpfängerflächePhotonencount =  SenderFläche-Photnencount * FormFaktor

        public override string ToString()
        {
            return this.FormFactor + " " + this.Patch.ToString();
        }
    }
}
