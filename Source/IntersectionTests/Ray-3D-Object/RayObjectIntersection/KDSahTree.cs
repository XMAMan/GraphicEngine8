using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using System.Diagnostics;
using GraphicGlobal;

namespace IntersectionTests
{
    public class KdBaumEntry
    {
        public IIntersecableObject Obj;
        public string Location;
    }

    //Erklärung für die KD-SAH-Konstruktion: http://dcgi.felk.cvut.cz/home/havran/ARTICLES/ingo06rtKdtree.pdf
    //Erklärung für das traversieren: http://www.sci.utah.edu/~wald/PhD/wald_phd.pdf -> Seite 121

    //Quelle: http://www.socher.org/uploads/Main/RenderingCompetitionSocherGranados/src/Kdtree.h
    //        http://www.socher.org/uploads/Main/RenderingCompetitionSocherGranados/src/SAHKdtree.h
    public class KDSahTree : IRayObjectIntersection
    {
        public KDSahTree(List<IIntersecableObject> rayObjects, Action<string, float> progressChanged)
        {
            if (progressChanged != null) progressChanged("Baue Kd-Baum", 0);

            this.KT = 1.0f;
            this.KI = 1.5f;            
            this.maxdepth = 0;
            this.nnodes = 0;
            this.KmaxDepth = 24;
            this.KtriTarget = 3;

            if (rayObjects.Any())
            {
                this.topBox = IntersectionHelper.GetBoundingBoxFromIVolumeObjektCollection(rayObjects);
                this.root = RecBuild(rayObjects, topBox, 0, new SplitPlane());  // Mit SAH
                //this.root = RecBuild(rayObjects, topBox, 0);                  // Ohne SAH
            }

            if (progressChanged != null) progressChanged("Baue Kd-Baum", 100);
        }

        public IEnumerable<KdBaumEntry> VisitEachLeafeItem() //Zur Fehleranalyse
        { 
            return root.VisitEachLeafeItem("root");
        }

        public IIntersectionPointSimple GetIntersectionPoint(Ray ray, IIntersecableObject excludedObject, float maxDistance, float time)
        {
            if (this.root == null) return null;
            //return GetIntersectionPointRayTreeIterativ(ray, excludedObject, maxDistance, time);
            return GetIntersectionPointRayTreeRekursiv(ray, excludedObject, maxDistance, time);
        }

        private IIntersectionPointSimple GetIntersectionPointRayTreeRekursiv(Ray ray, IIntersecableObject excludedObject, float maxDistance, float time)
        {
            IntersectionPoint point = new IntersectionPoint();
            float tNear, tFar;
            
            if (this.topBox.ClipRayWithBoundingBox(ray, out tNear, out tFar) == false) return null;
            tFar += 0.001f; //XMAMan-Bugfix: Vermeide den Streuselfehler wenn das Dreieck genau auf der Splitebene liegt; Siehe ToolsTest.CreateSceneBatFiles.08_WindowRoom wenn man diese Zeile hier nicht drin hat
            
            //tNear = 0; tFar = maxDistance; //XMAMan-Bugfix: Ohne Clipping: 30 Sekunden; Mit Clipping: 31 Sekunden
            
            if (maxDistance < float.MaxValue) point.Distance = maxDistance;
            this.root.Traverse(ray, time, excludedObject, tNear, tFar, point);
            return point.Point;
        }

        private IIntersectionPointSimple GetIntersectionPointRayTreeIterativ(Ray ray, IIntersecableObject excludedObject, float maxDistance, float time)
        {
            IntersectionPoint point = new IntersectionPoint();
            if (maxDistance < float.MaxValue) point.Distance = maxDistance;
            float t_min, t_max;
            if (this.topBox.ClipRayWithBoundingBox(ray, out t_min, out t_max) == false) return null;
            t_max += 0.001f; //XMAMan-Bugfix: Vermeide den Streuselfehler wenn das Dreieck genau auf der Splitebene liegt; Siehe ToolsTest.CreateSceneBatFiles.08_WindowRoom wenn man diese Zeile hier nicht drin hat

            INode node = this.root;
            KdStack stack = new KdStack();

            while (true)
            {
                // traverse ’til next leaf
                while (node is InnerNode)
                {
                    SplitPlane p = (node as InnerNode).p;

                    float t_split = (p.SplitValue - ray.Start[p.SplitDimensison]) * (ray.Direction[p.SplitDimensison] == 0 ? float.MaxValue : 1.0f / ray.Direction[p.SplitDimensison]);

                    bool isOnEgeFromRightSideAndLookingRight = false; //XMAMan-Bugfix: Edge-Case Ray-Start steht auf der Split-Ebene


                    // near is the side containing the origin of the ray
                    INode near, far;
                    if (ray.Start[p.SplitDimensison] < p.SplitValue)
                    {
                        near = (node as InnerNode).leftChild;
                        far = (node as InnerNode).rightChild;
                    }
                    else
                    {
                        near = (node as InnerNode).rightChild;
                        far = (node as InnerNode).leftChild;

                        if (t_split == 0 && ray.Direction[p.SplitDimensison] >= 0) isOnEgeFromRightSideAndLookingRight = true; //Stehe ich am linken Rand von der RightSide und schaue nach Rechts? Dann brauche ich nur die Right=Near-Side betrachten
                    }

                    if (t_split > t_max || t_split < 0 || isOnEgeFromRightSideAndLookingRight)
                    {
                        node = near;
                    }
                    else if (t_split < t_min)
                    {
                        node = far;
                    }
                    else
                    {
                        stack.Push(far, t_split, t_max);

                        node = near;
                        t_max = t_split;
                    }
                }

                // have a leaf now
                (node as LeafNode).Traverse(ray, time, excludedObject, t_min, t_max, point);

                //if (t_max <= point.Distance) point.Point; //early ray termination (Ingo Wald)
                if (point.Distance <= t_max) return point.Point; //early ray termination (XMAMan)

                if (stack.IsEmpty()) return point.Point; // noting else to traverse any more...
                node = stack.Pop(out t_min, out t_max);
            }
        }

        class KdStackFrame
        {
            public float T_Max;
            public float T_Min;
            public INode Node;
        }

        class KdStack
        {
            private Stack<KdStackFrame> stack = new Stack<KdStackFrame>();

            public void Push(INode node, float t_min, float t_max)
            {
                this.stack.Push(new KdStackFrame() { Node = node, T_Min = t_min, T_Max = t_max });
            }

            public INode Pop(out float t_min, out float t_max)
            {
                var element = this.stack.Pop();
                t_min = element.T_Min;
                t_max = element.T_Max;
                return element.Node;
            }

            public bool IsEmpty()
            {
                return this.stack.Any() == false;
            }
        }

        public List<IIntersectionPointSimple> GetAllIntersectionPoints(Ray ray, float maxDistance, float time)
        {
            if (this.root == null) return new List<IIntersectionPointSimple>();
            List<KeyValuePair<float, IIntersectionPointSimple>> returnList = new List<KeyValuePair<float, IIntersectionPointSimple>>();
            float tNear, tFar;
            if (this.topBox.ClipRayWithBoundingBox(ray, out tNear, out tFar) == false) return new List<IIntersectionPointSimple>();
            tNear -= 0.001f;
            tFar = Math.Min(tFar + 0.001f, maxDistance);
            this.root.GetAllIntersectionPoints(ray, time, tNear, tFar, returnList);

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

        struct SplitPlane
        {
            public short SplitDimensison;	// splitting dimension		
            public float SplitValue;	// splitting point

            public SplitPlane(short pk0 = -1, float pe0 = 0)
            {
                SplitDimensison = pk0;
                SplitValue = pe0;
            }

            public static bool operator ==(SplitPlane sp1, SplitPlane sp2)
            {
                return (sp1.SplitDimensison == sp2.SplitDimensison && sp1.SplitValue == sp2.SplitValue);
            }

            public static bool operator !=(SplitPlane sp1, SplitPlane sp2)
            {
                return (sp1.SplitDimensison != sp2.SplitDimensison || sp1.SplitValue != sp2.SplitValue);
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        };

        class IntersectionPoint
        {
            public IIntersectionPointSimple Point = null;
            public float Distance = float.MaxValue;
        }

        interface INode
        {
            IEnumerable<KdBaumEntry> VisitEachLeafeItem(string path); //Zur Fehleranalyse
            void Traverse(Ray ray, float time, IIntersecableObject excludedObject, float t_min, float t_max, IntersectionPoint point); //Note: the result is stored in ray! (in ray.t and ray.hit)
            void GetAllIntersectionPoints(Ray ray, float time, float t_min, float t_max, List<KeyValuePair<float, IIntersectionPointSimple>> returnList);
            int Depth(int d);
        };

        class InnerNode : INode
        {
            public SplitPlane p;
            public INode leftChild, rightChild;

            public InnerNode(SplitPlane p0, INode lc, INode rc)
            {
                this.p = p0;
                this.leftChild = lc;
                this.rightChild = rc;
            }

            public IEnumerable<KdBaumEntry> VisitEachLeafeItem(string path)
            {
                List<KdBaumEntry> list = new List<KdBaumEntry>();
                list.AddRange(this.leftChild.VisitEachLeafeItem(path + " " + "left"));
                list.AddRange(this.rightChild.VisitEachLeafeItem(path + " " + "right"));
                return list;
            }

            //Siehe "Das traversieren beim KD-Baum" in Dokumentation.odt um zu verstehen was hier passiert
            public void Traverse(Ray ray, float time, IIntersecableObject excludedObject, float t_min, float t_max, IntersectionPoint point)
            {	
                float t_split = (p.SplitValue - ray.Start[p.SplitDimensison]) * (ray.Direction[p.SplitDimensison] == 0 ? float.MaxValue : 1.0f / ray.Direction[p.SplitDimensison]);

                bool isOnEgeFromRightSideAndLookingRight = false; //XMAMan-Bugfix: Edge-Case Ray-Start steht auf der Split-Ebene

                // near is the side containing the origin of the ray
                INode near, far;
                if (ray.Start[p.SplitDimensison] < p.SplitValue)
                {
                    near = leftChild;
                    far = rightChild;
                }
                else
                {
                    near = rightChild;
                    far = leftChild;

                    if (t_split == 0 && ray.Direction[p.SplitDimensison] >= 0) isOnEgeFromRightSideAndLookingRight = true; //Stehe ich am linken Rand von der RightSide und schaue nach Rechts? Dann brauche ich nur die Right=Near-Side betrachten
                }

                if (t_split > t_max || t_split < 0 || isOnEgeFromRightSideAndLookingRight)
                {
                    near.Traverse(ray, time, excludedObject, t_min, t_max, point);
                }
                else if (t_split < t_min) 
                {
                    far.Traverse(ray, time, excludedObject, t_min, t_max, point);
                }
                else
                {
                    near.Traverse(ray, time, excludedObject, t_min, t_split, point);
                    if (point.Distance < t_split)
                        return;
                    far.Traverse(ray, time, excludedObject, t_split, t_max, point);
                }
            }

            public void GetAllIntersectionPoints(Ray ray, float time, float t_min, float t_max, List<KeyValuePair<float, IIntersectionPointSimple>> returnList)
            {
                float t_split = (p.SplitValue - ray.Start[p.SplitDimensison]) * (ray.Direction[p.SplitDimensison] == 0 ? float.MaxValue : 1.0f / ray.Direction[p.SplitDimensison]);

                // near is the side containing the origin of the ray
                INode near, far;
                if (ray.Start[p.SplitDimensison] < p.SplitValue)
                {
                    near = leftChild;
                    far = rightChild;
                }
                else
                {
                    near = rightChild;
                    far = leftChild;
                }

                if (t_split > t_max || t_split < 0)
                {
                    near.GetAllIntersectionPoints(ray, time, t_min, t_max, returnList);
                }
                else if (t_split < t_min && t_split != 0)
                {
                    far.GetAllIntersectionPoints(ray, time, t_min, t_max, returnList);
                }
                else
                {
                    near.GetAllIntersectionPoints(ray, time, t_min, t_split, returnList);
                    far.GetAllIntersectionPoints(ray, time, t_split, t_max, returnList);
                }
            }

            public int Depth(int d)
            {
                return Math.Max(leftChild.Depth(d + 1), rightChild.Depth(d + 1));
            }
        };

        class LeafNode : INode
        {
            List<IIntersecableObject> T;
            //Box V;

            public LeafNode(List<IIntersecableObject> T0/*, Box V0*/)
            {
                this.T = T0;
                //V = V0;
            }

            public IEnumerable<KdBaumEntry> VisitEachLeafeItem(string path)
            {
                for (int i=0;i<this.T.Count;i++)
                {                    
                    yield return new KdBaumEntry() { Obj = this.T[i], Location = path + " Index=" + i };
                }
            }

            public void Traverse(Ray ray, float time, IIntersecableObject excludedObject, float t_min, float t_max, IntersectionPoint point)
            {
                foreach (var t in this.T)
                {
                    if (t != excludedObject)
                    {
                        var s = t.GetSimpleIntersectionPoint(ray, time);
                        if (s != null && s.DistanceToRayStart < point.Distance)
                        {
                            point.Point = s;
                            point.Distance = s.DistanceToRayStart;
                        }
                    }
                }
            }

            public void GetAllIntersectionPoints(Ray ray, float time, float t_min, float t_max, List<KeyValuePair<float, IIntersectionPointSimple>> returnList)
            {
                foreach (var t in this.T)
                {
                    var points = t.GetAllIntersectionPoints(ray, time);
                    foreach (var point in points)
                    {
                        if (point.DistanceToRayStart <= t_max) //Hier an dieser Stelle steht keine '&& schnittpunkt.DistanceToRayStart > MagicNumbers.MinAllowedPathPointDistance'-Abfrage, da beim Brechen an Glas der Schnittpunkt nicht mit den Glas, in das eingedrungen werdne soll, gefunden wird
                        {
                            returnList.Add(new KeyValuePair<float, IIntersectionPointSimple>(point.DistanceToRayStart, point));
                        }
                    }
                }
            }

            public int Depth(int d)
            {
                return d;
            }
        };

        private INode root = null;
        private int KmaxDepth, KtriTarget;
        private BoundingBox topBox;

        bool terminate(List<IIntersecableObject> T, int depth)
        {
            return (T.Count <= KtriTarget || depth >= KmaxDepth);
        }

        SplitPlane findPlane(List<IIntersecableObject> T, BoundingBox V, int depth)
        {
            int pk = depth % 3;
            float pe = (V.Min[pk] + V.Max[pk]) / 2;
            return new SplitPlane((short)pk, pe);
        }

        void splitVoxelWithPlane(BoundingBox V, SplitPlane p, ref BoundingBox VL, ref BoundingBox VR)
        {
            //VL = VR = V;
            VL = new BoundingBox(new Vector3D(V.Min), new Vector3D(V.Max));
            VR = new BoundingBox(new Vector3D(V.Min), new Vector3D(V.Max));
            VL.Max[p.SplitDimensison] = VR.Min[p.SplitDimensison] = p.SplitValue;
        }

        void splitTrianglesIntoVoxels(List<IIntersecableObject> T, SplitPlane p, List<IIntersecableObject> TL, List<IIntersecableObject> TR)
        {
            foreach (var t in T)
            {
                if (t.MinPoint[p.SplitDimensison] <= p.SplitValue)
                    TL.Add(t);
                if (t.MaxPoint[p.SplitDimensison] >= p.SplitValue)
                    TR.Add(t);
            }
        }

        INode RecBuild(List<IIntersecableObject> T, BoundingBox V, int depth)
        {
            if (terminate(T, depth))
            {
                return new LeafNode(T/*, V*/);
            }
            SplitPlane p = findPlane(T, V, depth);
            BoundingBox VL = null, VR = null;
            splitVoxelWithPlane(V, p, ref VL, ref VR);
            List<IIntersecableObject> TL = new List<IIntersecableObject>(), TR = new List<IIntersecableObject>();
            splitTrianglesIntoVoxels(T, p, TL, TR);
            return new InnerNode(p, /*V,*/ RecBuild(TL, VL, depth + 1), RecBuild(TR, VR, depth + 1));
        }

        // traversal cost
        float KT;

        // triangle intersection cost
        float KI;

        // surface area of a voxel V
        float SA(BoundingBox V)
        {
            return 2 * V.XSize * V.YSize + 2 * V.XSize * V.ZSize + 2 * V.YSize * V.ZSize;
        }

        // probability of hitting the subvoxel Vsub given that the voxel V was hit
        float P_Vsub_given_V(BoundingBox Vsub, BoundingBox V)
        {
            float SA_Vsub = SA(Vsub);
            float SA_V = SA(V);
            return (SA_Vsub / SA_V);
        }

        // bias for the cost function s.t. it is reduced if NL or NR becomes zero
        float lambda(int NL, int NR, float PL, float PR)
        {
            if ((NL == 0 || NR == 0) &&
                !(PL == 1 || PR == 1) // NOT IN PAPER
            )
                return 0.8f;
            return 1.0f;
        }

        // cost C of a complete tree approximated using the cost CV of subdividing the voxel V with a plane p
        float C(float PL, float PR, int NL, int NR)
        {
            return (lambda(NL, NR, PL, PR) * (KT + KI * (PL * NL + PR * NR)));
        }

        // split a voxel V using a plane p
        void splitBox(BoundingBox V, SplitPlane p, out BoundingBox VL, out BoundingBox VR)
        {
            VL = new BoundingBox(new Vector3D(V.Min), new Vector3D(V.Max));
            VR = new BoundingBox(new Vector3D(V.Min), new Vector3D(V.Max));
            VL.Max[p.SplitDimensison] = p.SplitValue;
            VR.Min[p.SplitDimensison] = p.SplitValue;
            Debug.Assert(V.Contains(VL));
            Debug.Assert(V.Contains(VR));
        }

        enum PlaneSide { LEFT = -1, RIGHT = 1, UNKNOWN = 0 }

        // SAH heuristic for computing the cost of splitting a voxel V using a plane p
        void SAH(SplitPlane p, BoundingBox V, int NL, int NR, int NP, ref float CP, ref PlaneSide pside)
        {
            CP = float.MaxValue;
            BoundingBox VL, VR;
            splitBox(V, p, out VL, out VR);
            float PL, PR;
            PL = P_Vsub_given_V(VL, V);
            PR = P_Vsub_given_V(VR, V);
            if (PL == 0 || PR == 0) // NOT IN PAPER
                return;
            if ((V.Max - V.Min)[p.SplitDimensison] == 0) // NOT IN PAPER
                return;
            float CPL, CPR;
            CPL = C(PL, PR, NL + NP, NR);
            CPR = C(PL, PR, NL, NP + NR);
            if (CPL < CPR)
            {
                CP = CPL;
                pside = PlaneSide.LEFT;
            }
            else
            {
                CP = CPR;
                pside = PlaneSide.RIGHT;
            }
        }

        // criterion for stopping subdividing a tree node
        bool terminate(int N, float minCv)
        {
            return (minCv > KI * N);
        }

        struct Event : IComparable
        {
            public enum EventType { endingOnPlane = 0, lyingOnPlane = 1, startingOnPlane = 2 };
            public IIntersecableObject et;	// triangle
            public SplitPlane p;
            public EventType type;

            public Event(IIntersecableObject et0, int k, float ee0, EventType type0)
            {
                this.et = et0;
                this.type = type0;
                Debug.Assert(type == EventType.endingOnPlane || type == EventType.lyingOnPlane || type == EventType.startingOnPlane);
                p = new SplitPlane((short)k, ee0);
            }

            public static bool operator <(Event e1, Event e2)
            {
                return ((e1.p.SplitValue < e2.p.SplitValue) || (e1.p.SplitValue == e2.p.SplitValue && e1.type < e2.type));
            }

            public static bool operator >(Event e1, Event e2)
            {
                return ((e1.p.SplitValue > e2.p.SplitValue) || (e1.p.SplitValue == e2.p.SplitValue && e1.type > e2.type));
            }

            public int CompareTo(object obj)
            {
                if (obj == null) return 1;

                Event e1 = this;
                Event e2 = (Event)obj;
                if (e1 < e2) return -1;
                if (e1 > e2) return +1;
                return 0;
            }
        };

        // get primitives's clipped bounding box
        BoundingBox clipTriangleToBox(IIntersecableObject t, BoundingBox V)
        {
            BoundingBox b = new BoundingBox(new Vector3D(t.MinPoint), new Vector3D(t.MaxPoint));

            for (int k = 0; k < 3; k++)
            {
                if (V.Min[k] > b.Min[k])
                    b.Min[k] = V.Min[k];
                if (V.Max[k] < b.Max[k])
                    b.Max[k] = V.Max[k];
            }
            Debug.Assert(V.Contains(b));
            return b;
        }

        // best spliting plane using SAH heuristic
        void findPlane(List<IIntersecableObject> T, BoundingBox V, int depth, ref SplitPlane p_est, ref float C_est, ref PlaneSide pside_est)
        {
            // static int count = 0;
            C_est = float.MaxValue;
            for (int k = 0; k < 3; ++k)
            {
                List<Event> events = new List<Event>(T.Count * 2);
                foreach (var t in T)
                {
                    BoundingBox B = clipTriangleToBox(t, V);
                    if (Math.Abs((B.Max - B.Min).Min()) < float.Epsilon)
                    {
                        events.Add(new Event(t, k, B.Min[k], Event.EventType.lyingOnPlane));
                    }
                    else
                    {
                        events.Add(new Event(t, k, B.Min[k], Event.EventType.startingOnPlane));
                        events.Add(new Event(t, k, B.Max[k], Event.EventType.endingOnPlane));
                    }
                }
                events.Sort();
                int NL = 0, NP = 0, NR = T.Count;
                for (int Ei = 0; Ei < events.Count; ++Ei)
                {
                    SplitPlane p = events[Ei].p;
                    int pLyingOnPlane = 0, pStartingOnPlane = 0, pEndingOnPlane = 0;
                    while (Ei < events.Count && events[Ei].p.SplitValue == p.SplitValue && events[Ei].type == Event.EventType.endingOnPlane)
                    {
                        ++pEndingOnPlane;
                        Ei++;
                    }
                    while (Ei < events.Count && events[Ei].p.SplitValue == p.SplitValue && events[Ei].type == Event.EventType.lyingOnPlane)
                    {
                        ++pLyingOnPlane;
                        Ei++;
                    }
                    while (Ei < events.Count && events[Ei].p.SplitValue == p.SplitValue && events[Ei].type == Event.EventType.startingOnPlane)
                    {
                        ++pStartingOnPlane;
                        Ei++;
                    }
                    NP = pLyingOnPlane;
                    NR -= pLyingOnPlane;
                    NR -= pEndingOnPlane;
                    float C = float.MaxValue;
                    PlaneSide pside = PlaneSide.UNKNOWN;
                    SAH(p, V, NL, NR, NP, ref C, ref pside);

                    if (C < C_est)
                    {
                        C_est = C;
                        p_est = p;
                        pside_est = pside;
                    }
                    NL += pStartingOnPlane;
                    NL += pLyingOnPlane;
                    NP = 0;
                }
            }
        }

        // sort triangles into left and right voxels
        void splitTrianglesIntoVoxels(List<IIntersecableObject> T, SplitPlane p, PlaneSide pside, List<IIntersecableObject> TL, List<IIntersecableObject> TR)
        {
            foreach (var t in T)
            {
                BoundingBox tbox = new BoundingBox(new Vector3D(t.MinPoint), new Vector3D(t.MaxPoint));
                if (tbox.Min[p.SplitDimensison] == p.SplitValue && tbox.Max[p.SplitDimensison] == p.SplitValue)
                {
                    //Auskommentiert von XMAMan
                    //if (pside == PlaneSide.LEFT)
                    //    TL.Add(t);
                    //else if (pside == PlaneSide.RIGHT)
                    //    TR.Add(t);
                    //else
                    //    Debug.Assert(false); // wrong pside

                    //Frage: Welche Bedeutung hat der Parameter pside überhaupt?
                    TR.Add(t); //XMAMan-Bugfix: Liegt das Objekt komplett auf der Splitebene, dann muss es immer rechts einsortiert
                               //werden, da beim traversieren davon ausgegangen wird, dass alle Objekte, die auf der Splitebene
                               //liegen aber nicht links daneben nur auf der rechten Seite zu finden sind
                }
                else
                {
                    if (tbox.Min[p.SplitDimensison] < p.SplitValue)
                        TL.Add(t);
                    if (tbox.Max[p.SplitDimensison] >= p.SplitValue)
                        TR.Add(t);
                }
            }
        }

        private static bool IsZero(float f)
        {
            return Math.Abs(f) < 0.001f;
        }

        int maxdepth; // DEBUG ONLY
        int nnodes; // DEBUG ONLY

        INode RecBuild(List<IIntersecableObject> T, BoundingBox V, int depth, SplitPlane prev_p)
        {
            Debug.Assert(depth < 100); // just as a protection for when the stopping criterion fails

            ++nnodes; // DEBUG ONLY
            if (depth > maxdepth) maxdepth = depth; // DEBUG ONLY

            SplitPlane p = new SplitPlane();
            float Cp = 0;
            PlaneSide pside = PlaneSide.UNKNOWN;
            findPlane(T, V, depth, ref p, ref Cp, ref pside);
            if (terminate(T.Count, Cp)
                || p == prev_p) // NOT IN PAPER
            {
                return new LeafNode(T/*, V*/);
            }
            BoundingBox VL, VR;
            splitBox(V, p, out VL, out VR); // TODO: avoid doing this step twice
            List<IIntersecableObject> TL = new List<IIntersecableObject>(), TR = new List<IIntersecableObject>();
            splitTrianglesIntoVoxels(T, p, pside, TL, TR);
            return new InnerNode(p, /*V,*/ RecBuild(TL, VL, depth + 1, p), RecBuild(TR, VR, depth + 1, p));
        }

        
        
    }
}
