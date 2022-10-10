using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using RayObjects;
using RayObjects.RayObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphicPanels.Helper
{
    //Erzeugt die Grid-Daten für ein Lego-Objekt. Steht hier und nicht im TriangleObjectGeneration da ich ein IntersectionFinder benötige.
    public class LegoGridCreator
    {
        private BoundingBox box;
        private IntersectionFinder intersectionFinder;
        private float edgeSize;
        private byte[,,] grid;

        public static LegoGrid Create(List<Triangle> triangles, int separations)
        {
            return new LegoGridCreator(triangles, separations).CreateGrid();
        }

        //separations = So oft wird die längste BoundingBox-Kante unterteilt
        private LegoGridCreator(List<Triangle> triangles, int separations)
        {
            var rayHeight = new RayDrawingObject(new ObjectPropertys(), null, null);
            this.intersectionFinder = new IntersectionFinder(triangles.Select(x => new RayTriangle(x, rayHeight)).Cast<IIntersecableObject>().ToList(), null);
            this.box = triangles.GetBoundingBox();

            this.edgeSize = box.MaxEdge / separations; //Größe für ein einzelnen Würfel
            int maxEdgeIndex = box.MedEdgeIndex;
            int notMax1 = -1, notMax2 = -1; //Indizes der Kanten, die nicht maxEdge sind
            int[] gridSize = new int[3];
            for (int i = 0; i < gridSize.Length; i++)
            {
                gridSize[i] = (int)((box.Max - box.Min)[i] / edgeSize);
                if (i != maxEdgeIndex)
                {
                    if (notMax1 == -1)
                        notMax1 = i;
                    else
                        notMax2 = i;
                }

            }
            this.grid = new byte[gridSize[0], gridSize[1], gridSize[2]]; //Wert: Anzahl der Würfelseiten, die an nichts angrenzen (Wert zwischen 0 und 6)
        }

        private LegoGrid CreateGrid()
        {
            MarkGridCells(); //0=Free; 99 = NotFree
            SetNotFreeNeighborCounter();//0=Free; 26 = Inner Cell
            SetCellSideBits();

            return new LegoGrid(this.box, this.edgeSize, this.grid);
        }

        private void MarkGridCells()
        {
            //Scanne von allen 3 Axen scanline-Mäßig das Objekt durch
            MarkGridCells(0, 1, 2);
            MarkGridCells(1, 0, 2);
            MarkGridCells(2, 0, 1);
        }

        private void MarkGridCells(int runningIndex, int index1, int index2)
        {
            for (int i = 0; i < this.grid.GetLength(index1); i++)
                for (int j = 0; j < this.grid.GetLength(index2); j++)
                {
                    Vector3D start = new Vector3D(box.Min);
                    start[index1] += this.edgeSize / 2 + this.edgeSize * i;
                    start[index2] += this.edgeSize / 2 + this.edgeSize * j;
                    start[runningIndex] -= 1; //Start mit Abstand von 1 vor BoundingBox

                    Vector3D direction = new Vector3D(0, 0, 0);
                    direction[runningIndex] = 1;

                    //Scanline on Ray
                    bool isInside = false;
                    IntersectionPoint runningPoint = this.intersectionFinder.GetIntersectionPoint(new Ray(start, direction), 0);
                    Vector3D insideStart = null;
                    while (runningPoint != null)
                    {
                        isInside = !isInside;
                        if (isInside) //Ich bin von außen nach innen gewechselt
                        {
                            insideStart = runningPoint.Position;
                        }
                        else //Ich wechsle von innen nach außen
                        {
                            //Markiere von insideStart bis runningPoint.Position
                            float markStart = insideStart[runningIndex];
                            float markEnd = runningPoint.Position[runningIndex];
                            while (markStart < markEnd)
                            {
                                Vector3D markPos = new Vector3D(insideStart);
                                markPos[runningIndex] = markStart;
                                Vector3D index = (markPos - this.box.Min) / edgeSize;
                                SetGridValue(index, 99); //Markiere erstmal mit Platzhalter 99 um zu zeigen das da überhapt was ist aber ich nicht weiß, welche Nachbarfelder davon frei sind

                                markStart += this.edgeSize;
                            }
                            insideStart = null;
                        }
                        runningPoint = this.intersectionFinder.GetIntersectionPoint(new Ray(runningPoint.Position, direction), 0, runningPoint.IntersectedObject);
                    }
                }
        }

        private void SetNotFreeNeighborCounter()
        {
            for (int x = 0; x < grid.GetLength(0); x++)
                for (int y = 0; y < grid.GetLength(1); y++)
                    for (int z = 0; z < grid.GetLength(2); z++)
                    {
                        if (this.grid[x, y, z] != 0)
                            this.grid[x, y, z] = (byte)(GetNotFreeNeighborCount(x, y, z) + 10); //Erzeuge Zahlen im Bereich von 10 bis 36
                    }

            for (int x = 0; x < grid.GetLength(0); x++)
                for (int y = 0; y < grid.GetLength(1); y++)
                    for (int z = 0; z < grid.GetLength(2); z++)
                    {
                        if (this.grid[x, y, z] != 0)
                            this.grid[x, y, z] -= 10;
                    }

        }

        private byte GetNotFreeNeighborCount(int x, int y, int z)
        {
            byte count = 0;
            for (int xi = -1; xi <= 1; xi++)
                for (int yi = -1; yi <= 1; yi++)
                    for (int zi = -1; zi <= 1; zi++)
                    {
                        bool isCenter = xi == 0 && yi == 0 && zi == 0;
                        if (isCenter == false)
                        {
                            if (GetGridValue(x + xi, y + yi, z + zi) > 0) count++;
                        }
                    }
            return count;
        }

        private void SetCellSideBits()
        {
            //Lege für jede Cell-Side fest, ob da eine Wand ist. Ein Grid-Würfel, wo alle 6 Wände an sind wird
            //InnerCell-Würfel genannt und hat den Wert 64 (Anstatt 63, um zu zeigen, dass er ein InnerCell ist)
            //Bearbeite nur die Cellen, die nicht 0 sind

            byte[,,] newGrid = new byte[this.grid.GetLength(0), this.grid.GetLength(1), this.grid.GetLength(2)];
            SetCellSideBits(1, 2, 0, (i, j, k) => this.grid[k, i, j], (i, j, k, v) => newGrid[k, i, j] |= v); //X
            SetCellSideBits(0, 2, 1, (i, j, k) => this.grid[i, k, j], (i, j, k, v) => newGrid[i, k, j] |= v); //Y
            SetCellSideBits(0, 1, 2, (i, j, k) => this.grid[i, j, k], (i, j, k, v) => newGrid[i, j, k] |= v); //Z

            this.grid = newGrid;
        }

        private void SetCellSideBits(int index1, int index2, int runningIndex, Func<int, int, int, byte> get, Action<int, int, int, byte> set)
        {
            Dictionary<string, byte> sideMap = new Dictionary<string, byte>()
            {
                { "0-1", 1 },   //-X
                { "0+1", 2 },   //+X
                { "1-1", 4 },   //-Y
                { "1+1", 8 },   //+Y
                { "2-1", 16 },  //-Z
                { "2+1", 32 },  //+Z
            };

            for (int i = 0; i < this.grid.GetLength(index1); i++)
                for (int j = 0; j < this.grid.GetLength(index2); j++)
                    for (int k = 0; k < this.grid.GetLength(runningIndex); k++)
                    {
                        bool free1 = k > 0 ? get(i, j, k - 1) == 0 : true;
                        bool free2 = get(i, j, k + 0) == 0;
                        bool free3 = k + 1 < this.grid.GetLength(runningIndex) ? get(i, j, k + 1) == 0 : true;

                        if (free2 == false)
                        {
                            //Linke Kante soll bleiben
                            if (free1) set(i, j, k, sideMap[runningIndex.ToString() + "-1"]);

                            //Rechte Kante soll bleiben
                            if (free3) set(i, j, k, sideMap[runningIndex.ToString() + "+1"]);
                        }

                        bool isInnerCell = get(i, j, k + 0) == 26;
                        if (isInnerCell)
                        {
                            set(i, j, k, 64); //Inner Cell
                        }
                    }
        }

        private void SetGridValue(Vector3D index, byte value)
        {
            int i = (int)index.X;
            int j = (int)index.Y;
            int k = (int)index.Z;
            if (i >= 0 && i < this.grid.GetLength(0) &&
                j >= 0 && j < this.grid.GetLength(1) &&
                k >= 0 && k < this.grid.GetLength(2)) this.grid[i, j, k] = value;
        }

        private byte GetGridValue(int i, int j, int k)
        {
            if (i >= 0 && i < this.grid.GetLength(0) &&
                j >= 0 && j < this.grid.GetLength(1) &&
                k >= 0 && k < this.grid.GetLength(2)) return this.grid[i, j, k];

            return 0;
        }

    }
}
