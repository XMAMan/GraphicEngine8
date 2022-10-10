using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using GraphicGlobal;

namespace GraphicPipelineCPU.DrawingHelper.Helper3D
{
    //Hilft beim Interpolieren von Variablen(z.B. Texturkoodinaten) über das Dreieck
    class Interpolationvariables
    {
        public enum VariableType
        {
            WithPerspectiveDivision,
            WithoutPerspectiveDivision
        }

        class Variable
        {
            public float Value;
            public VariableType InterpolationType;

            public Variable(float value, VariableType interpolationType)
            {
                this.Value = value;
                this.InterpolationType = interpolationType;
            }

            public Variable GetCopy()
            {
                return new Variable(this.Value, this.InterpolationType);
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        private List<Variable> variables = new List<Variable>();
        private float zInEyeSpace;
        private int readIndex = 0;

        public void SetZInEyeSpace(float zInEyeSpace)
        {
            this.zInEyeSpace = zInEyeSpace;
        }

        public Interpolationvariables(float zInEyeSpace)
        {
            this.zInEyeSpace = zInEyeSpace;
        }

        public Interpolationvariables GetCopy()
        {
            var copy = new Interpolationvariables(this.zInEyeSpace)
            {
                variables = this.variables.Select(x => x.GetCopy()).ToList()
            };

            return copy;
        }

        public void AddVector3D(Vector3D vector, VariableType type)
        {
            this.variables.Add(new Variable(vector.X, type));
            this.variables.Add(new Variable(vector.Y, type));
            this.variables.Add(new Variable(vector.Z, type));
        }

        public Vector3D ReadVector3D()
        {
            float x = variables[this.readIndex++].Value;
            float y = variables[this.readIndex++].Value;
            float z = variables[this.readIndex++].Value;

            return new Vector3D(x, y, z);
        }

        public void AddVector2D(Vector2D vector, VariableType type)
        {
            this.variables.Add(new Variable(vector.X, type));
            this.variables.Add(new Variable(vector.Y, type));
        }

        public Vector2D ReadVector2D()
        {
            float x = variables[this.readIndex++].Value;
            float y = variables[this.readIndex++].Value;

            return new Vector2D(x, y);
        }

        public void AddVec4(Vector4D vec4, VariableType type)
        {
            this.variables.Add(new Variable(vec4.X, type));
            this.variables.Add(new Variable(vec4.Y, type));
            this.variables.Add(new Variable(vec4.Z, type));
            this.variables.Add(new Variable(vec4.W, type));
        }

        public Vector4D ReadVec4()
        {
            float x = variables[this.readIndex++].Value;
            float y = variables[this.readIndex++].Value;
            float z = variables[this.readIndex++].Value;
            float w = variables[this.readIndex++].Value;

            return new Vector4D(x, y, z, w );
        }

        public void AddVertex(Vertex vertex)
        {
            AddVector3D(vertex.Position, VariableType.WithPerspectiveDivision);
            AddVector3D(vertex.Normal, VariableType.WithoutPerspectiveDivision);
            AddVector3D(vertex.Tangent, VariableType.WithoutPerspectiveDivision);
            AddVector2D(vertex.TextcoordVector, VariableType.WithPerspectiveDivision);
        }

        public Vertex ReadVertex()
        {
            Vector3D pos = ReadVector3D();
            Vector3D normal = ReadVector3D();
            Vector3D tangent = ReadVector3D();
            Vector2D TextcoordVector = ReadVector2D();

            return new Vertex(pos, normal, tangent, TextcoordVector.X, TextcoordVector.Y);
        }

        public void StartOrEndPerspectiveDevision()
        {
            float invZ = 1.0f / this.zInEyeSpace;

            for (int i = 0; i < this.variables.Count; i++)
            {
                if (this.variables[i].InterpolationType == VariableType.WithPerspectiveDivision)
                {
                    this.variables[i].Value *= invZ;
                }
            }

            this.zInEyeSpace = invZ;
        }

        //f geht von 0 bis 1
        public static Interpolationvariables InterpolateLinear(Interpolationvariables p1, Interpolationvariables p2, float f)
        {
            Interpolationvariables p = new Interpolationvariables(p1.zInEyeSpace * (1 - f) + p2.zInEyeSpace * f);

            if (p1.variables.Count != p2.variables.Count) throw new Exception("Both interpolationpoints must be from the same type");

            for (int i = 0; i < p1.variables.Count; i++)
            {
                float v1 = p1.variables[i].Value;
                float v2 = p2.variables[i].Value;
                var type = p1.variables[i].InterpolationType;
                p.variables.Add(new Variable(v1 * (1 - f) + v2 * f, type));
            }

            return p;
        }

        public static Interpolationvariables InterpolateByzentric(Interpolationvariables p0, Interpolationvariables p1, Interpolationvariables p2, float w0, float w1, float w2)
        {
            Interpolationvariables p = new Interpolationvariables(w0 * p0.zInEyeSpace + w1 *p1.zInEyeSpace + w2 * p2.zInEyeSpace);

            if (p0.variables.Count != p1.variables.Count || p0.variables.Count != p2.variables.Count) throw new Exception("Both interpolationpoints must be from the same type");

            for (int i = 0; i < p0.variables.Count; i++)
            {
                float v0 = p0.variables[i].Value;
                float v1 = p1.variables[i].Value;
                float v2 = p2.variables[i].Value;
                var type = p0.variables[i].InterpolationType;
                p.variables.Add(new Variable(w0 * v0 + w1 * v1 + w2 * v2, type));
            }

            return p;
        }
    }
}
