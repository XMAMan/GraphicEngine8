using System.Collections.Generic;
using System.Text;
using GraphicMinimal;
using GraphicGlobal;

namespace Rasterizer
{
    class RasterizerTriangleData
    {
        public TriangleWithNeighborReferences[] Triangles { get; private set; }
        public string Name { get; private set; }
        public Vector3D CenterPoint { get; private set; }
        public float Radius { get; private set; }


        public RasterizerTriangleData(TriangleObject triangleObject)
        {
            this.Name = triangleObject.Name;
            this.Triangles = TransformTriangleArray(triangleObject.Triangles);
            this.CenterPoint = triangleObject.CenterPoint;
            this.Radius = triangleObject.Radius;
        }

        private TriangleWithNeighborReferences[] TransformTriangleArray(Triangle[] triangles)
        {
            return new RasterizerTriangleArrayBuilder(triangles).TriangleArray;
        }

        public string Print()
        {
            StringBuilder str = new StringBuilder(this.Name + "\n");
            foreach (var t in this.Triangles)
            {
                foreach (var v in t.V)
                {
                    str.Append(v.Position + " " + v.Normal + "#");
                }
                str.Append("\n");
            }
            return str.ToString();
        }
    }

    class RasterizerTriangleArrayBuilder
    {
        private List<TriangleWithNeighborReferences> triangles = new List<TriangleWithNeighborReferences>();

        public RasterizerTriangleArrayBuilder(Triangle[] triangles)
        {
            foreach (var t in triangles)
            {
                AddTriangle(t);
            }
        }

        public TriangleWithNeighborReferences[] TriangleArray
        {
            get
            {
                return this.triangles.ToArray();
            }
        }

        private void AddTriangle(Triangle triangle)
        {
            TriangleWithNeighborReferences newTriangle = new TriangleWithNeighborReferences(triangle);

            int edgeIndex;
            newTriangle.Neighbors[0] = SearchNeighborTriangle(triangle.V[0].Position, triangle.V[1].Position, out edgeIndex); if (newTriangle.Neighbors[0] != null) newTriangle.Neighbors[0].Neighbors[edgeIndex] = newTriangle;
            newTriangle.Neighbors[1] = SearchNeighborTriangle(triangle.V[1].Position, triangle.V[2].Position, out edgeIndex); if (newTriangle.Neighbors[1] != null) newTriangle.Neighbors[1].Neighbors[edgeIndex] = newTriangle;
            newTriangle.Neighbors[2] = SearchNeighborTriangle(triangle.V[2].Position, triangle.V[0].Position, out edgeIndex); if (newTriangle.Neighbors[2] != null) newTriangle.Neighbors[2].Neighbors[edgeIndex] = newTriangle;

            this.triangles.Add(newTriangle);
        }

        //Sucht in this.triangles, ob es ein Dreieck gibt, was zwei Punkte besitzt, die P1 und P2 gleichen. Wenn ja, gibt es dieses Dreick und dessen Kanten-Indize zurück
        private TriangleWithNeighborReferences SearchNeighborTriangle(Vector3D P1, Vector3D P2, out int edgeIndex)
        {
            edgeIndex = -1;
            int J, Y;
            for (int i = 0; i < this.triangles.Count; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (this.triangles[i].V[j].Position == P1)
                    //if ((this.triangles[i].V[j].Position - P1).Length() < 0.001f)
                    {
                        J = j;
                        for (int y = 0; y < 3; y++)
                        {
                            if (j != y && this.triangles[i].V[y].Position == P2)
                            //if (j != y && (this.triangles[i].V[y].Position - P2).Length() < 0.001f)
                            {
                                Y = y;
                                if ((J == 0 && Y == 1) || (J == 1 && Y == 0)) edgeIndex = 0;
                                if ((J == 1 && Y == 2) || (J == 2 && Y == 1)) edgeIndex = 1;
                                if ((J == 2 && Y == 0) || (J == 0 && Y == 2)) edgeIndex = 2;
                                return this.triangles[i];
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
