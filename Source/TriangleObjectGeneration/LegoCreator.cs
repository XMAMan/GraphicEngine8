using GraphicGlobal;
using GraphicMinimal;
using System.Collections.Generic;

namespace TriangleObjectGeneration
{
    class LegoCreator
    {
        public TriangleObject CreateLegoObject(LegoGrid data)
        {
            List<Triangle> gridTriangles = new List<Triangle>();

            for (int x = 0; x < data.Grid.GetLength(0); x++)
                for (int y = 0; y < data.Grid.GetLength(1); y++)
                    for (int z = 0; z < data.Grid.GetLength(2); z++)
                    {
                        if ((data.Grid[x, y, z] & 63) > 0)
                        {
                            gridTriangles.AddRange(CreateCube(data.Box.Min + new Vector3D(x + 0.5f, y + 0.5f, z + 0.5f) * data.EdgeSize, data.EdgeSize, data.Grid[x, y, z]));
                        }

                    }

            return new TriangleObject(gridTriangles.ToArray(), "LegoGrid");
        }

        private List<Triangle> CreateCube(Vector3D pos, float size, byte sides)
        {
            float s = size / 2;
            List<Triangle> triangles = new List<Triangle>();

            if ((sides & 1) != 0) triangles.AddRange(CreateQuad(new Vertex(pos.X - s, pos.Y - s, pos.Z - s, 0, 1), new Vertex(pos.X - s, pos.Y - s, pos.Z + s, 1, 1), new Vertex(pos.X - s, pos.Y + s, pos.Z + s, 1, 0), new Vertex(pos.X - s, pos.Y + s, pos.Z - s, 0, 0)));//Links
            if ((sides & 2) != 0) triangles.AddRange(CreateQuad(new Vertex(pos.X + s, pos.Y - s, pos.Z + s, 0, 1), new Vertex(pos.X + s, pos.Y - s, pos.Z - s, 1, 1), new Vertex(pos.X + s, pos.Y + s, pos.Z - s, 1, 0), new Vertex(pos.X + s, pos.Y + s, pos.Z + s, 0, 0)));//Rechts
            if ((sides & 4) != 0) triangles.AddRange(CreateQuad(new Vertex(pos.X - s, pos.Y - s, pos.Z - s, 0, 1), new Vertex(pos.X + s, pos.Y - s, pos.Z - s, 1, 1), new Vertex(pos.X + s, pos.Y - s, pos.Z + s, 1, 0), new Vertex(pos.X - s, pos.Y - s, pos.Z + s, 0, 0)));//Unterseite
            if ((sides & 8) != 0) triangles.AddRange(CreateQuad(new Vertex(pos.X - s, pos.Y + s, pos.Z + s, 0, 1), new Vertex(pos.X + s, pos.Y + s, pos.Z + s, 1, 1), new Vertex(pos.X + s, pos.Y + s, pos.Z - s, 1, 0), new Vertex(pos.X - s, pos.Y + s, pos.Z - s, 0, 0)));//Oberseite
            if ((sides & 16) != 0) triangles.AddRange(CreateQuad(new Vertex(pos.X - s, pos.Y + s, pos.Z - s, 1, 0), new Vertex(pos.X + s, pos.Y + s, pos.Z - s, 0, 0), new Vertex(pos.X + s, pos.Y - s, pos.Z - s, 0, 1), new Vertex(pos.X - s, pos.Y - s, pos.Z - s, 1, 1))); //Rückseite
            if ((sides & 32) != 0) triangles.AddRange(CreateQuad(new Vertex(pos.X - s, pos.Y - s, pos.Z + s, 0, 1), new Vertex(pos.X + s, pos.Y - s, pos.Z + s, 1, 1), new Vertex(pos.X + s, pos.Y + s, pos.Z + s, 1, 0), new Vertex(pos.X - s, pos.Y + s, pos.Z + s, 0, 0))); //Vorderseite

            return triangles;
        }

        private List<Triangle> CreateQuad(Vertex p1, Vertex p2, Vertex p3, Vertex p4)
        {
            var t1 = new Triangle(p1, p2, p3);
            var t2 = new Triangle(p3, p4, p1);
            Vector3D normal = t1.Normal;

            for (int i = 0; i < 3; i++)
            {
                t1.V[i].Normal = normal;
                t2.V[i].Normal = normal;
            }

            return new List<Triangle>()
            {
                t1,
                t2
            };
        }
    }
}
