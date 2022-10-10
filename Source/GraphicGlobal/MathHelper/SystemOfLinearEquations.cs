using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphicGlobal.MathHelper
{
    public static class SystemOfLinearEquations
    {
        //Gegeben: coefficientsMatrix, constants
        //Gesucht: a, b
        //coefficientsMatrix[0] * a + coefficientsMatrix[1] * b = constants[0]
        //coefficientsMatrix[2] * a + coefficientsMatrix[3] * b = constants[1]
        //coefficientsMatrix[4] * a + coefficientsMatrix[5] * b = constants[2]
        //Quelle: http://www.arndt-bruenner.de/mathe/scripts/gleichungssysteme.htm
        public static bool SolveABWithThreeLines(out float a, out float b, float[] coefficientsMatrix, float[] constants)
        {
            if (SolveABWithTwoLines(out a, out b, new float[] { coefficientsMatrix[0], coefficientsMatrix[1],
                                                                coefficientsMatrix[2], coefficientsMatrix[3]},
                                                   new float[] {constants[0], constants[1] })) return true;

            if (SolveABWithTwoLines(out a, out b, new float[] { coefficientsMatrix[0], coefficientsMatrix[1],
                                                                coefficientsMatrix[4], coefficientsMatrix[5]},
                                                   new float[] {constants[0], constants[2] })) return true;

            if (SolveABWithTwoLines(out a, out b, new float[] { coefficientsMatrix[2], coefficientsMatrix[3],
                                                                coefficientsMatrix[4], coefficientsMatrix[5]},
                                                   new float[] {constants[1], constants[2] })) return true;
            a = 0; b = 0;
            return false;

        }

        //Gegeben: coefficientsMatrix, constants
        //Gesucht: a, b
        //coefficientsMatrix[0] * a + coefficientsMatrix[1] * b = constants[0]
        //coefficientsMatrix[2] * a + coefficientsMatrix[3] * b = constants[1]
        //Quelle: http://www.arndt-bruenner.de/mathe/scripts/gleichungssysteme.htm
        public static bool SolveABWithTwoLines(out float a, out float b, float[] coefficientsMatrix, float[] constants)
        {
            try
            {
                //Forme: 1  .. = .. 
                //       .. .. = ..
                coefficientsMatrix[1] /= coefficientsMatrix[0];
                constants[0] /= coefficientsMatrix[0];
                coefficientsMatrix[0] = 1;

                //Forme: 1  .. = .. 
                //       0  .. = ..
                coefficientsMatrix[3] += coefficientsMatrix[1] * (-coefficientsMatrix[2]);
                constants[1] += constants[0] * (-coefficientsMatrix[2]);
                coefficientsMatrix[2] = 0;

                //Forme: 1  .. = .. 
                //       0  1  = ..
                constants[1] /= coefficientsMatrix[3];

                //Forme: 1  0  = .. 
                //       0  1  = ..
                constants[0] += constants[1] * (-coefficientsMatrix[1]);

                a = constants[0];
                b = constants[1];
            }
            catch (Exception)
            {
                a = 0; b = 0;
                return false;
            }

            if (float.IsNaN(a) || float.IsNaN(b))
            {
                a = 0; b = 0;
                return false;
            }

            return true;
        }

        //Vertauscht die Zeilen von den beiden Matrizen in die angegebene Reihenfolge
        private static bool SwapLines(float[] coefficientsMatrix, float[] constants, string newLinePositions)
        {
            Dictionary<char, int[]> map = new Dictionary<char, int[]>()
             {
                 {'1', new int[]{0,1,2,0}},//ZeilenNummer - Indizes für diese Zeile um daraus Daten aus koefMatrix und konstanten zu lesen
                 {'2', new int[]{3,4,5,1}},
                 {'3', new int[]{6,7,8,2}},
             };

            float[] old = coefficientsMatrix.ToArray();
            float[] oldConstants = constants.ToArray();

            if (Math.Abs(coefficientsMatrix[map[newLinePositions[0]][0]]) < 0.001 || Math.Abs(coefficientsMatrix[map[newLinePositions[1]][1]]) < 0.001 || Math.Abs(coefficientsMatrix[map[newLinePositions[2]][2]]) < 0.001)
            {
                return false;
            }

            coefficientsMatrix[0] = old[map[newLinePositions[0]][0]]; coefficientsMatrix[1] = old[map[newLinePositions[0]][1]]; coefficientsMatrix[2] = old[map[newLinePositions[0]][2]]; constants[0] = oldConstants[map[newLinePositions[0]][3]];
            coefficientsMatrix[3] = old[map[newLinePositions[1]][0]]; coefficientsMatrix[4] = old[map[newLinePositions[1]][1]]; coefficientsMatrix[5] = old[map[newLinePositions[1]][2]]; constants[1] = oldConstants[map[newLinePositions[1]][3]];
            coefficientsMatrix[6] = old[map[newLinePositions[2]][0]]; coefficientsMatrix[7] = old[map[newLinePositions[2]][1]]; coefficientsMatrix[8] = old[map[newLinePositions[2]][2]]; constants[2] = oldConstants[map[newLinePositions[2]][3]];

            return true;
        }

        //Gegeben: k1, k2, k3, k4
        //Gesucht: v
        //
        //k1 * v.x + k2 * v.y + k3 * v.z = k4
        //
        //k1.x * v.x + k2.x * v.y + k3.x * v.z = k4.x
        //k1.y * v.x + k2.y * v.y + k3.y * v.z = k4.y
        //k1.z * v.x + k2.z * v.y + k3.z * v.z = k4.z
        //Quelle: http://www.arndt-bruenner.de/mathe/scripts/gleichungssysteme.htm
        //Gibt Null zurück, wenn Lösung nicht findbar ist. Das passiert dann, wenn die Determinante 0 ist
        //Beispiel für Fälle, wo die Determinante 0 ist
        //Vector3D solution1 = SystemOfLinearEquations.SolveABCWithThreeLines(new Vector3D(-0.236523628f, 0.000000238418579f, -0.00001591444f), new Vector3D(0.0f, 0.195924044f, -0.0000000596046448f), new Vector3D(0.373314947f, -0.9277047f, 0.0000254006427f), new Vector3D(-0.118261814f, -0.293885827f, -0.00000780820847f));
        //Vector3D solution2 = SystemOfLinearEquations.SolveABCWithThreeLines(new Vector3D(-0.220952749f, 0.0f, 0.0f), new Vector3D(0.0f, -0.275817543f, 0.0f), new Vector3D(-0.0000006084467f, -1.0f, 0.0f), new Vector3D(-0.3492174f, 0.359116226f, 1.25557184f));
        public static Vector3D SolveABCWithThreeLines(Vector3D k1, Vector3D k2, Vector3D k3, Vector3D k4)
        {
            float a, b, c;
            if (SolveABCWithThreeLines(out a, out b, out c, new float[]
            {
                (float)k1.X,(float) k2.X, (float)k3.X,
                (float)k1.Y, (float)k2.Y, (float)k3.Y,
                (float)k1.Z, (float)k2.Z, (float)k3.Z
            },
            new float[]
            {
               (float)k4.X, (float)k4.Y, (float)k4.Z
            }) == false) return null;

            return new Vector3D(a, b, c);
        }

        //Gegeben: coefficientsMatrix, constants
        //Gesucht: a, b, c
        //coefficientsMatrix[0] * a + coefficientsMatrix[1] * b + coefficientsMatrix[2] * c = constants[0]
        //coefficientsMatrix[3] * a + coefficientsMatrix[4] * b + coefficientsMatrix[5] * c = constants[1]
        //coefficientsMatrix[6] * a + coefficientsMatrix[7] * b + coefficientsMatrix[8] * c = constants[2]
        //Quelle: http://www.arndt-bruenner.de/mathe/scripts/gleichungssysteme.htm
        private static bool SolveABCWithThreeLines(out float a, out float b, out float c, float[] coefficientsMatrix, float[] constants)
        {
            try
            {
                //Schritt 1: Kontrolliere, das in der Hauptdiagonale nirgentwo eine 0 steht
                if (coefficientsMatrix[0] == 0 || coefficientsMatrix[4] == 0 || coefficientsMatrix[8] == 0)
                {
                    do
                    {
                        if (SwapLines(coefficientsMatrix, constants, "132")) break;
                        if (SwapLines(coefficientsMatrix, constants, "213")) break;
                        if (SwapLines(coefficientsMatrix, constants, "312")) break;
                        if (SwapLines(coefficientsMatrix, constants, "231")) break;
                        if (SwapLines(coefficientsMatrix, constants, "321")) break;
                    } while (false);
                }

                float determinant = Determinant(coefficientsMatrix);
                string s = determinant.ToString(); //Ein Gleichungssystem ist nur dann Lösbar, wenn die Determinante ungleich 0 ist

                //Forme: 1  .. .. = .. 
                //       .. .. .. = ..
                //       .. .. .. = ..
                coefficientsMatrix[1] /= coefficientsMatrix[0];
                coefficientsMatrix[2] /= coefficientsMatrix[0];
                constants[0] /= coefficientsMatrix[0];
                coefficientsMatrix[0] = 1;

                //Forme: 1 .. .. = .. 
                //       0 .. .. = ..
                //       0 .. .. = ..
                coefficientsMatrix[4] += coefficientsMatrix[1] * (-coefficientsMatrix[3]);
                coefficientsMatrix[5] += coefficientsMatrix[2] * (-coefficientsMatrix[3]);
                constants[1] += constants[0] * (-coefficientsMatrix[3]);

                coefficientsMatrix[7] += coefficientsMatrix[1] * (-coefficientsMatrix[6]);
                coefficientsMatrix[8] += coefficientsMatrix[2] * (-coefficientsMatrix[6]);
                constants[2] += constants[0] * (-coefficientsMatrix[6]);

                coefficientsMatrix[3] = 0;
                coefficientsMatrix[6] = 0;

                //Forme: 1  .. .. = .. 
                //       0  1  .. = ..
                //       0  .. .. = ..
                coefficientsMatrix[5] /= coefficientsMatrix[4];
                constants[1] /= coefficientsMatrix[4];
                coefficientsMatrix[4] = 1;

                //Forme: 1  0 .. = .. 
                //       0  1 .. = ..
                //       0  0 .. = ..
                coefficientsMatrix[2] += coefficientsMatrix[5] * (-coefficientsMatrix[1]);
                constants[0] += constants[1] * (-coefficientsMatrix[1]);

                coefficientsMatrix[8] += coefficientsMatrix[5] * (-coefficientsMatrix[7]);
                constants[2] += constants[1] * (-coefficientsMatrix[7]);

                coefficientsMatrix[1] = 0;
                coefficientsMatrix[7] = 0;

                //Forme: 1  0 .. = .. 
                //       0  1 .. = ..
                //       0  0 1  = ..
                constants[2] /= coefficientsMatrix[8];
                coefficientsMatrix[8] = 1;

                //Forme: 1  0  0 = .. 
                //       0  1  0 = ..
                //       0  0  1 = ..
                constants[0] += constants[2] * (-coefficientsMatrix[2]);
                constants[1] += constants[2] * (-coefficientsMatrix[5]);

                a = constants[0];
                b = constants[1];
                c = constants[2];

                if (float.IsInfinity(a) || float.IsInfinity(b) || float.IsInfinity(c)) return false;

            }
            catch (Exception)
            {
                a = 0; b = 0; c = 0;
                return false;
            }

            if (float.IsNaN(a) || float.IsNaN(b))
            {
                a = 0; b = 0; c = 0;
                return false;
            }

            return true;
        }

        private static float Determinant(float[] matrix3x3)
        {
            return matrix3x3[0] * matrix3x3[4] * matrix3x3[8] + matrix3x3[1] * matrix3x3[5] * matrix3x3[6] + matrix3x3[2] * matrix3x3[3] * matrix3x3[7] - matrix3x3[6] * matrix3x3[4] * matrix3x3[2] - matrix3x3[7] * matrix3x3[5] * matrix3x3[0] - matrix3x3[8] * matrix3x3[3] * matrix3x3[1];
        }
    }
}
