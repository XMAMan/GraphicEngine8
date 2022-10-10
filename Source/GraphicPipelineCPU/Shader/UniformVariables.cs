using GraphicMinimal;

namespace GraphicPipelineCPU.Shader
{
    //Variablen für ein ganzes Triangle-Array
    interface IUniformVariables
    {
    }

    class ShaderDataForTriangleNormal : IUniformVariables
    {
        public Matrix4x4 WorldViewProj;
        public Matrix4x4 NormalMatrix;
        public Matrix4x4 ObjToWorld;
        public Matrix4x4 WorldToObj;
        public Matrix4x4 ShadowMatrix;
        public Matrix3x3 TextureMatrix;
    }

    class ShaderDataForShadowMapCreation : IUniformVariables
    {
        public Matrix4x4 ShadowMatrix;
        public Matrix3x3 TextureMatrix;
    }

    class ShaderDataForLines : IUniformVariables
    {
        public Matrix4x4 WorldViewProj;
    }
}
