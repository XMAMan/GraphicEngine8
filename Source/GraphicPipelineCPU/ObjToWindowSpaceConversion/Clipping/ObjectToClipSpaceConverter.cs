using GraphicGlobal;
using GraphicMinimal;
using GraphicPipelineCPU.Shader;
using System.Collections.Generic;
using System.Linq;

namespace GraphicPipelineCPU.ObjToWindowSpaceConversion.Clipping
{
    static class ObjectToClipSpaceConverter
    {
        public static List<ClipSpacePoint> TransformTriangleToClipSpace(Triangle triangle, IUniformVariables uniformVariables, VertexShaderFunc vertexShader, GeometryShaderFunc geometryShader)
        {
            var vOut1 = vertexShader(new VertexShaderInput { Vertex = triangle.V[0], UniformVariables = uniformVariables });
            var vOut2 = vertexShader(new VertexShaderInput { Vertex = triangle.V[1], UniformVariables = uniformVariables });
            var vOut3 = vertexShader(new VertexShaderInput { Vertex = triangle.V[2], UniformVariables = uniformVariables });

            List<VertexShaderOutput> geometryShaderOutput = geometryShader(new List<VertexShaderOutput>() { vOut1, vOut2, vOut3 });

            geometryShaderOutput.ForEach(x => x.Interpolationvariables.SetZInEyeSpace(x.Vec4Position.Z));

            return geometryShaderOutput.Select(x =>
                new ClipSpacePoint(x.Interpolationvariables, x.Vec4Position)
                ).ToList();
        }

        public static List<ClipSpacePoint> TransformLineToClipSpace(Vector3D v1, Vector3D v2, IUniformVariables uniformVariables, VertexShaderFunc vertexShader)
        {
            return TransformVertexListFromObjectToClipSpace(new List<Vertex>() { new Vertex(v1), new Vertex(v2) }, uniformVariables, vertexShader);
        }

        public static List<ClipSpacePoint> TransformVertexListFromObjectToClipSpace(List<Vertex> vertexList, IUniformVariables uniformVariables, VertexShaderFunc vertexShader)
        {
            return vertexList.Select(x => TransformVertexFromObjectToClipSpace(x, uniformVariables, vertexShader)).ToList();
        }

        private static ClipSpacePoint TransformVertexFromObjectToClipSpace(Vertex vertex, IUniformVariables uniformVariables, VertexShaderFunc vertexShader)
        {
            var shaderOutput = vertexShader(new VertexShaderInput { Vertex = vertex, UniformVariables = uniformVariables });
            shaderOutput.Interpolationvariables.SetZInEyeSpace(shaderOutput.Vec4Position.Z);
            return new ClipSpacePoint(shaderOutput.Interpolationvariables, shaderOutput.Vec4Position);
        }
    }
}
