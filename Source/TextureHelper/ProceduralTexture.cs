using System;
using GraphicMinimal;

namespace TextureHelper
{
    public interface IProceduralTexture
    {
        Vector3D GetColor(Vector3D position);
    }

    public class ProceduralTextureTile : IProceduralTexture
    {
        private float tileSize; //Um so größer, um so kleiner sind die Kacheln

        public ProceduralTextureTile(float tileSize = 10)
        {
            this.tileSize = tileSize;
        }

        public Vector3D GetColor(Vector3D position)
        {
            float f = ((int)(Minus(position.X) * this.tileSize) ^ (int)(Minus(position.Y) * this.tileSize) ^ (int)(Minus(position.Z) * this.tileSize)) & 1;
            return new Vector3D(1, 1, 1) * f;
        }

        private float Minus(float f)
        {
            return f < 0 ? (f + this.tileSize) : f;
        }
    }

    public class ProceduralTextureWood : IProceduralTexture
    {
        private Vector3D center;
        private float size;
        private float ringFactor; //Um So größer, um so kleiner werden die Ringe
        private float noiseFactor; //Um so größer, um so mehr Noise
        
        public ProceduralTextureWood(Vector3D center, float size, float ringFactor = 10, float noiseFactor = 5)
        {
            this.center = center;
            this.size = size;
            this.ringFactor = ringFactor;
            this.noiseFactor = noiseFactor;
        }

        //center .. Mitte von den 3D-Objekt, was Holztexture bekommen soll
        //position .. Punkt auf/bei 3D-Objekt, dessen Farbwert bestimmt werden soll
        public Vector3D GetColor(Vector3D position)
        {
            Vector3D stemDirection = new Vector3D(0, 1, 1);

            Vector3D stemProjection = this.center + Vector3D.Projektion(position - this.center, stemDirection);
            float ringDistance = (position - stemProjection).Length() * this.size;

            float centerDistance = (stemProjection - this.center).Length() * this.size;
            ringDistance += PulseTrain(centerDistance, this.noiseFactor, 0);

            float sphereNoise = (position - this.center).Length() * this.size;
            ringDistance += PulseTrain(sphereNoise, this.noiseFactor, 0);

            float ringPower = PulseTrain(ringDistance, this.ringFactor, 0.2f);

            return new Vector3D(1, 0.75f, 0.45f) * ringPower;
        }

        private static float PulseTrain(float f, float hu, float bias)
        {
            float sin = (float)Math.Sin(f * hu) / 2 + 0.5f;
            if (sin < bias) sin = bias;
            if (sin > 1 - bias) sin = 1 - bias;
            return sin;
        }
    }

    public class ProceduralTextureToonShader : IProceduralTexture
    {
        private Vector3D noLightColor;
        private Vector3D center;
        private float objSize;

        public ProceduralTextureToonShader(Vector3D noLightColor, Vector3D center, float objSize)
        {
            this.noLightColor = noLightColor;
            this.center = center;
            this.objSize = objSize;
        }

        public Vector3D GetColor(Vector3D position)
        {
            float lightPower = Vector3D.Normalize(position - this.center) * new Vector3D(0, 0, 1);

            return Stair(lightPower * 2, 0.5f * this.objSize) * this.noLightColor;
        }

        private static float Stair(float f, float stairSize)
        {
            return (int)(f / stairSize) * stairSize;
        }
    }

    public class ProceduralTextureHatch : IProceduralTexture
    {
        private Vector3D noLightColor;
        private Vector3D center;

        public ProceduralTextureHatch(Vector3D noLightColor, Vector3D center)
        {
            this.noLightColor = noLightColor;
            this.center = center;
        }

        public Vector3D GetColor(Vector3D position)
        {
            float lightPower = Vector3D.Normalize(position - this.center) * new Vector3D(0, 0, 1);

            return TileEffect(position, lightPower * 100) * this.noLightColor * lightPower;
        }

        private static float TileEffect(Vector3D position, float tileSize)
        {
            return ((int)(position.X * tileSize) ^ (int)(position.Y * tileSize) ^ (int)(position.Z * tileSize)) & 1;
        }
    }
}
