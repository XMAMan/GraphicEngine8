using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;
using System.Drawing;
using SlimDX;
using DXGI = SlimDX.DXGI;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using BitmapHelper;

namespace GraphicPipelineDirect3D11
{
    static class TextureHelper
    {
        //Quelle: http://www.rkoenig.eu/index.php?option=com_content&view=article&id=65:bitmap-from-texture-d3d11&catid=16:blog&Itemid=10
        public static Texture2D TextureFromBitmap(Device device, System.Drawing.Bitmap bitmap)
        {
            Texture2D result = null;

            //Lock bitmap so it can be accessed for texture loading
            System.Drawing.Imaging.BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            DataStream dataStream = new DataStream(
                bitmapData.Scan0,
                bitmapData.Stride * bitmapData.Height,
                true, false);
            DataRectangle dataRectangle = new DataRectangle(bitmapData.Stride, dataStream);

            try
            {
                //Load the texture
                result = new Texture2D(device, new Texture2DDescription()
                {
                    BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = DXGI.Format.B8G8R8A8_UNorm,
                    OptionFlags = ResourceOptionFlags.None,
                    MipLevels = 1,
                    Usage = ResourceUsage.Default, //Default = GPU RW, Immutable = GPU RO, DYNAMIC (CPU WO, GPU RO), STAGING (CPU RW, GPU RW)
                    Width = bitmap.Width,
                    Height = bitmap.Height,
                    ArraySize = 1,
                    SampleDescription = new DXGI.SampleDescription(1, 0)
                }, dataRectangle);
            }
            finally
            {
                //Free bitmap-access resources
                dataStream.Dispose();
                bitmap.UnlockBits(bitmapData);
            }

            return result;
        }

        public static Bitmap GetTextureData(Device device, Resource resource, int width, int height, bool isDepthTexture)
        {
            //Erzeuge eine leere (Bei allen RGBA-Werten steht 0) Staging-Textur, von der aus die CPU lesen kann. Die Grafikkarte darf auch drin schreiben
            Texture2D texi2 = new Texture2D(device, new Texture2DDescription()
            {
                //BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Read,
                Format = isDepthTexture ? SlimDX.DXGI.Format.R24_UNorm_X8_Typeless : DXGI.Format.B8G8R8A8_UNorm,
                //OptionFlags = DX11.ResourceOptionFlags.None,
                MipLevels = 1,
                Usage = ResourceUsage.Staging, //Default = GPU RW, Immutable = GPU RO, DYNAMIC (CPU WO, GPU RO), STAGING (CPU RW, GPU RW)
                Width = width,
                Height = height,
                ArraySize = 1,
                SampleDescription = new DXGI.SampleDescription(1, 0)
            });

            device.ImmediateContext.CopyResource(resource, texi2);//Kopiere im Grafikkartenspeicher von Quelltextur in Staging-Textur

            //Wandle Textur in Bytearray um
            DataBox dataBox = device.ImmediateContext.MapSubresource(texi2, 0, MapMode.Read, MapFlags.None);
            byte[] data = new byte[dataBox.SlicePitch];
            dataBox.Data.Read(data, 0, data.Length);
            device.ImmediateContext.UnmapSubresource(texi2, 0);

            //Lösche Staging-Textur auf Grafikkarte
            texi2.Dispose();

            if (isDepthTexture)
            {
                return BitmapHelp.ConvertByteArrayFromDepthTextureToBitmap(data, width, height, false);
            }
            else
            {
                return ConvertByteArrayFromColorTextureToBitmap(data, width, height, dataBox);
            }

            
            //Wandle Bytearray in Bitmap um
            /*System.Drawing.Bitmap result = new System.Drawing.Bitmap(width, height);
            BitmapData resultData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            for (int y = 0; y < height; y++)
                Marshal.Copy(data, y * dataBox.RowPitch, resultData.Scan0 + y * dataBox.RowPitch, dataBox.RowPitch);
            result.UnlockBits(resultData);
            return result;
            
            //UNORM - "Unsigned normalized integer; which is interpreted in a resource as an unsigned integer, and is interpreted in a shader as an unsigned normalized floating-point value in the range [0, 1]
            //UNORM -> https://msdn.microsoft.com/en-us/library/windows/desktop/dd607323%28v=vs.85%29.aspx
            if (isDepthTexture) //Konvertiere 24 Bit Depth (float), 8 Bit Stencil (Unsigned Integer) nach Graufwertbild
            {
                float min = float.MaxValue, max = float.MinValue;
                for (int x=0;x<result.Width;x++)
                    for (int y = 0; y < result.Height; y++)
                    {
                        Color c = result.GetPixel(x, y); //r = Stencil, g,b,a = Depth
                        float depth = GrafikHelper.ConvertUnsignedNormalizedIntegerToFloat(((int)c.G) << 16 | ((int)c.R) << 8 | ((int)c.A), 24);
                        if (depth < min) min = depth;
                        if (depth > max) max = depth;
                    }

                for (int x=0;x<result.Width;x++)
                    for (int y = 0; y < result.Height; y++)
                    {
                        if (max - min == 0)
                        {
                            result.SetPixel(x, y, Color.White);
                        }
                        else
                        {
                            Color c = result.GetPixel(x, y); //r = Stencil, g,b,a = Depth
                            float depth = GrafikHelper.ConvertUnsignedNormalizedIntegerToFloat(((int)c.G) << 16 | ((int)c.B) << 8 | ((int)c.A), 24);
                            int d = (int)((depth - min) / (max - min) * 255);
                            result.SetPixel(x, y, Color.FromArgb(d, d, d));
                        }
                    }
            }

            return result;*/
        }

        private static Bitmap ConvertByteArrayFromColorTextureToBitmap(byte[] data, int width, int height, DataBox dataBox)
        {
            System.Drawing.Bitmap result = new System.Drawing.Bitmap(width, height);
            BitmapData resultData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            for (int y = 0; y < height; y++)
                Marshal.Copy(data, y * dataBox.RowPitch, resultData.Scan0 + y * dataBox.RowPitch, dataBox.RowPitch);
            result.UnlockBits(resultData);
            return ConvertDirectXBitmapToNormalBitmap(result);
        }

        //Wenn ich die Textur, welche vom DirectX-Speicher kam direkt mit Save speichere, sieht sie falsch aus
        public static Bitmap ConvertDirectXBitmapToNormalBitmap(System.Drawing.Bitmap image)
        {
            System.Drawing.Bitmap image2 = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image2.Width; x++)
                for (int y = 0; y < image2.Height; y++)
                {
                    image2.SetPixel(x, y, image.GetPixel(x, y));
                }
            return image2;
        }
    }
}
