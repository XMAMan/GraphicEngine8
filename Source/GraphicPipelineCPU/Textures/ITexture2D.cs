using System.Drawing;

namespace GraphicPipelineCPU.Textures
{
    interface ITexture2D
    {
        int Id { get; }
        int Width { get; }
        int Height { get; }
        Bitmap GetAsBitmap();
        Size GetSize();
    }
}
