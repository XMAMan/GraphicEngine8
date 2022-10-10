using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphicMinimal;

namespace GraphicGlobal
{
    //Beschreibt die Form und Farbe aus Engine-Nutzersicht
    public class DrawingObject
    {
        public TriangleObject TriangleData { get; private set; }
        public ObjectPropertys DrawingProps { get; set; }

        public DrawingObject(TriangleObject triangleData, ObjectPropertys drawingProps)
        {
            this.TriangleData = triangleData;
            this.DrawingProps = drawingProps;
            this.DrawingProps.Name = triangleData.Name;
        }

        //Kopierkonstruktor
        public DrawingObject(DrawingObject copy)
            :this(copy.TriangleData, new ObjectPropertys(copy.DrawingProps))
        {           
        }

        public void MoveTriangleDataToCenterPoint()
        {
            Vector3D center = this.TriangleData.CenterPoint;
            if (center.X != 0 || center.Y != 0 || center.Z != 0)
            {
                this.TriangleData.MoveTrianglePoints(-center);
                this.DrawingProps.Position += center * this.DrawingProps.Size;
            }
        }

        public void MoveTrianglePoints(Vector3D tranlateInObjectSpace)
        {
            this.TriangleData.MoveTrianglePoints(tranlateInObjectSpace);
            this.DrawingProps.Position -= tranlateInObjectSpace * this.DrawingProps.Size;
        }

        public List<Triangle> GetTrianglesInWorldSpace()
        {
            Matrix4x4 objToWorld = Matrix4x4.Ident();
            
            if (this.DrawingProps.HasBillboardEffect)
            {
                objToWorld = Matrix4x4.BilboardMatrixFromCameraMatrix(this.DrawingProps.Position, this.DrawingProps.Orientation, this.DrawingProps.Size, objToWorld) * objToWorld;
            }
            else
            {
                objToWorld = Matrix4x4.Model(this.DrawingProps.Position, this.DrawingProps.Orientation, this.DrawingProps.Size) * objToWorld;
            }

            return TriangleHelper.TransformTrianglesWithMatrix(objToWorld, this.TriangleData.Triangles);
        }

        public BoundingBox GetBoundingBoxFromObject()
        {
            var vertices = GetTrianglesInWorldSpace().SelectMany(x => x.V).Select(x => x.Position);
            Vector3D min = new Vector3D(vertices.Min(x => x.X),
                                    vertices.Min(x => x.Y),
                                    vertices.Min(x => x.Z));

            Vector3D max = new Vector3D(vertices.Max(x => x.X),
                                    vertices.Max(x => x.Y),
                                    vertices.Max(x => x.Z));
            return new BoundingBox(min, max);
        }

        public static string GetWavefrontDataFromDrawingObjectList(List<DrawingObject> drawingObjects)
        {
            StringBuilder str = new StringBuilder();
            str.Append("# GraphicEngine8 " + DateTime.Now + System.Environment.NewLine);
            uint indexListStart = 0;
            foreach (var obj in drawingObjects)
            {
                str.Append(obj.GetWavefrontData(ref indexListStart));
            }
            return str.ToString();
        }

        //Exportiert die Dreiecke in Weltkoordinaten ins Wavefront-Format (.obj)
        public string GetWavefrontData(ref uint indexListStart)
        {
            StringBuilder str = new StringBuilder();
            str.Append("o " + this.DrawingProps.Name + " (id=" + this.DrawingProps.Id+")" + System.Environment.NewLine);

            List<Vertex> vertexList;
            List<uint> indexList;
            TriangleHelper.TransformTriangleListToVertexIndexList(GetTrianglesInWorldSpace().ToArray(), out vertexList, out indexList);
            foreach (var v in vertexList)
            {
                str.Append("v " + v.Position.X + " " + v.Position.Y + " " + v.Position.Z + System.Environment.NewLine);
            }
            foreach (var v in vertexList)
            {
                str.Append("vt " + (v.TexcoordU) + " " + (1 - v.TexcoordV) + System.Environment.NewLine);
            }
            str.Append("s off" + System.Environment.NewLine);
            for (int i=0;i<indexList.Count;i+=3)
            {
                uint i1 = indexList[i + 0] + 1 + indexListStart;
                uint i2 = indexList[i + 1] + 1 + indexListStart;
                uint i3 = indexList[i + 2] + 1 + indexListStart;
                str.Append("f " + i1 + "/" + i1 + " " + i2 + "/" + i2 + " " + i3 + "/" + i3 + System.Environment.NewLine);
            }
            indexListStart += (uint)vertexList.Count;
            return str.ToString().Replace(',', '.');
        }

        public override string ToString()
        {
            return this.DrawingProps.Name;
        }
    }
}
