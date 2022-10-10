using GraphicMinimal;

namespace ImageCreator
{
    //Bei großen Bildern dauert ImageBufferSum.GetScaledImage() lange. Hiermit cache ich den Aufruf, wenn sich sowiso nichts an der FrameCount geändert hat
    class CachedProgressImage
    {
        private int lastFrameCount = -1;
        private ImageBufferSum bufferSum;
        private ImageBuffer cachedImage = null;
        public CachedProgressImage(ImageBufferSum bufferSum)
        {
            this.bufferSum = bufferSum;
        }

        public ImageBuffer GetProgressImage()
        {
            if (this.bufferSum.FrameCount != this.lastFrameCount)
            {
                this.lastFrameCount = this.bufferSum.FrameCount;
                this.cachedImage = this.bufferSum.GetScaledImage();
            }

            return this.cachedImage;
        }
    }
}
