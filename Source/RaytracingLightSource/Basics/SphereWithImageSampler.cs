using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;
using RaytracingRandom;
using System;

namespace RaytracingLightSource
{
    //Auf die Einheitskugel wird ein Bild gemappt, dessen Helligkeit gibt die PdfA an, mit der Punkte auf der Kugel gesampelt werden
    //Anwendungsfall 1: Lightracing: Erzeuge zufälligen Punkt auf Kugel und gib die zugehörige PdfA und den Farbwert aus der Imagemap zurück
    //Anwendungsfall 2: DirectLighting: Verhält sich so wie Lightracing. Es gibt kein spezielles Importancesampling dafür
    //Anwendungsfall 3: Pathtracing: Gebe für ein Richtungsvektor die PdfA und den Farbwert zurück
    class SphereWithImageSampler
    {
        public class PointOnSphere
        {
            public Vector3D Position; //Ist auch gleichzeitig der Richtungsvektor, da das hier die Einheitskugel ist
            public double PdfA;
            public Vector3D Color;
        }

        private readonly Vector3D[,] image;
        private readonly Distribution2D sampler;
        private readonly SphericalCoordinateConverter sphericalCoordinateConverter;

        private readonly double invserTexelAreaWithoutThetaSin; //1/Flächeninhalt von ein Texel (ohne Theta-Sin) um die Pmf aus der Distribution2D in eine PdfA umzurechnen

        public SphereWithImageSampler(ImageBuffer data, float rotate, Vector3D cameraUpVector)
        {
            this.image = GetImage(data, rotate);
            this.sampler = GetImageSampler(this.image);

            //Um eine Pmf in eine PdfA umzurechnen muss ich die Texel-Pmf durch die Texelfläche dividieren
            double dPhi = (2 * Math.PI) / this.sampler.Width; //Einheitskugel hat beim Kreis auf Höhe Theta=90 Grad ein Radius von 2PI
            double dTheta = Math.PI / this.sampler.Height; //Die Linie, wo Theta langläuft ist PI lang
            this.invserTexelAreaWithoutThetaSin = 1 / (dPhi * dTheta); //Hier fehlt noch der SinTheta-Term. Er wird bei der PdfA-Formel noch mit hinzugefügt

            this.sphericalCoordinateConverter = new SphericalCoordinateConverter(new Frame(cameraUpVector));
        }

        //rotate = 0..1 = So viel wird das Bild X-Mäßig geshifted
        private Vector3D[,] GetImage(ImageBuffer data, float rotate)
        {
            int iRot = (int)(rotate * data.Width);

            Vector3D[,] image = new Vector3D[data.Width, data.Height];
            for (int x=0;x<data.Width; x++)
                for (int y=0;y<data.Height; y++)
                {
                    int xRot = x + iRot;
                    if (xRot >= data.Width) xRot -= data.Width;
                    image[xRot, y] = data[x, y];
                }
            return image;
        }

        private Distribution2D GetImageSampler(Vector3D[,] map)
        {
            double[,] data = new double[map.GetLength(0), map.GetLength(1)];

            double dPhi = (2 * Math.PI) / map.GetLength(0); //Einheitskugel hat beim Kreis auf Höhe Theta=90 Grad ein Radius von 2PI
            double dTheta = Math.PI / map.GetLength(1); //Die Linie, wo Theta langläuft ist PI lang

            for (int y = 0; y < map.GetLength(1); y++)
            {
                double sinTheta = Math.Sin((y + 0.5) / map.GetLength(1) * Math.PI);
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    //Nimm den Flächeninhalt vom Punkt (x,y) und wichte ihn mit der Luminanz
                    //Da dPhi*dTheta eine Konstante ist, könnte man das hier auch weglassen aber ich lasse es drin da ich das so logischer/leichter lesbar halte
                    double texelArea = dPhi * dTheta * sinTheta; 
                    data[x, y] = PixelHelper.ColorToGray(map[x, y]) * texelArea;
                }
            }

            return new Distribution2D(data);
        }

        public PointOnSphere SamplePointOnSphere(double u1, double u2)
        {
            var uv = this.sampler.SampleXYIndex(u1, u2);
            //double pdfA = 1 / (2 * Math.PI * Math.PI) * uv.PdfA / SinTheta(uv.Y); //SmallUPBP (hier fehlt Image.Width/Height)

            double u = uv.X / this.sampler.Width;
            double v = uv.Y / this.sampler.Height;

            Vector3D direction = LatLong2Dir(u, v);
            Vector3D color = LookupRadiance(u, v);
            //double pdfA = invserTexelAreaWithoutThetaSin * uv.PdfA / SinTheta(v); //uv.PdfA ist eine Pmf.
            
            return new PointOnSphere()
            {
                Position = direction,
                //PdfA = pdfA, //Wenn ich das so mache, kann es wegen der Sprungstellen in der Distibtuion2D-Funktion zu unterschliedlichen Werten kommen zwischen pdfA und GetPdfA da wegen Rundungsfehlern LatLong2Dir() != Dir2LatLong()
                PdfA = GetPdfAFromPointOnSphere(direction),
                Color = color
            };
        }

        // Returns direction on unit sphere such that its longitude equals 2*PI*u and its latitude equals PI*v.
        private Vector3D LatLong2Dir(double u, double v)
        {
            double phi = u * 2 * Math.PI;
            double theta = v * Math.PI;

            return this.sphericalCoordinateConverter.ToWorldDirection(new SphericalCoordinate(phi, theta));

            //double sinTheta = Math.Sin(theta);
            //return new Vector3D((float)(-sinTheta * Math.Cos(phi)), (float)(sinTheta * Math.Sin(phi)), (float)(Math.Cos(theta))); //SmallUPBP
        }

        // Returns vector [u,v] such that the longitude of the given direction equals 2*PI*u and its latitude equals PI*v. The direction must be non-zero and normalized.
        private Vector2D Dir2LatLong(Vector3D direction)
        {
            //double phi = (direction.X != 0 || direction.Y != 0) ? Math.Atan2(direction.Y, direction.X) : 0; //SmallUPBP
            //phi = Math.PI - phi;
            //double theta = Math.Acos(direction.Z);
            
            //My Way
            var coords = this.sphericalCoordinateConverter.ToSphereCoordinate(direction);
            double phi = coords.Phi;
            double theta = coords.Theta;

            float u = PixelHelper.Clamp((float)(phi * 0.5f / Math.PI), 0, 1);
            float v = PixelHelper.Clamp((float)(theta / Math.PI), 0, 1);
            return new Vector2D(u, v);
        }

        // Returns sine of latitude for a midpoint of a pixel in a map of the given height corresponding to v. Never returns zero.
        private double SinTheta(double v)
        {
            if (v < 1)
                return Math.Sin(Math.PI * ((int)(v * this.sampler.Height) + 0.5f) / (float)this.sampler.Height);
            else
                return Math.Sin(Math.PI * ((this.sampler.Height - 1) + 0.5f) / (float)this.sampler.Height);
        }

        //Beim aufnehmen des Bildes wurde impliziet für jeden Texel der Emission-Wert durch die Texelfläche dividiert 
        //was somit die Radiance ergibt. D.h. diese Funktion hier gibt Emission/(TexelArea) zurück
        private Vector3D LookupRadiance(double u, double v)
        {
            float xf = (float)u * this.sampler.Width; 
            float yf = (float)v * this.sampler.Height;

            int xi1 = Math.Min((int)xf, this.sampler.Width - 1);
            int yi1 = Math.Min((int)yf, this.sampler.Height - 1);

            int xi2 = xi1 == this.sampler.Width - 1 ? xi1 : xi1 + 1;
            int yi2 = yi1 == this.sampler.Height - 1 ? yi1 : yi1 + 1;

            float tx = xf - (float)xi1;
            float ty = yf - (float)yi1;

            return (1 - ty) * ((1 - tx) * this.image[xi1, yi1] + tx * this.image[xi2, yi1])
                + ty * ((1 - tx) * this.image[xi1, yi2] + tx * this.image[xi2, yi2]);
        }

        public double GetPdfAFromPointOnSphere(Vector3D position)
        {
            Vector2D uv = Dir2LatLong(position);
            //double pdfA = 1 / (2 * Math.PI * Math.PI) * this.sampler.Pdf(uv.X * this.sampler.Width, uv.Y * this.sampler.Height) / SinTheta(uv.Y); //SmallUPBP (Hier fehlt Image.Width/Height)
            
            double pdfA = invserTexelAreaWithoutThetaSin * this.sampler.Pdf(uv.X * this.sampler.Width, uv.Y * this.sampler.Height) / SinTheta(uv.Y);
            return pdfA;
        }

        public Vector3D GetColorFromPointOnSphere(Vector3D position)
        {
            Vector2D uv = Dir2LatLong(position);
            return LookupRadiance(uv.X, uv.Y);
        }
    }
}
