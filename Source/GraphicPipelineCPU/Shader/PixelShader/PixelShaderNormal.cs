using GraphicGlobal;
using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper;
using System;
using System.Drawing;

namespace GraphicPipelineCPU.Shader.PixelShader
{
    class PixelShaderNormal
    {
        private readonly PropertysForDrawing prop;
        private readonly PixelShaderHelper helper;

        public PixelShaderNormal(PropertysForDrawing prop)
        {
            this.prop = prop;
            this.helper = new PixelShaderHelper(prop);
        }

        public Color GetPixelColor(PixelShaderInput unparsedData)
        {
            PixelShaderParsedInput data = new PixelShaderParsedInput(unparsedData);

            //Farbe für diesen Pixel bestimmen
            Vector4D color = GetVertexColor(data);
            if (color == null) return Color.Transparent;
            return color.ToColor();
        }

        private Vector4D GetVertexColor(PixelShaderParsedInput data)
        {
            //Schritt 1: noLightColor bestimmen
            Vector4D noLightColor = GetNoLightColor(data);

            if (prop.BlendingIsEnabled && prop.BlendingMode == BlendingMode.WithBlackColor && noLightColor.IsBlackColor()) return null; //Discard

            //Schritt 2: normalVector bestimmen
            Vector3D normalVector = GetNormalVector(data);

            //return TestOutput(data.Vertex, normalVector, TestType.LightingSimple); //Testausgabe

            //Schritt 3: Ausgabe per Cubemapping, Beleuchtung oder ohne Beleuchtung

            //Ausgabemöglichkeit 1: Cubemapping
            //Cubemapping (Reflexion)
            if (prop.CubemapTexture != null)
            {
                return GetCubemappedColor(data, normalVector, noLightColor);
            }

            //Shadowmapping
            float shadowFactor = GetShadowFactor(data, normalVector);

            //Ausgabemöglichkeit 2: Mit Lichtquellen beleuchten
            if (prop.LightingIsEnabled) return this.helper.GetIlluminatedColor(data.Vertex.Position, normalVector, noLightColor).MultXyz(shadowFactor);

            //Ausgabemöglichkeit 3: Ohne Beleuchtungsformel
            return noLightColor.MultXyz(shadowFactor);
        }

        private Vector4D GetNoLightColor(PixelShaderParsedInput data)
        {
            if (prop.Deck0.IsEnabled) 
                return prop.Deck0.ReadTexel(data.Vertex.TextcoordVector, data.TexelPos).ToVector4D(); //Texturmapping
            else
                return prop.CurrentColor; //Nur RGB-Farbe von colorMaterial
        }

        private Vector3D GetNormalVector(PixelShaderParsedInput data)
        {
            if (prop.Deck1.IsEnabled && prop.NormalSource == NormalSource.Normalmap)
                return this.helper.ReadBumpnormalFromTexture(data.Vertex.TexcoordU, data.Vertex.TexcoordV, data.Vertex.Normal, data.Vertex.Tangent); //Normalmapping
            else
                return data.Vertex.Normal; //Normale per Gouraud- oder Flat-Shading
        }

        private Vector4D GetCubemappedColor(PixelShaderParsedInput data, Vector3D normalVector, Vector4D noLightColor)
        {
            Matrix4x4 normalmatrix = data.UniformVariables.NormalMatrix;

            Vector3D reflectionVector = Vector3D.GetReflectedDirection(data.Vertex.Position - prop.CameraPosition, normalVector);
            reflectionVector = Vector3D.Normalize(Matrix4x4.MultDirection(Matrix4x4.Transpose(normalmatrix), reflectionVector));

            Vector4D col1 = this.helper.GetIlluminatedColor(data.Vertex.Position, normalVector, noLightColor); //Farbe des Spiegels
            Vector4D col2 = prop.CubemapTexture.GetCubemapSample(reflectionVector).ToVector4D();//Farbe vom dem Objekt, was man wegen der Reflektion sieht
            Vector4D c = prop.CurrentColor; //Blendfaktor
            Vector4D cubResult = new Vector4D
            (
                col1.X * (1 - c.X) + col2.X * c.X,
                col1.Y * (1 - c.Y) + col2.Y * c.Y,
                col1.Z * (1 - c.Z) + col2.Z * c.Z,
                c.W
            );
            return cubResult;
        }

        private float GetShadowFactor(PixelShaderParsedInput data, Vector3D normalVector)
        {
            float shadowFactor = 1;
            if (prop.ShadowDepthTexture != null && prop.UseShadowmap)
            {
                //shadowPos = (XY = Texturcoodinate für die Shadowtexture; Z = Abstand von diesen Pixel zur Lichtquelle)
                Vector3D shadowPos = data.ShadowPosHome.XYZ / data.ShadowPosHome.W;
                shadowPos.Y = 1 - shadowPos.Y;

                float bias = Math.Max(0.001f * (1.0f - (normalVector * Vector3D.Normalize(prop.Lights[0].Position - data.Vertex.Position))), 0.0001f);

                float shadowTexelZ = prop.ShadowDepthTexture.ReadDepthValue(shadowPos.XY);
                if (shadowPos.Z < 1 && shadowPos.X > 0 && shadowPos.Y > 0 && shadowPos.X < 1 && shadowPos.Y < 1 && shadowTexelZ < shadowPos.Z - bias)
                {
                    shadowFactor = 0.5f;
                }
            }

            return shadowFactor;
        }

        enum TestType { LightingSimple, Tangent, Normal, TangentX }
        private Vector4D TestOutput(Vertex v, Vector3D normalVector, TestType type)
        {
            //Testausgabe aus Normale/Tangente
            switch (type)
            {
                case TestType.LightingSimple:
                    {
                        Vector3D toLight = Vector3D.Normalize(prop.Lights[0].Position - v.Position);
                        return new Vector4D(new Vector3D(1, 1, 1) * Math.Max(normalVector * toLight, 0.0f), 1);
                    }

                case TestType.Tangent:
                    return new Vector4D(v.Tangent, 1);

                case TestType.TangentX:
                    return new Vector4D(v.Tangent.X, 0, 0, 1);

                case TestType.Normal:
                    return new Vector4D(normalVector, 1);
            }

            throw new ArgumentException("Unknown Type " + type);
        }
    }
}
