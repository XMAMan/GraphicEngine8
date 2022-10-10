using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using GraphicGlobal;

namespace GraphicPipelineOpenGLv3_0
{
    class ShaderHelper : IDisposable
    {
        public enum ShaderMode
        {
            None,
            Normal,
            Parallax,
            Displacement,
            CreateShadowMap,
            MouseHitTest
        }

        //Speichert die Vertexpufferdaten für ein Triangle-Array
        class TriangleVertex
        {
            public int PositionVboHandle;
            public int NormalVboHandle;
            public int TangendsVboHandle;
            public int TextCoordsVboHandle;
            public int IndexVboHandle;
            public int IndexCount;
        }

        private int[] shaderPrograms = new int[] { 0, 0, 0, 0, 0, 0, 0 };//None, Pong, Parallax, Displacement, CreateShadowMap, MouseHitTest
        private Dictionary<string, int> attributVariables = new Dictionary<string, int>();  // Vertex-Variablen
        private Dictionary<string, int> uniformVariables = new Dictionary<string, int>();   // Gilt für den gesamten Vertexpuffer
        private Dictionary<int, TriangleVertex> triangles = new Dictionary<int, TriangleVertex>(); //[VAO-Handler | TriangleVertex]
        private ShaderMode currentMode = ShaderMode.Normal;

        public ShaderHelper()
        {
            CreateShaderprogram(Properties.Resources.VertexShaderNormal, Properties.Resources.PixelShaderNormal, Properties.Resources.GeometryShader, Properties.Resources.CommonShaderFunctions, ShaderMode.Normal);
            CreateShaderprogram(Properties.Resources.VertexShaderParallax, Properties.Resources.PixelShaderParallax, "", Properties.Resources.CommonShaderFunctions, ShaderMode.Parallax);
            CreateShaderprogram(Properties.Resources.VertexShaderNormal, Properties.Resources.PixelShaderNormal, Properties.Resources.GeometryShader, Properties.Resources.CommonShaderFunctions, ShaderMode.Displacement);
            CreateShaderprogram(Properties.Resources.VertexShaderCreateShadowMap, Properties.Resources.PixelShaderCreateShadowMap, "", Properties.Resources.CommonShaderFunctions, ShaderMode.CreateShadowMap);
            CreateShaderprogram(Properties.Resources.VertexShaderMouseHitTest, Properties.Resources.PixelShaderMouseHitTest, "", Properties.Resources.CommonShaderFunctions, ShaderMode.MouseHitTest);
        }

        public ShaderMode Mode
        {
            set
            {
                if (LockShaderModeWriting) return;
                GL.UseProgram(shaderPrograms[(int)value]);
                currentMode = value;

                //Wenn ich diese beiden Anweisungen nicht drin habe dann wird keine Siolette (DrawLine3D) bei der Rasterizer-Schatten-Mirror-Szene gemalt
                GL.Begin(PrimitiveType.Triangles);
                GL.End();
            }
            get
            {
                return currentMode;
            }
        }

        public bool LockShaderModeWriting = false;

        private void CreateShaderprogram(string vertexShader, string pixelShader, string geometryShader, string commonFunctionsShader, ShaderMode mode)
        {
            pixelShader = pixelShader.Replace("#COMMONFUNCTIONS#", commonFunctionsShader);
            // Schritt 1: Shaderdateien einlesen und übersetzen
            int vertexShaderhandler = GL.CreateShader(ShaderType.VertexShader);
            int fragmentShaderhandler = GL.CreateShader(ShaderType.FragmentShader);
            int geometryShaderhandler = GL.CreateShader(ShaderType.GeometryShader);

            GL.ShaderSource(vertexShaderhandler, vertexShader);
            GL.ShaderSource(fragmentShaderhandler, pixelShader);
            if (geometryShader != "") GL.ShaderSource(geometryShaderhandler, geometryShader);

            GL.CompileShader(vertexShaderhandler);
            GL.CompileShader(fragmentShaderhandler);
            if (geometryShader != "") GL.CompileShader(geometryShaderhandler);

            CheckShaderCompileErrorStateAndThrowException(vertexShaderhandler, "Vertexshader");
            CheckShaderCompileErrorStateAndThrowException(fragmentShaderhandler, "Pixelshader");
            if (geometryShader != "") CheckShaderCompileErrorStateAndThrowException(geometryShaderhandler, "GeometryShader");

            // Schritt 2: Shaderprogram erstellen (Enthält alle Shader)
            int shaderProgram = GL.CreateProgram();
            shaderPrograms[(int)mode] = shaderProgram;
            // Alle Shader ans Program anhängen(pro Anwendung darf es nur ein Program geben)
            GL.AttachShader(shaderProgram, vertexShaderhandler);      // Shader von Program wieder löschen: void glDetachShader(GLuint program, GLuint shader); 
            GL.AttachShader(shaderProgram, fragmentShaderhandler);    // void glDeleteShader(GLuint id); ->Darf erst nach glDetachShader ausgeführt werden; Program löschen: (void glDeleteProgram(GLuint id); )
            if (geometryShader != "")
            {
                GL.AttachShader(shaderProgram, geometryShaderhandler);

                //Festlegen, für welchen Geometryarten der Shader ist
                GL.Ext.ProgramParameter(shaderProgram, (AssemblyProgramParameterArb)ExtGeometryShader4.GeometryInputTypeExt, (int)All.Triangles);
                GL.Ext.ProgramParameter(shaderProgram, (AssemblyProgramParameterArb)ExtGeometryShader4.GeometryOutputTypeExt, (int)All.Triangles);

                int tmp;
                GL.GetInteger((GetPName)ExtGeometryShader4.MaxGeometryOutputVerticesExt, out tmp);
                GL.Ext.ProgramParameter(shaderProgram, (AssemblyProgramParameterArb)ExtGeometryShader4.GeometryVerticesOutExt, tmp);
            }

            GL.LinkProgram(shaderProgram);
            CheckShaderCompileErrorStateAndThrowException(shaderProgram, "linken des Shaderprogramms");

            GL.UseProgram(shaderProgram);

            //Schreibe in die Texturvariablen fest die Zahlen 0 und 1
            int TexturHandler = GL.GetUniformLocation(shaderProgram, "Texture0");
            int BumpmapHandler = GL.GetUniformLocation(shaderProgram, "Texture1");
            int CubmapHandler = GL.GetUniformLocation(shaderProgram, "Cubemap");
            int ShadowMapHandler = GL.GetUniformLocation(shaderProgram, "ShadowMap");
            GL.Uniform1(TexturHandler, 0);
            GL.Uniform1(BumpmapHandler, 1);  // The texture in the second slot#
            GL.Uniform1(CubmapHandler, 2);
            GL.Uniform1(ShadowMapHandler, 3);

            Mode = mode;

            StringBuilder globalVariables = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                string variableName = GL.GetActiveUniformName(shaderProgram, i);
                int id = GL.GetUniformLocation(shaderPrograms[(int)currentMode], variableName);
                if (id != -1) globalVariables.Append(id + " = " + variableName + "\n");
                if (id == -1) break;
            }
            string namen = globalVariables.ToString();
        }

        private void CheckShaderCompileErrorStateAndThrowException(int shaderHandler, string shaderFehlerName)
        {
            int errorCheck;
            GL.GetShader(shaderHandler, ShaderParameter.CompileStatus, out errorCheck);
            if (errorCheck == 1) return;
            GL.GetShader(shaderHandler, ShaderParameter.InfoLogLength, out errorCheck);
            if (errorCheck > 0)
            {
                StringBuilder errorMessage = new StringBuilder(errorCheck);
                GL.GetShaderInfoLog(shaderHandler, errorCheck, out errorCheck, errorMessage);
                throw new Exception("Fehler beim " + shaderFehlerName + ": " + errorMessage.ToString());
            }
        }

        //Uniformvariablen dürfen nicht zwischen glBegin() ... und glEnd() stehen
        //Man kann also keine Vertexinformationen übergeben. Nur Informationen für ein gesamamtes GrafikObjekt
        //Sie dienen z.B. zur Übergabe eines Timewertes
        //Uniformvariablen können vom Vertexshader und Fragmentshader nur gelesen werden
        //Hinweis: Nicht benutzte Variablen werden vom Compilier offentsichlicht wegoptimiert. Deswegen kann man sie dann mit GetUniformLocation nicht mehr finden.
        private int CreateUniformVariable(string variableName)
        {
            string key = currentMode.ToString() + variableName;
            if (!uniformVariables.Keys.Contains(key))
            {
                int id = GL.GetUniformLocation(shaderPrograms[(int)currentMode], variableName);
                //if (id != -1)
                uniformVariables.Add(key, id);
                //else
                 //throw new Exception("Fehler bei OpenGL 3.0: Variable konnte nicht im Shaderprogram '" + currentMode.ToString() + "' gefunden werden -> " + variableName);
                CheckShaderCompileErrorStateAndThrowException(shaderPrograms[(int)currentMode], variableName);
            }

            return uniformVariables[key];
        }

        //Setze/lese Int-Variable, die für das gesamte Vertexarray gilt
        public int this[string variableName]
        {
            set
            {
                SetUniformVariable(variableName, value, true);
            }
            get
            {
                int intvalue;
                GL.GetUniform(shaderPrograms[(int)currentMode], CreateUniformVariable(variableName), out intvalue);
                return intvalue;
            }
        }

        public void SetUniformVariable(string variableName, int data, bool setForAllShaders = true)
        {
            if (currentMode != ShaderMode.None)
            {
                GL.Uniform1(CreateUniformVariable(variableName), data);
            }
            if (setForAllShaders)
            {
                var modeBefore = Mode;
                Mode = ShaderMode.Normal; GL.Uniform1(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.Parallax; GL.Uniform1(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.Displacement; GL.Uniform1(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.CreateShadowMap; GL.Uniform1(CreateUniformVariable(variableName), data);
                Mode = modeBefore;
            }
        }

        public void SetUniformVariable(string variableName, Matrix4 data, bool setForAllShaders = true)
        {
            if (currentMode != ShaderMode.None)
            {
                GL.UniformMatrix4(CreateUniformVariable(variableName), false, ref data);
            }
            if (setForAllShaders)
            {
                var modeBefore = Mode;
                Mode = ShaderMode.Normal; GL.UniformMatrix4(CreateUniformVariable(variableName), false, ref data);
                Mode = ShaderMode.Parallax; GL.UniformMatrix4(CreateUniformVariable(variableName), false, ref data);
                Mode = ShaderMode.Displacement; GL.UniformMatrix4(CreateUniformVariable(variableName), false, ref data);
                Mode = ShaderMode.CreateShadowMap; GL.UniformMatrix4(CreateUniformVariable(variableName), false, ref data);
                Mode = modeBefore;
            }
        }

        public void SetUniformVariable(string variableName, Matrix3 data, bool setForAllShaders = true)
        {
            if (currentMode != ShaderMode.None)
            {
                GL.UniformMatrix3(CreateUniformVariable(variableName), false, ref data);
            }
            if (setForAllShaders)
            {
                var modeBefore = Mode;
                Mode = ShaderMode.Normal; GL.UniformMatrix3(CreateUniformVariable(variableName), false, ref data);
                Mode = ShaderMode.Parallax; GL.UniformMatrix3(CreateUniformVariable(variableName), false, ref data);
                Mode = ShaderMode.Displacement; GL.UniformMatrix3(CreateUniformVariable(variableName), false, ref data);
                Mode = ShaderMode.CreateShadowMap; GL.UniformMatrix3(CreateUniformVariable(variableName), false, ref data);
                Mode = modeBefore;
            }
        }

        public void SetUniformVariable(string variableName, Vector4 data, bool setForAllShaders = true)
        {
            if (currentMode != ShaderMode.None)
            {
                GL.Uniform4(CreateUniformVariable(variableName), data);
            }
            if (setForAllShaders)
            {
                var modeBefore = Mode;
                Mode = ShaderMode.Normal; GL.Uniform4(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.Parallax; GL.Uniform4(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.Displacement; GL.Uniform4(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.CreateShadowMap; GL.Uniform4(CreateUniformVariable(variableName), data);
                Mode = modeBefore;
            }
        }

        public void SetUniformVariable(string variableName, float data, bool setForAllShaders = true)
        {
            if (currentMode != ShaderMode.None)
            {
                GL.Uniform1(CreateUniformVariable(variableName), data);
            }
            if (setForAllShaders)
            {
                var modeBefore = Mode;
                Mode = ShaderMode.Normal; GL.Uniform1(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.Parallax; GL.Uniform1(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.Displacement; GL.Uniform1(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.CreateShadowMap; GL.Uniform1(CreateUniformVariable(variableName), data);
                Mode = modeBefore;
            }
        }

        public void SetUniformVariable(string variableName, Vector3 data, bool setForAllShaders = true)
        {
            if (currentMode != ShaderMode.None)
            {
                GL.Uniform3(CreateUniformVariable(variableName), data);
            }
            if (setForAllShaders)
            {
                var modeBefore = Mode;
                Mode = ShaderMode.Normal; GL.Uniform3(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.Parallax; GL.Uniform3(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.Displacement; GL.Uniform3(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.CreateShadowMap; GL.Uniform3(CreateUniformVariable(variableName), data);
                Mode = modeBefore;
            }
        }

        /*public void SetVertexData(string variableName, Vector3D data, bool setForAllShaders = true)
        {
            if (currentModus != ShaderModus.None)
            {
                GL.VertexAttrib3(CreateAttributVariable(currentModus.ToString() + variableName, variableName), data.x, data.y, data.z);
            }
            if (setForAllShaders)
            {
                var modeBefore = Modus;
                Modus = ShaderModus.FlatGouraudPongBumpmap; GL.VertexAttrib3(CreateAttributVariable(currentModus.ToString() + variableName, variableName), data.x, data.y, data.z);
                Modus = ShaderModus.Parallax; GL.VertexAttrib3(CreateAttributVariable(currentModus.ToString() + variableName, variableName), data.x, data.y, data.z);
                Modus = ShaderModus.ParallaxSimple; GL.VertexAttrib3(CreateAttributVariable(currentModus.ToString() + variableName, variableName), data.x, data.y, data.z);
                Modus = ShaderModus.Displacement; GL.VertexAttrib3(CreateAttributVariable(currentModus.ToString() + variableName, variableName), data.x, data.y, data.z);
                Modus = ShaderModus.CreateShadowMap; GL.VertexAttrib3(CreateAttributVariable(currentModus.ToString() + variableName, variableName), data.x, data.y, data.z);
                Modus = modeBefore;
            }
        }

        //Können für jedes Vertex gesetzt werden, d.h. darf zwischen GlBegin-glEnd stehen
        //Kann vom Vertexshader nur gelesen werden(Fragmentshader hat kein Zugriff drauf)
        //Benutzung: loc=Create_Attribut_Variable(9.81,"loc"); glBegin(GL_TRIANGLE_STRIP); glVertexAttrib1f(loc,2.0); glVertex2f(-1,1); ... glEnd();
        private int CreateAttributVariable(string key, string variableName)
        {
            if (!attributVariablen.Keys.Contains(key))
            {
                //GL.VertexAttrib1f(attributVariablen[variableName], 123.4f); //Schreiben
                attributVariablen.Add(key, GL.GetAttribLocation(shaderPrograme[(int)currentModus], variableName));
                CheckShaderCompileErrorStateAndThrowException(shaderPrograme[(int)currentModus], variableName);
            }
            return attributVariablen[variableName];
        }*/

        public int GetTriangleArrayID(Triangle[] data)
        {
            List<Vertex> vertexList;
            List<uint> indexList;
            TriangleHelper.TransformTriangleListToVertexIndexList(data, out vertexList, out indexList);

            Vector3[] vertexPositions = new Vector3[vertexList.Count];
            Vector2[] textcoords = new Vector2[vertexList.Count];
            Vector3[] normals = new Vector3[vertexList.Count];
            Vector3[] tangends = new Vector3[vertexList.Count];

            for (int i = 0; i < vertexList.Count; i++)
            {
                vertexPositions[i] = new Vector3(vertexList[i].Position.X, vertexList[i].Position.Y, vertexList[i].Position.Z);
                normals[i] = new Vector3(vertexList[i].Normal.X, vertexList[i].Normal.Y, vertexList[i].Normal.Z);
                textcoords[i] = new Vector2(vertexList[i].TexcoordU, vertexList[i].TexcoordV);
                tangends[i] = new Vector3(vertexList[i].Tangent.X, vertexList[i].Tangent.Y, vertexList[i].Tangent.Z);
            }

            int positionVboHandle, normalVboHandle, tangendsVboHandle, textCoordsVboHandle, indexVboHandle, vaoHandle;

            //Create VBOs
            GL.GenBuffers(1, out positionVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(vertexPositions.Length * Vector3.SizeInBytes),
                vertexPositions, BufferUsageHint.StaticDraw);

            GL.GenBuffers(1, out normalVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(normals.Length * Vector3.SizeInBytes),
                normals, BufferUsageHint.StaticDraw);

            GL.GenBuffers(1, out tangendsVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, tangendsVboHandle);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(tangends.Length * Vector3.SizeInBytes),
                tangends, BufferUsageHint.StaticDraw);

            GL.GenBuffers(1, out textCoordsVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, textCoordsVboHandle);
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer,
                new IntPtr(textcoords.Length * Vector2.SizeInBytes),
                textcoords, BufferUsageHint.StaticDraw);

            GL.GenBuffers(1, out indexVboHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexVboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                new IntPtr(sizeof(uint) * indexList.Count),
                indexList.ToArray(), BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            //Create VAOs
            // GL3 allows us to store the vertex layout in a "vertex array object" (VAO).
            // This means we do not have to re-issue VertexAttribPointer calls
            // every time we try to use a different vertex layout - these calls are
            // stored in the VAO so we simply need to bind the correct VAO.
            GL.GenVertexArrays(1, out vaoHandle);
            GL.BindVertexArray(vaoHandle);

            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);
            GL.BindAttribLocation(shaderPrograms[(int)currentMode], 0, "in_position");

            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);
            GL.BindAttribLocation(shaderPrograms[(int)currentMode], 1, "in_normal");

            GL.EnableVertexAttribArray(2);
            GL.BindBuffer(BufferTarget.ArrayBuffer, tangendsVboHandle);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);
            GL.BindAttribLocation(shaderPrograms[(int)currentMode], 2, "in_tangent");

            GL.EnableVertexAttribArray(3);
            GL.BindBuffer(BufferTarget.ArrayBuffer, textCoordsVboHandle);
            GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);
            GL.BindAttribLocation(shaderPrograms[(int)currentMode], 3, "in_textcoord");

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexVboHandle);

            GL.BindVertexArray(0);

            triangles.Add(vaoHandle, new TriangleVertex() { IndexVboHandle = indexVboHandle, IndexCount = indexList.Count, PositionVboHandle = positionVboHandle, NormalVboHandle = normalVboHandle, TangendsVboHandle = tangendsVboHandle, TextCoordsVboHandle = textCoordsVboHandle });
            return vaoHandle;
        }

        //Return: Anzahl gezeichneter Dreiecke
        public void DrawTriangleArray(int triangleArrayId)
        {
            GL.BindVertexArray(triangleArrayId);

            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, triangles[triangleArrayId].PositionVboHandle);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, triangles[triangleArrayId].NormalVboHandle);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

            GL.EnableVertexAttribArray(2);
            GL.BindBuffer(BufferTarget.ArrayBuffer, triangles[triangleArrayId].TangendsVboHandle);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

            GL.EnableVertexAttribArray(3);
            GL.BindBuffer(BufferTarget.ArrayBuffer, triangles[triangleArrayId].TextCoordsVboHandle);
            GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, triangles[triangleArrayId].IndexVboHandle);

            GL.DrawElements(PrimitiveType.Triangles, triangles[triangleArrayId].IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.DisableVertexAttribArray(2);
            GL.DisableVertexAttribArray(3);
        }

        public void RemoveTriangleArray(int triangleArrayId)
        {
            var d = triangles[triangleArrayId];
            GL.DeleteBuffers(5, new int[] { d.PositionVboHandle, d.NormalVboHandle, d.TangendsVboHandle, d.TextCoordsVboHandle, d.IndexVboHandle });
            triangles.Remove(triangleArrayId);
        }

        #region IDisposable Member

        public void Dispose()
        {
            foreach (int program in shaderPrograms)
                if (program != 0)
                    GL.DeleteProgram(program);
        }

        #endregion
    }
}
