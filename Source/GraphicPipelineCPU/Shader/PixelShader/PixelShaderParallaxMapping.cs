using BitmapHelper;
using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper;
using System;
using System.Drawing;

namespace GraphicPipelineCPU.Shader.PixelShader
{
    class PixelShaderParallaxMapping
    {
        private readonly PropertysForDrawing prop;
        private readonly PixelShaderHelper helper;

        public PixelShaderParallaxMapping(PropertysForDrawing prop)
        {
            this.prop = prop;
            this.helper = new PixelShaderHelper(prop);
        }

        public Color GetPixelColor(PixelShaderInput unparsedData)
        {
            PixelShaderParsedInput data = new PixelShaderParsedInput(unparsedData);

            Vector4D color = ParallaxMapping(data);
            if (color == null) return Color.Transparent;
            return color.ToColor();
        }

        private Vector4D ParallaxMapping(PixelShaderParsedInput data)
        {
            int nMaxSamples = 30;
            int nMinSamples = 4;

            //Mit der TBN-Matrix kann man Richtungsvektoren von Welt- in Texturkoordinaten transformieren. 
            Matrix4x4 worldToTangentSpace = PixelShaderHelper.GetWorldToTangentSpaceMatrix(data.Vertex.Normal, data.Vertex.Tangent);

            Vector3D camToPos = Vector3D.Normalize(data.Vertex.Position - prop.CameraPosition);
            Vector3D camToPosTex = Matrix4x4.MultDirection(worldToTangentSpace, camToPos);    //camToPos im Texturspace

            float aspectRatio = prop.TexturScaleFaktor.X / prop.TexturScaleFaktor.Y; //Gleiche die Verzehrung aus (Siehe Ring-Kugel-Test)
            camToPosTex.X *= aspectRatio; //z bleibt anverändert da sonst die Höhe verändert wird (Siehe Blaue Textur bei TexturMapping-Test)

            //Bilde Schnittpunkt zwischen Strahl {(input.tex.x, input.tex.y, HighscaleFaktor) + camToPosTex * t} und der Ebene {z=0}
            //Gleichung die nach t umgestellt wurde: HeighscaleFaktor + camToPosTex.z * t = 0
            float t = -prop.CurrentTextureHeighScaleFactor / camToPosTex.Z;

            // Um so flacher man auf die Textur schaut, um so mehr Schritt muss man auf der Textur laufen
            int nNumSamples = (int)PixelHelper.Lerp(nMaxSamples, nMinSamples, -camToPosTex.Z);

            //Starte oben auf der Textur
            Vector3D stepPoint = new Vector3D(data.Vertex.TexcoordU, data.Vertex.TexcoordV, prop.CurrentTextureHeighScaleFactor);
            Vector3D endPoint = stepPoint + camToPosTex * t;
            Vector3D stepDirection = (endPoint - stepPoint) / (float)nNumSamples;

            float currHeight;
            float lastHeight = prop.CurrentTextureHeighScaleFactor;
            Vector3D lastStepPoint = stepPoint;

            for (int i = 0; i < nNumSamples; i++)
            {
                currHeight = prop.Deck1.ReadTexelWithPointFilter(stepPoint.X, stepPoint.Y).A / 255.0f * prop.CurrentTextureHeighScaleFactor;

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
                lastHeight = currHeight;
            }

            Vector2D vFinalCoords = stepPoint.XY;

            //Schneide den Rand ab
            if (prop.CurrentTesselationFactor != 0)
            {
                if (vFinalCoords.X <= 0.01f || vFinalCoords.Y <= 0.01f || vFinalCoords.X >= prop.TexturScaleFaktor.X - 0.01f || vFinalCoords.Y >= prop.TexturScaleFaktor.Y - 0.01f) return null; //Discard
            }

            Vector4D noLightColor;
            if (prop.Deck0.IsEnabled)
                noLightColor = prop.Deck0.ReadTexel(new Vector2D(vFinalCoords.X, vFinalCoords.Y), data.TexelPos).ToVector4D();//Achtung: Der Footprint 'data.TexelPos' wurde nicht durch Parallax verschoben und ist somit falsch
            else
                noLightColor = prop.CurrentColor;

            if (prop.BlendingIsEnabled && prop.BlendingMode == BlendingMode.WithBlackColor && noLightColor.IsBlackColor()) return null; //Discard

            float shadowFactor = GetShadowFactor(data.Vertex.Position, new Vector2D(data.Vertex.TexcoordU, data.Vertex.TexcoordV), vFinalCoords, Matrix4x4.Transpose(worldToTangentSpace), worldToTangentSpace, data.UniformVariables, nMinSamples, nMaxSamples);

            if (prop.LightingIsEnabled == false) return noLightColor.MultXyz(shadowFactor);

            Vector3D bumpNormal = this.helper.ReadBumpnormalFromTexture(vFinalCoords.X, vFinalCoords.Y, data.Vertex.Normal, data.Vertex.Tangent);
            return this.helper.GetIlluminatedColor(data.Vertex.Position, bumpNormal, noLightColor).MultXyz(shadowFactor);
        }

        //Schattenfaktor für das Parallaxmapping
        private float GetShadowFactor(Vector3D inputWorldPosition, Vector2D inputTex, Vector2D vFinalCoords, Matrix4x4 tangentToWorldSpace, Matrix4x4 worldToTangentSpace, IUniformVariables uniformVariables, int nMinSamples, int nMaxSamples)
        {
            var data = (ShaderDataForTriangleNormal)uniformVariables;
            Matrix4x4 worldToObj = data.WorldToObj;
            Matrix4x4 shadowMatrix = data.ShadowMatrix;

            //Schritt 1: Bestimme den aktuellen Punkt im Texturspace wo ich hingelaufen bin
            float heightTex = prop.Deck1.ReadTexelWithPointFilter(vFinalCoords.X, vFinalCoords.Y).A / 255.0f * prop.CurrentTextureHeighScaleFactor;
            Vector3D pointTex = new Vector3D(vFinalCoords.X, vFinalCoords.Y, heightTex);

            //Schritt 2: Rechen den pointTex-Punkt in Worldspace um
            Vector3D pointWorld = inputWorldPosition + Matrix4x4.MultDirection(tangentToWorldSpace, pointTex - new Vector3D(inputTex.X, inputTex.Y, prop.CurrentTextureHeighScaleFactor));

            //Schritt 3: Gehe durch alle Lichtquelle durch und laufe von pointTex in Richtung Lichtquelle im Texturspace
            //			 und schaue, ob du bis Höhe 1 * HighscaleFaktor kommst
            foreach (var light in prop.Lights) //Gehe durch alle Lichtquelle durch
            {
                //Schritt 4: Bestimme den Richtungsvektor vom pointTex zur Lichtquelle im Texturspace
                Vector3D toLightDirectionTex = Matrix4x4.MultDirection(worldToTangentSpace, Vector3D.Normalize(light.Position - pointWorld));

                float aspectRatio = prop.TexturScaleFaktor.X / prop.TexturScaleFaktor.Y; //Gleiche die Verzehrung aus (Siehe Ring-Kugel-Test)
                toLightDirectionTex.X *= aspectRatio; //z bleibt anverändert da sonst die Höhe verändert wird (Siehe Blaue Textur bei TexturMapping-Test)


                //Schritt 5: Bestimme die Anzahl der Sampleschritte fürs Raycasting
                int nNumSamples = (int)PixelHelper.Lerp(nMaxSamples, nMinSamples, toLightDirectionTex.Z);// Um so flacher man auf die Textur schaut, um so mehr Schritt muss man auf der Textur laufen

                //Schritt 6: Die Textur wird oben beim Punkt 'endPoint = pointTex + toLightDirectionTex * t' geschnitten
                //			 Bestimme t indem der Schnittpunkt zwischen den Strahl {pointTex + toLightDirectionTex * t}
                //			 und der Ebene {z = HighscaleFaktor} berechnet wird
                //t ist die Distanz zwischen pointTex und endPoint (Liegt ganz oben im Texturspace)
                float t = (prop.CurrentTextureHeighScaleFactor - pointTex.Z) / Math.Max(toLightDirectionTex.Z, 0.0001f);
                Vector3D endPoint = pointTex + toLightDirectionTex * t;
                Vector3D posToEnd = endPoint - pointTex;
                Vector3D stepDirection = posToEnd / (float)nNumSamples;

                //Schritt 7: Laufe mit Raycasting bis zur oberen Texturkante und schaue, ob dort der Weg frei ist
                Vector3D stepPoint = pointTex;
                float currHeight;
                for (int j = 0; j < nNumSamples; j++)
                {
                    stepPoint += stepDirection;
                    currHeight = prop.Deck1.ReadTexelWithPointFilter(stepPoint.X, stepPoint.Y).A / 255.0f * prop.CurrentTextureHeighScaleFactor;

                    //Liegt stepPoint im Schatten?
                    if (currHeight > stepPoint.Z + 0.01f)
                    {
                        return 0.5f; //Punkt liegt im Schatten weil Hightmaptextur das so sagt
                    }
                }

                //Schritt 8: Ich bin ganz oben bei der Textur angekommen am Punkt endPoint. Prüfe nun per Shadow-Mapping
                //ob hier kein Schatten ist		
                if (prop.UseShadowmap)
                {
                    Vector3D endPointWorld = pointWorld + Matrix4x4.MultDirection(tangentToWorldSpace, endPoint - pointTex);

                    Vector4D objPos = worldToObj * endPointWorld.AsVector4D();

                    Vector4D shadowPos4 = shadowMatrix * objPos;
                    Vector3D shadowPos = shadowPos4.XYZ / shadowPos4.W;
                    shadowPos.Y = 1 - shadowPos.Y;

                    if (shadowPos.X > 0 && shadowPos.Y > 0 && shadowPos.X < 1 && shadowPos.Y < 1)
                    {
                        if (shadowPos.Z < 1 && prop.ShadowDepthTexture.ReadDepthValue(shadowPos.XY) < shadowPos.Z - 0.001)
                        {
                            return 0.5f; //Es gibt Schatten weil ein anders Objekt der Szene sein Schatten zum endPoint wirft
                        }
                    }
                }
            }

            return 1; //Es gibt kein Schatten
        }
    }
}
