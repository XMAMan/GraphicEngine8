using System.Collections.Generic;
using System.Linq;
using RayObjects;
using GraphicMinimal;
using IntersectionTests;
using GraphicGlobal;
using RaytracingLightSource.RayLightSource.Importance;

namespace RaytracingLightSource
{
    //Bliebiges 3D-Objekt, auf dessen Oberfläche Importancecellen erzeugt werden, welche Importance-DirectionCellen haben
    class ImportanceSurfaceLight : IRayLightSource
    {
        public IIntersectableRayDrawingObject RayDrawingObject { get; private set; }
        public float EmittingSurfaceArea { get; private set; }
        public float Emission { get; private set; }
        public float EmissionPerArea { get; protected set; }

        private readonly ImportanceSurfaceAndDirectionSampler sampler;   //Für das LightTracing
        private readonly IEnumerable<IFlatRandomPointCreator> flats;     //Für das DirectLighting


        public ImportanceSurfaceLight(List<IUVMapable> uvmaps, ConstruktorDataForLightSourceSampler lightCreationData, ImportanceSurfaceLightDescription lightDescription)
        {
            this.RayDrawingObject = uvmaps.First().RayHeigh;
            this.flats = uvmaps.Cast<IFlatRandomPointCreator>();
            this.EmittingSurfaceArea = flats.Sum(x => x.SurfaceArea);
            this.Emission = this.RayDrawingObject.Propertys.RaytracingLightSource.Emission;
            this.EmissionPerArea = this.Emission / this.EmittingSurfaceArea;

            this.sampler = CreateSampler(uvmaps, lightCreationData, lightDescription);
        }

        //Sende für jede Importance-Celle ein Photon aus
        private static ImportanceSurfaceAndDirectionSampler CreateSampler(List<IUVMapable> uvmaps, ConstruktorDataForLightSourceSampler lightCreationData, ImportanceSurfaceLightDescription lightDescription)
        {
            var sampler = new ImportanceSurfaceAndDirectionSampler(uvmaps, lightDescription.CellSurfaceCount, lightDescription.CellSurfaceCount, lightDescription.CellDirectionCount, lightDescription.CellDirectionCount);

            int cellCount = sampler.SurfaceCells.Length * lightDescription.CellDirectionCount * lightDescription.CellDirectionCount;
            IRandom rand = new Rand(0);
            var importancePhotonSender = new ImportancePhotonSender(lightCreationData.IntersectionFinder, lightCreationData.RayCamera, 3);
            int progressCounter = 0;
            for (int i=0;i<sampler.SurfaceCells.Length;i++)
            {
                var surfaceCell = sampler.SurfaceCells[i];
                for (int u = 0; u < surfaceCell.ExtraData.DirectionSampler.Cells.GetLength(0);u++)
                    for (int v = 0; v < surfaceCell.ExtraData.DirectionSampler.Cells.GetLength(1); v++)
                    {
                        if (lightCreationData.StopTriggerForColorEstimatorCreation.IsCancellationRequested) break;
                        lightCreationData.ProgressChangedHandler("Importance-Survace-Light", progressCounter * 100.0f / cellCount);
                        progressCounter++;

                        var directionCell = surfaceCell.ExtraData.DirectionSampler.Cells[u, v];
                        var surfacPoint = surfaceCell.SampleSurfacePoint(rand.NextDouble(), rand.NextDouble());
                        var direction = directionCell.SampleDirection(rand.NextDouble(), rand.NextDouble(), rand.NextDouble());

                        var photonPoints = importancePhotonSender.SendPhoton(surfacPoint.Position, direction.Direction, surfacPoint.PointSampler as IIntersecableObject, rand);

                        bool isVisible = photonPoints.Any(x => x.IsVisibleFromCamera);

                        directionCell.ExtraData.PhotonSendCounter++;
                        if (isVisible) directionCell.ExtraData.PhotonVisibleCounter++;
                    }
            }

            foreach (var cell in sampler.DirectionCells)
            {
                if (cell.ExtraData.PhotonVisibleCounter == 0) cell.IsEnabled = false;
            }

            sampler.UpdateSamplerAfterChangingCellEnablingFlags();

            return sampler;
        }

        public float GetDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            return new FlatSurfaceListSamplingNonUniform(this.flats, eyePoint).PdfA(pointOnLight.IntersectedObject as IFlatRandomPointCreator);
        }

        public float GetMultipleDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            var flat = pointOnLight.IntersectedObject as IFlatObject;
            return 1.0f / flat.SurfaceArea;
        }

        public List<DirectLightingSampleResult> GetRandomPointOnLightList(Vector3D eyePoint, IRandom rand)
        {
            List<DirectLightingSampleResult> directionList = new List<DirectLightingSampleResult>();

            foreach (var flat in this.flats)
            {
                if (flat.IsPointAbovePlane(eyePoint))
                {
                    var point = flat.GetRandomPointOnSurface(rand);
                    directionList.Add(new DirectLightingSampleResult()
                    {
                        DirectionToLightPoint = Vector3D.Normalize(point.Position - eyePoint),
                        PdfA = point.PdfA,
                        LightSource = this.RayDrawingObject,
                        IsLightIntersectable = true,
                    });
                }
            }

            return directionList;
        }

        public float GetEmissionForEyePathHitLightSourceDirectly(IntersectionPoint pointOnLight, Vector3D eyePoint, Vector3D directionFromEyeToLightPoint)
        {
            if (pointOnLight.FlatNormal * directionFromEyeToLightPoint < 0)
                return this.EmissionPerArea;
            return 0;
        }

        //Non-Uniformes Sampling für das DirectLighting
        public DirectLightingSampleResult GetRandomPointOnLight(Vector3D eyePoint, IRandom rand)
        {
            var sampling = new FlatSurfaceListSamplingNonUniform(this.flats, eyePoint);
            var point = sampling.GetRandomPointOnSurface(rand);
            if (point == null) return null; //Wenn keine Fläche zum Hitpoint zeigt, dann gebe Null zurück

            return new DirectLightingSampleResult()
            {
                DirectionToLightPoint = Vector3D.Normalize(point.Position - eyePoint),
                PdfA = point.PdfA,
                LightSource = this.RayDrawingObject,
                IsLightIntersectable = true,
            };
        }

        public virtual float GetPdfWFromLightDirectionSampling(IntersectionPoint pointOnLight, Vector3D direction)
        {
            return this.sampler.GetPdfWFromLightDirectionSampling(pointOnLight, direction);
        }

        public virtual float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight)
        {
            return this.sampler.PdfAFromRandomPointOnLightSourceSampling(pointOnLight);
        }

        public SurfaceLightPointForLightPathCreation GetRandomPointForLightPathCreation(IRandom rand)
        {
            var sampleResult = this.sampler.SampleLocationAndDirection(rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble());
            return new SurfaceLightPointForLightPathCreation()
            {
                PointOnLight = IntersectionPoint.CreatePointOnLight(sampleResult.SurfacePoint.Position, sampleResult.SurfacePoint.Color, sampleResult.SurfacePoint.Normal, sampleResult.SurfacePoint.PointSampler as IIntersecableObject, this.RayDrawingObject),
                Direction = sampleResult.DirectionResult.Direction,
                PdfA = sampleResult.SurfacePoint.PdfA,
                PdfW = sampleResult.DirectionResult.PdfW,
                EmissionPerArea = this.EmissionPerArea,
            };
        }
    }
}
