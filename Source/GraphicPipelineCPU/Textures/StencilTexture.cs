using System.Collections.Generic;
using System.Drawing;

namespace GraphicPipelineCPU.Textures
{
    class StencilTexture : ITexture2D
    {
        private byte[,] stencilValues;

        public StencilTexture(int id, int width, int height)
        {
            this.Id = id;
            this.stencilValues = new byte[width, height];
        }

        public int Id { get; private set; }
        public int Width
        {
            get
            {
                return this.stencilValues.GetLength(0);
            }
        }

        public int Height
        {
            get
            {
                return this.stencilValues.GetLength(1);
            }
        }

        public byte this[int x, int y]
        {
            get
            {
                return this.stencilValues[x, y];
            }
            set
            {
                this.stencilValues[x, y] = value;
            }
        }

        public void SetForEachTexel(byte value)
        {
            for (int x = 0; x < this.Width; x++)
                for (int y = 0; y < this.Height; y++)
                {
                    this.stencilValues[x, y] = value;
                }
        }

        public Bitmap GetAsBitmap()
        {
            Dictionary<int, Color> colorMap = new Dictionary<int, Color>()
            {
                {-6, Color.Plum },
                {-5, Color.Orchid },
                {-4, Color.Turquoise },
                {-3, Color.DeepPink },
                {-2, Color.Yellow },
                {-1, Color.Blue },
                {+0, Color.White },
                {+1, Color.Black },
                {+2, Color.Green },
                {+3, Color.Red },
                {+4, Color.AliceBlue },
                {+5, Color.Azure },
                {+6, Color.Gainsboro },
                {+7, Color.Lavender },
                {+8, Color.MediumPurple },
            };
            var bitmap = new Bitmap(this.Width, this.Height);
            for (int x = 0; x < this.Width; x++)
                for (int y = 0; y < this.Height; y++)
                {
                    bitmap.SetPixel(x, y, colorMap[this.stencilValues[x, y]]);
                    //bitmap.SetPixel(x, y, Color.FromArgb(stencilValues[x,y] * 50, stencilValues[x,y] * 50, stencilValues[x,y] * 50)); //Wenn ich den Stencil-Puffer anzeigen will
                }

            return bitmap;
        }

        public Size GetSize()
        {
            return new Size(this.stencilValues.GetLength(0), this.stencilValues.GetLength(1));
        }
    }
}
