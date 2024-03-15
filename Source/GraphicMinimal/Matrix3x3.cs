using System;
using System.Text;

namespace GraphicMinimal
{
    public class Matrix3x3
    {
        public Matrix3x3() { }
        private Matrix3x3(string definition, float[] values)
        {
            this.definition = definition;
            this.Values = values;
        }

        private string definition;
        public string Definition { get => this.definition; set { this.definition = value; this.Values = FromDefinitionString(value).Values; } } //Damit ich die Matrix besser in Json-Serialisierter Form modifizieren/definieren kann
        public float[] Values { get; private set; } //nehme ich ein String und kein float-Array zur Darstellung

        public static Matrix3x3 FromDefinitionString(string definition)
        {
            var parse = DefinitionStringParser.Parse(definition);

            switch (parse.FunctionName)
            {
                case "Ident":
                    return Matrix3x3.Ident();
                case "Translate":
                    return Matrix3x3.Translate(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle());
                case "Scale":
                    return Matrix3x3.Scale(parse.Parameter[0].ToSingle(), parse.Parameter[1].ToSingle());
                case "Rotate":
                    return Matrix3x3.Rotate(parse.Parameter[0].ToSingle());
                case "Transpose":
                    return Matrix3x3.Transpose(Matrix3x3.FromDefinitionString((parse.Parameter[0] as DefinitionStringParserResult).DefinitionString));
                case "Invert":
                    return Matrix3x3.Invert(Matrix3x3.FromDefinitionString((parse.Parameter[0] as DefinitionStringParserResult).DefinitionString));
                case "Mult":
                    return Matrix3x3.FromDefinitionString((parse.Parameter[0] as DefinitionStringParserResult).DefinitionString) * Matrix3x3.FromDefinitionString((parse.Parameter[1] as DefinitionStringParserResult).DefinitionString);
                case "SpriteMatrix":
                    if (parse.Parameter.Length == 4)
                        return Matrix3x3.SpriteMatrix(Convert.ToInt32(parse.Parameter[0]), Convert.ToInt32(parse.Parameter[1]), Convert.ToInt32(parse.Parameter[2]), Convert.ToInt32(parse.Parameter[3]));
                    else
                        return Matrix3x3.SpriteMatrix(Convert.ToInt32(parse.Parameter[0]), Convert.ToInt32(parse.Parameter[1]), Convert.ToInt32(parse.Parameter[2]));

            }

            throw new ArgumentException($"Unknown Functionname {parse.FunctionName}");
        }

        public string Print()
        {
            float[] matrix = this.Values;

            if (matrix.Length != 9) return "Error: Matrix.Length != 9";
            StringBuilder str = new StringBuilder();
            for (int y = 0; y < 3; y++)
            {
                str.Append("|");
                for (int x = 0; x < 3; x++)
                {
                    str.Append(String.Format("{0:+0.00;-0.00; 0.00}", matrix[y * 3 + x]));
                    if (x < 2) str.Append("\t");
                }
                str.Append("|" + System.Environment.NewLine);
            }

            return str.ToString();
        }

        public static Matrix3x3 Ident()
        {
            return new Matrix3x3("Ident()", new float[] {1, 0, 0,
                                                         0, 1, 0,
                                                         0, 0, 1});
        }

        public static Matrix3x3 Translate(float x, float y)
        {
            return new Matrix3x3($"Translate({x.ToEnString()},{y.ToEnString()})", new float[] {1, 0, 0,
                                                                                               0, 1, 0,
                                                                                               x, y, 1});
        }

        public static Matrix3x3 Scale(float x, float y)
        {
            return new Matrix3x3($"Scale({x.ToEnString()},{y.ToEnString()})", new float[] {x, 0, 0,
                                                                                           0, y, 0,
                                                                                           0, 0, 1});
        }

        public static Matrix3x3 Rotate(float angle)
        {
            float c = (float)Math.Cos(angle * Math.PI / 180);
            float s = (float)Math.Sin(angle * Math.PI / 180);

            return new Matrix3x3($"Rotate({angle.ToEnString()})", new float[] {c,  s,  0,
                                                                               -s, c,  0,
                                                                               0,  0,  1});
        }

        public static Matrix3x3 Transpose(Matrix3x3 matrix3x3)
        {
            float[] m = matrix3x3.Values;
            return new Matrix3x3($"Transpose({matrix3x3.Definition})", new float[] {m[0], m[3], m[6],
                                                                                    m[1], m[4], m[7],
                                                                                    m[2], m[5], m[8]});
        }

        //http://www.cg.info.hiroshima-cu.ac.jp/~miyazaki/knowledge/teche23.html
        public static Matrix3x3 Invert(Matrix3x3 matrix3x3)
        {
            float[] m = matrix3x3.Values;
            
            float determinant = m[0] * m[4] * m[8] + m[3] * m[7] * m[2] + m[6] * m[1] * m[5] - m[0] * m[7] * m[5] - m[6] * m[4] * m[2] - m[3] * m[1] * m[8];

            if (determinant == 0) throw new Exception("Can not create inverse because determinant is zero");

            float[] inverse = new float[]
            {
                m[4] * m[8]-m[5] * m[7],    m[2]*m[7]-m[1]*m[8],    m[1]*m[5]-m[2]*m[4],
                m[5]*m[6]-m[3]*m[8],        m[0]*m[8]-m[2]*m[6],    m[2]*m[3]-m[0]*m[5],
                m[3]*m[7]-m[4]*m[6],        m[1]*m[6]-m[0]*m[7],    m[0]*m[4]-m[1]*m[3]
            };

            float invDet = 1.0f / determinant;

            for (int i = 0; i < inverse.Length; i++) inverse[i] *= invDet;

            return new Matrix3x3($"Invert({matrix3x3.Definition})", inverse);
        }

        //Return=m1*m2
        public static Matrix3x3 operator *(Matrix3x3 m1, Matrix3x3 m2)
        {
            float[] P1 = m1.Values;
            float[] P2 = m2.Values;

            if (P2.Length != 9 || P1.Length != 9) return null;
            float[] R = new float[9];

            //1. Zeile von R
            R[0] = P1[0] * P2[0] + P1[1] * P2[3] + P1[2] * P2[6];
            R[1] = P1[0] * P2[1] + P1[1] * P2[4] + P1[2] * P2[7];
            R[2] = P1[0] * P2[2] + P1[1] * P2[5] + P1[2] * P2[8];

            //2. Zeile von R
            R[3] = P1[3] * P2[0] + P1[4] * P2[3] + P1[5] * P2[6];
            R[4] = P1[3] * P2[1] + P1[4] * P2[4] + P1[5] * P2[7];
            R[5] = P1[3] * P2[2] + P1[4] * P2[5] + P1[5] * P2[8];

            //3. Zeile von R
            R[6] = P1[6] * P2[0] + P1[7] * P2[3] + P1[8] * P2[6];
            R[7] = P1[6] * P2[1] + P1[7] * P2[4] + P1[8] * P2[7];
            R[8] = P1[6] * P2[2] + P1[7] * P2[5] + P1[8] * P2[8];

            return new Matrix3x3($"Mult({m1.Definition}, {m2.Definition})", R);
        }


        //Return matrix*v
        public static Vector3D operator *(Matrix3x3 matrix, Vector3D v)
        {
            float[] m = matrix.Values;

            float x = m[0] * v.X + m[3] * v.Y + m[6] * v.Z;
            float y = m[1] * v.X + m[4] * v.Y + m[7] * v.Z;
            float z = m[2] * v.X + m[5] * v.Y + m[8] * v.Z;

            return new Vector3D(x,y,z);
        }

        //Achtung: Die Matrzien-Operationen werden rückwärts ausgeführt. Wenn ich zuerst verschieben will und danach
        //skalieren, so muss ich Scale*Translate schreiben.
        //Das bedeutet der Matrix-Stack für 3D lautet 
        //ObjToEyeSpace = ScaleModel*RotateModel*TranslateModel*Camera*Projection
        public static Matrix3x3 ModelMatrix(Vector2D position, float angle, Vector2D scale)
        {
            Matrix3x3 matrix = Ident();
            matrix = Translate(position.X, position.Y) * matrix;
            matrix = Rotate(angle) * matrix;
            matrix = Scale(scale.X, scale.Y) * matrix;
            return matrix;
        }

        //Ich habe ein Bild aus xCount*yCount Kästchen und ich will nun nur das Kästchen mit Index x/y angeben
        //x = 0..xCount - 1
        //y = 0..yCount - 1
        public static Matrix3x3 SpriteMatrix(int xCount, int yCount, int x, int y)
        {
            Vector2D scale = new Vector2D(1.0f / xCount, 1.0f / yCount);    //Skaliere das Bild zuerst klein so das nur der linke obere Bildausschnitt zu sehen ist
            Vector2D translate = new Vector2D(x, y);  //Verschiebe dann den kleinen Bildausschnitt zu der Position (x,y)
            Matrix3x3 m = Translate(translate.X, translate.Y) * Scale(scale.X, scale.Y);

            return new Matrix3x3($"SpriteMatrix({xCount}, {yCount}, {x}, {y})", m.Values);
        }

        //Ich habe ein Bild aus xCount*yCount Kästchen und ich will nun nur das Kästchen mit index = x + y*xCount
        //index = 0..xCount * yCount (Sollte index außerhalb des 0...xCount * yCount-Bereichs liegen, wird es per Modulo in Form gebracht
        public static Matrix3x3 SpriteMatrix(int xCount, int yCount, int index)
        {
            int max = xCount * yCount;
            index = index % max;
            if (index < 0) index = (max + index) % max;

            Matrix3x3 m = SpriteMatrix(xCount, yCount, index % xCount, index / xCount);

            return new Matrix3x3($"SpriteMatrix({xCount}, {yCount}, {index})", m.Values);
        }

        public static Matrix4x4 Get4x4Matrix(Matrix3x3 matrix3x3)
        {
            float[] m = matrix3x3.Values;
            return new Matrix4x4(new float[] {m[0], m[1], m[2], 0,
                                              m[3], m[4], m[5], 0,
                                              m[6], m[7], m[8], 0,
                                              0,    0,    0,    0});
        }

        public static Vector2D GetTexturScale(Matrix3x3 textureMatrix)
        {
            Vector2D p00 = (textureMatrix * new Vector3D(0, 0, 1)).XY;
            Vector2D p01 = (textureMatrix * new Vector3D(0, 1, 1)).XY;
            Vector2D p10 = (textureMatrix * new Vector3D(1, 0, 1)).XY;

            return new Vector2D((p10 - p00).Length(), (p01 - p00).Length());
        }

        public static Vector2D MultDirection(Matrix3x3 matrix, Vector2D direction)
        {
            var m = matrix.Values;
            Vector2D res = new Vector2D(m[0] * direction.X + m[3] * direction.Y,
                                        m[1] * direction.X + m[4] * direction.Y);
            return res;
        }

        public static Vector2D MultPosition(Matrix3x3 matrix, Vector2D position)
        {
            var m = matrix.Values;
            Vector2D res = new Vector2D(m[0] * position.X + m[3] * position.Y + m[6],
                                        m[1] * position.X + m[4] * position.Y + m[7]);
            return res;
        }

        public static float GetSizeFactorFromMatrix(Matrix3x3 matrix)
        {
            var p1 = Matrix3x3.MultPosition(matrix, new Vector2D(0, 0));
            var p2 = Matrix3x3.MultPosition(matrix, new Vector2D(1, 0));
            return (p2 - p1).Length();
        }

        public static float GetAngleInDegreeFromMatrix(Matrix3x3 matrix)
        {
            var p1 = Matrix3x3.MultPosition(matrix, new Vector2D(0, 0));
            var p2 = Matrix3x3.MultPosition(matrix, new Vector2D(1, 0));
            return Vector2D.Angle360(new Vector2D(1, 0), p2 - p1);
        }

        public static Vector2D GetTranslationVectorFromMatrix(Matrix3x3 matrix)
        {
            return Matrix3x3.MultPosition(matrix, new Vector2D(0, 0));
        }
    }
}
