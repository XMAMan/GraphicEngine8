using System;
using GraphicGlobal;
using GraphicMinimal;

namespace RayCameraNamespace
{
    public class SphereCamera : IRayCamera
    {
        public int PixelCountFromScreen { get; private set; }

        public PixelSamplingMode SamplingMode { get; private set; }
        public bool DepthOfFieldIsEnabled => false;

        public Vector3D Position { get; private set; }

        public Vector3D Forward { get; private set; } //Phi=0-Achse

        public Vector3D Up { get; private set; } //Theta=0-Achse
        public bool UseCosAtCamera => false;

        private UniformOverSphereSegmentSampler[,] sampler;
        private CameraConstructorData data;
        private SphericalCoordinateConverter converter;

        public SphereCamera(CameraConstructorData data)
        {
            this.data = data;
            this.PixelCountFromScreen = data.PixelRange.Width * data.PixelRange.Height; //So viele Light-Subpaths erzeugt das Lighttracing
            this.SamplingMode = data.SamplingMode;
            this.Position = data.Camera.Position;
            this.Forward = data.Camera.Forward;
            this.Up = data.Camera.Up;

            //Wenn ich die Kamera mit Forward=(0,0,-1) angebe, dann sieht das erzeugte Bild genau so aus wie wenn ich die Hdr-Environmaplight verwende
            Frame frame = new Frame(Vector3D.Cross(this.Forward, this.Up), this.Forward, this.Up);
            this.converter = new SphericalCoordinateConverter(frame);

            this.sampler = new UniformOverSphereSegmentSampler[data.PixelRange.Width, data.PixelRange.Height];           
            for (int x=0;x< data.PixelRange.Width;x++)
                for (int y=0;y< data.PixelRange.Height;y++)
                {
                    double phiMin = (data.PixelRange.XStart + x) / (double)data.ScreenWidth * 2 * Math.PI;
                    double phiMax = (data.PixelRange.XStart + x + 1) / (double)data.ScreenWidth * 2 * Math.PI;
                    double thetaMin = (data.PixelRange.YStart + y) / (double)data.ScreenHeight * Math.PI;
                    double thetaMax = (data.PixelRange.YStart + y + 1) / (double)data.ScreenHeight * Math.PI;
                    this.sampler[x, y] = new UniformOverSphereSegmentSampler(frame, phiMin, phiMax, thetaMin, thetaMax);
                }
        }

        public Ray CreatePrimaryRay(int x, int y, IRandom rand)
        {
            return new Ray(this.Position, this.sampler[x, y].SampleDirection(rand.NextDouble(), rand.NextDouble()));
        }

        public Ray CreatePrimaryRayWithPixi(int x, int y, Vector2D pix)
        {
            var s = this.sampler[x, y];
            double phi = s.PhiMin + s.PhiDiff * pix.X;
            double theta = s.ThetaMin + s.ThetaDiff * pix.Y;
            Vector3D direction = this.converter.ToWorldDirection(new SphericalCoordinate(phi, theta));
            return new Ray(this.Position, direction);
        }

        public Ray CreateRandomPrimaryRay(IRandom rand)
        {
            return CreatePrimaryRay((int)(rand.NextDouble() * this.data.ScreenWidth), (int)(rand.NextDouble() * this.data.ScreenHeight), rand);
        }

        public Vector2D GetPixelFootprintSize(Vector3D point)
        {
            var coords = this.converter.ToSphereCoordinate(Vector3D.Normalize(point - this.Position));
            int x = (int)(coords.Phi / (2 * Math.PI) * this.data.ScreenWidth);
            int y = (int)(coords.Theta / (Math.PI) * this.data.ScreenHeight);

            double phiMin = (data.PixelRange.XStart + x) / (double)data.ScreenWidth * 2 * Math.PI;
            double phiMax = (data.PixelRange.XStart + x + 1) / (double)data.ScreenWidth * 2 * Math.PI;
            double thetaMin = (data.PixelRange.YStart + y) / (double)data.ScreenHeight * Math.PI;
            double thetaMax = (data.PixelRange.YStart + y + 1) / (double)data.ScreenHeight * Math.PI;
            double pixAreaOnUnitSphere = (phiMax - phiMin) * (thetaMax - thetaMin) * Math.Sin(coords.Theta);
            double footPrintArea = pixAreaOnUnitSphere * (point - this.Position).SquareLength();
            float size = (float)Math.Sqrt(footPrintArea);
            return new Vector2D(size, size);
        }

        public float GetPixelPdfW(int x, int y, Vector3D primaryRayDirection)
        {
            return (float)this.sampler[x, y].PdfW;
        }

        public Vector2D GetPixelPositionFromEyePoint(Vector3D point)
        {
            var coords = this.converter.ToSphereCoordinate(Vector3D.Normalize(point - this.Position));
            return new Vector2D((float)(coords.Phi / (2 * Math.PI) * this.data.ScreenWidth), (float)(coords.Theta / (Math.PI) * this.data.ScreenHeight));
        }

        public bool IsPointInVieldOfFiew(Vector3D point)
        {
            return true;
        }
    }

    class UniformOverSphereSegmentSampler
    {
        public double PdfW { get; private set; }

        public double PhiMin { get; private set; }
        public double PhiMax { get; private set; }
        public double ThetaMin { get; private set; }
        public double ThetaMax { get; private set; }

        public double PhiDiff { get; private set; }
        public double ThetaDiff { get; private set; }

        private Frame frame;        
        private double cosThetaMin;
        private double cosThetaDiff;

        public UniformOverSphereSegmentSampler(Frame frame, double phiMin, double phiMax, double thetaMin, double thetaMax)
        {
            this.frame = frame;
            this.PhiMin = phiMin;
            this.PhiMax = phiMax;
            this.ThetaMin = thetaMin;
            this.ThetaMax = thetaMax;

            this.PhiDiff = phiMax - phiMin;
            this.ThetaDiff = thetaMax - thetaMin;

            this.cosThetaMin = Math.Cos(thetaMin);
            double cosThetaMax = Math.Cos(thetaMax);
            this.cosThetaDiff = cosThetaMax - this.cosThetaMin;
            this.PdfW = 1.0 / ((this.cosThetaMin - cosThetaMax) * this.PhiDiff);
        }

        public Vector3D SampleDirection(double u1, double u2)
        {
            double phi = u1 * this.PhiDiff + this.PhiMin;
            double cosTheta = this.cosThetaMin + u2 * this.cosThetaDiff;
            return this.frame.GetDirectionFromPhiAndCosTheta(cosTheta, phi);
        }
    }
}
