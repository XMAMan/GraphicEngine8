namespace GraphicPipelineCPU.ObjToWindowSpaceConversion
{
    class WindowSpaceLine
    {
        public WindowSpacePoint P1;
        public WindowSpacePoint P2;

        public WindowSpaceLine(WindowSpacePoint p1, WindowSpacePoint p2)
        {
            this.P1 = p1;
            this.P2 = p2;
        }
    }
}
