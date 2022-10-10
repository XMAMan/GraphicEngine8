using System.Collections.Generic;
using FullPathGenerator;
using SubpathGenerator;
using IntersectionTests;
using Photonusmap;
using GraphicMinimal;
using GraphicGlobal;
using System.Linq;

namespace RaytracingColorEstimator
{
    //Diese Klasse enthält für alle ColorEstimator-Ableitungen aus dem RaytracingMethods-Projekt alle Daten/Funktionen, um die Radiance von ein einzelnen Pixel zu berechnen
    //Die Raytracing-Verfahren sollen keine eigenen Variablen besitzen außer ein PixelRadianceCalculator
    public class PixelRadianceCalculator
    {
        //Diese Variable wird von vielen Threads gleichzeitig genutzt (Alle Daten, die sich während des Renderns nicht verändert)
        private readonly PixelRadianceData data;

        public GlobalObjectPropertys GlobalProps { get => data.Frame3DData.GlobalObjektPropertys; }
        public int ScreenWidth { get => data.Frame3DData.ScreenWidth; }
        public int ScreenHeight { get => data.Frame3DData.ScreenHeight; }
        public IntersectionFinder IntersectionFinder { get => data.IntersectionFinder; }

        //Diese Variablen dürfen immer nur von jeweils ein Thread benutzt werden
        public FullPathFrameData FrameData; //Gültig für die Lebensdauer von ein Frame (Beim Photonmapping wird diese Variable von vielen genutzt)
        public PixelPhotonenCounter PixelPhotonenCounter = null; //Gültig über mehrere Frame-Iterationen hinweg

        public PixelRadianceCalculator(PixelRadianceData radianceData)
        {
            this.data = radianceData;
            this.FrameData = new FullPathFrameData();
        }

        public PixelRadianceCalculator(PixelRadianceCalculator copy)
        {
            this.data = copy.data;
            this.FrameData = new FullPathFrameData();
            if (copy.PixelPhotonenCounter != null)
            {
                this.PixelPhotonenCounter = new PixelPhotonenCounter(copy.PixelPhotonenCounter);            
            }
            
        }

        //Für das VCM,ProgressivPhotonmapping
        public PhotonMap CreateSurfacePhotonmapWithSingleThread(IRandom rand)
        {
            var lightPahts = CreateLightPathListWithSingleThread(rand);
            return new PhotonMap(lightPahts, this.GlobalProps.PhotonCount, (text, zahl) => { }, 1, int.MaxValue);
        }

        //Für PhotonmapDirect
        public Photonmaps CreateSurfacePhotonmapWithMultipleThreads()
        {
            var lightPahts = CreateLightPathListWithMultipleThreads(this.data.Frame3DData);

            var photonMap = new PhotonMap(lightPahts, this.GlobalProps.PhotonCount, this.data.Frame3DData.ProgressChanged, 1, int.MaxValue)
            {
                SearchRadius = PhotonMapSearchRadiusCalculator.GetSearchRadiusForPhotonmapWithPhotonDensity(lightPahts, this.data.IntersectionFinder, this.data.RayCamera)
            };

            return new Photonmaps() { GlobalSurfacePhotonmap = photonMap };
        }

        //Für das Photonmapping
        public Photonmaps CreateSurfaceAndCausticMapWithMultipleThreads()
        {
            var data = this.data.Frame3DData;
            var lightPahts = CreateLightPathListWithMultipleThreads(data);

            var photonMap = new PhotonMap(lightPahts, data.GlobalObjektPropertys.PhotonCount, data.ProgressChanged, 2, int.MaxValue)
            {
                SearchRadius = PhotonMapSearchRadiusCalculator.GetSearchRadiusForPhotonmapWithPhotonDensity(lightPahts, this.data.IntersectionFinder, this.data.RayCamera)
            };

            var causticSurfacemap = new CausticMap(lightPahts, data.GlobalObjektPropertys.PhotonCount, data.ProgressChanged, 1, 1)
            {
                SearchRadius = PhotonMapSearchRadiusCalculator.GetSearchRadiusForPhotonmapWithPixelFootprint(this.data.IntersectionFinder, this.data.RayCamera)
            };

            return new Photonmaps() { GlobalSurfacePhotonmap = photonMap, CausticSurfacemap = causticSurfacemap };
        }

        //Für UPBP
        public Photonmaps CreateVolumetricPhotonmap(IRandom rand, float beamDataLineQueryReductionFactor, int iteration)
        {
            int photonCount = this.GlobalProps.PhotonCount;
            var lightPahts = CreateLightPathListWithSingleThread(rand);

            //Das ist aus SmallUPBP
            //Hinweis: SmappUPBP verwendet keine Beam-Reduction und die minimale MeanFreePath-Length zum Speichern
            //von Beams ist 0. Das bedeutet alle Beams werden ohne Reduzierung in allen Medien gespeichert.
            //Der Radius bleibt über alle Iterationen für PP3D,PB2D,BB1D konstant
            /*float MinPhotonmapSearchRadius = 1e-7f; //Siehe SmallUPBP "Purely for numeric stability"
            float mSurfRadiusInitial = 0.0361190923f;
            float mPP3DRadiusInitial = 0.0361190923f;
            float mPB2DRadiusInitial = 0.0361190923f;
            float mBB1DRadiusInitial = 0.0361190923f;

            float mSurfRadiusAlpha = 0.750000000f;
            float mPP3DRadiusAlpha = 1.00000000f;       //RadiusAlpha von 0 bewirkt, dass unten bei Pow(iteration,0) immer 1 rauskommt, was bedeutet der Radius verringert sich überhaupt nicht
            float mPB2DRadiusAlpha = 1.00000000f;
            float mBB1DRadiusAlpha = 1.00000000f;

            float radiusSurf = Math.Max(MinPhotonmapSearchRadius, mSurfRadiusInitial * (float)Math.Pow(iteration, (mSurfRadiusAlpha - 1) * 0.5f));
            float radiusPP3D = Math.Max(MinPhotonmapSearchRadius, mPP3DRadiusInitial * (float)Math.Pow(iteration, (mPP3DRadiusAlpha - 1) * (1.0f / 3.0f)));
            float radiusPB2D = Math.Max(MinPhotonmapSearchRadius, mPB2DRadiusInitial * (float)Math.Pow(iteration, (mPB2DRadiusAlpha - 1) * 0.5f));
            float radiusBB1D = Math.Max(MinPhotonmapSearchRadius, mBB1DRadiusInitial * (float)Math.Pow(1 + iteration * (photonCount * beamDataLineQueryReductionFactor) / photonCount, mBB1DRadiusAlpha - 1));

            float factor = 1f;

            return new Photonmaps()
            {
                GlobalSurfacePhotonmap = new PhotonMap(lightPahts, photonCount, (text, zahl) => { }, 1, int.MaxValue) { SearchRadius = radiusSurf * factor },
                PointDataPointQueryMap = new PointDataPointQueryMap(lightPahts, photonCount, (text, zahl) => { }) { SearchRadius = radiusPP3D * factor },
                PointDataBeamQueryMap = new PointDataBeamQueryMap(lightPahts, photonCount, (text, zahl) => { }, radiusPB2D * factor),
                BeamDataLineQueryMap = new BeamDataLineQueryMap(lightPahts, photonCount, radiusBB1D * factor, beamDataLineQueryReductionFactor, rand)
            };*/

            float beamSearchRadius = VolumetricPhotonmapRadiusCalculator.GetSearchRadiusFromLighPathList(lightPahts, this.data.RayCamera) * this.GlobalProps.PhotonmapSearchRadiusFactor;

            //Achtung: Der SuchRadius von GlobalSurfacePhotonmap und PointDataPointQueryMap ist noch 0. 
            //         Er muss noch auf den PixelFootprint bei GetFullPathSampleResult() gesetzt werden!!!
            return new Photonmaps()
            {
                GlobalSurfacePhotonmap = new PhotonMap(lightPahts, photonCount, (text, zahl) => { }, 1, int.MaxValue),
                PointDataPointQueryMap = new PointDataPointQueryMap(lightPahts, photonCount, (text, zahl) => { }),
                PointDataBeamQueryMap = new PointDataBeamQueryMap(lightPahts, photonCount, (text, zahl) => { }, beamSearchRadius),
                BeamDataLineQueryMap = new BeamDataLineQueryMap(lightPahts, photonCount, beamSearchRadius, beamDataLineQueryReductionFactor, rand)
            };
        }

        //Volumetrisches Beammapping
        public Photonmaps CreateVolumetricBeammap(IRandom rand, float beamSearchRadius = float.NaN)
        {
            var lightPahts = CreateLightPathListWithSingleThread(rand);
            if (float.IsNaN(beamSearchRadius)) beamSearchRadius = VolumetricPhotonmapRadiusCalculator.GetSearchRadiusFromLighPathList(lightPahts, this.data.RayCamera);
            return new Photonmaps() { BeamDataLineQueryMap = new BeamDataLineQueryMap(lightPahts, this.GlobalProps.PhotonCount, beamSearchRadius, 1, rand) };
        }

        public List<SubPath> CreateLightPathListWithSingleThread(IRandom rand)
        {
            return MultipleLightPathSampler.SampleNLightPathsWithASingleThread(this.data.LightPathSampler, this.GlobalProps.PhotonCount, rand, this.data.Frame3DData.StopTrigger);
        }

        //PhotonmapDirectPixel
        public List<SubPath> CreateLightPathListWithMultipleThreads(RaytracingFrame3DData data)
        {
            ConstruktorDataForPhotonmapCreation photonmapData = new ConstruktorDataForPhotonmapCreation()
            {
                LightPathSampler = this.data.LightPathSampler,
                FrameId = 0,
                LightPathCount = data.GlobalObjektPropertys.PhotonCount,
                ProgressChanged = data.ProgressChanged,
                StopTrigger = data.StopTrigger,
                ThreadCount = data.GlobalObjektPropertys.ThreadCount,
                RayCamera = this.data.RayCamera
            };

            return MultipleLightPathSampler.SampleNLightPahts(photonmapData);
        }

        public float GetPixelFootprint(int pixX, int pixY)
        {
            var ray = this.data.RayCamera.CreatePrimaryRay(pixX, pixY, null);
            var point = this.data.IntersectionFinder.GetIntersectionPoint(ray, 0);
            if (point != null)
            {
                return this.data.RayCamera.GetPixelFootprintSize(point.Position).X;
            }
            return 0; //Ein zu großer Suchradius verlangsamt die Photonmapabfrage enorm
        }

        public float GetExactPixelFootprintArea(int pixX, int pixY)
        {
            var p1 = this.data.IntersectionFinder.GetIntersectionPoint(this.data.RayCamera.CreatePrimaryRayWithPixi(pixX, pixY, new Vector2D(-0.5f, -0.5f)), 0);
            var p2 = this.data.IntersectionFinder.GetIntersectionPoint(this.data.RayCamera.CreatePrimaryRayWithPixi(pixX, pixY, new Vector2D(0.5f, -0.5f)), 0);
            var p3 = this.data.IntersectionFinder.GetIntersectionPoint(this.data.RayCamera.CreatePrimaryRayWithPixi(pixX, pixY, new Vector2D(0.5f, 0.5f)), 0);
            var p4 = this.data.IntersectionFinder.GetIntersectionPoint(this.data.RayCamera.CreatePrimaryRayWithPixi(pixX, pixY, new Vector2D(-0.5f, 0.5f)), 0);
            bool onSamePlane = p1 != null && p2 != null && p3 != null && p4 != null && (p1.OrientedFlatNormal * p2.OrientedFlatNormal > 0.9) && (p1.OrientedFlatNormal * p3.OrientedFlatNormal > 0.9) && (p1.OrientedFlatNormal * p4.OrientedFlatNormal > 0.9);
            if (onSamePlane == false) return GetPixelFootprint(pixX, pixY); 
            Triangle t1 = new Triangle(p1.Position, p2.Position, p3.Position);
            Triangle t2 = new Triangle(p3.Position, p4.Position, p1.Position);
            return t1.SurfaceArea + t2.SurfaceArea;
        }

        //Diese Funktion kann man für coole Effekte nutzen
        public bool IsEdgePixel(int pixX, int pixY)
        {
            var p1 = this.data.IntersectionFinder.GetIntersectionPoint(this.data.RayCamera.CreatePrimaryRayWithPixi(pixX, pixY, new Vector2D(-0.5f, -0.5f)), 0);
            var p2 = this.data.IntersectionFinder.GetIntersectionPoint(this.data.RayCamera.CreatePrimaryRayWithPixi(pixX, pixY, new Vector2D(0.5f, -0.5f)), 0);
            var p3 = this.data.IntersectionFinder.GetIntersectionPoint(this.data.RayCamera.CreatePrimaryRayWithPixi(pixX, pixY, new Vector2D(0.5f, 0.5f)), 0);
            var p4 = this.data.IntersectionFinder.GetIntersectionPoint(this.data.RayCamera.CreatePrimaryRayWithPixi(pixX, pixY, new Vector2D(-0.5f, 0.5f)), 0);
            bool onSamePlane = p1 != null && p2 != null && p3 != null && p4 != null && (p1.OrientedFlatNormal * p2.OrientedFlatNormal > 0.9) && (p1.OrientedFlatNormal * p3.OrientedFlatNormal > 0.9) && (p1.OrientedFlatNormal * p4.OrientedFlatNormal > 0.9);
            bool allPointsAreNull = p1 == null && p2 == null && p3 == null && p4 == null;

            return onSamePlane == false && allPointsAreNull == false;
        }

        public bool IsPointInFieldOfView(Vector3D point)
        {
            return this.data.RayCamera.GetPixelPositionFromEyePoint(point) != null;
        }

        public FullPathSampleResult SampleSingleEyePath(int pixX, int pixY, IRandom rand)
        {
            var eyePath = this.data.EyePathSampler.SamplePathFromCamera(pixX, pixY, rand);
            return this.data.FullPathSampler.SampleFullPaths(eyePath, null, this.FrameData, rand);
        }

        public FullPathSampleResult SampleSingleEyeAndLightPath(int pixX, int pixY, IRandom rand)
        {
            var eyePath = this.data.EyePathSampler.SamplePathFromCamera(pixX, pixY, rand);
            var lightPath = this.data.LightPathSampler.SamplePathFromLighsource(rand);
            return this.data.FullPathSampler.SampleFullPaths(eyePath, lightPath, this.FrameData, rand);
        }

        public FullPathSampleResult SampleSingleLightPath(IRandom rand)
        {
            var lightPath = this.data.LightPathSampler.SamplePathFromLighsource(rand);
            return this.data.FullPathSampler.SampleFullPaths(null, lightPath, this.FrameData, rand);
        }

        public IntersectionPoint GetFirstEyePoint(int pixX, int pixY)
        {
            return this.data.IntersectionFinder.GetIntersectionPoint(this.data.RayCamera.CreatePrimaryRay(pixX, pixY, null), 0);
        }

        public SubPath SampleEyePath(int pixX, int pixY, IRandom rand)
        {
            return this.data.EyePathSampler.SamplePathFromCamera(pixX, pixY, rand);
        }

        public SubPath SampleLightPath(IRandom rand)
        {
            return this.data.LightPathSampler.SamplePathFromLighsource(rand);
        }

        public List<IIntersecableObject> GetIIntersecableObjectList()
        {
            return this.data.IntersectionFinder.RayObjekteRawList;
        }

        public static bool HasSceneAnyMedia(RaytracingFrame3DData data)
        {
            return data.DrawingObjects.Any(x => x.DrawingProps.MediaDescription != null);
        }
    }
}
