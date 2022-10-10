using GraphicGlobal;
using GraphicGlobal.MathHelper;
using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IntersectionTests.BeamLine
{
    //Grid für die Schnittpunktabfrage zwischen einer QueryLine(Strahl mit Start- und Endpunkt) und einer Menge von Zylindern    
    public class CylinderGrid : IBeamLineIntersector
    {
        private Voxel[,,] grid = null;
        private BoundingBox mAABB;
        private Vector3D cellSize; //Größe von ein einzelnen Voxel-Feld
        private Vector3D inverseCellSize;

        //size = Anzahl der Voxel bei längster BoundingBox-Kante
        public CylinderGrid(List<IIntersectableCylinder> cylinders, int gridSize = 256)
        {
            if (cylinders.Any() == false) return;

            this.mAABB = new BoundingBox(cylinders.Select(x => x.GetAxialAlignedBoundingBox()));
            Vector3D extent = (this.mAABB.Max - this.mAABB.Min);

            //Weg 1: Idee für die gridSize-Bestimmung: https://github.com/mmp/pbrt-v2/blob/master/src/accelerators/grid.cpp
            //int maxAxis = extent.MaxAxis();
            //float invMaxWidth = 1.0f / extent[maxAxis];
            //float cubeRoot = 3.0f * (float)Math.Pow(cylinders.Count, 1.0f / 3.0f);
            //float voxelsPerUnitDist = cubeRoot * invMaxWidth;
            //this.grid = new Voxel[MathExtensions.Clamp((int)(extent[0] * voxelsPerUnitDist), 1, 64), MathExtensions.Clamp((int)(extent[1] * voxelsPerUnitDist), 1, 64), MathExtensions.Clamp((int)(extent[2] * voxelsPerUnitDist), 1, 64)];

            //Weg 2: Von SmallUPBP Grid.hxx Zeile 318 intersect()
            float biggestVoxelEdge = extent.Max() / gridSize; //So lang ist die Kante von ein Voxelwürfel
            this.grid = new Voxel[(int)Math.Ceiling(extent.X / biggestVoxelEdge), (int)Math.Ceiling(extent.Y / biggestVoxelEdge), (int)Math.Ceiling(extent.Z / biggestVoxelEdge)];

            for (int x=0;x<this.grid.GetLength(0);x++)
                for (int y=0;y<this.grid.GetLength(1);y++)
                    for (int z=0;z<this.grid.GetLength(2);z++)
                    {
                        this.grid[x, y, z] = new Voxel();
                    }
            
            this.cellSize = new Vector3D(this.mAABB.XSize / this.grid.GetLength(0), this.mAABB.YSize / this.grid.GetLength(1), this.mAABB.ZSize / this.grid.GetLength(2));
            this.inverseCellSize = new Vector3D(1.0f / this.cellSize.X, 1.0f / this.cellSize.Y, 1.0f / this.cellSize.Z);

            
            foreach (var obj in cylinders)
            {
                VisitEachGridCellFromNonAlignedBoundingBox(obj.GetNonAlignedBoundingBox(), (cell) => 
                {
                    if (cell.Cylinders.Contains(obj) == false) cell.Cylinders.Add(obj);
                });
                
            }
        }
               

        private void VisitEachGridCellFromNonAlignedBoundingBox(NonAlignedBoundingBox box, Action<Voxel> visitCellAction)
        {
            
            //V3-Kante
            GoOntoRasterLine(box.Pos, box.Pos + box.V3, (cell1, tPoint1) =>
            {
                //V1-Kante
                GoOntoRasterLine(tPoint1, tPoint1 + box.V1, (cell2, tPoint2) =>
                {
                    //V2-Kante
                    GoOntoRasterLine(tPoint2, tPoint2 + box.V2, (cell3, tPoint3) =>
                    {
                        visitCellAction(cell3);
                    });
                });
            });
        }

        //Nur mal ganz kurz zu Testzwecken (Soll ganz schnell wieder weg)
        //Am Punkt pos hängen die nicht normierten Richtungsvektoren v1 und v2 dran. Sie spannen ein 2D-Rechteck, was nicht axial ist, auf 
        public void AddRectangleToGrid(Vector3D pos, Vector3D v1, Vector3D v2)
        {
            List<LineBeamIntersectionPoint> points = new List<LineBeamIntersectionPoint>();

            GoOntoRasterRec(pos, v1, v2, (cell, tPoint) =>
            {
                points.AddRange(cell.GetAllIntersectionPoints(null, points)); //Erhöhe Counter bei Celle
            });
        }

        private void GoOntoRasterRec(Vector3D pos, Vector3D v1, Vector3D v2, Action<Voxel, Vector3D> visitCellAction)
        {
            //Idee, lauf auf Linie von pos nach pos+v1 entlang und von dort aus dann lauter Linien nach v2
            GoOntoRasterLine(pos, pos + v1, (cell, tPoint) =>
            {
                GoOntoRasterLine(tPoint, tPoint + v2, visitCellAction);
            });
        }


        //Laufe durch das Gitter entlang der Linie p1-p2 und rufe für jede besuchte Celle die visitCellAction
        private void GoOntoRasterLine(Vector3D p1, Vector3D p2, Action<Voxel, Vector3D> visitCellAction)
        {
            Vector3D dir = p2 - p1;
            float length = dir.Length();
            if (length < 0.0001f) return;
            dir /= length;

            float[] invdir = new float[] { 1.0f / dir.X, 1.0f / dir.Y, 1.0f / dir.Z };
            Vector3D invDir = new Vector3D(float.IsInfinity(invdir[0]) ? 100000 : invdir[0], float.IsInfinity(invdir[1]) ? 100000 : invdir[1], float.IsInfinity(invdir[2]) ? 100000 : invdir[2]);

            if (this.mAABB.ClipRayWithBoundingBox(new Ray(p1, dir), out float tMin, out float tMax) && tMax > 0)
            {
                if (tMin < 0) tMin = 0;
                if (tMax > length) tMax = length;
                if (tMax <= tMin) return; //Abbruch

                Vector3D delta = Vector3D.Mult(this.cellSize, invDir);                
                Vector3D enter = ToLocalPos(p1 + dir * tMin);
                int[] ipos = new int[] { PosToVoxel(enter.X, 0), PosToVoxel(enter.Y, 1), PosToVoxel(enter.Z, 2) };
                Voxel cell = this.grid[ipos[0], ipos[1], ipos[2]];
                int[] shift = new int[3];
                int[] step = new int[3];
                int[] check = new int[3];

                for (int i = 0; i < 3; ++i)
                {
                    if (dir[i] >= 0.0f)
                    {
                        shift[i] = 1;
                        check[i] = this.grid.GetLength(i);
                        step[i] = 1;
                    }
                    else
                    {
                        shift[i] = 0;
                        check[i] = -1;
                        step[i] = -1;
                        delta[i] = -delta[i];
                    }
                }

                Vector3D l = Vector3D.Mult(Vector3D.Mult(new Vector3D(ipos[0] + shift[0] - enter.X, ipos[1] + shift[1] - enter.Y, ipos[2] + shift[2] - enter.Z), invDir), this.cellSize);
                float t = tMin;

                do
                {
                    visitCellAction(cell, p1 + dir * t);

                    // Move to the next cell.
                    int minAxis = l.MinAxis();
                    t += l[minAxis];

                    l -= new Vector3D(l[minAxis]);
                    l[minAxis] = delta[minAxis];
                    ipos[minAxis] += step[minAxis];
                    if (ipos[minAxis] == check[minAxis]) break;
                    cell = this.grid[ipos[0], ipos[1], ipos[2]];
                } while (t < tMax);

                Vector3D stop = ToLocalPos(p1 + dir * (tMax - 0.0001f));
                int[] ipos1 = new int[] { PosToVoxel(stop.X, 0), PosToVoxel(stop.Y, 1), PosToVoxel(stop.Z, 2) };
                visitCellAction(this.grid[ipos1[0], ipos1[1], ipos1[2]], p1 + dir * (tMax - 0.0001f));
            }
        }

        //Quelle: SmallUPBP Grid.hxx Zeile 318 intersect()
        public List<LineBeamIntersectionPoint> GetAllIntersectionPoints(IQueryLine line)
        {           
            List<LineBeamIntersectionPoint> points = new List<LineBeamIntersectionPoint>();
            if (this.grid == null) return points;

            float[] invdir = new float[] { 1.0f / line.Ray.Direction.X, 1.0f / line.Ray.Direction.Y, 1.0f / line.Ray.Direction.Z };
            //Vector3D invDir = new Vector3D(float.IsInfinity(invdir[0]) ? float.MaxValue : invdir[0], float.IsInfinity(invdir[1]) ? float.MaxValue : invdir[1], float.IsInfinity(invdir[2]) ? float.MaxValue : invdir[2]);
            Vector3D invDir = new Vector3D(float.IsInfinity(invdir[0]) ? 100000 : invdir[0], float.IsInfinity(invdir[1]) ? 100000 : invdir[1], float.IsInfinity(invdir[2]) ? 100000 : invdir[2]);

            if (this.mAABB.ClipRayWithBoundingBox(line.Ray, out float tMin, out float tMax) && tMax > 0)
            {
                if (tMin < 0) tMin = 0;
                if (tMax > line.LongRayLength) tMax = line.LongRayLength;
                if (tMax <= tMin) return points;

                Vector3D delta = Vector3D.Mult(this.cellSize, invDir);
                Vector3D enter = ToLocalPos(line.Ray.Start + line.Ray.Direction * tMin);
                int[] ipos = new int[] { PosToVoxel(enter.X, 0), PosToVoxel(enter.Y, 1), PosToVoxel(enter.Z, 2) };
                Voxel cell = this.grid[ipos[0], ipos[1], ipos[2]];
                int[] shift = new int[3];
                int[] step = new int[3];
                int[] check = new int[3];

                for (int i=0;i<3; ++i)
                {
                    if (line.Ray.Direction[i] >= 0.0f)
                    {
                        shift[i] = 1;
                        check[i] = this.grid.GetLength(i);
                        step[i] = 1;
                    }else
                    {
                        shift[i] = 0;
                        check[i] = -1;
                        step[i] = -1;
                        delta[i] = -delta[i];
                    }
                }

                Vector3D l = Vector3D.Mult(Vector3D.Mult(new Vector3D(ipos[0] + shift[0] - enter.X, ipos[1] + shift[1] - enter.Y, ipos[2] + shift[2] - enter.Z), invDir), this.cellSize);
                float t = tMin;

                do
                {
                    points.AddRange(cell.GetAllIntersectionPoints(line, points));

                    // Move to the next cell.
                    int minAxis = l.MinAxis();
                    t += l[minAxis];
                    l -= new Vector3D(l[minAxis]);
                    l[minAxis] = delta[minAxis];
                    ipos[minAxis] += step[minAxis];
                    if (ipos[minAxis] == check[minAxis]) break;
                    cell = this.grid[ipos[0], ipos[1], ipos[2]];
                } while (t < tMax);
            }

            return points;
        }
        private Vector3D ToLocalPos(Vector3D globalPos)
        {
            return Vector3D.Mult(globalPos - this.mAABB.Min, this.inverseCellSize);
        }

        private int PosToVoxel(float pos, int axis)
        {
            return MathExtensions.Clamp((int)pos, 0, this.grid.GetLength(axis));
        }

        //Quelle: https://github.com/mmp/pbrt-v2/blob/master/src/accelerators/grid.cpp Zeile 123: Intersect() -> Funktioniert genau so gut wie GetAllIntersectionPoints
        public List<LineBeamIntersectionPoint> GetAllIntersectionPoints1(IQueryLine line)
        {
            List<LineBeamIntersectionPoint> points = new List<LineBeamIntersectionPoint>();
            if (this.grid == null) return points;

            if (this.mAABB.ClipRayWithBoundingBox(line.Ray, out float tMin, out float tMax) && tMax > 0)
            {
                if (tMin < 0) tMin = 0;
                if (tMax > line.LongRayLength) tMax = line.LongRayLength;
                if (tMax <= tMin) return points;

                float rayT = tMin;
                Vector3D gridIntersect = line.Ray.Start + line.Ray.Direction * tMin;

                // Set up 3D DDA for ray
                float[] nextCrossingT = new float[3], deltaT = new float[3];
                int[] step = new int[3], Out = new int[3], pos = new int[3];
                for (int axis = 0;axis < 3;++axis)
                {
                    // Compute current voxel for axis
                    pos[axis] = PosToVoxel1(gridIntersect, axis);
                    if (line.Ray.Direction[axis] >= 0)
                    {
                        // Handle ray with positive direction for voxel stepping
                        nextCrossingT[axis] = rayT + (VoxelToPos1(pos[axis] + 1, axis) - gridIntersect[axis]) / line.Ray.Direction[axis];
                        deltaT[axis] = this.cellSize[axis] / line.Ray.Direction[axis];
                        step[axis] = 1;
                        Out[axis] = this.grid.GetLength(axis);
                    }
                    else
                    {
                        // Handle ray with negative direction for voxel stepping
                        nextCrossingT[axis] = rayT + (VoxelToPos1(pos[axis], axis) - gridIntersect[axis]) / line.Ray.Direction[axis];
                        deltaT[axis] = -this.cellSize[axis] / line.Ray.Direction[axis];
                        step[axis] = -1;
                        Out[axis] = -1;
                    }
                }

                int[] cmpToAxis = new int[] { 2, 1, 2, 1, 2, 2, 0, 0 };

                // Walk ray through voxel grid
                for (; ; )
                {
                    // Check for intersection in current voxel and advance to next
                    Voxel cell = this.grid[pos[0], pos[1], pos[2]];

                    points.AddRange(cell.GetAllIntersectionPoints(line, points));

                    // Advance to next voxel
                    // Find _stepAxis_ for stepping to next voxel
                    int bits = ((nextCrossingT[0] < nextCrossingT[1] ? 1 : 0) << 2) +
                               ((nextCrossingT[0] < nextCrossingT[2] ? 1 : 0) << 1) +
                               ((nextCrossingT[1] < nextCrossingT[2] ? 1 : 0));
                    int stepAxis = cmpToAxis[bits];
                    if (tMax < nextCrossingT[stepAxis])
                        break;
                    pos[stepAxis] += step[stepAxis];
                    if (pos[stepAxis] == Out[stepAxis])
                        break;
                    nextCrossingT[stepAxis] += deltaT[stepAxis];
                }
            }

            return points;
        }
        // GridAccel Private Methods
        private int PosToVoxel1(Vector3D P, int axis)
        {
            int v = (int)((P[axis] - this.mAABB.Min[axis]) * this.inverseCellSize[axis]);
            return MathExtensions.Clamp(v, 0, this.grid.GetLength(axis));
        }

        private float VoxelToPos1(int p, int axis)
        {
            return this.mAABB.Min[axis] + p * this.cellSize[axis];
        }



        class Voxel
        {
            public List<IIntersectableCylinder> Cylinders = new List<IIntersectableCylinder>();

            public List<LineBeamIntersectionPoint> GetAllIntersectionPoints(IQueryLine line, List<LineBeamIntersectionPoint> alreadyFondedPoints)
            {
                List<LineBeamIntersectionPoint> points = new List<LineBeamIntersectionPoint>();
                foreach (var cylinder in this.Cylinders)
                {
                    if (alreadyFondedPoints.Any(x => x.IntersectedBeam == cylinder)) continue; //Verhindere, dass Beam mehrmals in Liste vorkommt

                    var p = cylinder.GetIntersectionPoint(line);
                    if (p != null) points.Add(p);
                }
                return points;
            }
        }
    }
}
