using System.Collections.Generic;
using System.Linq;
using TriangleObjectGeneration;
using GraphicMinimal;
using GraphicGlobal;
using TextureHelper.TexturMapping;

namespace GraphicPanels
{
    public class DrawingObjectContainer
    {
        private Dictionary<int, DrawingObject> drawingData = new Dictionary<int, DrawingObject>();

        //Add / Remove

        // Gibt die Objekt-ID des erzeugten Objektes zurück (Ein Objekt besteht aus mehreren Dreiecken)
        public int AddObject(DrawingObject data)   
        {
            int id = GenerateId();
            data.DrawingProps.Id = id;
            this.drawingData.Add(id, data);            
            return id;
        }

        private int GenerateId()
        {
            return this.drawingData.Keys.DefaultIfEmpty().Max() + 1;
        }

        // Entfernt das Objekt -> Exception, wenn Objekt-ID nicht gefunden
        public void RemoveObject(int objId)
        {
            this.drawingData.Remove(objId);
        }

        // Entferne alle Objekte
        public void RemoveAllObjects()
        {
            this.drawingData.Clear();
        }

        //Alle Objekt-IDs abfragen
        public List<int> GetAllObjectIds()
        {
            return this.drawingData.Keys.ToList();
        }

        //Alle Objekte abfragen
        public List<DrawingObject> GetAllObjects()
        {
            return this.drawingData.Values.ToList();
        }

        public DrawingObject GetObjectById(int id)
        {
            return this.drawingData[id];
        }

        //Umwandlung
        public int TransformToWireObject(int sourceObjId, float wireWidth)
        {
            var newObj = new DrawingObject(TriangleObjectGenerator.GetWireObject(this.drawingData[sourceObjId].TriangleData, wireWidth), this.drawingData[sourceObjId].DrawingProps);
            RemoveObject(sourceObjId);
            return AddObject(newObj);
        }

        public int FlipNormals(int objID)
        {
            var newObj = new DrawingObject(TriangleObjectGenerator.GetFlippedNormalsObjectFromOtherObject(this.drawingData[objID].TriangleData), this.drawingData[objID].DrawingProps);
            RemoveObject(objID);
            return AddObject(newObj);

        }

        public int MergeTwoObjects(int objId1, int objId2) //Gibt die Id von den neu erstellen Objekt zurück
        {
            Vector3D positionFromMergedObject;
            TriangleObject newObjekt = TriangleObjectGenerator.MergeTwoObjects(this.drawingData[objId1].GetTrianglesInWorldSpace(), this.drawingData[objId2].GetTrianglesInWorldSpace(), out positionFromMergedObject, "MergeTwoObjects(" + this.drawingData[objId1].DrawingProps.Name + "," + this.drawingData[objId2].DrawingProps.Name+")");
            int newId = AddObject(new DrawingObject(newObjekt, this.drawingData[objId1].DrawingProps));
            this.drawingData[newId].DrawingProps.Position = positionFromMergedObject;
            this.drawingData[newId].DrawingProps.Size = 1;
            RemoveObject(objId1);
            RemoveObject(objId2);
            return newId;
        }

        public void SetCenterOfObjectOrigin(int objId, Vector3D worldPosition)
        {
            DrawingObject drawingObject = this.drawingData[objId];

            Vector3D translateWorld = worldPosition - drawingObject.DrawingProps.Position;
            Matrix4x4 worldToObj = Matrix4x4.InverseModel(drawingObject.DrawingProps.Position, drawingObject.DrawingProps.Orientation, drawingObject.DrawingProps.Size);
            Vector3D translateObj = Matrix4x4.MultDirection(worldToObj, translateWorld);
            drawingObject.MoveTrianglePoints(-translateObj);
        }


        //Zusammenspiel mit Physikengine
        public BoundingBox GetBoundingBoxFromObject(int objId)
        {
            return this.drawingData[objId].GetBoundingBoxFromObject();
        }

        //centerPosition = An dieser World-Position wird das neue Objekt erzeugt
        public void TransformToCubemappedObject(int objId, Vector3D centerPosition)
        {
            var triangles = this.drawingData[objId].GetTrianglesInWorldSpace();

            var cubemapping = new CubeMapping(centerPosition);

            List<Triangle> newTriangles = new List<Triangle>();
            foreach (var t in triangles)
            {
                var newTextCoords = t.V.Select(x => cubemapping.Map(Vector3D.Normalize(x.Position - centerPosition))).ToArray();
                newTriangles.Add(new Triangle(
                    new Vertex(t.V[0].Position - centerPosition, t.V[0].Normal, null, newTextCoords[0].X, newTextCoords[0].Y),
                    new Vertex(t.V[1].Position - centerPosition, t.V[1].Normal, null, newTextCoords[1].X, newTextCoords[1].Y),
                    new Vertex(t.V[2].Position - centerPosition, t.V[2].Normal, null, newTextCoords[2].X, newTextCoords[2].Y)
                    ));
            }

            var newObj = new ObjectPropertys(this.drawingData[objId].DrawingProps) { Position = centerPosition, Size = 1, Orientation = new Vector3D(0, 0, 0) };
            this.drawingData[objId] = new DrawingObject(new TriangleObject(newTriangles.ToArray(), this.drawingData[objId].DrawingProps.Name + "_Cubmapped"), newObj);
        }

        public int GetObjectId(DrawingObject obj)
        {
            foreach (KeyValuePair<int, DrawingObject> pair in this.drawingData)
                if (obj.Equals(pair.Value)) return pair.Key;

            return -1;
        }

        public string ExportToWavefront()
        {
            return DrawingObject.GetWavefrontDataFromDrawingObjectList(GetAllObjects());
        }
    }
}
