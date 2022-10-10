using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;

namespace GraphicMinimal
{
    [Serializable()]
    public class ImageBuffer
    {
        private Vector3D[,] buffer;

        public int Width
        {
            get
            {
                return this.buffer.GetLength(0);
            }
        }

        public int Height
        {
            get
            {
                return this.buffer.GetLength(1);
            }
        }

        public Vector3D this[int x, int y]
        {
            get
            {
                return this.buffer[x, y];
            }
            set
            {
                this.buffer[x, y] = value;
            }
        }

        public ImageBuffer(ImageBuffer copy)
            :this(copy.Width, copy.Height)
        {
            for (int x = 0; x < copy.Width; x++)
            {
                for (int y = 0; y < copy.Height; y++)
                    this.buffer[x, y] = new Vector3D(copy[x,y]);
            }
        }

        public ImageBuffer(int width, int height)
        {
            this.buffer = new Vector3D[width, height];
        }

        public ImageBuffer(int width, int height, Vector3D initColor)
        {
            this.buffer = new Vector3D[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                    this.buffer[x, y] = new Vector3D(initColor);
            }
        }

        public ImageBuffer(Bitmap bitmap)
            : this(bitmap.Width, bitmap.Height)
        {
            for (int x = 0; x < this.Width; x++)
                for (int y = 0; y < this.Height; y++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    this[x, y] = new Vector3D(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
                }
        }

        public ImageBuffer(Vector3D[,] buffer)
        {
            this.buffer = buffer;
        }

        public ImageBuffer(string fileName)
        {
            //So benötige ich 50Mb im Fullscreen:
            var stream = File.Open(fileName, FileMode.Open);
            var bformatter = new BinaryFormatter();            
            this.buffer = (Vector3D[,])bformatter.Deserialize(stream);
            stream.Close();

            //So brauche ich auch 50Mb im Fullscreen:
            //Vector3D[,] data = null;
            //using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            //{
            //    int width = reader.ReadInt32();
            //    int height = reader.ReadInt32();
            //    data = new Vector3D[width, height];
            //    for (int y = 0; y < height; y++)
            //        for (int x = 0; x < width; x++)
            //        {
            //            float r = reader.ReadSingle();
            //            float g = reader.ReadSingle();
            //            float b = reader.ReadSingle();
            //            data[x, y] = new Vector3D(r, g, b);
            //        }
            //}
            //this.buffer = data;
        }

        public void WriteToFile(string fileName)
        {
            Stream stream = File.Open(fileName, FileMode.Create);
            BinaryFormatter bformatter = new BinaryFormatter();
            bformatter.Serialize(stream, this.buffer);
            stream.Close();

            //using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            //{
            //    writer.Write(this.buffer.GetLength(0)); //Width
            //    writer.Write(this.buffer.GetLength(1)); //Height
            //    for (int y = 0; y < this.buffer.GetLength(1); y++)
            //        for (int x = 0; x < this.buffer.GetLength(0); x++)
            //        {
            //            writer.Write(this.buffer[x, y].X);
            //            writer.Write(this.buffer[x, y].Y);
            //            writer.Write(this.buffer[x, y].Z);
            //        }
            //}
        }
    }
}
