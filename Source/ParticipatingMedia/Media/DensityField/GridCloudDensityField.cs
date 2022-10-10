using GraphicGlobal;
using GraphicMinimal;
using System.Drawing;
using TextureHelper;

namespace ParticipatingMedia.Media.DensityField
{
    class GridCloudDensityField : IDensityField
    {
        public float MaxDensity { get; private set; } = 1;

        private DescriptionForGridCloudMedia desc;
        private EbertNoiseGenerator3D noise;        
        private float turbulenceFactor;

        public GridCloudDensityField(BoundingBox worldSpaceBox, DescriptionForGridCloudMedia desc)
        {
            this.desc = desc;
            this.noise = new EbertNoiseGenerator3D(worldSpaceBox, new Rand(0), 1);
            this.turbulenceFactor = worldSpaceBox.MaxEdge - desc.LegoGrid.Box.MaxEdge;

            //Testausgabe(new Size(150, 300), worldSpaceBox).Save("Testausgabe.bmp");
        }

        private Bitmap Testausgabe(Size size, BoundingBox worldSpaceBox)
        {
            var b = worldSpaceBox;
            Bitmap image = new Bitmap(size.Width, size.Height);
            for (int x=0;x<size.Width;x++)
                for (int y=0;y<size.Height;y++)
                {
                    float fx = x / (float)size.Width;
                    float fy = y / (float)size.Height;

                    Vector3D point = new Vector3D(b.Min.X + b.XSize * fx, b.Min.Y + b.YSize * fy, b.Min.Z + b.ZSize * 0.5f);

                    bool isInside = GetDensity(point) > 0;

                    if (isInside) image.SetPixel(x, size.Height - y - 1, Color.Red);
                }

            return image;
        }

        public float GetDensity(Vector3D point)
        {
            point += this.noise.GetNoiseVector(point) * this.turbulenceFactor;
            return this.desc.LegoGrid.IsPointInside(point) ? 1 : 0;
        }
    }
}
