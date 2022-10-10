using GraphicMinimal;
using GraphicGlobal;
using GraphicPipelineCPU.DrawingHelper.Helper3D;

namespace GraphicPipelineCPU.Shader
{
    class VertexShader
    {
        public static VertexShaderOutput VertexShaderForTriangles(VertexShaderInput input)
        {
            ShaderDataForTriangleNormal data = (ShaderDataForTriangleNormal)input.UniformVariables;

            Vector2D textcoord = (data.TextureMatrix * new Vector3D(input.Vertex.TexcoordU, input.Vertex.TexcoordV, 1)).XY;

            //Vertex im Worldspace
            Vertex outputVertex = new Vertex(0, 0, 0)
            {
                Position = Matrix4x4.MultPosition(data.ObjToWorld, input.Vertex.Position),
                Normal = Vector3D.NormalizeWithoutZeroDivision(Matrix4x4.MultDirection(data.NormalMatrix, input.Vertex.Normal)),
                TexcoordU = textcoord.X,
                TexcoordV = textcoord.Y,
                Tangent = Vector3D.Normalize(Matrix4x4.MultDirection(data.NormalMatrix, input.Vertex.Tangent))
            };

            VertexShaderOutput shaderOutput = new VertexShaderOutput();
            shaderOutput.Vec4Position = data.WorldViewProj * input.Vertex.Position.AsVector4D();

            shaderOutput.Interpolationvariables.AddVertex(outputVertex);
            shaderOutput.Interpolationvariables.AddVec4(data.ShadowMatrix * input.Vertex.Position.AsVector4D(), Interpolationvariables.VariableType.WithPerspectiveDivision);

            return shaderOutput;
        }

        public static VertexShaderOutput VertexShaderForLines(VertexShaderInput input)
        {
            var data = (ShaderDataForLines)input.UniformVariables;

            VertexShaderOutput shaderOutput = new VertexShaderOutput();
            shaderOutput.Vec4Position = data.WorldViewProj * input.Vertex.Position.AsVector4D();

            return shaderOutput;
        }

        public static VertexShaderOutput VertexShaderForTrianglesShadowMapCreation(VertexShaderInput input)
        {
            ShaderDataForShadowMapCreation data = (ShaderDataForShadowMapCreation)input.UniformVariables;

            Vector2D textcoord = (data.TextureMatrix * new Vector3D(input.Vertex.TexcoordU, input.Vertex.TexcoordV, 1)).XY;

            VertexShaderOutput shaderOutput = new VertexShaderOutput();
            shaderOutput.Vec4Position = data.ShadowMatrix * input.Vertex.Position.AsVector4D();

            shaderOutput.Interpolationvariables.AddVector3D(new Vector3D(textcoord.X, textcoord.Y, 0), Interpolationvariables.VariableType.WithPerspectiveDivision);
            shaderOutput.Interpolationvariables.AddVec4(data.ShadowMatrix * input.Vertex.Position.AsVector4D(), Interpolationvariables.VariableType.WithPerspectiveDivision);

            return shaderOutput;
        }
    }
}
