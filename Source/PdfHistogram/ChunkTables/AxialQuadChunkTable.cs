using GraphicGlobal;
using GraphicGlobal.MathHelper;
using GraphicMinimal;
using System.Collections.Generic;

namespace PdfHistogram
{
    //Ein Viereck, was im 3D-Raum axial liegt. Darauf sollen Punkte analysiert werden
    public class AxialQuadChunkTable<T> where T : new()
    {
        public EntryWithIndizes[,] Data { get; private set; } //2D-Grid
        private Vector3D corner;
        private int axis1;
        private int axis2;
        private float quadSize;

        //Es wird ein Viereck, was axial im 3D-Raum liegt. Die eine Kante von den Viereck liegt auf axis1 (0=X;1=Y;2=Z); die andere Kante auf axis2
        //Die Kantenlänge von dem Quad ist 'quadSize'
        //Das Histogram ist ein 2D-Array mit histogramSize*histogramSize Feldern
        public AxialQuadChunkTable(Vector3D quadCenterPosition, int axis1, int axis2, float quadSize, int histogramSize)
        {
            this.Data = new EntryWithIndizes[histogramSize, histogramSize];
            for (int x = 0; x < histogramSize; x++)
                for (int y = 0; y < histogramSize; y++)
                {
                    this.Data[x, y] = new EntryWithIndizes()
                    {
                        Data = new T(),
                        X = x,
                        Y = y,
                    };
                }


            Vector3D v = new Vector3D(0, 0, 0);
            v[axis1] = quadSize;
            v[axis2] = quadSize;
            this.corner = quadCenterPosition - v / 2;
            this.axis1 = axis1;
            this.axis2 = axis2;
            this.quadSize = quadSize;

            this.DifferentialArea = quadSize / histogramSize * quadSize / histogramSize;
        }

        public double DifferentialArea { get; private set; }

        public EntryWithIndizes this[Vector3D position]
        {
            get
            {
                float f1 = (position[this.axis1] - this.corner[this.axis1]) / this.quadSize;
                float f2 = (position[this.axis2] - this.corner[this.axis2]) / this.quadSize;

                int i1 = MathExtensions.Clamp((int)(f1 * this.Data.GetLength(0)), 0, this.Data.GetLength(0));
                int i2 = MathExtensions.Clamp((int)(f2 * this.Data.GetLength(1)), 0, this.Data.GetLength(1));

                return this.Data[i1, i2];
            }
        }

        public IEnumerable<T> EntryCollection()
        {
            for (int x = 0; x < this.Data.GetLength(0); x++)
                for (int y = 0; y < this.Data.GetLength(1); y++)
                {
                    yield return this.Data[x, y].Data;
                }
        }

        public IEnumerable<EntryWithIndizes> EntryCollectionWithIndizes()
        {
            for (int x = 0; x < this.Data.GetLength(0); x++)
                for (int y = 0; y < this.Data.GetLength(1); y++)
                {
                    yield return this.Data[x, y];
                }
        }

        public class EntryWithIndizes
        {
            public T Data;
            public int X;
            public int Y;
        }
    }
}
