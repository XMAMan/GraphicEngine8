using System.Collections.Generic;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;

namespace RaytracingLightSource
{
    //Stellt eine einzelne Lichtquelle dar
    interface IRayLightSource
    {
        IIntersectableRayDrawingObject RayDrawingObject { get; }
        float EmittingSurfaceArea { get; }
        float Emission { get; } //Wie viel Photonen pro Sekunde sendet die gesamte Lichtfläche aus
        float EmissionPerArea { get; } //Leuchtkraft pro Fläche => Entspricht Emission / EmittingSurvaceArea (So viel Leuchtet ein einzelner Punkt auf der Fläche. Man darf aber immer nur zwischen zwei Flächen die Lichtenergie austauschen.)
        float GetDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime); //Berechnet die Wahrscheinlichkeit, einen Punkt auf der Lichtquelle zu erzeugen, welcher den eyePoint beleuchtet. d.h. die Fläche der Lichtquelle, welche vom eyePoint aus zu sehen ist(Ohne VisibleTest)
        float GetMultipleDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime); //Berechnet die Wahrscheinlichkeit, das einer von den vielen DirectLight-Samples dem pointOnLight entspricht
        float GetPdfWFromLightDirectionSampling(IntersectionPoint pointOnLight, Vector3D direction);
        float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight);

        List<DirectLightingSampleResult> GetRandomPointOnLightList(Vector3D eyePoint, IRandom rand);
        DirectLightingSampleResult GetRandomPointOnLight(Vector3D eyePoint, IRandom rand); //Gibt null zurück, wenn eyepoint außerhalb vom Spotcutoff oder keine Fläche zum eyepoint zeigt

        float GetEmissionForEyePathHitLightSourceDirectly(IntersectionPoint pointOnLight, Vector3D eyePoint, Vector3D directionFromEyeToLightPoint);//Falls sich beim Brdf-Sampling beim eyePoint jemand die Lichtquelle am pointOnLight trifft

        SurfaceLightPointForLightPathCreation GetRandomPointForLightPathCreation(IRandom rand); //Zum erstellen von LightSub-Pahts
    }

    public class DirectLightingSampleResult
    {
        public Vector3D DirectionToLightPoint;
        public float PdfA; //(1 / Flächeninhalt) von der Lichtfläche, welche vom Hitpoint aus zu sehen ist
        public IIntersectableRayDrawingObject LightSource;
        public bool IsLightIntersectable; //Ist das Licht Bestandteil vom IntersectionFinder/MediaIntersectionFinder? Wenn nein, dann ist es wohl Richtungslicht/Umgebungslicht
        public bool LightSourceIsInfinityAway = false; //True bei Richtungs- und Umgebungslicht
        public IntersectionPoint LightPointIfNotIntersectable = null;
        
    }

    public class SurfaceLightPointForLightPathCreation
    {
        public IntersectionPoint PointOnLight;
        public Vector3D Direction;
        public float PdfA;
        public float PdfW;
        public float EmissionPerArea;
        public bool LightSourceIsInfinityAway = false; //True bei Richtungs- und Umgebungslicht
    }

    interface ISphereRayLightSource : IRayLightSource
    {
        Vector3D Center { get; }
        float Radius { get; }
    }
}
