using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia;
using ParticipatingMedia.Media;
using RayTracerGlobal;
using RaytracingBrdf;
using RaytracingBrdf.SampleAndRequest;

namespace SubpathGenerator
{
    //Ein PathPoint liegt entweder auf Oberflächen oder hängt in der Luft(Media,Kamera)
    public class PathPoint : IPoint
    {
        public IntersectionPoint SurfacePoint { get; private set; } = null;
        public BrdfPoint BrdfPoint { get; private set; } = null; //Diese Property ist beim ersten Subpathpoint (Camera/Lightsource) null

        public MediaIntersectionPoint MediaPoint { get; private set; } = null;
        public MediaLine LineToNextPoint = null; //Bei ParticpatingMedia-Verfahren steht hier ein Wert       

        public MediaPointLocationType LocationType { get; private set; } = MediaPointLocationType.Unknown;
        public Vector3D PathWeight { get; private set; }
        public BrdfSampleEvent BrdfSampleEventOnThisPoint { get; private set; }
        public Vector3D Position { get; private set; }
        public Vector3D Normal { get; private set; } //Surface+Kamera
        public bool IsLocatedOnLightSource { get; private set; }

        //Liegt es auf Umgebungslichtquelle/Richtungslichtquelle?
        //Wenn ja gelten folgende Regeln: Man darf beim GeometryTerm und beim PdfW2PdfA-Umrechung nicht durch die Quadrat-Distanz dividieren
        //Man darf beim Radiosity nicht mit Flächeninhalt vom jeweiligen Light-Texel multiplizieren
        public bool IsLocatedOnInfinityAwayLightSource { get; private set; } = false; 
        public bool IsDiffusePoint { get; private set; } //Ist true, wenn es auf Surface liegt, was diffusen Anteil(Diffuse, Microfacet oder Phong) enthält oder MediaPartikel. Das BrdfSample-Event hat KEIN Einfluß auf diese Property

        public double PdfA = double.NaN; //Geometrisch Summe aller PdfAs aller Vorgängerknoten bis zu diesen Knoten (Das enthält die PdfW und PdfL)
        public double PdfAReverse = double.NaN; //Das ist die PdfW in PdfA umgerechnet, um vom Nachfolger zu diesen Punkt zu gehen (Das ist keine Geometrische Summe, da ich erst beim Full-Patherstellen diese Summe bilden kann)
        public float PdfLFromNextPointToThis = float.NaN; //Die Distance-Sampling-Pdf, um vom Nachfolger zu diesen Punkt zu kommen

        public int Index = -1; //0..N = Point der durch ein SubPathSapmler erzeugt wurde; -1 = Wurde durch ein FullPathSampler erzeugt
        public PathPoint Predecessor = null;
        public SubPath AssociatedPath = null;       

        public PathPoint(PathPoint copy)
        {
            this.SurfacePoint = copy.SurfacePoint;
            this.BrdfPoint = copy.BrdfPoint;
            this.MediaPoint = copy.MediaPoint;
            this.LineToNextPoint = copy.LineToNextPoint;
            this.LocationType = copy.LocationType;
            this.PathWeight = copy.PathWeight;
            this.BrdfSampleEventOnThisPoint = copy.BrdfSampleEventOnThisPoint;
            this.Position = copy.Position;
            this.Normal = copy.Normal;
            this.IsLocatedOnLightSource = copy.IsLocatedOnLightSource;
            this.IsLocatedOnInfinityAwayLightSource = copy.IsLocatedOnInfinityAwayLightSource;
            this.IsDiffusePoint = copy.IsDiffusePoint;
            this.PdfA = copy.PdfA;
            this.PdfAReverse = copy.PdfAReverse;
            this.PdfLFromNextPointToThis = copy.PdfLFromNextPointToThis;
            this.Index = copy.Index;
            this.AssociatedPath = copy.AssociatedPath;
            this.Predecessor = copy.Predecessor;
        }

        public float this[int d]
        {
            get
            {
                return this.Position[d];
            }
        }

        
        //Kamera ohne Media
        public static PathPoint CreateCameraPoint(Vector3D position, Vector3D forwardDirection, BrdfSampleEvent sampleEvent)
        {
            return new PathPoint(position, forwardDirection, null) { Index = 0, BrdfSampleEventOnThisPoint = sampleEvent };
        }

        //Kamera mit Media
        public static PathPoint CreateCameraPoint(MediaIntersectionPoint mediaPoint, Vector3D forwardDirection, BrdfSampleEvent sampleEvent)
        {
            return new PathPoint(mediaPoint.Position, forwardDirection, mediaPoint) { Index = 0, BrdfSampleEventOnThisPoint = sampleEvent };
        }

        public static PathPoint CreateLightsourcePointWithoutSurroundingMedia(IntersectionPoint point, Vector3D pathWeight, bool lighIsInfinityAway, BrdfSampleEvent sampleEvent)
        {
            return new PathPoint(new BrdfPoint(point, null, float.NaN, float.NaN), pathWeight) 
            { 
                Index = 0, 
                IsLocatedOnInfinityAwayLightSource = point.IsLocatedOnLightSource && lighIsInfinityAway,
                BrdfSampleEventOnThisPoint = sampleEvent
            };
        }

        public static PathPoint CreateLightsourcePointWithSurroundingMedia(IntersectionPoint point, Vector3D pathWeight, MediaIntersectionPoint mediaPoint, bool lighIsInfinityAway, BrdfSampleEvent sampleEvent = null)
        {
            return new PathPoint(new BrdfPoint(point, null, float.NaN, float.NaN), pathWeight)
            {
                Index = 0,
                MediaPoint = mediaPoint,
                IsLocatedOnInfinityAwayLightSource = point.IsLocatedOnLightSource && lighIsInfinityAway,
                BrdfSampleEventOnThisPoint = sampleEvent
            };
        }

        public static PathPoint CreateSurfacePointWithoutSurroundingMedia(BrdfPoint brdfPoint, Vector3D pathWeight)
        {
            return new PathPoint(brdfPoint, pathWeight);
        }

        public static PathPoint CreateSurfacePointWithSurroundingMedia(BrdfPoint brdfPoint, Vector3D pathWeight, MediaIntersectionPoint mediaPoint)
        {
            return new PathPoint(brdfPoint, pathWeight) { MediaPoint = mediaPoint };
        }

        public static PathPoint CreateMediaParticlePoint(MediaIntersectionPoint mediaPoint, Vector3D pathWeight)
        {
            return new PathPoint(mediaPoint, pathWeight, mediaPoint.Position, MediaPointLocationType.MediaParticle);
        }

        public static PathPoint CreateMediaInfinityPoint(MediaIntersectionPoint mediaPoint, Vector3D pathWeight)
        {
            return new PathPoint(mediaPoint, pathWeight, mediaPoint.Position, MediaPointLocationType.MediaInfinity);
        }

        public static PathPoint CreateMediaBorderPoint(BrdfPoint brdfPoint, Vector3D pathWeight, MediaIntersectionPoint mediaPoint)
        {
            return new PathPoint(mediaPoint, pathWeight, mediaPoint.Position, mediaPoint.SurfacePoint.RefractionIndex == 1 ? MediaPointLocationType.NullMediaBorder : MediaPointLocationType.MediaBorder)
            {
                SurfacePoint = mediaPoint.SurfacePoint,
                BrdfPoint = brdfPoint,
                Normal = mediaPoint.SurfacePoint.ShadedNormal,
                IsLocatedOnLightSource = mediaPoint.SurfacePoint.IsLocatedOnLightSource,
                IsDiffusePoint = false// mediaPoint.SurvacePoint.IsOnlySpecular == false
            };
        }

        //CameraPoint
        private PathPoint(Vector3D position, Vector3D forwardDirection, MediaIntersectionPoint mediaPoint)
        {
            this.SurfacePoint = new IntersectionPoint(new Vertex(position, forwardDirection), null, null, null, forwardDirection, null, null, null);
            this.MediaPoint = mediaPoint;
            this.PathWeight = new Vector3D(1, 1, 1);
            this.Position = position;
            this.Normal = forwardDirection;
            this.IsLocatedOnLightSource = false;
            this.IsDiffusePoint = false;
            this.LocationType = MediaPointLocationType.Camera;
        }

        //SurfacePoint
        private PathPoint(BrdfPoint brdfPoint, Vector3D pathWeight)
        {
            this.BrdfPoint = brdfPoint;
            this.SurfacePoint = brdfPoint.SurfacePoint;
            this.PathWeight = pathWeight;
            this.Position = brdfPoint.SurfacePoint.Position;
            this.Normal = brdfPoint.SurfacePoint.ShadedNormal;
            this.IsLocatedOnLightSource = brdfPoint.SurfacePoint.IsLocatedOnLightSource;
            this.IsLocatedOnInfinityAwayLightSource = brdfPoint.SurfacePoint.IsLocatedOnInfinityAwayLightSource;
            this.IsDiffusePoint = brdfPoint.IsOnlySpecular == false;
            this.LocationType = MediaPointLocationType.Surface;
        }

        //MediaPoint (MediaParticel,MediBorder,Infinity)
        private PathPoint(MediaIntersectionPoint mediaPoint, Vector3D pathWeight, Vector3D position, MediaPointLocationType locationType)
        {
            this.MediaPoint = new MediaIntersectionPoint(mediaPoint);
            this.PathWeight = pathWeight;
            this.LocationType = locationType;
            this.Position = position;
            this.IsLocatedOnLightSource = false;
            this.IsDiffusePoint = locationType == MediaPointLocationType.MediaParticle;
        }

        //Getter, die nicht im Kopierkonstruktor extra mit kopiert werden müssen

        public Vector3D DirectionToThisPoint { get { return this.Predecessor.BrdfSampleEventOnThisPoint.Ray.Direction; } }

        //Surface
        public IIntersecableObject IntersectedObject
        {
            get
            {
                return this.SurfacePoint.IntersectedObject;
            }
        }

        //Surface
        public bool IsSpecularSurfacePoint
        {
            get
            {
                return (this.LocationType == MediaPointLocationType.Surface || this.LocationType == MediaPointLocationType.MediaBorder) && this.BrdfPoint.IsOnlySpecular == true;
            }
        }

        //MediaPunkte
        public IParticipatingMedia SurrondingMedia { get { return this.MediaPoint.CurrentMedium; } }//In diesen Medium liegt der Punkt

        //Lichtquelle, die vom Pathtracer getroffen werden kann
        public bool IsLocatedOnLightSourceWhichCanBeHitViaPathtracing
        {
            get
            {
                return this.IsLocatedOnLightSource && IsLightSourceWhichCanBeHitViaPathtracing(this.SurfacePoint.IntersectedRayHeigh.Propertys.RaytracingLightSource);
            }
        }

        //Lichtquelle, die vom Pathtracer getroffen werden kann
        private static bool IsLightSourceWhichCanBeHitViaPathtracing(ILightSourceDescription lichtquellenArt)
        {
            return (lichtquellenArt is FarAwayDirectionLightDescription) == false;
        }

        
        public void UpdateBrdfSampleEvent(BrdfSampleEvent sampleEvent)
        {
            this.BrdfSampleEventOnThisPoint = sampleEvent;
        }

        public override string ToString()
        {
            if (this.IsLocatedOnLightSource) return "LightSource";
            return this.LocationType.ToString();
        }
    }
}
