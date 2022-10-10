using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphicMinimal;

namespace PdfHistogram
{
    //Man kann Celle innerhalb von ein 2D-Rechteck über ein 2D-Punkt ansprechen. Das Rechteck liegt im 1. Quadrant und liebt bei Punkt (0,0) und geht bis (Width,Height)
    class RectangleChunkTable<T> where T : new()
    {
        public T[,] Data { get; private set; } //x=[0..Width] | y=[0..Height]
        private Size size;

        public RectangleChunkTable(int xSize, int ySize, Size size)
        {
            this.Data = new T[xSize, ySize];
            for (int x = 0; x < xSize; x++)
                for (int y = 0; y < ySize; y++)
                {
                    this.Data[x, y] = new T();
                }

            this.size = size;
            this.DifferentialSurfaceArea = (size.Width / (float)xSize) * (size.Height / (float)ySize);
        }

        public float DifferentialSurfaceArea { get; private set; }

        public T this[Vector2D point]
        {
            get
            {
                int x = Math.Min((int)(point.X / (this.size.Width) * Data.GetLength(0)), Data.GetLength(0) - 1);
                int y = Math.Min((int)(point.Y / (this.size.Height) * Data.GetLength(1)), Data.GetLength(1) - 1);
                return this.Data[x, y];
            }
        }
    }
}
