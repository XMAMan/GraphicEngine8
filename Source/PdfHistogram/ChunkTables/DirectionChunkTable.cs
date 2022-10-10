using GraphicGlobal;
using GraphicMinimal;
using System;
using System.Collections.Generic;

namespace PdfHistogram
{
    //Man kann für ein Richtungsvektor Daten im generischen T-Eintrag speichern
    public class DirectionChunkTable<T> where T : new()
    {
        public T[,] Data { get; private set; } //phi=[0..2PI] | Theta=[0..PI]
        private SphericalCoordinateConverter sphere;

        private double dPhi;
        private double dTheta;

        public DirectionChunkTable(int phiSize, int thetaSize, SphericalCoordinateConverter converter)
        {
            this.Data = new T[phiSize, thetaSize];
            for (int phi = 0; phi < phiSize; phi++)
                for (int theta = 0; theta < thetaSize; theta++)
                {
                    this.Data[phi, theta] = new T();
                }
            this.sphere = converter;

            this.dPhi = 2 * Math.PI / this.Data.GetLength(0);
            this.dTheta = Math.PI / this.Data.GetLength(1);
        }

        public double GetDifferentialSolidAngle(Vector3D direction)
        {
            var sphereCoord = this.sphere.ToSphereCoordinate(direction);
            int thetaInt = Math.Min((int)(sphereCoord.Theta / (Math.PI) * Data.GetLength(1)), Data.GetLength(1) - 1);
            double theta = (thetaInt + 0.5) / Data.GetLength(1) * Math.PI;
            double differentialSolidAngle = Math.Sin(theta) * this.dPhi * this.dTheta;
            return differentialSolidAngle;
        }

        public T this[Vector3D direction]
        {
            get
            {
                var sphereCoord = this.sphere.ToSphereCoordinate(direction);
                int phi = Math.Min((int)(sphereCoord.Phi / (2 * Math.PI) * Data.GetLength(0)), Data.GetLength(0) - 1);
                int theta = Math.Min((int)(sphereCoord.Theta / (Math.PI) * Data.GetLength(1)), Data.GetLength(1) - 1);
                return this.Data[phi, theta];
            }
        }

        public IEnumerable<T> EntryCollection()
        {
            // Use yield return to return all 2D array elements.
            for (int phi = 0; phi < this.Data.GetLength(0); phi++)
            {
                for (int theta = 0; theta < this.Data.GetLength(1); theta++)
                {
                    yield return this.Data[phi, theta];
                }
            }
        }
    }
}
