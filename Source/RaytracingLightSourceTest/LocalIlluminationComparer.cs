using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using PointSearch;
using RayTracerGlobal;
using RaytracingBrdf;
using RaytracingBrdf.SampleAndRequest;
using RaytracingLightSourceTest.Fullpathsampling;

namespace RaytracingLightSourceTest
{
    class LocalIlluminationComparer
    {
        private readonly int[] sampleCounts = new int[] { 100, 100, 1, 100000 };//LightsourceSampling, BrdfSampling, Photonmapping, Lighttracing


        public Bitmap CreateImageForAllLightsourceTypes(int imageSize)
        {
            List<Bitmap> images = new List<Bitmap>();
            foreach (LightsourceType method in (LightsourceType[])Enum.GetValues(typeof(LightsourceType)))
            {
                images.Add(CreateImageForSingleLightsourceType(method, imageSize));
            }

            return BitmapHelp.TransformBitmapListToCollum(images);
        }

        public Bitmap CreateImageForSingleLightsourceType(LightsourceType lightsourceType, int imageSize)
        {
            TestSzene data = new TestSzene(lightsourceType, imageSize);

            List<Bitmap> images = new List<Bitmap>();
            foreach (SamplingMethod method in (SamplingMethod[])Enum.GetValues(typeof(SamplingMethod)))
            {
                images.Add(CreateImage(data, sampleCounts[(int)method], method));
            }

            return BitmapHelp.TransformBitmapListToRow(images);
        }

        public Vector3D GetSinglePixel(TestSzene data, int sampleCount, SamplingMethod creationMethod, int x, int y)
        {
            FixedRadiusPointSearch photonmap = creationMethod == SamplingMethod.Photonmapping ? CreatePhotonMap(data) : null;
            Vector3D[,] pixelColors = creationMethod == SamplingMethod.Lighttracing ? GetLightTracingImage(data, sampleCount) : null;

            Vector3D pixelColor = new Vector3D(0, 0, 0);

            //Hier habe ich mal testweise die Varianz berechnet und sie als Abbruchtrigger genommen
            //List<float> values = new List<float>();
            //StringBuilder test = new StringBuilder();

            switch (creationMethod)
            {
                case SamplingMethod.LightsourceSampling:
                    {
                        var ray = data.RayCamera.CreatePrimaryRay(x, y, data.rand);
                        var eyePoint = data.IntersectionFinder.GetIntersectionPoint(ray, 0);
                        if (eyePoint == null) return null;
                        if (eyePoint.IsLocatedOnLightSource) return new Vector3D(1, 1, 1);
                        pixelColor = new Vector3D(0, 0, 0);
                        for (int i = 0; i < sampleCount; i++)
                        {
                            Vector3D color = GetLightsourceSamplingColor(data, eyePoint, ray);
                            pixelColor += color;

                            //float currentColor = pixelColor.X / (i + 1);
                            //values.Add(currentColor);
                            //if (values.Count > 10)
                            //{
                            //    float varianze = values.GetRange(values.Count - 10, 10).Average(c => Math.Abs(currentColor - c));
                            //    if (varianze < 0.0001f)
                            //    {
                            //        sampleCount = i + 1;
                            //        break;
                            //    }
                            //    test.Append(i + ": " + currentColor + "\t" + varianze + System.Environment.NewLine);
                            //}
                        }
                        //string result = test.ToString();
                        pixelColor /= sampleCount;
                    }
                    break;
                case SamplingMethod.BrdfSampling:
                    {
                        var ray = data.RayCamera.CreatePrimaryRay(x, y, data.rand);
                        var eyePoint = data.IntersectionFinder.GetIntersectionPoint(ray, 0);
                        if (eyePoint == null) return null;
                        if (eyePoint.IsLocatedOnLightSource) return new Vector3D(1, 1, 1);
                        pixelColor = new Vector3D(0, 0, 0);
                        for (int i = 0; i < sampleCount; i++) pixelColor += GetBrdfSamplingColor(data, eyePoint, ray);
                        pixelColor /= sampleCount;
                    }
                    break;
                case SamplingMethod.Photonmapping:
                    {
                        var ray = data.RayCamera.CreatePrimaryRay(x, y, data.rand);
                        var eyePoint = data.IntersectionFinder.GetIntersectionPoint(ray, 0);
                        if (eyePoint == null) return null;
                        if (eyePoint.IsLocatedOnLightSource) return new Vector3D(1, 1, 1);
                        pixelColor = GetPhotonmappingColor(data, eyePoint, ray, photonmap);
                    }
                    break;
                case SamplingMethod.Lighttracing:
                    {
                        var ray = data.RayCamera.CreatePrimaryRay(x, y, data.rand);
                        var eyePoint = data.IntersectionFinder.GetIntersectionPoint(ray, 0);
                        if (eyePoint == null) return null;
                        if (eyePoint.IsLocatedOnLightSource) return new Vector3D(1, 1, 1);
                        pixelColor = pixelColors[x, y];
                    }
                    break;
            }

            return pixelColor;
        }

        public Bitmap CreateImage(TestSzene data, int sampleCount, SamplingMethod creationMethod)
        {
            Bitmap image = new Bitmap(data.ImageSize, data.ImageSize);

            FixedRadiusPointSearch photonmap = creationMethod == SamplingMethod.Photonmapping ? CreatePhotonMap(data) : null;
            Vector3D[,] pixelColors = creationMethod == SamplingMethod.Lighttracing ? GetLightTracingImage(data, sampleCount) : null;

            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    var ray = data.RayCamera.CreatePrimaryRay(x, y, data.rand);
                    var eyePoint = data.IntersectionFinder.GetIntersectionPoint(ray, 0);
                    if (eyePoint == null) { image.SetPixel(x, y, Color.AliceBlue); continue; }
                    if (eyePoint.IsLocatedOnLightSource) { image.SetPixel(x, y, Color.White); continue; }

                    Vector3D pixelColor = new Vector3D(0, 0, 0);

                    //Weg 1: LightsourceSampling: GetRandomPointOnLight, GetEmissionForEyePathHitLightSourceDirectly
                    if (creationMethod == SamplingMethod.LightsourceSampling)
                    {
                        for (int i = 0; i < sampleCount; i++) pixelColor += GetLightsourceSamplingColor(data, eyePoint, ray);
                        pixelColor /= sampleCount;
                    }

                    //Weg 2: BrdfSampling: GetEmissionForEyePathHitLightSourceDirectly
                    if (creationMethod == SamplingMethod.BrdfSampling)
                    {
                        for (int i = 0; i < sampleCount; i++) pixelColor += GetBrdfSamplingColor(data, eyePoint, ray);
                        pixelColor /= sampleCount;
                    }

                    //Weg 3: Photonmapping: GetRandomPointForLightPathCreation
                    if (creationMethod == SamplingMethod.Photonmapping)
                    {
                        pixelColor += GetPhotonmappingColor(data, eyePoint, ray, photonmap);
                    }

                    //Weg 4: Lighttracing: GetRandomPointForLightPathCreation
                    if (creationMethod == SamplingMethod.Lighttracing)
                    {
                        pixelColor = pixelColors[x, y];
                    }

                    image.SetPixel(x, y, ConvertVectorToColor(pixelColor));
                }

            return image;
        }

        private Vector3D GetLightsourceSamplingColor(TestSzene data, IntersectionPoint eyePoint, Ray ray)
        {
            var toLightDirection = data.LightSourceSampler.GetRandomPointOnLight(eyePoint.Position, data.rand);
            if (toLightDirection == null) return new Vector3D(0, 0, 0);
            var lightPoint = data.IntersectionFinder.GetIntersectionPoint(new Ray(eyePoint.Position, toLightDirection.DirectionToLightPoint), 0, eyePoint.IntersectedObject);
            if (lightPoint == null) return new Vector3D(0, 0, 0);
 
            var brdfAndPdf = new BrdfPoint(eyePoint, ray.Direction, float.NaN, float.NaN).Evaluate(toLightDirection.DirectionToLightPoint);
            if (brdfAndPdf == null) return new Vector3D(0, 0, 0);

            float distanceSqr = Math.Max(1e-6f, (lightPoint.Position - eyePoint.Position).SquareLength());
            float lightLambda = (-toLightDirection.DirectionToLightPoint) * lightPoint.ShadedNormal;
            float geometryTherm = lightLambda * brdfAndPdf.CosThetaOut / distanceSqr;
            Vector3D pixelColor = Vector3D.Mult(Vector3D.Mult(new Vector3D(1, 1, 1), brdfAndPdf.Brdf), lightPoint.Color) * geometryTherm * data.LightSourceSampler.GetEmissionForEyePathHitLightSourceDirectly(lightPoint, eyePoint.Position, toLightDirection.DirectionToLightPoint) / toLightDirection.PdfA;
            return pixelColor;
        }

        private Vector3D GetBrdfSamplingColor(TestSzene data, IntersectionPoint eyePoint, Ray ray)
        {
            var brdf = new BrdfPoint(eyePoint, ray.Direction, float.NaN, float.NaN).SampleDirection(new BrdfSampler(), data.rand);
            if (brdf == null) return new Vector3D(0, 0, 0);
            var lightPoint = data.IntersectionFinder.GetIntersectionPoint(brdf.Ray, 0, eyePoint.IntersectedObject);
            if (lightPoint == null) return new Vector3D(0, 0, 0);
            Vector3D pixelColor = Vector3D.Mult(Vector3D.Mult(new Vector3D(1, 1, 1), brdf.Brdf), lightPoint.Color) * data.LightSourceSampler.GetEmissionForEyePathHitLightSourceDirectly(lightPoint, eyePoint.Position, brdf.Ray.Direction);
            return pixelColor;
        }

        private Vector3D GetPhotonmappingColor(TestSzene data, IntersectionPoint eyePoint, Ray ray, FixedRadiusPointSearch photonmap)
        {
            var photons = photonmap.FixedRadiusSearch(eyePoint.Position, data.SearchRadius).Cast<Photon>().ToList();

            float kernelFunction = 1.0f / (data.SearchRadius * data.SearchRadius * (float)Math.PI);

            Vector3D colorSum = new Vector3D(0, 0, 0);
            foreach (var p in photons)
            {
                var brdf = new BrdfPoint(eyePoint, ray.Direction, float.NaN, float.NaN).Evaluate(-p.DirectionToPhoton);
                if (brdf == null) continue;
                colorSum += Vector3D.Mult(Vector3D.Mult(new Vector3D(1, 1, 1), p.Color), brdf.Brdf) * kernelFunction / data.PhotonCount;
            }

            return colorSum;
        }

        private FixedRadiusPointSearch CreatePhotonMap(TestSzene data)
        {
            List<Photon> photons = new List<Photon>();
            for (int i = 0; i < data.PhotonCount; i++)
            {
                var lightPoint = data.LightSourceSampler.GetRandomPointForLightPathCreation(data.rand);
                var point = data.IntersectionFinder.GetIntersectionPoint(new Ray(lightPoint.PointOnLight.Position, lightPoint.Direction), 0, lightPoint.PointOnLight.IntersectedObject);
                if (point == null) continue;
                photons.Add(new Photon()
                {
                    Color = lightPoint.PointOnLight.Color * lightPoint.EmissionPerArea * Math.Max(0, lightPoint.PointOnLight.ShadedNormal * lightPoint.Direction) / (lightPoint.PdfA * lightPoint.PdfW),
                    DirectionToPhoton = lightPoint.Direction,
                    Position = point.Position
                });
            }
            FixedRadiusPointSearch photonmap = new FixedRadiusPointSearch(photons.Cast<IPoint>().ToList());
            return photonmap;
        }

        class Photon : IPoint
        {
            public Vector3D Color;
            public Vector3D DirectionToPhoton;
            public Vector3D Position;

            public float this[int dimension] { get { return this.Position[dimension]; } }
        }

        private Vector3D[,] GetLightTracingImage(TestSzene data, int sampleCount)
        {
            Vector3D[,] pixelColors = new Vector3D[data.ImageSize, data.ImageSize];
            for (int x = 0; x < data.ImageSize; x++)
                for (int y = 0; y < data.ImageSize; y++)
                    pixelColors[x, y] = new Vector3D(0, 0, 0);

            for (int i = 0; i < sampleCount; i++)
            {
                var lightPoint = data.LightSourceSampler.GetRandomPointForLightPathCreation(data.rand);
                var point = data.IntersectionFinder.GetIntersectionPoint(new Ray(lightPoint.PointOnLight.Position, lightPoint.Direction), 0, lightPoint.PointOnLight.IntersectedObject);
                if (point == null) continue;
                var pixelPosition = data.RayCamera.GetPixelPositionFromEyePoint(point.Position);
                if (pixelPosition == null) continue;
                Vector3D lightPointToCamera = data.RayCamera.Position - point.Position;
                float lightPointToCameraDistance = lightPointToCamera.Length();
                Vector3D lightPointToCameraDirection = lightPointToCamera / lightPointToCameraDistance;
                var lightBrdf = new BrdfPoint(point, lightPoint.Direction, float.NaN, float.NaN).Evaluate(lightPointToCameraDirection);//LightTracing
                if (lightBrdf == null) continue;
                float cameraCos = data.RayCamera.Forward * (-lightPointToCameraDirection);
                float geomertryFaktor = (cameraCos * lightBrdf.CosThetaOut) / (lightPointToCameraDistance * lightPointToCameraDistance);
                Vector3D color = lightPoint.PointOnLight.Color * lightPoint.EmissionPerArea * Math.Max(0, lightPoint.PointOnLight.ShadedNormal * lightPoint.Direction) / (lightPoint.PdfA * lightPoint.PdfW);
                float cameraPdfW = data.RayCamera.GetPixelPdfW((int)pixelPosition.X, (int)pixelPosition.Y, - lightPointToCameraDirection);
                color = Vector3D.Mult(color, lightBrdf.Brdf) * geomertryFaktor * cameraPdfW / sampleCount;
                int x = (int)pixelPosition.X;
                int y = (int)pixelPosition.Y;
                pixelColors[x, y] += color;
            }

            return pixelColors;
        }

        private static Color ConvertVectorToColor(Vector3D col)
        {
            if (col == null) return Color.Black;
            if (float.IsNaN(col.X) || float.IsNaN(col.Y) || float.IsNaN(col.Z)) return Color.Black;

            float x = col.X;
            float y = col.Y;
            float z = col.Z;

            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (z < 0) z = 0;

            if (x > 1) x = 1;
            if (y > 1) y = 1;
            if (z > 1) z = 1;

            return Color.FromArgb((int)(x * 255), (int)(y * 255), (int)(z * 255));
        }
    }
}
