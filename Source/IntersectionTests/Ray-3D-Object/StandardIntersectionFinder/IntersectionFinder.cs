using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using GraphicGlobal;
using IntersectionTests.Ray_3D_Object.IntersectableObjects;

namespace IntersectionTests
{
    //Der IntersectionPoint kennt das RayDrawingObject. Deswegen habe ich den IntersectionPoint und den IntersectionFinder hier definiert und nicht im IntersectionTests-Projekt
    //Ich warte mal noch ab, welche public-Methoden/Propertys bei RayDrawingObject noch hinzukommen. Bis jetzt sind es nur IIntersectableRayDrawingObject-Sachen. Wenn das so bleibt,
    //dann kann der IntersectionFinder + IntersectionPoint doch noch ins IntersectionTests-Projekt wandern
    public class IntersectionFinder : IParseableString
    {
        private readonly IRayObjectIntersection rayObjectIntersection;
        //private IRayObjectIntersection rayObjectIntersection1; //So kann man ein Fehler im KDBaum finden (Siehe KDSahTreeTests um zu sehen, welche Tests dann umfallen)

        public List<IIntersecableObject> RayObjekteRawList { get; private set; }

        public BoundingBox GetBoundingBoxFromSzene()
        {
            if (this.RayObjekteRawList.Any() == false) return new BoundingBox(new Vector3D(-1, -1, -1), new Vector3D(1, 1, 1));
            return IntersectionHelper.GetBoundingBoxFromIVolumeObjektCollection(this.RayObjekteRawList);
        }

        public IntersectionFinder(List<IIntersecableObject> rayObjects, Action<string, float> progressChangeHandler)
        {
            this.RayObjekteRawList = rayObjects;
            //this.rayObjectIntersection = new LinearSearchIntersector(rayObjects); //7 Minuten
            //this.rayObjectIntersection = new BoundingIntervallHierarchy(rayObjects, progressChangeHandler); //48 Sekunden
            this.rayObjectIntersection = new KDSahTree(rayObjects, progressChangeHandler); //30 Sekunden Rekursiv; 31 Sekunden Iterativ 
            

        }

        public string ToCtorString()
        {
            return string.Join(",\n", this.RayObjekteRawList.Select(x => ((IParseableString)x).ToCtorString()));
        }

        //Findet den nächsten Schnittpunkt auch dann, wenn Strahl auf Kugelrand startet und Lichtstrahl gebrochen wurde
        //parallaxPoint = Wenn der Strahl von ein Objekt startet, was Parallaxmapping verwendet, dann ist dieser Parameter gefüllt. 
        public IntersectionPoint GetIntersectionPoint(Ray ray, float pathCreationTime, IIntersecableObject excludedObjekt = null, float maxDistance = float.MaxValue)
        {
            if (excludedObjekt is IntersectableSphere)
            {
                IntersectableSphere sphere = excludedObjekt as IntersectableSphere;
                Vector3D flatNormal = Vector3D.Normalize(ray.Start - sphere.Center);
                if (flatNormal * ray.Direction < 0)           //Strahl startet auf Kugelrand(Außen) und fliegt nach Brechung in Richtung Kugelinneres oder Strahl startet auf Innenrand und fliegt nach Reflexion gegen andere Innenwand
                {
                    var points = sphere.GetAllIntersectionPoints(ray, pathCreationTime);
                    if (points.Any() == false) return GetIntersectionPoint(ray, excludedObjekt, maxDistance, pathCreationTime); //Strahl befindet sich auf Innenwand von Kugel und wurde dort diffuse flach reflektiert, was dazu führt, dass er Kugel verläßt
                    var spherePoint = points.Last(); //Zweiter Schnittpunkt mit Kugel

                    //Jetzt prüfe nur noch, ob zwischen den Kugel-Eintrittspunkt und den Kugelaustrittspunkt noch ein anders Objekt liegt
                    var pointInSphere = this.rayObjectIntersection.GetIntersectionPoint(ray, excludedObjekt, spherePoint.DistanceToRayStart, pathCreationTime);
                    if (pointInSphere == null || pointInSphere.DistanceToRayStart == 0 || (pointInSphere.Position - ray.Start).Length() == 0) 
                        return sphere.TransformSimplePointToIntersectionPoint(spherePoint); //Innerhalb der Kugel liegt nichts, also gebe zweiten Kugelschnittpunkt zurück

                    return pointInSphere.IntersectedObject.TransformSimplePointToIntersectionPoint(pointInSphere); //Innerhalb der Kugel liegt was, also gebe dieses Objekt zurück
                }
            }

            return GetIntersectionPoint(ray, excludedObjekt, maxDistance, pathCreationTime);
        }

        private IntersectionPoint GetIntersectionPoint(Ray ray, IIntersecableObject excludedObject, float maxDistance, float time)   //Schnitpunkttest für Sekundärstrahlen. Es wird nur von Ray-Start bis maxDistance gesucht
        {
            IIntersectionPointSimple point = this.rayObjectIntersection.GetIntersectionPoint(ray, excludedObject, maxDistance, time);

            //So kann man ein Fehler im KDBaum finden (Vergleicht BIH gegen KD-Baum)
            /*IIntersectionPointSimple point1 = this.rayObjectIntersection1.GetIntersectionPoint(ray, excludedObject, maxDistance, time);
            if (point != null)
            {
                if (point1 == null)
                {
                    string objList = this.ToCtorString();
                    string rayString = ray.ToCtorString();
                    throw new Exception("");
                }
                if (Math.Abs(point.DistanceToRayStart - point1.DistanceToRayStart) > 0.0001f)
                {
                    string objList = this.ToCtorString();
                    string rayString = ray.ToCtorString();
                    throw new Exception("");
                }
            }
            else
            {
                if (point1 != null)
                {
                    string objList = this.ToCtorString();
                    string rayString = ray.ToCtorString();
                    throw new Exception("");
                }
            }*/

            if (point == null || point.DistanceToRayStart == 0) return null; 
            if ((point.Position - ray.Start).Length() == 0) return null; //throw new Exception("Abstand zwischen Brdf-Punkt und Light-Point darf nicht 0 sein " + point.DistanceToRayStart + " " + (excludedObjekt != point.IntersectedObject).ToString());

            return point.IntersectedObject.TransformSimplePointToIntersectionPoint(point);
        }

        public List<IntersectionPoint> GetAllIntersectionPoints(Ray ray, float maxDistance, float time)
        {
            var points = this.rayObjectIntersection.GetAllIntersectionPoints(ray, maxDistance, time);
            if (points == null) return null;
            return points.Select(x => x.IntersectedObject.TransformSimplePointToIntersectionPoint(x)).ToList();
        }
    }
}
