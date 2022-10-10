using System.Collections.Generic;
using IntersectionTests;
using RayObjects;

namespace RaytracingLightSource.RayLightSource.Importance
{
    //Sampelt ein SurfacePunkt innerhalb einer Surfacecell, bei der man Photonenstatistik hat
    class ImportanceSurfacePointSampler
    {
        public class SurfaceExtraData
        {
            public int PhotonSendCounter = 0;   //Wie viele Importance-Photonen wurden insgesamt über diese Celle gesendet?
            public int PhotonVisibleCounter = 0;//Wie viel davon sind im Sichtbereich gelandet?
        }

        public ImportanceUVMapSampler<SurfaceExtraData>.UVMapCell[] SurfaceCells { get { return this.surfaceSampler.Cells; } }

        private readonly ImportanceUVMapListSampler<SurfaceExtraData> surfaceSampler;

        public ImportanceSurfacePointSampler(List<IUVMapable> uvmaps, int uSize, int vSize)
        {
            this.surfaceSampler = new ImportanceUVMapListSampler<SurfaceExtraData>(uvmaps, uSize, vSize);

            foreach (var surfaceCell in this.surfaceSampler.Cells)
            {
                surfaceCell.ExtraData = new SurfaceExtraData();

                surfaceCell.IsEnabled = true;
            }
            this.surfaceSampler.UpateRussiaRolleteSamplerAfterEnablingDisablingCells();
        }

        public void UpdateSamplerAfterChangingCellEnablingFlags()
        {
            this.surfaceSampler.UpateRussiaRolleteSamplerAfterEnablingDisablingCells();
        }

        public class SampleResult
        {
            public ImportanceUVMapSampler<SurfaceExtraData>.UVMapCell SurfaceCell;
            public SurfacePoint SurfacePoint;
        }

        //u1 = Surface-Celle auswählen
        //u2/u3 = Punkt(u/v) in Surface-Celle auswählen
        public SampleResult SampleLocation(double u1, double u2, double u3)
        {
            var surfacePoint = this.surfaceSampler.SampleSurfacePoint(u1, u2, u3, out ImportanceUVMapSampler<SurfaceExtraData>.UVMapCell surfaceCell);
            //var surfaceCell = this.surfaceSampler.GetCellFromPosition(new IntersectionPoint(new Vertex(surfacePoint.Position, surfacePoint.Normal), null, null, null, surfacePoint.PointSampler as IIntersecableObject, null));

            return new SampleResult()
            {
                SurfaceCell = surfaceCell,
                SurfacePoint = surfacePoint,
            };
        }

        public float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight)
        {
            return this.surfaceSampler.GetPdfA(pointOnLight);
        }
    }
}
