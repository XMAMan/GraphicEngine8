using GraphicMinimal;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using BitmapHelper;

namespace ImageCreator
{
    //Stellt die Summe von mehreren ImageBuffer-Objekten dar 
    //Vom Speicher her ist das genau so groß wie ein ImageBuffer-Objekt. Man hat hier lediglich noch die FrameCount-Variable
    [Serializable()]
    public class ImageBufferSum
    {
        public ImageBuffer Buffer { get; private set; }
        public int FrameCount { get; private set; }      //So viele Buffers wurden bei 'Buffer' summiert

        public ImageBufferSum(int width, int height, Vector3D initalColor)
            : this(new ImageBuffer(width, height, initalColor), 0)
        {
        }

        public ImageBufferSum GetCopy()
        {
            ImageBuffer copyBuffer = null;
            int copyCount = 0;

            lock (this.Buffer)
            {
                copyBuffer = new ImageBuffer(this.Buffer);
                copyCount = this.FrameCount;
            }
            return new ImageBufferSum(copyBuffer, copyCount);
        }

        public ImageBufferSum(ImageBuffer buffer, int frameCount)
        {
            this.Buffer = buffer;
            this.FrameCount = frameCount;
        }

        public void AddFrame(ImageBuffer frame)
        {
            lock (this.Buffer)
            {
                this.Buffer.AddFrame(frame);
                this.FrameCount++;
            }
        }

        public ImageBuffer GetScaledImage()
        {
            if (this.FrameCount == 0) return this.Buffer;

            lock (this.Buffer)
            {
                return this.Buffer.GetColorScaledImage(1.0f / this.FrameCount);
            }

        }

        public ImageBufferSum(string file)
        {
            var stream = File.Open(file, FileMode.Open);
            var bformatter = new BinaryFormatter();
            var buffer = (ImageBufferSum)bformatter.Deserialize(stream);
            stream.Close();

            this.Buffer = buffer.Buffer;
            this.FrameCount = buffer.FrameCount;
        }

        public void WriteToFile(string filename)
        {
            Stream stream = File.Open(filename, FileMode.Create);
            BinaryFormatter bformatter = new BinaryFormatter();
            bformatter.Serialize(stream, this);
            stream.Close();
        }
    }
}
