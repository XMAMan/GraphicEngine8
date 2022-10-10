using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphicGlobal;
using GraphicMinimal;

namespace LineIntersectionTest
{
    public class UniformGridForLine : ILineLineIntersection
    {
        private GridCell[, ,] cells;
        private Vektor min, max;
        private Vektor range;
        private float minRange;
        private int teilung;

        public UniformGridForLine(List<ILine> lines, int teilung, Action<string, float> fortschrittsanzeigeChanged)
        {
            fortschrittsanzeigeChanged("Erstelle UniformGridForLine", 0);
            this.RawList = lines;


            CalculateMinMax();

            this.teilung = teilung;
            this.range = (max - min) / teilung;
            this.minRange = Math.Min(Math.Min(range.X, range.Y), range.Z);

            this.cells = new GridCell[teilung, teilung, teilung];

            for (int x = 0; x < teilung; x++)
                for (int y = 0; y < teilung; y++)
                    for (int z = 0; z < teilung; z++)
                    {
                        this.cells[x, y, z] = new GridCell();
                    }

            foreach (var line in lines)
            {
                for (float t = 0; t < line.RayLength; t += this.minRange)
                {
                    Vektor p = line.Ray.Start + line.Ray.Direction * t;

                    int xp = (int)((p.X - min.X) / range.X);
                    int yp = (int)((p.Y - min.Y) / range.Y);
                    int zp = (int)((p.Z - min.Z) / range.Z);
                    if (xp >= teilung) xp--;
                    if (yp >= teilung) yp--;
                    if (zp >= teilung) zp--;

                    if (this.cells[xp, yp, zp].lines.Contains(line) == false)
                    {
                        this.cells[xp, yp, zp].lines.Add(line);
                    }
                }
            }
        }

        private void CalculateMinMax()
        {
            this.min = new Vektor(float.MaxValue, float.MaxValue, float.MaxValue);
            this.max = new Vektor(float.MinValue, float.MinValue, float.MinValue);
            foreach (var line in this.RawList)
            {
                Vektor lineEnd = line.Ray.Start + line.Ray.Direction * line.RayLength;
                if (line.Ray.Start.X < min.X) min.X = line.Ray.Start.X;
                if (line.Ray.Start.Y < min.Y) min.Y = line.Ray.Start.Y;
                if (line.Ray.Start.Z < min.Z) min.Z = line.Ray.Start.Z;
                if (lineEnd.X < min.X) min.X = lineEnd.X;
                if (lineEnd.Y < min.Y) min.Y = lineEnd.Y;
                if (lineEnd.Z < min.Z) min.Z = lineEnd.Z;

                if (line.Ray.Start.X > this.max.X) this.max.X = line.Ray.Start.X;
                if (line.Ray.Start.Y > this.max.Y) this.max.Y = line.Ray.Start.Y;
                if (line.Ray.Start.Z > this.max.Z) this.max.Z = line.Ray.Start.Z;
                if (lineEnd.X > this.max.X) this.max.X = lineEnd.X;
                if (lineEnd.Y > this.max.Y) this.max.Y = lineEnd.Y;
                if (lineEnd.Z > this.max.Z) this.max.Z = lineEnd.Z;
            }
        }

        public List<LineLineIntersectionResult> GetIntersections(Ray querryRay, float rayLength)
        {
            List<LineLineIntersectionResult> resultList = new List<LineLineIntersectionResult>();

            GridCell lastCell = null;
            for (float t = 0; t < rayLength; t += this.minRange)
            {
                Vektor p = querryRay.Start + querryRay.Direction * t;

                int xp = (int)((p.X - min.X) / range.X);
                int yp = (int)((p.Y - min.Y) / range.Y);
                int zp = (int)((p.Z - min.Z) / range.Z);
                if (xp >= teilung) xp--;
                if (yp >= teilung) yp--;
                if (zp >= teilung) zp--;

                if (xp >= 0 && xp < this.teilung &&
                    yp >= 0 && yp < this.teilung &&
                    zp >= 0 && zp < this.teilung)
                {
                    var nextCell = this.cells[xp, yp, zp];
                    if (nextCell != lastCell)
                    {
                        lastCell = nextCell;
                        nextCell.IntersetionTest(querryRay, rayLength, resultList);
                    }
                }
            }
            return resultList;
        }

        public List<ILine> RawList { get; private set; }

        class GridCell
        {
            public List<ILine> lines = new List<ILine>();

            public void IntersetionTest(Ray ray, float rayLength, List<LineLineIntersectionResult> resultList)
            {
                foreach (var line in this.lines)
                {
                    var intersectionPoint = line.GetIntersection(ray, rayLength);
                    if (intersectionPoint != null)
                    {
                        resultList.Add(intersectionPoint);
                    }
                }
            }
        }
    }
}
