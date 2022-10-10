using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tao.OpenGl;
using GraphicMinimal;
using GraphicGlobal;

namespace GraphicPipelineOpenGLv1_0
{
    class ShaderHelper
    {
        public enum ShaderMode
        {
            None,
            Normal,
            Parallax,
            CreateShadowMap,
            MouseHitTest,
        }

        private readonly int[] shaderPrograms = new int[] { 0, 0, 0, 0, 0};//None, Normal, Parallax, CreateShadowmap, MouseHitTest, LinesAndPoints
        private readonly Dictionary<string, int> attributVariables = new Dictionary<string, int>();  // Vertex-Variablen
        private readonly Dictionary<string, int> uniformVariables = new Dictionary<string, int>();   // Gilt für den gesamten Vertexpuffer
        private ShaderMode currentMode = ShaderMode.None;

        public ITriangleDrawer TriangleDrawer { get; private set; }

        public bool UseGeometryShader { get; private set; }

        public ShaderHelper(bool useOldWay)
        {
            try
            {
                string vertexShader = "";
                string geometryShader = "";
                string[] vertexData = null;
                string vertexShaderShadowMap = "";
                string vertexShaderMouseHit = "";

                if (useOldWay) //Ohne GeometryShader; Somit fehlt der Explosionseffekt
                {
                    //Achtung: Der Shader für die Shadowmap und den Mousehittest sind nun für den neuen Weg geschrieben; Die Texturekoordinaten werden dort falsch berechnet, wenn ich OldWay=true setze
                    this.TriangleDrawer = new TriangleDrawerOldWay(this);
                    vertexShader = Properties.Resources.VertexShaderNoGeometryBehind;
                    geometryShader = "";
                    vertexData = new string[] { "in_tangent" }; //Wenn ich über shader.SetVertexData("in_tangent", T.V[j].Tangent); die Daten bei jeden Vertex übergebe
                    vertexShaderShadowMap = Properties.Resources.VertexShaderCreateShadowmapOldWay;
                    vertexShaderMouseHit = Properties.Resources.VertexShaderMouseHitOldWay;
                }
                else //Mit GeometryShader mit Explosionseffekt
                {
                    this.TriangleDrawer = new TriangleDrawerNewWay();
                    vertexShader = Properties.Resources.VertexShaderNormal;
                    geometryShader = Properties.Resources.GeometryShader;
                    vertexData = new string[] { };
                    vertexShaderShadowMap = Properties.Resources.VertexShaderCreateShadowmap;
                    vertexShaderMouseHit = Properties.Resources.VertexShaderMouseHitTest;
                }

                shaderPrograms[(int)ShaderMode.Normal] = Init(ShaderMode.Normal, vertexShader, Properties.Resources.PixelShaderNormal, Properties.Resources.CommonShaderFunctions, geometryShader, vertexData);
                shaderPrograms[(int)ShaderMode.Parallax] = Init(ShaderMode.Parallax, vertexShader, Properties.Resources.PixelShaderParallax, Properties.Resources.CommonShaderFunctions, geometryShader, vertexData);
                shaderPrograms[(int)ShaderMode.CreateShadowMap] = Init(ShaderMode.CreateShadowMap, vertexShaderShadowMap, Properties.Resources.PixelShaderCreateShadowmap, Properties.Resources.CommonShaderFunctions,/*Properties.Resources.GeometryShader*/"", vertexData);
                shaderPrograms[(int)ShaderMode.MouseHitTest] = Init(ShaderMode.MouseHitTest, vertexShaderMouseHit, Properties.Resources.PixelShaderMouseHitTest, Properties.Resources.CommonShaderFunctions, geometryShader, vertexData);

            }
            catch (Exception ex) //Wenn Fehler beim Shader, dann nutze OpenGL ohne Shader
            {
                string errorMessage = ex.ToString();
            }
        }

        public bool LockShaderModeWriting = false;

        public ShaderMode Mode
        {
            set
            {
                if (LockShaderModeWriting) return;
                Gl.glUseProgram(shaderPrograms[(int)value]);
                currentMode = value;
            }
            get
            {
                return currentMode;
            }
        }

        //Wenn ich mit den Geometry-Shader arbeite, muss ich mit VBOs arbeiten.
        //Wenn ich mit VBOs arbeite, muss ich mit glDrawElements angeben, ob ich denn Dreicke, Linien oder Punkte zeichnen will
        //Da ich momentan aber nur Dreiecke in VBOs speichere aber Linien und Punkte bei jeden DrawFrame erneut von der CPU in die Grafikkarte kopiere,
        //muss ich das Zeichnen ohne Geometry-Shader/VBOs machen
        public void SetModeForDrawingLinesOrPoints()
        {
            if (Mode != ShaderMode.None && this.TriangleDrawer.VBOsAreUsed) this.Mode = ShaderMode.None;
        }

        private int Init(ShaderMode mode, string vertexShader, string pixelShader, string pixelShaderReplace, string geometryShader, string[] vertexVariables)
        {
            // Schritt 1: Shaderdateien einlesen und übersetzen
            int vertexShaderhandler = Gl.glCreateShader(Gl.GL_VERTEX_SHADER);
            int fragmentShaderhandler = Gl.glCreateShader(Gl.GL_FRAGMENT_SHADER);
            int geometryShaderhandler = Gl.glCreateShader(Gl.GL_GEOMETRY_SHADER_EXT);

            string[] vertexShaderFile = new string[] { vertexShader };
            string[] fragmentShaderFile = new string[] { pixelShader.Replace("#COMMONFUNCTIONS#", pixelShaderReplace)  };
            string[] geometryShaderFile = new string[] { geometryShader };

            Gl.glShaderSource(vertexShaderhandler, 1, vertexShaderFile, null);
            Gl.glShaderSource(fragmentShaderhandler, 1, fragmentShaderFile, null);
            if (!geometryShaderFile[0].Equals("")) Gl.glShaderSource(geometryShaderhandler, 1, geometryShaderFile, null);

            Gl.glCompileShader(vertexShaderhandler);
            Gl.glCompileShader(fragmentShaderhandler);
            if (!geometryShaderFile[0].Equals("")) Gl.glCompileShader(geometryShaderhandler);

            CheckShaderCompileErrorStateAndThrowException(vertexShaderhandler, "Vertexshader");
            CheckShaderCompileErrorStateAndThrowException(fragmentShaderhandler, "Pixelshader");
            if (!geometryShaderFile[0].Equals("")) CheckShaderCompileErrorStateAndThrowException(geometryShaderhandler, "GeometryShader");

            // Schritt 2: Shaderprogram erstellen (Enthält alle Shader)
            int shaderProgram = Gl.glCreateProgram();
            // Alle Shader ans Program anhängen(pro Anwendung darf es nur ein Program geben)
            Gl.glAttachShader(shaderProgram, vertexShaderhandler);      // Shader von Program wieder löschen: void glDetachShader(GLuint program, GLuint shader); 
            Gl.glAttachShader(shaderProgram, fragmentShaderhandler);    // void glDeleteShader(GLuint id); ->Darf erst nach glDetachShader ausgeführt werden; Program löschen: (void glDeleteProgram(GLuint id); )
            if (!geometryShaderFile[0].Equals(""))
            {
                Gl.glAttachShader(shaderProgram, geometryShaderhandler);

                //Festlegen, für welchen Geometryarten der Shader ist
                Gl.glProgramParameteriEXT(shaderProgram, Gl.GL_GEOMETRY_INPUT_TYPE_EXT, Gl.GL_TRIANGLES);
                Gl.glProgramParameteriEXT(shaderProgram, Gl.GL_GEOMETRY_OUTPUT_TYPE_EXT, Gl.GL_TRIANGLES);

                int[] temp = new int[1];
                Gl.glGetIntegerv(Gl.GL_MAX_GEOMETRY_OUTPUT_VERTICES_EXT, temp);
                Gl.glProgramParameteriEXT(shaderProgram, Gl.GL_GEOMETRY_VERTICES_OUT_EXT, temp[0]);
            }

            Gl.glLinkProgram(shaderProgram);
            CheckShaderCompileErrorStateAndThrowException(shaderProgram, "linken des Shaderprogramms");

            Gl.glUseProgram(shaderProgram);

            //Schreibe in die Texturvariablen fest die Zahlen 0 und 1
            int TexturHandler = Gl.glGetUniformLocation(shaderProgram, "Texture0");
            int BumpmapHandler = Gl.glGetUniformLocation(shaderProgram, "Texture1");
            int CubemapHandler = Gl.glGetUniformLocation(shaderProgram, "Cubemap");
            int ShadowMapHandler = Gl.glGetUniformLocation(shaderProgram, "ShadowMap");
            Gl.glUniform1i(TexturHandler, 0);
            Gl.glUniform1i(BumpmapHandler, 1);  // The texture in the second slot
            Gl.glUniform1i(CubemapHandler, 2);
            Gl.glUniform1i(ShadowMapHandler, 3);

            //Vertexvariablen müssen zwingend hier angelegt werden, da innerhalb des glBegin-glEnd-Blocks das zu spät ist und zu Fehlern führt
            foreach (string name in vertexVariables)
                CreateAttributVariable(mode.ToString() + name, name, shaderProgram);

            return shaderProgram;
        }

        private void CheckShaderCompileErrorStateAndThrowException(int shaderHandler, string shaderFehlerName)
        {
            int[] errorCheck = new int[] { 0 };
            Gl.glGetShaderiv(shaderHandler, Gl.GL_COMPILE_STATUS, errorCheck);
            if (errorCheck[0] == Gl.GL_TRUE) return;
            Gl.glGetShaderiv(shaderHandler, Gl.GL_INFO_LOG_LENGTH, errorCheck);
            if (errorCheck[0] > 0)
            {
                StringBuilder errorMessage = new StringBuilder(errorCheck[0]);
                Gl.glGetShaderInfoLog(shaderHandler, errorCheck[0], errorCheck, errorMessage);
                throw new Exception("Fehler beim " + shaderFehlerName + ": " + errorMessage.ToString());
            }
        }

        //Uniformvariablen dürfen nicht zwischen glBegin() ... und glEnd() stehen
        //Man kann also keine Vertexinformationen übergeben. Nur Informationen für ein gesamamtes GrafikObjekt
        //Sie dienen z.B. zur Übergabe eines Timerwertes
        //Uniformvariablen können vom Vertexshader und Fragmentshader nur gelesen werden
        private int CreateUniformVariable(string variableName)
        {
            string key = currentMode.ToString() + variableName;
            if (!uniformVariables.Keys.Contains(key))
            {
                int id = Gl.glGetUniformLocation(shaderPrograms[(int)currentMode], variableName);
                //if (id != -1)
                uniformVariables.Add(key, id);
                //else
                // throw new Exception("Fehler bei OpenGL 3.0: Variable konnte nicht im Shaderprogram '" + currentModus.ToString() + "' gefunden werden -> " + variableName);
                CheckShaderCompileErrorStateAndThrowException(shaderPrograms[(int)currentMode], variableName);
            }

            return uniformVariables[key];
        }

        //Setze Variable, die für das gesamte Vertexarray gilt
        public int this[string variableName]
        {
            set
            {
                SetUniformVariable(variableName, value);
            }
            get
            {
                int[] intvalue = new int[1];
                Gl.glGetUniformivARB(shaderPrograms[(int)currentMode], CreateUniformVariable(variableName), intvalue);
                return intvalue[0];
            }
        }

        public void SetUniformVariable(string variableName, int data)
        {
            if (currentMode != ShaderMode.None)
            {
                var modeBefore = Mode;
                Mode = ShaderMode.Normal; Gl.glUniform1iARB(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.Parallax; Gl.glUniform1iARB(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.CreateShadowMap; Gl.glUniform1iARB(CreateUniformVariable(variableName), data);
                Mode = modeBefore;
            }
        }

        public void SetUniformVariable(string variableName, Matrix4x4 data)
        {
            if ((int)currentMode != 0)
            {
                var modeBefore = Mode;
                Mode = ShaderMode.Normal; Gl.glUniformMatrix4fv(CreateUniformVariable(variableName), 1, 0, data.Values);
                Mode = ShaderMode.Parallax; Gl.glUniformMatrix4fv(CreateUniformVariable(variableName), 1, 0, data.Values);
                Mode = ShaderMode.CreateShadowMap; Gl.glUniformMatrix4fv(CreateUniformVariable(variableName), 1, 0, data.Values);
                Mode = modeBefore;
            }
        }
        public void SetUniformVariableMatrix3x3(string variableName, Matrix3x3 data)
        {
            if ((int)currentMode != 0)
            {
                var modeBefore = Mode;
                Mode = ShaderMode.Normal; Gl.glUniformMatrix3fv(CreateUniformVariable(variableName), 1, 0, data.Values);
                Mode = ShaderMode.Parallax; Gl.glUniformMatrix3fv(CreateUniformVariable(variableName), 1, 0, data.Values);
                Mode = ShaderMode.CreateShadowMap; Gl.glUniformMatrix3fv(CreateUniformVariable(variableName), 1, 0, data.Values);
                Mode = modeBefore;
            }
        }
        

        public void SetUniformVariable(string variableName, float data)
        {
            if (currentMode != ShaderMode.None)
            {
                var modeBefore = Mode;
                Mode = ShaderMode.Normal; Gl.glUniform1fARB(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.Parallax; Gl.glUniform1fARB(CreateUniformVariable(variableName), data);
                Mode = ShaderMode.CreateShadowMap; Gl.glUniform1fARB(CreateUniformVariable(variableName), data);
                Mode = modeBefore;
            }
        }

        public void SetUniformVariable(string variableName, Vector3D data)
        {
            if (currentMode != ShaderMode.None)
            {
                var modeBefore = Mode;
                Mode = ShaderMode.Normal; Gl.glUniform3fvARB(CreateUniformVariable(variableName), 1, data.Float3f);
                Mode = ShaderMode.Parallax; Gl.glUniform3fvARB(CreateUniformVariable(variableName), 1, data.Float3f);
                Mode = ShaderMode.CreateShadowMap; Gl.glUniform3fvARB(CreateUniformVariable(variableName), 1, data.Float3f);
                Mode = modeBefore;
            }
        }

        public void SetUniformVariable(string variableName, Vector3D xyz, float w)
        {
            if (currentMode != ShaderMode.None)
            {
                var modeBefore = Mode;
                Mode = ShaderMode.Normal; Gl.glUniform4fvARB(CreateUniformVariable(variableName), 1, new float[] { xyz.X, xyz.Y, xyz.Z, w });
                Mode = ShaderMode.Parallax; Gl.glUniform4fvARB(CreateUniformVariable(variableName), 1, new float[] { xyz.X, xyz.Y, xyz.Z, w });
                Mode = ShaderMode.CreateShadowMap; Gl.glUniform4fvARB(CreateUniformVariable(variableName), 1, new float[] { xyz.X, xyz.Y, xyz.Z, w });
                Mode = modeBefore;
            }
        }

        public void SetVertexData(string variableName, Vector3D data, int programmId = 0)
        {
            if (attributVariables.Keys.Contains(currentMode.ToString() + variableName))
            {
                Gl.glVertexAttrib3f(CreateAttributVariable(currentMode.ToString() + variableName, variableName, programmId), data.X, data.Y, data.Z);
            }
        }

        //Können für jedes Vertex gesetzt werden, d.h. darf zwischen GlBegin-glEnd stehen
        //Kann vom Vertexshader nur gelesen werden(Fragmentshader hat kein Zugriff drauf)
        //Benutzung: loc=Create_Attribut_Variable(9.81,"loc"); glBegin(GL_TRIANGLE_STRIP); glVertexAttrib1f(loc,2.0); glVertex2f(-1,1); ... glEnd();
        private int CreateAttributVariable(string key, string variableName, int programmId)
        {
            if (!attributVariables.Keys.Contains(key) && programmId != 0)
            {
                //GL.VertexAttrib1f(attributVariablen[variableName], 123.4f); //Schreiben
                attributVariables.Add(key, Gl.glGetAttribLocation(programmId, variableName));
                CheckShaderCompileErrorStateAndThrowException(shaderPrograms[(int)currentMode], variableName);
            }
            return attributVariables[key];
        }
    }
}
