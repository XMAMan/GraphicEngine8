using GraphicGlobal;
using GraphicMinimal;
using RaytracingRandom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RaytracingLightSource.Basics.LightDirectionSampler
{
    //Der Theta- und Phi-Bereich ist in lauter Bereiche(Cellen) unterteilt. Jede Celle kann ein/ausgeschaltet werden
    //Beim sampeln wird eine Celle gleichmäßig in Abhängigkeit von ihren Flächeninhalt ausgewählt und innerhalb der Celle
    //dann ein Richtungsvektor bestimmt
    class ImportanceLightDirectionSampler<T> : ILightDirectionSampler
    {
        public class DirectionCell : ILightDirectionSampler, IRussiaRolleteValue
        {
            public bool IsEnabled { get; set; } = false;

            public float Weight { get; private set; } //Entspricht dem Raumwinkel
            public float RunningWeight { get; set; }

            public T ExtraData { get; set; }

            private readonly CosWeightedSphereSegmentLightDirectionSampler cosSampler;

            public DirectionCell(Frame frame, double phiMin, double phiMax, double thetaMin, double thetaMax)
            {
                this.cosSampler = new CosWeightedSphereSegmentLightDirectionSampler(frame, phiMin, phiMax, thetaMin, thetaMax);
                this.Weight = (float)this.cosSampler.SolidAngle;
            }

            public LightDirectionSamplerResult SampleDirection(double u1, double u2, double u3)
            {
                return this.cosSampler.SampleDirection(u1, u2, u3);
            }

            public float GetPdfW(Vector3D direction)
            {
                return this.cosSampler.GetPdfW(direction);
            }

            public SimpleFunction GetBrdfOverThetaFunction()
            {
                return this.cosSampler.GetBrdfOverThetaFunction();
            }
        }

        public DirectionCell[,] Cells { get; private set; } //phi=[0..2PI] | Theta=[0..PI/2]
        private RussiaRollete<DirectionCell> russiaRollete;
        private readonly SphericalCoordinateConverter sphere;

        public ImportanceLightDirectionSampler(Vector3D normal, int phiSize, int thetaSize)
        {
            Frame frame = new Frame(normal);
            this.sphere = new SphericalCoordinateConverter(frame);

            double dPhi = 2 * Math.PI / phiSize;
            double dTheta = Math.PI / 2 / thetaSize;

            this.Cells = new DirectionCell[phiSize, thetaSize];
            for (int phi = 0; phi < phiSize; phi++)
                for (int theta = 0; theta < thetaSize; theta++)
                {
                    var cell = new DirectionCell(frame, phi * dPhi, phi * dPhi + dPhi, theta * dTheta, theta * dTheta + dTheta);

                    this.Cells[phi, theta] = cell;
                }

        }

        public void UpateRussiaRolleteSamplerAfterEnablingDisablingCells()
        {
            List<DirectionCell> cellList = new List<DirectionCell>();

            float runningWeight = 0;
            for (int phi = 0; phi < this.Cells.GetLength(0); phi++)
            {
                for (int theta = 0; theta < this.Cells.GetLength(1); theta++)
                {
                    var cell = this.Cells[phi, theta];
                    if (cell.IsEnabled)
                    {
                        runningWeight += cell.Weight;
                        cell.RunningWeight = runningWeight;
                        cellList.Add(cell);
                    }
                }
            }

            if (cellList.Any())
                this.russiaRollete = new RussiaRollete<DirectionCell>(cellList);
            else
                this.russiaRollete = null;
        }

        public IEnumerable<DirectionCell> CellCollection()
        {
            // Use yield return to return all 2D array elements.
            for (int phi = 0; phi < this.Cells.GetLength(0); phi++)
            {
                for (int theta = 0; theta < this.Cells.GetLength(1); theta++)
                {
                    yield return this.Cells[phi, theta];
                }
            }
        }

        public LightDirectionSamplerResult SampleDirection(double u1, double u2, double u3)
        {
            if (this.russiaRollete == null || this.russiaRollete.Values.Any() == false) throw new Exception("Es muss erst mindestens eine Cell aktiviert werden. Rufe nach den aktivieren " + nameof(UpateRussiaRolleteSamplerAfterEnablingDisablingCells));

            var cell = this.russiaRollete.GetSample(u1);
            var result = cell.ResultValue.SampleDirection(u2, u3, double.NaN);
            result.PdfW *= cell.Pmf;

            return result;
        }

        public float GetPdfW(Vector3D direction)
        {
            var cell = GetCellFromDirection(direction);
            if (cell.IsEnabled == false) return 0;
            return this.russiaRollete.Pmf(cell) * cell.GetPdfW(direction);
        }

        public DirectionCell GetCellFromDirection(Vector3D direction)
        {
            var sphereCoord = this.sphere.ToSphereCoordinate(direction);
            int phi = Math.Min((int)(sphereCoord.Phi / (2 * Math.PI) * Cells.GetLength(0)), Cells.GetLength(0) - 1);
            int theta = Math.Min((int)(sphereCoord.Theta / (Math.PI / 2) * Cells.GetLength(1)), Cells.GetLength(1) - 1);
            return this.Cells[phi, theta];
        }

        public SimpleFunction GetBrdfOverThetaFunction()
        {
            return new SimpleFunction((theta) =>
            {
                if (theta > Math.PI / 2) return 0;
                int thetaI = Math.Min((int)(theta / (Math.PI / 2) * Cells.GetLength(1)), Cells.GetLength(1) - 1);

                int phi = 0;
                var cell = this.Cells[phi, thetaI];
                return cell.IsEnabled ? cell.GetBrdfOverThetaFunction()(theta) : 0;
            });
        }
    }
}
