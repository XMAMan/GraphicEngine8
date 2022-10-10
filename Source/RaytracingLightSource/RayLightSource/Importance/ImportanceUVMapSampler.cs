using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GraphicMinimal;
using IntersectionTests;
using RayObjects;
using RaytracingRandom;

namespace RaytracingLightSource.RayLightSource.Importance
{
    //Bekommt Liste von Dreiecken/Quads/Kugeln und unterteilt jedes dieser Objekte in uSize*vSize Kästchen und erzeugt dann ein Zufallspunkt auf einen der aktivierten Kästchen
    class ImportanceUVMapListSampler<T> where T : new()
    {
        public ImportanceUVMapSampler<T>.UVMapCell[] Cells { get; private set; } //Hierüber kann ich Cellen dann enablen/disablen
        private readonly Dictionary<IIntersecableObject, ImportanceUVMapSampler<T>> uvmaps = new Dictionary<IIntersecableObject, ImportanceUVMapSampler<T>>();
        private RussiaRollete<ImportanceUVMapSampler<T>.UVMapCell> russiaRollete;

        public ImportanceUVMapListSampler(List<IUVMapable> uvmaps, int uSize, int vSize)
        {
            List<ImportanceUVMapSampler<T>.UVMapCell> cells = new List<ImportanceUVMapSampler<T>.UVMapCell>();
            foreach (var map in uvmaps)
            {
                var sampler = new ImportanceUVMapSampler<T>(map, uSize, vSize);
                this.uvmaps.Add(map, sampler);
                for (int u = 0; u < uSize; u++)
                    for (int v = 0; v < vSize; v++)
                        cells.Add(sampler.Cells[u, v]);
            }
            this.Cells = cells.ToArray();
        }

        public void UpateRussiaRolleteSamplerAfterEnablingDisablingCells()
        {
            List<ImportanceUVMapSampler<T>.UVMapCell> cellList = new List<ImportanceUVMapSampler<T>.UVMapCell>();

            float runningWeight = 0;
            for (int i=0;i<this.Cells.Length;i++)
            {
                var cell = this.Cells[i];
                if (cell.IsEnabled)
                {
                    runningWeight += cell.Weight;
                    cell.RunningWeight = runningWeight;
                    cellList.Add(cell);
                }
            }
            //Wenn nichts im Sichtbereich, dann mache kein Importancesampling
            if (cellList.Any() == false)
            {
                this.Cells.ToList().ForEach(x => x.IsEnabled = true);
                UpateRussiaRolleteSamplerAfterEnablingDisablingCells();
                return;
            }
            this.russiaRollete = new RussiaRollete<ImportanceUVMapSampler<T>.UVMapCell>(cellList);
        }

        public SurfacePoint SampleSurfacePoint(double u1, double u2, double u3, out ImportanceUVMapSampler<T>.UVMapCell cell)
        {
            if (this.russiaRollete == null || this.russiaRollete.Values.Any() == false) throw new Exception("Es muss erst mindestens eine Cell aktiviert werden. Rufe nach den aktivieren " + nameof(UpateRussiaRolleteSamplerAfterEnablingDisablingCells));

            var rusiaResult = this.russiaRollete.GetSample(u1);
            cell = rusiaResult.ResultValue;
            var result = cell.SampleSurfacePoint(u2, u3);
            result.PdfA *= rusiaResult.Pmf;

            return result;
        }

        public float GetPdfA(IntersectionPoint point)
        {
            var cell = GetCellFromPosition(point);
            if (cell == null || cell.IsEnabled == false) return 0;
            return this.russiaRollete.Pmf(cell) * cell.PdfA;
        }

        public ImportanceUVMapSampler<T>.UVMapCell GetCellFromPosition(IntersectionPoint point)
        {
            return this.uvmaps[point.IntersectedObject].GetCellFromPosition(point.Position);
        }
    }

    //Bestimmt zufällig ein Punkt auf ein einzelnen Dreieck/Quad/Kugel
    class ImportanceUVMapSampler<T> where T : new()
    {
        public class UVMapCell : IRussiaRolleteValue
        {
            private readonly IUVMapable uvmap;
            private readonly RectangleF uvSpace;

            public bool IsEnabled { get; set; } = false;
            public float Weight { get; private set; }   //Entspricht dem Flächeninhalt im Objektspace * (PhotonVisible/PhotonSend)
            public float RunningWeight { get; set; }

            public Vector3D NormalFromCenter { get; private set; } //Die Normale von der Mitte von der Celle
            public float PdfA { get; private set; }
            public T ExtraData { get; set; }

            public UVMapCell(IUVMapable uvmap, RectangleF uvSpace)
            {
                this.uvmap = uvmap;
                this.uvSpace = uvSpace;
                this.Weight = (float)uvmap.GetSurfaceAreaFromUVRectangle(uvSpace);
                this.PdfA = 1.0f / this.Weight;

                this.NormalFromCenter = this.uvmap.GetSurfacePointFromUAndV(uvSpace.X + uvSpace.Width / 2, uvSpace.Y + uvSpace.Height / 2).Normal;
            }

            public SurfacePoint SampleSurfacePoint(double u1, double u2)
            {
                double u = this.uvSpace.X + u1 * this.uvSpace.Width;
                double v = this.uvSpace.Y + u2 * this.uvSpace.Height;
                var sample = this.uvmap.GetSurfacePointFromUAndV(u, v);
                sample.PdfA = this.PdfA;
                return sample;
            }

            public void UpdateWeight(float newValue)
            {
                this.Weight = newValue;
                this.PdfA = 1.0f / this.Weight;
            }

        }

        public UVMapCell[,] Cells { get; private set; } //u=[0..1] | v=[0..1]
        private RussiaRollete<UVMapCell> russiaRollete;
        private readonly IUVMapable uvmap;

        public ImportanceUVMapSampler(IUVMapable uvmap, int uSize, int vSize)
        {
            this.Cells = new UVMapCell[uSize, vSize];
            for (int u = 0; u < uSize; u++)
                for (int v = 0; v < vSize; v++)
                {
                    var cell = new UVMapCell(uvmap, new RectangleF(u / (float)uSize, v / (float)vSize, 1.0f / uSize, 1.0f / vSize));

                    this.Cells[u, v] = cell;
                }

            this.uvmap = uvmap;
        }

        public void UpateRussiaRolleteSamplerAfterEnablingDisablingCells()
        {
            List<UVMapCell> cellList = new List<UVMapCell>();

            float runningWeight = 0;
            for (int u = 0; u < this.Cells.GetLength(0); u++)
            {
                for (int v = 0; v < this.Cells.GetLength(1); v++)
                {
                    var cell = this.Cells[u, v];
                    if (cell.IsEnabled)
                    {
                        runningWeight += cell.Weight;
                        cell.RunningWeight = runningWeight;
                        cellList.Add(cell);
                    }
                }
            }

            this.russiaRollete = new RussiaRollete<UVMapCell>(cellList);
        }

        public SurfacePoint SampleSurfacePoint(double u1, double u2, double u3)
        {
            if (this.russiaRollete == null || this.russiaRollete.Values.Any() == false) throw new Exception("Es muss erst mindestens eine Cell aktiviert werden. Rufe nach den aktivieren " + nameof(UpateRussiaRolleteSamplerAfterEnablingDisablingCells));

            var cell = this.russiaRollete.GetSample(u1);
            var result = cell.ResultValue.SampleSurfacePoint(u2, u3);
            result.PdfA *= cell.Pmf;

            return result;
        }

        public float GetPdfA(Vector3D position)
        {
            var cell = GetCellFromPosition(position);
            if (cell.IsEnabled == false) return 0;
            return this.russiaRollete.Pmf(cell) * cell.PdfA;
        }

        public UVMapCell GetCellFromPosition(Vector3D position)
        {
            this.uvmap.GetUAndVFromSurfacePoint(position, out double u, out double v);
            if (double.IsNaN(u) || double.IsNaN(v)) return null;
            int x = Math.Min((int)(u * Cells.GetLength(0)), Cells.GetLength(0) - 1);
            int y = Math.Min((int)(v * Cells.GetLength(1)), Cells.GetLength(1) - 1);
            return this.Cells[x, y];
        }
    }
}
