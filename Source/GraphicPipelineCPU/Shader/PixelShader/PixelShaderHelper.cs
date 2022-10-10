using BitmapHelper;
using GraphicMinimal;
using System;
using System.Drawing;

namespace GraphicPipelineCPU.Shader.PixelShader
{
    class PixelShaderHelper
    {
        private readonly PropertysForDrawing prop = null;

        public PixelShaderHelper(PropertysForDrawing prop)
        {
            this.prop = prop;
        }

        public Vector4D GetIlluminatedColor(Vector3D posWorld, Vector3D normalVector, Vector4D objektColor)
        {
            //Beleuchtung 
            //Quelle: http://www.glprogramming.com/red/chapter05.html
            Vector3D sumColor = new Vector3D(0, 0, 0);
            Vector3D noLightColor = objektColor.XYZ;
            foreach (var L in prop.Lights)
            {
                Vector3D toLight = L.Position - posWorld;
                float dist = toLight.Length();
                float distanceFactor = 1 / (L.ConstantAttenuation +
                                           L.LinearAttenuation * dist +
                                           L.QuadraticAttenuation * dist * dist);

                toLight = Vector3D.Normalize(toLight);

                //Berechne Diffiuse Farbe
                //Vector3D reflektetLight = new Vector3D(0, 0, 1); //Vector3D.Normiere(toLight - Vector3D.Normiere(posEye));
                Vector3D reflektetLight = Vector3D.Normalize(Matrix4x4.MultDirection(Matrix4x4.Transpose(prop.CameraMatrix), new Vector3D(0, 0, 1)));

                float NdotL = Math.Max(normalVector * toLight, 0.0f);           // Diffuse Faktor
                float NdotS = Math.Max(normalVector * reflektetLight, 0.0f);        // Glanzpunkt Faktor
                if (prop.CullFaceIsEnabled == false && NdotL == 0)                       // Soll Rückseite beleuchtet werden?
                {
                    NdotL = Math.Max(-normalVector * toLight, 0.0f);            // Diffuse Faktor
                    NdotS = Math.Max(-normalVector * reflektetLight, 0.0f);         // Glanzpunkt Faktor
                }

                float spot = 1;
                if (L.SpotCutoff != -1) //Punkt-Richtungslicht
                {
                    spot = Math.Max(L.SpotDirection * (-toLight), 0.0f);
                    if (spot < L.SpotCutoff) spot = 0;
                    spot = (float)Math.Pow(spot, L.SpotExponent);
                }

                //Berechne größe des Glanzpunktes
                if (NdotS > 1.0f) NdotS = 1.0f;
                float specuFarbe = (float)Math.Pow(NdotS, prop.SpecularHighlightPowExponent);
                if (prop.SpecularHighlightPowExponent == 0) specuFarbe = 0;

                Vector3D diffuseTerm = new Vector3D(NdotL * noLightColor.X,
                                                NdotL * noLightColor.Y,
                                                NdotL * noLightColor.Z);
                Vector3D specularTerm = new Vector3D(1, 1, 1) * specuFarbe;

                Vector3D contribution = PixelHelper.Clamp(noLightColor * 0.05f + distanceFactor * spot * (noLightColor * 0.05f + diffuseTerm + specularTerm), 0, 1);

                sumColor += contribution;
            }
            return new Vector4D(sumColor, objektColor.W);
        }

        public Vector3D ReadBumpnormalFromTexture(float texcoordU, float texcoordV, Vector3D normalEyespace, Vector3D tangentEyespace)
        {
            Color bumpCol = prop.Deck1.ReadTexelWithLinearFilter(texcoordU, texcoordV);
            Vector3D bumpNormal = new Vector3D((bumpCol.R - 128) / 128f, (bumpCol.G - 128) / 128f, (bumpCol.B - 128) / 128f);

            Matrix4x4 tangentToWorldSpace = Matrix4x4.TBNMatrix(normalEyespace, tangentEyespace);

            return Vector3D.Normalize(Matrix4x4.MultDirection(tangentToWorldSpace, bumpNormal));
        }

        public static Matrix4x4 GetWorldToTangentSpaceMatrix(Vector3D normalWorldspace, Vector3D tangentWorldspace)
        {
            Matrix4x4 tangentToWorldSpace = Matrix4x4.TBNMatrix(normalWorldspace, tangentWorldspace);

            //Transpose stellt die Inverse der Richtungsmatrix 'tangentToWorldSpace' dar
            Matrix4x4 worldToTangentSpace = Matrix4x4.Transpose(tangentToWorldSpace);
            return worldToTangentSpace;
        }
    }
}
