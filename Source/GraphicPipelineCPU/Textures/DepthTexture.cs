using BitmapHelper;
using GraphicMinimal;
using System.Drawing;

namespace GraphicPipelineCPU.Textures
{
    class DepthTexture : ITexture2D
    {
        private float[,] depthValues;

        public DepthTexture(int id, int width, int height)
        {
            this.Id = id;
            this.depthValues = new float[width, height];
        }

        public int Id { get; private set; }

        public int Width
        {
            get
            {
                return this.depthValues.GetLength(0);
            }
        }

        public int Height
        {
            get
            {
                return this.depthValues.GetLength(1);
            }
        }

        public float this[int x, int y]
        {
            get
            {
                return this.depthValues[x, y];
            }
            set
            {
                this.depthValues[x, y] = value;
            }
        }

        public void SetForEachTexel(float value)
        {
            for (int x = 0; x < this.Width; x++)
                for (int y = 0; y < this.Height; y++)
                {
                    this.depthValues[x, y] = value;
                }
        }

        public float ReadDepthValue(Vector2D texPosition)
        {
            int x = (int)(this.depthValues.GetLength(0) * texPosition.X);
            int y = (int)(this.depthValues.GetLength(1) * texPosition.Y);

            if (x < 0) x = 0;
            if (x >= this.depthValues.GetLength(0)) x = this.depthValues.GetLength(0) - 1;
            if (y < 0) y = 0;
            if (y >= this.depthValues.GetLength(1)) y = this.depthValues.GetLength(1) - 1;

            return this.depthValues[x, y];
        }

        public Bitmap GetAsBitmap()
        {
            return BitmapHelp.ConvertDepthValuesToBitmap(this.depthValues, false);
        }

        public Size GetSize()
        {
            return new Size(this.depthValues.GetLength(0), this.depthValues.GetLength(1));
        }
    }
}
