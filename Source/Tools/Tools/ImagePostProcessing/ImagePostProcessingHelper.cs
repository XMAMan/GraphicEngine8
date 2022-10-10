using System;
using GraphicMinimal;
using ImageCreator;
using System.IO;
using BitmapHelper;
using System.Runtime.Serialization.Formatters.Binary;

namespace Tools.Tools
{
    internal class ImagePostProcessingHelper
    {
        public static ImageBuffer ReadImageBufferFromFile(string fileName)
        {
            if (fileName.EndsWith(".hdr"))
            {
                return new RgbeFile(fileName).GetAsImageBuffer();
            }
            else
            {
                return ReadFromFile(fileName);
            }
        }

        public static void SaveImageBuffer(ImageBuffer image, string fileName, TonemappingMethod tonemapping)
        {
            switch(new FileInfo(fileName).Extension)
            {
                case ".raw":
                    image.WriteToFile(fileName);
                    break;

                case ".hdr":
                    new RgbeFile(image).WriteToFile(fileName);
                    break;

                case ".bmp":
                    Tonemapping.GetImage(image, tonemapping).Save(fileName, System.Drawing.Imaging.ImageFormat.Bmp);
                    break;

                case ".png":
                    Tonemapping.GetImage(image, tonemapping).Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                    break;

                case ".jpg":
                    Tonemapping.GetImage(image, tonemapping).Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;

                default: throw new NotSupportedException(new FileInfo(fileName).Extension);
            }
            
            
        }

        

        //Wenn ich eine 0_0_1536_801_InWork.dat-Datei einlese, dann kann das entweder eine ImageBuffer (PixelMode) oder ein ImageBufferSum (FrameMode) sein
        private static ImageBuffer ReadFromFile(string fileName)
        {
            var stream = File.Open(fileName, FileMode.Open);
            var bformatter = new BinaryFormatter();
            var data = bformatter.Deserialize(stream);
            stream.Close();

            if (data is Vector3D[,]) //Raw-Datei wurde über PixelMode erstellt (Oder fertige Ausgabedatei von Pixel/Frame-Mode)
            {
                return new ImageBuffer((Vector3D[,])data);
            }

            if (data is ImageBufferSum)
            {
                return (data as ImageBufferSum).GetScaledImage();
            }

            throw new Exception("Unknown format " + data.GetType());
        }
    }
}
