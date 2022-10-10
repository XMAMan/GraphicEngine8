using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using GraphicGlobal;

namespace IntersectionTests
{
    public class BoundingIntervallHierarchy : IRayObjectIntersection
    {
        private BIHNode node = null;

        public BoundingIntervallHierarchy(List<IIntersecableObject> rayObjects, Action<string, float> progressChangedHandler)
        {
            if (rayObjects.Any())
            {
                this.node = BIHNode.Build(rayObjects);
            }

            if (progressChangedHandler != null)
            {
                progressChangedHandler("Fertig mit BoundingIntervallHierarchy erstellen", 100);
            }
        }

        public IIntersectionPointSimple GetIntersectionPoint(Ray ray, IIntersecableObject excludedObject, float maxDistance, float time)
        {
            if (this.node == null) return null;
            return this.node.GetIntersectionPointWithRay(ray, excludedObject, maxDistance, time);
        }

        public List<IIntersectionPointSimple> GetAllIntersectionPoints(Ray ray, float maxDistance, float time)
        {
            if (this.node == null) return new List<IIntersectionPointSimple>();
            return this.node.GetAllIntersectionPoints(ray, maxDistance, time);
        }
    }

    class BIHNode
    {
        enum Axis { X, Y, Z };

        class AxialPlane
        {
            public Axis Axi;
            public float Position;

            public AxialPlane(Axis axi, float position)
            {
                this.Axi = axi;
                this.Position = position;
            }
        }


        private static int MaxObjectCountPerNode = 5;
        private static int MaxRecursionDepth = 15;

        private Axis Axi;
        private float Left, Right;
        private BIHNode LeftNode, RightNode;
        private List<IIntersecableObject> RayObjects;

        private Vector3D sphereCenter;
        private float sphereRadiusSqr;

        #region Erstellen
        public static BIHNode Build(List<IIntersecableObject> rayObjects)
        {
            return new BIHNode(rayObjects, GetSplitPlaneFromAxis(rayObjects, Axis.X), 0);
        }
        
        private BIHNode(List<IIntersecableObject> rayObjects, AxialPlane splitPlane, int recursionDepth)
        {
            this.Axi = splitPlane.Axi;

            CalculateBoundingSphere(rayObjects);

            if (rayObjects.Count <= BIHNode.MaxObjectCountPerNode || recursionDepth >= MaxRecursionDepth)
            {
                this.RayObjects = rayObjects;
                this.Left = Single.NaN;
                this.Right = Single.NaN;
                this.LeftNode = null;
                this.RightNode = null;
            }
            else
            {
                List<IIntersecableObject> leftRayObjects = new List<IIntersecableObject>();
                List<IIntersecableObject> rightRayObjects = new List<IIntersecableObject>();

                GetLeftAndRightList(rayObjects, splitPlane, ref leftRayObjects, ref rightRayObjects);

                this.Left = GetMaxValueFromAxis(leftRayObjects, splitPlane.Axi);
                this.Right = GetMinValueFromAxis(rightRayObjects, splitPlane.Axi);

                this.RayObjects = null;

                this.LeftNode = new BIHNode(leftRayObjects, GetSplitPlaneFromAxis(leftRayObjects, GetNextAxi(leftRayObjects/*, splitPlane.Axi*/)), recursionDepth + 1);
                this.RightNode = new BIHNode(rightRayObjects, GetSplitPlaneFromAxis(rightRayObjects, GetNextAxi(rightRayObjects/*, splitPlane.Axi*/)), recursionDepth + 1);
            }

        }

        private void CalculateBoundingSphere(List<IIntersecableObject> rayObjects)
        {
            var box = IntersectionHelper.GetBoundingBoxFromIVolumeObjektCollection(rayObjects);

            this.sphereCenter = box.Center;
            this.sphereRadiusSqr = box.RadiusOutTheBox * box.RadiusOutTheBox;
        }

        //Splitplane über Mittelwert
        /*private static AxialPlane GetSplitPlaneFromAxis(List<IIntersecableObject> rayObjects, Axis axi)
        {
            switch (axi)
            {
                case Axis.X:
                    return new AxialPlane(Axis.X, rayObjects.Sum(x => x.AABBCenterPoint.x) / rayObjects.Count); 
                case Axis.Y:
                    return new AxialPlane(Axis.Y, rayObjects.Sum(x => x.AABBCenterPoint.y) / rayObjects.Count);
                case Axis.Z:
                    return new AxialPlane(Axis.Z, rayObjects.Sum(x => x.AABBCenterPoint.z) / rayObjects.Count);
            }
            throw new ArgumentException("Invalid Value in parameter axi: " + axi.ToString());
        }*/

        //Splitplane über Median
        private static AxialPlane GetSplitPlaneFromAxis(List<IIntersecableObject> rayObjects, Axis axi)
        {
            switch (axi)
            {
                case Axis.X:
                    var list1 = rayObjects.OrderBy(x => x.AABBCenterPoint.X).ToList();
                    return new AxialPlane(Axis.X, list1[list1.Count / 2].AABBCenterPoint.X);
                case Axis.Y:
                    var list2 = rayObjects.OrderBy(x => x.AABBCenterPoint.Y).ToList();
                    return new AxialPlane(Axis.Y, list2[list2.Count / 2].AABBCenterPoint.Y);
                case Axis.Z:
                    var list3 = rayObjects.OrderBy(x => x.AABBCenterPoint.Z).ToList();
                    return new AxialPlane(Axis.Z, list3[list3.Count / 2].AABBCenterPoint.Z);
            }
            throw new ArgumentException("Invalid Value in parameter axi: " + axi.ToString());
        }

        //Nächste Axe über längste Boundingbox-Kante
        private static Axis GetNextAxi(List<IIntersecableObject> rayObjects)
        {
            var box = IntersectionHelper.GetBoundingBoxFromIVolumeObjektCollection(rayObjects);

            float f1 = box.Max.X - box.Min.X, f2 = box.Max.Y - box.Min.Y, f3 = box.Max.Z - box.Min.Z;
            if (f1 > f2)
            {
                if (f1 > f3)
                {
                    return Axis.X;
                }
                else
                {
                    return Axis.Z;
                }
            }
            else
            {
                if (f2 > f3)
                {
                    return Axis.Y;
                }
                else
                {
                    return Axis.Z;
                }
            }
        }

        //Nächste Axe einfacher nach Reihenfolge: X, Y, Z, X, Y, Z, ...
        /*private static Axis GetNextAxi(Axis axi)
        {
            switch (axi)
            {
                case Axis.X:
                    return Axis.Y;
                case Axis.Y:
                    return Axis.Z;
                case Axis.Z:
                    return Axis.X;
            }
            throw new ArgumentException("Invalid Value in parameter axi: " + axi.ToString());
        }*/

        private static float GetMinValueFromAxis(List<IIntersecableObject> rayObjects, Axis axi)
        {
            switch (axi)
            {
                case Axis.X:
                    return rayObjects.Min(x => x.MinPoint.X);
                case Axis.Y:
                    return rayObjects.Min(x => x.MinPoint.Y);
                case Axis.Z:
                    return rayObjects.Min(x => x.MinPoint.Z);
            }
            throw new ArgumentException("Invalid Value in parameter axi: " + axi.ToString());
        }

        private static float GetMaxValueFromAxis(List<IIntersecableObject> rayObjects, Axis axi)
        {
            switch (axi)
            {
                case Axis.X:
                    return rayObjects.Max(x => x.MaxPoint.X);
                case Axis.Y:
                    return rayObjects.Max(x => x.MaxPoint.Y);
                case Axis.Z:
                    return rayObjects.Max(x => x.MaxPoint.Z);
            }
            throw new ArgumentException("Invalid Value in parameter axi: " + axi.ToString());
        }

        private static void GetLeftAndRightList(List<IIntersecableObject> rayObjects, AxialPlane splitPlane, ref List<IIntersecableObject> leftNode, ref List<IIntersecableObject> rightNode)
        {
            switch (splitPlane.Axi)
            {
                case Axis.X:
                    foreach (IIntersecableObject obj in rayObjects)
                    {
                        if (obj.AABBCenterPoint.X <= splitPlane.Position)
                        {
                            leftNode.Add(obj);
                        }
                        else
                        {
                            rightNode.Add(obj);
                        }
                    }
                    break;
                case Axis.Y:
                    foreach (IIntersecableObject obj in rayObjects)
                    {
                        if (obj.AABBCenterPoint.Y <= splitPlane.Position)
                        {
                            leftNode.Add(obj);
                        }
                        else
                        {
                            rightNode.Add(obj);
                        }
                    }
                    break;
                case Axis.Z:
                    foreach (IIntersecableObject obj in rayObjects)
                    {
                        if (obj.AABBCenterPoint.Z <= splitPlane.Position)
                        {
                            leftNode.Add(obj);
                        }
                        else
                        {
                            rightNode.Add(obj);
                        }
                    }
                    break;
            }

            //Wenn eine von beiden Listen Elemente enthält und die andere nicht, dann teile das auf
            //sowas passiert, wenn alle AABBCenterPoint-Punkte auf einen Punkt liegen
            TryToSplitList1(ref leftNode, ref rightNode);
            TryToSplitList1(ref rightNode, ref leftNode);
        }

        //Wenn list1 Elemente enthält und liste2 nicht, dann wird liste1 auf beide Listen aufgeteilt
        private static void TryToSplitList1(ref List<IIntersecableObject> list1, ref List<IIntersecableObject> list2)
        {
            if (list1.Any() && list2.Any() == false)
            {
                for (int i = list1.Count - 1; i > list1.Count / 2; i--)
                {
                    list2.Add(list1[i]);
                    list1.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Abfrage

        //Bei GetAllIntersectionPoints darf ich noch kein MagicNumbers.MinAllowedPathPointDistance-Check machen,
        //da das ja nur die Länge eines LineSegments bestimmt. Wenn ich bei Kante von Media-Würfel bin, der Durchlaufen wird,
        //dann muss es Ein- und Austrittspunkt geben
        public List<IIntersectionPointSimple> GetAllIntersectionPoints(Ray ray, float maxDistance, float time)
        {
            //SortedList<float, ISchnittpunkt> returnList = new SortedList<float, ISchnittpunkt>();
            List<KeyValuePair<float, IIntersectionPointSimple>> returnList = new List<KeyValuePair<float, IIntersectionPointSimple>>();
            Vector3D rayEndpoint = ray.Start + ray.Direction * maxDistance;
            GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);


            //return returnList.OrderBy(x => x.Key).Select(x => x.Value).ToList();

            
            var sortedList = returnList.OrderBy(x => x.Key).ToList();

            //Enterfne alle Punkte, dessen Abstand zueinander 0 ist. Damit wird verhindert, dass Strahl bei Kanten 
            //zwischen zwei Dreiecken hindurchfliegt und dann beide Dreiecke trifft. Wenn Strahl aus Würfel ein- oder austritt
            //dann darf es nur ein Schnittpunkt geben
            List<KeyValuePair<float, IIntersectionPointSimple>> destinctReturnList = new List<KeyValuePair<float, IIntersectionPointSimple>>();
            for (int i=0;i<sortedList.Count-1;i++)
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
            if (IntersectionHelper.IsAnyIntersectionPointBetweenRayAndSphere(ray, this.sphereCenter, this.sphereRadiusSqr) == false) return;

            if (this.RayObjects != null) //Blattknoten
            {
                foreach (var rayObject in this.RayObjects)
                {
                    var points = rayObject.GetAllIntersectionPoints(ray, time);
                    foreach (var point in points)
                    {
                        if (point.DistanceToRayStart <= maxDistance) //Hier an dieser Stelle steht keine '&& schnittpunkt.DistanceToRayStart > MagicNumbers.MinAllowedPathPointDistance'-Abfrage, da beim Brechen an Glas der Schnittpunkt nicht mit den Glas, in das eingedrungen werdne soll, gefunden wird
                        {
                            returnList.Add(new KeyValuePair<float, IIntersectionPointSimple>(point.DistanceToRayStart, point));
                        }
                    }
                }
            }
            else
            {
                switch (this.Axi)
                {
                    case Axis.X:
                        if (ray.Direction.X == 0 && ray.Start.X > this.Left && ray.Start.X < this.Right) //Kein Knoten (Strahl fliegt durch Lücke)
                        {
                            return;
                        }
                        else
                            if (ray.Start.X < this.Right && ray.Direction.X <= 0) //Nur linker Knoten
                            {
                                this.LeftNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                            }
                            else
                                if (ray.Start.X > this.Left && ray.Direction.X >= 0) //Nur rechter Knoten
                                {
                                    this.RightNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                }
                                else //Beide Knoten
                                {
                                    if (ray.Direction.X >= 0) //Erst Links, dann Rechts
                                    {
                                        this.LeftNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                        if (rayEndpoint.X > this.Right)
                                        {
                                            this.RightNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                        }
                                    }
                                    else //Erst Rechts, dann Links
                                    {
                                        this.RightNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                        if (rayEndpoint.X < this.Left)
                                        {
                                            this.LeftNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                        }
                                    }
                                }
                        break;
                    case Axis.Y:
                        if (ray.Direction.Y == 0 && ray.Start.Y > this.Left && ray.Start.Y < this.Right) //Kein Knoten (Strahl fliegt durch Lücke)
                        {
                            return;
                        }
                        else
                            if (ray.Start.Y < this.Right && ray.Direction.Y <= 0) //Nur linker Knoten
                            {
                                this.LeftNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                            }
                            else
                                if (ray.Start.Y > this.Left && ray.Direction.Y >= 0) //Nur rechter Knoten
                                {
                                    this.RightNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                }
                                else //Beide Knoten
                                {
                                    if (ray.Direction.Y >= 0) //Erst Links, dann Rechts
                                    {
                                        this.LeftNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                        if (rayEndpoint.Y > this.Right)
                                        {
                                            this.RightNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                        }
                                    }
                                    else //Erst Rechts, dann Links
                                    {
                                        this.RightNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                        if (rayEndpoint.Y < this.Left)
                                        {
                                            this.LeftNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                        }
                                    }
                                }
                        break;
                    case Axis.Z:
                        if (ray.Direction.Z == 0 && ray.Start.Z > this.Left && ray.Start.Z < this.Right) //Kein Knoten (Strahl fliegt durch Lücke)
                        {
                            return;
                        }
                        else
                            if (ray.Start.Z < this.Right && ray.Direction.Z <= 0) //Nur linker Knoten
                            {
                                this.LeftNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                            }
                            else
                                if (ray.Start.Z > this.Left && ray.Direction.Z >= 0) //Nur rechter Knoten
                                {
                                    this.RightNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                }
                                else //Beide Knoten
                                {
                                    if (ray.Direction.Z >= 0) //Erst Links, dann Rechts
                                    {
                                        this.LeftNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                        if (rayEndpoint.Z > this.Right)
                                        {
                                            this.RightNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                        }
                                    }
                                    else //Erst Rechts, dann Links
                                    {
                                        this.RightNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                        if (rayEndpoint.Z < this.Left)
                                        {
                                            this.LeftNode.GetAllIntersectionPoints(ray, returnList, rayEndpoint, maxDistance, time);
                                        }
                                    }
                                }
                        break;
                }
            }
        }

        public IIntersectionPointSimple GetIntersectionPointWithRay(Ray ray, IIntersecableObject excludedObject, float maxDistance, float time)
        {
            IIntersectionPointSimple point = null;
            float distance = maxDistance;
            GetIntersectionPoint(ray, excludedObject, time, ref point, ref distance);
            return point;
        }

        private void GetIntersectionPoint(Ray ray, IIntersecableObject excludedObject, float time, ref IIntersectionPointSimple nearestPoint, ref float minDistance)
        {
            if (IntersectionHelper.IsAnyIntersectionPointBetweenRayAndSphere(ray, this.sphereCenter, this.sphereRadiusSqr) == false) return;

            if (this.RayObjects != null) //Blattknoten
            {
                foreach (var rayObject in this.RayObjects)
                {
                    if (rayObject != excludedObject)
                    {
                        var point = rayObject.GetSimpleIntersectionPoint(ray, time);

                        if (point != null && point.DistanceToRayStart < minDistance && point.DistanceToRayStart > 0) 
                        {
                            minDistance = point.DistanceToRayStart;
                            nearestPoint = point;
                        }
                    }
                }
            }
            else
            {
                switch (this.Axi)
                {
                    case Axis.X:
                        if (ray.Direction.X == 0 && ray.Start.X > this.Left && ray.Start.X < this.Right) //Kein Knoten (Strahl fliegt durch Lücke)
                        {
                            return;
                        }
                        else
                            if (ray.Start.X < this.Right && ray.Direction.X <= 0) //Nur linker Knoten
                            {
                                this.LeftNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                            }
                            else
                                if (ray.Start.X > this.Left && ray.Direction.X >= 0) //Nur rechter Knoten
                                {
                                    this.RightNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                }
                                else //Beide Knoten
                                {
                                    if (ray.Direction.X >= 0) //Erst Links, dann Rechts
                                    {
                                        this.LeftNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                        if (!(nearestPoint != null && nearestPoint.Position.X < this.Right))
                                        {
                                            this.RightNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                        }
                                    }
                                    else //Erst Rechts, dann Links
                                    {
                                        this.RightNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                        if (!(nearestPoint != null && nearestPoint.Position.X > this.Left))
                                        {
                                            this.LeftNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                        }
                                    }
                                }
                        break;
                    case Axis.Y:
                        if (ray.Direction.Y == 0 && ray.Start.Y > this.Left && ray.Start.Y < this.Right) //Kein Knoten (Strahl fliegt durch Lücke)
                        {
                            return;
                        }
                        else
                            if (ray.Start.Y < this.Right && ray.Direction.Y <= 0) //Nur linker Knoten
                            {
                                this.LeftNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                            }
                            else
                                if (ray.Start.Y > this.Left && ray.Direction.Y >= 0) //Nur rechter Knoten
                                {
                                    this.RightNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                }
                                else //Beide Knoten
                                {
                                    if (ray.Direction.Y >= 0) //Erst Links, dann Rechts
                                    {
                                        this.LeftNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                        if (!(nearestPoint != null && nearestPoint.Position.Y < this.Right))
                                        {
                                            this.RightNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                        }
                                    }
                                    else //Erst Rechts, dann Links
                                    {
                                        this.RightNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                        if (!(nearestPoint != null && nearestPoint.Position.Y > this.Left))
                                        {
                                            this.LeftNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                        }
                                    }
                                }
                        break;
                    case Axis.Z:
                        if (ray.Direction.Z == 0 && ray.Start.Z > this.Left && ray.Start.Z < this.Right) //Kein Knoten (Strahl fliegt durch Lücke)
                        {
                            return;
                        }
                        else
                            if (ray.Start.Z < this.Right && ray.Direction.Z <= 0) //Nur linker Knoten
                            {
                                this.LeftNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                            }
                            else
                                if (ray.Start.Z > this.Left && ray.Direction.Z >= 0) //Nur rechter Knoten
                                {
                                    this.RightNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                }
                                else //Beide Knoten
                                {
                                    if (ray.Direction.Z >= 0) //Erst Links, dann Rechts
                                    {
                                        this.LeftNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                        if (!(nearestPoint != null && nearestPoint.Position.Z < this.Right))
                                        {
                                            this.RightNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                        }
                                    }
                                    else //Erst Rechts, dann Links
                                    {
                                        this.RightNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                        if (!(nearestPoint != null && nearestPoint.Position.Z > this.Left))
                                        {
                                            this.LeftNode.GetIntersectionPoint(ray, excludedObject, time, ref nearestPoint, ref minDistance);
                                        }
                                    }
                                }
                        break;
                }
            }
        }

        #endregion

        #region Testausgabe

        public string Print()
        {
            return this.Print(0);
        }

        private string Print(int indent)
        {
            if (this.RayObjects != null)
            {
                string s = "";
                for (int i = 0; i < this.RayObjects.Count; i++)
                {
                    s += new string(' ', indent) + (i + 1) + ": " + this.RayObjects[i].MinPoint.ToString() + " | " + this.RayObjects[i].MaxPoint.ToString() + "\n";
                }
                return s;
            }
            else
            {
                string s = new string(' ', indent) + "[" + this.Axi.ToString() + " " + this.Left + " " + this.Right + "]\n";
                if (this.LeftNode != null) s += this.LeftNode.Print(indent + 2);
                if (this.RightNode != null) s += this.RightNode.Print(indent + 2);
                return s;
            }
        }

        public string GetOverlaps()
        {
            var liste = this.GetFlatList(false).Select(x => (x.Right - x.Left)).ToList();
            liste.Sort();
            return string.Join(" ", liste);
        }

        private List<BIHNode> GetFlatList(bool withLeaveNode)
        {
            List<BIHNode> retList = new List<BIHNode>();

            if (withLeaveNode || (withLeaveNode == false && this.RayObjects == null)) retList.Add(this);

            if (this.LeftNode != null) retList.AddRange(this.LeftNode.GetFlatList(withLeaveNode));
            if (this.RightNode != null) retList.AddRange(this.RightNode.GetFlatList(withLeaveNode));

            return retList;
        }

        #endregion
    }
}
