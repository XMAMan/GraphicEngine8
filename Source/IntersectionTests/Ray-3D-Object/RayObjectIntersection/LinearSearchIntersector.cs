using System.Collections.Generic;
using System.Linq;
using GraphicGlobal;
using GraphicMinimal;
using RayTracerGlobal;

namespace IntersectionTests
{
    public class LinearSearchIntersector : IRayObjectIntersection
    {
        private List<IIntersecableObject> rayObjects;
        public LinearSearchIntersector(List<IIntersecableObject> rayObjects)
        {
            this.rayObjects = rayObjects;
        }

        public IIntersectionPointSimple GetIntersectionPoint(Ray ray, IIntersecableObject excludedObject, float maxDistance, float time)
        {
            IIntersectionPointSimple point = null;
            float distance = maxDistance;
            GetIntersectionPoint(ray, excludedObject, time, ref point, ref distance);
            return point;
        }

        private void GetIntersectionPoint(Ray ray, IIntersecableObject excludedObjekt, float time, ref IIntersectionPointSimple nearestPoint, ref float minDistance)
        {
            for (int i=0;i<this.rayObjects.Count;i++)
            {
                var rayObjekt = this.rayObjects[i];
                if (rayObjekt != excludedObjekt)
                {
                    var point = rayObjekt.GetSimpleIntersectionPoint(ray, time);

                    //Die schnittpunkt.DistanceToRayStart > MagicNumbers.MinAllowedPathPointDistance habe ich drin, weil die SurfaceLightWithMotion-Klasse bei der DirectLighting-PdfA
                    //das Dreieck(IntersectedObjekt=Dreieck) benötigt aber die Schnittpunktabfrage als IntersectedObjekt das MotionBlurObjekt benötigt
                    if (point != null && point.DistanceToRayStart < minDistance) //&& point.DistanceToRayStart > MagicNumbers.MinAllowedPathPointDistance
                    {
                        minDistance = point.DistanceToRayStart;
                        nearestPoint = point;
                    }
                }
            }
        }

        public List<IIntersectionPointSimple> GetAllIntersectionPoints(Ray ray, float maxDistance, float time)
        {
            List<KeyValuePair<float, IIntersectionPointSimple>> returnList = new List<KeyValuePair<float, IIntersectionPointSimple>>();
            Vector3D rayEndpoint = ray.Start + ray.Direction * maxDistance;
            GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);

            var sortedList = returnList.OrderBy(x => x.Key).ToList();

            //Enterfne alle Punkte, dessen Abstand zueinander 0 ist. Damit wird verhindert, dass Strahl bei Kanten 
            //zwischen zwei Dreiecken hindurchfliegt und dann beide Dreiecke trifft. Wenn Strahl aus Würfel ein- oder austritt
            //dann darf es nur ein Schnittpunkt geben
            List<KeyValuePair<float, IIntersectionPointSimple>> destinctReturnList = new List<KeyValuePair<float, IIntersectionPointSimple>>();
            for (int i = 0; i < sortedList.Count - 1; i++)
            {
                float distanceToNext = sortedList[i + 1].Key - sortedList[i].Key;
                bool skip = distanceToNext == 0 && sortedList[i + 1].Value.IntersectedObject.RayHeigh == sortedList[i].Value.IntersectedObject.RayHeigh;
                //bool skip = distanceToNext < MagicNumbers.MinAllowedPathPointDistance && sortedList[i + 1].Value.IntersectedObject.RayHeigh == sortedList[i].Value.IntersectedObject.RayHeigh;
                if (skip == false) destinctReturnList.Add(sortedList[i]);
            }
            if (sortedList.Any()) destinctReturnList.Add(sortedList.Last());
            return destinctReturnList.Select(x => x.Value).ToList();
        }

        private void GetAllIntersectionPoints(Ray ray, List<KeyValuePair<float, IIntersectionPointSimple>> returnList, Vector3D rayEndpoint, float maxDistance, float time)
        {
            for (int i=0;i<this.rayObjects.Count;i++)
            {
                var schnittpunkte = rayObjects[i].GetAllIntersectionPoints(ray, time);
                foreach (var schnittpunkt in schnittpunkte)
                {
                    if (schnittpunkt.DistanceToRayStart <= maxDistance) //Hier an dieser Stelle steht keine '&& schnittpunkt.DistanceToRayStart > MagicNumbers.MinAllowedPathPointDistance'-Abfrage, da beim Brechen an Glas der Schnittpunkt nicht mit den Glas, in das eingedrungen werdne soll, gefunden wird
                    {
                        returnList.Add(new KeyValuePair<float, IIntersectionPointSimple>(schnittpunkt.DistanceToRayStart, schnittpunkt));
                    }
                }
            }
        }
    }
}
