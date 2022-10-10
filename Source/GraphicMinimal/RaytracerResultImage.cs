using System.Drawing;

namespace GraphicMinimal
{
    public class RaytracerResultImage
    {
        public Bitmap Bitmap { get; private set; }
        public string RenderTime { get; private set; }
        public ImageBuffer RawImage { get; private set; }

        public RaytracerResultImage(Bitmap bitmap, string renderTime, ImageBuffer rawImage)
        {
            this.Bitmap = bitmap;
            this.RenderTime = renderTime;
            this.RawImage = rawImage;
        }
    }
}
