using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphicMinimal;
using GraphicGlobal;

namespace LineIntersectionTest
{
    public class GridForLine : ILineLineIntersection
    {
        private GridCell cell;

        public GridForLine(List<ILine> lines, Action<string, float> fortschrittsanzeigeChanged)
        {
            fortschrittsanzeigeChanged("Erstelle GridForLine", 0);
            this.cell = new GridCell(lines);
            this.cell.Subdivide(5, 5);
            this.RawList = lines;
        }

        public List<LineLineIntersectionResult> GetIntersections(Ray querryRay, float rayLength)
        {
            return this.cell.Intersection(querryRay, rayLength);
        }

        public List<ILine> RawList { get; private set; }

        //Entweder ist das ein Baumknoten oder ein Blattknoten
        class GridCell
        {
            private GridCell[, ,] subCells;   //wenn != null, dann ist das ein Baumknoten
            private List<ILine> lines;      //wenn != null, dann ist das ein Blattknoten
            private Vektor min, max;        //Wird für die Boundingbox-Abfrage benötigt

            public GridCell(List<ILine> lines)
            {
                this.lines = lines;
            }

            private GridCell()
            {
                this.lines = new List<ILine>();
            }

            #region Erstellen
            public void Subdivide(int lineCountPerLeaveNode, int maxRecurcionsDeep)
            {
                CalculateMinMax();

                if (maxRecurcionsDeep <= 0)
                {
                    return;
                }

                if (this.lines.Count > lineCountPerLeaveNode)
                {
                    OrderLinesIntoSubCells();
                    for (int x = 0; x < this.subCells.GetLength(0); x++)
                        for (int y = 0; y < this.subCells.GetLength(1); y++)
                            for (int z = 0; z < this.subCells.GetLength(2); z++)
                            {
                                this.subCells[x, y, z].Subdivide(lineCountPerLeaveNode, maxRecurcionsDeep - 1);
                            }
                }
            }

            private void OrderLinesIntoSubCells()
            {
                CalculateMinMax();

                int teilung = 2;

                Vektor range = (this.max - this.min) / teilung;
                float minRange = Math.Min(Math.Min(range.X, range.Y), range.Z);

                this.subCells = new GridCell[teilung, teilung, teilung];

                for (int x = 0; x < teilung; x++)
                    for (int y = 0; y < teilung; y++)
                        for (int z = 0; z < teilung; z++)
                        {
                            this.subCells[x, y, z] = new GridCell();
                        }

                foreach (var line in this.lines)
                {
                    for (float t = 0; t < line.RayLength; t += minRange)
                    {
                        Vektor p = line.Ray.Start + line.Ray.Direction * t;

                        int xp = (int)((p.X - min.X) / range.X);
                        int yp = (int)((p.Y - min.Y) / range.Y);
                        int zp = (int)((p.Z - min.Z) / range.Z);
                        if (xp >= teilung) xp--;
                        if (yp >= teilung) yp--;
                        if (zp >= teilung) zp--;

                        if (this.subCells[xp, yp, zp].lines.Contains(line) == false)
                        {
                            this.subCells[xp, yp, zp].lines.Add(line);
                        }
                    }
                }

                this.lines = null;
            }

            private void CalculateMinMax()
            {
                this.min = new Vektor(float.MaxValue, float.MaxValue, float.MaxValue);
                this.max = new Vektor(float.MinValue, float.MinValue, float.MinValue);
                foreach (var line in this.lines)
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
            #endregion

            #region Abfrage

            public List<LineLineIntersectionResult> Intersection(Ray ray, float rayLength)
            {
                List<LineLineIntersectionResult> resultList = new List<LineLineIntersectionResult>();
                IntersetionTest(ray, rayLength, resultList);
                return resultList;
            }

            private void IntersetionTest(Ray ray, float rayLength, List<LineLineIntersectionResult> resultList)
            {
                if (IntersectionTestBetweenBoxAndRay(ray, rayLength) == false) return;

                //Baumknotentest
                if (this.subCells != null)
                {
                    for (int x = 0; x < this.subCells.GetLength(0); x++)
                        for (int y = 0; y < this.subCells.GetLength(1); y++)
                            for (int z = 0; z < this.subCells.GetLength(2); z++)
                            {
                                this.subCells[x, y, z].IntersetionTest(ray, rayLength, resultList);
                            }
                }

                //Blattknotentest
                if (this.lines != null)
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

            //https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-box-intersection
            private bool IntersectionTestBetweenBoxAndRay(Ray ray, float rayLength)
            {
                float tmin = (min.X - ray.Start.X) / ray.Direction.X;
                float tmax = (max.X - ray.Start.X) / ray.Direction.X;

                if (tmin > tmax) Swap(ref tmin, ref tmax);

                float tymin = (min.Y - ray.Start.Y) / ray.Direction.Y;
                float tymax = (max.Y - ray.Start.Y) / ray.Direction.Y;

                if (tymin > tymax) Swap(ref tymin, ref tymax);

                if ((tmin > tymax) || (tymin > tmax))
                    return false;

                if (tymin > tmin)
                    tmin = tymin;

                if (tymax < tmax)
                    tmax = tymax;

                float tzmin = (min.Z - ray.Start.Z) / ray.Direction.Z;
                float tzmax = (max.Z - ray.Start.Z) / ray.Direction.Z;

                if (tzmin > tzmax) Swap(ref tzmin, ref tzmax);

                if ((tmin > tzmax) || (tzmin > tmax))
                    return false;

                if (tzmin > tmin)
                    tmin = tzmin;

                if (tzmax < tmax)
                    tmax = tzmax;

                return true;
            }

            private void Swap(ref float f1, ref float f2)
            {
                float temp = f1;
                f1 = f2;
                f2 = temp;
            }

            #endregion
        }
    }
}
