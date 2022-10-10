using BitmapHelper;
using GraphicMinimal;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.SlopSpace.SlopeDistribution;
using System;
using System.Drawing;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.SlopSpace
{
    //Ich bräuchte ein eigenes Testprojekt für die RaytracingBrdf-Dll um das hier ausführen zu können

    //Hier möchte ich den Quellcode aus "Forschungen\Microfaset Brdfs_2018_2019\Importance Sampling Microfacet-Based BSDFs with the Distribution of Visible Normals Supplemental Material 1_2 2014" testen
    class SlopeSpaceVisualisizer
    {
        public static void CreateTestImage()
        {
            new SlopeSpaceVisualisizer(new SlopeSpaceSmithMicrofacet(new BeckmannSlopeDistribution(), 2, 1), 4.0f).GetImage(300, 300).Save("MicrofacetBeckman.bmp");
            new SlopeSpaceVisualisizer(new SlopeSpaceSmithMicrofacet(new GgxSlopeDistribution(), 2, 1), 4.0f).GetImage(300, 300).Save("MicrofacetGGX.bmp");
        }

        private readonly ISlopeSpaceMicrofacet slopeSpaceMicrofacet;
        private readonly float slopeMax;

        //die X- und Y-Werte gehen von -slopeMax bis +slopeMax
        public SlopeSpaceVisualisizer(ISlopeSpaceMicrofacet slopeSpaceMicrofacet, float slopeMax)
        {
            this.slopeSpaceMicrofacet = slopeSpaceMicrofacet;
            this.slopeMax = slopeMax;
        }

        public Bitmap GetImage(int width, int height)
        {
            float phi = 0.765397f, theta = 1.56f;
            Bitmap image1 = GetImageFromSlopeSampling(width, height, phi, theta, 10000000, this.slopeSpaceMicrofacet.SampleMicronormalAnalytical);
            Bitmap image2 = GetImageFromSlopeSampling(width, height, phi, theta, 10000000, this.slopeSpaceMicrofacet.SampleMicronormalFromTableData);
            Bitmap image3 = GetImageFromVisibleSlopeDistribution(width, height, phi, theta);

            Bitmap image = new Bitmap(width * 3 + 3, height);
            Graphics grx = Graphics.FromImage(image);
            grx.DrawImage(image1, new Point(0, 0));
            grx.DrawString("AnalyticalSamling", new Font("Arial", 10), Brushes.Yellow, new PointF(0, height - 15));
            grx.DrawImage(image2, new Point(width + 1, 0));
            grx.DrawString("TableDataSampling", new Font("Arial", 10), Brushes.Yellow, new PointF(width + 1, height - 15));
            grx.DrawImage(image3, new Point(width * 2 + 2, 0));
            grx.DrawString("SlopeDistribution", new Font("Arial", 10), Brushes.Yellow, new PointF(width * 2 + 2, height - 15));
            grx.Dispose();

            return image;
        }

        private Bitmap GetImageFromSlopeSampling(int width, int height, float phi, float theta, int samplingCount, Func<Vector3D, float, float, Vector3D> sampleFunction)
        {
            Bitmap image = new Bitmap(width, height);

            var histogram = GetHistogram(width, height, samplingCount, phi, theta, sampleFunction);
            float maxP22 = GetMaxValueFrom2DFloatArray(histogram);
            //float maxP22 = histogram[width / 2, height / 2];

            //maxP22 = 0.08267737f;

            //float range = 2 * this.slopeMax;
            //float pixelArea = (range * range) / (width * height); //Das entspricht dA (Differential Area)
            float sum = 0;

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    //float xm = x * 2 * this.slopeMax / (float)width - this.slopeMax;
                    //float ym = y * 2 * this.slopeMax / (float)height - this.slopeMax;
                    float p22 = histogram[x, y];

                    sum += p22;// *pixelArea;

                    p22 = (maxP22 - p22) * (1 / maxP22);
                    Color color = PixelHelper.ConvertFloatToColor(p22);

                    image.SetPixel(x, y, color);
                }

            return image;
        }

        private Bitmap GetImageFromVisibleSlopeDistribution(int width, int height, float phi, float theta)
        {
            Bitmap image = new Bitmap(width, height);

            float maxP22 = float.MinValue;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    float xm = x * 2 * this.slopeMax / (float)width - this.slopeMax;
                    float ym = y * 2 * this.slopeMax / (float)height - this.slopeMax;
                    float p22 = this.slopeSpaceMicrofacet.GetVisibleSlopeDistribution(phi, theta, xm, ym);
                    if (p22 > maxP22) maxP22 = p22;
                }

            //maxP22 = 0.08267737f;

            //float range = 2 * this.slopeMax;
            //float pixelArea = (range * range) / (width * height); //Das entspricht dA (Differential Area)
            float sum = 0;

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    float xm = x * 2 * this.slopeMax / (float)width - this.slopeMax;
                    float ym = y * 2 * this.slopeMax / (float)height - this.slopeMax;
                    float p22 = this.slopeSpaceMicrofacet.GetVisibleSlopeDistribution(phi, theta, xm, ym);

                    sum += p22;// *pixelArea;

                    p22 = (maxP22 - p22) * (1 / maxP22);
                    Color color = PixelHelper.ConvertFloatToColor(p22);

                    image.SetPixel(x, y, color);
                }

            return image;
        }

        private float[,] GetHistogram(int width, int height, int sampleCount, float phi, float theta, Func<Vector3D, float, float, Vector3D> sampleFunction)
        {
            float[,] data = new float[width, height];

            Vector3D wi = new Vector3D((float)(Math.Cos(phi) * Math.Sin(theta)), (float)(Math.Sin(phi) * Math.Sin(theta)), (float)Math.Cos(theta));

            Vector3D minSlope = sampleFunction(wi, 0.02f, 0.02f);
            Vector3D maxSlope = sampleFunction(wi, 1, 1);

            float rangeX = Math.Abs(maxSlope.X - minSlope.X);
            float rangeY = Math.Abs(maxSlope.Y - minSlope.Y);
            float pixelArea = (rangeX * rangeY) / (width * height); //Das entspricht dA (Differential Area)


            Random rand = new Random(0);
            for (int i = 0; i < sampleCount; i++)
            {
                Vector3D slope = sampleFunction(wi, (float)rand.NextDouble(), (float)rand.NextDouble());
                int x = (int)((slope.X - (-this.slopeMax)) / (this.slopeMax * 2) * width);
                int y = (int)((slope.Y - (-this.slopeMax)) / (this.slopeMax * 2) * height);
                if (x >= 0 && x < width && y >= 0 && y < height) data[x, y] += 1.0f / (float)sampleCount / pixelArea;
            }
            return data;
        }

        private float GetMaxValueFrom2DFloatArray(float[,] data)
        {
            float max = float.MinValue;
            for (int x = 0; x < data.GetLength(0); x++)
                for (int y = 0; y < data.GetLength(1); y++)
                {
                    if (data[x, y] > max) max = data[x, y];
                }
            return max;
        }
    }
}
