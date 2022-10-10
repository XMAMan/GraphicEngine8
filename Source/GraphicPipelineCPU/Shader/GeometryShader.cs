using GraphicGlobal;
using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper.Helper3D;
using System;
using System.Collections.Generic;

namespace GraphicPipelineCPU.Shader
{
    //Bekommt eine Liste von Vertizen für ein Fragment/PrimitiveType (Triangle/Linie) und gibt erneut Liste von 
    //Vertizen zurück, welche wieder den selben PrimitiveTyp darstellen
    class GeometryShader
    {
        private PropertysForDrawing prop = null;

        public GeometryShader(PropertysForDrawing prop)
        {
            this.prop = prop;
        }

        public static List<VertexShaderOutput> DoNothing(List<VertexShaderOutput> input)
        {
            return input;
        }

        //Input: 3 Vertize ausm Vertexshader
        //Ouput: 3 Vertize mit Explosionseffektverschiebung
        public List<VertexShaderOutput> ForTriangleNormal(List<VertexShaderOutput> input)
        {
            Vector3D translation = new Vector3D(0, 0, 0);

            List<VertexShaderOutput> outputStream = new List<VertexShaderOutput>();
            for (int i=0;i<3;i++)
            {
                Vertex vertex = input[i].Interpolationvariables.ReadVertex();
                Vector4D shadowPosHome = input[i].Interpolationvariables.ReadVec4();
                
                if (prop.ExplosionEffectIsEnabled && i == 0)
                {
                    translation = Vector3D.Normalize(vertex.Normal) * (float)Math.Abs(Math.Sin(prop.Time / 100.0f)) * prop.ExplosionsRadius;
                }

                vertex.Position += translation;

                VertexShaderOutput output = new VertexShaderOutput();
                
                output.Vec4Position = (prop.CameraMatrix * prop.ProjectionMatrix) * vertex.Position.AsVector4D();
                
                output.Interpolationvariables.AddVertex(vertex);
                output.Interpolationvariables.AddVec4(shadowPosHome, Interpolationvariables.VariableType.WithPerspectiveDivision);

                outputStream.Add(output);
            }

            return outputStream;
        }


        //Input: 3 Vertize ausm Vertexshader
        //Ouput: 3 Vertize mit Explosionseffektverschiebung
        public List<VertexShaderOutput> ForTriangleShadowMapCreation(List<VertexShaderOutput> input)
        {
            List<VertexShaderOutput> outputStream = new List<VertexShaderOutput>();
            for (int i = 0; i < 3; i++)
            {
                Vector3D vector = input[i].Interpolationvariables.ReadVector3D();
                Vector4D shadowPosHome = input[i].Interpolationvariables.ReadVec4();

                
                VertexShaderOutput output = new VertexShaderOutput();

                output.Vec4Position = input[i].Vec4Position;
 
                output.Interpolationvariables.AddVector3D(vector, Interpolationvariables.VariableType.WithPerspectiveDivision);
                output.Interpolationvariables.AddVec4(shadowPosHome, Interpolationvariables.VariableType.WithPerspectiveDivision);

                outputStream.Add(output);
            }

            return outputStream;
        }
    }
}
