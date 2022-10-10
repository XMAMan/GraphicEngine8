using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Radiosity._03_ViewFactor
{
    public class VisibleMatrix
    {
        public enum VisibleValue { NotSet, Visible, NotVisible };

        private readonly VisibleValue[,] matrix;
        public VisibleMatrix(int size)
        {
            this.matrix = new VisibleValue[size, size];
        }

        public int Size { get { return this.matrix.GetLength(0); } }

        public VisibleValue this[int index1, int index2]
        {
            get
            {
                if (index1 > index2) SwapNum(ref index1, ref index2);
                return this.matrix[index1, index2];
            }
            set
            {
                if (index1 > index2) SwapNum(ref index1, ref index2);
                this.matrix[index1, index2] = value;
            }
        }

        private static void SwapNum(ref int x, ref int y)
        {
            int tempswap = x;
            x = y;
            y = tempswap;
        }

        private byte[] ToByteArray()
        {
            int size = this.matrix.GetLength(0) * this.matrix.GetLength(1) / 8 + 1;
            byte[] data = new byte[size];
            for (int y = 0; y < this.matrix.GetLength(1); y++)
                for (int x=0;x<this.matrix.GetLength(0);x++)
                {
                    if (this.matrix[x,y] == VisibleValue.Visible)
                    {
                        int i = y * this.matrix.GetLength(0) + x;
                        int i1 = i / 8;
                        int i2 = i % 8;
                        data[i1] |= (byte)(1 << i2);
                    }                    
                }
            return data;
        }

        private void ReadFromByteArray(byte[]data)
        {
            for (int y = 0; y < this.matrix.GetLength(1); y++)
                for (int x = 0; x < this.matrix.GetLength(0); x++)
                {
                    int i = y * this.matrix.GetLength(0) + x;
                    int i1 = i / 8;
                    int i2 = i % 8;
                    bool isVisible = (data[i1] & (byte)(1 << i2)) != 0;
                    this.matrix[x, y] = isVisible ? VisibleValue.Visible : VisibleValue.NotVisible;
                }
        }

        public void WriteToFile(string fileName)
        {
            byte[] data = ToByteArray();

            FileStream  writeStream = new FileStream(fileName, FileMode.Create);
            BinaryWriter writeBinay = new BinaryWriter(writeStream);
            writeBinay.Write(this.matrix.GetLength(0));
            writeBinay.Write(data.Length);
            writeBinay.Write(data);
            writeBinay.Close();
        }

        public VisibleMatrix(string fileName)
        {
            using (BinaryReader b = new BinaryReader(
            File.Open(fileName, FileMode.Open)))
            {
                int matrixSize = b.ReadInt32();
                this.matrix = new VisibleValue[matrixSize, matrixSize];
                int bitMatrixSize = b.ReadInt32();
                byte[] data = b.ReadBytes(bitMatrixSize);
                ReadFromByteArray(data);
            }
        }
    }
}
