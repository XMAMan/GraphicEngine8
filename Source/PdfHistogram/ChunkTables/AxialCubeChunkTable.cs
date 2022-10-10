using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphicGlobal;
using GraphicGlobal.MathHelper;
using GraphicMinimal;

namespace PdfHistogram
{
    //Ein axialter Würfel, welcher in Unterwürfel unterteilt wird
    public class AxialCubeChunkTable<T> where T : new()
    {
        public EntryWithIndizes[,,] Data { get; private set; } //2D-Grid
        private BoundingBox box;

        public AxialCubeChunkTable(BoundingBox box, int histogramSize)
        {
            this.Data = new EntryWithIndizes[histogramSize, histogramSize, histogramSize];
            for (int x = 0; x < histogramSize; x++)
                for (int y = 0; y < histogramSize; y++)
                    for (int z = 0; z < histogramSize; z++)
                    {
                        this.Data[x, y, z] = new EntryWithIndizes()
                        {
                            Data = new T(),
                            X = x,
                            Y = y,
                            Z = z,
                            Index = x * histogramSize * histogramSize + y * histogramSize + z
                        };
                }

            this.box = box;
            this.DifferentialVolume = (box.XSize / histogramSize) * (box.YSize / histogramSize) * (box.ZSize / histogramSize);
        }

        public double DifferentialVolume { get; private set; }

        public EntryWithIndizes this[Vector3D position]
        {
            get
            {
                float fx = (position.X - this.box.Min.X) / this.box.XSize;
                float fy = (position.Y - this.box.Min.Y) / this.box.YSize;
                float fz = (position.Z - this.box.Min.Z) / this.box.ZSize;

                int ix = MathExtensions.Clamp((int)(fx * this.Data.GetLength(0)), 0, this.Data.GetLength(0));
                int iy = MathExtensions.Clamp((int)(fy * this.Data.GetLength(1)), 0, this.Data.GetLength(1));
                int iz = MathExtensions.Clamp((int)(fz * this.Data.GetLength(2)), 0, this.Data.GetLength(2));

                return this.Data[ix, iy, iz];
            }
        }

        public IEnumerable<T> EntryCollection()
        {
            for (int x = 0; x < this.Data.GetLength(0); x++)
                for (int y = 0; y < this.Data.GetLength(1); y++)
                    for (int z = 0; z < this.Data.GetLength(1); z++)
                    {
                        yield return this.Data[x, y, z].Data;
                    }
        }

        public IEnumerable<EntryWithIndizes> EntryCollectionWithIndizes()
        {
            for (int x = 0; x < this.Data.GetLength(0); x++)
                for (int y = 0; y < this.Data.GetLength(1); y++)
                    for (int z = 0; z < this.Data.GetLength(1); z++)
                    {
                        yield return this.Data[x, y, z];
                    }
        }

        public class EntryWithIndizes
        {
            public T Data;
            public int X;
            public int Y;
            public int Z;
            public int Index;
        }
    }
}
