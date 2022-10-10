using GraphicMinimal;
using System.Drawing;
using TextureHelper.TexturMapping;

namespace GraphicPipelineCPU.Textures
{
    class Cubemap
    {
        public ColorTexture[] Color; //[ CubemapSide ]

        public Cubemap(int width, int height)
        {
            this.Color = new ColorTexture[6];
            for (int i = 0; i < 6; i++)
            {
                this.Color[i] = new ColorTexture(-1, width, height);
            }
        }

        public Color GetCubemapSample(Vector3D direction)
        {
            //Hinweis zum Thema Cubmapping und Spheremapping:

            //Beim Spheremapping ist ein 3D-Punkt P gegeben und ich such die Kugel mit dem Radius, so das P auf der
            //Kugel liegt. die UV-Koordinate von P auf der skalierten Kugel ist dann die gesuchte UV-Koordinate

            //Beim Cubemapping ist ein 3D-Punkt P gegeben und ich versuche den Einheitswürfel (alle Kanten haben Länge 1)
            //so zu skalieren, das P auf dem Würfel liegt. Die UV-Koordinate von P auf den skalierten Würfel ist die gesuchte Koordinate.

            var coords = CubeMapping.Mappi(direction);

            return this.Color[coords.SideIndex].TextureMappingPoint(TextureMode.Clamp, coords.TextCoord.X, coords.TextCoord.Y);
        }

        //Diese Funktion macht genau das gleiche wie GetCubemapSample nur das der Quelltext nicht von Wikipedia stammt sondern von mir
        public Color GetCubemapSample1(Vector3D direction)
        {
            Vector3D[] directions = new Vector3D[]{new Vector3D(+1,+0,+0),
                                               new Vector3D(-1,+0,+0),
                                               new Vector3D(+0,+1,+0),
                                               new Vector3D(+0,-1,+0),
                                               new Vector3D(+0,+0,+1),
                                               new Vector3D(+0,+0,-1)};

            float minAngle = float.MaxValue;
            int index = 0;
            for (int i = 0; i < 6; i++)
            {
                float angle = Vector3D.AngleDegree(direction, directions[i]);
                if (angle < minAngle)
                {
                    minAngle = angle;
                    index = i;
                }
            }

            float X = 0, Y = 0;
            if (index == 0) { X = direction.Z; Y = direction.Y; }
            if (index == 1) { X = direction.Z; Y = direction.Y; }
            if (index == 2) { X = direction.Z; Y = direction.X; }
            if (index == 3) { X = direction.Z; Y = direction.X; }
            if (index == 4) { X = direction.X; Y = direction.Y; }
            if (index == 5) { X = direction.X; Y = direction.Y; }

            int Xi = (int)(Mix(X, -1, +1) * this.Color[index].Width);
            int Yi = (int)(Mix(Y, -1, +1) * this.Color[index].Height);
            if (Xi < 0) Xi = 0;
            if (Xi >= this.Color[index].Width) Xi = this.Color[index].Width - 1;
            if (Yi < 0) Yi = 0;
            if (Yi >= this.Color[index].Height) Yi = this.Color[index].Height - 1;

            return this.Color[index][Xi, Yi];
        }

        //Gibt ein Wert zwischen 0 und 1 zurück
        private static float Mix(float f, float minf, float maxf)
        {
            if (f < minf) f = minf;
            if (f > maxf) f = maxf;
            return (f - minf) / (maxf - minf);
        }
    }
}
