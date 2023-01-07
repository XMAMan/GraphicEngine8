using GraphicMinimal;
using SubpathGenerator;

namespace RaytracingMethods.McVcm
{
    //Wird benötigt, damit EyeMapVertexMerging den Fullpaths eine PixelPosition zuweisen kann
    class EyeSubPath : SubPath
    {
        public readonly Vector2D PixelPosition;

        public EyeSubPath(SubPath path, Vector2D pixelPosition)
            :base(path.Points, path.PathCreationTime)
        {
            this.PixelPosition = pixelPosition;
        }
    }
}
