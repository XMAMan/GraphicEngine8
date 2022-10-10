using System;

namespace GraphicMinimal
{
    //Es hilft dabei eine Position mit einer 4*4-Matrix zu multiplizieren und dann diese homogene Variable weiter zu verarbeiten
    //Für die ungeclippte RGBA-Darstellung im PixelShader hilft es auch
    public class Vector4D
    {
        private float x, y, z, w;
        public Vector4D(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public Vector4D(Vector3D xyz, float w)
        {
            this.x = xyz.X;
            this.y = xyz.Y;
            this.z = xyz.Z;
            this.w = w;
        }

        public float X
        {
            get
            {
                return this.x;
            }
            set
            {
                this.x = value;
            }
        }

        public float Y
        {
            get
            {
                return this.y;
            }
            set
            {
                this.y = value;
            }
        }

        public float Z
        {
            get
            {
                return this.z;
            }
            set
            {
                this.z = value;
            }
        }

        public float W
        {
            get
            {
                return this.w;
            }
            set
            {
                this.w = value;
            }
        }

        public Vector3D XYZ
        {
            get
            {
                return new Vector3D(this.x, this.y, this.z);
            }
            set
            {
                this.x = value.X;
                this.y = value.Y;
                this.z = value.Z;
            }
        }

        public static Vector4D operator *(Vector4D v, float f)
        {
            return new Vector4D(v.X * f, v.Y * f, v.Z * f, v.W * f);
        }

        public static Vector4D operator +(Vector4D v1, Vector4D v2)
        {
            return new Vector4D(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z, v1.W + v2.W);
        }

        public static Vector4D operator -(Vector4D v1, Vector4D v2)
        {
            return new Vector4D(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z, v1.W - v2.W);
        }

        public Vector4D MultXyz(float factor)
        {
            return new Vector4D(this.X * factor, this.Y * factor, this.Z * factor, this.W);
        }
    }
}
