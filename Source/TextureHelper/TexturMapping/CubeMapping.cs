using GraphicMinimal;
using System;

namespace TextureHelper.TexturMapping
{
    public class CubeMapReadResult
    {
        public int SideIndex; //0..5
        public Vector2D TextCoord; //U=0..1; V=0..1
    }

    public class CubeMapping : ITextureMapping
    {
        private Vector3D center;
        public CubeMapping(Vector3D center)
        {
            this.center = center;
        }
        public Vector2D Map(Vector3D pos)
        {
            return CubeMapping.Mappi(Vector3D.Normalize(pos - this.center)).TextCoord;
        }


        //Quelle: https://en.wikipedia.org/wiki/Cube_mapping
        //Dieser Algorithmus macht das gleiche wie die samplerCube.texture(Cubemap, ReflectDir)-Funktion bei OpenGL
        //Wenn ich will, dass sich diese Funktion exakt wie bei DirectX die 
        //TextureCube.CubeMapTexture.Sample(samAnisotropic, reflectionVector);  - Funktion
        //verhält, muss ich folgendes zurück geben: new Vector2D(u, 1 - v)
        //Damit die Reflektion aber so wie beim Raytracer aussieht müsste ich new Vector2D(1 - u, 1 - v) zurück geben
        //D.h. DirectX als auch OpenGL verwenden beide geflippte UV-Koordianten und sind somit falsch
        //OpenGL: new Vector2D(u, v)
        //DirectX: new Vector2D(u, 1 - v)
        //Raytracer: new Vector2D(1 - u, 1 - v)
        //Wenn ich den Up-Vektor negiere, hat das den gleichen Effekt wie new Vector2D(1 - u, 1 - v)
        //D.h. wenn ich den Up-Vektor negiere sieht OpenGL korrekt aus und bei DirectX steht alles aufm Kopf
        public static CubeMapReadResult Mappi(Vector3D direction)
        {
            float absX = Math.Abs(direction.X);
            float absY = Math.Abs(direction.Y);
            float absZ = Math.Abs(direction.Z);

            bool isXPositive = direction.X > 0 ? true : false;
            bool isYPositive = direction.Y > 0 ? true : false;
            bool isZPositive = direction.Z > 0 ? true : false;

            float maxAxis = float.NaN, uc = float.NaN, vc = float.NaN;
            int index = -1;

            // POSITIVE X
            if (isXPositive && absX >= absY && absX >= absZ)
            {
                // u (0 to 1) goes from +z to -z
                // v (0 to 1) goes from -y to +y
                maxAxis = absX;
                uc = -direction.Z;
                vc = direction.Y;
                index = 0;
            }
            // NEGATIVE X
            if (!isXPositive && absX >= absY && absX >= absZ)
            {
                // u (0 to 1) goes from -z to +z
                // v (0 to 1) goes from -y to +y
                maxAxis = absX;
                uc = direction.Z;
                vc = direction.Y;
                index = 1;
            }
            // POSITIVE Y
            if (isYPositive && absY >= absX && absY >= absZ)
            {
                // u (0 to 1) goes from -x to +x
                // v (0 to 1) goes from +z to -z
                maxAxis = absY;
                uc = direction.X;
                vc = -direction.Z;
                index = 2;
            }
            // NEGATIVE Y
            if (!isYPositive && absY >= absX && absY >= absZ)
            {
                // u (0 to 1) goes from -x to +x
                // v (0 to 1) goes from -z to +z
                maxAxis = absY;
                uc = direction.X;
                vc = direction.Z;
                index = 3;
            }
            // POSITIVE Z
            if (isZPositive && absZ >= absX && absZ >= absY)
            {
                // u (0 to 1) goes from -x to +x
                // v (0 to 1) goes from -y to +y
                maxAxis = absZ;
                uc = direction.X;
                vc = direction.Y;
                index = 4;
            }
            // NEGATIVE Z
            if (!isZPositive && absZ >= absX && absZ >= absY)
            {
                // u (0 to 1) goes from +x to -x
                // v (0 to 1) goes from -y to +y
                maxAxis = absZ;
                uc = -direction.X;
                vc = direction.Y;
                index = 5;
            }

            // Convert range from -1 to 1 to 0 to 1
            float u = 0.5f * (uc / maxAxis + 1.0f);
            float v = 0.5f * (vc / maxAxis + 1.0f);

            return new CubeMapReadResult()
            {
                SideIndex = index,
                TextCoord = new Vector2D(u, v)
            };
        }
    }
}
