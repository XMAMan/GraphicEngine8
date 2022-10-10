using GraphicMinimal;
using System.IO;
using System.Runtime.InteropServices;

namespace BitmapHelper
{
    //Hinweis: Die OpenEXRDLL.dll ist x64. Eigentlich müsste ich auch dieses Projekt als x64 anstatt AnyCPU übersetzen aber dann wären auch all .NET-Dlls, welche diese Dll hier verwenden, von der Änderung betroffen
    public static class OpenEXRLoader
    {
        [DllImport("OpenEXRDLL.dll", EntryPoint = "GetImageSize", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetImageSize([MarshalAs(UnmanagedType.LPStr)] string filename, int[] data);

        [DllImport("OpenEXRDLL.dll", EntryPoint = "LoadOpenHDRImage", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern void LoadOpenHDRImage([MarshalAs(UnmanagedType.LPStr)] string filename, float[] data);

        public static ImageBuffer LoadHdrImage(string fileName)
        {
            if (File.Exists(fileName) == false) throw new FileNotFoundException(fileName);
            int[] size = new int[2];
            GetImageSize(fileName, size);
            float[] rgbData = new float[size[0] * size[1] * 3];
            LoadOpenHDRImage(fileName, rgbData);
            ImageBuffer data = new ImageBuffer(size[0], size[1]);
            int c = 0;
            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                {
                    data[x, y] = new Vector3D(rgbData[c++], rgbData[c++], rgbData[c++]);
                }
            return data;
        }        
    }
}
