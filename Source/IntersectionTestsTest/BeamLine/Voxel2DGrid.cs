using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using IntersectionTests.BeamLine;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IntersectionTestsTest.BeamLine
{
    //Erstellt im Einheitswürfel ein Grid, was in der XY-Ebene in size*size unterteilt ist und in der Z-Richtung aus einer Reihe
    class Voxel2DGrid
    {
        public CylinderGrid CylinderGrid;
        public float VoxelSize;
        private List<VoxelMock> voxels;
        private int gridSize;

        //2D-Grid-Konstruktor
        //Erstellt im Einheitswürfel ein Grid, was in der XY-Ebene in size*size unterteilt ist und in der Z-Richtung aus einer Reihe
        public Voxel2DGrid(int gridSize)
        {
            this.gridSize = gridSize;

            float v = 1.0f / gridSize; //Größe eines Voxel-Würfels
            this.VoxelSize = v;
            this.voxels = new List<VoxelMock>();
            for (int x = 0; x < gridSize; x++)
                for (int y = 0; y < gridSize; y++)
                {
                    Vector3D min = new Vector3D(x / (float)gridSize, y / (float)gridSize, 0);
                    Vector3D max = min + new Vector3D(v, v, v);
                    voxels.Add(new VoxelMock(new BoundingBox(min, max)));
                }
            this.CylinderGrid = new CylinderGrid(voxels.Cast<IIntersectableCylinder>().ToList(), gridSize);
        }


        //Zur Kontrollzwecken
        public List<LineBeamIntersectionPoint> GetAllIntersectionPoints(IQueryLine line)
        {
            List<LineBeamIntersectionPoint> intersectedVoxels = new List<LineBeamIntersectionPoint>();
            foreach (var voxel in this.voxels)
            {
                var box = voxel.GetAxialAlignedBoundingBox();
                if (box.ClipRayWithBoundingBox(line.Ray, out float tMin, out float tMax) && tMax > 0)
                {
                    if (tMin < 0) tMin = 0;
                    if (tMax > line.LongRayLength) tMax = line.LongRayLength;

                    if (tMax > tMin)
                    {
                        voxel.IntersectionCounter++;
                        intersectedVoxels.Add(new LineBeamIntersectionPoint() { IntersectedBeam = voxel });
                    }
                }
            }
            return intersectedVoxels;
        }

        public string GetExpectedString(QueryLine line)
        {
            ResetAllVoxelCounter();
            GetAllIntersectionPoints(line);
            return GetEnabledVoxelsString();
        }

        public string GetActualString(QueryLine line)
        {
            ResetAllVoxelCounter();
            this.CylinderGrid.GetAllIntersectionPoints(line);
            //this.CylinderGrid.GetAllIntersectionPoints1(line);
            return GetEnabledVoxelsString();
        }

        public Bitmap GetExpectedImage(QueryLine line)
        {
            ResetAllVoxelCounter();
            GetAllIntersectionPoints(line);
            return Create2DGridImageWithLine(line);
        }

        public Bitmap GetActualImage(QueryLine line)
        {
            ResetAllVoxelCounter();
            this.CylinderGrid.GetAllIntersectionPoints(line);
            return Create2DGridImageWithLine(line);
        }

        private string GetEnabledVoxelsString()
        {
            return string.Join(",", this.voxels.Select(x => x.IntersectionCounter == 0 ? "0" : "1"));
        }

        private void ResetAllVoxelCounter()
        {
            this.voxels.ForEach(x => x.IntersectionCounter = 0);
        }

        //pixSize = So viele Pixel ist ein Kästchen groß
        public Bitmap Create2DGridImageWithLine(QueryLine line, int pixSize = 10)
        {
            int s = gridSize * pixSize;
            Bitmap bild = new Bitmap(s * 3, s * 3);
            Graphics grx = Graphics.FromImage(bild);
            AddVoxelGrid(grx, pixSize);

            Vector3D p1 = line.Ray.Start;
            Vector3D p2 = line.Ray.Start + line.Ray.Direction * line.LongRayLength;
            grx.DrawLine(Pens.Blue, p1.X * s + s, p1.Y * s + s, p2.X * s + s, p2.Y * s + s);

            grx.Dispose();

            return bild;
        }

        //pixSize = So viele Pixel ist ein Kästchen groß
        public Bitmap Create2DGridImageWithRectangle(GridRectangle rec, int pixSize = 10)
        {
            int s = gridSize * pixSize;
            Bitmap bild = new Bitmap(s * 3, s * 3);
            Graphics grx = Graphics.FromImage(bild);
            AddVoxelGrid(grx, pixSize);

            Vector3D p1 = rec.Pos;
            Vector3D p2 = rec.Pos + rec.V1;
            Vector3D p3 = rec.Pos + rec.V1 + rec.V2;
            Vector3D p4 = rec.Pos + rec.V2;
            grx.DrawLine(Pens.Blue, p1.X * s + s, p1.Y * s + s, p2.X * s + s, p2.Y * s + s);
            grx.DrawLine(Pens.Blue, p2.X * s + s, p2.Y * s + s, p3.X * s + s, p3.Y * s + s);
            grx.DrawLine(Pens.Blue, p3.X * s + s, p3.Y * s + s, p4.X * s + s, p4.Y * s + s);
            grx.DrawLine(Pens.Blue, p1.X * s + s, p1.Y * s + s, p4.X * s + s, p4.Y * s + s);
            grx.Dispose();

            return bild;
        }

        private void AddVoxelGrid(Graphics grx, int pixSize)
        {
            int s = gridSize * pixSize;
            foreach (var v in this.voxels)
            {
                var box = v.GetAxialAlignedBoundingBox();
                int x = (int)(box.Min.X * s) + s;
                int y = (int)(box.Min.Y * s) + s;

                if (v.IntersectionCounter > 0) grx.FillRectangle(Brushes.Red, x, y, pixSize, pixSize);
                grx.DrawRectangle(Pens.Black, x, y, pixSize, pixSize);
            }
        }
    }

    class VoxelMock : IIntersectableCylinder
    {
        public Ray Ray => throw new NotImplementedException();

        public float Length => throw new NotImplementedException();

        public float Radius => throw new NotImplementedException();
        public float RadiusSqrt => throw new NotImplementedException();

        public BoundingBox GetAxialAlignedBoundingBox()
        {
            return this.box;
        }

        //Da diese Box hier Axial ausgerichet ist, kann ich diese Box somit auch für die NonAlignedBoundingBox verwenden
        public NonAlignedBoundingBox GetNonAlignedBoundingBox()
        {
            return new NonAlignedBoundingBox(this.box);
        }

        public LineBeamIntersectionPoint GetIntersectionPoint(IQueryLine line)
        {
            this.IntersectionCounter++;
            return new LineBeamIntersectionPoint() { IntersectedBeam = this };
        }

        private BoundingBox box;
        public VoxelMock(BoundingBox box)
        {
            this.box = box;
        }

        public int IntersectionCounter = 0;
    }
}
