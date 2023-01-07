using FullPathGenerator;
using GraphicGlobal;
using SubpathGenerator;
using System.Drawing;

namespace RaytracingMethods.McVcm
{
    //Speichert für jedes Pixel ein EyeSubPath
    //Wird benötigt für PT/DL für ein gegebenes Pixel den zugehörigen Pfad zu bekommen
    //Bei VC wird zu jeden Pixel sein zufällig permutierter Pixel/Subpfad zurückgegeben
    //Für VM wird hier eine Photonmap gespeichert, welche aus allen EyeSubpaths besteht
    class EyeMap
    {
        class Pixel
        {
            public EyeSubPath Path;
            public Point RandPos; //Erlaubt den zufällig permutierten Zugriff auf die EyeMap (Fürs VertexConnection)

            public Pixel(EyeSubPath path, Point pos)
            {
                this.Path = path;
                this.RandPos = pos;
            }
        }

        private readonly Pixel[,] Map;
        public readonly FullPathFrameData FrameData;

        private EyeMap(Pixel[,] map, FullPathFrameData frameData)
        {
            this.Map = map;
            this.FrameData = frameData;
        }

        public static EyeMap Create(EyeSubPath[,] map, FullPathFrameData frameData, IRandom rand)
        {
            Pixel[,] pixMap = new Pixel[map.GetLength(0), map.GetLength(1)];

            for (int x=0;x<map.GetLength(0);x++)
                for (int y=0;y<map.GetLength(1);y++)
                {
                    pixMap[x, y] = new Pixel(map[x, y], new Point(x, y));
                }

            //EyeMap-RandPos zufällig vertauschen (Damit kann ich für jeden PT-Pfad ein zufälligen VC-Pfad zuordnen)
            for (int x = 0; x < pixMap.GetLength(0); x++)
                for (int y = 0; y < pixMap.GetLength(1); y++)
                {
                    int x1 = rand.Next(pixMap.GetLength(0));
                    int y1 = rand.Next(pixMap.GetLength(1));

                    var tmp = pixMap[x, y].RandPos;
                    pixMap[x, y].RandPos = pixMap[x1, y1].RandPos;
                    pixMap[x1, y1].RandPos = tmp;
                }

            return new EyeMap(pixMap, frameData);
        }

        public EyeSubPath GetPathtracingPath(int x, int y)
        {
            return this.Map[x, y].Path;
        }

        public EyeSubPath GetVertexConnectionPath(int x, int y)
        {
            var p = this.Map[x, y].RandPos;
            return this.Map[p.X, p.Y].Path;
        }
    }
}
