using System;
using System.Text;

namespace GraphicMinimal
{
    //Das hier ist eine 4x4-Matrix um 3D-Vektoren affin zu transformieren
    //Wichtige Anmerkung beim Vergleich zwischen selbsterstellten Matrizen und OpenGL-Matrizen: Manchmal muss man die Matrix transponieren, damit sie übereinstimmt
    public class Matrix4x4
    {
        public Matrix4x4() { }
        private Matrix4x4(string definition, float[] values)
        {
            this.definition = definition;
            this.Values = values;
        }

        public Matrix4x4(float[] values)
        {
            this.definition = "UserDefined";
            this.Values = values;
        }

        private string definition;
        public string Definition { get => this.definition; set { this.definition = value; this.Values = FromDefinitionString(value).Values; } } //Damit ich die Matrix besser in Json-Serialisierter Form modifizieren/definieren kann
        public float[] Values { get; private set; } //nehme ich ein String und kein float-Array zur Darstellung

        public static Matrix4x4 FromDefinitionString(string definition)
        {
            var parse = DefinitionStringParser.Parse(definition);

            switch (parse.FunctionName)
            {
                case "Ident":
                    return Matrix4x4.Ident();
                case "Translate":
                    return Matrix4x4.Translate(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle());
                case "Scale":
                    return Matrix4x4.Scale(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle());
                case "Rotate":
                    return Matrix4x4.Rotate(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle(), parse.Parameter[3].ToSingle());
                case "Reflect":
                    return Matrix4x4.Reflect(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle(), parse.Parameter[3].ToSingle());
                case "LookAt":
                    return Matrix4x4.LookAt(
                        new Vector3D(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle()), //Eye
                        new Vector3D(parse.Parameter[3].ToSingle(), parse.Parameter[4].ToSingle(), parse.Parameter[5].ToSingle()), //Forward
                        new Vector3D(parse.Parameter[6].ToSingle(), parse.Parameter[7].ToSingle(), parse.Parameter[8].ToSingle()));//Up
                case "InverseLookAt":
                    return Matrix4x4.InverseLookAt(
                        new Vector3D(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle()), //Eye
                        new Vector3D(parse.Parameter[3].ToSingle(), parse.Parameter[4].ToSingle(), parse.Parameter[5].ToSingle()), //Forward
                        new Vector3D(parse.Parameter[6].ToSingle(), parse.Parameter[7].ToSingle(), parse.Parameter[8].ToSingle()));//Up

                case "Transpose":
                    return Matrix4x4.Transpose(Matrix4x4.FromDefinitionString((parse.Parameter[0] as DefinitionStringParserResult).DefinitionString));
                case "Invert":
                    return Matrix4x4.Invert(Matrix4x4.FromDefinitionString((parse.Parameter[0] as DefinitionStringParserResult).DefinitionString));
                case "Mult":
                    return Matrix4x4.FromDefinitionString((parse.Parameter[0] as DefinitionStringParserResult).DefinitionString) * Matrix4x4.FromDefinitionString((parse.Parameter[1] as DefinitionStringParserResult).DefinitionString);
                case "Model":
                    return Matrix4x4.Model(
                        new Vector3D(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle()), //Position
                        new Vector3D(parse.Parameter[3].ToSingle(), parse.Parameter[4].ToSingle(), parse.Parameter[5].ToSingle()), //Orientation
                        parse.Parameter[6].ToSingle());//Size
                case "NormalRotate":
                    return Matrix4x4.NormalRotate(new Vector3D(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle()));
                case "InverseModel":
                    return Matrix4x4.InverseModel(
                        new Vector3D(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle()), //Position
                        new Vector3D(parse.Parameter[3].ToSingle(), parse.Parameter[4].ToSingle(), parse.Parameter[5].ToSingle()), //Orientation
                        parse.Parameter[6].ToSingle());//Size
                case "InverseNormalRotate":
                    return Matrix4x4.InverseNormalRotate(new Vector3D(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle()));
                case "ProjectionMatrixOrtho":
                    return Matrix4x4.ProjectionMatrixOrtho(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle(), parse.Parameter[3].ToSingle(), parse.Parameter[4].ToSingle(), parse.Parameter[5].ToSingle());
                case "ProjectionMatrixPerspective":
                    return Matrix4x4.ProjectionMatrixPerspective(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle(), parse.Parameter[3].ToSingle());
                case "BilboardMatrixFromCameraMatrix":
                    return Matrix4x4.BilboardMatrixFromCameraMatrix(
                        new Vector3D(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle()), //ObjectPos
                        new Vector3D(parse.Parameter[3].ToSingle(), parse.Parameter[4].ToSingle(), parse.Parameter[5].ToSingle()), //Orientation
                        parse.Parameter[6].ToSingle(), //ObjectSize
                        Matrix4x4.FromDefinitionString((parse.Parameter[7] as DefinitionStringParserResult).DefinitionString));//CameraMatrix
                case "TBNMatrix":
                    return Matrix4x4.TBNMatrix(
                        new Vector3D(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle(), parse.Parameter[2].ToSingle()), //Normal
                        new Vector3D(parse.Parameter[3].ToSingle(), parse.Parameter[4].ToSingle(), parse.Parameter[5].ToSingle())  //Tangent
                        );


            }

            throw new ArgumentException($"Unknown Functionname {parse.FunctionName}");
        }

        public string Print()
        {
            if (this.Values.Length != 16) return "Fehler: Matrix.Length != 16";
            StringBuilder str = new StringBuilder();
            for (int y = 0; y < 4; y++)
            {
                str.Append("|");
                for (int x = 0; x < 4; x++)
                {
                    str.Append(String.Format("{0:+0.00;-0.00; 0.00}", this.Values[y * 4 + x]));
                    if (x < 3) str.Append("\t");
                }
                str.Append("|" + System.Environment.NewLine);
            }

            return str.ToString();
        }

        public override string ToString()
        {
            return this.Definition;
        }

        public static Matrix4x4 Ident()
        {
            return new Matrix4x4("Ident()", new float[] {1, 0, 0, 0,
                                                         0, 1, 0, 0,
                                                         0, 0, 1, 0,
                                                         0, 0, 0, 1});
        }

        public static Matrix4x4 Translate(float x, float y, float z)
        {
            return new Matrix4x4($"Translate({x.ToEnString()},{y.ToEnString()},{z.ToEnString()})", new float[] {1,    0,    0,    0,
                                                                                                                0,    1,    0,    0,
                                                                                                                0,    0,    1,    0,
                                                                                                                x,    y,    z,    1});
        }

        public static Matrix4x4 Scale(float x, float y, float z)
        {
            return new Matrix4x4($"Scale({x.ToEnString()},{y.ToEnString()},{z.ToEnString()})", new float[] {x, 0, 0, 0,
                                                                                                            0, y, 0, 0,
                                                                                                            0, 0, z, 0,
                                                                                                            0, 0, 0, 1});
        }

        //Quelle: http://www.gamedev.net/topic/600537-instead-of-glrotatef-build-a-matrix/
        //[x | y | z] - Drehachse
        public static Matrix4x4 Rotate(float angle, float x, float y, float z)
        {
            float c = (float)Math.Cos(angle * Math.PI / 180);
            float s = (float)Math.Sin(angle * Math.PI / 180);

            return new Matrix4x4($"Rotate({angle.ToEnString()},{x.ToEnString()},{y.ToEnString()},{z.ToEnString()})",
                new float[] {x * x * (1-c)+c,       y * x * (1-c)+z*s,  x*z*(1-c)-y*s,  0,
                             x*y*(1-c)-z*s,         y*y*(1-c)+c,        y*z*(1-c)+x*s,  0,
                             x*z*(1-c)+y*s,         y*z*(1-c)-x*s,      z*z*(1-c)+c,    0,
                             0,                     0,                  0,              1});
        }

        //http://ami.ektf.hu/uploads/papers/finalpdf/AMI_40_from175to186.pdf
        //http://msdn.microsoft.com/en-us/library/windows/desktop/bb205356(v=vs.85).aspx
        //abc ist die Normale der Ebene(muss normiert sein), d ist die Ursprungsverschiebung
        public static Matrix4x4 Reflect(float a, float b, float c, float d)
        {
            return new Matrix4x4($"Reflect({a.ToEnString()},{b.ToEnString()},{c.ToEnString()},{d.ToEnString()})",
                new float[] {-2 * a * a + 1,         -2 * b * a,         -2 * c * a,      0,
                             -2 * a * b   ,          -2 * b * b + 1,     -2 * c * b,      0,
                             -2 * a * c,             -2 * b * c,         -2 * c * c + 1,  0,
                             -2 * a * d,             -2 * b * d,         -2 * c * d,      1});
        }

        //Quelle: http://stackoverflow.com/questions/8124066/implementation-of-glulookat-and-gluperspective
        public static Matrix4x4 LookAt(Vector3D eye, Vector3D forward, Vector3D up)
        {
            Vector3D forward1 = Vector3D.Normalize(forward);
            Vector3D side = Vector3D.Cross(forward1, up);
            side = Vector3D.Normalize(side);
            Vector3D up1 = Vector3D.Cross(side, forward1);

            var res = new Matrix4x4(null, new float[]{side.X, up1.X, -forward1.X, 0,
                                     side.Y, up1.Y, -forward1.Y, 0,
                                     side.Z, up1.Z, -forward1.Z, 0,
                                     0, 0, 0, 1});

            var lookAt = Matrix4x4.Translate(-eye.X, -eye.Y, -eye.Z) * res;
            return new Matrix4x4($"LookAt({eye.X.ToEnString()},{eye.Y.ToEnString()},{eye.Z.ToEnString()},{forward.X.ToEnString()},{forward.Y.ToEnString()},{forward.Z.ToEnString()},{up.X.ToEnString()},{up.Y.ToEnString()},{up.Z.ToEnString()})", lookAt.Values);
        }

        //https://www.gamedev.net/topic/288155-how-to-compute-inverse-view-matrix/
        //Das hier entspricht NICHT Matrix.Inverse(GetGluLookAtMatrix) sondern eher von der Idee wie Matrix.GetTranslationMatrix(-x, -y, -z). Das ist die Umkehroperation von World2Eye(Also Eye2World)
        //Ich vermute, dass Matrix.Inverse einfach numerisch zu ungenau ist und es deswegen nicht geht. 
        public static Matrix4x4 InverseLookAt(Vector3D eye, Vector3D forward, Vector3D up)
        {
            //Die Look-At-Matrix besteht aus einer Rotationsmatrix und einer Translationsmatrix.
            Vector3D forward1 = Vector3D.Normalize(forward);
            Vector3D side = Vector3D.Cross(forward1, up);
            side = Vector3D.Normalize(side);
            Vector3D up1 = Vector3D.Cross(side, forward1);

            var res = new Matrix4x4(null, new float[]{side.X, up1.X, -forward1.X, 0,
                                     side.Y, up1.Y, -forward1.Y, 0,
                                     side.Z, up1.Z, -forward1.Z, 0,
                                     0, 0, 0, 1});

            res = Matrix4x4.Transpose(res); //Inverse von der Kamera-Rotationsmatrix

            var inverseLookAt = res * Matrix4x4.Translate(eye.X, eye.Y, eye.Z);

            return new Matrix4x4($"InverseLookAt({eye.X.ToEnString()},{eye.Y.ToEnString()},{eye.Z.ToEnString()},{forward.X.ToEnString()},{forward.Y.ToEnString()},{forward.Z.ToEnString()},{up.X.ToEnString()},{up.Y.ToEnString()},{up.Z.ToEnString()})", inverseLookAt.Values);

        }

        public static Matrix4x4 Transpose(Matrix4x4 matrix4x4)
        {
            float[] m = matrix4x4.Values;
            float[] r = new float[16];
            r[0] = m[0];
            r[1] = m[4];
            r[2] = m[8];
            r[3] = m[12];

            r[4] = m[1];
            r[5] = m[5];
            r[6] = m[9];
            r[7] = m[13];

            r[8] = m[2];
            r[9] = m[6];
            r[10] = m[10];
            r[11] = m[14];

            r[12] = m[3];
            r[13] = m[7];
            r[14] = m[11];
            r[15] = m[15];

            return new Matrix4x4($"Transpose({matrix4x4.Definition})", r);
        }

        //http://www.cg.info.hiroshima-cu.ac.jp/~miyazaki/knowledge/teche23.html
        public static Matrix4x4 Invert(Matrix4x4 matrix4x4)
        {
            float[] m = matrix4x4.Values;

            float determinant = m[0] * m[5] * m[10] * m[15] + m[0] * m[6] * m[11] * m[13] + m[0] * m[7] * m[9] * m[14]
                                + m[1] * m[4] * m[11] * m[14] + m[1] * m[6] * m[8] * m[15] + m[1] * m[7] * m[10] * m[12]
                                + m[2] * m[4] * m[9] * m[15] + m[2] * m[5] * m[11] * m[12] + m[2] * m[7] * m[8] * m[13]
                                + m[3] * m[4] * m[10] * m[13] + m[3] * m[5] * m[8] * m[14] + m[3] * m[6] * m[9] * m[12]
                                - m[0] * m[5] * m[11] * m[14] - m[0] * m[6] * m[9] * m[15] - m[0] * m[7] * m[10] * m[13]
                                - m[1] * m[4] * m[10] * m[15] - m[1] * m[6] * m[11] * m[12] - m[1] * m[7] * m[8] * m[14]
                                - m[2] * m[4] * m[11] * m[13] - m[2] * m[5] * m[8] * m[15] - m[2] * m[7] * m[9] * m[12]
                                - m[3] * m[4] * m[9] * m[14] - m[3] * m[5] * m[10] * m[12] - m[3] * m[6] * m[8] * m[13];
            if (determinant == 0) throw new Exception("Can not create inverse because determinant is zero");

            float b11 = m[5] * m[10] * m[15] + m[6] * m[11] * m[13] + m[7] * m[9] * m[14] - m[5] * m[11] * m[14] - m[6] * m[9] * m[15] - m[7] * m[10] * m[13];
            float b12 = m[1] * m[11] * m[14] + m[2] * m[9] * m[15] + m[3] * m[10] * m[13] - m[1] * m[10] * m[15] - m[2] * m[11] * m[13] - m[3] * m[9] * m[14];
            float b13 = m[1] * m[6] * m[15] + m[2] * m[7] * m[13] + m[3] * m[5] * m[14] - m[1] * m[7] * m[14] - m[2] * m[5] * m[15] - m[3] * m[6] * m[13];
            float b14 = m[1] * m[7] * m[10] + m[2] * m[5] * m[11] + m[3] * m[6] * m[9] - m[1] * m[6] * m[11] - m[2] * m[7] * m[9] - m[3] * m[5] * m[10];
            float b21 = m[4] * m[11] * m[14] + m[6] * m[8] * m[15] + m[7] * m[10] * m[12] - m[4] * m[10] * m[15] - m[6] * m[11] * m[12] - m[7] * m[8] * m[14];
            float b22 = m[0] * m[10] * m[15] + m[2] * m[11] * m[12] + m[3] * m[8] * m[14] - m[0] * m[11] * m[14] - m[2] * m[8] * m[15] - m[3] * m[10] * m[12];
            float b23 = m[0] * m[7] * m[14] + m[2] * m[4] * m[15] + m[3] * m[6] * m[12] - m[0] * m[6] * m[15] - m[2] * m[7] * m[12] - m[3] * m[4] * m[14];
            float b24 = m[0] * m[6] * m[11] + m[2] * m[7] * m[8] + m[3] * m[4] * m[10] - m[0] * m[7] * m[10] - m[2] * m[4] * m[11] - m[3] * m[6] * m[8];
            float b31 = m[4] * m[9] * m[15] + m[5] * m[11] * m[12] + m[7] * m[8] * m[13] - m[4] * m[11] * m[13] - m[5] * m[8] * m[15] - m[7] * m[9] * m[12];
            float b32 = m[0] * m[11] * m[13] + m[1] * m[8] * m[15] + m[3] * m[9] * m[12] - m[0] * m[9] * m[15] - m[1] * m[11] * m[12] - m[3] * m[8] * m[13];
            float b33 = m[0] * m[5] * m[15] + m[1] * m[7] * m[12] + m[3] * m[4] * m[13] - m[0] * m[7] * m[13] - m[1] * m[4] * m[15] - m[3] * m[5] * m[12];
            float b34 = m[0] * m[7] * m[9] + m[1] * m[4] * m[11] + m[3] * m[5] * m[8] - m[0] * m[5] * m[11] - m[1] * m[7] * m[8] - m[3] * m[4] * m[9];
            float b41 = m[4] * m[10] * m[13] + m[5] * m[8] * m[14] + m[6] * m[9] * m[12] - m[4] * m[9] * m[14] - m[5] * m[10] * m[12] - m[6] * m[8] * m[13];
            float b42 = m[0] * m[9] * m[14] + m[1] * m[10] * m[12] + m[2] * m[8] * m[13] - m[0] * m[10] * m[13] - m[1] * m[8] * m[14] - m[2] * m[9] * m[12];
            float b43 = m[0] * m[6] * m[13] + m[1] * m[4] * m[14] + m[2] * m[5] * m[12] - m[0] * m[5] * m[14] - m[1] * m[6] * m[12] - m[2] * m[4] * m[13];
            float b44 = m[0] * m[5] * m[10] + m[1] * m[6] * m[8] + m[2] * m[4] * m[9] - m[0] * m[6] * m[9] - m[1] * m[4] * m[10] - m[2] * m[5] * m[8];

            float[] inverse = {b11, b12, b13, b14,
                               b21, b22, b23, b24,
                               b31, b32, b33, b34,
                               b41, b42, b43, b44};

            float invDet = 1.0f / determinant;

            for (int i = 0; i < inverse.Length; i++) inverse[i] *= invDet;

            return new Matrix4x4($"Invert({matrix4x4.Definition})", inverse);
        }

        public static Matrix4x4 operator *(Matrix4x4 m1, Matrix4x4 m2)
        {
            float[] P1 = m1.Values;
            float[] P2 = m2.Values;

            if (P2.Length != 16 || P1.Length != 16) return null;
            float[] R = new float[16];

            R[0] = P2[0] * P1[0] + P2[4] * P1[1] + P2[8] * P1[2] + P2[12] * P1[3]; //1. Spaltenvektor
            R[1] = P2[1] * P1[0] + P2[5] * P1[1] + P2[9] * P1[2] + P2[13] * P1[3];
            R[2] = P2[2] * P1[0] + P2[6] * P1[1] + P2[10] * P1[2] + P2[14] * P1[3];
            R[3] = P2[3] * P1[0] + P2[7] * P1[1] + P2[11] * P1[2] + P2[15] * P1[3];

            R[4] = P2[0] * P1[4] + P2[4] * P1[5] + P2[8] * P1[6] + P2[12] * P1[7]; //2. Spaltenvektor
            R[5] = P2[1] * P1[4] + P2[5] * P1[5] + P2[9] * P1[6] + P2[13] * P1[7];
            R[6] = P2[2] * P1[4] + P2[6] * P1[5] + P2[10] * P1[6] + P2[14] * P1[7];
            R[7] = P2[3] * P1[4] + P2[7] * P1[5] + P2[11] * P1[6] + P2[15] * P1[7];

            R[8] = P2[0] * P1[8] + P2[4] * P1[9] + P2[8] * P1[10] + P2[12] * P1[11]; //3. Spaltenvektor
            R[9] = P2[1] * P1[8] + P2[5] * P1[9] + P2[9] * P1[10] + P2[13] * P1[11];
            R[10] = P2[2] * P1[8] + P2[6] * P1[9] + P2[10] * P1[10] + P2[14] * P1[11];
            R[11] = P2[3] * P1[8] + P2[7] * P1[9] + P2[11] * P1[10] + P2[15] * P1[11];

            R[12] = P2[0] * P1[12] + P2[4] * P1[13] + P2[8] * P1[14] + P2[12] * P1[15];//4. Spaltenvektor
            R[13] = P2[1] * P1[12] + P2[5] * P1[13] + P2[9] * P1[14] + P2[13] * P1[15];
            R[14] = P2[2] * P1[12] + P2[6] * P1[13] + P2[10] * P1[14] + P2[14] * P1[15];
            R[15] = P2[3] * P1[12] + P2[7] * P1[13] + P2[11] * P1[14] + P2[15] * P1[15];

            return new Matrix4x4($"Mult({m1.Definition}, {m2.Definition})", R);
        }

        public static Vector4D operator *(Matrix4x4 matrix, Vector4D vec4)
        {
            var m = matrix.Values;
            return new Vector4D(m[0] * vec4.X + m[4] * vec4.Y + m[8] *  vec4.Z + m[12] * vec4.W,
                              m[1] * vec4.X + m[5] * vec4.Y + m[9] *  vec4.Z + m[13] * vec4.W,
                              m[2] * vec4.X + m[6] * vec4.Y + m[10] * vec4.Z + m[14] * vec4.W,
                              m[3] * vec4.X + m[7] * vec4.Y + m[11] * vec4.Z + m[15] * vec4.W);
        }

        //Diese Funktion dient zur Transformation von RICHTUNGS-VEKTOREN. Für Positionstransformation bitte MatVMult mit 1 als letzten Parameter aufrufen
        public static Vector3D MultDirection(Matrix4x4 matrix, Vector3D direction)
        {
            var m = matrix.Values;
            Vector3D res = new Vector3D(m[0] * direction.X + m[4] * direction.Y + m[8] * direction.Z,
                                        m[1] * direction.X + m[5] * direction.Y + m[9] * direction.Z,
                                        m[2] * direction.X + m[6] * direction.Y + m[10] * direction.Z);
            return res;
        }

        public static Vector3D MultPosition(Matrix4x4 matrix, Vector3D position)
        {
            var m = matrix.Values;
            Vector3D res = new Vector3D(m[0] * position.X + m[4] * position.Y + m[8] * position.Z + m[12],
                                    m[1] * position.X + m[5] * position.Y + m[9] * position.Z + m[13],
                                    m[2] * position.X + m[6] * position.Y + m[10] * position.Z + m[14]);
            return res;
        }

        public static Matrix4x4 Model(Vector3D position, Vector3D orientation, float size)
        {
            var matrix = Matrix4x4.Ident();

            matrix = Matrix4x4.Translate(position.X, position.Y, position.Z) * matrix;
            matrix = Matrix4x4.Rotate(orientation.X, 1.0f, 0.0f, 0.0f) * matrix;
            matrix = Matrix4x4.Rotate(orientation.Y, 0.0f, 1.0f, 0.0f) * matrix;
            matrix = Matrix4x4.Rotate(orientation.Z, 0.0f, 0.0f, 1.0f) * matrix;
            matrix = Matrix4x4.Scale(size, size, size) * matrix;

            return new Matrix4x4($"Model({position.X.ToEnString()},{position.Y.ToEnString()},{position.Z.ToEnString()},{orientation.X.ToEnString()},{orientation.Y.ToEnString()},{orientation.Z.ToEnString()},{size.ToEnString()})", matrix.Values);
        }

        //Zum rotieren der normalen von Objekt- in Welt-Koordianten
        public static Matrix4x4 NormalRotate(Vector3D orientation)
        {
            var matrix = Matrix4x4.Ident();

            matrix = Matrix4x4.Rotate(orientation.X, 1.0f, 0.0f, 0.0f) * matrix;
            matrix = Matrix4x4.Rotate(orientation.Y, 0.0f, 1.0f, 0.0f) * matrix;
            matrix = Matrix4x4.Rotate(orientation.Z, 0.0f, 0.0f, 1.0f) * matrix;

            return new Matrix4x4($"NormalRotate({orientation.X.ToEnString()},{orientation.Y.ToEnString()},{orientation.Z.ToEnString()})", matrix.Values);
        }

        public static Matrix4x4 InverseModel(Vector3D position, Vector3D orientation, float size)
        {
            var matrix = Matrix4x4.Ident();

            matrix = Matrix4x4.Scale(1.0f / size, 1.0f / size, 1.0f / size) * matrix;
            matrix = Matrix4x4.Rotate(-orientation.Z, 0.0f, 0.0f, 1.0f) * matrix;
            matrix = Matrix4x4.Rotate(-orientation.Y, 0.0f, 1.0f, 0.0f) * matrix;
            matrix = Matrix4x4.Rotate(-orientation.X, 1.0f, 0.0f, 0.0f) * matrix;
            matrix = Matrix4x4.Translate(-position.X, -position.Y, -position.Z) * matrix;

            return new Matrix4x4($"InverseModel({position.X.ToEnString()},{position.Y.ToEnString()},{position.Z.ToEnString()},{orientation.X.ToEnString()},{orientation.Y.ToEnString()},{orientation.Z.ToEnString()},{size.ToEnString()})", matrix.Values);
        }

        public static Matrix4x4 InverseNormalRotate(Vector3D orientation)
        {
            var matrix = Matrix4x4.Ident();

            matrix = Matrix4x4.Rotate(-orientation.Z, 0.0f, 0.0f, 1.0f) * matrix;
            matrix = Matrix4x4.Rotate(-orientation.Y, 0.0f, 1.0f, 0.0f) * matrix;
            matrix = Matrix4x4.Rotate(-orientation.X, 1.0f, 0.0f, 0.0f) * matrix;

            return new Matrix4x4($"InverseNormalRotate({orientation.X.ToEnString()},{orientation.Y.ToEnString()},{orientation.Z.ToEnString()})", matrix.Values);
        }

        //Quelle: http://wiki.delphigl.com/index.php/glOrtho
        //procedure glOrtho(left, right, bottom, top, znear, zfar : double);
        public static Matrix4x4 ProjectionMatrixOrtho(float left, float right, float bottom, float top, float znear, float zfar)
        {
            return new Matrix4x4($"ProjectionMatrixOrtho({left.ToEnString()},{right.ToEnString()},{bottom.ToEnString()},{top.ToEnString()},{znear.ToEnString()},{zfar.ToEnString()})",
                new float[] {2 / (right - left),               0,                                0,                                  0,
                             0,                                2 / (top - bottom),               0,                                  0,
                             0,                                0,                                -2 / (zfar - znear),                0,
                             -(right + left) / (right - left), -(top + bottom) / (top - bottom), -(zfar + znear) / (zfar - znear),   1});
        }

        //Quelle: http://wiki.delphigl.com/index.php/gluPerspective
        //procedure gluPerspective(fovy, aspect, zNear, zFar : glDouble);
        public static Matrix4x4 ProjectionMatrixPerspective(float fovy/*Angabe in Grad: 0 bis 180*/, float aspect, float zNear, float zFar)
        {
            float f = (float)(1 / Math.Tan(fovy * Math.PI / 180 / 2));

            return new Matrix4x4($"ProjectionMatrixPerspective({fovy.ToEnString()},{aspect.ToEnString()},{zNear.ToEnString()},{zFar.ToEnString()})",
                new float[] {f / aspect, 0, 0,                                  0,
                             0,          f, 0,                                  0,
                             0,          0, (zFar + zNear) / (zNear - zFar),    -1,
                             0,          0, (2 * zFar * zNear) / (zNear - zFar), 0});
        }

        /// <summary>
        /// Erzeugt eine Modelviewmatrix für ein Objekt, was an der Stelle 'objektPos' sich befindet und immer zur Kamera (angegeben durch 'kameraMatrix') zeigt
        /// </summary>
        /// <param name="objektPos"></param>
        /// <param name="cameraMatrix"></param>
        /// <returns>Diese Matrix muss nur noch mit der Kameramatrix mulipliziert werden, um von Objekt- in Augkoordinaten transformiren zu können</returns>
        public static Matrix4x4 BilboardMatrixFromCameraMatrix(Vector3D objektPos, Vector3D orientation, float objektSize, Matrix4x4 cameraMatrix)
        {
            Vector3D camPos = new Vector3D(0, 0, 0);
            Vector3D camUp = new Vector3D(0, 0, 0);
            Matrix4x4.GetCameraPosAndUp(cameraMatrix, ref camPos, ref camUp);

            // create the look vector: pos -> camPos
            Vector3D look = camPos - objektPos;
            look = Vector3D.Normalize(look);
            // right hand rule cross products
            Vector3D right = Vector3D.Cross(camUp, look);
            Vector3D up = Vector3D.Cross(look, right);

            var matrix = Matrix4x4.Ident();
            matrix = Matrix4x4.CreateBillboardMatrix(right, up, look, objektPos) * matrix;
            matrix = Matrix4x4.Rotate(orientation.X, 1.0f, 0.0f, 0.0f) * matrix;
            matrix = Matrix4x4.Rotate(orientation.Y, 0.0f, 1.0f, 0.0f) * matrix;
            matrix = Matrix4x4.Rotate(orientation.Z, 0.0f, 0.0f, 1.0f) * matrix;
            matrix = Matrix4x4.Scale(objektSize, objektSize, objektSize) * matrix;

            return new Matrix4x4($"BilboardMatrixFromCameraMatrix({objektPos.X.ToEnString()},{objektPos.Y.ToEnString()},{objektPos.Z.ToEnString()},{orientation.X.ToEnString()},{orientation.Y.ToEnString()},{orientation.Z.ToEnString()},{objektSize.ToEnString()},{cameraMatrix.Definition})", matrix.Values);
        }

        //Die Kameraposition und den KameraUP-Vektor aus der Modelviewmatrix nach aufruf von gluLookAt bestimmen
        //Quelle: http://nehe.gamedev.net/data/articles/article.asp?article=19
        //*****************************************************************************
        // This only needs to be done once for all the particles, because the view
        // matrix does not change any between them.  If you were to modify the view
        // matrix at all, you would need to recalculate the camera pos and up vector.
        //*****************************************************************************
        private static void GetCameraPosAndUp(Matrix4x4 matrix, ref Vector3D camPos, ref Vector3D camUp)
        {
            float[] mv_matrix = matrix.Values;
            if (mv_matrix.Length != 16) return;

            mv_matrix = (float[])mv_matrix.Clone();

            // The values in the view matrix for the camera are the negative values of
            // the camera. This is because of the way gluLookAt works; I'm don't fully
            // understand why gluLookAt does what it does, but I know doing this works:)
            // I know that gluLookAt creates a the look vector as (eye - center), the
            // resulting direction vector is a vector from center to eye (the oposite
            // of what are view direction really is).
            camPos = new Vector3D(-mv_matrix[12], -mv_matrix[13], -mv_matrix[14]);
            camUp = new Vector3D(mv_matrix[1], mv_matrix[5], mv_matrix[9]);

            // zero the translation in the matrix, so we can use the matrix to transform
            // camera postion to world coordinates using the view matrix
            mv_matrix[12] = mv_matrix[13] = mv_matrix[14] = 0;

            // the view matrix is how to get to the gluLookAt pos from what we gave as
            // input for the camera position, so to go the other way we need to reverse
            // the rotation.  Transposing the matrix will do this.
            var revertRotation = Matrix4x4.Transpose(new Matrix4x4(null, mv_matrix));

            // get the correct position of the camera in world space
            camPos = Matrix4x4.MultPosition(revertRotation, camPos);
        }

        //Die Billboardmatrix aus den Kameravektoren und der Billboardposition bestimmen
        //Multipliziert man die Billboardmatrix mit der Modelviewmatrix der Kamera, zeigt das Objekt immer zum Betrachter
        //Quelle: http://nehe.gamedev.net/data/articles/article.asp?article=19
        //*****************************************************************************
        // Create the billboard matrix: a rotation matrix created from an arbitrary set
        // of axis.  Store those axis values in the first 3 columns of the matrix.  Col
        // 1 is the X axis, col 2 is the Y axis, and col 3 is the Z axis.  We are
        // rotating right into X, up into Y, and look into Z.  The rotation matrix
        // created from the rows will translate the arbitrary axis set to the global
        // axis set.  Lastly, OpenGl stores the matrices by columns, so enter the data
        // into the array columns first.
        //*****************************************************************************
        private static Matrix4x4 CreateBillboardMatrix(Vector3D right, Vector3D up, Vector3D look, Vector3D pos)
        {
            float[] bbmat = new float[16];
            bbmat[0] = right.X;
            bbmat[1] = right.Y;
            bbmat[2] = right.Z;
            bbmat[3] = 0;
            bbmat[4] = up.X;
            bbmat[5] = up.Y;
            bbmat[6] = up.Z;
            bbmat[7] = 0;
            bbmat[8] = look.X;
            bbmat[9] = look.Y;
            bbmat[10] = look.Z;
            bbmat[11] = 0;
            // Add the translation in as well.
            bbmat[12] = pos.X;
            bbmat[13] = pos.Y;
            bbmat[14] = pos.Z;
            bbmat[15] = 1;

            return new Matrix4x4(null, bbmat);
        }

        //Mit der TBN-Matrix kann man Richtungsvektoren von Tangent- in Objektkoordinaten transformieren
        public static Matrix4x4 TBNMatrix(Vector3D normal, Vector3D tangent)
        {
            Vector3D N = normal;
            Vector3D T = Vector3D.Normalize(tangent);
            Vector3D B = Vector3D.Normalize(Vector3D.Cross(T, N));
            return new Matrix4x4($"TBNMatrix({normal.X.ToEnString()},{normal.Y.ToEnString()},{normal.Z.ToEnString()},{tangent.X.ToEnString()},{tangent.Y.ToEnString()},{tangent.Z.ToEnString()})", new float[] {
                                        T.X, T.Y, T.Z, 0,
                                        B.X, B.Y, B.Z, 0,
                                        N.X, N.Y, N.Z, 0,
                                        0,   0,   0,   0});
        }

    }
}
