using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using IntersectionTests;
using RayObjects;
using RaytracingLightSource.Basics.LightDirectionSampler;

namespace RaytracingLightSource.RayLightSource.Importance
{
    //Bekommt Liste von Dreiecken/Quads/Kugeln und unterteilt erst jedes dieser Objekte in lauter Surface-Kästchen und auf jedem 
    //Surface-Kästchen wird dann eine Halbkugel platziert, welche auch wieder in Richtungszellen unterteilt ist
    class ImportanceSurfaceAndDirectionSampler
    {
        public class DirectionExtraData
        {
            public int PhotonSendCounter = 0;   //Wie viele Importance-Photonen wurden insgesamt über diese Celle gesendet?
            public int PhotonVisibleCounter = 0;//Wie viel davon sind im Sichtbereich gelandet?
        }

        public class SurfaceExtraData
        {
            public ImportanceLightDirectionSampler<DirectionExtraData> DirectionSampler { get; set; }
        }

        public ImportanceLightDirectionSampler<DirectionExtraData>.DirectionCell[] DirectionCells { get; private set; } //Hierüber kann ich Cellen dann enablen/disablen. Nach den Änderungen muss UpdateSamplerAfterChangingCellEnablingFlags() aufgerufen werden
        public ImportanceUVMapSampler<SurfaceExtraData>.UVMapCell[] SurfaceCells { get { return this.surfaceSampler.Cells; } }

        private readonly ImportanceUVMapListSampler<SurfaceExtraData> surfaceSampler;

        public ImportanceSurfaceAndDirectionSampler(List<IUVMapable> uvmaps, int uSize, int vSize, int phiSize, int thetaSize)
        {
            this.surfaceSampler = new ImportanceUVMapListSampler<SurfaceExtraData>(uvmaps, uSize, vSize);

            List<ImportanceLightDirectionSampler<DirectionExtraData>.DirectionCell> directionCells = new List<ImportanceLightDirectionSampler<DirectionExtraData>.DirectionCell>();

            foreach (var surfaceCell in this.surfaceSampler.Cells)
            {
                var directionSampler = new ImportanceLightDirectionSampler<DirectionExtraData>(surfaceCell.NormalFromCenter, phiSize, thetaSize);

                for (int phi = 0; phi < directionSampler.Cells.GetLength(0); phi++)
                    for (int theta = 0; theta < directionSampler.Cells.GetLength(1); theta++)
                    {
                        var directionCell = directionSampler.Cells[phi, theta];
                        directionCells.Add(directionCell);
                        directionCell.IsEnabled = true;
                        directionCell.ExtraData = new DirectionExtraData();
                    }

                directionSampler.UpateRussiaRolleteSamplerAfterEnablingDisablingCells();

                surfaceCell.ExtraData = new SurfaceExtraData()
                {
                    DirectionSampler = directionSampler
                };

                surfaceCell.IsEnabled = true;
            }
            this.surfaceSampler.UpateRussiaRolleteSamplerAfterEnablingDisablingCells();
            this.DirectionCells = directionCells.ToArray();
        }

        public void UpdateSamplerAfterChangingCellEnablingFlags()
        {
            foreach (var surfaceCell in this.surfaceSampler.Cells)
            {
                if (surfaceCell.ExtraData.DirectionSampler.CellCollection().All(x => x.IsEnabled == false)) surfaceCell.IsEnabled = false;
                surfaceCell.ExtraData.DirectionSampler.UpateRussiaRolleteSamplerAfterEnablingDisablingCells();
            }
            this.surfaceSampler.UpateRussiaRolleteSamplerAfterEnablingDisablingCells();
        }

        public class SampleResult
        {
            public ImportanceUVMapSampler<SurfaceExtraData>.UVMapCell SurfaceCell;
            public SurfacePoint SurfacePoint;
            public ImportanceLightDirectionSampler<DirectionExtraData>.DirectionCell DirectionCell;
            public LightDirectionSamplerResult DirectionResult;
        }

        //u1 = Surface-Celle auswählen
        //u2/u3 = Punkt(u/v) in Surface-Celle auswählen
        //u4 = Direction-Celle auswählen
        //u5/u6 = Phi/Theta in Celle Sampeln
        public SampleResult SampleLocationAndDirection(double u1, double u2, double u3, double u4, double u5, double u6)
        {
            var surfacePoint = this.surfaceSampler.SampleSurfacePoint(u1, u2, u3, out ImportanceUVMapSampler<SurfaceExtraData>.UVMapCell surfaceCell);
            //var surfaceCell1 = this.surfaceSampler.GetCellFromPosition(new IntersectionPoint(new Vertex(surfacePoint.Position, surfacePoint.Normal), null, null, null, null, null, surfacePoint.PointSampler as IIntersecableObject, null));
            var directionResult = surfaceCell.ExtraData.DirectionSampler.SampleDirection(u4, u5, u6);
            //var directionCell = surfaceCell.ExtraData.DirectionSampler.GetCellFromDirection(directionResult.Direction);

            return new SampleResult()
            {
                //SurfaceCell = surfaceCell,
                SurfacePoint = surfacePoint,
                //DirectionCell = directionCell,
                DirectionResult = directionResult
            };
        }

        public float GetPdfWFromLightDirectionSampling(IntersectionPoint pointOnLight, Vector3D direction)
        {
            var surfaceCell = this.surfaceSampler.GetCellFromPosition(pointOnLight);
            if (surfaceCell == null) return 0;
            return surfaceCell.ExtraData.DirectionSampler.GetPdfW(direction);
        }

        public float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight)
        {
            return this.surfaceSampler.GetPdfA(pointOnLight);
        }
    }
}
