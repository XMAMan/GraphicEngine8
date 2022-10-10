using System.Collections.Generic;
using RaytracingBrdf;
using GraphicMinimal;
using IntersectionTests;
using GraphicGlobal;
using RayCameraNamespace;
using static RaytracingLightSource.RayLightSource.Importance.PhotonData;
using RaytracingBrdf.SampleAndRequest;

namespace RaytracingLightSource.RayLightSource.Importance
{
    //Bekommt als Input ein Lichtpunkt auf der Lichtquelle und versendet dann das Photon zufällig im Raum und schaut, welche Photon-Intersectionspoints im Sichtbereich der Kamera liegen
    class ImportancePhotonSender
    {
        private readonly int recursionDepth;
        private readonly IntersectionFinder intersectionFinder;
        private readonly IRayCamera camera;
        private readonly IBrdfSampler brdfSampler;
        
        public ImportancePhotonSender(IntersectionFinder intersectionFinder, IRayCamera camera, int recursionDepth)
        {
            this.recursionDepth = recursionDepth;
            this.camera = camera;
            this.intersectionFinder = intersectionFinder;
            this.brdfSampler = new BrdfSampler();
        }

        public PhotonData[] SendPhoton(Vector3D position, Vector3D direction, IIntersecableObject pointSampler, IRandom rand)
        {
            return SendPhoton(new RayData(position, direction, pointSampler, rand));
        }

        private PhotonData[] SendPhoton(RayData rayData)
        {
            if (rayData.Depth > this.recursionDepth) return rayData.Photons.ToArray(); //Rekursionsende

            IntersectionPoint point = this.intersectionFinder.GetIntersectionPoint(rayData.Ray, 0, rayData.ExcluedObject);
            if (point == null) return rayData.Photons.ToArray();

            float ni = rayData.RayIsOutside ? 1 : point.RefractionIndex; //Brechungsindex von wo der Strahl herkommt
            float no = rayData.RayIsOutside ? point.RefractionIndex : 1; //Brechungsindex von der anderen Seite

            var brdfPoint = new BrdfPoint(point, rayData.Ray.Direction, ni, no);

            if (rayData.Photontype == PhotonType.None)
            {
                if (brdfPoint.IsOnlySpecular) rayData.Photontype = PhotonType.Specular; else rayData.Photontype = PhotonType.Diffuse;
            }

            //Photon darf auf Glas landen, da beim Importance-Map-Erstellen nur die Information wichtig ist, ob ein Photon überhaupt sichtbar ist und nicht, was sein Brdf-Wert ist
            if (/*point.IsOnlySpecular == false && */point.IsLocatedOnLightSource == false)
            {
                rayData.Photons.Add(new PhotonData() { IsVisibleFromCamera = IsPointVisibleFromCamera(point), Photontype = rayData.Photontype });
            }

            var newDirection = brdfPoint.SampleDirection(this.brdfSampler, rayData.Rand);
            if (newDirection == null) return rayData.Photons.ToArray();

            return SendPhoton(new RayData(newDirection, rayData));
        }

        private bool IsPointVisibleFromCamera(IntersectionPoint photonPoint)
        {
            if (this.camera.DepthOfFieldIsEnabled)
            {
                if (this.camera.IsPointInVieldOfFiew(photonPoint.Position) == false) return false;
            }                
            else
            {
                if (this.camera.GetPixelPositionFromEyePoint(photonPoint.Position) == null) return false; //Bei dieser Variante muss ich eine Kamera ohne Tiefenunschärfe verwenden
            }                

            IntersectionPoint glasPoint = this.intersectionFinder.GetIntersectionPoint(new Ray(this.camera.Position, Vector3D.Normalize(photonPoint.Position - this.camera.Position)), 0);
            if (glasPoint != null && IsGlasPoint(glasPoint)) return IsPointVisibleGlassTest(photonPoint, glasPoint, 0);
            return glasPoint != null && photonPoint.IntersectedObject == glasPoint.IntersectedObject && (photonPoint.Position - glasPoint.Position).Length() < 0.1f;
        }

        //Sendet einen Strahl von der Kamera zu 'point' auch durch Glass (Ohne Brechung)
        private bool IsPointVisibleGlassTest(IntersectionPoint photonPoint, IntersectionPoint lastGlassPoint, int depth)
        {
            if (depth > 10) return false;
            IntersectionPoint glasPoint = this.intersectionFinder.GetIntersectionPoint(new Ray(lastGlassPoint.Position, Vector3D.Normalize(photonPoint.Position - this.camera.Position)), 0, lastGlassPoint.IntersectedObject);
            if (glasPoint != null && IsGlasPoint(glasPoint)) return IsPointVisibleGlassTest(photonPoint, glasPoint, depth + 1);
            return glasPoint != null && photonPoint.IntersectedObject == glasPoint.IntersectedObject && photonPoint.OrientedFlatNormal * glasPoint.OrientedFlatNormal > 0.99f;
        }

        private static bool IsGlasPoint(IntersectionPoint point)
        {
            return BrdfFactory.CreateBrdf(point, point.OrientedFlatNormal, 1, 2).CanCreateRefractedRays;
        }

        class RayData
        {
            public int Depth = 0;
            public Ray Ray;
            public IIntersecableObject ExcluedObject;
            public List<PhotonData> Photons = new List<PhotonData>();
            public PhotonType Photontype = PhotonType.None;
            public bool RayIsOutside = true;
            public IRandom Rand;

            //Kontruktor für Erzeugung auf der Lichtquelle
            public RayData(Vector3D position, Vector3D direction, IIntersecableObject pointSampler, IRandom rand)
            {
                this.Ray = new Ray(position, direction);
                this.ExcluedObject = pointSampler;
                this.Rand = rand;
            }

            //Konstruktor nach Brdf-Sampling
            public RayData(BrdfSampleEvent sampleEvent, RayData oldData)
            {
                this.Depth = oldData.Depth + 1;
                this.Ray = sampleEvent.Ray;
                this.ExcluedObject = sampleEvent.ExcludedObject;
                this.Photons = oldData.Photons;
                this.Photontype = oldData.Photontype;
                this.RayIsOutside = sampleEvent.RayWasRefracted ? !this.RayIsOutside : this.RayIsOutside;
                this.Rand = oldData.Rand;
            }
        }
    }

    class PhotonPoints
    {
        public List<PhotonData> Photons;

        public PhotonPoints(List<PhotonData> photons)
        {
            this.Photons = photons;
        }
    }

    class PhotonData
    {
        public enum PhotonType { None, Diffuse, Specular } //Wenn Photon aus Lichtquelle austritt, ist es noch none. Trifft es auf Oberfläche mit Streuwinkel == 0, dann wird es Specualar, sonst Diffuse

        public bool IsVisibleFromCamera;
        public PhotonType Photontype;
    }
}
