using GraphicGlobal;
using GraphicMinimal;

namespace TextureHelper
{
    public class EbertNoiseGenerator3D
    {
        private EbertNoiseGenerator[] noise = new EbertNoiseGenerator[3];
        private BoundingBox box;
        private int size;
        private float noiseFaktor;
        public EbertNoiseGenerator3D(BoundingBox box, IRandom rand, float noiseFaktor, int size = 64)
        {
            this.box = box;
            for (int i = 0; i < noise.Length; i++) noise[i] = new EbertNoiseGenerator(rand, size);
            this.size = size;
            this.noiseFaktor = noiseFaktor;
        }

        private Vector3D TransformToSolidSpace(Vector3D point)
        {
            Vector3D t = point - this.box.Min;
            return new Vector3D(t.X / this.box.XSize, t.Y / this.box.YSize, t.Z / this.box.ZSize) * this.size;
        }

        //Returnwert: -1 .. +1
        public Vector3D GetNoiseVector(Vector3D point)
        {
            var p = TransformToSolidSpace(point);
            return new Vector3D(
                this.noise[0].GetNoise(p * this.noiseFaktor),
                this.noise[1].GetNoise(p * this.noiseFaktor),
                this.noise[2].GetNoise(p * this.noiseFaktor)
                );
        }
    }

    //Quelle: David Ebert/Perlin -> Ebert Perlin Texturing and Modeling a Procedural Approach 1998 Seite 241
    public class EbertNoiseGenerator
    {
        private float[,,] noise;
        private int size;

        //Size muss eine 2er-Potenz sein, da nur so die Binäre Und-Operation für das kostengünstige Modulo geht
        public EbertNoiseGenerator(IRandom rand, int size = 64)
        {
            this.size = size;

            this.noise = new float[size + 1, size + 1, size + 1];
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    for (int k = 0; k < size; k++)
                    {
                        this.noise[i, j, k] = (float)rand.NextDouble();
                    }

            for (int i = 0; i < size + 1; i++)
                for (int j = 0; j < size + 1; j++)
                    for (int k = 0; k < size + 1; k++)
                    {
                        int ii = (i == size) ? 0 : i;
                        int jj = (j == size) ? 0 : j;
                        int kk = (k == size) ? 0 : k;
                        noise[i, j, k] = noise[ii, jj, kk];
                    }
        }

        // ////////////////////////////////////////////////////// 
        // Calc_noise 
        // This is basically how the trilinear interpolation works. I 
        // lerp down the left front edge of the cube, then the right 
        // front edge of the cube(p_l, p_r). Then I lerp down the left 
        // back and right back edges of the cube (p_l2, p_r2). Then I 
        // lerp across the front face between p_l and p_r (p_face1). Then 
        // I lerp across the back face between p_l2 and p_r2 (p_face2). 
        // Now I lerp along the line between p_face1 and p_face2. 
        // ////////////////////////////////////////////////////// 
        public float GetNoise(Vector3D point)
        {
            float t1;
            float p_l, p_l2,// value lerped down left side of face 1 & face 2 
                  p_r, p_r2, // value lerped down left side of face 1 & face 2 
                  p_face1, // value lerped across face 1 (x-y plane ceil of z) 
                  p_face2, // value lerped across face 2 (x-y plane floor of z) 
                  p_final; //value lerped through cube (in z) 

            int x, y, z, px, py, pz;
            px = (int)point.X;
            py = (int)point.Y;
            pz = (int)point.Z;
            x = px & (this.size - 1); // make sure the values are in the table 
            y = py & (this.size - 1); // Effectively replicates table throughout space 
            z = pz & (this.size - 1);

            t1 = point.Y - py;
            p_l = noise[x, y, z + 1] + t1 * (noise[x, y + 1, z + 1] - noise[x, y, z + 1]);
            p_r = noise[x + 1, y, z + 1] + t1 * (noise[x + 1, y + 1, z + 1] - noise[x + 1, y, z + 1]);
            p_l2 = noise[x, y, z] + t1 * (noise[x, y + 1, z] - noise[x, y, z]);
            p_r2 = noise[x + 1, y, z] + t1 * (noise[x + 1, y + 1, z] - noise[x + 1, y, z]);

            t1 = point.X - px;
            p_face1 = p_l + t1 * (p_r - p_l);
            p_face2 = p_l2 + t1 * (p_r2 - p_l2);
            t1 = point.Z - pz;
            p_final = p_face2 + t1 * (p_face1 - p_face2);

            return (p_final) - 0.5f;
        }

        public Vector3D GetNoiseVector(Vector3D point)
        {
            float n1 = GetNoise(point);
            float n2 = GetNoise(new Vector3D(point.Y, point.Z, point.X));
            float n3 = GetNoise(new Vector3D(point.Y, point.X, point.Z));
            return new Vector3D(n1, n2, n3);
        }

        // /////////////////////////////////////////////////// 
        // TURBULENCE (Diese Funktion hat David von Perlin 1985 geklaut)
        // /////////////////////////////////////////////////// 
        public float Turbulence(Vector3D point, float pixelSize)
        {
            float t = 0;

            for (float scale = 1.0f; scale > pixelSize; scale /= 2.0f)
            {
                point.X = point.X / scale;
                point.Y = point.Y / scale;
                point.Z = point.Z / scale;
                t += GetNoise(point) * scale;
            }

            return t;
        }
    }


}
