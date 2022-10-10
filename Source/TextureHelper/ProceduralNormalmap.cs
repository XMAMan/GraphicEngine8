using System;
using GraphicGlobal;
using GraphicMinimal;

namespace TextureHelper
{
    public interface IProceduralNormalmap
    {
        Vector3D GetNormal(Vertex point);
    }

    public abstract class ProceduralNormalmap
    {
        protected Matrix3x3 textureMatrix;
        protected ProceduralNormalmap(Matrix3x3 textureMatrix)
        {
            this.textureMatrix = textureMatrix;
        }

        public virtual Vector3D GetNormal(Vertex point)
        {
            Vector3D texCoords = this.textureMatrix * new Vector3D(point.TexcoordU, point.TexcoordV, 1);
            Vector3D tangentNormal = GetNormalInTangentSpace(texCoords.X, texCoords.Y);
            if (tangentNormal.Z < 0) tangentNormal.Z = 0;

            Matrix4x4 tangentToEyeMatrix = Matrix4x4.TBNMatrix(point.Normal, point.Tangent);
            return Vector3D.Normalize(Matrix4x4.MultDirection(tangentToEyeMatrix, tangentNormal));
        }


        //Normale zeigt im einfachsten Falle nach (0,0,1)
        protected abstract Vector3D GetNormalInTangentSpace(float texcoordU, float texcoordV);
    }

    public class ProceduralTextureSinU : ProceduralNormalmap, IProceduralNormalmap
    {
        private float waveCount = 500;
        private float waveHeight = 2;

        public ProceduralTextureSinU(Matrix3x3 textureMatrix)
            : base(textureMatrix) { }

        protected override Vector3D GetNormalInTangentSpace(float texcoordU, float texcoordV)
        {
            float y = (float)Math.Sin(texcoordU * waveCount) * waveHeight;
            return Vector3D.Normalize(new Vector3D(-y, 0, 1));
        }
    }

    public class ProceduralTextureSinUCosV : ProceduralNormalmap, IProceduralNormalmap
    {
        private float waveCount = 100;
        private float waveHeight = 0.5f;

        public ProceduralTextureSinUCosV(Matrix3x3 textureMatrix)
            : base(textureMatrix) { }

        protected override Vector3D GetNormalInTangentSpace(float texcoordU, float texcoordV)
        {
            float x = (float)Math.Cos(texcoordU * waveCount) * waveHeight;
            float y = (float)Math.Sin(texcoordV * waveCount) * waveHeight;
            return Vector3D.Normalize(new Vector3D(x, y, 1));
        }
    }

    public class ProceduralTextureTent : ProceduralNormalmap, IProceduralNormalmap
    {
        private int count = 10; //Anzahl der Kästchen
        private float bias = 0.2f;
        private float inclinedFactor = 2; //Schrägheitsfaktor
        public ProceduralTextureTent(Matrix3x3 textureMatrix)
            : base(textureMatrix) { }

        protected override Vector3D GetNormalInTangentSpace(float texcoordU, float texcoordV)
        {
            int x = (int)(texcoordU * count);
            int y = (int)(texcoordV * count);

            //fx/fy ist die Position innerhalb eines Kästchen und liegt im Bereich von 0..1
            float fx = (texcoordU - x / (float)count) * count;
            float fy = (texcoordV - y / (float)count) * count;

            float dx = fx < 0.5f ? fx : 1 - fx;
            float dy = fy < 0.5f ? fy : 1 - fy;

            float d = Math.Min(dx, dy);

            if (d < bias)
            {
                if (dx < dy)
                {
                    if (fx < 0.5f)
                        return Vector3D.Normalize(new Vector3D(+1 * inclinedFactor, 0, 1));
                    else
                        return Vector3D.Normalize(new Vector3D(-1 * inclinedFactor, 0, 1));
                }
                else
                {
                    if (fy < 0.5f)
                        return Vector3D.Normalize(new Vector3D(0, +1 * inclinedFactor, 1));
                    else
                        return Vector3D.Normalize(new Vector3D(0, -1 * inclinedFactor, 1));
                }
            }

            return new Vector3D(0, 0, 1);
        }
    }

    public class ProceduralTexturePerlin : IProceduralNormalmap
    {
        private int GeneratorSize = 256;
        private PerlinNoise perlinNoise1 = new PerlinNoise(99);
        private PerlinNoise perlinNoise2 = new PerlinNoise(59);

        private BoundingBox box;
        private float noiseFactor;

        public ProceduralTexturePerlin(BoundingBox box, float noiseFactor)
        {
            this.box = box;
            this.noiseFactor = noiseFactor;
        }

        //Diese Funktion habe ich am 12.7.2019 mir mal eben so ausgedacht
        public Vector3D GetNormal(Vertex point)
        {
            float f = noiseFactor; //Um so größer, um so feiner ist die Huckellandschaft (Beispielwert: 0.1)

            Vector3D local = point.Position - this.box.Min;
            Vector3D pos = new Vector3D(local.X / this.box.MaxEdge, local.Y / this.box.MaxEdge, local.Z / this.box.MaxEdge) * this.GeneratorSize;

            float noiseCoefx = this.perlinNoise1.GetNoise(f * pos);
            float noiseCoefy = this.perlinNoise2.GetNoise(f * pos);


            noiseCoefx = (noiseCoefx + 1) / 2;
            noiseCoefy = (noiseCoefy + 1) / 2;

            Vector3D w = point.Normal,
                   u = Vector3D.Cross(point.Normal, point.Tangent),
                   v = Vector3D.Cross(w, u);

            float thetaScatterFactor = 0.5f; //Zahl zwischen 0 und 1; 0=Die Normale zeigt immer nach vorne; 1= Normale geht von 0 bis 90°

            float phi = noiseCoefx * 2 * (float)Math.PI;
            float cosTheta = noiseCoefy * thetaScatterFactor + (1 - thetaScatterFactor);
            float sinTheta = (float)Math.Sqrt(1 - cosTheta * cosTheta);

            Vector3D d = Vector3D.Normalize((u * (float)Math.Cos(phi) * sinTheta + v * (float)Math.Sin(phi) * sinTheta + w * cosTheta));
            if (d * point.Normal < 0) d = -d;
            return d;
        }
    }

}
