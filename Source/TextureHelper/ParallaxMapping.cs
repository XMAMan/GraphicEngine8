using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;
using System;
using System.Drawing;

namespace TextureHelper
{
    //Eine Parallax-Map ist ein Cubus welche eine Textur im Texturspace darstellt. 
    //Die X-Kante des Würfels geht von 0 bis TexturScaleFactorX (TexturU-Koordinate) -> Tangente
    //Die Y-Kante des Würfels geht von 0 bis TexturScaleFactorY (TexurV-Koordinate) -> Bitangente
    //Die Z-Kante des Würfels geht von 0 bis TextureHeightFactor * Kantenlänge des Würfels in Worldspace
    //Der Nullpunkt des Texturspaces liegt links oben bei der Textur
    public class ParallaxMapping
    {
        private readonly int nMaxSamples = 30;
        private readonly int nMinSamples = 4;


        private readonly ColorTexture bumpmapBitmap;        
        private readonly float texturScaleFaktorX;
        private readonly float texturScaleFaktorY;
        private readonly float textureHighScaleFaktor;
        private readonly Matrix3x3 textureMatrix;
        private readonly Matrix3x3 inverseTextureMatrix;
        private readonly TextureMode textureMode;
        private readonly bool isParallaxEdgeCutoffEnabled;

        public ParallaxVisibleMap VisibleMap { get; private set; }

        public ParallaxMapping(ColorTexture bumpmapBitmap, float textureHighScaleFaktor, Matrix3x3 textureMatrix, TextureMode textureMode, bool isParallaxEdgeCutoffEnabled)
        {
            var texturScale = Matrix3x3.GetTexturScale(textureMatrix); //Um den Parallax-Edge-Cutoff zu machen

            this.bumpmapBitmap = bumpmapBitmap;
            this.texturScaleFaktorX = texturScale.X;
            this.texturScaleFaktorY = texturScale.Y;
            this.textureMatrix = textureMatrix;
            this.inverseTextureMatrix = Matrix3x3.Invert(textureMatrix);
            this.textureHighScaleFaktor = textureHighScaleFaktor;
            this.textureMode = textureMode;
            this.isParallaxEdgeCutoffEnabled = isParallaxEdgeCutoffEnabled;
            this.VisibleMap = new ParallaxVisibleMap(new Size(64, 64), textureMatrix, textureHighScaleFaktor, texturScaleFaktorY);
        }

        //Prüft innerhalb der Textur, ob ich von 'point.WorldSpacePoint' Richtung toLightWorld bis zur Texturoberfläche (auf Höhe textureHighScaleFaktor) alles frei ist
        //return: true = Kein Schatten; false = Strahl liegt im Schatten
        public ParallaxPoint GetParallaxIntersectionPointStartingInside(ParallaxPoint point, Vector3D toLightWorld)
        {
            Vector3D toLightDirectionTex = Matrix4x4.MultDirection(point.WorldToTangentMatrix, toLightWorld);

            float aspectRatio = this.texturScaleFaktorX / this.texturScaleFaktorY; //Gleiche die Verzehrung aus (Siehe Ring-Kugel-Test)
            toLightDirectionTex.X *= aspectRatio; //z bleibt anverändert da sonst die Höhe verändert wird (Siehe Blaue Textur bei TexturMapping-Test)

            float t = (this.textureHighScaleFaktor - point.TexturSpacePoint.Z) / Math.Max(toLightDirectionTex.Z, 0.0001f);
            Vector3D endPointTex = point.TexturSpacePoint + toLightDirectionTex * t;

            int nNumSamples = (int)PixelHelper.Lerp(nMaxSamples, nMinSamples, toLightDirectionTex.Z);
            Vector3D posToEnd = endPointTex - point.TexturSpacePoint;
            Vector3D stepDirection = posToEnd / (float)nNumSamples;

            Vector3D stepPoint = point.TexturSpacePoint;
            for (int j = 0; j < nNumSamples; j++)
            {
                stepPoint += stepDirection;
                float currHeight = bumpmapBitmap.ReadColorFromTexture(stepPoint.X, stepPoint.Y, false, textureMode).A / 255.0f * this.textureHighScaleFaktor;

                //Trifft der stepPoint auf ein Kästchen in der Textur?
                if (currHeight > stepPoint.Z + 0.01f)
                {
                    //Ja, gib StepPoint zurück (Strahl liegt somit im Schatten)
                    return new ParallaxPoint()
                    {
                        PointIsOnTopHeight = false, 
                        EntryWorldPoint = point.EntryWorldPoint,
                        TexturSpacePoint = stepPoint,
                        WorldSpacePoint = point.EntryWorldPoint.Position + Matrix4x4.MultDirection(point.TangentToWorldMatrix, stepPoint - new Vector3D(point.EntryWorldPoint.TexcoordU, point.EntryWorldPoint.TexcoordV, this.textureHighScaleFaktor)),
                        Normal = null,
                        TangentToWorldMatrix = point.TangentToWorldMatrix,
                        WorldToTangentMatrix = point.WorldToTangentMatrix,
                    };

                }
            }

            return new ParallaxPoint()
            {
                PointIsOnTopHeight = true,
                EntryWorldPoint = point.EntryWorldPoint,
                TexturSpacePoint = endPointTex,
                WorldSpacePoint = point.EntryWorldPoint.Position + Matrix4x4.MultDirection(point.TangentToWorldMatrix, endPointTex - new Vector3D(point.EntryWorldPoint.TexcoordU, point.EntryWorldPoint.TexcoordV, this.textureHighScaleFaktor)),
                Normal = null, //Für den Schattentest ist die Normale nicht wichtig sondern nur die Information ob der Strahl unterbrochen wird oder nicht
                TangentToWorldMatrix = point.TangentToWorldMatrix,
                WorldToTangentMatrix = point.WorldToTangentMatrix,
            };
        }

        //Ich komme von Außen und sehe auf die Textur. Es wird der Schnittpunkt innerhalb der Textur gesucht (Wenn es nicht am Rand vorbei fliegt)
        //v.TexcoordU, v.TexcoordV = Texturkoordinaten vom Objekt ohne das sie mit der TextureMatrix multipliziert wurden
        public ParallaxPoint GetParallaxIntersectionPointFromOutToIn(Vertex v, Vector3D rayDirectionInWorldSpace)
        {
            //Trage testweise in die Visiblemap all die Pixel ein, welche die Kamera sieht
            //this.VisibleMap.MarkAsVisibleInBoolMap(new Vector2D(v.TexcoordU, v.TexcoordV));

            //Mit der TBN-Matrix kann man Richtungsvektoren von Tangent- in Weltkoordinaten transformieren. 
            Matrix4x4 tangentToWorldSpace = Matrix4x4.TBNMatrix(v.Normal, v.Tangent);

            //Transpose stellt die Inverse der Richtungsmatrix 'tangentToWorldSpace' dar
            Matrix4x4 worldToTangentSpace = Matrix4x4.Transpose(tangentToWorldSpace);

            Vector3D rayDirectionInTangentSpace = Matrix4x4.MultDirection(worldToTangentSpace, rayDirectionInWorldSpace);

            if (Math.Abs(rayDirectionInTangentSpace.Z) < 1e-5f) //Wenn man zu flach auf die Texture schaut, dann ist parallax Mapping nicht möglich (Man erhält dann die Minimal TexU/V-Integerzahl, welche nicht negierbar ist)
            {
                return new ParallaxPoint()
                {
                    PointIsOnTopHeight = true,
                    EntryWorldPoint = v,
                    TexureCoords = new Vector2D(v.TexcoordU, v.TexcoordV),
                    TexturSpacePoint = new Vector3D(v.TexcoordU, v.TexcoordV, textureHighScaleFaktor),
                    WorldSpacePoint = v.Position,
                    Normal = v.Normal,
                    TangentToWorldMatrix = tangentToWorldSpace,
                    WorldToTangentMatrix = worldToTangentSpace,
                };
            }

            float aspectRatio = this.texturScaleFaktorX / this.texturScaleFaktorY; //Gleiche die Verzehrung aus (Siehe Ring-Kugel-Test)
            rayDirectionInTangentSpace.X *= aspectRatio; //z bleibt anverändert da sonst die Höhe verändert wird (Siehe Blaue Textur bei TexturMapping-Test)

            //Bilde Schnittpunkt zwischen Strahl {(v.TexcoordU, v.TexcoordV, textureHighScaleFaktor) + rayDirectionInTangentSpace * t} und der Ebene {z=0}
            float t = -textureHighScaleFaktor / rayDirectionInTangentSpace.Z;

            // Um so flacher man auf die Textur schaut, um so mehr Schritt muss man auf der Textur laufen
            int nNumSamples = (int)PixelHelper.Lerp(nMaxSamples, nMinSamples, -rayDirectionInTangentSpace.Z);

            //Starte oben auf der Textur
            var texCoordM = this.textureMatrix * new Vector3D(v.TexcoordU, v.TexcoordV, 1);
            Vector3D stepPoint = new Vector3D(texCoordM.X, texCoordM.Y, textureHighScaleFaktor);
            Vector3D endPoint = stepPoint + rayDirectionInTangentSpace * t;
            Vector3D stepDirection = (endPoint - stepPoint) / (float)nNumSamples;

            float currHeight = textureHighScaleFaktor;
            Vector3D lastStepPoint = stepPoint;

            for (int i = 0; i < nNumSamples; i++)
            {
                float lastHeight = currHeight;
                currHeight = bumpmapBitmap.ReadColorFromTexture(stepPoint.X, stepPoint.Y, false, textureMode).A / 255.0f * textureHighScaleFaktor;

                //Habe ich den Schnittpunkt zwischen den camToPos-Strahl und der Textur gefunden?
                if (currHeight > stepPoint.Z)
                {
                    float delta1 = currHeight - stepPoint.Z;                        //Aktueller Abstand zwischen Strahl und Textur
                    float delta2 = (stepPoint.Z - stepDirection.Z) - lastHeight;    //Vorheriger Abstand zwischen Strahl und Textur
                    float ratio = delta1 / (delta1 + delta2);

                    // Interpolate between the final two segments to find the true intersection point offset.
                    stepPoint = (ratio) * lastStepPoint + (1.0f - ratio) * stepPoint;

                    break;
                }

                lastStepPoint = stepPoint;
                stepPoint += stepDirection;                
            }

            Vector2D vFinalCoords = stepPoint.XY;

            //Schneide den Rand ab
            if (isParallaxEdgeCutoffEnabled)
            {
                if (this.VisibleMap.ObjectHasOnlyTwoTriangles)
                {
                    //Dieser Ansatz geht nur, wenn man ein Viereck hat
                    //Achtung: Der Rand wird vom Viereck hier abgeschnitten was dazu führt, dass das Viereck kleiner wird. Man müsste das über den Size-Faktor ausgleichen
                    if (vFinalCoords.X <= 0.01f || vFinalCoords.Y <= 0.01f || vFinalCoords.X >= texturScaleFaktorX - 0.01f || vFinalCoords.Y >= texturScaleFaktorY - 0.01f) return null;
                }
                else
                {
                    //Prüfe ob der Punkt, von den ich aus komme den vFinalCoords-Punkt überhaupt sehen kann
                    if (this.VisibleMap.IsPointVisibleFromTextPoint(v.Position - rayDirectionInWorldSpace, vFinalCoords) == false) return null;
                }
            }

            Vector3D bumpNormal = TextureHelper.TransformBumpNormalFromTangentToWorldSpace(bumpmapBitmap.ReadColorFromTexture(vFinalCoords.X, vFinalCoords.Y, true, textureMode), tangentToWorldSpace);

            //Wenn ich beim Raycasting durch zwei Flächen in der Textur durchlaufe und nun also auf der Rückseite von ein Huckel bin,
            //dann zeigt die bumpNormale in die entgegengesetzte Richtung. In diesen Falle müsste ich die Normale von der zuerst
            //durchlaufenen Fläche zurück geben. Da ich aber dessen Normale nicht ohne weitere Suchschritte weiß, benutzt ich einfach die
            //Parallx-Flatnormale als Returnwert
            if (bumpNormal * rayDirectionInWorldSpace > 0) bumpNormal = v.Normal; //Gib die Parallx-Flatnormale zurück, wenn Huckel komplett duchlaufen wurde

            var textureSpacePoint = new Vector3D(stepPoint.X, stepPoint.Y, currHeight);
            var textCoords = this.inverseTextureMatrix * new Vector3D(stepPoint.X, stepPoint.Y, 1);
            return new ParallaxPoint()
            {
                PointIsOnTopHeight = currHeight == textureHighScaleFaktor,
                EntryWorldPoint = v,
                TexureCoords = textCoords.XY,
                TexturSpacePoint = textureSpacePoint,
                WorldSpacePoint = v.Position + Matrix4x4.MultDirection(tangentToWorldSpace, textureSpacePoint - new Vector3D(texCoordM.X, texCoordM.Y, textureHighScaleFaktor)),
                Normal = bumpNormal,
                TangentToWorldMatrix = tangentToWorldSpace,
                WorldToTangentMatrix = worldToTangentSpace,
            };
        }
    }
}
