namespace GraphicMinimal
{
    //Stellt die Unterteilung von ein 3D-Objekt in lauter Blöcke dar so das ich sofort für ein gegebenen 3D-Punkt
    //sagen kann, ob ich innerhalb vom Objekt bin oder außerhalb
    public class LegoGrid
    {
        public BoundingBox Box { get; private set; }    //BBox vom 3D-Objekt, was über ein Grid angenähert wird
        public float EdgeSize { get; private set; }     //Kantenlänge von ein einzelnen Grid-Würfel
        public byte[,,] Grid { get; private set; }      //0=Free; 1=-X; 2=+X; 4=-Y; 8=+Y; 16=-Z; 32=+Z; 64=InnerBlock

        public LegoGrid(BoundingBox box, float edgeSize, byte[,,] grid)
        {
            this.Box = box;
            this.EdgeSize = edgeSize;
            this.Grid = grid;
        }

        public bool IsPointInside(Vector3D pos)
        {
            if (this.Box.IsPointInside(pos) == false) return false;

            Vector3D index = (pos - this.Box.Min) / this.EdgeSize;
            int x = (int)index.X;
            int y = (int)index.Y;
            int z = (int)index.Z;
            if (x >= 0 && x < this.Grid.GetLength(0) &&
                y >= 0 && y < this.Grid.GetLength(1) &&
                z >= 0 && z < this.Grid.GetLength(2))
            {
                return this.Grid[x, y, z] != 0;
            }
                

            return false;
        }
    }
}
