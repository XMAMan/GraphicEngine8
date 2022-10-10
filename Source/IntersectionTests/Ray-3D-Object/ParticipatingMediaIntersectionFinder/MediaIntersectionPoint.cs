using GraphicMinimal;
using ParticipatingMedia;
using ParticipatingMedia.Media;
using System;

namespace IntersectionTests
{
    //Ein 3D-Punkt, der entweder auf ein Surface liegt, oder in ein Media
    public class MediaIntersectionPoint
    {
        public IntersectionPoint SurfacePoint { get; private set; } //Ist nur gefüllt, wenn Location = Surface (MediaBorder-Punkte gehören nicht dazu)
        private Vector3D MediaPoint = null;
        private ParticipatingMediaStack mediaStack;
        public MediaPointLocationType Location { get; private set; }

        public bool IsInGlobalMedia { get { return this.mediaStack.StackIsEmpty; } }

        public IParticipatingMedia CurrentMedium
        {
            get
            {
                return this.mediaStack.CurrentMedium;
            }
        }

        public bool HasScattering()
        {
            return (this.Location == MediaPointLocationType.MediaParticle || this.Location == MediaPointLocationType.MediaInfinity) &&
                this.CurrentMedium.HasScatteringOnPoint(this.Position);
        }

        public Vector3D Position
        {
            get
            {
                if (this.SurfacePoint != null) return this.SurfacePoint.Position;
                if (this.MediaPoint != null) return this.MediaPoint;
                throw new MemberAccessException("Es muss SurfacePoint oder MediaPoint != null sein");
            }
        }

        private MediaIntersectionPoint() { }

        public static MediaIntersectionPoint CreateCameraPoint(Vector3D cameraLoation, IParticipatingMedia globalMedia)
        {
            return new MediaIntersectionPoint()
            {
                Location = MediaPointLocationType.Camera,
                SurfacePoint = null,
                MediaPoint = cameraLoation,
                mediaStack = new ParticipatingMediaStack(globalMedia),
            };
        }

        public static MediaIntersectionPoint CreateCameraPoint(Vector3D cameraLoation, IParticipatingMedia globalMedia, IIntersectableRayDrawingObject mediaObjectCameraIsInside)
        {
            var stack = new ParticipatingMediaStack(globalMedia);
            stack.CrossBorder(mediaObjectCameraIsInside);
            return new MediaIntersectionPoint()
            {
                Location = MediaPointLocationType.Camera,
                SurfacePoint = null,
                MediaPoint = cameraLoation,
                mediaStack = stack,
            };
        }

        //Konstruktor für Punkt auf Lichtquelle welcher im Vacuum liegt
        public static MediaIntersectionPoint CreatePointOnLight(IntersectionPoint surfacePoint, IParticipatingMedia globalMedia)
        {
            return new MediaIntersectionPoint()
            {
                Location = MediaPointLocationType.Surface,
                SurfacePoint = surfacePoint,
                MediaPoint = null,
                mediaStack = new ParticipatingMediaStack(globalMedia),
            };
        }

        //Konstruktor für Punkt auf Lichtquelle welcher in Medium liegt
        public static MediaIntersectionPoint CreatePointOnLight(IntersectionPoint surfacePoint, IParticipatingMedia globalMedia, IIntersectableRayDrawingObject mediaObjectLightPointIsInside)
        {
            var lightPoint = CreatePointOnLight(surfacePoint, globalMedia);
            lightPoint.mediaStack.CrossBorder(mediaObjectLightPointIsInside);
            return lightPoint;
        }

        //Konstruktor für Punkt auf MediaParticel,MediaBorder oder Infinity
        public static MediaIntersectionPoint CreateMediaPoint(MediaIntersectionPoint oldPointRaysComeFrome, Vector3D mediaPointLocation, MediaPointLocationType locationType)
        {
            return new MediaIntersectionPoint()
            {
                Location = locationType,
                SurfacePoint = null,
                MediaPoint = mediaPointLocation,
                mediaStack = oldPointRaysComeFrome.mediaStack,
            };
        }

        //Konstruktor für Surface-Punkte, wo dessen Objekt ein Medium enthält
        public static MediaIntersectionPoint CreateMediaBorderPoint(MediaIntersectionPoint oldPointRaysComeFrome, IntersectionPoint surfaceBorderPoint)
        {
            return new MediaIntersectionPoint()
            {
                Location = surfaceBorderPoint.RefractionIndex == 1 ? MediaPointLocationType.NullMediaBorder : MediaPointLocationType.MediaBorder,
                SurfacePoint = surfaceBorderPoint,
                MediaPoint = surfaceBorderPoint.Position,
                mediaStack = oldPointRaysComeFrome.mediaStack,
            };
        }

        //Konstruktor für Surface-Punkt, welcher nicht auf der Lichtquelle startet
        public static MediaIntersectionPoint CreateSurfacePoint(MediaIntersectionPoint oldPointRaysComeFrome, IntersectionPoint surfacePoint)
        {
            return new MediaIntersectionPoint()
            {
                Location = MediaPointLocationType.Surface,
                SurfacePoint = surfacePoint,
                MediaPoint = null,
                mediaStack = oldPointRaysComeFrome.mediaStack,
            };
        }

        public MediaIntersectionPoint(MediaIntersectionPoint copy)
        {
            this.SurfacePoint = copy.SurfacePoint;
            this.MediaPoint = copy.MediaPoint != null ? new Vector3D(copy.MediaPoint) : null;
            this.mediaStack = new ParticipatingMediaStack(copy.mediaStack);
            this.Location = copy.Location;
        }

        public void JumpToNextMediaBorder(IntersectionPoint mediaBorderPoint)
        {
            this.MediaPoint = mediaBorderPoint.Position;
            this.mediaStack.CrossBorder(mediaBorderPoint.IntersectedRayHeigh);
            this.Location = mediaBorderPoint.RefractionIndex == 1 ? MediaPointLocationType.NullMediaBorder : MediaPointLocationType.MediaBorder;
        }

        //Wenn das hier ein Borderpunkt ist und ich noch vor dem JumpToNextMediaBorder-Aufruf bin, dann kann man hiermit abfragen, welches Medium hinter der Bordergrenze kommt
        public IParticipatingMedia GetNextMediaAfterCrossingBorder()
        {
            //if (this.Location != MediaPointLocationType.MediaBorder) throw new Exception("Diese Funktion darf nur für Borderpunkte benutzt werden");
            return this.mediaStack.GetNextMediaAfterCrossingBorder(this.SurfacePoint.IntersectedRayHeigh);
        }

        public bool ThereIsNoMediaChangeAfterCrossingBorder()
        {
            return this.mediaStack.CurrentMedium == GetNextMediaAfterCrossingBorder();
        }
    }
}
