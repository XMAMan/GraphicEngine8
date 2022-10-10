using GraphicGlobal;
using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper.Helper3D;

namespace GraphicPipelineCPU.Shader.PixelShader
{
    //Ließt die Interpolationsvariablen, welche der Vertexshader eingefügt hat aus
    class PixelShaderParsedInput
    {
        public Vertex Vertex { get; private set; }
        public Footprint TexelPos { get; private set; }
        public Vector4D ShadowPosHome { get; private set; }
        public ShaderDataForTriangleNormal UniformVariables { get; private set; }

        public PixelShaderParsedInput(PixelShaderInput data)
        {
            this.UniformVariables = data.UniformVariables as ShaderDataForTriangleNormal;

            //Interpolationsvariablen auslesen
            Vertex vertex = data.PixelCenter.V.ReadVertex();
            this.ShadowPosHome = data.PixelCenter.V.ReadVec4();

            //Shadenormale ist Flat (Vom Dreieck) oder Smooth(Vom Vertex)
            Vector3D shadeNormal = data.TriangleNormalWorldSpace != null ? data.TriangleNormalWorldSpace : Vector3D.NormalizeWithoutZeroDivision(vertex.Normal);
            this.Vertex = new Vertex(vertex) { Normal = shadeNormal }; // Interpolierter Vertex -> Achtung: Nach dem Interpolieren müssen Richtungsvektoren unbedingt nochmal normiert werden!!!

            //4 Texturekoordinaten des Pixels auslesen, wenn man als Texturefilter Anisotroph nutzt
            this.TexelPos = null;
            if (data.PixelLeftTop != null)
            {
                Vertex vertexLo = data.PixelLeftTop.V.ReadVertex();
                Vertex vertexRo = data.PixelRightTop.V.ReadVertex();
                Vertex vertexLu = data.PixelLeftBottom.V.ReadVertex();
                Vertex vertexRu = data.PixelRightBottom.V.ReadVertex();

                this.TexelPos = new Footprint(data.PixelCenter.XY, vertexLo.TextcoordVector, vertexRo.TextcoordVector, vertexLu.TextcoordVector, vertexRu.TextcoordVector);
            }
        }
    }
}
