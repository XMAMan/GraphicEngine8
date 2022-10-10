using System.Drawing;

namespace GraphicPipelineCPU
{
    class MouseHitTest
    {
        public int CurrentObjectIdToDraw = -1;
        private int mouseHitTestResultObjId = -1;
        private Point mousePosition;
        private bool isMouseHitTestActive = false;
        private float mouseHitTestResultZ;

        public void StartMouseHitTest(Point mousePosition, float clearDepth)
        {
            this.isMouseHitTestActive = true;
            this.mousePosition = mousePosition;
            this.mouseHitTestResultObjId = -1;
            this.mouseHitTestResultZ = clearDepth;
        }

        public int GetMouseHitTestResult()
        {
            this.isMouseHitTestActive = false;
            return this.mouseHitTestResultObjId;
        }

        //Schaue ob das Objekt, was an der Stelle vom Mauszeiger gezeichnet werden soll, näher ist als das aktuell gefundene Objekt
        public bool IsMouseHitTestActive(Point drawPosition, float drawZ)
        {
            if (this.isMouseHitTestActive)
            {
                if (drawPosition.X >= this.mousePosition.X - 1 && drawPosition.X <= this.mousePosition.X + 1 &&
                    drawPosition.Y >= this.mousePosition.Y - 1 && drawPosition.Y <= this.mousePosition.Y + 1 &&
                    drawZ >= 0 && drawZ <= 1)
                {
                    if (drawZ < this.mouseHitTestResultZ)
                    {
                        this.mouseHitTestResultZ = drawZ;
                        this.mouseHitTestResultObjId = this.CurrentObjectIdToDraw;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
