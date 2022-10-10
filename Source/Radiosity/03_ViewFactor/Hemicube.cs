using GraphicMinimal;
using System;
using System.Collections.Generic;

namespace Radiosity._03_ViewFactor
{
    //Es wird ein Würfel mit der Kantenlänge 1 wo jede Würfelseite hemicubeResolution*hemicubeResolution Pixel enthält erstellt
    //Für jeden Pixel kann die Richtung vom Punkt (0,0,0) bis PixelCenter hier ermittelt werden und der GeometryTerm
    //zwischen den Pixel und der Center-Fläche.
    //Idee für Hemicube: http://freespace.virgin.net/hugo.elias/radiosity/radiosity.htm
    class Hemicube
    {
        public class HemiDirection
        {
            public Vector3D Direction;
            public float DeltaFormFactor; //PixelArea * Lambda2 / r² / PI
        }

        private readonly int hemicubeResolution;
        private readonly float binormalRotation;
        private readonly Vector3D normal;

        public Hemicube(int hemicubeResolution, float binormalRotation, Vector3D normal)
        {
            this.hemicubeResolution = hemicubeResolution;
            this.binormalRotation = binormalRotation;
            this.normal = normal;

            //GetHemiPixelFromIndex((hemicubeResolution * hemicubeResolution) / 2 + hemicubeResolution / 2); //Erzeugt Richtungsvektor in Richtung Normale
            //float sum = GetDirectionsWithFaktor().Sum(x => x.DeltaFormFactor);
        }

        public IEnumerable<HemiDirection> GetDirectionsWithFaktor()
        {
            int maxIndex = MaxIndexForHemicube();
            for (int index = 0; index < maxIndex; index++)
            {
                yield return GetHemiPixelFromIndex(index);
            }
        }

        private int MaxIndexForHemicube()
        {
            return this.hemicubeResolution * this.hemicubeResolution * 5; //Jede Würfelseite enthält hemicubeResolution * hemicubeResolution Felder; Es gibt 5 Würfelseiten
        }

        //Über der Normale wird ein Würfel gespannt, wo eine Seite offen ist(Ganz unten wo die Normale startet). Die 5 Würfelseiten werden in ein Raster mit der Genauigkeit von 'unterteilungen' unterteilt. 
        //Die Würfelseiten, welche nicht vorne(in Richtung Normale) liegen, haben nur die halbe Höhe
        //index ist eine Zahl zwischen 0 und unterteilungen * unterteilungen * 3 (siehe MaxIndexForHemicube)
        private HemiDirection GetHemiPixelFromIndex(int index)
        {
            int res = this.hemicubeResolution;
            float resF = (float)this.hemicubeResolution;
            int u = res * res;
            float halfPixel = 1.0f / res / 2.0f;

            //Schritt 1: Bestimme auf Einheitswürfel den Pixel laut Index
            Vector3D pixelCenter;
            Vector3D pixelNormal;
            if (index >= 0 && index < u) //Vorne
            {
                int x = index % res;
                int y = index / res;
                pixelCenter = new Vector3D(x / resF + halfPixel, y / resF + halfPixel, 1);
                pixelNormal = new Vector3D(0, 0, -1);
            }
            else if (index >= u && index < 2 * u)//Oben
            {
                int start = index - u;
                int x = start % res;
                int z = start / res;
                pixelCenter = new Vector3D(x / resF + halfPixel, 1, z / resF + halfPixel);
                pixelNormal = new Vector3D(0, -1, 0);
            }
            else if (index >= 2 * u && index < 3 * u) //Rechts
            {
                int start = index - (2 * u);
                int z = start % res;
                int y = start / res;
                pixelCenter = new Vector3D(1, y / resF + halfPixel, z / resF + halfPixel);
                pixelNormal = new Vector3D(-1, 0, 0);
            }
            else if (index >= 3 * u && index < 4 * u)//Unten
            {
                int start = index - (3 * u);
                int x = start % res;
                int z = start / res;
                pixelCenter = new Vector3D(x / resF + halfPixel, 0, z / resF + halfPixel);
                pixelNormal = new Vector3D(0, 1, 0);
            }
            else if (index >= 4 * u && index < u * 5) //Links
            {
                int start = index - (4 * u);
                int z = start % res;
                int y = start / res;
                pixelCenter = new Vector3D(0, y / resF + halfPixel, z / resF + halfPixel);
                pixelNormal = new Vector3D(1, 0, 0);
            }
            else
            {
                throw new Exception("Index muss im Bereich 0 und " + (u * 5) + " liegen");
            }
            pixelCenter -= new Vector3D(0.5f + halfPixel, 0.5f + halfPixel, 0);

            float pixelArea = (1.0f / this.hemicubeResolution) * (1.0f / this.hemicubeResolution);
            Vector3D direction = Vector3D.Normalize(pixelCenter);
            float cosPixel = pixelNormal * (-direction);
            float cosCenter = new Vector3D(0, 0, 1) * direction;
            float deltaFormFactor = pixelArea * cosPixel * cosCenter / pixelCenter.SquareLength() / (float)Math.PI;


            //Schritt 2: Projektiziere die direction in Koordinatensystem von normale (Ich kann hier nicht die Methode aus der Matrix4x4 verwenden da ich über binormalRotation noch drehe)
            Vector3D N = normal;
            Vector3D B = -Vector3D.Normalize(Vector3D.Cross((Math.Abs(normal.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), normal));
            B = Vector3D.Normalize(Vector3D.RotateVerticalDirectionAroundAxis(B, normal, this.binormalRotation));
            Vector3D T = Vector3D.Cross(B, normal);

            Matrix4x4 TBN = new Matrix4x4(new float[]{T.X, T.Y, T.Z, 0,
                                                      B.X, B.Y, B.Z, 0,
                                                      N.X, N.Y, N.Z, 0,
                                                      0,   0,   0,   0});
            return new HemiDirection()
            {
                Direction = Vector3D.Normalize(Matrix4x4.MultDirection(TBN, direction)),
                DeltaFormFactor = deltaFormFactor
            };
        }
    }
}
