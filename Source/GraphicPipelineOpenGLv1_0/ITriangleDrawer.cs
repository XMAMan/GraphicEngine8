using GraphicGlobal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tao.OpenGl;

namespace GraphicPipelineOpenGLv1_0
{
    interface ITriangleDrawer
    {
        int GetTriangleArrayId(Triangle[] data);
        void DrawTriangleArray(int triangleArrayId);
        void RemoveTriangleArray(int triangleArrayId);        
        bool VBOsAreUsed { get; }
    }

    class TriangleDrawerOldWay : ITriangleDrawer
    {
        private readonly ShaderHelper shader;
        private readonly Dictionary<int, Triangle[]> triangleArrays = new Dictionary<int, Triangle[]>(); //TriangleArray-ID | Daten
        private readonly float currentNormalScaleFaktor = 1;

        public TriangleDrawerOldWay(ShaderHelper shader)
        {
            this.shader = shader;
        }

        public bool VBOsAreUsed { get; } = false;

        //Alter Weg (hier geht der Geometry-Shader nicht. Deswegen geht kein Explosionseffekt)
        public int GetTriangleArrayId(Triangle[] data)
        {
            int triangleArrayID = 1;
            if (triangleArrays.Count > 0) triangleArrayID = triangleArrays.Keys.Max() + 1;

            triangleArrays.Add(triangleArrayID, data);

            return triangleArrayID;
        }

        //Alter Weg (hier geht der Geometry-Shader nicht. Deswegen geht kein Explosionseffekt)
        public void DrawTriangleArray(int triangleArrayId)
        {
            //Gl.glEnable(Gl.GL_NORMALIZE);

            for (int i = 0; i < triangleArrays[triangleArrayId].Count(); i++)
            {
                Triangle T = triangleArrays[triangleArrayId][i];
                Gl.glBegin(Gl.GL_TRIANGLES);
                //Gl.glPointSize(5);	Gl.glBegin(Gl.GL_POINTS);
                for (int j = 0; j < 3; j++)
                {
                    Gl.glMultiTexCoord2fARB(Gl.GL_TEXTURE0_ARB, T.V[j].TexcoordU, T.V[j].TexcoordV);
                    shader.SetVertexData("in_tangent", T.V[j].Tangent);
                    Gl.glNormal3f(T.V[j].Normal.X * currentNormalScaleFaktor, T.V[j].Normal.Y * currentNormalScaleFaktor, T.V[j].Normal.Z * currentNormalScaleFaktor);
                    Gl.glVertex3f(T.V[j].Position.X, T.V[j].Position.Y, T.V[j].Position.Z);

                    //shader.SetVertexData("in_position", T.V[j].Position);
                    //shader.SetVertexData("in_normal", T.V[j].Normale * currentNormalScaleFaktor);
                    //shader.SetVertexData("in_tangent", T.V[j].Tangent);
                    //shader.SetVertexData("in_textcoord", T.V[j].TextcoordVector);
                }

                Gl.glEnd();
            }
        }

        //Alter Weg (hier geht der Geometry-Shader nicht. Deswegen geht kein Explosionseffekt)
        public void RemoveTriangleArray(int triangleArrayId)
        {
            if (triangleArrays.ContainsKey(triangleArrayId))
                triangleArrays.Remove(triangleArrayId);
        }
    }

    class TriangleDrawerNewWay : ITriangleDrawer
    {
        //Speichert die Vertexpufferdaten für ein Triangle-Array
        class TriangleVertex
        {
            public int PositionVboHandle;
            public int NormalVboHandle;
            public int TangendsVboHandle;
            public int TextCoordsVboHandle;
            public int IndexVboHandle;
            public int IndexCount;
            public uint[] IndexArray;
        }
        private readonly Dictionary<int, TriangleVertex> triangles = new Dictionary<int, TriangleVertex>(); //[VAO-Handler | TriangleVertex]

        public bool VBOsAreUsed { get; } = true;

        //Return: Anzahl gezeichneter Dreiecke
        public int GetTriangleArrayId(Triangle[] data)
        {
            int triangleArrayID = 1;
            if (triangles.Count > 0) triangleArrayID = triangles.Keys.Max() + 1;

            TriangleHelper.TransformTriangleListToVertexIndexList(data, out List<Vertex> vertexList, out List<uint> indexList);

            List<float> vertexPositions = new List<float>();
            List<float> normals = new List<float>();
            List<float> tangends = new List<float>();
            List<float> textcoords = new List<float>();

            foreach (Vertex V in vertexList)
            {
                vertexPositions.Add(V.Position.X);
                vertexPositions.Add(V.Position.Y);
                vertexPositions.Add(V.Position.Z);

                normals.Add(V.Normal.X);
                normals.Add(V.Normal.Y);
                normals.Add(V.Normal.Z);

                tangends.Add(V.Tangent.X);
                tangends.Add(V.Tangent.Y);
                tangends.Add(V.Tangent.Z);

                textcoords.Add(V.TexcoordU);
                textcoords.Add(V.TexcoordV);
            }

            int[] positionVboHandle = new int[1];
            int[] normalVboHandle = new int[1];
            int[] tangendsVboHandle = new int[1];
            int[] textCoordsVboHandle = new int[1];
            int[] indexVboHandle = new int[1];

            //https://learnopengl.com/Getting-started/Hello-Triangle
            //Es kann immer ein Buffer-Type (GL_ARRAY_BUFFER) gebunden werden
            //GL_STATIC_DRAW: the data is set only once and used many times.

            //Create VBOs
            Gl.glGenBuffers(1, positionVboHandle);
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, positionVboHandle[0]);
            Gl.glBufferData(Gl.GL_ARRAY_BUFFER, (IntPtr)(vertexPositions.Count * sizeof(float)), vertexPositions.ToArray(), Gl.GL_STATIC_DRAW);

            Gl.glGenBuffers(1, normalVboHandle);
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, normalVboHandle[0]);
            Gl.glBufferData(Gl.GL_ARRAY_BUFFER, (IntPtr)(normals.Count * sizeof(float)), normals.ToArray(), Gl.GL_STATIC_DRAW);

            Gl.glGenBuffers(1, tangendsVboHandle);
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, tangendsVboHandle[0]);
            Gl.glBufferData(Gl.GL_ARRAY_BUFFER, (IntPtr)(tangends.Count * sizeof(float)), tangends.ToArray(), Gl.GL_STATIC_DRAW);

            Gl.glGenBuffers(1, textCoordsVboHandle);
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, textCoordsVboHandle[0]);
            Gl.glBufferData(Gl.GL_ARRAY_BUFFER, (IntPtr)(textcoords.Count * sizeof(float)), textcoords.ToArray(), Gl.GL_STATIC_DRAW);

            Gl.glGenBuffers(1, indexVboHandle);
            Gl.glBindBuffer(Gl.GL_ELEMENT_ARRAY_BUFFER, indexVboHandle[0]);
            Gl.glBufferData(Gl.GL_ELEMENT_ARRAY_BUFFER, (IntPtr)(indexList.Count * sizeof(uint)), indexList.ToArray(), Gl.GL_STATIC_DRAW);

            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, 0);
            Gl.glBindBuffer(Gl.GL_ELEMENT_ARRAY_BUFFER, 0);

            triangles.Add(triangleArrayID, new TriangleVertex() { IndexVboHandle = indexVboHandle[0], IndexCount = indexList.Count, IndexArray = indexList.ToArray(), PositionVboHandle = positionVboHandle[0], NormalVboHandle = normalVboHandle[0], TangendsVboHandle = tangendsVboHandle[0], TextCoordsVboHandle = textCoordsVboHandle[0] });
            return triangleArrayID;
        }

        public void DrawTriangleArray(int triangleArrayID)
        {
            //Erlaubte Werte: GL_COLOR_ARRAY, GL_EDGE_FLAG_ARRAY, GL_FOG_COORD_ARRAY, GL_INDEX_ARRAY, GL_NORMAL_ARRAY, GL_SECONDARY_COLOR_ARRAY, GL_TEXTURE_COORD_ARRAY, GL_VERTEX_ARRAY
            Gl.glEnableClientState(Gl.GL_VERTEX_ARRAY);
            Gl.glEnableClientState(Gl.GL_NORMAL_ARRAY);
            Gl.glEnableClientState(Gl.GL_TEXTURE_COORD_ARRAY);

            Gl.glEnableVertexAttribArray(0);
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, triangles[triangleArrayID].PositionVboHandle);
            Gl.glVertexAttribPointer(0, 3, Gl.GL_FLOAT, Gl.GL_FALSE, sizeof(float) * 3, IntPtr.Zero);

            Gl.glEnableVertexAttribArray(1);
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, triangles[triangleArrayID].NormalVboHandle);
            Gl.glVertexAttribPointer(1, 3, Gl.GL_FLOAT, Gl.GL_TRUE, sizeof(float) * 3, IntPtr.Zero);

            Gl.glEnableVertexAttribArray(2);
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, triangles[triangleArrayID].TangendsVboHandle);
            Gl.glVertexAttribPointer(2, 3, Gl.GL_FLOAT, Gl.GL_TRUE, sizeof(float) * 3, IntPtr.Zero);

            Gl.glEnableVertexAttribArray(3);
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, triangles[triangleArrayID].TextCoordsVboHandle);
            Gl.glVertexAttribPointer(3, 2, Gl.GL_FLOAT, Gl.GL_FALSE, sizeof(float) * 2, IntPtr.Zero);

            Gl.glBindBuffer(Gl.GL_ELEMENT_ARRAY_BUFFER, triangles[triangleArrayID].IndexVboHandle);
            Gl.glDrawElements(Gl.GL_TRIANGLES, triangles[triangleArrayID].IndexCount, Gl.GL_UNSIGNED_INT, IntPtr.Zero);

            //Remember to unbind your buffer to prevent it to destroy
            //other draw calls or objects
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, 0);
            Gl.glBindBuffer(Gl.GL_ELEMENT_ARRAY_BUFFER, 0);

            Gl.glDisableClientState(Gl.GL_VERTEX_ARRAY);
            Gl.glDisableClientState(Gl.GL_NORMAL_ARRAY);
            Gl.glDisableClientState(Gl.GL_TEXTURE_COORD_ARRAY);
        }

        public void RemoveTriangleArray(int triangleArrayId)
        {
            var d = triangles[triangleArrayId];
            Gl.glDeleteBuffers(5, new int[] { d.PositionVboHandle, d.NormalVboHandle, d.TangendsVboHandle, d.TextCoordsVboHandle, d.IndexVboHandle });
            triangles.Remove(triangleArrayId);
        }
    }
}
