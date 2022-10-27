using System;
using System.Linq;
using IntersectionTests;
using GraphicMinimal;
using RaytracingBrdf;
using RaytracingLightSource;
using GraphicGlobal;
using RayCameraNamespace;
using SubpathGenerator.SubPathSampler;
using RaytracingBrdf.SampleAndRequest;
using SubpathGenerator.SubPathPointsSampler.Media;

namespace SubpathGenerator
{
    public enum PathSamplingType
    {
        None,
        NoMedia,
        ParticipatingMediaShortRayWithDistanceSampling,     //Erzeuge die Segmente nur bis zum Distanz-Samplepunkt
        ParticipatingMediaLongRayOneSegmentWithDistanceSampling,      //Suche über den Distanz-Samplepunkt hinaus noch den nächsten Surface-Punkt und füge das Segment noch mit dran (Line-Endpoint liegt somit in der Mitte von der Linie)
        ParticipatingMediaLongRayManySegmentsWithDistanceSampling,  //Suche per Distanzsampling Partikel und füge von dort über alle MediaAir-Borderpunkte hinausgehend alle restlichen Segmente noch an
        ParticipatingMediaWithoutDistanceSampling,
    }

    public class SubPathSettings
    {
        public PathSamplingType EyePathType;
        public PathSamplingType LightPathType;
        public int MaxEyePathLength = -1; //-1 bedeutet, diese Angabe wird durch Rekursionstiefe ersetzt
    }

    public class SubpathSamplerConstruktorData
    {
        public IRayCamera RayCamera;
        public LightSourceSampler LightSourceSampler;
        public IntersectionFinder IntersectionFinder;
        public MediaIntersectionFinder MediaIntersectionFinder = null;
        public PathSamplingType PathSamplingType;
        public int MaxPathLength;
        public IBrdfSampler BrdfSampler;
        public IPhaseFunctionSampler PhaseFunction;
    }

    public class SubpathSampler
    {
        private readonly ISubPathPointsSampler subPathSampler;
        private readonly IRayCamera rayCamera;        
        private readonly LightSourceSampler lightSourceSampler;
        private readonly bool globalMediaHasScattering;

        public PathSamplingType PathSamplingType { get; private set; }

        public SubpathSampler(SubpathSamplerConstruktorData data)
        {
            this.PathSamplingType = data.PathSamplingType;
            this.rayCamera = data.RayCamera;
            this.lightSourceSampler = data.LightSourceSampler;
            this.subPathSampler = CreateSubpathSampler(data);
            this.globalMediaHasScattering = data.MediaIntersectionFinder != null && (this.globalMediaHasScattering = data.MediaIntersectionFinder.GlobalParticipatingMediaFromScene.HasScatteringSomeWhereInMedium());
        }

        private static ISubPathPointsSampler CreateSubpathSampler(SubpathSamplerConstruktorData data)
        {
            switch (data.PathSamplingType)
            {
                case PathSamplingType.None:
                    return null;
                case PathSamplingType.NoMedia:
                    return new StandardSubPathPointsSampler(data.IntersectionFinder, data.LightSourceSampler, data.MaxPathLength, data.BrdfSampler);
                
                //Media
                case PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling:
                case PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling:
                case PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling:
                case PathSamplingType.ParticipatingMediaWithoutDistanceSampling:
                    return new MediaSubPathPointsSampler(data.MediaIntersectionFinder, data.LightSourceSampler, data.MaxPathLength, TransformPathSamplingTypeToMediaMode(data.PathSamplingType), data.BrdfSampler, data.PhaseFunction, data.RayCamera);
            }
            throw new Exception("Unknown PathSamplingType " + data.PathSamplingType);
        }

        private static MediaIntersectionFinder.IntersectionMode TransformPathSamplingTypeToMediaMode(PathSamplingType type)
        {
            switch (type)
            {
                case PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling:
                    return MediaIntersectionFinder.IntersectionMode.ShortRayWithDistanceSampling;
                case PathSamplingType.ParticipatingMediaLongRayOneSegmentWithDistanceSampling:
                    return MediaIntersectionFinder.IntersectionMode.LongRayOneSegmentWithDistanceSampling;
                case PathSamplingType.ParticipatingMediaLongRayManySegmentsWithDistanceSampling:
                    return MediaIntersectionFinder.IntersectionMode.LongRayManySegmentsWithDistanceSampling;
                case PathSamplingType.ParticipatingMediaWithoutDistanceSampling:
                    return MediaIntersectionFinder.IntersectionMode.NoDistanceSampling;
            }
            throw new Exception($"Can not transform {type} to {nameof(MediaIntersectionFinder.IntersectionMode)}");
        }

        public SubPath SamplePathFromCamera(int pixX, int pixY, IRandom rand)
        {
            var primaryRay = this.rayCamera.CreatePrimaryRay(pixX, pixY, rand);

            float cosAtCamera = this.rayCamera.UseCosAtCamera ? this.rayCamera.Forward * primaryRay.Direction : 1;
            //Hier muss nicht durch die cameraPdfW dividiert werden, da der PixelFilter == CameraPdfW entspricht. Das Pfadgewicht ergibt sich aus PixelFilter / CameraPdfW = 1. Somit kürzt es sich weg.
            Vector3D pathWeight = new Vector3D(1, 1, 1) * cosAtCamera;// / cameraPdfW;      

            float cameraPdfW = this.rayCamera.GetPixelPdfW(pixX, pixY, primaryRay.Direction);
            var sampleEvent = new BrdfSampleEvent()
            {
                Ray = primaryRay,
                ExcludedObject = null,
                PdfW = cameraPdfW,
                Brdf = pathWeight
            };
            
            float pathCreationTime = (float)rand.NextDouble();
            PathPoint[] points = this.subPathSampler.SamplePointsFromCamera(this.rayCamera.Forward, sampleEvent, pathWeight, pathCreationTime, rand);

            if (points.Last().PdfA == 0 || (points.Length > 2 && points.Last().LocationType == ParticipatingMedia.MediaPointLocationType.MediaInfinity))
            {
                points = points.ToList().GetRange(0, points.Length - 1).ToArray();
            }

            var eyePath = new SubPath(points, pathCreationTime);
            for (int i = 0; i < points.Length; i++) points[i].AssociatedPath = eyePath;
            return eyePath;
        }

        public SubPath SamplePathFromLighsource(IRandom rand)
        {
            var lightPoint = this.lightSourceSampler.GetRandomPointForLightPathCreation(rand);
            if (this.globalMediaHasScattering && lightPoint.LightSourceIsInfinityAway) return new SubPath(new PathPoint[0], 0); //Bei Richtungslicht aus dem unendlichen mit Mediapartikeln dazwischen ist der Attenuation-Term immer 0

            //Formel: Um die Photonanfangsflux zu berechnen http://cgg.mff.cuni.cz/~jaroslav/teaching/2016-npgr010/slides/11%20-%20npgr010-2016%20-%20PM.pptx Folie 11
            //Wichtige Erkentniss: Der EmissionPerArea-Term gibt die Leuchtkraft pro Fläche an. D.h. Flux-Gesamtlichtquelle / Area
            //                     PdfA entspricht 1 / Area -> Das bedeutet Emission / PdfA führt dazu, dass der Oberflächeninhalt der Lichtquelle sich aus der Formel rauskürtzt. Beachtet man das
            //                     nicht, dann bleibt der Flächeninhalt in der Formel enthalten und DirectLighting erzeugt andere Bilder als Photonmap-Direct
            Vector3D pathWeightFromPointOnLight = lightPoint.PointOnLight.Color * lightPoint.EmissionPerArea / lightPoint.PdfA;

            //Pfadgewicht nach dem Richtungssampeln (Das Gewicht bekommt der LightSuppathPoint mit Index 1)
            float cosAtLight = Math.Max(0, lightPoint.PointOnLight.ShadedNormal * lightPoint.Direction);
            Vector3D pathWeight = pathWeightFromPointOnLight * cosAtLight / lightPoint.PdfW;

            var sampleEvent = new BrdfSampleEvent()
            {
                Ray = new Ray(lightPoint.PointOnLight.Position, lightPoint.Direction),
                ExcludedObject = lightPoint.PointOnLight.IntersectedObject,
                PdfW = lightPoint.PdfW,
                Brdf = new Vector3D(1, 1, 1) * cosAtLight / lightPoint.PdfW,
            };

            IntersectionPoint lightIntersectionPoint = lightPoint.PointOnLight;

            float pathCreationTime = (float)rand.NextDouble();
            PathPoint[] points = this.subPathSampler.SamplePointsFromLightSource(lightIntersectionPoint, pathWeightFromPointOnLight, lightPoint.PdfA, sampleEvent, pathWeight, pathCreationTime, lightPoint.LightSourceIsInfinityAway, rand);
            if (points.Length > 1 && points.Last().IsLocatedOnLightSource || points.Last().PdfA == 0)
            {
                points = points.ToList().GetRange(0, points.Length - 1).ToArray(); //Ein Light-Subpath darf nicht auf der Lichtquelle enden
            }
            
            var lightPath = new SubPath(points, pathCreationTime);
            for (int i = 0; i < points.Length; i++) points[i].AssociatedPath = lightPath;
            return lightPath;
        }
    }
}
