using System;
using GraphicMinimal;

namespace GraphicGlobal
{
    //Transformiert ein Richtungsvektor in ein kartesisches Koordiantensystem, was durch 3 senkrechte X-Y-Z-Vektoren beschrieben wird. Ein Frame ist ein Koordinatensystem
    //Quelle: SmallVCM
    public class Frame
    {
        public Vector3D Normal { get; private set; }
        public Vector3D Tangent { get; private set; }
        public Vector3D Binormal { get; private set; }

        //Linke Handregel: Daumen=Normale;Mittelfinger=Tangente;Zeigerfinger=Binormale (Hinweis: Die Vorzeichen müssen von allen Vektoren Positiv sein, da Minus * Minus sich beim Transformieren auslöscht)
        public Frame(Vector3D tangent, Vector3D binormal, Vector3D normal)
        {
            this.Normal = normal;
            this.Tangent = tangent;
            this.Binormal = binormal;
        }

        public Frame(Vector3D normal)
        {
            this.Normal = normal;
            this.Binormal = -Vector3D.Normalize(Vector3D.Cross((Math.Abs(normal.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), normal));
            this.Tangent  = -Vector3D.Cross(normal, this.Binormal);
        }

        public Vector3D ToWorld(Vector3D direction)
        {
            return this.Tangent * direction.X + this.Binormal * direction.Y + this.Normal * direction.Z;
        }

        public Vector3D ToLocal(Vector3D direction)
        {
            return new Vector3D(direction * this.Tangent,
                              direction * this.Binormal,
                              direction * this.Normal);
        }

        public Vector3D GetDirectionFromPhiAndTheta(double theta, double phi)
        {
            return Vector3D.Normalize(((-this.Tangent) * (float)Math.Cos(phi) * (float)Math.Sin(theta) + this.Binormal * (float)Math.Sin(phi) * (float)Math.Sin(theta) + this.Normal * (float)Math.Cos(theta)));
        }

        public Vector3D GetDirectionFromPhiAndCosTheta(double cosTheta, double phi)
        {
            double sinTheta = Math.Sqrt(1 - cosTheta * cosTheta);
            return Vector3D.Normalize(((-this.Tangent) * (float)Math.Cos(phi) * (float)sinTheta + this.Binormal * (float)Math.Sin(phi) * (float)sinTheta + this.Normal * (float)cosTheta));
        }
    }
}
