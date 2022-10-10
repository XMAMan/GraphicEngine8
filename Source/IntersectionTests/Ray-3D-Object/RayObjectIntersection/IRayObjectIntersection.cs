using System.Collections.Generic;
using GraphicMinimal;
using GraphicGlobal;
using ParticipatingMedia.Media;
using TextureHelper;

namespace IntersectionTests
{
    //Zum Schnittpunktest zwischen 3D-Objekten und einem Strahl
    public interface IRayObjectIntersection
    {
        IIntersectionPointSimple GetIntersectionPoint(Ray ray, IIntersecableObject excludedObject, float maxDistance, float time);   //Schnitpunkttest für Primärstrahlen(excludedObjekt==null) und Sekundärstrahlen (excludedObjekt!=null) 
        List<IIntersectionPointSimple> GetAllIntersectionPoints(Ray ray, float maxDistance, float time);
    }

    //Enthält nur die Schnittpunkt-Daten, welcher das Interface IRayObjectIntersection (BIH oder der KD-Baum) benötigt
    public interface IIntersectionPointSimple
    {
        Vector3D Position { get; }
        float DistanceToRayStart { get; }
        IIntersecableObject IntersectedObject { get; } //Wird benötigt, um TransformSimplePointToIntersectionPoint aufrufen zu können
    }

    //Schnittpunkttest zwischen einen Strahl und ein einzelnen 3D-Objekt (Triangle, Blob, Sphere)
    public interface IIntersecableObject
    {
        IIntersectableRayDrawingObject RayHeigh { get; } //Wird für die GetAllIntersectionPoints-Abfrage benötigt, damit doppelte Punkt auf gleichen Objekt entfernt werden können
        Vector3D AABBCenterPoint { get; } //Das ist der Mittelpunkt von der Axis Aligned Bounding Box
        Vector3D MinPoint { get; }
        Vector3D MaxPoint { get; }
        IIntersectionPointSimple GetSimpleIntersectionPoint(Ray ray, float time);        // Schnittpunktstest für Strahl, gibt null zurück, wenn es kein Schnittpunkt gibt
        List<IIntersectionPointSimple> GetAllIntersectionPoints(Ray ray, float time);       //Bei Kugel/Blob können hier bis zu 2 Schnittpunkte returned werden
        IntersectionPoint TransformSimplePointToIntersectionPoint(IIntersectionPointSimple simplePoint); //Erweitert die Schnittpunktdaten um Farbe + Normale
    }

    //Beim GetAllIntersectionPoints muss ich wissen, ob es mehr als ein Schnittpunkt pro Objekt geben kann
    public interface IFlatIntersectableObject : IIntersecableObject
    {
    }

    //Was muss das IIntersecableObject über sein Eltern-Element(Würfel, Spirale, 3D-Wavefront) zum Zeitpunkt der KD-Baum-Abfrage alles wissen, um ein IIntersectionPoint-Object erzeugen zu können?
    public interface IIntersectableRayDrawingObject
    {
        IRaytracerDrawingProps Propertys { get; }
        Vector3D GetColor(float textcoordU, float textcoordV, Vector3D position);
        bool IsBlackColor(float textcoordU, float textcoordV, Vector3D position); //Kein Schnittpunkt wegen BlackIsTransparent-Check
        IParticipatingMedia Media { get; } //Dieses Medium befindet sich innerhalb vom RayHeigh
        ParallaxMapping ParallaxMap { get; } //Kein Schnittpunkt wegen Parallax-Edge-Cutoff
        IntersectionPoint CreateIntersectionPoint(Vertex point, Vector3D orientedFlatNormal, Vector3D notOrientedFlatNormal, Vector3D rayDirection, ParallaxPoint parallaxPoint, IIntersecableObject intersectedObject); //point = Schnittpunkt mit Dreieck/Kugel wo die Normale laut NormalMode interpoliert wurde (Flat/Smooth)
    }
}
