using System;
using System.Collections.Generic;
using System.Linq;
using IntersectionTests;
using GraphicMinimal;
using RayObjects;
using GraphicGlobal;
using RaytracingLightSource.Basics;
using RaytracingLightSource.RayLightSource.Importance;
using RaytracingLightSource.Basics.LightDirectionSampler;

namespace RaytracingLightSource
{
    class ImportanceSurfaceWithSpot : IRayLightSource
    {
        public IIntersectableRayDrawingObject RayDrawingObject { get; private set; }
        public float EmittingSurfaceArea { get; private set; }
        public float Emission { get; private set; }
        public float EmissionPerArea { get; protected set; }

        private readonly IEnumerable<IFlatRandomPointCreator> flats;              //Für das DirectLighting
        private readonly RectangleDirectLightSourceSampler directLightingSampler; //Für das DirectLighting
        private readonly ImportanceSurfacePointSampler surfaceSampler;            //Für das LightTracing
        private readonly ILightDirectionSampler lightDirectionSampler;            //Für das LightTracing


        public ImportanceSurfaceWithSpot(List<IUVMapable> uvmaps, ConstruktorDataForLightSourceSampler lightCreationData, ImportanceSurfaceWithSpotLightDescription lightDescription)
        {
            this.RayDrawingObject = uvmaps.First().RayHeigh;
            this.flats = uvmaps.Cast<IFlatRandomPointCreator>();
            var flatListSampler = new FlatSurfaceListSamplingUniform(flats);
            this.EmittingSurfaceArea = flatListSampler.SurfaceArea;
            this.Emission = (this.RayDrawingObject.Propertys.RaytracingLightSource as IRaytracingLightSource).Emission;
            this.EmissionPerArea = this.Emission / this.EmittingSurfaceArea;

            Vector3D diffuseDirection = this.flats.First().Normal;
            Vector3D spotDirection = (this.RayDrawingObject.Propertys.RaytracingLightSource as ImportanceSurfaceWithSpotLightDescription).SpotDirection;
            float spotCutoff = (this.RayDrawingObject.Propertys.RaytracingLightSource as ImportanceSurfaceWithSpotLightDescription).SpotCutoff;
            float spotMix = (this.RayDrawingObject.Propertys.RaytracingLightSource as ImportanceSurfaceWithSpotLightDescription).SpotMix;
            if (spotDirection == null) spotDirection = diffuseDirection;

            this.lightDirectionSampler = new MixTwoFunctionsLightDirectionSampler(new UniformOverHalfSphereLightDirectionSampler(diffuseDirection), new UniformOverThetaRangeLightDirectionSampler(spotDirection, 0, spotCutoff * Math.PI / 180), spotMix);

            this.directLightingSampler = new RectangleDirectLightSourceSampler(flats, spotMix, new UniformOverThetaRangeLightDirectionSampler(-spotDirection, 0, spotCutoff * Math.PI / 180));

            this.surfaceSampler = CreateSampler(uvmaps, lightCreationData, lightDescription, this.lightDirectionSampler);
        }

        //Sende für jede Importance-Celle ein Photon aus
        private static ImportanceSurfacePointSampler CreateSampler(List<IUVMapable> uvmaps, ConstruktorDataForLightSourceSampler lightCreationData, ImportanceSurfaceWithSpotLightDescription lightDescription, ILightDirectionSampler lightDirectionSampler)
        {
            var sampler = new ImportanceSurfacePointSampler(uvmaps, lightDescription.CellSurfaceCount, lightDescription.CellSurfaceCount);

            IRandom rand = new Rand(0);
            var importancePhotonSender = new ImportancePhotonSender(lightCreationData.IntersectionFinder, lightCreationData.RayCamera, 3);
            for (int i = 0; i < sampler.SurfaceCells.Length; i++)
            {
                var surfaceCell = sampler.SurfaceCells[i];
                if (lightCreationData.StopTriggerForColorEstimatorCreation.IsCancellationRequested) break;
                lightCreationData.ProgressChangedHandler("Importance-Surface-Light", i * 100.0f / sampler.SurfaceCells.Length);

                //Beim ImportanceSurfaceWithSpot_CheckHowManyLightPathsAreVisible-Test sehe ich:
                //Wenn ich nur 1 Versuch pro Zelle mache, dann sind von 5000 Zellen zwar nur 194 enabled aber diese Zellen
                //Erzeugen Photonen wo 61% im Sichtbereich liegen.
                //Nehme ich 5 Versuche pro Zelle, dann sind 409 Zellen enabled und nur 44% der ausgesendeten Photonen im Sichtbereich
                //Um so mehr Versuche pro Zelle ich mache, um so mehr bleiben auch Zellen enabled, die erst nach mehreren Richtungssampelversuchen
                //mal ein Treffer landen.
                //Lösung damit die IsVisible-Zahl höher wird: Mit dem sendCountDividedByVisibleCount-Faktor bekommen Zellen, die nur wenig helfen weniger Gewicht
                for (int k = 0; k < 5; k++) //Pro Zelle 5 Versuche
                {
                    var surfacPoint = surfaceCell.SampleSurfacePoint(rand.NextDouble(), rand.NextDouble());
                    var direction = lightDirectionSampler.SampleDirection(rand.NextDouble(), rand.NextDouble(), rand.NextDouble());

                    var photonPoints = importancePhotonSender.SendPhoton(surfacPoint.Position, direction.Direction, surfacPoint.PointSampler as IIntersecableObject, rand);

                    bool isVisible = photonPoints.Any(x => x.IsVisibleFromCamera);

                    surfaceCell.ExtraData.PhotonSendCounter++;
                    if (isVisible) surfaceCell.ExtraData.PhotonVisibleCounter++;
                }

            }


            foreach (var cell in sampler.SurfaceCells)
            {
                if (cell.ExtraData.PhotonVisibleCounter == 0) 
                    cell.IsEnabled = false;
                else
                {
                    //So viel Prozent der ausgesendeten Photonen liegen im Sichtbereich
                    float sendCountDividedByVisibleCount = cell.ExtraData.PhotonVisibleCounter / (float)cell.ExtraData.PhotonSendCounter;
                    cell.UpdateWeight(cell.Weight * sendCountDividedByVisibleCount);
                }
                    
            }

            sampler.UpdateSamplerAfterChangingCellEnablingFlags();

            return sampler;
        }

        public float GetDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            return this.directLightingSampler.GetDirectLightingPdfA(eyePoint, pointOnLight.Position);
        }

        public float GetMultipleDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            return this.directLightingSampler.GetDirectLightingPdfA(eyePoint, pointOnLight.Position);
        }

        public List<DirectLightingSampleResult> GetRandomPointOnLightList(Vector3D eyePoint, IRandom rand)
        {
            return new List<DirectLightingSampleResult>() { this.directLightingSampler.SampleToLightDirection(eyePoint, rand) };
        }

        public float GetEmissionForEyePathHitLightSourceDirectly(IntersectionPoint pointOnLight, Vector3D eyePoint, Vector3D directionFromEyeToLightPoint)
        {
            if (pointOnLight.FlatNormal * directionFromEyeToLightPoint < 0)
                return this.EmissionPerArea * this.lightDirectionSampler.GetPdfW(-directionFromEyeToLightPoint);
            return 0;
        }

        //Non-Uniformes Sampling für das DirectLighting
        public DirectLightingSampleResult GetRandomPointOnLight(Vector3D eyePoint, IRandom rand)
        {
            return this.directLightingSampler.SampleToLightDirection(eyePoint, rand);
        }

        public float GetPdfWFromLightDirectionSampling(IntersectionPoint pointOnLight, Vector3D direction)
        {
            return this.lightDirectionSampler.GetPdfW(direction);
        }

        public float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight)
        {
            return this.surfaceSampler.PdfAFromRandomPointOnLightSourceSampling(pointOnLight);
        }

        //Uniform-Sampling fürs Lightpath erstellen
        public virtual SurfaceLightPointForLightPathCreation GetRandomPointForLightPathCreation(IRandom rand)
        {
            var sampleResult = this.surfaceSampler.SampleLocation(rand.NextDouble(), rand.NextDouble(), rand.NextDouble());
            var directionSample = this.lightDirectionSampler.SampleDirection(rand.NextDouble(), rand.NextDouble(), rand.NextDouble());

            return new SurfaceLightPointForLightPathCreation()
            {
                PointOnLight = IntersectionPoint.CreatePointOnLight(sampleResult.SurfacePoint.Position, sampleResult.SurfacePoint.Color, sampleResult.SurfacePoint.Normal, sampleResult.SurfacePoint.PointSampler as IIntersecableObject, this.RayDrawingObject),
                Direction = directionSample.Direction,
                PdfA = sampleResult.SurfacePoint.PdfA,
                PdfW = directionSample.PdfW,
                EmissionPerArea = this.EmissionPerArea * directionSample.PdfW,
            };
        }
    }
}
